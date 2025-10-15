﻿using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst.Intrinsics;
using UnityEngine;
using Verse;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;
using static UnityEngine.Networking.UnityWebRequest;
using static ZealousInnocence.CompRegressionMemory;
using static ZealousInnocence.DebugActions;

namespace ZealousInnocence
{
    public class Hediff_RegressionDamage : HediffWithComps
    {
        const int HealingTickInterval = 250;
        const int MaxDelta = HealingTickInterval * 3;

        // Pull settings once; if your mod updates settings live, replace with a property that re-fetches.
        private static ZealousInnocenceSettings Settings
            => LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();

        private int _lastHealTick;

        // Persisted state so age can return to what it was
        private long baselineBioTicks = -1;     // original biological age (ticks) when effect first applied
        private long lastBioSeen = -1;       // The last chronological age we have seen/updates ourself. The time difference to it is what we need to advance on the baseline
        private int lastWholeYears = -1;
        public bool forceTick = false;
        private BeardDef lastBeardType = null;
        private TickTimer resurrectTimer = new TickTimer();
        private TickTimer resurrectTimerLetter = new TickTimer();
        // Growth moments
        public static bool AllowBirthdayThrough = false;
        public bool suppressVanillaBirthdays = true;
        private int lastBaselineWholeYears = -1;
        private HashSet<int> deferredBirthdays = new();
        static readonly System.Reflection.MethodInfo MI_BirthdayBiological = AccessTools.Method(typeof(Pawn_AgeTracker), "BirthdayBiological");

        //public override bool ShouldRemove => false;
        static bool IsHumanlike(Pawn p) => p?.RaceProps?.Humanlike == true;
        static bool IsAnimal(Pawn p) => p?.RaceProps?.Animal == true;
        private SimpleCurve RegressionCurve = new SimpleCurve
        {
            new CurvePoint(0f, 0f),
            new CurvePoint(0.25f, 0.2f),
            new CurvePoint(0.5f, 0.5f),
            new CurvePoint(0.75f, 0.8f),
            new CurvePoint(1f, 1f)
        };
        private enum AgeStage3 { Baby, Child, Adult }
        private AgeStage3 StageFor(long bioTicks)
        {
            var (childCore, childEdge) = ChildBandTicks(pawn); // core=end of baby, edge=start of adult
            if (bioTicks < childCore) return AgeStage3.Baby;
            if (bioTicks < childEdge) return AgeStage3.Child;
            return AgeStage3.Adult;
        }
        private float ResurrectDurationSeconds = 60f;
        private float ResurrectMoveDurationSeconds = 30f;
        public Corpse Corpse { get => pawn.ParentHolder as Corpse; }
        public float BaseAgeYearFloat {
            get
            {
                if(baselineBioTicks <= 0) return 0f;
                return baselineBioTicks / GenDate.TicksPerYear;
            }
        }
        public float AgeYearFloat
        {
            get
            {
                return (float)pawn.ageTracker.AgeBiologicalTicks / GenDate.TicksPerYear;
            }
        }
        public bool PlayerControlled
        {
            get
            {
                return this.pawn.IsColonist && (this.pawn.HostFaction == null || this.pawn.IsSlave);
            }
        }
        public bool InProgress
        {
            get
            {
                return !this.resurrectTimer.Finished;
            }
        }

        public CompRegressionMemory Memory
        {
            get
            {
                return GetMemory(pawn);
            }
        }
        public static CompRegressionMemory GetMemory(Pawn pawn)
        {
            return pawn.TryGetComp<CompRegressionMemory>();
        }


        //public override bool Visible => !dormant && base.Visible;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _lastHealTick, "ZI_reg_lastHealTick", 0);

            Scribe_Values.Look(ref baselineBioTicks, "ZI_reg_baselineBioTicks", -1);
            Scribe_Values.Look(ref lastBioSeen, "ZI_reg_lastBioSeen", -1);

            Scribe_Defs.Look(ref lastBeardType, "ZI_reg_lastBeardType");
            Scribe_Deep.Look<TickTimer>(ref resurrectTimerLetter, "ZI_reg_resurrectTimerLetter", Array.Empty<object>());
            Scribe_Deep.Look<TickTimer>(ref resurrectTimer, "ZI_reg_resurrectTimer", Array.Empty<object>());
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if(resurrectTimer == null) resurrectTimer = new TickTimer();
                if(resurrectTimerLetter == null) resurrectTimerLetter = new TickTimer();
                resurrectTimer.OnFinish = new Action(Resurrect);
                resurrectTimerLetter.OnFinish = new Action(ResurrectLetterTrigger);
                if (lastBeardType == null)
                {
                    lastBeardType = BeardDefOf.NoBeard;
                    if (pawn.style?.beardDef != null) lastBeardType = pawn.style.beardDef;
                }
            }

            Scribe_Collections.Look(ref deferredBirthdays, "ZI_deferredBirthdays", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && deferredBirthdays == null) deferredBirthdays = new HashSet<int>();
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
#if DEBUG
            if (Settings.debugging && Settings.debuggingRegression && dinfo.HasValue) Log.Message($"[ZI] Regression PostAdd {pawn.LabelShort} from {dinfo.Value.Def?.defName} sev={Severity:0.###}");
#endif
            base.PostAdd(dinfo);

            if (pawn == null) return;

            // Capture baselines once
            if (baselineBioTicks < 0)
                baselineBioTicks = pawn.ageTracker.AgeBiologicalTicks;

            (int core, int edge) band = ChildBandTicks(pawn);
            int childCore = band.core;
            int childEdge = band.edge;
            lastWholeYears = pawn.ageTracker.AgeBiologicalYears;
            lastBeardType = pawn.style?.beardDef;

            forceTick = true;
            if (IsHumanlike(pawn)) new HediffGiver_BedWetting().OnIntervalPassed(pawn, null);
        }

        
        const float minSplit = 0.40f; // old pawns: child band starts at ~40%
        const float maxSplit = 0.75f; // young pawns: child band starts at ~75%
        const float youngRef = 0.3f;
        const float oldRef = 0.9f;
        const float babyExtraMax = 0.50f;  // 50% overdrive beyond 1.0 at birth
        const double babyExp = 0.75;       // curve shape (higher = gentler)

        // Convert a target age (years) to the severity s that would produce it.
        public float SeverityForTargetYears(float targetYears)
        {
            long targetTicks = Mathf.RoundToInt(targetYears * GenDate.TicksPerYear);
            return SeverityForTargetTicks(targetTicks);
        }
        public float SeverityForTargetTicks(long targetTicks)
        {
            float life = pawn.RaceProps.lifeExpectancy;
            float baseYears = baselineBioTicks / (float)GenDate.TicksPerYear;
            float f = Mathf.InverseLerp(youngRef * life, oldRef * life, baseYears);  // young→0, old→1
            float split = Mathf.Lerp(minSplit, maxSplit, f);                          // e.g., 0.40..0.75

            var (childCore, childEdge) = ChildBandTicks(pawn);
            const long babyFloor = 0;

            // Allow full range: [babyFloor .. baseline]
            long lo = Math.Min(baselineBioTicks, (long)babyFloor);
            long hi = Math.Max(baselineBioTicks, (long)babyFloor);
            targetTicks = Math.Max(lo, Math.Min(targetTicks, hi));

            const double eps = 1e-9;

            if (targetTicks >= childEdge)
            {
                // Adult zone inverse (s in [0..split])
                double den = (double)(childEdge - baselineBioTicks);
                double t = Math.Abs(den) > eps ? (targetTicks - baselineBioTicks) / den : 0.0;
                t = Math.Clamp(t, 0.0, 1.0);
                double s = split * Math.Pow(t, 1.0 / 1.25);
                return (float)s; // keep 0..split
            }
            else if (targetTicks >= childCore)
            {
                // Child zone inverse (s in [split..1])
                double den = (double)(childCore - childEdge);
                double r = Math.Abs(den) > eps ? (targetTicks - childEdge) / den : 0.0;
                r = Math.Clamp(r, 0.0, 1.0);
                double u = Math.Pow(r, 1.0 / 0.8);
                double s = split + (1.0 - split) * u;
                return (float)s; // keep ≤ 1
            }
            else
            {
                // Baby zone inverse → map childCore..0 to s in (1..1+extra)
                double span = (double)(childCore - babyFloor);
                double r = span > eps ? (childCore - targetTicks) / span : 1.0; // 0 at childCore, 1 at birth
                r = Math.Clamp(r, 0.0, 1.0);
                double s = 1.0 + babyExtraMax * Math.Pow(r, babyExp);
                return (float)s; // can exceed 1.0 when pushing into baby age
            }
        }
        public long BioTicksForSeverity(float s)
        {
            var (childCore, childEdge) = ChildBandTicks(pawn);

            float life = pawn.RaceProps.lifeExpectancy;
            float baseYears = baselineBioTicks / (float)GenDate.TicksPerYear;
            float f = Mathf.InverseLerp(youngRef * life, oldRef * life, baseYears);
            float split = Mathf.Lerp(minSplit, maxSplit, f); // e.g. 0.40..0.75

            const double eps = 1e-9;

            if (s < split)
            {
                // Adult zone: baseline → childEdge with exponent 1.25
                double t = s / Math.Max(split, eps);   // 0..1
                t = Math.Pow(t, 1.25);
                double d = baselineBioTicks + (childEdge - (double)baselineBioTicks) * t;
                long desired = (long)Math.Round(d);
                return Math.Max(0L, Math.Min(desired, Math.Max(baselineBioTicks, (long)childEdge)));
            }
            else if (s <= 1f)
            {
                // Child zone: childEdge → childCore with exponent 0.8
                double t = (s - split) / Math.Max(1.0 - split, eps); // 0..1
                t = Math.Pow(t, 0.8);
                double d = childEdge + (childCore - (double)childEdge) * t;
                long desired = (long)Math.Round(d);
                long lo = Math.Min((long)childCore, (long)childEdge);
                long hi = Math.Max((long)childCore, (long)childEdge);
                return Math.Max(lo, Math.Min(desired, hi));
            }
            else
            {
                // Baby zone: childCore → birth (0) for s in (1 .. 1 + babyExtraMax]

                // normalize overdrive portion to 0..1
                double o = (s - 1.0) / Math.Max(babyExtraMax, eps); // 0..1+
                o = Math.Clamp(o, 0.0, 1.0);

                // forward map (inverse of earlier): r in 0..1, then ease with 1/babyExp
                double r = Math.Pow(o, 1.0 / babyExp); // 0 at childCore, 1 at birth
                double span = (double)childCore;       // childCore → 0
                double d = childCore - span * r;

                long desired = (long)Math.Round(d);
                return Math.Max(0L, Math.Min(desired, (long)childCore));
            }
        }

        private void ApplyAgeMapping(bool force)
        {
            // Force backup methode call in case of already applied Damage
            if (baselineBioTicks < 0)
            {
                Log.Warning($"[ZI] Removing incomplete RegressionDamage from {pawn.LabelShortCap}");
                pawn.health.RemoveHediff(this);
                return;
            }

            AdvanceBaselineAndMaybeFireBirthdays();

            long desiredBio = BioTicksForSeverity(Severity);

            const long AgeAdjustInterval = 60L; // ticks

            if (force || Math.Abs(desiredBio - pawn.ageTracker.AgeBiologicalTicks) >= AgeAdjustInterval)
            {
                long prevBio = pawn.ageTracker.AgeBiologicalTicks;
                pawn.ageTracker.AgeBiologicalTicks = desiredBio;
                lastBioSeen = pawn.ageTracker.AgeBiologicalTicks; // We ignore our own changes!!
#if DEBUG
                if (Settings.debugging && Settings.debuggingRegression)
                    Log.Message($"[ZI] Regression now {desiredBio} at {Severity}");
#endif
                

                if(StageFor(desiredBio) != AgeStage3.Adult)
                {
                    Helper_Regression.healPawnBrain(pawn);
                }
                int curYears = pawn.ageTracker.AgeBiologicalYears;
                if (curYears != lastWholeYears)
                {
#if DEBUG
                    if (Settings.debugging && Settings.debuggingRegression)
                        Log.Message($"[ZI] Regression Year change {lastWholeYears} => {curYears}");
#endif
                    var curStage = StageFor(desiredBio);
                    if (curStage != StageFor(prevBio))
                    {
                        Building_Bed ownedBed = pawn.ownership.OwnedBed;
                        if (ownedBed != null && pawn.ageTracker.CurLifeStage.bodySizeFactor > ownedBed.def.building.bed_maxBodySize)
                        {
                            pawn.ownership.UnclaimBed();
                        }
                        switch (curStage)
                        {
                            case AgeStage3.Baby: OnEnterBaby(pawn); break;
                            case AgeStage3.Child: OnEnterChild(pawn); break;
                            case AgeStage3.Adult: OnEnterAdult(pawn); break;
                        }
                        
                    }

                    
                    SyncBirthdayHediffs(pawn, desiredBio);
                    ProcessDeferredBirthdaysOnAgeUp();
                    refreshAgeStageCache(pawn: pawn);
                    lastWholeYears = curYears;
                }
            }
        }
        private void AdvanceBaselineAndMaybeFireBirthdays()
        {
            long curBio = pawn.ageTracker.AgeBiologicalTicks;
            if (lastBioSeen < 0) lastBioSeen = curBio;

            long dtBio = curBio - lastBioSeen;
            if (dtBio > 0)
            {
                if (dtBio > MaxDelta)
                {
#if DEBUG
                    Log.Warning($"[ZI] Regression: bio delta {dtBio} over {MaxDelta}. Probably debugging step for {pawn.LabelShortCap}");
#endif
                }

                long prevBaseline = baselineBioTicks;
                baselineBioTicks += dtBio;
                lastBioSeen = curBio;

                int prevBaseYears = (lastBaselineWholeYears >= 0)
                    ? lastBaselineWholeYears
                    : Mathf.FloorToInt(prevBaseline / (float)GenDate.TicksPerYear);

                int curBaseYears = Mathf.FloorToInt(baselineBioTicks / (float)GenDate.TicksPerYear);

                if (curBaseYears > prevBaseYears)
                {
                    for (int y = prevBaseYears + 1; y <= curBaseYears; y++)
                    {
                        FireSyntheticBirthdayAtYear(pawn, y);
                    }
                }

                lastBaselineWholeYears = curBaseYears;
            }
            else
            {
                lastBioSeen = curBio;
            }
        }

        private void FireSyntheticBirthdayAtYear(Pawn pawn, int birthdayYear, bool deferIfMappedYounger = true)
        {
            if (MI_BirthdayBiological == null) return;
            if (deferredBirthdays == null) deferredBirthdays = new HashSet<int>();

            // Defer if the pawn’s *mapped* age hasn’t reached this year yet
            int mappedYears = pawn.ageTracker.AgeBiologicalYears;
            if (deferIfMappedYounger && mappedYears < birthdayYear)
            {
                deferredBirthdays.Add(birthdayYear);
#if DEBUG
                if (Settings.debugging && Settings.debuggingRegression)
                    Log.Message($"[ZI] Deferred synthetic birthday Y{birthdayYear} for {pawn.LabelShort} (mapped {mappedYears})");
#endif
                return;
            }

            // Deterministic seed so results are stable for (pawn, year)
            int seed = Gen.HashCombineInt(pawn.thingIDNumber, birthdayYear ^ 0x6B1A39);
            Rand.PushState(seed);
            try
            {
                // Let vanilla birthday + growth moment through our suppression patches
                AllowBirthdayThrough = true;

#if DEBUG
                if (Settings.debugging && Settings.debuggingRegression)
                    Log.Message($"[ZI] FireSyntheticBirthdayAtYear for {pawn.LabelShort} at {birthdayYear}");
#endif

                MI_BirthdayBiological.Invoke(pawn.ageTracker, new object[] { birthdayYear });
            }
            finally
            {
                AllowBirthdayThrough = false;
                Rand.PopState();
            }
        }
        private void ProcessDeferredBirthdaysOnAgeUp()
        {
            if (deferredBirthdays == null || deferredBirthdays.Count == 0) return;

            int mappedYears = pawn.ageTracker.AgeBiologicalYears;
            // gather due years in ascending order
            var due = deferredBirthdays.Where(y => y <= mappedYears).OrderBy(y => y).ToList();
            foreach (int y in due)
            {
                // fire now; don't defer again
                FireSyntheticBirthdayAtYear(pawn, y, deferIfMappedYounger: false);
                deferredBirthdays.Remove(y);
            }
        }
        public void HandleMothballedAgeAdvance(int interval)
        {
            // 1) Advance baseline by exactly the mothballed interval (trust vanilla here)
            long prevBaseline = baselineBioTicks;
            baselineBioTicks += interval;

            // 2) Compute year crossings on the BASELINE
            int prevY = (lastBaselineWholeYears >= 0)
                ? lastBaselineWholeYears
                : Mathf.FloorToInt(prevBaseline / (float)GenDate.TicksPerYear);

            int curY = Mathf.FloorToInt(baselineBioTicks / (float)GenDate.TicksPerYear);
            if (curY <= prevY || MI_BirthdayBiological == null) { lastBaselineWholeYears = curY; return; }

            // 3) Fire vanilla birthdays for each crossed year (letters + growth moment)
            for (int y = prevY + 1; y <= curY; y++)
            {
                AllowBirthdayThrough = true;
                try
                {
#if DEBUG
                    if (Settings.debugging && Settings.debuggingRegression)
                        Log.Message($"[ZI] HandleMothballedAgeAdvance for {pawn.LabelShort} at {y}");
#endif
                    MI_BirthdayBiological.Invoke(pawn.ageTracker, new object[] { y });
                }
                finally
                {
                    AllowBirthdayThrough = false;
                }
            }

            lastBaselineWholeYears = curY;
        }

        private void OnEnterChild(Pawn p)
        {            
            if (IsHumanlike(p))
            {
                p.story.bodyType = BodyTypeDefOf.Child;
                p.ageTracker.canGainGrowthPoints = true;
                
                lastBeardType = p.style?.beardDef;
                if (lastBeardType != null)
                {
                    p.style.beardDef = BeardDefOf.NoBeard;
                }
                if (pawn.IsCreepJoiner)
                {
                    pawn.creepjoiner.form = (CreepJoinerFormKindDef)CreepJoinerFormKindDef.Named("LoneGenius");
                    pawn.creepjoiner.form.bodyTypeGraphicPaths.Clear();
                }
            }

            Helper_Regression.dropAllUnwearable(p);
        }
        private void OnEnterBaby(Pawn p)
        {      
            if (IsHumanlike(p))
            {
                p.story.bodyType = BodyTypeDefOf.Baby;
                p.ageTracker.canGainGrowthPoints = true;
            }

            Helper_Regression.dropAllUnwearable(p);
        }
        private void OnEnterAdult(Pawn p)
        {
            if (IsHumanlike(p))
            {
                p.style.beardDef = lastBeardType;
                p.ageTracker.canGainGrowthPoints = false;
            }
            Helper_Regression.dropAllUnwearable(p);
        }
        public static (int core, int edge) ChildBandTicks(Pawn p)
        {
            if (IsHumanlike(p))
                return (Mathf.RoundToInt(3f * GenDate.TicksPerYear),
                        Mathf.RoundToInt(13f * GenDate.TicksPerYear));

            // Animals: core = end of Baby, edge = start of Adult
            // If stages missing, fall back to 10%..40% of life expectancy.
            var stages = p?.RaceProps?.lifeStageAges;
            float life = p?.RaceProps?.lifeExpectancy ?? 10f;

            float coreYears = life * 0.10f;     // fallback
            float edgeYears = life * 0.40f;     // fallback

            if (stages != null && stages.Count > 0)
            {
                float? babyEnd = null;
                float? adultStart = null;

                foreach (var lsa in stages)
                {
                    var ds = lsa.def?.developmentalStage;
                    // lsa.minAge = start of this stage
                    if (ds == DevelopmentalStage.Baby) babyEnd = Mathf.Max(babyEnd ?? 0f, lsa.minAge);
                    if (ds == DevelopmentalStage.Adult) adultStart = adultStart ?? lsa.minAge;
                    if (ds == DevelopmentalStage.Child && !IsHumanlike(p)) // some animals use Child/Juvenile
                        adultStart ??= lsa.minAge; // next stage after juvenile will be adult
                }

                if (babyEnd.HasValue) coreYears = Mathf.Clamp(babyEnd.Value, 0f, life);
                if (adultStart.HasValue) edgeYears = Mathf.Clamp(adultStart.Value, coreYears + 0.1f, life);
            }

            return (Mathf.RoundToInt(coreYears * GenDate.TicksPerYear),
                    Mathf.RoundToInt(edgeYears * GenDate.TicksPerYear));
        }
        private static bool BirthdayRollDeterministic(Pawn p, HediffGiver_Birthday agb, int year)
        {
            // Stable seed: pawn + hediff + absolute year index
            int seed = Gen.HashCombineInt(p.thingIDNumber, agb.hediff.shortHash);
            seed = Gen.HashCombineInt(seed, year);

            Rand.PushState(seed);
            try
            {
                float life = p.RaceProps.lifeExpectancy;
                float ageFrac = (year / life);            // vanilla uses fraction of life expectancy
                float chance = agb.ageFractionChanceCurve?.Evaluate(ageFrac) ?? 1f;
                return Rand.Value < chance;
            }
            finally { Rand.PopState(); }
        }
        private int LikelyOnsetYear(Pawn p, HediffGiver_Birthday agb, bool clampToBaseline = true, int? capYearOverride = null)
        {
            float life = p.RaceProps.lifeExpectancy;

            // entry threshold (youngest possible)
            float xMin = (agb.ageFractionChanceCurve?.Points?.Count ?? 0) > 0
                ? agb.ageFractionChanceCurve.Points[0].x
                : 0f;
            int entryYear = Mathf.Max(0, Mathf.CeilToInt(xMin * life));

            // choose upper bound
            int baselineYear = Mathf.Max(entryYear, Mathf.FloorToInt(baselineBioTicks / (float)GenDate.TicksPerYear));
            int lifeYearCap = Mathf.Max(entryYear, Mathf.FloorToInt(life)); // e.g., ~80 for humans

            int upperYear = capYearOverride ?? (clampToBaseline ? baselineYear : lifeYearCap);
            if (upperYear < entryYear) upperYear = entryYear; // degenerate safety

            // Find y maximizing: mass(y) = survival(y-1) * p(y)
            double logSurvival = 0.0;
            double bestLogMass = double.NegativeInfinity;
            int bestYear = entryYear;

            for (int y = entryYear; y <= upperYear; y++)
            {
                float p_y = Mathf.Clamp01(agb.ageFractionChanceCurve?.Evaluate(Mathf.Clamp01(y / life)) ?? 0f);
                if (p_y > 0f && p_y < 1f)
                {
                    double logMass = logSurvival + Math.Log(p_y);
                    if (logMass > bestLogMass)
                    {
                        bestLogMass = logMass;
                        bestYear = y;
                    }
                    logSurvival += Math.Log(1.0 - p_y); // advance survival
                }
                else
                {
                    if (p_y >= 1f) return y; // certainty at this year
                                             // p_y == 0: just keep survival unchanged
                }
            }

            return (bestLogMass == double.NegativeInfinity) ? entryYear : bestYear;
        }
        private void SyncBirthdayHediffs(Pawn p, long desiredBioTicks)
        {
            if (p?.RaceProps?.hediffGiverSets == null) return;

            float lifeExp = p.RaceProps.lifeExpectancy;
            float desiredYears = desiredBioTicks / (float)GenDate.TicksPerYear;
            float desiredFrac = desiredYears / lifeExp;
            int currentYear = Mathf.FloorToInt(desiredYears);

            // hysteresis to only remove “likely age-caused” if at least N years before likely onset
            // Set to 0 to disable.
            const int LikelyOnsetHysteresisYears = 1;

            foreach (var set in p.RaceProps.hediffGiverSets)
            {
                if (set?.hediffGivers == null) continue;

                foreach (var giver in set.hediffGivers)
                {
                    if (giver is not HediffGiver_Birthday agb) continue;

                    var curve = agb.ageFractionChanceCurve;
                    float xMin = (curve?.Points != null && curve.Points.Count > 0) ? curve.Points[0].x : 0f;

                    // Already on pawn?
                    var existing = new List<Hediff>();
                    p.health.hediffSet.GetHediffs(ref existing, h => h.def == agb.hediff);

                    static List<int> PathTo(BodyPartRecord leaf)
                    {
                        var path = new List<int>();
                        var cur = leaf;
                        while (cur?.parent != null)
                        {
                            var list = cur.parent.parts;
                            path.Add(list.IndexOf(cur));
                            cur = cur.parent;
                        }
                        path.Reverse();
                        return path;
                    }

                    // 1) DEFINITE: below entry → impossible now → snapshot + remove
                    if (desiredFrac < xMin)
                    {
                        if (existing.Count > 0)
                        {
                            var mem = GetMemory(p);
                            if (mem != null)
                            {
                                int likelyYear = LikelyOnsetYear(p, agb); // never later than baseline
                                foreach (var h in existing)
                                {
#if DEBUG
                                    if (Settings.debugging && Settings.debuggingRegression)
                                        Log.Message($"[ZI] Definite-remove {h.def.defName} → restore@{likelyYear}");
#endif
                                    mem.removedBirthday.Add(new CompRegressionMemory.StoredHediff
                                    {
                                        defName = h.def.defName,
                                        partPath = h.Part != null ? PathTo(h.Part) : null,
                                        severity = h.Severity,
                                        restoreAtYear = likelyYear
                                    });
                                }
                            }
                            foreach (var h in existing) p.health.RemoveHediff(h);
                        }
                        continue;
                    }

                    // 2) LIKELY: below most-likely onset (with hysteresis) → treat as “not yet happened”
                    if (existing.Count > 0)
                    {
                        int likelyYear = LikelyOnsetYear(p, agb);
                        int cutoffYear = Mathf.Max(0, likelyYear - LikelyOnsetHysteresisYears);

                        if (currentYear < cutoffYear)
                        {
                            var mem = GetMemory(p);
                            if (mem != null)
                            {
                                foreach (var h in existing)
                                {
#if DEBUG
                                    if (Settings.debugging && Settings.debuggingRegression)
                                        Log.Message($"[ZI] Likely-remove {h.def.defName} (now Y{currentYear} < cutoff Y{cutoffYear}) → restore@{likelyYear}");
#endif
                                    mem.removedBirthday.Add(new CompRegressionMemory.StoredHediff
                                    {
                                        defName = h.def.defName,
                                        partPath = h.Part != null ? PathTo(h.Part) : null,
                                        severity = h.Severity,
                                        restoreAtYear = likelyYear
                                    });
                                }
                            }
                            foreach (var h in existing) p.health.RemoveHediff(h);
                            continue;
                        }
                    }

                    // 3) OLD ENOUGH & NONE EXIST: if nothing pending in memory for this hediff and we’re past likely onset, apply now
                    // This will cause the pawn develop a lot of effects that they didn't before. Maybe not as good of an idea now...
                    /*if (existing.Count == 0)
                    {
                        int likelyYear = LikelyOnsetYear(p, agb);

                        bool pendingInMemory = false;
                        var mem = GetMemory(p);
                        if (mem != null && mem.removedBirthday != null)
                            pendingInMemory = mem.removedBirthday.Any(s => s.defName == agb.hediff.defName);

                        if (!pendingInMemory && currentYear >= likelyYear)
                        {
                            // Vanilla apply (handles immunity/part selection/severity)
                            agb.TryApply(p);
#if DEBUG
                            if (Settings.debugging && Settings.debuggingRegression)
                                Log.Message($"[ZI] Applied {agb.hediff.defName} at Y{currentYear} (likely {likelyYear})");
#endif
                        }
                    }*/
                }
            }

            // Let remembered items come back at their stored restoreAtYear
            TryRestoreBirthdayFromMemory(p, currentYear);
        }

        private void TryRestoreBirthdayFromMemory(Pawn p, int currentYear)
        {
            var mem = GetMemory(p);
            if (mem == null || mem.removedBirthday.Count == 0) return;

#if DEBUG
            if (Settings.debugging && Settings.debuggingRegression)
                Log.Message($"[ZI]Try restoring for {mem.removedBirthday.Count} at {currentYear}");
#endif

            var tmpHediffsGained = new List<HediffDef>();
            for (int i = mem.removedBirthday.Count - 1; i >= 0; i--)
            {
                var s = mem.removedBirthday[i];
                if (currentYear < s.restoreAtYear) continue;

                var def = DefDatabase<HediffDef>.GetNamedSilentFail(s.defName);
                if (def == null) { mem.removedBirthday.RemoveAt(i); continue; }

                var part = s.partPath != null ? new CompRegressionMemory.StoredHediff { partPath = s.partPath }.ResolvePart(p) : null;
                if (part != null && p.health.hediffSet.PartIsMissing(part)) continue; // wait until part restored

                // don’t duplicate
                if (p.health.hediffSet.hediffs.Any(h => h.def == def && h.Part == part)) { mem.removedBirthday.RemoveAt(i); continue; }

#if DEBUG
                if (Settings.debugging && Settings.debuggingRegression)
                    Log.Message($"[ZI]Restore {def.defName} on {part.LabelShort} at {s.severity}!");
#endif

                var h2 = HediffMaker.MakeHediff(def, p, part);
                h2.Severity = Mathf.Clamp(s.severity, 0f, def.maxSeverity > 0 ? def.maxSeverity : 1f);
                p.health.AddHediff(h2);
                mem.removedBirthday.RemoveAt(i);
                tmpHediffsGained.Add(def);
            }

            
            if (tmpHediffsGained.Count > 0)
            {
                TaggedString taggedString = "LetterBirthdayBiological".Translate(this.pawn, currentYear);
                taggedString += "\n\n" + "BirthdayBiologicalAgeInjuries".Translate(this.pawn);
                taggedString += ":\n\n" + (from h in tmpHediffsGained
                                           select h.LabelCap.Resolve()).ToLineList("  - ", false);
                LetterDef letterDef = LetterDefOf.NegativeEvent;
                Find.LetterStack.ReceiveLetter("LetterLabelBirthday".Translate(), taggedString, letterDef, pawn);
            }
        }
        private void refreshAgeStageCache(Pawn pawn)
        {
            Helper_Regression.getAgeStage(pawn, true);
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            pawn.Notify_DisabledWorkTypesChanged();
            pawn.workSettings?.EnableAndInitialize();
            pawn.skills?.Notify_SkillDisablesChanged();
            Find.ColonistBar?.MarkColonistsDirty();
        }
        bool debugging => (Settings.debugging && Settings.debuggingRegression);
        public void TickRare()
        {

            if (Severity == def.maxSeverity)
            {
#if DEBUG
                if (debugging) Log.Message($"[ZI]Start Resurrection Timer {Severity} == {def.maxSeverity}");
#endif
                TaggedString taggedString = "LetterResurrectStart".Translate(pawn.Named("PAWN"));
                Find.LetterStack.ReceiveLetter("LetterLabelResurrectStart".Translate(), taggedString, LetterDefOf.NeutralEvent, pawn);
                resurrectTimer.Start(GenTicks.TicksGame, ResurrectDurationSeconds.SecondsToTicks(), new Action(Resurrect));
                resurrectTimerLetter.Start(GenTicks.TicksGame, ResurrectMoveDurationSeconds.SecondsToTicks(), new Action(ResurrectLetterTrigger));
                Severity -= 0.01f;
            }
            if (!resurrectTimer.Finished)
            {
#if DEBUG
                if (debugging) Log.Message($"[ZI]Ticking Resurrection");
#endif
                resurrectTimer.TickIntervalDelta();
                if (!resurrectTimerLetter.Finished) resurrectTimerLetter.TickIntervalDelta();
            }
        }
        private void ResurrectLetterTrigger()
        {
            TaggedString taggedString = "LetterResurrectMove".Translate(pawn.Named("PAWN"));
            Find.LetterStack.ReceiveLetter("LetterLabelResurrectMove".Translate(), taggedString, LetterDefOf.NeutralEvent, pawn);
        }
        
        private void Resurrect()
        {
            Messages.Message("MessagePawnResurrected".Translate(pawn), pawn, MessageTypeDefOf.PositiveEvent);
            Find.LetterStack.ReceiveLetter("MessagePawnResurrected".Translate(pawn), "MessagePawnResurrected".Translate(pawn), LetterDefOf.PositiveEvent, pawn);
            this.pawn.Drawer.renderer.SetAnimation(null);
            ResurrectionUtility.TryResurrect(this.pawn, new ResurrectionParams
            {
                gettingScarsChance = 0.1f,
                canKidnap = false,
                canTimeoutOrFlee = false,
                useAvoidGridSmart = true,
                canSteal = false,
                invisibleStun = true,
                removeDiedThoughts = true
            });
            Severity = def.maxSeverity;
        }
        public override void Tick()
        {
            base.Tick();
            if (pawn == null || !pawn.Spawned) return;

            if (!forceTick && !this.pawn.IsHashIntervalTick(HealingTickInterval)) return;

            //if(Severity > 0f && dormant)  dormant = false;
            //if (dormant) return;

            HealStep(HealingTickInterval);
            ApplyAgeMapping(force: forceTick);
            forceTick = false;

            //if (Severity <= 0f && !dormant) dormant = true;
        }

        private void HealStep(int intervalTicks)
        {
            if (Severity <= 0f) return;

            float perDay = Mathf.Max(0f, Settings.Regression_BaseRecoveryPerDay);
            if (perDay <= 0f) return;

            float mult = 1f;

            if (Settings.Regression_RestingMultiplier > 0f && pawn.InBed())
                mult *= Settings.Regression_RestingMultiplier;

            if (Settings.Regression_ChildMultiplier > 0f && !pawn.ageTracker.Adult)
                mult *= Settings.Regression_ChildMultiplier;

            // If you want tending to help; Hediff has IsTended() in recent RimWorld versions.
            if (Settings.Regression_TendedMultiplier > 0f && this.IsTended())
                mult *= Settings.Regression_TendedMultiplier;

            float delta = perDay * mult * (intervalTicks / (float)GenDate.TicksPerDay);

            float before = Severity;
            Severity = Mathf.Max(0f, Severity - delta);
            _lastHealTick = Find.TickManager.TicksGame;

#if DEBUG
            if (Settings.debugging && Settings.debuggingRegression && !Mathf.Approximately(before, Severity))
                Log.Message($"[ZI] Regression heal {pawn.LabelShort} {before:0.###} -> {Severity:0.###} (Δ={delta:0.###})");
#endif
        }

        public static float NormalizedSeverityOn(Pawn p)
        {
            if (p?.health == null) return 0f;
            var h = p.health.hediffSet?.hediffs?.FirstOrDefault(x => x is Hediff_RegressionDamage);
            if (h == null) return 0f;

            float max = h.def.maxSeverity > 0f ? h.def.maxSeverity : 1f;
            return Mathf.Clamp01(h.Severity / max);
        }

        public static float LevelsModifierToMask(Pawn p)
        {
            if (p == null) return 0;
            float sev = NormalizedSeverityOn(p);
            return (float)Settings.Regression_LevelMaskBySeverity * sev;

            // Tuned entirely by your settings:
            //float levels = sev * Mathf.Max(0f, (float)Settings.Regression_LevelsPerSeverity);
            //return levels > 0 ? levels : 0;
        }

        public override string TipStringExtra
        {
            get
            {
                string tip = base.TipStringExtra;
                float sevPct = Severity * 100f;
                tip += (tip.NullOrEmpty() ? "" : "\n") + $"Regression: {sevPct:F1}%";

       
                if (Severity > 0f)
                {
                    tip += $"\nBase Age: {BaseAgeYearFloat:F1} years";
                    tip += $"\nAge Stage: {Helper_Regression.getAgeStage(pawn)} years";
                    if(Memory != null && Memory.desiredAgeYears != -1)
                    {
                        tip += $"\nDesired Age: {Memory.desiredAgeYears}";
                    }
                    
                    var reducePercent = Mathf.CeilToInt(Hediff_RegressionDamage.LevelsModifierToMask(pawn) * 100f);
                    tip += $"\nSkilles reduced: {reducePercent}%";
                    // Neutral-condition ETA: assumes multiplier ~1
                    tip += $"\nEstimated recovery: ~{EstimatedRecoveryDays():0.#} day(s)";
                }
                return tip;
            }
        }
        public override string LabelBase
        {
            get
            {
                // "Regression (0.62, 2.3 days)"
                string baseLabel = "Regression";
                string pct = Severity.ToStringPercent();
                string remaining = $"{EstimatedRecoveryDays():0.#} days";
                return $"{baseLabel} ({pct}, {remaining})";
            }
        }
        private float EstimatedRecoveryDays()
        {
            float perDay = Settings.Regression_BaseRecoveryPerDay;
            if (perDay <= 0f) return 0f;
            return Severity / perDay;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos()) yield return g;

            var mem = Memory;
            string label = (mem != null && mem.desiredAgeYears >= 0)
    ? $"Desired age: {mem.desiredAgeYears}"
    : "Set desired age...";
            yield return new Command_Action
            {
                defaultLabel = label,
                defaultDesc = "Choose a target biological age range to maintain.",
                icon = ContentFinder<Texture2D>.Get("Things/Building/Misc/FountainOfYouth/FountainOfYouth_A"),
                action = () => Find.WindowStack.Add(new Dialog_DesiredAge(pawn, mem))
            };
            if (!Prefs.DevMode) yield break;

            yield return new Command_Action
            {
                defaultLabel = "DBG: Apply regression (damage)…",
                defaultDesc = "Applies regression via debug DamageInfo.",
                action = () => Find.WindowStack.Add(new Dialog_Slider("Damage amount", 0, 100, v => ZI_DebugRegression.ApplyByDamage(pawn, v)))
            };

            yield return new Command_Action
            {
                defaultLabel = "DBG: Set regression (severity)…",
                defaultDesc = "Directly sets the regression hediff severity.",
                action = () => Find.WindowStack.Add(new Dialog_Slider("Severity (0–100%)", 0, 100, v => ZI_DebugRegression.ApplyBySeverity(pawn, v / 100f)))
            };
        }

    }

    public class Dialog_DesiredAge : Window
    {
        private readonly Pawn pawn;
        private readonly CompRegressionMemory mem;

        private int desiredY;
        private readonly int minAllowed = 0;
        private readonly int maxAllowed;

        public override Vector2 InitialSize => new Vector2(460f, 180f);
        public override bool IsDebug => false;

        public Dialog_DesiredAge(Pawn pawn, CompRegressionMemory mem)
        {
            this.pawn = pawn;
            this.mem = mem;

            doWindowBackground = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            forcePause = false;

            // Allow up to ~2× life expectancy (or tweak as you like)
            int life2 = Mathf.Max(10, Mathf.RoundToInt(pawn.RaceProps.lifeExpectancy) * 2);
            maxAllowed = life2;

            // seed from memory or sensible default (current age)
            int cur = pawn.ageTracker.AgeBiologicalYears;
            desiredY = (mem != null && mem.desiredAgeYears >= 0)
                ? Mathf.Clamp(mem.desiredAgeYears, minAllowed, maxAllowed)
                : Mathf.Clamp(cur, minAllowed, maxAllowed);
        }

        public override void DoWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);

            list.Label($"Pawn: {pawn.LabelShortCap}   Current age: {pawn.ageTracker.AgeBiologicalYears}  (range {minAllowed}–{maxAllowed})");
            list.GapLine();
            list.Gap(6f);

            // Single desired-age slider
            Rect r = list.GetRect(24f);
            int newVal = Mathf.RoundToInt(Widgets.HorizontalSlider(
                r, desiredY, minAllowed, maxAllowed, middleAlignment: true,
                label: $"Desired age: {desiredY}", roundTo: 1f));
            if (newVal != desiredY) desiredY = newVal;

            list.Gap(10f);

            // Buttons row
            Rect row = list.GetRect(30f);
            float bw = (row.width - 8f) / 3f;

            if (Widgets.ButtonText(new Rect(row.x, row.y, bw, row.height), "Use current"))
            {
                int cur = pawn.ageTracker.AgeBiologicalYears;
                desiredY = Mathf.Clamp(cur, minAllowed, maxAllowed);
            }

            if (Widgets.ButtonText(new Rect(row.x + bw + 4f, row.y, bw, row.height), "Clear"))
            {
                if (mem != null) mem.desiredAgeYears = -1; // disable auto-dosing
                Close();
            }

            if (Widgets.ButtonText(new Rect(row.x + (bw + 4f) * 2f, row.y, bw, row.height), "OK"))
            {
                if (mem != null) mem.desiredAgeYears = desiredY;
                Close();
            }

            list.End();
        }
    }


    // Gameplay path (also covers Level.get because it calls GetLevel(true))
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.GetLevel))]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_GetLevel_MaskByRegression
    {
        public static void Postfix(SkillRecord __instance, ref int __result, bool includeAptitudes)
        {
            var pawn = __instance?.Pawn;
            if (pawn == null) return;

            float mask = Hediff_RegressionDamage.LevelsModifierToMask(pawn);
            if (mask <= 0f) return;

            __result = Mathf.Clamp(__result - Mathf.CeilToInt(((float)__result) * mask), 0, 20);
        }
    }

    // UI path (what the skills card shows)
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.GetLevelForUI))]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_GetLevelForUI_MaskByRegression
    {
        public static void Postfix(SkillRecord __instance, ref int __result, bool includeAptitudes)
        {
            var pawn = __instance?.Pawn;
            if (pawn == null) return;

            float mask = Hediff_RegressionDamage.LevelsModifierToMask(pawn);
            if (mask <= 0f) return;

            __result = Mathf.Clamp(__result - Mathf.CeilToInt(((float)__result) * mask), 0, 20);
        }
    }

    // just to be sure, gate growth moments too
    [HarmonyPatch(typeof(Pawn_AgeTracker), "TryChildGrowthMoment")]
    static class Patch_TryChildGrowthMoment
    {
        public static bool Prefix(Pawn_AgeTracker __instance)
        {
            var pawn = AccessTools.FieldRefAccess<Pawn_AgeTracker, Pawn>("pawn")(__instance);
            var reg = pawn?.health?.hediffSet?.hediffs?.OfType<Hediff_RegressionDamage>()?.FirstOrDefault();
            return reg == null || !reg.suppressVanillaBirthdays || Hediff_RegressionDamage.AllowBirthdayThrough;
        }
    }
    // block vanilla birthdays unless explicitly allowed
    [HarmonyPatch(typeof(Pawn_AgeTracker), "BirthdayBiological")]
    static class Patch_SuppressBirthdayBiological
    {
        public static bool Prefix(Pawn_AgeTracker __instance)
        {
            var pawn = AccessTools.FieldRefAccess<Pawn_AgeTracker, Pawn>("pawn")(__instance);
            var reg = pawn?.health?.hediffSet?.hediffs?.OfType<Hediff_RegressionDamage>()?.FirstOrDefault();
            return reg == null || !reg.suppressVanillaBirthdays || Hediff_RegressionDamage.AllowBirthdayThrough;
        }
    }

    // when mothballed age advances, move baseline and fire synthetic birthdays.
    [HarmonyPatch(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.AgeTickMothballed))]
    static class Patch_AgeTickMothballed_Postfix
    {
        public static void Postfix(Pawn_AgeTracker __instance, int interval)
        {
            var pawn = AccessTools.FieldRefAccess<Pawn_AgeTracker, Pawn>("pawn")(__instance);
            var reg = pawn?.health?.hediffSet?.hediffs?.OfType<Hediff_RegressionDamage>()?.FirstOrDefault();
            if (reg == null) return;

            reg.HandleMothballedAgeAdvance(interval);
        }
    }
    public class CompRegressionMemory : ThingComp
    {
        public struct StoredHediff : IExposable
        {
            public string defName;
            public List<int> partPath;
            public float severity;
            public int restoreAtYear; // absolute biological year to re-apply at

            public BodyPartRecord ResolvePart(Pawn p)
            {
                var r = p.RaceProps.body.corePart;
                if (partPath != null)
                    foreach (var idx in partPath) r = r?.parts?.ElementAtOrDefault(idx);
                return r;
            }
            public void ExposeData()
            {
                Scribe_Values.Look(ref defName, "defName");
                Scribe_Values.Look(ref severity, "severity", 1f);
                Scribe_Values.Look(ref restoreAtYear, "restoreAtYear", 0);
                Scribe_Collections.Look(ref partPath, "partPath", LookMode.Value);
            }
        }

        public List<StoredHediff> removedBirthday = new();
        
        public int desiredAgeYears = -1;
        public int desiredAgeDoseTick = -1;
        public int pendingPassionSwaps = 0;
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref removedBirthday, "ZI_removedBirthday", LookMode.Deep);
            if (removedBirthday == null) removedBirthday = new List<StoredHediff>();
            Scribe_Values.Look(ref desiredAgeYears, "desiredAgeYears", -1);
            Scribe_Values.Look(ref desiredAgeDoseTick, "desiredAgeDoseTick", -1);
            Scribe_Values.Look(ref pendingPassionSwaps, "ZI_pendingPassionSwaps", 0);

        }
    }
    public class CompProperties_RegressionMemory : CompProperties
    {
        public CompProperties_RegressionMemory()
        {
            this.compClass = typeof(CompRegressionMemory);
        }
    }
    [StaticConstructorOnStartup]
    public static class ZI_InjectRegressionMemoryComp
    {
        static ZI_InjectRegressionMemoryComp()
        {
            foreach (var def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def?.race == null) continue;
                if (!def.race.Humanlike) continue;

                if (def.comps == null) def.comps = new List<CompProperties>();
                bool exists = def.comps.Any(c => c?.compClass == typeof(CompRegressionMemory));
                if (!exists)
                    def.comps.Add(new CompProperties_RegressionMemory());
            }
        }
    }

    [HarmonyPatch(typeof(Corpse), nameof(Corpse.TickRare))]
    public static class Patch_Corpse_TickRare
    {
        public static void Postfix(Corpse __instance)
        {
            var pawn = __instance?.InnerPawn;
            var hediffs = pawn?.health?.hediffSet?.hediffs;
            if (hediffs == null) return;

            if (__instance.GetRotStage() == RotStage.Dessicated) return;

            Hediff_RegressionDamage firstHediff = pawn.health.hediffSet.GetFirstHediff<Hediff_RegressionDamage>();
            if (firstHediff != null)
            {
                firstHediff.TickRare();
            }
        }
    }
}

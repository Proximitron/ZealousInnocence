using DubsBadHygiene;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;
using static UnityEngine.GridBrushBase;
using static ZealousInnocence.DebugActions;
using static ZealousInnocence.Hediff_PhysicalRegression;

namespace ZealousInnocence
{

    public abstract class Hediff_RegressionBase : HediffWithComps
    {
        protected static ZealousInnocenceSettings Settings => LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();

        public bool forceTick = false;
        protected static bool IsHumanlike(Pawn p) => p?.RaceProps?.Humanlike == true;
        protected static bool IsAnimal(Pawn p) => p?.RaceProps?.Animal == true;
        public bool PlayerControlled
        {
            get
            {
                return this.pawn.IsColonist && (this.pawn.HostFaction == null || this.pawn.IsSlave);
            }
        }
        public Hediff_PhysicalRegression PhysicalRegressionHediff
        {
            get
            {
                return HediffByPawn(pawn);
            }
        }

        public abstract long BaseAgeBioTicks { get; }

        public float BaseAgeYearFloat
        {
            get
            {
                return BaseAgeBioTicks / GenDate.TicksPerYear;
            }
        }
        public int BaseAgeYearInt
        {
            get
            {
                return Mathf.FloorToInt(BaseAgeYearFloat);
            }
        }
        public float AgeYearFloat
        {
            get
            {
                return (float)pawn.ageTracker.AgeBiologicalTicks / GenDate.TicksPerYear;
            }
        }
        public long MentalTicksForSeverity(float Severity)
        {
            return Math.Max(0, BioTicksForSeverity(Severity, pawn, BaseAgeBioTicks));
        }
        public float MentalTicks
        {
            get
            {
                return MentalTicksForSeverity(Severity);
            }
        }
        public float MentalTicksYearFloat
        {
            get
            {
                return MentalTicks / GenDate.TicksPerYear;
            }
        }
        public int MentalTicksYearInt
        {
            get
            {
                return Mathf.FloorToInt(MentalTicks / GenDate.TicksPerYear);
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
        private float EstimatedRecoveryDays()
        {
            float perDay = Settings.Regression_BaseRecoveryPerDay / 100f;
            if (perDay <= 0f) return 0f;
            return Severity / perDay;
        }
        public override void PreRemoved()
        {
            base.PreRemoved();
            forceTick = true;
            Severity = 0f;
            Tick();
        }
        const float minSplit = 0.40f; // old pawns: child band starts at ~40%
        const float maxSplit = 0.75f; // young pawns: child band starts at ~75%
        const float youngRef = 0.3f;
        const float oldRef = 0.9f;
        const float babyExtraMax = 0.30f;  // 30% overdrive beyond 1.0 at birth
        const double babyExp = 0.75;       // curve shape (higher = gentler)

        // Convert a target age (years) to the severity s that would produce it.
        public float SeverityForTargetYears(float targetYears)
        {
            return SeverityForTargetYears(targetYears, pawn, BaseAgeBioTicks);
        }
        public static float SeverityForTargetYears(float targetYears, Pawn pawn, long baselineBioTicks)
        {
            long targetTicks = Mathf.RoundToInt(targetYears * GenDate.TicksPerYear);
            return SeverityForTargetTicks(targetTicks, pawn, baselineBioTicks);
        }
        public static float SeverityForTargetTicks(long targetTicks, Pawn pawn, long baselineBioTicks)
        {
            float life = pawn.RaceProps.lifeExpectancy;
            float baseYears = baselineBioTicks / (float)GenDate.TicksPerYear;
            float f = Mathf.InverseLerp(youngRef * life, oldRef * life, baseYears);  // young→0, old→1
            float split = Mathf.Lerp(minSplit, maxSplit, f);                          // e.g., 0.40..0.75

            var band = ChildBands.Get(pawn);
            var (childCore, childEdge) = (band.core, band.edge);
            const long babyFloor = 0;

            // Allow full range: [babyFloor .. max(baseline, childCore)]
            long lo = Math.Min(baselineBioTicks, (long)babyFloor);
            long hi = Math.Max(Math.Max((long)baselineBioTicks, (long)babyFloor), (long)childCore);
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
            return BioTicksForSeverity(s, pawn, BaseAgeBioTicks);
        }
        public static long BioTicksForSeverity(float s, Pawn pawn, long baselineBioTicks)
        {
            var band = ChildBands.Get(pawn);
            var (childCore, childEdge) = (band.core, band.edge);

            float life = pawn.RaceProps.lifeExpectancy;
            float baseYears = baselineBioTicks / (float)GenDate.TicksPerYear;
            float f = Mathf.InverseLerp(youngRef * life, oldRef * life, baseYears);
            float split = Mathf.Lerp(minSplit, maxSplit, f); // e.g. 0.40..0.75

            const double eps = 1e-9;
            long calculatedResult = 0;
            if (s < split)
            {
                // Adult zone: baseline → childEdge with exponent 1.25
                double t = s / Math.Max(split, eps);   // 0..1
                t = Math.Pow(t, 1.25);
                double d = baselineBioTicks + (childEdge - (double)baselineBioTicks) * t;
                long desired = (long)Math.Round(d);
                calculatedResult = Math.Max(0L, Math.Min(desired, Math.Max(baselineBioTicks, (long)childEdge)));
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
                calculatedResult = Math.Max(lo, Math.Min(desired, hi));
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
                calculatedResult = Math.Max(0L, Math.Min(desired, (long)childCore));
            }
            return Math.Min(calculatedResult, baselineBioTicks);
        }
        public static class ChildBands
        {
            private static readonly MethodInfo MinAge = AccessTools.Method("Toddlers.HARFunctions:HARToddlerMinAge");

            private static readonly MethodInfo EndAge = AccessTools.Method("Toddlers.HARFunctions:HARToddlerEndAge");
            public static float? GetMinAge(Pawn p)
            {
                if (MinAge == null) return null;
                return (float)MinAge.Invoke(null, new object[] { p });
            }

            public static float? GetEndAge(Pawn p)
            {
                if (EndAge == null) return null;
                return (float)EndAge.Invoke(null, new object[] { p });
            }

            // Small, auto-cleaning cache keyed by Pawn (no leaks).
            private static readonly ConditionalWeakTable<Pawn, DevelopmentStages> _cache = new();
            private const int TTL = GenDate.TicksPerYear; // recompute after a year

            public sealed class DevelopmentStages
            {
                public float toddler, core, edge;
                public int stampTick;
                public long ageBioTicks; // dependency
                public bool valid;
            }

            public static DevelopmentStages Get(Pawn pawn)
            {
                if (pawn == null) return null;

                int now = Find.TickManager.TicksGame;

                if (!_cache.TryGetValue(pawn, out var entry))
                {
                    entry = new DevelopmentStages();
                    _cache.Add(pawn, entry);
                }

                // If entry expired, recompute it
                if (!entry.valid || now - entry.stampTick >= TTL)
                {
                    Recompute(entry, pawn);
                    entry.stampTick = now;
                    entry.valid = true;
                }

                return entry;
            }

            public static void Clear() => _cache.Clear();

            private static void Recompute(DevelopmentStages e, Pawn p)
            {

                if (IsHumanlike(p))
                {
                    if (HARLoaded && Helper_Toddlers.ToddlersLoaded)
                    {
                        var toddlerEndAgeSource = GetEndAge(p);
                        var toddlerMinAgeSource = GetMinAge(p);
                        if (toddlerEndAgeSource.HasValue && toddlerMinAgeSource.HasValue)
                        {
                            float toddlerEndAge = toddlerEndAgeSource.Value;
                            float toddlerMinAge = toddlerMinAgeSource.Value;
                            float? adultStart = p.def.race.lifeStageAges.Where(s => s.def?.developmentalStage == DevelopmentalStage.Adult).Select(s => s.minAge).FirstOrDefault();

                            if (adultStart.HasValue && adultStart.Value > 1f)
                            {
                                if(toddlerMinAge > toddlerEndAge){
                                    var cache = toddlerMinAge;
                                    toddlerMinAge = toddlerEndAge;
                                    toddlerEndAge = cache;
                                }

                                bool toddlerInvalid =
                                 toddlerMinAge <= 0f ||
                                 toddlerEndAge <= 0f ||
                                 Math.Abs(toddlerMinAge - toddlerEndAge) < 0.0001f ||
                                 toddlerEndAge >= adultStart;

                                if (toddlerInvalid)
                                {
                                    var toddlerMinAgeNew = adultStart.Value / 4f;
                                    var toddlerEndAgeNew = adultStart.Value / 2f;
#if DEBUG
                                    if (Settings.debugging)
                                    {
                                        Log.Warning($"[ZI] Given toddler ages {toddlerMinAge:F1}-{toddlerEndAge:F1} to adultStart {adultStart.Value} was invalid. FIX -> " +
                                            $"{toddlerMinAgeNew:F1}-{toddlerEndAgeNew:F1}");
                                    }
#endif
                                    toddlerMinAge = toddlerMinAgeNew;
                                    toddlerEndAge = toddlerEndAgeNew;
                                }

                                e.toddler = toddlerMinAge * GenDate.TicksPerYear;
                                e.core = toddlerEndAge * GenDate.TicksPerYear;
                                e.edge = adultStart.Value * GenDate.TicksPerYear;

                                return;
                            }
                        }

                    }
                    e.toddler = 1f * GenDate.TicksPerYear;
                    e.core = 3f * GenDate.TicksPerYear;
                    e.edge = 13f * GenDate.TicksPerYear;
                    return;
                }


                var stages = p?.RaceProps?.lifeStageAges;
                float life = p?.RaceProps?.lifeExpectancy ?? 10f;

                // Defaults if defs are bizarre/missing
                float coreYears = Mathf.Clamp(life * 0.10f, 0f, life);
                float edgeYears = Mathf.Clamp(life * 0.40f, coreYears + 0.01f, life);

                if (stages != null && stages.Count > 0)
                {
                    // Order by minAge (already true in vanilla, but be safe)
                    var ordered = stages.OrderBy(s => s.minAge).ToList();

                    // 1) Find earliest minAge (usually 0)
                    float firstMin = ordered[0].minAge;

                    // 2) childCore = first DISTINCT minAge > firstMin
                    float? nextDistinct = null;
                    for (int i = 1; i < ordered.Count; i++)
                    {
                        if (ordered[i].minAge > firstMin + 1e-6f)
                        {
                            nextDistinct = ordered[i].minAge;
                            break;
                        }
                    }

                    // If there was no second distinct breakpoint, synthesize a tiny baby window
                    if (!nextDistinct.HasValue)
                    {
                        // 6–10 days is a nice universal “calf” window for fast growers
                        float synthDays = 8f;
                        nextDistinct = firstMin + Mathf.Max(synthDays / 60f, life * 0.01f);
                    }

                    coreYears = Mathf.Clamp(nextDistinct.Value, 0f, life);

                    // 3) Prefer the explicit Adult start if it is AFTER childCore
                    float? adultStart = ordered
                        .Where(s => s.def?.developmentalStage == DevelopmentalStage.Adult)
                        .Select(s => s.minAge)
                        .Where(a => a > coreYears + 1e-6f)
                        .Cast<float?>()
                        .FirstOrDefault();

                    if (adultStart.HasValue)
                    {
                        edgeYears = Mathf.Clamp(adultStart.Value, coreYears + 0.01f, life);
                    }
                    else
                    {
                        // Otherwise take the next DISTINCT minAge after childCore
                        float? nextAfterCore = ordered
                            .Select(s => s.minAge)
                            .FirstOrDefault(a => a > coreYears + 1e-6f);

                        if (nextAfterCore.HasValue)
                            edgeYears = Mathf.Clamp(nextAfterCore.Value, coreYears + 0.01f, life);
                        else
                            edgeYears = Mathf.Clamp(coreYears + Mathf.Max(0.05f, life * 0.10f), coreYears + 0.01f, life);
                    }
                }

                float coreTicks = coreYears * GenDate.TicksPerYear;
                float edgeTicks = edgeYears * GenDate.TicksPerYear;

#if DEBUG
                if (Settings.debugging && Settings.debuggingRegression)
                    Log.Message($"[ZI] {p?.LabelShortCap ?? "<null>"} bands -> " +
               $"core(end Baby)={coreYears:F6}y ({coreTicks / (float)GenDate.TicksPerDay:F2} d)  " +
               $"edge(start Adult)={edgeYears:F6}y  life={life:F2}y");
#endif
                e.toddler = 1f * GenDate.TicksPerYear;
                e.core = coreTicks;
                e.edge = edgeTicks;
                return;
            }
        }
        public override void Tick()
        {
            base.Tick();
            /*if (this.def == HediffDefOf.RegressionDamage || this.def == HediffDefOf.RegressionDamageMental)
            {
                Severity = 0;
                return;
            }*/
        }

        /*public string TipExtraDetails(bool mental) {
            string tip = "";
            var info = pawn.ComputeTotalRegression(mental: mental);
            tip += $"\n\nTotal = {info.finalTotal:0.###}  (Σ {info.sumContrib:0.###} × {info.productMult:0.###})";
            if (!float.IsInfinity(info.postEffectCap))
                tip += $"  |  Cap ≤ {info.postEffectCap:0.###}";

            var sources = pawn.health.hediffSet.hediffs
                .OfType<HediffWithComps>()
                .Select(h => (h, c: h.TryGetComp<HediffComp_RegressionInfluence>()))
                .Where(t => t.c != null && t.c.IsInfluence(mental: mental))
                .Select(t => new
                {
                    label = t.h.def.label.CapitalizeFirst(),
                    contrib = t.c.GetExternalCappedContribution(mental: mental),
                    mult = t.c.GetMultiplierFactor(mental: mental),
                    cap = t.c.GetCap(mental: mental),
                    decPerDay = t.c.decayPerDay,
                    severity = t.c.parent.Severity,
                })
                .Where(x => x.contrib > 0f || Mathf.Abs(x.mult - 1f) > 0.001f || x.cap > 0f)
                .OrderByDescending(x => x.contrib)
                .ThenByDescending(x => x.mult)
                .ToList();

            if (sources.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("\nSources:");
                foreach (var s in sources)
                {
                    sb.Append("  • ").Append(s.label).Append("  -> ");
                    if (s.contrib > 0f) sb.Append($"+{s.contrib:0.###}  ");
                    if (Mathf.Abs(s.mult - 1f) > 0.001f) sb.Append($"×{s.mult:0.###}  ");
                    if (s.cap > 0f) sb.Append($"(cap ≤ {s.cap:0.###})  ");
                    if (s.decPerDay > 0f)
                    {
                        var remaining = s.severity / s.decPerDay;
                        sb.Append($"(≈ {remaining:0.#} day");
                        if (remaining >= 2f) sb.Append("s");
                        sb.Append(" left)  ");
                    }
                    sb.AppendLine();
                }
                tip += sb.ToString().TrimEnd();
            }
            return tip;
        }

    }*/
        public string TipExtraDetails(bool mental)
        {
            var info = pawn.ComputeTotalRegression(mental: mental);

            var tipSb = new StringBuilder();
            tipSb.Append($"\n\nTotal = {info.finalTotal:0.###}  (Σ {info.sumContrib:0.###} × {info.productMult:0.###})");
            if (!float.IsInfinity(info.postEffectCap))
                tipSb.Append($"  |  Cap ≤ {info.postEffectCap:0.###}");

            // collect raw entries
            var entries = pawn.health.hediffSet.hediffs
    .OfType<HediffWithComps>()
    .Select(h => (h, c: h.TryGetComp<HediffComp_RegressionInfluence>()))
    .Where(t => t.c != null && t.c.IsInfluence(mental: mental))
    .Select(t =>
    {
        var props = t.c.props; // per-def, same across instances of this HediffDef
        float contrib = t.c.GetExternalCappedContribution(mental: mental);
        float mult = t.c.GetMultiplierFactor(mental: mental);
        float capDef = t.c.GetCap(mental);
        float decPerDay = t.c.decayPerDay;
        float sev = t.c.parent.Severity;

        // remaining time (in days)
        float remaining = decPerDay > 0f ? sev / decPerDay : float.PositiveInfinity;

        return new
        {
            def = t.h.def,
            label = t.h.def.label.CapitalizeFirst(),
            contrib,
            mult,
            capDef,           // << per-def cap
            decPerDay,
            severity = sev,
            remaining
        };
    })
    .Where(x => x.contrib > 0f || Mathf.Abs(x.mult - 1f) > 0.001f || x.capDef > 0f)
    .ToList();

            if (entries.Count == 0)
                return tipSb.ToString();

            // group by HediffDef (one row per hediff type)
            var groups = entries
                .GroupBy(e => e.def)
                .Select(g =>
                {
                    float sumContrib = g.Sum(e => e.contrib);

                    float prodMult = 1f;
                    foreach (var e in g) prodMult *= e.mult;

                    // per-def cap is identical across instances; pick any (or max for safety)
                    float capDef = g.Max(e => e.capDef);

                    // summarize remaining time as a bucket range
                    float minRem = g.Min(e => e.remaining);
                    float maxRem = g.Max(e => e.remaining);
                    string bucket;
                    if (float.IsPositiveInfinity(minRem) && float.IsPositiveInfinity(maxRem))
                    {
                        bucket = "no decay";
                    }
                    else if (minRem == maxRem)
                    {
                        bucket = Helper_Regression.FormatDays(minRem);
                    }
                    else
                    {
                        string minStr = Helper_Regression.FormatDays(minRem);
                        string maxStr = Helper_Regression.FormatDays(maxRem);
                        bucket = $"{minStr} – {maxStr}";
                    }

                    // optional: show the post-cap contribution per group (display only)
                    float cappedContrib = (capDef > 0f) ? Mathf.Min(sumContrib, capDef) : sumContrib;

                    return new
                    {
                        typeLabel = g.First().label,
                        count = g.Count(),
                        sumContrib,
                        cappedContrib,
                        prodMult,
                        capDef,
                        bucket
                    };
                })
                .OrderByDescending(gr => gr.sumContrib)
                .ThenByDescending(gr => gr.prodMult)
                .ThenBy(gr => gr.typeLabel)
                .ToList();

            // render
            var sb = new StringBuilder();
            sb.AppendLine("\nSources :");
            foreach (var g in groups)
            {
                sb.Append("  • ").Append(g.typeLabel)
                  .Append(" (x").Append(g.count).Append(", ").Append(g.bucket).Append(")  -> ");

                // contribution (show capped if a cap exists)
                if (g.capDef > 0f)
                    sb.Append($"+{g.cappedContrib:0.###}  ");
                else if (g.sumContrib > 0f)
                    sb.Append($"+{g.sumContrib:0.###}  ");

                // multiplier product
                if (Mathf.Abs(g.prodMult - 1f) > 0.001f)
                    sb.Append($"×{g.prodMult:0.###}  ");

                // single cap note per group
                if (g.capDef > 0f)
                    sb.Append($"(cap ≤ {g.capDef:0.###})  ");

                sb.AppendLine();
            }

            tipSb.Append(sb.ToString().TrimEnd());
            return tipSb.ToString();
        }
    }

    public class Hediff_MentalRegression : Hediff_RegressionBase
    {
        public static Hediff_MentalRegression HediffByPawn(Pawn pawn)
        {
            return (Hediff_MentalRegression)pawn.health.hediffSet?.hediffs?.FirstOrDefault(x => x is Hediff_MentalRegression);
        }

        const int HealingTickInterval = 150;

        
        private int lastStateYears = -1;
        private float lastStateYearsFloat = -1f;
        private int _lastHealTick;


        public override long BaseAgeBioTicks
        {
            get
            {
                long baselineBioTicks = PhysicalRegressionHediff == null ? pawn.ageTracker.AgeBiologicalTicks : PhysicalRegressionHediff.BaseAgeBioTicks;
                if (baselineBioTicks <= 0) return pawn.ageTracker.AgeBiologicalTicks;
                return baselineBioTicks;
            }
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastStateYears, "ZI_reg_lastStateYears", -1);
            Scribe_Values.Look(ref lastStateYearsFloat, "ZI_reg_lastStateYearsFloat", -1f);
            Scribe_Values.Look(ref _lastHealTick, "ZI_reg_lastHealTick", 0);
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
#if DEBUG
            if (Settings.debugging && Settings.debuggingRegression && dinfo.HasValue) Log.Message($"[ZI] MentalRegressionDamage PostAdd {pawn.LabelShort} from {dinfo.Value.Def?.defName} sev={Severity:0.###}");
#endif
            base.PostAdd(dinfo);

            if (pawn == null) return;

            lastStateYears = -1;
            lastStateYearsFloat = -1f;
            forceTick = true;
        }

        public override void Tick()
        {
            base.Tick();
            if (pawn == null || !pawn.Spawned) return;

            if (!forceTick && !this.pawn.IsHashIntervalTick(HealingTickInterval)) return;

            Severity = pawn.ComputeTotalRegression(true).finalTotal;

            var currMentalTicksYearFloat = MentalTicksYearFloat;
            var hasGrown = lastStateYearsFloat < currMentalTicksYearFloat || forceTick;
            
            //HealStep(HealingTickInterval);
            lastStateYearsFloat = currMentalTicksYearFloat;
            if (lastStateYears != MentalTicksYearInt)
            {
                lastStateYears = MentalTicksYearInt;
                
                refreshAgeStageCache(pawn: pawn);
            }

            forceTick = false;
        }
        private void HealStep(int intervalTicks)
        {
            if (Severity <= 0f) return;

            float perDay = Mathf.Max(0f, Settings.Regression_BaseRecoveryPerDay / 100f);
            if (perDay <= 0f) return;

            float mult = 1f;

            if (Settings.Regression_RestingMultiplier > 0f && pawn.InBed())
                mult *= Settings.Regression_RestingMultiplier;

            if (Settings.Regression_ChildMultiplier > 0f && !pawn.ageTracker.Adult)
                mult *= Settings.Regression_ChildMultiplier;

            if (Settings.Regression_AnimalMultiplier > 0f && pawn.IsAnimal)
                mult *= Settings.Regression_AnimalMultiplier;

            float delta = perDay * mult * (intervalTicks / (float)GenDate.TicksPerDay);

            float before = Severity;
            Severity = Mathf.Max(0f, Severity - delta);
            _lastHealTick = Find.TickManager.TicksGame;

#if DEBUG
            if (Settings.debugging && Settings.debuggingRegression && !Mathf.Approximately(before, Severity))
                Log.Message($"[ZI] Regression Mental heal {pawn.LabelShort} {before:0.###} -> {Severity:0.###} (Δ={delta:0.###})");
#endif
        }

        public override string TipStringExtra
        {
            get
            {
                string tip = base.TipStringExtra;
                //float sevPct = Severity * 100f;
                //tip += (tip.NullOrEmpty() ? "" : "\n") + $"Mental Regression: {sevPct:F1}%";

                tip += $"\nBase Age: {BaseAgeYearFloat:F1} years";
                tip += $"\nMental Age: {Helper_Regression.getAgeStageMentalInt(pawn)} years";

                var reducePercent = Mathf.CeilToInt(LevelsModifierToMask(pawn) * 100f);
                tip += $"\nSkilles reduced: {reducePercent}%";

                tip += $"\nBehaves like: {pawn.getAgeBehaviour()}";
                /*if (Severity > 0f)
                {
                    tip += $"\nEstimated recovery: ~{EstimatedRecoveryDays():0.#} day(s)";
                }*/

                tip += TipExtraDetails(mental: true);
                return tip;
            }
        }

        public override TextureAndColor StateIcon
        {
            get
            {
                return new TextureAndColor(Widgets.GetIconFor(ThingDefOf.ZI_RayGun, null, null, null), ThingDefOf.ZI_RayGun.uiIconColor);
            }
        }
        public override UnityEngine.Color LabelColor
        {
            get
            {
                if (pawn.isAdultMental() && Severity < 0.3f) return UnityEngine.Color.gray;
                float t = Mathf.InverseLerp(0f, def.maxSeverity, Severity);
                return UnityEngine.Color.Lerp(new UnityEngine.Color(1f, 0.9f, 0.3f), new UnityEngine.Color(1f, 0.4f, 0.2f), t);
            }
        }

        public override string LabelBase
        {
            get
            {
                // "Regression (0.62, 2.3 days)"
                string baseLabel = "Mental Regression";
                string pct = Severity.ToStringPercent();
                //string remaining = $"{EstimatedRecoveryDays():0.#} d";
                return $"{baseLabel} ({pct})";
            }
        }




        public int LastMentalYearsInt
        {
            get
            {
                return lastStateYears;
            }
        }
        public float LastMentalYears
        {
            get
            {
                return lastStateYearsFloat;
            }
        }

        public static void refreshAgeStageCache(Pawn pawn)
        {
            Hediff_PhysicalRegression.refreshAgeStageCache(pawn);
        }

        public static float NormalizedSeverityOn(Pawn p)
        {
            if (p?.health == null) return 0f;
            var h = HediffByPawn(p);
            if (h == null) return 0f;

            float max = h.def.maxSeverity > 0f ? h.def.maxSeverity : 1f;
            return Mathf.Clamp01(h.Severity / max);
        }

        public static float LevelsModifierToMask(Pawn p)
        {
            if (p == null) return 0;
            float sev = NormalizedSeverityOn(p);
            return (float)Settings.Regression_LevelMaskBySeverity * sev;
        }
        public static bool WasEverAdult(Pawn p)
        {
            if (p.ageTracker.AgeBiologicalYears >= 13) return true;
            var mentalHediff = HediffByPawn(p);
            if (mentalHediff == null)
            {
                var physicalHediff = Hediff_PhysicalRegression.HediffByPawn(p);
                if(physicalHediff == null) return false;
                return physicalHediff.BaseAgeYearInt >= 13;
            }
            return mentalHediff.BaseAgeYearInt >= 13;
        }

        public static bool CanHaveRole(Pawn p)
        {
            if (!ModsConfig.IdeologyActive) return false;
            var age = Helper_Regression.getAgeStageMentalInt(p);
            return age >= 3;
        }


    }
    public class Hediff_PhysicalRegression : Hediff_RegressionBase
    {
        public static Hediff_PhysicalRegression HediffByPawn(Pawn pawn)
        {
            return (Hediff_PhysicalRegression)pawn?.health?.hediffSet?.hediffs?.FirstOrDefault(x => x is Hediff_PhysicalRegression) ?? null;
        }

        private static bool? _harLoaded;
        public static bool HARLoaded
        {
            get
            {
                if (_harLoaded == null)
                {
                    _harLoaded = LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Humanoid Alien Races");
                }
                return _harLoaded.Value;
            }
        }

        const int HealingTickInterval = 150;
        const int MaxDelta = HealingTickInterval * 4;


        private int _lastHealTick;

        // Persisted state so age can return to what it was
        private long baselineBioTicks = -1;     // original biological age (ticks) when effect first applied
        private long lastBioSeen = -1;       // The last chronological age we have seen/updates ourself. The time difference to it is what we need to advance on the baseline
        private int lastWholeYears = -1;
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

        private enum AgeStagePhysicalTransform { Baby, Child, Adult }
        private AgeStagePhysicalTransform StageFor(long bioTicks)
        {
            var band = ChildBands.Get(pawn);
            if (bioTicks < band.core) return AgeStagePhysicalTransform.Baby;
            if (bioTicks < band.edge) return AgeStagePhysicalTransform.Child;
            return AgeStagePhysicalTransform.Adult;
        }
        private float ResurrectDurationSeconds = 60f;
        private float ResurrectMoveDurationSeconds = 30f;
        public Corpse Corpse { get => pawn.ParentHolder as Corpse; }
        
        public override long BaseAgeBioTicks
        {
            get
            {
                if (baselineBioTicks <= 0) return pawn.ageTracker.AgeBiologicalTicks;
                return baselineBioTicks;
            }
        }

        public bool InProgress
        {
            get
            {
                return !this.resurrectTimer.Finished;
            }
        }

        public override float Severity {
            get => base.Severity;
            set  {
                float old = base.Severity;
                base.Severity = value;
                if (value > old)  
                {
                    // regression increased
                    MaybeApplyRegressionShock(old, value);
                }
                else
                {
                    // regression decrease
                }
            }
        }

        //public override bool Visible => !dormant && base.Visible;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref forceTick, "ZI_reg_forceTick", false);
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
            if (Settings.debugging && Settings.debuggingRegression && dinfo.HasValue) Log.Message($"[ZI] RegressionDamage PostAdd {pawn.LabelShort} from {dinfo.Value.Def?.defName} sev={Severity:0.###}");
#endif
            base.PostAdd(dinfo);

            if (pawn == null) return;

            // Capture baselines once
            if (baselineBioTicks < 0)
                baselineBioTicks = pawn.ageTracker.AgeBiologicalTicks;

            lastWholeYears = pawn.ageTracker.AgeBiologicalYears;
            lastBeardType = pawn.style?.beardDef;

            forceTick = true;
            if (IsHumanlike(pawn)) new HediffGiver_BedWetting().OnIntervalPassed(pawn, null);
        }
        public override void PreRemoved()
        {
            base.PreRemoved();
            forceTick = true;
            Severity = 0f;
            Tick();
        }

        
 
        void MaybeApplyRegressionShock(float oldSeverity, float newSeverity)
        {
            if (pawn == null || !pawn.RaceProps.Animal) return;

            if (newSeverity > 1.0f && oldSeverity <= 1.0f)
            {
                pawn.mindState?.mentalStateHandler?.TryStartMentalState(MentalStateDefOf.WanderConfused);
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
                

                if(StageFor(desiredBio) != AgeStagePhysicalTransform.Adult)
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
                            case AgeStagePhysicalTransform.Baby: OnEnterBaby(pawn); break;
                            case AgeStagePhysicalTransform.Child: OnEnterChild(pawn); break;
                            case AgeStagePhysicalTransform.Adult: OnEnterAdult(pawn); break;
                        }
                        if (pawn.stances?.stunner != null)
                        {
                            int stunTicks = Rand.Range(150, 300);
                            pawn.stances.stunner.StunFor(stunTicks, null, true);
                        }
                    }

                    
                    SyncBirthdayHediffs(pawn, desiredBio);
                    ProcessDeferredBirthdaysOnAgeUp();
                    lastWholeYears = curYears;
                    refreshAgeStageCache(pawn: pawn);
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
                /* #if DEBUG
                                Unused now because bio deltas can be really high with some of the easier to cure damages
                                 * if (dtBio > MaxDelta)
                                {

                                    Log.Warning($"[ZI] Regression: bio delta {dtBio} over {MaxDelta}. Probably debugging step for {pawn.LabelShortCap}");

                                }
                #endif */
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





        

        // If you change any *internal* tuning fields that affect CapMods without changing Severity,
        // call this to force a recalc:
        void MarkCapsDirty() => pawn?.health?.capacities?.Notify_CapacityLevelsDirty();

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
        public static void refreshAgeStageCache(Pawn pawn)
        {
            Helper_Regression.refreshAllAgeStageCaches(pawn);
            pawn.Notify_DisabledWorkTypesChanged();
            pawn.needs?.AddOrRemoveNeedsAsAppropriate();
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            pawn.workSettings?.EnableAndInitialize();
            pawn.skills?.Notify_SkillDisablesChanged();
            Find.ColonistBar?.MarkColonistsDirty();
            if (IsHumanlike(pawn)) new HediffGiver_BedWetting().OnIntervalPassed(pawn, null);
        }
        bool debugging => (Settings.debugging && Settings.debuggingRegression);
        public void TickRare()
        {
            if(!resurrectTimer.Finished && Severity != def.maxSeverity && Severity > (def.maxSeverity-0.009f) && Settings.Regression_RessurectChance > 0)
            {
                if(Settings.Regression_RessurectChance == 1.0f || Rand.ChanceSeeded(Settings.Regression_RessurectChance, pawn.HashOffsetTicks() + 462))
                {
                    Severity = def.maxSeverity;
                }
                else
                {
                    Severity = def.maxSeverity - 0.01f;
                }
            }
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

            //HealStep(HealingTickInterval);
            Severity = pawn.ComputeTotalRegression(false).finalTotal;
            ApplyAgeMapping(force: forceTick);
            forceTick = false;

            //if (Severity <= 0f && !dormant) dormant = true;
        }

        private void HealStep(int intervalTicks)
        {
            if (Severity <= 0f) return;

            float perDay = Mathf.Max(0f, Settings.Regression_BaseRecoveryPerDay / 100f);
            if (perDay <= 0f) return;

            float mult = 1f;

            if (Settings.Regression_RestingMultiplier > 0f && pawn.InBed())
                mult *= Settings.Regression_RestingMultiplier;

            if (Settings.Regression_ChildMultiplier > 0f && !pawn.ageTracker.Adult)
                mult *= Settings.Regression_ChildMultiplier;

            // If you want tending to help; Hediff has IsTended() in recent RimWorld versions.
            if (Settings.Regression_AnimalMultiplier > 0f && pawn.IsAnimal)
                mult *= Settings.Regression_AnimalMultiplier;

            float delta = perDay * mult * (intervalTicks / (float)GenDate.TicksPerDay);

            float before = Severity;
            Severity = Mathf.Max(0f, Severity - delta);
            _lastHealTick = Find.TickManager.TicksGame;

#if DEBUG
            if (Settings.debugging && Settings.debuggingRegression && !Mathf.Approximately(before, Severity))
                Log.Message($"[ZI] Regression heal {pawn.LabelShort} {before:0.###} -> {Severity:0.###} (Δ={delta:0.###})");
#endif
        }
        private float EstimatedRecoveryDays()
        {
            float perDay = Settings.Regression_BaseRecoveryPerDay / 100f;
            if (perDay <= 0f) return 0f;
            return Severity / perDay;
        }

        public override string TipStringExtra
        {
            get
            {
                string tip = base.TipStringExtra;
                float sevPct = Severity * 100f;
                tip += (tip.NullOrEmpty() ? "" : "\n") + $"Physical Regression: {sevPct:F1}%";

       
                if (Severity > 0f)
                {
                    tip += $"\nBase Age: {BaseAgeYearFloat:F1} years";
                    tip += $"\nPhysical Age: {Helper_Regression.getAgeStagePhysical(pawn):F1} years";
                    if(Memory != null && Memory.desiredAgeYears != -1)
                    {
                        tip += $"\nDesired Age: {Memory.desiredAgeYears}";
                    }
                    tip += $"\nBehaves Like: {pawn.getAgeBehaviour()}";
                    
                    // Neutral-condition ETA: assumes multiplier ~1
                    //tip += $"\nEstimated recovery: ~{EstimatedRecoveryDays():0.#} day(s)";
                }

                tip += TipExtraDetails(mental: false);

                return tip;
            }
        }
        public override TextureAndColor StateIcon
        {
            get
            {
                return new TextureAndColor(Widgets.GetIconFor(ThingDefOf.ZI_Foy_Vial, null, null, null), ThingDefOf.ZI_Foy_Vial.uiIconColor);
            }
        }
        public override UnityEngine.Color LabelColor
        {
            get
            {
                if (pawn.isAdultPhysical() && Severity < 0.3f) return UnityEngine.Color.gray;
                float t = Mathf.InverseLerp(0f, def.maxSeverity, Severity);
                return UnityEngine.Color.Lerp(new UnityEngine.Color(1f, 0.9f, 0.3f), new UnityEngine.Color(1f, 0.4f, 0.2f), t);
            }
        }
        public override string LabelBase
        {
            get
            {
                // "Regression (0.62, 2.3 days)"
                string baseLabel = "Physical Regression";
                string pct = Severity.ToStringPercent();
                //return $"{baseLabel} ({pct}, {remaining})";

                /*int nSources = pawn.health.hediffSet.hediffs
                    .OfType<HediffWithComps>()
                    .Count(h => h.TryGetComp<HediffComp_RegressionInfluence>() is { } c && c.IsInfluence(mental: false));
                float mult = pawn.ComputeTotalRegression(false).productMult;*/
                // [{nSources} src · ×{mult:0.##}]
                return $"{baseLabel} ({pct})";
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos()) yield return g;

            if (pawn.Drafted) yield break;
            if (Find.Selector?.SelectedPawns?.Count >= 2) yield break;

            if (pawn.RaceProps != null && pawn.RaceProps.Humanlike)
            {
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
            }
            if (!Prefs.DevMode) yield break;

            yield return new Command_Action
            {
                defaultLabel = "DBG: Apply regression (damage)…",
                defaultDesc = "Applies regression via debug DamageInfo.",
                action = () => Find.WindowStack.Add(new Dialog_Slider("Damage amount", 0, 100, v => ZI_DebugRegression.ApplyByDamage(pawn, v)))
            };

            yield return new Command_Action
            {
                defaultLabel = "DBG: Set physical regression (severity)…",
                defaultDesc = "Directly sets the regression hediff severity.",
                action = () => Find.WindowStack.Add(new Dialog_Slider("Severity (0–100%)", 0, 100, v => ZI_DebugRegression.ApplyBySeverityPhysical(pawn, v / 100f)))
            };
            yield return new Command_Action
            {
                defaultLabel = "DBG: Set mental regression (severity)…",
                defaultDesc = "Directly sets the regression hediff severity.",
                action = () => Find.WindowStack.Add(new Dialog_Slider("Severity (0–100%)", 0, 100, v => ZI_DebugRegression.ApplyBySeverityMental(pawn, v / 100f)))
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

            // Allow up to ~1.25× life expectancy
            int life2 = Mathf.Max(10, Mathf.RoundToInt(pawn.RaceProps.lifeExpectancy * 1.25f));
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

            float realAge = Hediff_PhysicalRegression.HediffByPawn(pawn)?.BaseAgeYearFloat ?? (pawn.ageTracker.AgeBiologicalTicks / GenDate.TicksPerYear);
            list.Label($"Pawn: {pawn.LabelShortCap} Real age: {realAge:F1} Physical age: {Helper_Regression.getAgeStagePhysical(pawn):F1}  (range {minAllowed}–{maxAllowed})");
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
                
                int cur = Mathf.FloorToInt(realAge);
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

            float mask = Hediff_MentalRegression.LevelsModifierToMask(pawn);
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

            float mask = Hediff_MentalRegression.LevelsModifierToMask(pawn);
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
            var reg = Hediff_PhysicalRegression.HediffByPawn(pawn);
            return reg == null || !reg.suppressVanillaBirthdays || Hediff_PhysicalRegression.AllowBirthdayThrough;
        }
    }
    // block vanilla birthdays unless explicitly allowed
    [HarmonyPatch(typeof(Pawn_AgeTracker), "BirthdayBiological")]
    static class Patch_SuppressBirthdayBiological
    {
        public static bool Prefix(Pawn_AgeTracker __instance)
        {
            var pawn = AccessTools.FieldRefAccess<Pawn_AgeTracker, Pawn>("pawn")(__instance);
            var reg = Hediff_PhysicalRegression.HediffByPawn(pawn);
            return reg == null || !reg.suppressVanillaBirthdays || Hediff_PhysicalRegression.AllowBirthdayThrough;
        }
    }

    // when mothballed age advances, move baseline and fire synthetic birthdays.
    [HarmonyPatch(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.AgeTickMothballed))]
    static class Patch_AgeTickMothballed_Postfix
    {
        public static void Postfix(Pawn_AgeTracker __instance, int interval)
        {
            var pawn = AccessTools.FieldRefAccess<Pawn_AgeTracker, Pawn>("pawn")(__instance);
            var reg = Hediff_PhysicalRegression.HediffByPawn(pawn);
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

        public int targetDisposablesCount = 2; // Amount of disposables this pawn should have in the inventory
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref removedBirthday, "ZI_removedBirthday", LookMode.Deep);
            if (removedBirthday == null) removedBirthday = new List<StoredHediff>();
            Scribe_Values.Look(ref desiredAgeYears, "desiredAgeYears", -1);
            Scribe_Values.Look(ref desiredAgeDoseTick, "desiredAgeDoseTick", -1);
            Scribe_Values.Look(ref pendingPassionSwaps, "ZI_pendingPassionSwaps", 0);

            Scribe_Values.Look(ref targetDisposablesCount, "targetDisposablesCount", 2);
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

            Hediff_PhysicalRegression firstHediff = Hediff_PhysicalRegression.HediffByPawn(pawn);
            if (firstHediff != null)
            {
                firstHediff.TickRare();
            }
        }
    }
}

using DubsBadHygiene;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using Verse;
using Verse.AI;
using Verse.Sound;
using static ZealousInnocence.Hediff_RegressionDamage;

namespace ZealousInnocence
{
    public static class Helper_Regression
    {
        private static Dictionary<Pawn, AgeStageInfo> cachedMentalAgeStages = new Dictionary<Pawn, AgeStageInfo>();
        public static float getAgeStageMental(this Pawn pawn, bool force = false)
        {
            if (!cachedMentalAgeStages.TryGetValue(pawn, out var value) || (value.lastCheckTick + 60) < Find.TickManager.TicksGame || force)
            {
                refreshAgeStageMentalCache(pawn);
                cachedMentalAgeStages.TryGetValue(pawn, out value);
            }

            return value.cachedAgeStage;
        }
        public static int getAgeStageMentalInt(this Pawn pawn, bool force = false)
        {
            return Mathf.FloorToInt(getAgeStageMental(pawn,force));
        }
        private static void refreshAgeStageMentalCache(Pawn pawn)
        {
            Dictionary<Pawn, AgeStageInfo> dictionary = cachedMentalAgeStages;
            AgeStageInfo obj = new AgeStageInfo
            {
                cachedAgeStage = fetchAgeStageMental(pawn),
                lastCheckTick = Find.TickManager.TicksGame
            };
            dictionary[pawn] = obj;
        }

        private static float fetchAgeStageMental(Pawn pawn)
        {
            if (pawn == null)
            {
                return 14f;
            }

            foreach (var curr in pawn.health.hediffSet.hediffs)
            {
                if (curr.def == HediffDefOf.RegressionState)
                {
                    // RegressionState isn't a thing on children
                    if (pawn.ageTracker.AgeBiologicalYears < 13)
                    {
                        pawn.health.RemoveHediff(curr);
                        return pawn.ageTracker.AgeBiologicalYears;
                    }
                    return curr.CurStageIndex * 3;
                }
            }
            var hediff = Hediff_RegressionDamageMental.HediffByPawn(pawn);
            if (hediff != null && hediff.LastMentalYears > 0f) return hediff.LastMentalYears;

            var hediff2 = Hediff_RegressionDamage.HediffByPawn(pawn);
            if (hediff2 != null) return hediff2.BaseAgeYearFloat;

            return pawn.ageTracker.AgeBiologicalYearsFloat;
        }

        public static void refreshAllAgeStageCaches(this Pawn pawn)
        {
            /*
            float oldValuePhysical = -1;
            if(cachedMentalAgeStages.TryGetValue(pawn, out var value)) oldValuePhysical = value.cachedAgeStage;
            
            float oldValueMental = -1;
            if (cachedMentalAgeStages.TryGetValue(pawn, out var value2)) oldValueMental = value2.cachedAgeStage;
            */
            refreshAgeStagePhysicalCache(pawn);
            refreshAgeStageMentalCache(pawn);

            //Log.Message($"[ZI]Resetting ageStageCaches Mental {oldValueMental:F2} => {getAgeStageMental(pawn, false):F2} Physical {oldValuePhysical:F2} => {getAgeStagePhysical(pawn, false):F2} for {pawn.Name}");
        }
        public static float getAgeStagePhysicalMentalMin(this Pawn pawn, bool force = false)
        {
            return Mathf.Min(getAgeStagePhysical(pawn, force), getAgeStageMental(pawn, force));
        }

        private static Dictionary<Pawn, AgeStageInfo> cachedPhysicalAgeStages = new Dictionary<Pawn, AgeStageInfo>();
        public static float getAgeStagePhysical(this Pawn pawn, bool force = false)
        {
            if (!cachedPhysicalAgeStages.TryGetValue(pawn, out var value) || (value.lastCheckTick + 60) < Find.TickManager.TicksGame || force)
            {
                refreshAgeStagePhysicalCache(pawn);
                cachedPhysicalAgeStages.TryGetValue(pawn, out value);
            }

            return value.cachedAgeStage;
        }

        private static void refreshAgeStagePhysicalCache(Pawn pawn)
        {
            Dictionary<Pawn, AgeStageInfo> dictionary = cachedPhysicalAgeStages;
            AgeStageInfo obj = new AgeStageInfo
            {
                cachedAgeStage = fetchAgeStagePhysical(pawn),
                lastCheckTick = Find.TickManager.TicksGame
            };
            dictionary[pawn] = obj;
        }

        private static float fetchAgeStagePhysical(Pawn pawn)
        {
            if (pawn == null)
            {
                return 14f;
            }

            return pawn.ageTracker.AgeBiologicalYearsFloat;
        }
        public static bool ShouldHaveLearning(this Pawn p)
        {
            if (p?.health == null) return false;
            if (!p.RaceProps.Humanlike) return false;
            if (ShouldHavePlaying(p)) return false;
            return isChildMental(p);
        }
        public static bool ShouldHavePlaying(this Pawn p)
        {
            if (p?.health == null) return false;
            if (!p.RaceProps.Humanlike) return false;
            return isToddlerMentalOrPhysical(p) || isBabyMentalOrPhysical(p);
        }
        public static bool canChangeDiaperOrUnderwear(Pawn p)
        {
            if (p == null || p.Dead) return false;
            if (!p.RaceProps.Humanlike) return false;

            if (p.Downed) return false;

            var caps = p.health?.capacities;
            if (caps == null) return false;

            // Match common work thresholds: must be at least minimally conscious and able to move & manipulate.
            if (caps.GetLevel(RimWorld.PawnCapacityDefOf.Consciousness) < 0.3f) return false;
            if (caps.GetLevel(RimWorld.PawnCapacityDefOf.Moving) <= 0.25f) return false;
            if (caps.GetLevel(RimWorld.PawnCapacityDefOf.Manipulation) <= 0.25f) return false;

            if (p.InMentalState && p.MentalStateDef.blockNormalThoughts) return false;

            return !isToddlerMentalOrPhysical(p);
        }   

        public static AcceptanceReport canUsePottyReport(Pawn pawn)
        {
            if (pawn.needs == null) Log.WarningOnce($"[ZI] {pawn} has null needs tracker.", 0x1A7E001);
            if (pawn.story == null) Log.WarningOnce($"[ZI] {pawn} has null story.", 0x1A7E002);
            if (pawn.story?.traits == null) Log.WarningOnce($"[ZI] {pawn} has null traits.", 0x1A7E003);
            if (TraitDefOf.Potty_Rebel == null) Log.WarningOnce("[ZI] TraitDefOf.Potty_Rebel is null (DefOf init?).", 0x1A7E004);

            var diaperNeed = pawn.needs.TryGetNeed<Need_Diaper>();
            if (diaperNeed == null) return true; // anything without that gets a free pass
            if (pawn.story.traits.HasTrait(TraitDefOf.Potty_Rebel))
            {
                return "ShortReasonNotComply".Translate(); // will never run to a potty!
            }

            // Mental states can make them unable to remember that
            MentalState mentalState = pawn.MentalState;
            if (mentalState != null && mentalState.def != null && (mentalState.def == RimWorld.MentalStateDefOf.PanicFlee || mentalState.def == RimWorld.MentalStateDefOf.Wander_Psychotic))
            {
                return "ShortReasonWrongMentalState".Translate(); // wrong mental state
            }

            var currDiapie = Helper_Diaper.getDiaper(pawn);
            if (currDiapie != null)
            {
                if (pawn.outfits?.forcedHandler != null && pawn.outfits.forcedHandler.IsForced(currDiapie))
                { 
                    return "ShortReasonForcedInDiaper".Translate(); // forced in diapers
                }
                if (!canChangeDiaperOrUnderwear(pawn))
                {
                    return "ShortReasonCantChangeDiaperOrUnderwearSelf".Translate();
                }
            }
            return true;
        }
        public static bool isAdultInEveryWay(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return false;
            return !pawn.DevelopmentalStage.Juvenile() && pawn.isAdultMental() && pawn.isAdultPhysical();
        }
        public static bool isAdultMental(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return true;
            return isAdultAtAge(pawn, getAgeStageMental(pawn, forceRecheck));
        }
        public static bool isChildMental(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return false;
            return isChildAtAge(pawn, getAgeStageMental(pawn, forceRecheck));
        }
        public static bool isToddlerMental(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return false;
            return isToddlerAtAge(pawn, getAgeStageMental(pawn, forceRecheck));
        }
        public static bool isBabyMental(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return false;
            return isBabyAtAge(pawn, getAgeStageMental(pawn, forceRecheck));
        }

        public static bool isAdultPhysical(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return false;
            return isAdultAtAge(pawn, getAgeStagePhysical(pawn, forceRecheck));
        }
        public static bool isChildPhysical(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return false;
            return isChildAtAge(pawn, getAgeStagePhysical(pawn, forceRecheck));
        }
        public static bool isToddlerPhysical(this Pawn pawn, bool forceRecheck = false)
        {
            return isToddlerAtAge(pawn, getAgeStagePhysical(pawn, forceRecheck));
        }
        public static bool isBabyPhysical(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return false;
            return isBabyAtAge(pawn, getAgeStagePhysical(pawn, forceRecheck));
        }
        public static bool isOld(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return false;
            return isOldAtAge(pawn, getAgeStagePhysical(pawn, forceRecheck));
        }
        public static bool isTeen(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return false;
            return isTeenAtAge(pawn, getAgeStagePhysical(pawn, forceRecheck));
        }

        public static bool isAdultMentalOrPhysical(this Pawn pawn, bool forceRecheck = false)
        {
            return isAdultMental(pawn, forceRecheck) || isAdultPhysical(pawn, forceRecheck);
        }
        public static bool isChildMentalOrPhysical(this Pawn pawn, bool forceRecheck = false)
        {
            return isChildMental(pawn, forceRecheck) || isChildPhysical(pawn, forceRecheck);
        }
        public static bool isToddlerMentalOrPhysical(this Pawn pawn, bool forceRecheck = false)
        {
            return isToddlerMental(pawn, forceRecheck) || isToddlerPhysical(pawn, forceRecheck);
        }
        public static bool isBabyMentalOrPhysical(this Pawn pawn, bool forceRecheck = false)
        {
            return isBabyMental(pawn, forceRecheck) || isBabyPhysical(pawn, forceRecheck);
        }

        public static float toddlerMinAge(this Pawn pawn)
        {
            return ChildBands.Get(pawn).toddler / GenDate.TicksPerYear;
        }
        public static float childMinAge(this Pawn pawn)
        {
            return ChildBands.Get(pawn).core / GenDate.TicksPerYear;
        }
        public static float adultMinAge(this Pawn pawn)
        {
            return ChildBands.Get(pawn).edge / GenDate.TicksPerYear;
        }
        public static float teenMaxAge(this Pawn pawn)
        {
            float adultStart = pawn.adultMinAge();
            float lifeExp = pawn.RaceProps.lifeExpectancy;

            // A human (~100y) has adultStart ≈14; teen window = 14–17 → 3y ≈3% of life
            float teenSpan = lifeExp * 0.03f;      // ~3% of lifespan as “teen”
            float teenEnd = adultStart + teenSpan;

            return Mathf.FloorToInt(teenEnd);
        }
        public static float oldMinAge(this Pawn pawn)
        {
            return Mathf.FloorToInt(pawn.RaceProps.lifeExpectancy * 0.65f);
        }
       
        public static bool isBabyAtAge(this Pawn pawn, float ageYears)
        {
            return ageYears < toddlerMinAge(pawn);
        }
        public static bool isToddlerAtAge(this Pawn pawn, float ageYears)
        {
            return ageYears >= toddlerMinAge(pawn) && ageYears < childMinAge(pawn);
        }
        public static bool isChildAtAge(this Pawn pawn, float ageYears)
        {
            return ageYears >= childMinAge(pawn) && ageYears < adultMinAge(pawn);
        }
        public static bool isAdultAtAge(this Pawn pawn, float ageYears)
        {
            return ageYears >= adultMinAge(pawn);
        }
        public static bool isTeenAtAge(this Pawn pawn, float ageYears)
        {
            return ageYears <= teenMaxAge(pawn) && isAdultAtAge(pawn, ageYears) && !isOldAtAge(pawn,ageYears);
        }
        public static bool isOldAtAge(this Pawn pawn, float ageYears)
        {
            return ageYears >= oldMinAge(pawn) && isAdultAtAge(pawn,ageYears);
        }



        public static void healPawnBrain(Pawn pawn)
        {
            for (int num = pawn.health.hediffSet.hediffs.Count - 1; num >= 0; num--)
            {
                var curr = pawn.health.hediffSet.hediffs[num];
                if (curr.Part?.def == RimWorld.BodyPartDefOf.Head || curr.Part?.def.tags?.Contains(RimWorld.BodyPartTagDefOf.ConsciousnessSource) == true)
                {
                    // Always preserve implants/added parts.
                    if (curr.def.countsAsAddedPartOrImplant) continue;
                    pawn.health.RemoveHediff(curr);
                }
            }

            /*Hediff hediff = HediffMaker.MakeHediff(RimWorld.HediffDefOf.ResurrectionSickness, innerPawn);
            if (!innerPawn.health.WouldDieAfterAddingHediff(hediff))
            {
                innerPawn.health.AddHediff(hediff);
            }*/

        }

        private static void healPawnMissingBodyparts(Pawn pawn, bool healAddictions = false)
        {
            if (pawn?.health?.hediffSet == null) return;
            var hediffs = new List<Hediff>(pawn.health.hediffSet.hediffs);
            // --- 1) Restore missing natural parts (no implant replacement)
            foreach (var h in hediffs)
            {
                if (h is Hediff_MissingPart miss && miss.Part != null)
                {
                    IntVec3 pos = pawn.Spawned ? pawn.Position : IntVec3.Invalid;
                    Map map = pawn.Spawned ? pawn.Map : null;
                    MedicalRecipesUtility.RestorePartAndSpawnAllPreviousParts(pawn, miss.Part, pos, map);
                }
            }

            pawn.health.hediffSet.DirtyCache();
        }

        public static bool regressOrReincarnateToChild(Pawn pawn,ThingDef cause,  bool targetMentalRegression, out List<Hediff> removedHediffs, out float pawnAgeDelta)
        {
            removedHediffs = new List<Hediff>();
            pawnAgeDelta = 0f;
            if (LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().reduceAge && !targetMentalRegression)
            {
                return reincarnateToChildPawn(pawn, cause, out removedHediffs, out pawnAgeDelta);
            }
            else
            {
                return mentalRegressPawn(pawn);
            }
        }
        public static bool mentalRegressPawn(Pawn pawn)
        {
            healPawnBrain(pawn);
            Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.RegressionDamageMental, pawn);
            hediff.Severity = hediff.def.maxSeverity;
            pawn.health.AddHediff(hediff);

            healPawnMissingBodyparts(pawn, true);
            refreshAgeStageMentalCache(pawn);
            Messages.Message("MessagePawnRegressed".Translate(pawn), pawn, MessageTypeDefOf.CautionInput);
            return true;
        }

        public static void dropAllUnwearable(Pawn pawn)
        {
            if (pawn?.apparel == null) return;

            // Snapshot list since Drop() mutates the collection.
            var worn = pawn.apparel.WornApparel.ListFullCopy();
            IntVec3 pos = pawn.PositionHeld;

            foreach (var apparel in worn)
            {
                if (!apparel.PawnCanWear(pawn, true) || !Helper_Diaper.allowedByPolicy(pawn, apparel))
                {
                    // Drop it on the ground but keep all other apparel.
                    pawn.apparel.TryDrop(apparel, out Apparel dropped, pos, forbid: false);
                }
            }
        }

        public static bool reincarnateToChildPawn(Pawn pawn, ThingDef cause, out List<Hediff> removedHediffs, out float pawnAgeDelta)
        {
            SetRegressionSeverityMental(pawn, cause, 1.0f);
            SetRegressionSeverityPhysical(pawn, cause, 1.0f);
            removedHediffs = new List<Hediff>();
            pawnAgeDelta = 0;
            return true;

            int oldAge = pawn.ageTracker.AgeBiologicalYears;
            float num = pawn.ageTracker.AgeBiologicalYears;
            
            if (pawn.ageTracker.Adult)
            {
                //num = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().targetChronoAge;
                pawnAgeDelta = pawn.ageTracker.AgeBiologicalYearsFloat - num;
                pawn.ageTracker.AgeBiologicalTicks = Mathf.RoundToInt(num * 3600000f);
            }

            if (pawn.ageTracker.AgeBiologicalYears < 13 && oldAge >= 13)
            {
                if (pawn.records.GetValue(RecordDefOf.ResearchPointsResearched) == 0)
                {
                    pawn.records.AddTo(RecordDefOf.ResearchPointsResearched, 1f);
                }

                pawn.style.beardDef = BeardDefOf.NoBeard;
                pawn.story.bodyType = BodyTypeDefOf.Child;

                if (pawn.IsCreepJoiner)
                {
                    pawn.creepjoiner.form = (CreepJoinerFormKindDef)CreepJoinerFormKindDef.Named("LoneGenius");
                    pawn.creepjoiner.form.bodyTypeGraphicPaths.Clear();
                }

                pawn.ageTracker.canGainGrowthPoints = true;
                pawn.Drawer.renderer.SetAllGraphicsDirty();
                Find.ColonistBar.MarkColonistsDirty();

                dropAllUnwearable(pawn);
            }

            if (pawn.ageTracker.AgeBiologicalYears < 13)
            {
                pawn.Notify_DisabledWorkTypesChanged();
            }

            healPawnBrain(pawn);
            
            List<HediffGiverSetDef> hediffGiverSets = pawn.RaceProps.hediffGiverSets;
            if (hediffGiverSets != null)
            {
                List<Hediff> resultHediffs = new List<Hediff>();
                foreach (HediffGiverSetDef item in hediffGiverSets)
                {
                    List<HediffGiver> hediffGivers = item.hediffGivers;
                    if (hediffGivers == null)
                    {
                        continue;
                    }
                    foreach (HediffGiver item2 in hediffGivers)
                    {
                        HediffGiver_Birthday agb;
                        if ((agb = item2 as HediffGiver_Birthday) == null)
                        {
                            continue;
                        }
                        float num2 = num / pawn.RaceProps.lifeExpectancy;
                        float x = agb.ageFractionChanceCurve.Points[0].x;
                        if (!(num2 < x))
                        {
                            continue;
                        }
                        pawn.health.hediffSet.GetHediffs(ref resultHediffs, (Hediff hd) => hd.def == agb.hediff);
                        foreach (Hediff item3 in resultHediffs)
                        {
                            pawn.health.RemoveHediff(item3);
                            removedHediffs.Add(item3);
                        }
                    }
                }

            }
            SetRegressionSeverityPhysical(pawn, cause, 0.7f);

            HediffGiver_BedWetting bedWettingGiver = new HediffGiver_BedWetting();
            bedWettingGiver.OnIntervalPassed(pawn, null);
            refreshAgeStageMentalCache(pawn);

            return false;
        }


        /*int num3 = Rand.RangeInclusive(1, 2);
for (int num4 = pawn.health.hediffSet.hediffs.Count - 1; num4 >= 0; num4--)
{
    Hediff hediff = pawn.health.hediffSet.hediffs[num4];
    if (hediff is Hediff_Injury && hediff.IsPermanent())
    {
        pawn.health.RemoveHediff(hediff);
        removedHediffs.Add(hediff);
        num3--;
        if (num3 <= 0)
        {
            break;
        }
    }
}*/

        //healPawnMissingBodyparts(pawn, true);


        public static bool CanBeRegressed(Pawn pawn)
        {
            if (pawn?.RaceProps?.IsFlesh == true) return true;
            return false;
        }
        public static DamageInfo ReduceDamage(DamageInfo dInfo) //we want to reduce the damage so weapons look more damage then they actually are 
        {
            float reduceAmount = 1f;
            var extensionInfo = dInfo.Def.GetModExtension<RegressionDamageExtension>();
            if (extensionInfo != null)
            {
                reduceAmount = extensionInfo.reduceValue;
            }
            float reducedDamage = dInfo.Amount - (dInfo.Amount * reduceAmount);

            dInfo = new DamageInfo(dInfo.Def, reducedDamage, dInfo.ArmorPenetrationInt, dInfo.Angle, dInfo.Instigator,
                                   dInfo.HitPart, dInfo.Weapon, dInfo.Category, dInfo.intendedTargetInt);
            return dInfo;
        }

        public static void SetRegressionSeverityMental([NotNull] Pawn pawn, [NotNull] ThingDef cause, float severityAmount)
        {
            if (pawn == null || severityAmount <= 0f) return;

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.RegressionDamageMental);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(HediffDefOf.RegressionDamageMental, pawn);
                pawn.health.AddHediff(hediff);
                hediff.Severity = severityAmount;
            }
            else
            {
                hediff.Severity = severityAmount;
            }
            hediff.sourceDef = cause;
        }
        public static void IncreaseRegressionSeverityMental([NotNull] Pawn pawn, [NotNull] ThingDef cause, float severityAmount)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.RegressionDamageMental);
            if (hediff == null) SetRegressionSeverityMental(pawn, cause, severityAmount);
            else SetRegressionSeverityMental(pawn, cause, hediff.Severity + severityAmount);
        }
        public static void SetRegressionSeverityPhysical([NotNull] Pawn pawn, [NotNull] ThingDef cause, float severityAmount)
        {
            if (pawn == null || severityAmount <= 0f) return;

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.RegressionDamage);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(HediffDefOf.RegressionDamage, pawn);
                pawn.health.AddHediff(hediff);
                hediff.Severity = severityAmount;
            }
            else
            {
                hediff.Severity = severityAmount;
            }
            hediff.sourceDef = cause;
        }
        public static void IncreaseRegressionSeverityPhysical([NotNull] Pawn pawn, [NotNull] ThingDef cause, float severityAmount)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.RegressionDamage);
            if (hediff == null) SetRegressionSeverityPhysical(pawn, cause, severityAmount);
            else SetRegressionSeverityPhysical(pawn, cause, hediff.Severity + severityAmount);
        }
        // Will set the regression severity to a defined level or increase the severity by the minIncrease. Never decreases.
        public static void SetIncreasedRegressionSeverity(
            [NotNull] Pawn pawn,
            [NotNull] ThingDef source,
            float severityAmount,
            float minIncrease = 0.0f)
        {
            SetIncreasedRegressionSeverityPhysical(pawn,source, severityAmount, minIncrease);
            SetIncreasedRegressionSeverityMental(pawn,source, severityAmount, minIncrease);
        }
        public static void SetIncreasedRegressionSeverityPhysical(
            [NotNull] Pawn pawn,
            [NotNull] ThingDef source,
            float severityAmount,
            float minIncrease = 0.0f)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.RegressionDamage);

            SetRegressionSeverityPhysical(pawn, source, hediff == null ? severityAmount : Math.Max(severityAmount, hediff.Severity + minIncrease));
        }
        public static void SetIncreasedRegressionSeverityMental(
            [NotNull] Pawn pawn,
            [NotNull] ThingDef source,
            float severityAmount,
            float minIncrease = 0.0f)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.RegressionDamageMental);

            SetRegressionSeverityMental(pawn, source, hediff == null ? severityAmount : Math.Max(severityAmount, hediff.Severity + minIncrease));
        }
        public static void ApplyPureRegressionDamage(DamageInfo dInfo, [NotNull] Pawn pawn, DamageWorker.DamageResult result, float originalDamage)
        {
            var ext = dInfo.Def.GetModExtension<RegressionDamageExtension>();
            if (ext == null)
            {
                Log.Warning("Damage caused by regression weapon does not contain damage extension!");
                return;
            }

            float severityToAddMental = Mathf.Clamp(originalDamage * ext.mentalSeverityPerDamage, 0, Mathf.Min(HediffDefOf.RegressionDamageMental.maxSeverity,ext.mentalMaxSeverity) );
            IncreaseRegressionSeverityMental(pawn, dInfo.Weapon, severityToAddMental);

            float severityToAddPhysical = Mathf.Clamp(originalDamage * ext.physicalSeverityPerDamage, 0, Mathf.Min(HediffDefOf.RegressionDamage.maxSeverity, ext.physicalMaxSeverity));
            IncreaseRegressionSeverityPhysical(pawn, dInfo.Weapon, severityToAddPhysical);

            /*if (hediff is ICaused caused)
            {
                if (dInfo.Weapon != null)
                    caused.Causes.Add(MutationCauses.WEAPON_PREFIX, dInfo.Weapon);

                if (mutagen != null)
                    caused.Causes.Add(MutationCauses.MUTAGEN_PREFIX, mutagen);

                if (dInfo.Def != null)
                    caused.Causes.Add(string.Empty, dInfo.Def);
            }*/
            Log.Message($"[ZI]original damage:{originalDamage}, reducedDamage{dInfo.Amount}, mentalAdd:{severityToAddMental}, physicalAdd:{severityToAddPhysical}");
        }
    }

    public class AgeStageInfo
    {
        public float cachedAgeStage;

        public int lastCheckTick;
    }

    public class Recipe_Regression : Recipe_Surgery
    {
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            yield return pawn.health.hediffSet.GetBrain();
        }
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (IsViolationOnPawn(pawn, part, Faction.OfPlayer))
            {
                ReportViolation(pawn, billDoer, pawn.HomeFaction, -30);
            }
            SoundDefOf.MechSerumUsed.PlayOneShot(SoundInfo.InMap(pawn));

            bool targetMentalRegression = false;
            if (this.recipe.HasModExtension<RecipeExtension_RegressionParameter>())
            {
                targetMentalRegression = this.recipe.GetModExtension<RecipeExtension_RegressionParameter>().targetMentalRegression;
            }
            var hediffsRemoved = new List<Hediff>();
            float ageDifference = 0f;
            Helper_Regression.regressOrReincarnateToChild(pawn,billDoer.def, targetMentalRegression,  out hediffsRemoved, out ageDifference);

        }
    }

    public class RecipeExtension_RegressionParameter : DefModExtension
    {
        public bool targetMentalRegression = false;
    }

    [HarmonyPatch(typeof(HealthAIUtility))]
    public static class Patch_HealthAIUtility
    {

        [HarmonyPostfix]
        [HarmonyPatch(nameof(HealthAIUtility.ShouldSeekMedicalRest))]
        public static void ShouldSeekMedicalRest(Pawn pawn, ref bool __result)
        {
            if (__result == false)
            {
                Need_Diaper need_diaper = pawn.needs.TryGetNeed<Need_Diaper>();
                if (need_diaper == null) return;
                if (Helper_Regression.canChangeDiaperOrUnderwear(pawn)) return;

                var diaper = Helper_Diaper.getUnderwearOrDiaper(pawn);
                if (diaper == null || !Helper_Diaper.allowedByPolicy(pawn, diaper) || need_diaper.CurLevel < 0.5f)
                {
                    var cloth = need_diaper.getCachedBestDiaperOrUndie();
                    __result = cloth != null && cloth.IsValid && cloth.HasThing;
                }
            }
        }
    }
    [HarmonyPatch(typeof(JobGiver_OptimizeApparel), "TryGiveJob")]
    public static class JobGiver_OptimizeApparel_TryGiveJob_Patch
    {
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            // Add your condition here to check if the pawn should not dress themselves
            if (!Helper_Regression.canChangeDiaperOrUnderwear(pawn))
            {
                JobFailReason.Is("Can't change their own diaper or underwear.");
                __result = null; // Prevent the job from being given
                return false;    // Skip the original method
            }
            return true; // Continue with the original method
        }
    }
    [HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
    public static class Pawn_JobTracker_StartJob_Patch
    {
        public static bool Prefix(Pawn_JobTracker __instance, Job newJob)
        {
            
            // Check if the job is Wear and if the pawn should not dress themselves
            if (newJob.def == RimWorld.JobDefOf.Wear)
            {
                Pawn pawn = (Pawn)AccessTools.Field(typeof(Pawn_JobTracker), "pawn").GetValue(__instance);
                if (Helper_Regression.canChangeDiaperOrUnderwear(pawn)) return true;
                JobFailReason.Is("Can't change their own diaper or underwear.");

                Messages.Message("This pawn can't change their own diaper or underwear.", pawn, MessageTypeDefOf.RejectInput, historical: false);
                return false; // Prevent the job from being started
            }
            if(newJob.def == DubDef.UseToilet)
            {
                Pawn pawn = (Pawn)AccessTools.Field(typeof(Pawn_JobTracker), "pawn").GetValue(__instance);
                var report = Helper_Regression.canUsePottyReport(pawn);
                if (!report.Accepted)
                {
                    JobFailReason.Is(report.Reason);
                    Messages.Message(report.Reason, pawn, MessageTypeDefOf.RejectInput, historical: false);
                    return false; // can't change their own diaper
                }

            }
            return true; // Continue with the original method
        }
    }

    /*public class Hediff_RegressionDamage : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
             Log.Message($"[ZI]Hit by {dinfo.Value}");
            base.PostAdd(dinfo);
            // Additional logic when this hediff is added, like notifications or initial setup
        }

        public override void Tick()
        {
            base.Tick();
            // Custom logic to be executed every tick if needed
        }

        public override void PostTick()
        {
            base.PostTick();
            // You can add logic here if you want something to happen at each tick.
        }

        public override string TipStringExtra
        {
            get
            {
                return $"Regression Level: {Severity * 100:F1}%";
            }
        }
    }*/
}


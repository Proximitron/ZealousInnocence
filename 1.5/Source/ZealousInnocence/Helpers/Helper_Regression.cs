﻿using DubsBadHygiene;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ZealousInnocence
{
    public static class Helper_Regression
    {
        private static Dictionary<Pawn, AgeStageInfo> cachedAgeStages = new Dictionary<Pawn, AgeStageInfo>();
        public static int getAgeStage(Pawn pawn, bool force = false)
        {
            if (!cachedAgeStages.TryGetValue(pawn, out var value) || force)
            {
                refreshAgeStageCache(pawn);
                cachedAgeStages.TryGetValue(pawn, out value);
            }

            return value.cachedAgeStage;
        }
        private static void refreshAgeStageCache(Pawn pawn)
        {
            Dictionary<Pawn, AgeStageInfo> dictionary = cachedAgeStages;
            AgeStageInfo obj = new AgeStageInfo
            {
                cachedAgeStage = getAgeStageInt(pawn),
                lastCheckTick = Find.TickManager.TicksGame
            };
            dictionary[pawn] = obj;
        }
        public static bool isAdult(Pawn pawn)
        {
            return getAgeStageInt(pawn) > 12;
        }
        public static int getAgeStageInt(Pawn pawn)
        {
            if (pawn == null)
            {
                return 14;
            }

            foreach (var curr in pawn.health.hediffSet.hediffs)
            {
                if (curr.def == HediffDefOf.RegressionState)
                {
                    // Regression isn't a thing on children
                    if (pawn.ageTracker.AgeBiologicalYears < 13)
                    {
                        pawn.health.RemoveHediff(curr);
                        return pawn.ageTracker.AgeBiologicalYears;
                    }
                    return curr.CurStageIndex * 3;
                }
            }
            return pawn.ageTracker.AgeBiologicalYears;
        }
        public static bool canChangeDiaperOrUnderwear(Pawn pawn)
        {
            if (!pawn.IsColonist) return true;
            return getAgeStageInt(pawn) >= 6;
        }
        public static AcceptanceReport canUsePottyReport(Pawn pawn)
        {
            var diaperNeed = pawn.needs.TryGetNeed<Need_Diaper>();
            if (diaperNeed == null) return true; // anything without that gets a free pass
            if (pawn.story.traits.HasTrait(TraitDefOf.Potty_Rebel))
            {
                return "ShortReasonNotComply".Translate(); // will never run to a potty!
            }

            // Mental states can make them unable to remember that
            MentalState mentalState = pawn.MentalState;
            if (mentalState != null && mentalState.def != null && (mentalState.def == MentalStateDefOf.PanicFlee || mentalState.def == MentalStateDefOf.Wander_Psychotic))
            {
                return "ShortReasonWrongMentalState".Translate(); // wrong mental state
            }

            var currDiapie = Helper_Diaper.getDiaper(pawn);
            if (currDiapie != null)
            {
                if (pawn.outfits.forcedHandler.IsForced(currDiapie))
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
        public static bool isChild(Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return false;
            return getAgeStage(pawn, forceRecheck) < 13;
        }
        private static void healPawnBrain(Pawn pawn)
        {
            for (int num = pawn.health.hediffSet.hediffs.Count - 1; num >= 0; num--)
            {
                var curr = pawn.health.hediffSet.hediffs[num];
                if (curr.Part?.def == RimWorld.BodyPartDefOf.Head)
                {
                    pawn.health.RemoveHediff(curr);
                }
            }

            /*Hediff hediff = HediffMaker.MakeHediff(RimWorld.HediffDefOf.ResurrectionSickness, innerPawn);
            if (!innerPawn.health.WouldDieAfterAddingHediff(hediff))
            {
                innerPawn.health.AddHediff(hediff);
            }*/

        }
        public static void regressPawn(Pawn pawn)
        {
            healPawnBrain(pawn);
            Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.RegressionState, pawn);
            if (!pawn.health.WouldDieAfterAddingHediff(hediff))
            {
                pawn.health.AddHediff(hediff);
            }
            refreshAgeStageCache(pawn);
            Messages.Message("MessagePawnRegressed".Translate(pawn), pawn, MessageTypeDefOf.CautionInput);
        }

        public static bool reincarnateToChildPawn(Pawn pawn, out List<Hediff> removedHediffs, out float pawnAgeDelta)
        {
            int oldAge = pawn.ageTracker.AgeBiologicalYears;
            removedHediffs = new List<Hediff>();
            float num = pawn.ageTracker.AgeBiologicalYears;
            pawnAgeDelta = 0;
            if (pawn.ageTracker.Adult)
            {
                num = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().targetChronoAge; // Mathf.Max(pawn.ageTracker.AgeBiologicalYearsFloat - years, LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().targetChronoAge);
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

                pawn.apparel.DropAll(pawn.Position, false);
            }

            if (pawn.ageTracker.AgeBiologicalYears < 13)
            {
                pawn.Notify_DisabledWorkTypesChanged();
            }

            healPawnBrain(pawn);
            List<HediffGiverSetDef> hediffGiverSets = pawn.RaceProps.hediffGiverSets;
            if (hediffGiverSets == null)
            {
                return false;
            }
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
            int num3 = Rand.RangeInclusive(1, 2);
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
            }
            HediffGiver_BedWetting bedWettingGiver = new HediffGiver_BedWetting();
            bedWettingGiver.OnIntervalPassed(pawn, null);
            refreshAgeStageCache(pawn);
            return false;
        }

        public static void dropAllGear(Pawn pawn)
        {
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            foreach (Apparel item in pawn.apparel.WornApparel)
            {
            }
            for (int num = wornApparel.Count - 1; num >= 0; num--)
            {
                Apparel resultingAp;
                pawn.apparel.TryDrop(wornApparel[num], out resultingAp, pawn.Position, forbid: false);
            }
        }
    }

    public class AgeStageInfo
    {
        public int cachedAgeStage;

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

            if (!targetMentalRegression)
            {
                var hediffsRemoved = new List<Hediff>();
                float ageDifference = 0f;
                Helper_Regression.reincarnateToChildPawn(pawn, out hediffsRemoved, out ageDifference);
            }
            else
            {
                Helper_Regression.regressPawn(pawn);
            }
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
                // Optionally, send a message to the player or log the action
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
}


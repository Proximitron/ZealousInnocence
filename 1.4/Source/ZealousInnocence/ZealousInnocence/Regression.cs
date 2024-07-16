using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ZealousInnocence
{
    public static class RegressionHelper
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
        private static int getAgeStageInt(Pawn pawn)
        {
            if (pawn == null)
            {
                return 10;
            }

            foreach(var curr in pawn.health.hediffSet.hediffs)
            {
                if(curr.def == HediffDefOf.RegressionState)
                {
                    return curr.CurStageIndex;
                }
            }
            return 10;
        }
        public static bool isAgeStage(Pawn pawn, int ageStage, bool force = false)
        {
            return getAgeStage(pawn, force) == ageStage;
        }
        private static void healPawnBrain(Pawn pawn)
        {
            for (int num = pawn.health.hediffSet.hediffs.Count - 1; num >= 0; num--)
            {
                var curr = pawn.health.hediffSet.hediffs[num];
                if (curr.Part?.def == RimWorld.BodyPartDefOf.Brain)
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

            RegressionHelper.regressPawn(pawn);
        }
    }
}

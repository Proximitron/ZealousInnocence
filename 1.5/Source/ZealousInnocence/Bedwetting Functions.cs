using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ZealousInnocence
{
    public class HediffGiver_BedWetting : HediffGiver
    {
        private int lastCheckTick = 0;
        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            if (!pawn.IsColonist) return;

            bool shouldWet = BedWetting_Helper.BedwettingAtAge(pawn, pawn.ageTracker.AgeBiologicalYears);
            //Log.Message($"ZealousInnocence bedwetting interval {Find.TickManager.TicksGame}: Pawn {pawn.LabelShort} {shouldWet}");
            var def = HediffDef.Named("BedWetting");

            if (shouldWet) { 
                if (!pawn.health.hediffSet.HasHediff(def)) {
                    pawn.health.AddHediff(def);
                    Messages.Message($"{pawn.Name.ToStringShort} has developed a bedwetting condition.", MessageTypeDefOf.NegativeEvent, true);
                }
                var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def);
                hediff.Severity = BedWetting_Helper.BedwettingSeverity(pawn);
            }
            else
            {
                if (pawn.health.hediffSet.HasHediff(def))
                {
                    pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(def));
                    Messages.Message($"{pawn.Name.ToStringShort} has outgrown their bedwetting condition.", MessageTypeDefOf.PositiveEvent, true);
                }
            }
        }
    }
    public static class BedWetting_Helper
    {
        public static float BedwettingSeverity(Pawn pawn)
        {
            if (pawn.ageTracker.AgeBiologicalYears <= 12)
            {
                return 0.1f; // Child stage
            }
            else if (pawn.ageTracker.AgeBiologicalYears <= 16)
            {
                return 0.4f; // Teen stage
            }
            else
            {
                return 0.7f; // Adult stage
            }
        }
        public static bool BedwettingAtAge(Pawn pawn, int age)
        {
            float chance;

            if (age <= 3)
            {
                chance = 1f; // 100% chance for ages 0 to 3
            }
            else if (age <= 8)
            {
                chance = Mathf.Lerp(0.7f, 0.25f, (age - 3) / 5f); // Gradually decrease from 0.7 to 0.25 between ages 3 and 8
            }
            else if (age <= 15)
            {
                chance = Mathf.Lerp(0.25f, 0.05f, (age - 8) / 7f); // Gradually decrease from 0.25 to 0.05 between ages 8 and 15
            }
            else if (age <= 65)
            {
                chance = 0.05f; // Constant 0.05 chance from age 15 to 65
            }
            else if (age <= 80)
            {
                chance = Mathf.Lerp(0.05f, 0.3f, (age - 65) / 15f); // Gradually increase from 0.05 to 0.3 from age 65 to 80
            }
            else
            {
                chance = 0.3f; // Constant 0.3 chance from age 81+
            }

            return Rand.ChanceSeeded(chance, pawn.thingIDNumber);
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) })]
    public static class PawnGenerator_GeneratePawn_Patch
    {
        public static void Postfix(Pawn __result, PawnGenerationRequest request)
        {
            var def = HediffDef.Named("BedWetting");

            if (BedWetting_Helper.BedwettingAtAge(__result, __result.ageTracker.AgeBiologicalYears))
            {
                __result.health.AddHediff(def);
                var hediff = __result.health.hediffSet.GetFirstHediffOfDef(def);
                hediff.Severity = BedWetting_Helper.BedwettingSeverity(__result);
            }
        }
    }
}

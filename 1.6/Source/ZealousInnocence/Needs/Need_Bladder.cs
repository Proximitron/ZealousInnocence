using DubsBadHygiene;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace ZealousInnocence
{
    public static class Need_Bladder_Patch
    {
        /*public static void BladderRate_Postfix(Need_Bladder __instance, ref float __result)
        {
            // Access the private pawn field
            Pawn pawn = (Pawn)AccessTools.Field(typeof(Need), "pawn").GetValue(__instance);
            // Add your custom logic here
            // For example, modify the bladder rate based on certain conditions
            float bladderStrength = pawn.GetStatValue(StatDefOf.BladderStrengh, true);
            var debugging = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var dbug = true; // debugging.debugging && debugging.debuggingCapacities;
            if (dbug) Log.Message($"BladderRate_Postfix: {pawn.Name} changing rate from {__result} on {bladderStrength}");
            if (bladderStrength != 1f)
            {
                float factor = 1f / bladderStrength;
               
                if (dbug) Log.Message($"BladderRate_Postfix: {pawn.Name} changing rate from {__result} to {__result*factor}");
                
                __result *= factor;
            }

        }*/
        public static bool NeedInterval_Prefix(Need_Bladder __instance)
        {
            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debugging = settings.debugging && settings.debuggingCapacities;
            if (__instance.IsFrozen)
            {
                return false;
            }

            Pawn pawn = (Pawn)AccessTools.Field(typeof(Need), "pawn").GetValue(__instance);
            float fallPerTick = (float)AccessTools.Property(typeof(Need_Bladder), "FallPerTick").GetValue(__instance);
            __instance.CurLevel -= fallPerTick * 150f * ModOption.BladderRateD.Val;
            if (__instance.CurLevel < 0f)
            {
                __instance.CurLevel = 0f;
            }

            if (__instance.CurCategory <= BowelCategory.needBathroom && pawn.CurrentBed() != null && pawn.health.capacities.CanBeAwake && (HealthAIUtility.ShouldSeekMedicalRest(pawn) || HealthAIUtility.ShouldSeekMedicalRestUrgent(pawn) || pawn.Downed || pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) < 0.3f || pawn.CurJob.playerForced))
            {
                if (settings.useBedPan && (settings.useBedPanIfDiaperEquipped || Helper_Diaper.getDiaper(pawn) == null))
                {
                    __instance.CurLevel = 1f;
                    Thing thing;
                    GenThing.TryDropAndSetForbidden(ThingMaker.MakeThing(DubDef.BedPan, null), pawn.Position, pawn.Map, ThingPlaceMode.Near, out thing, false);
                }

            }
            return false;
        }

        public static void CurCategory_Postfix(Need_Bladder __instance, ref BowelCategory __result)
        {

            // We only need to fix this if the bladder is very low and the pawn doesn't notice
            if (__instance.CurLevel < 0.15f)
            {
                // Access the private pawn field
                Pawn pawn = (Pawn)AccessTools.Field(typeof(Need), "pawn").GetValue(__instance);
                if (!Helper_Diaper.remembersPotty(pawn)) __result = BowelCategory.Empty;
            }
            // Add your custom logic here
            /*if (pawn.story.traits.HasTrait(TraitDefOf.AnotherTrait))
            {
                if (__result == BowelCategory.needBathroom && __instance.CurLevel < 0.1f)
                {
                    __result = BowelCategory.Busting; // Make the pawn more likely to feel urgent if they have another trait
                }
            }*/
        }
    }
}

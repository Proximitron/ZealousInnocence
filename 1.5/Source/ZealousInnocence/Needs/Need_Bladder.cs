using DubsBadHygiene;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

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

        public static void CurCategory_Postfix(Need_Bladder __instance, ref BowelCategory __result)
        {

            // We only need to fix this if the bladder is very low and the pawn doesn't notice
            if (__instance.CurLevel < 0.15f)
            {
                // Access the private pawn field
                Pawn pawn = (Pawn)AccessTools.Field(typeof(Need), "pawn").GetValue(__instance);
                if (!DiaperHelper.remembersPotty(pawn)) __result = BowelCategory.Empty;
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

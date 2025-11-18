using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace ZealousInnocence.ToddlersMod
{
    /*[HarmonyPatch(typeof(JobGiver_OptimizeBabyApparel), "TryGiveJob")]*/
    public static class Patch_JobGiver_OptimizeBabyApparel_TryGiveJob
    {
        public static void Postfix(ref Job __result)
        {
            // If original jobgiver found no job, do nothing.
            if (__result == null)
                return;

            // We care about jobs that involve apparel:
            //  - Strip jobs:  targetB = worn apparel
            //  - Dress/force-wear jobs: targetB = apparel on ground
            Apparel apparel = __result.targetB.Thing as Apparel;
            if (apparel == null)
                return;

            // If this apparel should NOT be optimized by that jobgiver,
            // then cancel the job so your own logic can take over.
            if (Helper_Diaper.isDiaper(apparel) || Helper_Diaper.isNightDiaper(apparel))
            {
                __result = null;
                //Log.Message($"[ZI] Job cancled for {apparel.LabelShort}");
            }
            else
            {
                //Log.Message($"[ZI] Job allowed for {apparel.LabelShort} ");
            }
        }
    }
}

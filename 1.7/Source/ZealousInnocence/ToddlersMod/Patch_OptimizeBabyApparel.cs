using HarmonyLib;
using Ionic.Zlib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace ZealousInnocence.ToddlersMod
{
    /*[HarmonyPatch(typeof(JobGiver_OptimizeBabyApparel), "TryGiveJob")]*/
    public static class Patch_JobGiver_OptimizeBabyApparel_TryGiveJob
    {
        // Fetch the private static field "wornApparelScores".
        private static readonly FieldInfo ScoresField = AccessTools.Field(AccessTools.TypeByName("Toddlers.JobGiver_OptimizeBabyApparel"), "wornApparelScores");
        public static void Postfix(ref Job __result, Pawn hauler)
        {
            // If original jobgiver found no job, do nothing.
            if (__result == null)
                return;

            // We care about jobs that involve apparel:
            //  - Strip jobs:  targetB = worn apparel
            //  - Dress/force-wear jobs: targetB = apparel on ground
            Apparel apparelFound = __result.targetB.Thing as Apparel;
            if (apparelFound == null)
                return;

            Pawn baby = __result.targetA.Thing as Pawn;
            if (baby == null)
                return;

            // If this apparel should NOT be optimized by that jobgiver,
            // then cancel the job so your own logic can take over.
            if (Helper_Diaper.isDiaper(apparelFound))
            {
                __result = null;
            }
            else return;

            var wornApparelScores = ScoresField.GetValue(null) as List<float>;
            if (wornApparelScores == null)
            {
                Helper_OptimizeApparel.SetNextOptimizeTick(baby);
                Log.ErrorOnce(
                    "Patch_JobGiver_OptimizeBabyApparel_TryGiveJob error, 'wornApparelScores' is not accessable. Toddlers probably changed something major!",
                    "Patch_JobGiver_OptimizeBabyApparel_TryGiveJob_wornApparelScores".GetHashCode()
                );
                return;
            }


            Thing thing = Helper_OptimizeApparel.bestBetterApparel(baby, hauler,wornApparelScores);
            if (thing == null)
            {
                Helper_OptimizeApparel.SetNextOptimizeTick(baby);
                return;
            }

            JobDef forceTargetWear = DefDatabase<JobDef>.GetNamedSilentFail("ForceTargetWear");
            if (forceTargetWear == null)
            {
                Helper_OptimizeApparel.SetNextOptimizeTick(baby);
                Log.ErrorOnce(
                    "Patch_JobGiver_OptimizeBabyApparel_TryGiveJob error, 'forceTargetWear' is no valid JobDef. Toddlers probably changed something major!",
                    "Patch_JobGiver_OptimizeBabyApparel_TryGiveJob_forceTargetWear".GetHashCode()
                );
                return; // mod not loaded or def missing
            }
                

            __result = JobMaker.MakeJob(forceTargetWear, baby, thing);
        }
    }
}

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
        private static void SetNextOptimizeTick(Pawn pawn) => typeof(JobGiver_OptimizeApparel).GetMethod("SetNextOptimizeTick", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { pawn });
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

            ApparelPolicy curApparelPolicy = baby.outfits.CurrentApparelPolicy;


            Thing thing = null;
            float num2 = 0f;
            List<Thing> list = hauler.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel);
            var wornApparelScores = ScoresField.GetValue(null) as List<float>;
            if (wornApparelScores == null)
            {
                SetNextOptimizeTick(baby);
                Log.ErrorOnce(
                    "Patch_JobGiver_OptimizeBabyApparel_TryGiveJob error, 'wornApparelScores' is not accessable. Toddlers probably changed something major!",
                    "Patch_JobGiver_OptimizeBabyApparel_TryGiveJob_wornApparelScores".GetHashCode()
                );
                return;
            }
                

            for (int j = 0; j < list.Count; j++)
            {
                Apparel apparel = (Apparel)list[j];
                //Log.Message("Contemplating apparel: " + apparel.ToString());
                //Log.Message("currentOutfit.filter.Allows(apparel): "+ currentOutfit.filter.Allows(apparel));
                //Log.Message("apparel.IsInAnyStorage(): " + apparel.IsInAnyStorage());
                //Log.Message("apparel.IsForbidden(hauler): " + apparel.IsForbidden(hauler));
                //Log.Message("apparel.IsForbidden(baby): " + apparel.IsForbidden(baby));
                if (curApparelPolicy.filter.Allows(apparel)
                    && apparel.IsInAnyStorage()
                    && !apparel.IsForbidden(hauler) && !apparel.IsForbidden(baby)
                    && !apparel.IsBurning()
                    && !Helper_Diaper.isDiaper(apparel))
                {
                    float num3 = JobGiver_OptimizeApparel.ApparelScoreGain(baby, apparel, wornApparelScores);

                    if (!(num3 < 0.05f) && !(num3 < num2)
                        && (!CompBiocodable.IsBiocoded(apparel) || CompBiocodable.IsBiocodedFor(apparel, baby))
                        && ApparelUtility.HasPartsToWear(baby, apparel.def)
                        && hauler.CanReserveAndReach(apparel, PathEndMode.OnCell, hauler.NormalMaxDanger())
                        && hauler.CanReserveAndReach(baby, PathEndMode.OnCell, hauler.NormalMaxDanger())
                        && apparel.def.apparel.developmentalStageFilter.Has(baby.DevelopmentalStage))
                    {
                        //Log.Message("picked " + apparel.ToString() + "as an option");
                        thing = apparel;
                        num2 = num3;
                    }
                }
            }
            if (thing == null)
            {
                SetNextOptimizeTick(baby);
                return;
            }

            JobDef forceTargetWear = DefDatabase<JobDef>.GetNamedSilentFail("ForceTargetWear");
            if (forceTargetWear == null)
            {
                SetNextOptimizeTick(baby);
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

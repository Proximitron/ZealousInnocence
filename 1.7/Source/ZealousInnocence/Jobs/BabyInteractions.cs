using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using static UnityEngine.GridBrushBase;

namespace ZealousInnocence
{
    public static class Patch_BottleFeedBaby_HandleDispenser
    {
        public static bool Prefix(JobDriver_BottleFeedBaby __instance, ref IEnumerable<Toil> __result)
        {
            var thingB = __instance.job.targetB.Thing;
            if (thingB is Building_NutrientPasteDispenser disp)
            {
                var pawn = __instance.pawn;
                // Basic sanity checks like vanilla ingest
                if (disp == null || pawn == null || !disp.CanDispenseNow)
                {
                    __result = EndAsIncompletable();
                    return false; // don't build original toils (prevents spam)
                }
                var meal = disp.TryDispenseFood();
                // Try to dispense one meal in a version-agnostic way
                if (meal == null || meal.Destroyed)
                {
                    __result = EndAsIncompletable();
                    return false;
                }

                // Prefer putting the meal in inventory (then the original "jump" path succeeds)
                if (!pawn.inventory.innerContainer.TryAdd(meal, canMergeWithExistingStacks: true))
                {
                    // If inventory is full, drop it near the pawn
                    if (!meal.Spawned)
                        GenPlace.TryPlaceThing(meal, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                }

                // Rewrite B to the actual ingestible stack so the vanilla toils work
                __instance.job.SetTarget(TargetIndex.B, meal);

                // Continue to the original MakeNewToils (now B is a proper stack)
                return true;
            }


            return true; // normal path
        }
        private static IEnumerable<Toil> EndAsIncompletable()
        {
            Toil toil = ToilMaker.MakeToil("GeneralFail");
            toil.FailOn(() => true);
            yield return toil;
        }

    }

    public static class Patch_ChildcareUtility_CanSuckle
    {
        public static void Postfix(ref bool __result, Pawn baby, ref ChildcareUtility.BreastfeedFailReason? reason)
        {
            if (baby != null && Helper_Regression.ShouldHavePlaying(baby) && reason == ChildcareUtility.BreastfeedFailReason.BabyTooOld)
            {
                reason = null;
                __result = true;
            }
        }

    }
    public static class Patch_WorkGiver_PlayWithBaby_HasJobOnThing
    {
        public static void Postfix(ref bool __result, Pawn pawn, Thing t, bool forced = false)
        {
            if (__result == true) return;
            Pawn pawn2 = t as Pawn;
            if (pawn2 == null)
            {
                __result = false;
                return;
            }
            if (!Helper_Regression.ShouldHavePlaying(pawn2))
            {
                __result = false;
                return;
            }
            if (pawn2.needs.play == null)
            {
                __result = false;
                return;
            }
            if (forced)
            {
                if (pawn2.needs.play.CurLevelPercentage >= 0.95f)
                {
                    JobFailReason.Is("CannotInteractBabyPlayFull".Translate(), null);
                    __result = false;
                    return;
                }
            }
            else
            {
                if (!pawn2.Awake())
                {
                    __result = false;
                    return;
                }
                if (!pawn2.needs.play.IsLow)
                {
                    __result = false;
                    return;
                }
            }
            using (IEnumerator<BabyPlayDef> enumerator = DefDatabase<BabyPlayDef>.AllDefs.InRandomOrder(null).GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.Worker.CanDo(pawn, pawn2))
                    {

                        __result = true;
                        return;
                    }
                }
            }
            __result = false;
        }
    }
}

using DubsBadHygiene;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;
using Verse;
using Verse.AI;
using static UnityEngine.GraphicsBuffer;

namespace ZealousInnocence
{
    //if (errorOnFailed)
    //this.LogCouldNotReserveError(claimant, job, target, maxPawns, stackCount, layer);

    // Try Romance is age gated (years) but existing love partners are possible and denying this call will resolve all issues related to it
    public static class Patch_LovePartnerRelationUtility_ExistingLovePartner
    {
        public static bool Prefix(Pawn pawn, bool allowDead, ref Pawn __result)
        {
            if (pawn != null && !pawn.isAdultInEveryWay())
            {
                __result = null;
                return false;
            }
            return true;
        }
    }
    public static class Patch_JobGiver_MarryAdjacentPawn_CanMarry
    {
        public static bool Prefix(Pawn pawn, Pawn toMarry, ref bool __result)
        {
            if (!pawn.isAdultInEveryWay() || !toMarry.isAdultInEveryWay())
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
    public static class Patch_InteractionWorker_RomanceAttempt_SuccessChance
    {
        public static bool Prefix(Pawn initiator, Pawn recipient, float baseChance, ref float __result)
        {
            if (!initiator.isAdultInEveryWay() || !recipient.isAdultInEveryWay())
            {
                __result = 0f;
                return false;
            }
            return true;
        }
    }
    public static class Patch_BlockQuestionableInteractions
    {
        public static bool Prefix(object __0, ref object __result)
        {
            if (__0 is IJobEndable endable)
            {
                endable.AddEndCondition(() =>
                {
                    var actor = endable.GetActor();
                    if (actor == null) return JobCondition.Incompletable;

                    if (actor.isAdultInEveryWay() || PawnUtility.WillSoonHaveBasicNeed(actor, -0.05f))
                        return JobCondition.Incompletable;

                    return JobCondition.Ongoing;
                });

                // return the same T instance
                __result = __0;
                return false; // skip original
            }

            // If somehow not the expected type, let original run
            return true;
        }
    }
    [HarmonyPatch(typeof(Need_Learning), nameof(Need_Learning.Suspended), MethodType.Getter)]
    static class Need_Learning_IsFrozen
    {
        public static void Postfix(ref bool __result, ref Pawn ___pawn, ref Need_Learning __instance)
        {
            if (___pawn == null || !___pawn.RaceProps.Humanlike) return;
            if (___pawn.Deathresting) return;

            if (!___pawn.ShouldHaveLearning())
            {
                __instance.CurLevel = 0.5f;
                __result = true;
            }
        }
    }

    public static class Patch_FailOnChildLearningConditions
    {
        public static bool Prefix(object __0, ref object __result)
        {
            if (__0 is IJobEndable endable)
            {
                endable.AddEndCondition(() =>
                {
                    var actor = endable.GetActor();
                    if (actor == null) return JobCondition.Incompletable;

                    // !actor.ShouldHaveLearning() // We simply let it run if it came to it
                    if (PawnUtility.WillSoonHaveBasicNeed(actor, -0.05f))
                        return JobCondition.Incompletable;

                    return JobCondition.Ongoing;
                });


                __result = __0;
                return false; // skip original
            }

            // If somehow not the expected type, let original run
            return true;
        }
    }
    public static class Patch_ThinkNode_Priority_Learn
    {
        private static bool PassesOtherGates(Pawn pawn)
        {
            if (!ModsConfig.BiotechActive) return false;

            var tt = pawn.timetable;
            var assignment = (tt != null ? tt.CurrentAssignment : null) ?? TimeAssignmentDefOf.Anything;
            if (!assignment.allowJoy) return false;

            if (pawn.learning == null) return false;
            if (Find.TickManager.TicksGame < 5000) return false;
            if (LearningUtility.LearningSatisfied(pawn)) return false;

            return true;
        }

        public static void Postfix(Pawn pawn, ref float __result)
        {
            // If vanilla already gave a priority, leave it.
            if (__result > 0f) return;

            // In case all of the gates are passed and learning should be active
            if (PassesOtherGates(pawn) && pawn.ShouldHaveLearning())
            {
                __result = 9.1f; // vanilla value
            }
        }
    }
    public static class Patch_LearningGiver_CanDo
    {
        public static bool Prefix(Pawn pawn, ref bool __result)
        {
            if (!ModsConfig.BiotechActive || pawn == null)
            {
                __result = false;
                return false;
            }

            bool inLearnableState =
                !pawn.Downed &&
                !PawnUtility.WillSoonHaveBasicNeed(pawn, 0.1f) &&
                pawn.needs?.learning != null &&
                pawn.needs.learning.CurLevel < 0.9f;

            __result = inLearnableState && pawn.ShouldHaveLearning();
            return false; // skip original
        }
    }

    public static class Patch_Pawn_GetGizmos
    {
        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            // Quick guardrails
            if (__instance == null || __instance.DestroyedOrNull()) return;

            // Only add if criteria say so AND vanilla didn’t already add it
            if (!ShouldShow(__instance)) return;

            // Materialize once to avoid re-enumeration surprises
            var list = __result as List<Gizmo> ?? __result?.ToList() ?? new List<Gizmo>();

            bool alreadyHas = list.Any(g => g is Gizmo_GrowthTier);
            if (!alreadyHas)
            {
                // Vanilla gizmo class; construct with pawn
                list.Add(new Gizmo_GrowthTier(__instance));
            }

            __result = list;
        }

        private static bool ShouldShow(Pawn p)
        {
            if (p.Drafted) return false;
            if (Find.Selector?.SelectedPawns?.Count >= 2) return false;
            if (p.RaceProps == null || !p.RaceProps.Humanlike) return false;

            return p.needs?.learning != null;
        }
    }

    public static class Patch_Learning_ShowOnNeedList
    {
        public static void Postfix(Need __instance, ref bool __result)
        {
            if (__instance is Need_Learning c)
            {
                var p = AccessTools.FieldRefAccess<Need_Learning, Pawn>("pawn")(c);
                if (p?.needs?.learning != null)
                {
                    // If your pawn actually has a learning need, ensure it's visible
                    __result = true;
                }
            }
        }
    }

    public static class Patch_NeedLearning_DrawOnGUI
    {
        static ZealousInnocenceSettings settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();

        public static void Prefix(
            Need_Learning __instance,
            Rect rect, int maxThresholdMarkers, float customMargin, bool drawArrows, bool doTooltip, Rect? rectForTooltip, bool drawLabel)
        {

            var pawn = AccessTools.FieldRefAccess<Need_Learning, Pawn>("pawn")(__instance);
            if (pawn == null || pawn.Dead || pawn.RaceProps == null || !pawn.RaceProps.Humanlike) return;

            if(pawn.needs.learning == null)
            {
                if (settings.debugging) Log.Message($"[ZI]NeedLearning_DrawOnGUI: Learning need is null?");
                return;
            }

            if (pawn.learning == null)
            {
                if (settings.debugging) Log.Message($"[ZI]NeedLearning_DrawOnGUI: Emergency patching learning");
                pawn.learning = new Pawn_LearningTracker(pawn);
            }
        }
    }


 }

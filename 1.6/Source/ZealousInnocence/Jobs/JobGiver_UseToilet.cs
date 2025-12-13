using DubsBadHygiene;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ZealousInnocence
{
    public static class JobGiver_UseToilet_TryGiveJob_Patch
    {
        // Prefix to save runs in unnessesary cases. It tracks if the pawn notices 
        public static bool Prefix(JobGiver_UseToilet __instance, Pawn pawn)
        {
            return Helper_Diaper.remembersPotty(pawn);
        }
        // Postfix to observe or modify the output of TryGiveJob
        public static void Postfix(JobGiver_UseToilet __instance, Pawn pawn, ref Job __result)
        {
            if (__result != null && pawn.RaceProps.Humanlike)
            {
                var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
                var debugging = settings.debugging && settings.debuggingJobs;
                var debugBedwetting = debugging || (settings.debugging && settings.debuggingBedwetting && !pawn.Awake());
                if (pawn.Awake())
                {
                    var liked = Helper_Diaper.getDiaperPreference(pawn);
                    if (liked == DiaperLikeCategory.Liked)
                    {
                        var currDiapie = Helper_Diaper.getDiaper(pawn);
                        if (currDiapie == null)
                        {
                            pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("HadToGoPotty"), null, null);
                        }
                        else
                        {
                            if (debugging)  Log.Message($"[ZI]JobGiver_UseToilet postfix null for {pawn.Name.ToStringShort}");
                            __result = null;
                            return;
                        }
                    }
                    else if (liked == DiaperLikeCategory.Diaper_Lover)
                    {
                        var currDiapie = Helper_Diaper.getDiaper(pawn);
                        if (currDiapie == null)
                        {
                            pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("HadToUseToilet"), null, null);
                        }
                        else
                        {
                            if (debugging)  Log.Message($"[ZI]JobGiver_UseToilet postfix null for {pawn.Name.ToStringShort}");
                            __result = null;
                            return;
                        }
                    }
                }
                else
                {
                    if (debugging || debugBedwetting)  Log.Message($"[ZI]JobGiver_UseToilet not awake for {pawn.Name.ToStringShort}");
                    JobFailReason.Is("Not awake.");
                    __result = null;
                    return;
                }
                var diaperNeed = pawn.needs.TryGetNeed<Need_Diaper>();
                if (diaperNeed != null) diaperNeed.FailureSeed = 0; // resetting seed

                if(debugging) Log.Message($"[ZI]JobGiver_UseToilet attempting to assign a job to {pawn.Name.ToStringShort}");
            }
        }
    }
    [HarmonyPatch(typeof(Building_AssignableFixture), nameof(Building_AssignableFixture.PawnAllowed))]
    public static class Building_AssignableFixture_PawnAllowed_Patch
    {
        // Prefix to save runs in unnessesary cases. It tracks if the pawn notices 
        public static bool Prefix(Building_AssignableFixture __instance, Pawn p, ref AcceptanceReport __result)
        {
            var report = Helper_Regression.canUsePottyReport(p);
            if (!report.Accepted)
            {
                __result = report;
                return false;
            }
            return true;
        }

    }

    public class ThinkNode_ConditionalVisitorLike : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn?.RaceProps?.Humanlike != true) return false;

            // Exclude player colonists and prisoners; allow guests/visitors/neutral/caravan pawns
            if (pawn.IsColonist) return false;
            if (pawn.IsPrisoner) return false;

            // Optional: exclude hostiles/raiders, keep visitors only
            //if (pawn.HostileTo(Faction.OfPlayer)) return false;

            return true;
        }
    }

    public class JobGiver_UseToilet : ThinkNode_JobGiver
    {
        public virtual bool UrgentForLord => false;

        private static MethodInfo _miFindBestToilet;
        private static bool _initTried;

        private bool debug =>
            LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>()?.debuggingCapacities == true;

        private static bool TryInitReflection()
        {
            if (_initTried) return _miFindBestToilet != null;
            _initTried = true;

            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name.Contains("DubsBadHygiene", StringComparison.OrdinalIgnoreCase));
                if (asm == null) return false;

                var type = asm.GetTypes().FirstOrDefault(t => t.Name == "ClosestSanitation");
                if (type == null) return false;

                _miFindBestToilet = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m => m.Name == "FindBestToilet");

                return _miFindBestToilet != null;
            }
            catch (Exception e)
            {
                Log.Warning($"[ZI] Reflection init for ClosestSanitation.FindBestToilet failed: {e}");
                return false;
            }
        }

        private static LocalTargetInfo Call_FindBestToilet(Pawn pawn, bool urgent, float shortRange)
        {
            if (!TryInitReflection()) return LocalTargetInfo.Invalid;

            try
            {
                var result = _miFindBestToilet.Invoke(null, new object[] { pawn, urgent, shortRange });
                return result is LocalTargetInfo info ? info : LocalTargetInfo.Invalid;
            }
            catch (Exception e)
            {
                Log.Warning($"[ZI] Failed to call FindBestToilet via reflection: {e}");
                return LocalTargetInfo.Invalid;
            }
        }

        public override float GetPriority(Pawn pawn)
        {
            var need_Bladder = pawn.needs?.TryGetNeed<Need_Bladder>();
            if (need_Bladder == null) return 0f;

            if (FoodUtility.ShouldBeFedBySomeone(pawn)) return 0f;

            Need_Rest need_Rest = pawn.needs?.TryGetNeed<Need_Rest>();
            float threshold = 0.3f;
            if (pawn.mindState.IsIdle) threshold = 0.5f;
            if (need_Rest != null && need_Rest.CurLevel > 0.95f) threshold = 0.6f;
            if (UrgentForLord) threshold = 0.01f;

            if (need_Bladder.CurLevel < threshold)
            {
                if (debug) Log.Message($"[ZI/JobGiver] {pawn.LabelShort} priority triggered; Cur={need_Bladder.CurLevel:F2} < {threshold:F2}");
                return 9.6f;
            }

            return 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (GetPriority(pawn) == 0f) return null;

            Need_Bladder bladder = pawn.needs?.TryGetNeed<Need_Bladder>();
            if (bladder == null)
            {
                if (debug) Log.Message($"[ZI/JobGiver] {pawn.LabelShort} has no Need_Bladder; aborting.");
                return null;
            }

            float shortRange = 20f;
            if (bladder.CurLevel <= 0.3f) shortRange = 30f;
            if (bladder.CurLevel <= 0.2f) shortRange = 40f;
            if (bladder.CurLevel <= 0.1f) shortRange = 9999f;

            LocomotionUrgency urgency = LocomotionUrgency.Jog;
            bool urgent = false;
            if (bladder.CurLevel <= 0f)
            {
                urgent = true;
                urgency = LocomotionUrgency.Sprint;
            }

            // Animals
            if (pawn.RaceProps.Animal)
            {
                if (pawn.Faction == Faction.OfPlayer)
                {
                    Thing thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                        ThingRequest.ForDef(DubDef.LitterBox),
                        PathEndMode.OnCell, TraverseParms.For(pawn), 30f,
                        x => pawn.CanReserve(x) && !x.IsForbidden(pawn));

                    if (thing != null)
                    {
                        if (debug) Log.Message($"[ZI/JobGiver] {pawn.LabelShort} found litter box {thing.LabelCap}");
                        return JobMaker.MakeJob(DubDef.UseToilet, thing);
                    }
                }

                IntVec3 poopCell = SanitationUtil.TryFindPoopCell(pawn);
                if (poopCell.IsValid)
                {
                    if (debug) Log.Message($"[ZI/JobGiver] {pawn.LabelShort} using wild poo cell {poopCell}");
                    return JobMaker.MakeJob(DubDef.haveWildPoo, poopCell);
                }

                if (debug) Log.Message($"[ZI/JobGiver] {pawn.LabelShort} no animal toilet found.");
                return null;
            }

            // Humanlikes
            LocalTargetInfo toiletTarget = Call_FindBestToilet(pawn, urgent, shortRange);
            if (toiletTarget.IsValid && toiletTarget.HasThing)
            {
                if (debug) Log.Message($"[ZI/JobGiver] {pawn.LabelShort} assigned UseToilet -> {toiletTarget.Thing.LabelCap} at {toiletTarget.Cell}");
                Job job = JobMaker.MakeJob(DubDef.UseToilet, toiletTarget);
                job.locomotionUrgency = urgency;
                return job;
            }

            if (bladder.GivingUpAndShittingOnTheFloorFunction())
            {
                IntVec3 fallback = SanitationUtil.TryFindPoopCell(pawn);
                if (fallback.IsValid)
                {
                    if (debug) Log.Message($"[ZI/JobGiver] {pawn.LabelShort} fallback wild poo at {fallback}");
                    Job job2 = JobMaker.MakeJob(DubDef.haveWildPoo, fallback);
                    job2.locomotionUrgency = urgency;
                    return job2;
                }
            }

            if (debug) Log.Message($"[ZI/JobGiver] {pawn.LabelShort} found no valid toilet target.");
            return null;
        }
    }

}

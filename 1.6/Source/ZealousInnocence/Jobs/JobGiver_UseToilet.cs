﻿using DubsBadHygiene;
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

    public class JobDriver_UseToilet : JobDriver
    {
        private bool debug =>
            LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>()?.debuggingCapacities == true;

        private Building TargetToilet => this.job.targetA.Thing as Building;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (debug) Log.Message($"[ZI/Toilet] TryMakePreToilReservations: pawn={pawn?.LabelShort} target={TargetToilet?.LabelCap} at {TargetToilet?.Position}");

            var ok = this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null, errorOnFailed, false);
            if (debug) Log.Message($"[ZI/Toilet] Reserve(targetA) -> {ok}");
            return ok;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            if (TargetToilet == null)
            {
                if (debug) Log.Message("[ZI/Toilet] ABORT: targetA is not a Building.");
                yield break;
            }

            // Standard null/forbidden fail
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);

            // Extra trace: reachability and interaction cell
            var ic = TargetToilet.InteractionCell;
            if (debug)
            {
                Log.Message($"[ZI/Toilet] Target={TargetToilet.LabelCap} def={TargetToilet.def?.defName} pos={TargetToilet.Position} ic={ic} rot={TargetToilet.Rotation}");
                Log.Message($"[ZI/Toilet] CanReach(ToiletCell)={pawn.CanReach(TargetToilet, PathEndMode.Touch, Danger.Deadly)} CanReach(ICell)={pawn.CanReach(ic, PathEndMode.OnCell, Danger.Deadly)}");
            }

            // Early FailOn "Working" state — trace it
            yield return new Toil().FailOn(() =>
            {
                var assignable = TargetToilet as Building_AssignableFixture;
                var accepted = assignable != null && assignable.Working(0f).Accepted;
                if (debug) Log.Message($"[ZI/Toilet] FailOn Working(0) check -> accepted={accepted}");
                return assignable != null && !accepted;
            });

            // Reserve (trace outcome)
            var reserveToil = Toils_Reserve.Reserve(TargetIndex.A, 1, -1, null);
            reserveToil.initAction = () =>
            {
                if (debug) Log.Message($"[ZI/Toilet] Toils_Reserve.Reserve init for {TargetToilet.LabelCap}");
            };
            yield return reserveToil;

            // ✅ RECOMMENDED FIX: go to the *interaction cell*, not OnCell
            // If you want to keep your exact code for now, comment the next line
            // and un-comment the original GotoCell (but expect failures on impassables).
            var gotoToil = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            // ---- original (likely wrong) ----
            // var gotoToil = Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

            gotoToil.initAction = () =>
            {
                if (debug) Log.Message($"[ZI/Toilet] Goto init. ic={TargetToilet.InteractionCell}, onCell={TargetToilet.Position}");
            };
            yield return gotoToil;

            // Main toil
            Toil toil = new Toil
            {
                initAction = delegate
                {
                    if (debug) Log.Message("[ZI/Toilet] initAction start");
                    var terlet = TargetToilet as Building_toilet;
                    if (terlet != null) terlet.seatUp = true;

                    base.Map.mapDrawer.MapMeshDirty(TargetToilet.Position, MapMeshFlagDefOf.Things);
                    PrivacyUtil.ToiletPrivacyLOS(this.pawn, 7f);
                    SanitationUtil.ApplyBathroomThought(this.pawn, TargetToilet);

                    if (debug) Log.Message("[ZI/Toilet] initAction done");
                },
                tickAction = delegate
                {
                    // Face the toilet; if rotation math is odd, at least trace it
                    var faceCell = this.job.targetA.Cell - TargetToilet.Rotation.FacingCell;
                    this.pawn.rotationTracker.FaceCell(faceCell);
                    this.pawn.GainComfortFromCellIfPossible(1, false);

                    var bladder = this.pawn.needs?.TryGetNeed<Need_Bladder>();
                    if (bladder == null)
                    {
                        // Don’t explode if caravans don’t have the need
                        if (debug) Log.Message("[ZI/Toilet] tickAction: No Need_Bladder present -> ending job as Incompletable.");
                        this.EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    // Do the thing
                    bladder.dump();
                },
                defaultDuration = 900,
                defaultCompleteMode = ToilCompleteMode.Delay,
                socialMode = RandomSocialMode.Quiet,
                handlingFacing = true
            };

            // Progress/end condition — guard null
            toil.AddEndCondition(() =>
            {
                var b = this.pawn.needs?.TryGetNeed<Need_Bladder>();
                if (b == null)
                {
                    if (debug) Log.Message("[ZI/Toilet] EndCondition: no bladder need -> Incompletable");
                    return JobCondition.Incompletable;
                }

                // Continue until bladder >= 1 (empty)
                var cond = b.CurLevel < 1f ? JobCondition.Ongoing : JobCondition.Succeeded;
                if (debug) Log.Message($"[ZI/Toilet] EndCondition: Cur={b.CurLevel:F3} -> {cond}");
                return cond;
            });

            toil.AddFinishAction(delegate
            {
                if (debug) Log.Message("[ZI/Toilet] FinishAction start");

                var baseToilet = TargetToilet as bibblefuckwit;
                var toilet = TargetToilet as Building_toilet;

                baseToilet?.TryUseFlush();

                if (toilet != null && Rand.Chance(0.7f)) toilet.seatUp = false;

                if (!(TargetToilet is Building_AdvToilet) &&
                    ((this.pawn.gender == Gender.Male && Rand.Chance(0.05f)) ||
                     (this.pawn.gender == Gender.Female && Rand.Chance(0.03f)) ||
                     (this.pawn.health.hediffSet.HasHediff(RimWorld.HediffDefOf.AlcoholHigh, false) && Rand.Chance(0.15f))))
                {
                    FilthMaker.TryMakeFilth(TargetToilet.Position, base.Map, DubDef.FilthUrine, 1, FilthSourceFlags.None, true);
                }

                SanitationUtil.CheckForBlockage(TargetToilet);
                PrivacyUtil.ToiletPrivacyLOS(this.pawn, 7f);
                SanitationUtil.ContaminationFromCellForPawn(this.pawn, this.pawn.Position);

                // Wash hands follow-up (guard nulls + trace)
                var hygiene = this.pawn.needs?.TryGetNeed<Need_Hygiene>();
                if (hygiene != null)
                {
                    var room = this.pawn.GetRoom(RegionType.Set_All);
                    LocalTargetInfo basin = default;

                    if (room != null)
                    {
                        basin = room.ContainedAndAdjacentThings.FirstOrDefault(x =>
                        {
                            var fixture = x as Building_AssignableFixture;
                            return fixture != null && fixture.fixture == FixtureType.Basin;
                        });
                    }

                    if (!basin.IsValid)
                    {
                        if (debug) Log.Message("[ZI/Toilet] No basin in room; trying clean water source within 10 cells");
                        basin = DbH_ClosestSanitationBridge.FindBestCleanWaterSource(this.pawn, this.pawn, false, 10f, null, null);
                    }

                    if (basin.Thing != null)
                    {
                        var newJob = JobMaker.MakeJob(DubDef.washHands, basin);
                        if (newJob.TryMakePreToilReservations(this.pawn, false))
                        {
                            this.pawn.jobs.jobQueue.EnqueueLast(newJob, null);
                            if (debug) Log.Message($"[ZI/Toilet] Queued washHands at {basin.Thing.LabelCap} ({basin.Thing.Position})");
                        }
                        else if (debug) Log.Message("[ZI/Toilet] Failed to reserve washHands target");
                    }
                    else if (debug) Log.Message("[ZI/Toilet] No washHands target found");
                }
                else if (debug) Log.Message("[ZI/Toilet] Pawn has no Need_Hygiene; skipping washHands");

                if (debug) Log.Message("[ZI/Toilet] FinishAction done");
            });

            yield return toil;
        }
        public static class DbH_ClosestSanitationBridge
        {
            private static MethodInfo _miFindBestCleanWater;
            private static bool _initTried;
            private const string TypeName = "ClosestSanitation";       // DBH internal class name
            private const string MethodName = "FindBestCleanWaterSource";

            private static bool TryInit()
            {
                if (_initTried) return _miFindBestCleanWater != null;
                _initTried = true;

                try
                {
                    // 1) Find the DBH assembly by name heuristic
                    var asm = AppDomain.CurrentDomain
                        .GetAssemblies()
                        .FirstOrDefault(a =>
                        {
                            var n = a.GetName().Name;
                            return n.Contains("DubsBadHygiene", StringComparison.OrdinalIgnoreCase)
                                   || n.Contains("DBH", StringComparison.OrdinalIgnoreCase);
                        });

                    if (asm == null) return false;

                    // 2) Get the internal type (namespace can vary; search by Name)
                    var type = asm.GetTypes().FirstOrDefault(t => t.Name == TypeName);
                    if (type == null) return false;

                    // 3) Find the static method (public or non-public). DBH’s call looks like:
                    // FindBestCleanWaterSource(pawn, pawn, bool, float, Predicate<Thing>, Predicate<Thing>)
                    // We’ll select by name + param count = 6 (safer across minor changes).
                    _miFindBestCleanWater = type
                        .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        .FirstOrDefault(m => m.Name == MethodName && m.GetParameters().Length == 6);

                    return _miFindBestCleanWater != null;
                }
                catch
                {
                    return false;
                }
            }

            /// <summary>
            /// Reflection call to DBH ClosestSanitation.FindBestCleanWaterSource.
            /// Returns Invalid LocalTargetInfo if DBH isn’t present or invocation fails.
            /// </summary>
            public static LocalTargetInfo FindBestCleanWaterSource(Pawn searcher, Pawn user, bool allowDirty, float maxDistance,
                                                                   Predicate<Thing> validator1 = null, Predicate<Thing> validator2 = null)
            {
                if (!TryInit()) return LocalTargetInfo.Invalid;

                try
                {
                    // If DBH signature differs, you may need to adjust the argument list order.
                    var result = _miFindBestCleanWater.Invoke(
                        obj: null,
                        parameters: new object[] { searcher, user, allowDirty, maxDistance, validator1, validator2 }
                    );
                    return result is LocalTargetInfo lti ? lti : LocalTargetInfo.Invalid;
                }
                catch (Exception e)
                {
                    Log.Warning($"[ZI] Reflection call to {MethodName} failed: {e}");
                    return LocalTargetInfo.Invalid;
                }
            }
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

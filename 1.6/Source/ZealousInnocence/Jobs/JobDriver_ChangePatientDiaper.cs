using DubsBadHygiene;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst.CompilerServices;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using static UnityEngine.GridBrushBase;

namespace ZealousInnocence
{
    internal class JobDriver_ChangePatientDiaper : JobDriver
    {
        protected Thing ClothThing
        {
            get
            {
                return this.job.targetA.Thing;
            }
        }
        protected Apparel Cloth
        {
            get
            {
                if(ClothThing is Apparel app)
                {
                    return app;
                }
                return null;
            }
        }
        protected Apparel OldCloth
        {
            get
            {
                return Helper_Diaper.getUnderwearOrDiaper(Patient);
            }
        }

        protected Pawn Patient
        {
            get
            {
                return (Pawn)this.job.targetB.Thing;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn pawn = this.pawn;
            LocalTargetInfo target = this.Patient;
            Job job = this.job;

            if (!pawn.Reserve(target, job, 1, -1, null, errorOnFailed, false))
            {
                Log.Message($"[ZI]JobDriver_ChangePatientDiaper Fails Toil reservations stage 2");
                return false;
            }
            
            if (job.targetA.HasThing)
            {
                if (pawn.inventory == null || !pawn.inventory.Contains(base.TargetThingA))
                {
                    target = this.ClothThing;
                    if (!pawn.Reserve(target, job, 1, -1, null, errorOnFailed, false))
                    {
                         Log.Message($"[ZI]JobDriver_ChangePatientDiaper Fails Toil reservations stage 3");
                        return false;
                    }
                }
            }
            return true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            
        }
        private bool startedPatientHoldJob = false;
        private Toil holdPatient()
        {
            // Put the patient into a wait/hold posture so they don't get up
            Toil holdPatient = new Toil();
            holdPatient.initAction = () =>
            {
                var patient = this.Patient;
                if (patient != null && patient.Spawned)
                {
                    // Only start if they aren't already in a compatible wait job
                    if (patient.CurJobDef != RimWorld.JobDefOf.Wait_MaintainPosture)
                    {
                        PawnPosture posture = PawnPosture.LayingOnGroundFaceUp;
                        if (patient.InBed())
                        {
                            posture |= PawnPosture.InBedMask;
                        }
                        

                        var wait = JobMaker.MakeJob(RimWorld.JobDefOf.Wait_MaintainPosture);
                        wait.expiryInterval = 10000;
                        wait.checkOverrideOnExpire = true;
                        wait.playerForced = true;
                        startedPatientHoldJob = true;

                        // Interrupt to force it now but allow resume of prior job later
                        patient.jobs.StartJob(wait, JobCondition.InterruptForced, null, resumeCurJobAfterwards: true);
                        patient.jobs.posture = posture;
                    }
                }
            };
            holdPatient.defaultCompleteMode = ToilCompleteMode.Instant;
            holdPatient.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            return holdPatient;
        }
        private Toil releasePatient()
        {
            Toil releasePatient = new Toil();
            releasePatient.initAction = () =>
            {
                if (startedPatientHoldJob && Patient != null && Patient.Spawned)
                {
                    // End their waiting job so they can resume normal AI
                    if (Patient.CurJobDef == RimWorld.JobDefOf.Wait_MaintainPosture)
                        Patient.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
                startedPatientHoldJob = false;
            };
            releasePatient.defaultCompleteMode = ToilCompleteMode.Instant;
            return releasePatient;
        }
        private void releasePatientFinal()
        {
            this.AddFinishAction(delegate
            {
                if (startedPatientHoldJob && Patient != null && Patient.Spawned &&
                    Patient.CurJobDef == RimWorld.JobDefOf.Wait_MaintainPosture)
                {
                    Patient.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
                startedPatientHoldJob = false;
            });
        }
        public static bool immediateFailReasons(Pawn patient, Pawn actor)
        {
            if (patient == null || patient.Dead) return true;
            if(patient.GetPosture() == PawnPosture.Standing && !patient.isToddlerOrBabyMentalOrPhysical())

            if (actor == null || actor.Dead || actor.Downed) return true;
            return false;
        }
        public static void orientBaby(Pawn patient, Pawn actor)
        {
            float angle = (actor.Position - patient.Position).AngleFlat;
            patient.Rotation = Rot4.FromAngleFlat(angle).Opposite;
        }
        private Apparel Spare => job.targetA.Thing as Apparel;
        protected override IEnumerable<Toil> MakeNewToils()
        {
            //this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            //this.FailOn(() => !FoodUtility.ShouldBeFedBySomeone(this.Patient) &&  !Patient.IsPrisoner);
            //this.FailOn(() => !WorkGiver_Tend.GoodLayingStatusForTend(this.Patient, this.pawn));
            this.FailOn(() => immediateFailReasons(Patient, this.pawn));
            base.SetFinalizerJob(delegate (JobCondition condition)
            {
                if (!pawn.IsCarryingPawn(Patient))
                {
                    return null;
                }
                return ChildcareUtility.MakeBringBabyToSafetyJob(pawn, Patient);
            });

            releasePatientFinal();

            var thinkNow = Toils_General.Wait(20); // 1/3 second so this doesn't crash the game in case of issues
            yield return thinkNow;

            if (Patient.isToddlerMentalOrPhysical() && !Patient.InBed())
            {
                LocalTargetInfo dest;
                if(!TryFindChangeSpot(pawn, Patient, Patient.Position, 40, out dest))
                {
                    EndJobWith(JobCondition.Incompletable);
                    yield break;
                }
                job.SetTarget(TargetIndex.C, dest);

                Toil carryingBabyStart = Toils_General.Label();
                yield return Toils_Jump.JumpIf(carryingBabyStart, () => this.pawn.IsCarryingPawn(Patient));
                const int sprintAfterTicks = 300;        // 300 => 5s @ 60 TPS

                int chaseTicks = 0;
                bool sprinting = false;

                // PURSUE the moving toddler
                var pursue = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
                pursue.FailOnDestroyedNullOrForbidden(TargetIndex.B);
                pursue.FailOn(() => Patient.Dead);

                // keep it focused on movement (no chat)
                pursue.socialMode = RandomSocialMode.Off;

                // Escalation + repath loop
                pursue.AddPreTickAction(() =>
                {
                    chaseTicks++;

                    // tiny nudge to repath if target changed cell (GotoThing already repaths; this just tightens it)
                    if (pawn.pather.Moving && (chaseTicks % 15 == 0)) // every 15 ticks
                    {
                        if (pawn.pather.Destination.Thing != Patient)
                            pawn.pather.StartPath(Patient, PathEndMode.Touch);
                    }

                    // Sprint escalation
                    if (!sprinting && chaseTicks >= sprintAfterTicks)
                    {
                        sprinting = true;
                        if (pawn.jobs?.curJob != null)
                        {
                            pawn.jobs.curJob.locomotionUrgency = LocomotionUrgency.Sprint;
                            // poke pather to pick up new urgency quickly
                            pawn.pather?.TryRecoverFromUnwalkablePosition();
                        }
                        Log.Message($"[ZI]ChangeDiaper Escalate: Sprint after {chaseTicks} ticks for {pawn.LabelShort} and {Patient.LabelShort}");
                    }
                });
                yield return pursue;
                yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false, true, false);
                yield return carryingBabyStart;

                if (job.GetTarget(TargetIndex.C).HasThing)
                {
                    yield return Toils_Reserve.Reserve(TargetIndex.C);
                    yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.Touch)
                        .FailOnDestroyedNullOrForbidden(TargetIndex.C)
                        .FailOn(() => Patient.DestroyedOrNull());
                    // Releasing reservation so baby can reserve
                    yield return Toils_General.Do(() =>
                    {
                        pawn.Map.reservationManager.Release(job.targetC, pawn, job);
                    });
                    yield return Toils_Bed.TuckIntoBed(TargetIndex.C, TargetIndex.B);
                }
                else
                {
                    // ---- C is a cell ----
                    yield return Toils_Goto.GotoCell(TargetIndex.C, PathEndMode.OnCell)
                        .FailOn(() => Patient.DestroyedOrNull());

                    yield return Toils_General.Do(() =>
                    {
                        var dropCell = job.GetTarget(TargetIndex.C).Cell;
                        pawn.carryTracker.TryDropCarriedThing(dropCell, ThingPlaceMode.Direct, out _);
                        orientBaby(Patient, pawn);
                    });
                }
            }
            yield return holdPatient();

            Thing FoundSpare = pawn?.inventory?.innerContainer?.FirstOrDefault(t => t == Spare);
            if (FoundSpare != null)
            {
                Toil seperateSingle = new Toil();
                seperateSingle.initAction = () =>
                {

                    if (Spare != null)
                    {
                        var SpareSingle = pawn.MoveSingleFromInventoryToHands(Spare);
                        if (SpareSingle == null)
                        {
                            EndJobWith(JobCondition.Incompletable);
                            return;
                        }
                        job.SetTarget(TargetIndex.A, SpareSingle);
                    }
                    else
                    {
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }
                };
                seperateSingle.defaultCompleteMode = ToilCompleteMode.Instant;
                yield return seperateSingle;

            }
            else
            {
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch, false).FailOnForbidden(TargetIndex.A);
                yield return Toils_Reserve.Release(TargetIndex.A);
                yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, true, false);
            }
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch, false);

            if (OldCloth != null)
            {
                Toil changeDiaper = Toils_General.Wait(150, TargetIndex.B); // 2.5 seconds at 60 ticks per second 
                changeDiaper.initAction = () =>
                {
                        SoundStarter.PlayOneShotOnCamera(DiaperChangie.Diapertape, pawn.Map);
                };
                changeDiaper.defaultCompleteMode = ToilCompleteMode.Delay;
                changeDiaper.WithProgressBarToilDelay(TargetIndex.B);
                changeDiaper.FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch);
                changeDiaper.AddFinishAction(() =>
                {

                    if (OldCloth != null)
                    {
                        Apparel resultingAp;
                        Patient.apparel.TryDrop(OldCloth, out resultingAp, this.pawn.PositionHeld, false);
                    }

                });
                yield return changeDiaper;
            }
            

            Toil changingProgress = Toils_General.Wait(300, TargetIndex.B);
            changingProgress.tickAction = () =>
            {
                if (this.pawn.IsHashIntervalTick(150))
                {
                    SoundStarter.PlayOneShotOnCamera(DiaperChangie.Diapertape, pawn.Map);
                }
                if (this.pawn.IsHashIntervalTick(30))
                {
                    MoteMaker.MakeAttachedOverlay(Patient, RimWorld.ThingDefOf.Mote_BabyCryingDots, new UnityEngine.Vector3(0.27f, 0f, 0.066f).RotatedBy(90f), 1f, -1f).exactRotation = Rand.Value * 180f;
                    MoteMaker.MakeAttachedOverlay(Patient, RimWorld.ThingDefOf.Mote_BabyCryingDots, new UnityEngine.Vector3(-0.27f, 0f, 0.066f).RotatedBy(45f), 1f, -1f).exactRotation = Rand.Value * 180f;
                }
            };
            changingProgress.WithProgressBarToilDelay(TargetIndex.B);
            changingProgress.defaultCompleteMode = ToilCompleteMode.Delay;
            changingProgress.FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch);
            yield return changingProgress;

            Toil wearNewDiaper = new Toil();
            wearNewDiaper.initAction = () =>
            {
                this.pawn.carryTracker.innerContainer.Remove(ClothThing);
                Patient.apparel.Wear(Cloth);
            };
            wearNewDiaper.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return wearNewDiaper;

            Toil triggerDiaperChangeInteractionResult = new Toil();
            triggerDiaperChangeInteractionResult.initAction = () =>
            {
                Helper_Diaper.triggerDiaperChangeInteractionResult(this.pawn,Patient);
            };
            triggerDiaperChangeInteractionResult.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return triggerDiaperChangeInteractionResult;

            yield return releasePatient();

            // Log.Message($"[ZI]Ended toils for {this.ClothThing.LabelShort}");
            yield break;
        }

        public static bool TryFindChangeSpot(Pawn worker, Pawn baby, LocalTargetInfo fallbackSpot, int radius, out LocalTargetInfo spot)
        {
            spot = LocalTargetInfo.Invalid;
            var map = worker.Map;
            if (map == null) return false;

            bool FindNearestSpot(Predicate<Thing> validator, Func<Thing, IntVec3?> pickCell, ref LocalTargetInfo spot)
            {
                var thing = GenClosest.ClosestThingReachable(
                    worker.Position, map,
                    ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial),
                    PathEndMode.Touch,
                    TraverseParms.For(worker),
                    radius,
                    t => validator(t) && worker.CanReserve(t)
                );
                if (thing == null) return false;

                var c = pickCell(thing);
                if (c.HasValue && c.Value.InBounds(map) && c.Value.GetFirstPawn(map) == null &&
                    worker.CanReach(c.Value, PathEndMode.OnCell, Danger.Deadly))
                {
                    spot = thing; // Save the Thing itself, not just the cell
                    return true;
                }
                return false;
            }

            // Own Crib
            if (baby.CurrentBed() is Building_Bed bed && bed.Position.InHorDistOf(worker.Position, radius))
            {
                for (int i = 0; i < bed.SleepingSlotsCount; i++)
                    if (bed.GetCurOccupant(i) == null)
                    {
                        spot = bed; // Directly return the bed Thing
                        return true;
                    }
            }

            // Near empty Crib
            if (FindNearestSpot(
                t => t is Building_Bed b && Helper_Regression.IsCrib(b),
                t =>
                {
                    if (t is Building_Bed bed)
                    {
                        for (int i = 0; i < bed.SleepingSlotsCount; i++)
                            if (bed.GetCurOccupant(i) == null)
                                return bed.GetSleepingSlotPos(i);
                    }
                    return null;
                },
                ref spot))
                return true;

            // Chair as fallback
            if (FindNearestSpot(
                t => t.def.building?.isSittable == true,
                t => t.Position.Standable(map) ? t.Position : (IntVec3?)null,
                ref spot))
                return true;

            // Just use fallback spot
            if (fallbackSpot.Cell != null && fallbackSpot.Cell.InBounds(map) && fallbackSpot.Cell.Standable(map))
            {
                spot = fallbackSpot;
                return true;
            }

            return false;
        }
    }
}

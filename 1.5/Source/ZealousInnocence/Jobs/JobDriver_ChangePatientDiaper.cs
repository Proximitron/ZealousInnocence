using DubsBadHygiene;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using UnityEngine;
using System.Net.NetworkInformation;
using Verse.Sound;

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
        protected Thing OldClothThing
        {
            get
            {
                return this.job.targetC.Thing;
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
                if (OldClothThing is Apparel app)
                {
                    return app;
                }
                return null;
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
                Log.Message($"JobDriver_ChangePatientDiaper Fails Toil reservations stage 2");
                return false;
            }
            
            if (job.targetA.HasThing)
            {
                if (pawn.inventory == null || !pawn.inventory.Contains(base.TargetThingA))
                {
                    target = this.ClothThing;
                    if (!pawn.Reserve(target, job, 1, -1, null, errorOnFailed, false))
                    {
                        Log.Message($"JobDriver_ChangePatientDiaper Fails Toil reservations stage 3");
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
        
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            this.FailOn(() => !FoodUtility.ShouldBeFedBySomeone(this.Patient));
            this.FailOn(() => !WorkGiver_Tend.GoodLayingStatusForTend(this.Patient, this.pawn));

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch, false).FailOnForbidden(TargetIndex.A);
            yield return Toils_Reserve.Release(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, true, false);
            
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch, false);


            if (OldCloth != null)
            {
                Toil changeDiaper = new Toil();
                changeDiaper.initAction = () =>
                {
                    if (this.pawn.IsHashIntervalTick(150))
                    {
                        SoundStarter.PlayOneShotOnCamera(DiaperChangie.Diapertape, pawn.Map);
                    }
                    this.pawn.jobs.curDriver.ticksLeftThisToil = 300; // 5 seconds at 60 ticks per second 
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

            Toil changingProgress = new Toil();
            changingProgress.initAction = () =>
            {
                this.pawn.jobs.curDriver.ticksLeftThisToil = 600; // 10 seconds at 60 ticks per second
                
            };
            changingProgress.tickAction = () =>
            {
                if (this.pawn.IsHashIntervalTick(150))
                {
                    SoundStarter.PlayOneShotOnCamera(DiaperChangie.Diapertape, pawn.Map);
                }
                if (this.pawn.IsHashIntervalTick(30))
                {
                    MoteMaker.MakeAttachedOverlay(Patient, ThingDefOf.Mote_BabyCryingDots, new Vector3(0.27f, 0f, 0.066f).RotatedBy(90f), 1f, -1f).exactRotation = Rand.Value * 180f;
                    MoteMaker.MakeAttachedOverlay(Patient, ThingDefOf.Mote_BabyCryingDots, new Vector3(-0.27f, 0f, 0.066f).RotatedBy(45f), 1f, -1f).exactRotation = Rand.Value * 180f;
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
            Log.Message($"Ended toils for {this.ClothThing.LabelShort}");
            yield break;
        }
    }
}

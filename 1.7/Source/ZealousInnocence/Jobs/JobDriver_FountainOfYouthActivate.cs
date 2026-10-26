using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse.AI;
using Verse;

namespace ZealousInnocence
{
    public class JobDriver_FountainOfYouthActivate : JobDriver
    {
        private Building_FountainOfYouth Fountain
        {
            get
            {
                return this.job.GetTarget(FountainIndex).Thing as Building_FountainOfYouth;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (base.TargetThingC != null)
            {
                this.pawn.ReserveAsManyAsPossible(this.job.GetTargetQueue(ActivationItemIndex), this.job, 1, -1, null);
            }
            return (base.TargetThingB == null || this.pawn.Reserve(this.job.GetTarget(ActivationItemIndex), this.job, 1, -1, null, errorOnFailed, false)) && this.pawn.Reserve(this.Fountain, this.job, 1, -1, null, errorOnFailed, false) && this.pawn.ReserveSittableOrSpot(this.Fountain.InteractionCell, this.job, errorOnFailed);
        }
        public GameComponent_RegressionGame regression
        {
            get
            {
                return Current.Game.GetComponent<GameComponent_RegressionGame>();
            }
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(delegate ()
            {
                string text;
                string text2;
                return !this.Fountain.CanActivate(out text, out text2);
            });
            if (regression.Level == 0) // show confirm if not yet active
            {
                yield return Toils_General.Do(delegate
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("FountainOfYouthActivateConfirmationText".Translate(), delegate ()
                    {
                    }, delegate ()
                    {
                        this.pawn.jobs.EndCurrentJob(JobCondition.Succeeded, true, true);
                    }, false, null, WindowLayer.Dialog));
                });
            }
            if (base.TargetThingB != null)
            {
                Toil getToHaulTarget = Toils_Goto.GotoThing(ActivationItemIndex, PathEndMode.ClosestTouch, true).FailOnDespawnedNullOrForbidden(ActivationItemIndex).FailOnSomeonePhysicallyInteracting(ActivationItemIndex);
                yield return getToHaulTarget;
                yield return Toils_Haul.StartCarryThing(ActivationItemIndex, false, true, false, true, true);
                yield return Toils_Haul.JumpIfAlsoCollectingNextTargetInQueue(getToHaulTarget, ActivationItemIndex);
                getToHaulTarget = null;
            }
            if (this.job.targetC == null)
            {
                this.job.targetC = base.TargetThingA.SpawnedParentOrMe;
            }
            yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.InteractionCell, false).FailOnDespawnedNullOrForbidden(TargetIndex.C).FailOnSomeonePhysicallyInteracting(TargetIndex.C);
            int ticks = 600;
            Toil toil = Toils_General.WaitWith(FountainIndex, ticks, true, true, false, FountainIndex);
            toil.WithEffect(EffecterDefOf.MonolithStage2, () => base.TargetA, null);
            Toil toil2 = toil;
            toil2.tickAction = (Action)Delegate.Combine(toil2.tickAction, new Action(delegate ()
            {
                Find.TickManager.slower.SignalForceNormalSpeed();
            }));
            yield return toil;
            yield return Toils_General.Do(delegate
            {
                if (base.TargetThingB != null)
                {
                    this.pawn.carryTracker.DestroyCarriedThing();
                }
                this.Fountain.Activate(this.pawn);
            });
            yield return Toils_General.Wait(360, FountainIndex);
            yield break;
        }

        private const TargetIndex FountainIndex = TargetIndex.A;

        private const TargetIndex ActivationItemIndex = TargetIndex.B;
    }
}

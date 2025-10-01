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
    public class JobDriver_FountainOfYouthInvestigate : JobDriver
    {
        private Building_FountainOfYouth Fountain
        {
            get
            {
                return base.TargetThingA as Building_FountainOfYouth;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.Fountain, this.job, 1, -1, null, errorOnFailed, false) && this.pawn.ReserveSittableOrSpot(this.Fountain.InteractionCell, this.job, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            if (this.job.targetB == null)
            {
                this.job.targetB = base.TargetThingA.SpawnedParentOrMe;
            }
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell, false).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            int ticks = 300;
            Toil toil = Toils_General.WaitWith(TargetIndex.A, ticks, true, true, false, TargetIndex.A);
            toil.WithEffect(EffecterDefOf.MonolithStage1, () => base.TargetA, null);
            toil.PlaySustainerOrSound(SoundDefOf.VoidMonolith_InspectLoop, 1f);
            Toil toil2 = toil;
            toil2.tickAction = (Action)Delegate.Combine(toil2.tickAction, new Action(delegate ()
            {
                Find.TickManager.slower.SignalForceNormalSpeed();
            }));
            yield return toil;
            yield return Toils_General.Do(delegate
            {
                this.Fountain.Investigate(this.pawn);
            });
            yield break;
        }
    }
}

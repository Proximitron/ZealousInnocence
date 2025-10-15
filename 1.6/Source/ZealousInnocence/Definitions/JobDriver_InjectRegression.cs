using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ZealousInnocence
{
    public class JobDriver_InjectRegression : JobDriver
    {
        private LocalTargetInfo Target
        {
            get
            {
                return job.GetTarget(TargetIndex.A);
            }
        }

        private Thing Item
        {
            get
            {
                return job.GetTarget(TargetIndex.B).Thing;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(Target, this.job, 1, -1, null, errorOnFailed, false) && this.pawn.Reserve(this.Item, this.job, 1, -1, null, errorOnFailed, false);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false, true, false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.A);
            Toil toil = Toils_General.Wait(600, TargetIndex.None);
            toil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            toil.tickAction = delegate ()
            {
                CompUsable compUsable = this.Item.TryGetComp<CompUsable>();
                if (compUsable != null && this.warmupMote == null && compUsable.Props.warmupMote != null)
                {
                    this.warmupMote = MoteMaker.MakeAttachedOverlay(this.Target.Thing, compUsable.Props.warmupMote, Vector3.zero, 1f, -1f);
                }
                Mote mote = this.warmupMote;
                if (mote == null)
                {
                    return;
                }
                mote.Maintain();
            };
            yield return toil;
            yield return Toils_General.Do(new Action(this.Inject));
            yield break;
        }

        private void Inject()
        {
            Pawn pawn = null;
            if (this.Target.Thing is Corpse c)
            {
                pawn = c.InnerPawn;
            }
            else if(this.Target.Thing is Pawn p)
            {
                pawn = p;
            }

            if (pawn == null) return;
            int dose = Math.Max(1, this.job.count);
            var effectComp = Item.TryGetComp<CompTargetEffectFoyDose>();

            float severity = 0.05f;
            ThingDef moteDef = null;
            if (effectComp == null) return;

            var props = effectComp?.Props;

            severity = (props?.severityPerDose ?? severity) * dose;

            SoundDefOf.MechSerumUsed.PlayOneShot(SoundInfo.InMap(pawn));
            if (moteDef != null)
                MoteMaker.MakeAttachedOverlay(pawn, moteDef, Vector3.zero, 1f, -1f);

            Helper_Regression.SetIncreasedRegressionSeverity(pawn, this.pawn, severity, severity);

            // consume exactly 'dose' from the stack (even if carried)
            Item.SplitOff(dose).Destroy(DestroyMode.Vanish);
        }

        private Mote warmupMote;
    }
}

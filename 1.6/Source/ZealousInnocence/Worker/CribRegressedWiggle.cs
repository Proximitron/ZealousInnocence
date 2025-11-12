using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ZealousInnocence
{
    class JobDriver_RegressedWiggleInCrib : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFailCondition(() => !pawn.InCrib() || pawn.Downed);
            Toil toil = ToilMaker.MakeToil("RegressedWiggleInCrib");
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = 1200;
            toil.AddPreInitAction(delegate ()
            {
                this.pawn.jobs.posture = RimWorld.PawnPosture.InBedMask;
                pawn.Rotation = Rot4.South;

                pawn.Drawer.renderer.SetAnimation(AnimationDefOf.RegressedWiggleInCrib);
            });
            toil.AddPreTickAction(delegate ()
            {
                if (pawn.Drawer.renderer.CurAnimation != AnimationDefOf.RegressedWiggleInCrib)
                    pawn.Drawer.renderer.SetAnimation(AnimationDefOf.RegressedWiggleInCrib);
            });
            toil.handlingFacing = true;
            toil.AddFinishAction(() => {
                pawn.Drawer.renderer.SetAnimation(null);
            });
            yield return toil;
        }
    }
    public class AnimationWorker_Toddler_RegressedWiggleInCrib : BaseAnimationWorker
    {
        public static float Waveform(Func<float, float> quarterform, float x)
        {
            x = Mathf.Clamp01(x);
            if (x <= 0.25f) return quarterform(4f * x);
            if (x <= 0.5f) return quarterform(4f * (0.5f - x));
            if (x <= 0.75f) return -1f * quarterform(4f * (x - 0.5f));
            return -1f * quarterform(4f * (1f - x));
        }
        public override float AngleAtTick(int tick, AnimationDef def, PawnRenderNode node, AnimationPart part, PawnDrawParms parms)
        {
            float x = (float)(Find.TickManager.TicksGame % 120) / 120f;
            return 15f * Waveform(f => f, x);
        }

        public override bool Enabled(AnimationDef def, PawnRenderNode node, AnimationPart part, PawnDrawParms parms)
        {
            if (!def.playWhenDowned && parms.pawn.Downed)
            {
                return false;
            }
            if (parms.pawn.jobs.curDriver.CurToilString == "RegressedWiggleInCrib") return true;


            return false;
        }

        public override GraphicStateDef GraphicStateAtTick(int tick, AnimationDef def, PawnRenderNode node, AnimationPart part, PawnDrawParms parms)
        {
            return null;
        }

        public override Vector3 OffsetAtTick(int tick, AnimationDef def, PawnRenderNode node, AnimationPart part, PawnDrawParms parms)
        {
            return Vector3.zero;
        }

        public override void PostDraw(AnimationDef def, PawnRenderNode node, AnimationPart part, PawnDrawParms parms, Matrix4x4 matrix)
        {
        }

        public override Vector3 ScaleAtTick(int tick, AnimationDef def, PawnRenderNode node, AnimationPart part, PawnDrawParms parms)
        {
            return Vector3.one;
        }
    }
}

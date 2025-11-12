using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace ZealousInnocence
{
    class ThinkNode_ConditionalInCrib : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn) => pawn.InCrib();
    }
    class JobDriver_TrappedInCrib : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFailCondition(() => !pawn.InCrib() || pawn.Downed);
            Toil toil = ToilMaker.MakeToil("MakeNewToil");
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = 3000;
            toil.AddPreInitAction(delegate ()
            {
                pawn.jobs.posture = RimWorld.PawnPosture.LayingInBed;
            });

            yield return toil;
        }
    }
    class JobGiver_IdleInCrib : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            if (!pawn.Awake()) return 0f;
            return 1f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.CurJob != null && !(pawn.CurJob.def == JobDefOf.LayDown)) return pawn.CurJob;

            Thing crib = pawn.GetCurrentCrib();
            if (crib == null) return null;

            List<JobDef> Activities;
            Activities = new List<JobDef>
            {
                //Toddlers_DefOf.LayAngleInCrib,
                JobDefOf.TrappedInCrib,
                JobDefOf.RegressedWiggleInCrib
            };
            

            JobDef jobDef = Activities.RandomElement<JobDef>();
            return JobMaker.MakeJob(jobDef, crib);
        }
    }
}

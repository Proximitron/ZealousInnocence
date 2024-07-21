using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using Verse.AI.Group;
using System.Runtime;
using UnityEngine;
using System.Security.Cryptography;

namespace ZealousInnocence
{
    
    public class InteractionWorker_RegressedPlayTime : InteractionWorker
    {
        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            /*
            // too big self
            if(RegressionHelper.isAgeStage(initiator, 10))
            {
                return 0f;
            }
            // too young self
            if(RegressionHelper.isAgeStage(initiator, 0))
            {
                return 0f;
            }
            // too young target
            if (RegressionHelper.isAgeStage(recipient, 0))
            {
                return 0f;
            }
            */
            if (!RegressionHelper.isChild(initiator))
            {
                return 0f;
            }
            if (!RegressionHelper.isChild(recipient))
            {
                return 0f;
            }
            if (initiator.GetLord() != null || recipient.GetLord() != null)
            {
                return 0f;
            }
            float num = 0.5f; // Regressed people might not always do what you want
            if (initiator.GetTimeAssignment() == TimeAssignmentDefOf.Anything)
            {
                num+= 1.1f;
            }
            else if (initiator.GetTimeAssignment() == TimeAssignmentDefOf.Joy)
            {
                num+= 1.4f;
            }
            if (initiator.mindState.IsIdle && recipient.mindState.IsIdle && initiator.GetTimeAssignment() != TimeAssignmentDefOf.Work && recipient.GetTimeAssignment() != TimeAssignmentDefOf.Work)
            {
                num+= 2f;
            }
           
            var age1 = RegressionHelper.getAgeStage(recipient);
            var age2 = RegressionHelper.getAgeStage(initiator);
            if(age1 > age2)
            {
                num += (1.0f - ((age1 - age2) * 0.2f));
            }
            else
            {
                num += (1.0f - ((age2 - age1) * 0.2f));
            }
            
            return 0.33f * num;
        }

        public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
        {
            letterText = null;
            letterLabel = null;
            letterDef = null;
            lookTargets = null;
            initiator.jobs.StopAll();
            recipient.jobs.StopAll();
            Lord lord = LordMaker.MakeNewLord(initiator.Faction, new LordJob_RegressedPlayTime(initiator, recipient), initiator.Map, new Pawn[2] { initiator, recipient });
        }
    }
    public class JobDriver_RegressedPlayAround : JobDriver
    {
        private bool jumping;

        public int Ticks => Find.TickManager.TicksGame - startTick;

        public override Vector3 ForcedBodyOffset
        {
            get
            {
                float num = Mathf.Sin((float)Ticks / 60f * 8f);
                float z = Mathf.Max(Mathf.Pow((num + 1f) * 0.5f, 2f) * 0.2f - 0.06f, 0f);
                return new Vector3(0f, 0f, z);
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil toil = new Toil();
            jumping = Rand.Bool;
            toil.tickAction = delegate
            {
                if (Ticks % 10 == 0)
                {
                    jumping = !jumping;
                }
                if (Ticks % 60 == 0 && !jumping)
                {
                    pawn.Rotation = Rot4.Random;
                }
 
                if (pawn.needs.joy != null) pawn.needs.joy.GainJoy(job.def.joyGainRate * 0.000644f, JoyKindDefOf.Social);
                else if (pawn.needs.learning != null) LearningUtility.LearningTickCheckEnd(pawn);
            };
            toil.socialMode = RandomSocialMode.SuperActive;
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = 30;
            toil.handlingFacing = true;
            yield return toil;
        }
    }
    public class LordToil_RegressedPlayTime : LordToil
    {
        public Pawn[] friends;

        public Job playTime;

        public int ticksToNextJoy = 0;

        public int tickSinceLastJobGiven = 0;

        public LordToil_RegressedPlayTime(Pawn[] pawns)
        {
            friends = pawns;
        }

        public override void UpdateAllDuties()
        {
            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                lord.ownedPawns[i].mindState.duty = new PawnDuty(DutyDefOf.RegressedPlayTime, friends[0].Position, friends[1].Position);
            }
        }
    }
    internal class LordJob_RegressedPlayTime : LordJob
    {
        private Trigger_TicksPassed timeoutTrigger;

        public Pawn initiator;

        public Pawn recipient;

        public LordJob_RegressedPlayTime()
        {
        }

        public LordJob_RegressedPlayTime(Pawn initiator, Pawn recipient)
        {
            this.initiator = initiator;
            this.recipient = recipient;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            LordToil_RegressedPlayTime lordToil_PlayTime = new LordToil_RegressedPlayTime(new Pawn[2] { initiator, recipient });
            stateGraph.AddToil(lordToil_PlayTime);
            LordToil_End lordToil_End = new LordToil_End();
            stateGraph.AddToil(lordToil_End);
            Transition transition = new Transition(lordToil_PlayTime, lordToil_End);
            transition.AddTrigger(new Trigger_TickCondition(() => ShouldBeCalledOff()));
            transition.AddTrigger(new Trigger_TickCondition(() => initiator.health.summaryHealth.SummaryHealthPercent < 1f || recipient.health.summaryHealth.SummaryHealthPercent < 1f));
            transition.AddTrigger(new Trigger_TickCondition(() => initiator.Drafted || recipient.Drafted));
            transition.AddTrigger(new Trigger_TickCondition(() => initiator.Map == null || recipient.Map == null));
            transition.AddTrigger(new Trigger_PawnLost());
            stateGraph.AddTransition(transition);
            timeoutTrigger = new Trigger_TicksPassed(Rand.RangeInclusive(2500, 5000));
            Transition transition2 = new Transition(lordToil_PlayTime, lordToil_End);
            transition2.AddTrigger(timeoutTrigger);
            transition2.AddPreAction(new TransitionAction_Custom((Action)delegate
            {
                Finished();
            }));
            stateGraph.AddTransition(transition2);
            return stateGraph;
        }

        public void Finished()
        {

            initiator.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.RegressedGames, recipient);
            recipient.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.RegressedGames, initiator);
        }

        public override void ExposeData()
        {
            Scribe_References.Look(ref initiator, "initiator");
            Scribe_References.Look(ref recipient, "recipient");
        }

        public override string GetReport(Pawn pawn)
        {
            return "LordJobRegressedPlayAround".Translate();
        }

        private bool ShouldBeCalledOff()
        {
            return !GatheringsUtility.AcceptableGameConditionsToContinueGathering(base.Map)
                || initiator.GetTimeAssignment() == TimeAssignmentDefOf.Work
                || recipient.GetTimeAssignment() == TimeAssignmentDefOf.Work
                || (initiator.needs.rest != null && initiator.needs.rest.CurLevel < 0.3f)
                || (recipient.needs.rest != null && recipient.needs.rest.CurLevel < 0.3f)
                || initiator.needs.rest == null
                || recipient.needs.rest == null
                || (initiator.needs.joy != null && initiator.needs.joy.CurLevel > 0.9f)
                || (recipient.needs.joy != null && recipient.needs.joy.CurLevel > 0.9f)
                || (recipient.needs.learning != null && recipient.needs.learning.CurLevel > 0.9f)
                || (recipient.needs.learning != null && recipient.needs.learning.CurLevel > 0.9f);
        }
    }

    public class JobGiver_RegressedPlayTime : JobGiver_GetJoy
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debugging = settings.debugging && settings.debuggingJobs;
            if(debugging) Log.Message($"JobGiver_RegressedPlayTime: TryGiveJob called for pawn: {pawn?.Name?.ToStringFull}");

            // Check if pawn or its lord is null
            if (pawn == null)
            {
                if (debugging) Log.Error("JobGiver_RegressedPlayTime: Pawn is null");
                return null;
            }

            var lord = pawn.GetLord();
            if (lord == null)
            {
                if (debugging) Log.Error("JobGiver_RegressedPlayTime: Pawn's lord is null");
                return null;
            }

            // Check if the current lord toil is of the expected type
            LordToil_RegressedPlayTime lordToil_PlayTime = lord.CurLordToil as LordToil_RegressedPlayTime;
            if (lordToil_PlayTime == null)
            {
                if (debugging) Log.Error("JobGiver_RegressedPlayTime: Current lord toil is not LordToil_RegressedPlayTime");
                return null;
            }

            // Check if the friends array is valid
            if (lordToil_PlayTime.friends == null || lordToil_PlayTime.friends.Length < 2)
            {
                if (debugging) Log.Error("JobGiver_RegressedPlayTime: Friends array is null or does not have enough elements");
                return null;
            }
            
            // Determine the friend pawn
            Pawn friend = (pawn == lordToil_PlayTime.friends[0]) ? lordToil_PlayTime.friends[1] : lordToil_PlayTime.friends[0];
            if (friend == null)
            {
                if (debugging) Log.Warning("JobGiver_RegressedPlayTime: Friend is null");
                return null;
            }
            if (lordToil_PlayTime.playTime == null || lordToil_PlayTime.ticksToNextJoy < Find.TickManager.TicksGame)
            {
                lordToil_PlayTime.playTime = new Job(JobDefOf.RegressedPlayAround); 
                lordToil_PlayTime.ticksToNextJoy = Find.TickManager.TicksGame + Rand.RangeInclusiveSeeded(2500, 7500, pawn.HashOffsetTicks());
            }
            Need need = pawn.needs.joy;
            if (need == null) need = pawn.needs.learning;
            if(need == null)
            {
                if (debugging) Log.Error("JobGiver_RegressedPlayTime: No usable need found!");
                return null;
            }
            if (lordToil_PlayTime.playTime != null &&
                (friend.needs.food == null || friend.needs.food.CurLevel > 0.33f) &&
                need.CurLevel < 0.8f &&
                Math.Abs(lordToil_PlayTime.tickSinceLastJobGiven - Find.TickManager.TicksGame) > 1)
            {
                Job job = new Job(lordToil_PlayTime.playTime.def)
                {
                    targetA = lordToil_PlayTime.playTime.targetA,
                    targetB = lordToil_PlayTime.playTime.targetB,
                    targetC = lordToil_PlayTime.playTime.targetC,
                    targetQueueA = lordToil_PlayTime.playTime.targetQueueA,
                    targetQueueB = lordToil_PlayTime.playTime.targetQueueB,
                    count = lordToil_PlayTime.playTime.count,
                    countQueue = lordToil_PlayTime.playTime.countQueue,
                    expiryInterval = lordToil_PlayTime.playTime.expiryInterval,
                    locomotionUrgency = lordToil_PlayTime.playTime.locomotionUrgency
                };
                if (job.TryMakePreToilReservations(pawn, errorOnFailed: false))
                {
                    lordToil_PlayTime.tickSinceLastJobGiven = Find.TickManager.TicksGame;
                    return job;
                }
                pawn.ClearAllReservations(releaseDestinationsOnlyIfObsolete: false);
            }
            // Movement and job assignment logic with logging
            if ((pawn.Position - friend.Position).LengthHorizontalSquared >= 6 || !GenSight.LineOfSight(pawn.Position, friend.Position, pawn.Map, skipFirstCell: true))
            {
                if (friend.CurJob != null && friend.CurJob.def != RimWorld.JobDefOf.Goto)
                {
                    return new Job(RimWorld.JobDefOf.Goto, friend);
                }

                pawn.rotationTracker.FaceCell(friend.Position);
                return null;
            }
            if (Rand.ChanceSeeded(0.8f, pawn.HashOffsetTicks()))
            {
                Predicate<IntVec3> validator2 = (IntVec3 x) =>
                    x.Standable(pawn.Map) &&
                    x.InAllowedArea(pawn) &&
                    !x.IsForbidden(pawn) &&
                    pawn.CanReserveAndReach(x, PathEndMode.OnCell, Danger.None) &&
                    (x - friend.Position).LengthHorizontalSquared < 50 &&
                    GenSight.LineOfSight(x, friend.Position, pawn.Map, skipFirstCell: true) &&
                    x != friend.Position;

                if (CellFinder.TryFindRandomReachableCellNearPosition(pawn.Position, pawn.Position, pawn.Map, 12f, TraverseParms.For(TraverseMode.NoPassClosedDoors), validator2, null, out var result2))
                {
                    if ((pawn.Position - friend.Position).LengthHorizontalSquared >= 5 || (LovePartnerRelationUtility.LovePartnerRelationExists(pawn, friend) && pawn.Position != friend.Position))
                    {
                        pawn.mindState.nextMoveOrderIsWait = !pawn.mindState.nextMoveOrderIsWait;
                        if (!result2.IsValid || pawn.mindState.nextMoveOrderIsWait)
                        {
                            pawn.rotationTracker.FaceCell(friend.Position);
                            return null;
                        }
                    }

                    Job job2 = new Job(JobDefOf.RegressedPlayAround, result2);
                    pawn.Map.pawnDestinationReservationManager.Reserve(pawn, job2, result2);
                    return job2;
                }
                if (debugging) Log.Message($"JobGiver_RegressedPlayTime: Post block 5");
                pawn.rotationTracker.FaceCell(friend.Position);
                return null;
            }

            Predicate<IntVec3> validator = (IntVec3 x) =>
                x.Standable(friend.Map) &&
                x.InAllowedArea(friend) &&
                !x.IsForbidden(friend) &&
                friend.CanReserveAndReach(x, PathEndMode.OnCell, Danger.None) &&
                (x - pawn.Position).LengthHorizontalSquared < 50 &&
                GenSight.LineOfSight(x, pawn.Position, pawn.Map, skipFirstCell: true) &&
                x != pawn.Position;

            if (CellFinder.TryFindRandomReachableCellNearPosition(friend.Position, friend.Position, friend.Map, 12f, TraverseParms.For(TraverseMode.NoPassClosedDoors), validator, null, out var result3))
            {
                if ((friend.Position - pawn.Position).LengthHorizontalSquared >= 5 || (LovePartnerRelationUtility.LovePartnerRelationExists(friend, pawn) && pawn.Position != friend.Position))
                {
                    friend.mindState.nextMoveOrderIsWait = !friend.mindState.nextMoveOrderIsWait;
                    if (!result3.IsValid || pawn.mindState.nextMoveOrderIsWait)
                    {
                        friend.rotationTracker.FaceCell(pawn.Position);
                        return null;
                    }
                }

                Job job3 = new Job(RimWorld.JobDefOf.GotoWander, result3);
                friend.Map.pawnDestinationReservationManager.Reserve(friend, job3, result3);
                return job3;
            }

            friend.rotationTracker.FaceCell(pawn.Position);
            return null;
        }
    }

}

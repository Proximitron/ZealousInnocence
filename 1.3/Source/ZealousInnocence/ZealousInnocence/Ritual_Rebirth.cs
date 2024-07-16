
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI.Group;
using Verse.AI;
using Verse;
using Verse.Sound;
using Mono.Cecil;
using System.Diagnostics;
using UnityEngine;

namespace ZealousInnocence
{
    public class RitualObligationTargetWorker_AnyGraveWithPlayerPawn : RitualObligationTargetFilter
    {
        public RitualObligationTargetWorker_AnyGraveWithPlayerPawn()
        {
        }

        public RitualObligationTargetWorker_AnyGraveWithPlayerPawn(RitualObligationTargetFilterDef def)
            : base(def)
        {
        }

        public override IEnumerable<TargetInfo> GetTargets(RitualObligation obligation, Map map)
        {
            Thing thing = map.listerThings.ThingsInGroup(ThingRequestGroup.Grave).FirstOrDefault((Thing t) =>
            {
                return ((Building_Grave)t).Corpse != null && ((Building_Grave)t).Corpse.InnerPawn.IsColonist;
            } );
            if (thing != null)
            {
                yield return thing;
            }
        }

        protected override RitualTargetUseReport CanUseTargetInternal(TargetInfo target, RitualObligation obligation)
        {
            Building_Grave building_Grave;
            return target.HasThing && (building_Grave = target.Thing as Building_Grave) != null && building_Grave.Corpse != null && building_Grave.Corpse.InnerPawn.IsColonist;
        }

        public override bool ObligationTargetsValid(RitualObligation obligation)
        {
            if (obligation.targetA.HasThing)
            {
                return !obligation.targetA.ThingDestroyed;
            }

            return false;
        }

        public override IEnumerable<string> GetTargetInfos(RitualObligation obligation)
        {
            if (obligation == null)
            {
                yield return "RitualTargetEmptyGraveInfoAbstract".Translate(parent.ideo.Named("IDEO"));
                yield break;
            }

            Pawn arg = (Pawn)obligation.targetA.Thing;
            TaggedString taggedString = "RitualTargetEmptyGraveInfo".Translate(arg.Named("PAWN"));
            yield return taggedString;
        }

        public override string LabelExtraPart(RitualObligation obligation)
        {
            return ((Pawn)obligation.targetA.Thing).LabelShort;
        }
    }
    public class RitualObligationTrigger_Rebirth : RitualObligationTrigger_EveryMember
    {
        private static List<Pawn> existingObligations = new List<Pawn>();

        public override string TriggerExtraDesc => "RitualUnbladderingTriggerExtraDesc".Translate(ritual.ideo.memberName.Named("IDEOMEMBER"));

        protected override void Recache()
        {
            try 
            {
                Log.Message("debug: starting rebirth obligations ");
                if (ritual.activeObligations != null)
                {
                    ritual.activeObligations.RemoveAll((RitualObligation o) => o.targetA.Thing is Pawn pawn && pawn.Ideo != ritual.ideo);
                    foreach (RitualObligation activeObligation in ritual.activeObligations)
                    {
                        existingObligations.Add(activeObligation.targetA.Thing as Pawn);
                    }
                }

                foreach (Pawn innerPawn in PawnsFinder.All_AliveOrDead)
                {
                    if (innerPawn.Dead && innerPawn.Faction != null && innerPawn.Faction.IsPlayer && innerPawn.Corpse != null)
                    {
                        Log.Message("debug: adding rebirth pawn obligation ");
                        ritual.AddObligation(new RitualObligation(ritual, innerPawn));
                    }
                }

            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                existingObligations.Clear();
            }
        }
    }
    public class RitualObligationTrigger_RebirthProperties : RitualObligationTriggerProperties
    {
        public RitualObligationTrigger_RebirthProperties()
        {
            triggerClass = typeof(RitualObligationTrigger_Unbladdering);
        }
    }

    public class RitualObligationTrigger_PhoenixProperties : RitualObligationTriggerProperties
    {
        public RitualObligationTrigger_PhoenixProperties()
        {
            triggerClass = typeof(RitualObligationTrigger_Phoenix);
        }
    }
    public class RitualObligationTrigger_Phoenix : RitualObligationTrigger_EveryMember
    {
        private static List<Pawn> existingObligations = new List<Pawn>();

        public override string TriggerExtraDesc => "RitualBlindingTriggerExtraDesc".Translate(ritual.ideo.memberName.Named("IDEOMEMBER"));

        protected override void Recache()
        {
            try
            {
                if (ritual.activeObligations != null)
                {
                    ritual.activeObligations.RemoveAll((RitualObligation o) => o.targetA.Thing is Pawn pawn && pawn.Ideo != ritual.ideo);
                    foreach (RitualObligation activeObligation in ritual.activeObligations)
                    {
                        existingObligations.Add(activeObligation.targetA.Thing as Pawn);
                    }
                }

                foreach (Pawn innerPawn in PawnsFinder.All_AliveOrDead)
                {
                    if (innerPawn.Dead && innerPawn.Faction != null && innerPawn.Faction.IsPlayer && innerPawn.Corpse == null)
                        {
                            ritual.AddObligation(new RitualObligation(ritual, innerPawn));
                        }
                    }
                
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                existingObligations.Clear();
            }
        }
    }
    public class RitualBehaviorWorker_Rebirth : RitualBehaviorWorker
    {
        public RitualBehaviorWorker_Rebirth()
        {
        }

        public RitualBehaviorWorker_Rebirth(RitualBehaviorDef def)
            : base(def)
        {
        }

        public override string CanStartRitualNow(TargetInfo target, Precept_Ritual ritual, Pawn selectedPawn = null, Dictionary<string, Pawn> forcedForRole = null)
        {
            Building_Grave building_Grave;
            if (target.HasThing && (building_Grave = target.Thing as Building_Grave) != null && building_Grave.Corpse != null && building_Grave.Corpse.InnerPawn.IsSlave)
            {
                return "CantStartFuneralForSlave".Translate(building_Grave.Corpse.InnerPawn);
            }

            return base.CanStartRitualNow(target, ritual, selectedPawn, forcedForRole);
        }
    }
    public class RitualBehaviorWorker_Phoenix : RitualBehaviorWorker
    {
        public RitualBehaviorWorker_Phoenix()
        {
        }

        public RitualBehaviorWorker_Phoenix(RitualBehaviorDef def)
            : base(def)
        {
        }

        public override string CanStartRitualNow(TargetInfo target, Precept_Ritual ritual, Pawn selectedPawn = null, Dictionary<string, Pawn> forcedForRole = null)
        {
            Building_Grave building_Grave;
            if (target.HasThing && (building_Grave = target.Thing as Building_Grave) != null && building_Grave.Corpse != null && building_Grave.Corpse.InnerPawn.IsSlave)
            {
                return "CantStartFuneralForSlave".Translate(building_Grave.Corpse.InnerPawn);
            }

            return base.CanStartRitualNow(target, ritual, selectedPawn, forcedForRole);
        }
    }

    public class JobGiver_Phoenix : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!ModLister.CheckIdeology("Deliver to altar"))
            {
                return null;
            }


            Building_Grave building_Grave = pawn.mindState.duty.focusThird.Thing as Building_Grave;
            Corpse corpse = building_Grave.Corpse;
            if (corpse != null)
            {
                // Not phoenix, this is resurrection
                return null;
            }

            if (building_Grave != null)
            {
                if (!pawn.CanReserveAndReach(building_Grave, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    return null;
                }
            }
            else if (!pawn.CanReserveAndReach(corpse, PathEndMode.Touch, Danger.Deadly))
            {
                return null;
            }


            Job job = JobMaker.MakeJob(JobDefOf.Phoenix, corpse, building_Grave);
            job.count = 1;
            return job;
        }
    }

    public class JobGiver_Rebirth : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!ModLister.CheckIdeology("Deliver to altar"))
            {
                return null;
            }


            Building_Grave building_Grave = pawn.mindState.duty.focusThird.Thing as Building_Grave;
            Corpse corpse = building_Grave.Corpse;
            if (corpse == null)
            {
                return null;
            }

            if (building_Grave != null)
            {
                if (!pawn.CanReserveAndReach(building_Grave, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    return null;
                }
            }
            else if (!pawn.CanReserveAndReach(corpse, PathEndMode.Touch, Danger.Deadly))
            {
                return null;
            }


            Job job = JobMaker.MakeJob(JobDefOf.Rebirth, corpse, building_Grave);
            job.count = 1;
            return job;
        }
    }

    public class JobDriver_Phoenix : JobDriver
    {
        private const TargetIndex CorpseInd = TargetIndex.A;

        private const TargetIndex GraveInd = TargetIndex.B;

        private static List<IntVec3> tmpCells = new List<IntVec3>();

        private Corpse Corpse => (Corpse)job.GetTarget(TargetIndex.A).Thing;

        private Building_Grave Grave => (Building_Grave)job.GetTarget(TargetIndex.B).Thing;

        private bool InGrave => Grave != null;

        private Thing Target => (Thing)(((object)Grave) ?? ((object)Corpse));

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Target, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            if (job.GetTarget(TargetIndex.A).Thing.DestroyedOrNull())
            {
                Toil execute = new Toil();
                execute.initAction = delegate
                {
                    foreach (Pawn innerPawn in PawnsFinder.All_AliveOrDead)
                    {
                        if (innerPawn.IsColonist)
                        {
                            Log.Message("debug: colonist" + innerPawn.LabelShort);
                        }
                        if (innerPawn.Dead && innerPawn.Faction != null && innerPawn.Faction.IsPlayer && innerPawn.Corpse == null)
                        {
                            Log.Message("debug: grave" + Grave.ToString());
                            Log.Message("debug: grave" + innerPawn.LabelShort);

                            var tempCorps = innerPawn.MakeCorpse(Grave, false, 0.0f);
                            Grave.TryAcceptThing(tempCorps);
                            if (Grave.HasCorpse)
                            {
                                //ritual.AddObligation(new RitualObligation(ritual, innerPawn));
                            }
                        }
                    }
                };
                execute.defaultCompleteMode = ToilCompleteMode.Instant;
                execute.activeSkill = () => SkillDefOf.Intellectual;
                yield return execute;
            }
            else
            {
                Log.Message("debug: not destroyed");
            }
        }


    }

    public class JobDriver_Rebirth : JobDriver
    {
        private const TargetIndex CorpseInd = TargetIndex.A;

        private const TargetIndex GraveInd = TargetIndex.B;

        private static List<IntVec3> tmpCells = new List<IntVec3>();

        private Corpse Corpse => (Corpse)job.GetTarget(TargetIndex.A).Thing;

        private Building_Grave Grave => (Building_Grave)job.GetTarget(TargetIndex.B).Thing;

        private bool InGrave => Grave != null;

        private Thing Target => (Thing)(((object)Grave) ?? ((object)Corpse));

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Target, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            if (!job.GetTarget(TargetIndex.A).Thing.DestroyedOrNull())
            {
                Log.Message("debug: resurrect" + Grave.ToString());
                Log.Message("debug: resurrect" + Corpse.LabelShort);
                this.FailOnDestroyedOrNull(TargetIndex.A);
                Toil gotoCorpse = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);
                yield return Toils_Jump.JumpIfTargetInvalid(TargetIndex.B, gotoCorpse);
                yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell).FailOnDespawnedOrNull(TargetIndex.B);
                yield return Toils_General.Wait(Grave.OpenTicks).WithProgressBarToilDelay(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.B)
                    .FailOnCannotTouch(TargetIndex.B, PathEndMode.InteractionCell);
                yield return Toils_General.Open(TargetIndex.B);
                yield return Toils_Reserve.Reserve(TargetIndex.A);
                yield return gotoCorpse;
                yield return Toils_General.Do(delegate
                {
                    Pawn innerPawn = Corpse.InnerPawn;
                    float x2 = ((Corpse == null) ? 0f : (Corpse.GetComp<CompRottable>().RotProgress / 60000f));
                    ResurrectionUtility.Resurrect(innerPawn);

                    Messages.Message("MessagePawnResurrected".Translate(innerPawn), innerPawn, MessageTypeDefOf.PositiveEvent);
                    SoundDefOf.MechSerumUsed.PlayOneShot(SoundInfo.InMap(innerPawn));
                    //BodyPartRecord brain = innerPawn.health.hediffSet.GetBrain();

                    RegressionHelper.regressPawn(innerPawn);
                });
            }
        }

    }
}


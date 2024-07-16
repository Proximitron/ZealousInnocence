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

namespace ZealousInnocence
{
    public class RitualObligationTargetWorker_AnyRitualSpotOrAltar_Unbladdering : RitualObligationTargetWorker_AnyRitualSpotOrAltar
    {
        public RitualObligationTargetWorker_AnyRitualSpotOrAltar_Unbladdering()
        {
        }

        public RitualObligationTargetWorker_AnyRitualSpotOrAltar_Unbladdering(RitualObligationTargetFilterDef def)
            : base(def)
        {
        }

        public override bool ObligationTargetsValid(RitualObligation obligation)
        {
            Pawn pawn;
            if ((pawn = obligation.targetA.Thing as Pawn) != null)
            {
                if (pawn.Dead)
                {
                    return false;
                }

                return pawn.health.hediffSet.GetNotMissingParts().Any((BodyPartRecord p) => p.def == BodyPartDefOf.Bladder);
            }

            return false;
        }
    }
    public class RitualOutcomeEffectWorker_Unbladdering : RitualOutcomeEffectWorker_FromQuality
    {
        public const float PsylinkGainChance = 0.5f;

        public RitualOutcomeEffectWorker_Unbladdering()
        {
        }

        public RitualOutcomeEffectWorker_Unbladdering(RitualOutcomeEffectDef def)
            : base(def)
        {
        }

        protected override void ApplyExtraOutcome(Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual, OutcomeChance outcome, out string extraOutcomeDesc, ref LookTargets letterLookTargets)
        {
            extraOutcomeDesc = null;
            if (!ModsConfig.RoyaltyActive || !outcome.Positive) return;

            Pawn pawn = ((LordJob_Ritual_Mutilation)jobRitual).mutilatedPawns[0];
            if (outcome.BestPositiveOutcome(jobRitual) || Rand.ChanceSeeded(PsylinkGainChance, pawn.HashOffsetTicks()))
            {
                
                extraOutcomeDesc = "RitualOutcomeExtraDesc_UnbladderingPsylink".Translate(pawn.Named("PAWN"));
                List<Ability> existingAbils = pawn.abilities.AllAbilitiesForReading.ToList();
                pawn.ChangePsylinkLevel(1);
                Ability ability = pawn.abilities.AllAbilitiesForReading.FirstOrDefault((Ability a) => !existingAbils.Contains(a));
                if (ability != null)
                {
                    // Neutral text, i took the existing translation from blinding
                    extraOutcomeDesc += " " + "RitualOutcomeExtraDesc_BlindingPsylinkAbility".Translate(ability.def.LabelCap, pawn.Named("PAWN"));
                }
            }
        }
    }
    public class RitualObligationTrigger_Unbladdering : RitualObligationTrigger_EveryMember
    {
        private static List<Pawn> existingObligations = new List<Pawn>();

        public override string TriggerExtraDesc => "RitualUnbladderingTriggerExtraDesc".Translate(ritual.ideo.memberName.Named("IDEOMEMBER"));

        protected override void Recache()
        {
            try
            {
                if (ritual.activeObligations != null)
                {
                    ritual.activeObligations.RemoveAll(delegate (RitualObligation o)
                    {
                        Pawn pawn = o.targetA.Thing as Pawn;
                        return pawn != null && pawn.Ideo != ritual.ideo;
                    });
                    foreach (RitualObligation activeObligation in ritual.activeObligations)
                    {
                        existingObligations.Add(activeObligation.targetA.Thing as Pawn);
                    }
                }

                foreach (Pawn allMapsCaravansAndTravelingTransportPods_Alive_Colonist in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists)
                {
                    if (!existingObligations.Contains(allMapsCaravansAndTravelingTransportPods_Alive_Colonist) && allMapsCaravansAndTravelingTransportPods_Alive_Colonist.Ideo != null) //!allMapsCaravansAndTravelingTransportPods_Alive_Colonist.IsPrisoner
                    {
                        if (allMapsCaravansAndTravelingTransportPods_Alive_Colonist.health.hediffSet.GetNotMissingParts().Any((BodyPartRecord p) => p.def == BodyPartDefOf.Bladder))
                        {
                            ritual.AddObligation(new RitualObligation(ritual, allMapsCaravansAndTravelingTransportPods_Alive_Colonist, expires: false));
                        }
                    }
                }
            }
            finally
            {
                existingObligations.Clear();
            }
        }
    }
    public class RitualObligationTrigger_UnbladderingProperties : RitualObligationTriggerProperties
    {
        public RitualObligationTrigger_UnbladderingProperties()
        {
            triggerClass = typeof(RitualObligationTrigger_Unbladdering);
        }
    }
    public class ThoughtWorker_Precept_Bladderless : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            return !p.health.capacities.CapableOf(PawnCapacityDefOf.BladderControl);
        }
    }
    public class ThoughtWorker_Precept_Bladderless_Social : ThoughtWorker_Precept_Social
    {
        protected override ThoughtState ShouldHaveThought(Pawn p, Pawn otherPawn)
        {
            return !otherPawn.health.capacities.CapableOf(PawnCapacityDefOf.BladderControl);
        }
    }

    public class ThoughtWorker_Precept_HalfUnbladdered : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            return IsHalfUnbladdered(p);
        }

        public static bool IsHalfUnbladdered(Pawn p)
        {
            if (!p.health.capacities.CapableOf(PawnCapacityDefOf.BladderControl))
            {
                return false;
            }

            return DiaperHelper.getBladderControlLevel(p) <= 0.5f;
        }
    }
    public class ThoughtWorker_Precept_HalfUnbladdered_Social : ThoughtWorker_Precept_Social
    {
        protected override ThoughtState ShouldHaveThought(Pawn p, Pawn otherPawn)
        {
            return ThoughtWorker_Precept_HalfUnbladdered.IsHalfUnbladdered(otherPawn);
        }
    }
    public class ThoughtWorker_Precept_Bladdered : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            return p.health.capacities.CapableOf(PawnCapacityDefOf.BladderControl);
        }
    }
    public class ThoughtWorker_Precept_Bladdered_Social : ThoughtWorker_Precept_Social
    {
        protected override ThoughtState ShouldHaveThought(Pawn p, Pawn otherPawn)
        {
            return otherPawn.health.capacities.CapableOf(PawnCapacityDefOf.BladderControl);
        }
    }
    
    public class JobGiver_Unbladder : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            Lord lord = pawn.GetLord();
            LordJob_Ritual_Mutilation lordJob_Ritual_Mutilation;
            if (lord == null || (lordJob_Ritual_Mutilation = lord.LordJob as LordJob_Ritual_Mutilation) == null)
            {
                return null;
            }

            Pawn pawn2 = pawn.mindState.duty.focusSecond.Pawn;
            if (lordJob_Ritual_Mutilation.mutilatedPawns.Contains(pawn2) || !pawn.CanReserveAndReach(pawn2, PathEndMode.ClosestTouch, Danger.None))
            {
                return null;
            }

            return JobMaker.MakeJob(JobDefOf.Unbladder, pawn2, pawn.mindState.duty.focus);
        }
    }
    public class JobDriver_Unbladder : JobDriver
    {
        private const TargetIndex TargetPawnIndex = TargetIndex.A;

        protected Pawn Target => (Pawn)job.GetTarget(TargetIndex.A).Thing;

        public static void Unbladder(Pawn pawn, Pawn doer)
        {
            Lord lord = pawn.GetLord();
            IEnumerable<BodyPartRecord> enumerable = from p in pawn.health.hediffSet.GetNotMissingParts()
                                                     where p.def == BodyPartDefOf.Bladder
                                                     select p;
            LordJob_Ritual_Mutilation lordJob_Ritual_Mutilation;
            if (lord != null && (lordJob_Ritual_Mutilation = lord.LordJob as LordJob_Ritual_Mutilation) != null && enumerable.Count() == 1)
            {
                lordJob_Ritual_Mutilation.mutilatedPawns.Add(pawn);
            }

            foreach (BodyPartRecord item in enumerable)
            {
                if (item.def == BodyPartDefOf.Bladder)
                {
                    pawn.TakeDamage(new DamageInfo(DamageDefOf.SurgicalCut, 99999f, 999f, -1f, null, item));
                    break;
                }
            }

            if (pawn.Dead)
            {
                ThoughtUtility.GiveThoughtsForPawnExecuted(pawn, doer, PawnExecutionKind.GenericBrutal);
            }
        }

        public static void CreateHistoryEventDef(Pawn pawn)
        {
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.BladderControl))
            {
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.GotUnbladdered, pawn.Named(HistoryEventArgsNames.Doer)));
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Target, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            Pawn target = Target;
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return Toils_General.Wait(35);
            Toil scarify = new Toil
            {
                initAction = delegate
                {
                    Unbladder(target, pawn);
                    CreateHistoryEventDef(target);
                    SoundDefOf.Execute_Cut.PlayOneShot(target);
                    if (target.RaceProps.BloodDef != null)
                    {
                        CellRect cellRect = new CellRect(target.PositionHeld.x - 1, target.PositionHeld.z - 1, 3, 3);
                        for (int i = 0; i < 3; i++)
                        {
                            Rand.PushState(target.HashOffsetTicks());
                            IntVec3 randomCell = cellRect.RandomCell;
                            Rand.PopState();
                            bool isOk = randomCell.InBounds(base.Map);
                            
                            if (GenSight.LineOfSight(randomCell, target.PositionHeld, base.Map))
                            {
                                FilthMaker.TryMakeFilth(randomCell, target.MapHeld, target.RaceProps.BloodDef, target.LabelIndefinite());
                            }
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return Toils_General.Wait(120);
            yield return scarify;
        }
    }

}

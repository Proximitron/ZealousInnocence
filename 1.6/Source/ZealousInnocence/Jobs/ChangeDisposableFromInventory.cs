using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ZealousInnocence
{
    public class JobGiver_ChangeDisposableIfWornUsed : ThinkNode_JobGiver
    {
        private const float SwapAtPct = 0.50f;    // swap when worn <50% (tune)

        public override float GetPriority(Pawn pawn)
        {
            Apparel_Disposable_Diaper worn = (Apparel_Disposable_Diaper)pawn.apparel?.WornApparel?.FirstOrDefault(a => a is Apparel_Disposable_Diaper);
            if (worn == null) return 0f;

            if (pawn.CurJobDef == JobDefOf.ChangeDisposableFromInventory) return 0f;

            var diaperNeed = pawn.needs.TryGetNeed<Need_Diaper>();
            if (diaperNeed == null) return 0f; // no diaper need? No diaper change!

            if (!Helper_Regression.canChangeDiaperOrUnderwear(pawn)) return 0f;
            if (Helper_Diaper.shouldStayPut(pawn)) return 0f;

            int have = Apparel_Disposable_Diaper.SparesOfDiaper(pawn, worn);
            if (have <= 0) return 0f; // Can't

            // High priority
            if (worn.HitPoints <= worn.MaxHitPoints * SwapAtPct) return 9f;

            return 0f;
        }
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn?.apparel == null || pawn.inventory == null) return null;
            if (!pawn.Spawned || pawn.Dead || pawn.Downed) return null;

            // Are we having an accident right now?
            var diaperNeed = pawn.needs.TryGetNeed<Need_Diaper>();
            if (diaperNeed == null) return null; // no diaper need? No diaper change!
            if (diaperNeed.IsHavingAccident) return null; // Not while having an accident...

            // Find worn diaper
            Apparel_Disposable_Diaper worn = (Apparel_Disposable_Diaper)pawn.apparel?.WornApparel?.FirstOrDefault(a => a is Apparel_Disposable_Diaper);
            if (worn == null) return null;

            // Is it below swap threshold?
            if (worn.HitPoints > worn.MaxHitPoints * SwapAtPct) return null;

            // Do we have a fresh spare in INVENTORY (≥90%)?
            bool HasFreshSpare() => Apparel_Disposable_Diaper.SparesOfDiaper(pawn,worn) > 0;

            if (!HasFreshSpare()) return null;

            //h.tick = Find.TickManager.TicksGame;
            return JobMaker.MakeJob(JobDefOf.ChangeDisposableFromInventory);
        }
    }
    public class JobDriver_ChangeDisposableFromInventory : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;
        private Apparel Spare => job.targetA.Thing as Apparel;
        protected override IEnumerable<Toil> MakeNewToils()
        {
            Apparel_Disposable_Diaper worn = (Apparel_Disposable_Diaper)pawn.apparel?.WornApparel?.FirstOrDefault(a => a is Apparel_Disposable_Diaper);

            var toil = ToilMaker.MakeToil("ChangeDisposableFromInventory");
            toil.initAction = () =>
            {
                if (pawn?.inventory == null || pawn.apparel == null) { EndJobWith(JobCondition.Incompletable); return; }

                // Find a fresh spare in INVENTORY
                if(Spare == null)
                {
                    Thing FoundSpare = pawn?.inventory?.innerContainer?.FirstOrDefault(t => t is Apparel_Disposable_Diaper && t.def == worn.def);

                    if (FoundSpare == null) { EndJobWith(JobCondition.Incompletable); return; }

                    job.SetTarget(TargetIndex.A, FoundSpare);
                }

            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;

            
            if (worn != null)
            {
                Toil removeOld = Toils_General.Wait(150); // 2.5 seconds at 60 ticks per second 
                removeOld.initAction = () =>
                {
                    SoundStarter.PlayOneShotOnCamera(DiaperChangie.Diapertape, pawn.Map);
                };
                removeOld.defaultCompleteMode = ToilCompleteMode.Delay;
                removeOld.WithProgressBarToilDelay(TargetIndex.None);
                removeOld.AddFinishAction(() =>
                {

                    if (worn != null)
                    {
                        Apparel resultingAp;
                        pawn.apparel.TryDrop(worn, out resultingAp, this.pawn.PositionHeld, true);
                    }

                });
                yield return removeOld;
            }


            
            Toil seperateSingle = new Toil();
            seperateSingle.initAction = () =>
            {

                if (Spare != null)
                {
                    var SpareSingle = pawn.MoveSingleFromInventoryToHands(Spare);
                    if(SpareSingle == null)
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


            Toil changingProgress = Toils_General.Wait(900); // 15 seconds
            changingProgress.tickAction = () =>
            {
                if (this.pawn.IsHashIntervalTick(250)) // 4.16 seconds
                {
                    SoundStarter.PlayOneShotOnCamera(DiaperChangie.Diapertape, pawn.Map);
                }
            };
            changingProgress.WithProgressBarToilDelay(TargetIndex.None);
            changingProgress.defaultCompleteMode = ToilCompleteMode.Delay;
            yield return changingProgress;

            Toil wearNewDiaper = new Toil();
            wearNewDiaper.initAction = () =>
            {

                if (Spare != null)
                {
                    pawn.carryTracker.innerContainer.Remove(Spare);
                    pawn.apparel.Wear(Spare);
                }
                else
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
            };
            wearNewDiaper.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return wearNewDiaper;

        }
    }

    public class JobGiver_RestockDisposables : ThinkNode_JobGiver
    {
        private const int ScanRadius = 24;

        public override float GetPriority(Pawn pawn)
        {
            var diaperNeed = pawn.needs.TryGetNeed<Need_Diaper>();
            if (diaperNeed == null) return 0f; // no diaper need? No diaper change!

            if (pawn.CurJobDef == JobDefOf.RestockDisposableToInventory) return 0f;

            if (!Helper_Regression.canChangeDiaperOrUnderwear(pawn)) return 0f;
            if (Helper_Diaper.shouldStayPut(pawn)) return 0f;

            Apparel_Disposable_Diaper worn = (Apparel_Disposable_Diaper)pawn.apparel?.WornApparel?.FirstOrDefault(a => a is Apparel_Disposable_Diaper);
            if (worn == null) return 0f;

            int DesiredCount = GetDesiredSpareCountFor(pawn, 0);

            int have = Apparel_Disposable_Diaper.SparesOfDiaper(pawn, worn);

            if (have >= DesiredCount) return 0f;

            if (have > 0)
            {
                // Opportunistic
                return 3f;
            } else {
                // High priority
                if (worn.HitPoints < worn.MaxHitPoints / 2) return 9f;
                
                // Medium priority
                return 5f;
            }
        }
        public static CompRegressionMemory GetMemory(Pawn pawn)
        {
            return pawn?.TryGetComp<CompRegressionMemory>();
        }
        public static int GetDesiredSpareCountFor(Pawn pawn, int fallback = 2)
        {
            CompRegressionMemory Memory = GetMemory(pawn);
            if (Memory == null) return fallback;
            return Memory.targetDisposablesCount;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn?.inventory == null) return null;
            if (!pawn.Spawned || pawn.Dead || pawn.Downed) return null;


            int have = Apparel_Disposable_Diaper.SparesOfCurrentDiaper(pawn);
            int DesiredCount = GetDesiredSpareCountFor(pawn,0);

            if (have >= DesiredCount) return null;
            // find a nearby fresh stack on map
            Thing found = GenClosest.ClosestThingReachable(
                pawn.Position, pawn.Map,
                ThingRequest.ForDef(ThingDefOf.Apparel_Diaper_Disposable),
                PathEndMode.ClosestTouch,
                TraverseParms.For(pawn),
                maxDistance: ScanRadius,
                validator: t => t.Spawned && pawn.CanReserve(t) && !t.IsForbidden(pawn) && t.HitPoints > t.MaxHitPoints / 2);

            if (found == null) return null;

            int need = DesiredCount - have;
            var job = JobMaker.MakeJob(JobDefOf.RestockDisposableToInventory, found);
            job.count = Mathf.Min(need, found.stackCount);
            return job;
        }
    }

    // Simple driver: reserve → goto → take to inventory
    public class JobDriver_RestockDisposableToInventory : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
            => pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A);

            yield return Toils_Haul.TakeToInventory(TargetIndex.A, job.count)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A);
        }
    }
}

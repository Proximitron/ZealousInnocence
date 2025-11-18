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

namespace ZealousInnocence
{
    /*public class WorkGiver_ChangePatientDiaper : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

        private LocalTargetInfo cachedDiaperTarget;
        private float lastCacheUpdateTime;
        private const float CacheUpdateInterval = 1f; // 1 second

        public LocalTargetInfo getCachedBestDiaperOrUndie(Pawn caretaker, Pawn patient)
        {
            if (Time.realtimeSinceStartup - lastCacheUpdateTime > CacheUpdateInterval)
            {
                cachedDiaperTarget = Need_Diaper.getBestDiaperOrUndie(caretaker, patient);
                lastCacheUpdateTime = Time.realtimeSinceStartup;
            }
            return cachedDiaperTarget;
        }

        public override bool HasJobOnThing(Pawn caretaker, Thing t, bool forced = false)
        {
            JobFailReason.Clear();
            if (t is not Pawn patient || patient == caretaker)
            {
                return false;
            }
            bool isRealGuest = patient.HostFaction != null && patient.HostFaction != patient.Faction && patient.guest != null && patient.guest.GuestStatus == GuestStatus.Guest;
            bool isWardenJob = patient.IsPrisoner || patient.IsSlaveOfColony || isRealGuest;
            if (!forced)
            {
                if(caretaker.workSettings == null || def.workType == null)
                {
                    return false;
                }
                if (!caretaker.workSettings.WorkIsActive(def.workType))
                {
                    JobFailReason.Is("Work settings prevent.");
                    return false;
                }
                

                if (def.workType == WorkTypeDefOf.Childcare)
                {
                    if (!patient.isToddlerOrBabyMentalOrPhysical()) return false;
                }
                else if(def.workType == WorkTypeDefOf.Warden)
                {
                    if (!isWardenJob) return false;
                }
                else if (def.workType == WorkTypeDefOf.Doctor || def.workType == DefDatabase<WorkTypeDef>.GetNamedSilentFail("FSFNurse"))
                {
                    if (!patient.InBed()) return false;
                }
            }

            if (!isWardenJob && Helper_Regression.canChangeDiaperOrUnderwear(patient) && !Helper_Diaper.shouldStayPut(patient))
            {
                JobFailReason.Is("Can do it themselfs.");
                return false;
            }

            if (JobDriver_ChangePatientDiaper.immediateFailReasons(patient, caretaker))
            {
                if (patient.GetPosture() == PawnPosture.Standing && !patient.isToddlerOrBabyMentalOrPhysical())
                {
                    JobFailReason.Is("Needs to be in a bed.");
                }
                return false;
            }

            var oldDiaper = Helper_Diaper.getUnderwearOrDiaper(patient);
            Need_Diaper need_diaper = patient.needs.TryGetNeed<Need_Diaper>();

            if (oldDiaper != null && Helper_Diaper.allowedByPolicy(patient, oldDiaper) && need_diaper.CurLevel >= 0.5f)
            {
                var needDiaper = Helper_Diaper.needsDiaper(patient);
                if (needDiaper == Helper_Diaper.isDiaper(oldDiaper))
                {
                    var needDiaperButIsNight = needDiaper && Helper_Diaper.isNightDiaper(oldDiaper);
                    
                    if (!needDiaperButIsNight)
                    {
                        JobFailReason.Is("No change needed.");
                        return false;
                    }
                }
            }

            if (!caretaker.CanReserve(patient, 1, -1, null, forced)) return false;
            
            LocalTargetInfo a = Need_Diaper.getBestDiaperOrUndie(caretaker, patient);
            if (a == null || !a.IsValid || !a.HasThing)
            {
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn caretaker, Thing t, bool forced = false)
        {
            if (t is not Pawn patient || patient == caretaker)
            {
                return null;
            }
            if (!HasJobOnThing(caretaker, t, forced)) return null;

            LocalTargetInfo a = Need_Diaper.getBestDiaperOrUndie(caretaker, patient);
            if (a == null || !a.IsValid || !a.HasThing)
            {
                return null;
            }

            Job job2 = JobMaker.MakeJob(JobDefOf.ChangePatientDiaper, a.Thing, patient);
            job2.count = 1;
            return job2;
        }
    }*/
    public class WorkGiver_ChangePatientDiaper : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

        // ---------------------------------------------
        // Per-patient cooldown for periodic checks
        // ---------------------------------------------
        private static readonly Dictionary<int, int> nextCheckTickByPawnId = new();
        private const int CheckIntervalTicks = 2500; // ~41 seconds; tune as desired

        private static bool ReadyForPeriodicCheck(Pawn patient)
        {
            if (patient == null || !patient.Spawned) return false;
            int now = Find.TickManager.TicksGame;
            int id = patient.thingIDNumber;

            if (!nextCheckTickByPawnId.TryGetValue(id, out var allowedTick))
                return true; // never checked before → allow

            return now >= allowedTick;
        }

        private static void ScheduleNextPeriodicCheck(Pawn patient)
        {
            if (patient == null) return;
            int now = Find.TickManager.TicksGame;
            nextCheckTickByPawnId[patient.thingIDNumber] = now + CheckIntervalTicks;
        }

        // ---------------------------------------------
        // Optional caching for diaper/undie search
        // ---------------------------------------------
        private LocalTargetInfo cachedDiaperTarget;
        private float lastCacheUpdateTime;
        private const float CacheUpdateInterval = 1f; // 1 second

        public LocalTargetInfo getCachedBestDiaperOrUndie(Pawn caretaker, Pawn patient)
        {
            if (Time.realtimeSinceStartup - lastCacheUpdateTime > CacheUpdateInterval)
            {
                cachedDiaperTarget = Need_Diaper.getBestDiaperOrUndie(caretaker, patient);
                lastCacheUpdateTime = Time.realtimeSinceStartup;
            }
            return cachedDiaperTarget;
        }

        public override bool HasJobOnThing(Pawn caretaker, Thing t, bool forced = false)
        {
            JobFailReason.Clear();

            if (t is not Pawn patient || patient == caretaker)
                return false;

            Need_Diaper need_diaper = patient.needs?.TryGetNeed<Need_Diaper>();
            if (need_diaper == null) return false; // Doesn't/never needs diapers

            bool isRealGuest = patient.HostFaction != null && patient.HostFaction != patient.Faction &&
                               patient.guest != null && patient.guest.GuestStatus == GuestStatus.Guest;
            bool isWardenJob = patient.IsPrisoner || patient.IsSlaveOfColony || isRealGuest;

            if (!forced)
            {
                if (caretaker.workSettings == null || def.workType == null)
                    return false;

                if (!caretaker.workSettings.WorkIsActive(def.workType))
                {
                    JobFailReason.Is("Work settings prevent.");
                    return false;
                }

                if (def.workType == WorkTypeDefOf.Childcare)
                {
                    if (!patient.isToddlerOrBabyMentalOrPhysical())
                        return false;
                }
                else if (def.workType == WorkTypeDefOf.Warden)
                {
                    if (!isWardenJob)
                        return false;
                }
                else if (def.workType == WorkTypeDefOf.Doctor ||
                         def.workType == DefDatabase<WorkTypeDef>.GetNamedSilentFail("FSFNurse"))
                {
                    if (!patient.InBed())
                        return false;
                }
            }

            // If they can handle it themselves, no job
            if (!isWardenJob && Helper_Regression.canChangeDiaperOrUnderwear(patient) &&
                !Helper_Diaper.shouldStayPut(patient))
            {
                JobFailReason.Is("Can do it themselfs.");
                return false;
            }

            // Immediate fail reasons from the driver
            if (JobDriver_ChangePatientDiaper.immediateFailReasons(patient, caretaker))
            {
                if (patient.GetPosture() == PawnPosture.Standing && !patient.isToddlerOrBabyMentalOrPhysical())
                {
                    JobFailReason.Is("Needs to be in a bed.");
                }
                return false;
            }

            if (!caretaker.CanReserve(patient, 1, -1, null, forced))
                return false;

            /*var oldDiaper = Helper_Diaper.getUnderwearOrDiaper(patient);
            if (oldDiaper != null)
            {
                if (patient.outfits?.forcedHandler != null && patient.outfits.forcedHandler.IsForced(oldDiaper))
                {
                    JobFailReason.Is("ShortReasonForcedInDiaper".Translate());
                    return false;
                }

                if (!Helper_Diaper.allowedByPolicy(patient, oldDiaper))
                {
                    JobFailReason.Is("No change needed.");
                    return false;
                }
            }*/
            



            // -------------------------------------------------
            // Unable to do self: periodic "check" gating
            // -------------------------------------------------
            bool isBabyOrToddler = !patient.canChangeDiaperOrUnderwear();
            if (isBabyOrToddler)
            {
                // Only check periodically; if not ready yet, skip.
                if (!need_diaper.KnowsNeedChange && !ReadyForPeriodicCheck(patient))
                {
                    JobFailReason.Is("Next check not yet.");
                    return false;
                }
            }
            else
            {
                if (patient.needsDiaperChange() && !need_diaper.KnowsNeedChange)
                {
                    JobFailReason.Is("Is unaware of diaper state.");
                    return false;
                }
                if (!need_diaper.KnowsNeedChange)
                {
                    JobFailReason.Is("No change needed.");
                    return false;
                }
            }

            LocalTargetInfo diaperTarget = getCachedBestDiaperOrUndie(caretaker, patient);
            if (!diaperTarget.IsValid || !diaperTarget.HasThing)
                return false;

            return true;
        }

        public override Job JobOnThing(Pawn caretaker, Thing t, bool forced = false)
        {
            if (t is not Pawn patient || patient == caretaker)
                return null;

            if (!HasJobOnThing(caretaker, t, forced))
                return null;

            Need_Diaper need_diaper = patient.needs.TryGetNeed<Need_Diaper>();
            bool isBabyOrToddler = !patient.canChangeDiaperOrUnderwear();
            // Schedule next periodic check for babies/toddlers
            if (isBabyOrToddler && !need_diaper.KnowsNeedChange)
            {
                Log.Message($"[ZI] Trigger CheckPatientDiaper for {patient.LabelShort}");
                ScheduleNextPeriodicCheck(patient);
                Job checkJob = JobMaker.MakeJob(JobDefOf.CheckPatientDiaper, null, patient);
                checkJob.count = 1;
                return checkJob;
            }
            if (!need_diaper.KnowsNeedChange)
                return null;

            LocalTargetInfo diaperTarget = getCachedBestDiaperOrUndie(caretaker, patient);
            if (!diaperTarget.IsValid || !diaperTarget.HasThing)
                return null;

            Log.Message($"[ZI] Trigger ChangePatientDiaper for {patient.LabelShort}");
            Job job = JobMaker.MakeJob(JobDefOf.ChangePatientDiaper, diaperTarget.Thing, patient);
            job.count = 1;
            return job;
        }
    }
}

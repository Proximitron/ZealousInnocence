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
    public class WorkGiver_ChangePatientDiaper : WorkGiver_Scanner
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
            bool isGuest = patient.guest != null && patient.guest.GuestStatus == GuestStatus.Guest;
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
                if(def.workType == WorkTypeDefOf.Childcare)
                {
                    if (!patient.isToddlerOrBabyMentalOrPhysical()) return false;
                }
                else if(def.workType == WorkTypeDefOf.Warden)
                {
                    if (!patient.IsPrisoner && !patient.IsSlaveOfColony && !isGuest) return false;
                }
                else if (def.workType == WorkTypeDefOf.Doctor || def.workType == DefDatabase<WorkTypeDef>.GetNamedSilentFail("FSFNurse"))
                {
                    if (!Helper_Diaper.shouldStayPut(patient)) return false;
                }
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

           
            
            if (!isGuest && !patient.IsPrisoner && !patient.IsSlaveOfColony && Helper_Regression.canChangeDiaperOrUnderwear(patient) && !Helper_Diaper.shouldStayPut(patient))
            {
                JobFailReason.Is("Is big enough to do it themselfs.");
                return false;
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
    }
}

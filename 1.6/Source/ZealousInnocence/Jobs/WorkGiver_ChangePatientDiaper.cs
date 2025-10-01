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

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            if (this.def.workType == WorkTypeDefOf.Warden)
            {
                foreach (Pawn pawn2 in pawn.Map.mapPawns.SlavesAndPrisonersOfColonySpawned)
                {
                    Need_Diaper need_diaper = pawn2.needs.TryGetNeed<Need_Diaper>();
                    if (pawn2.needs.food != null && need_diaper != null && need_diaper.CurLevel < 0.5f)
                    {
                        yield return pawn2;
                    }
                }
            }
            else
            {
                foreach (Pawn pawn3 in pawn.Map.mapPawns.FreeColonistsSpawned)
                {
                    Need_Diaper need_diaper = pawn3.needs.TryGetNeed<Need_Diaper>();
                    if (pawn3.needs.food != null && need_diaper != null && need_diaper.CurLevel < 0.5f)
                    {
                        yield return pawn3;
                    }
                }
            }
            yield break;
        }
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

            var diaper = Helper_Diaper.getUnderwearOrDiaper(patient);
            Need_Diaper need_diaper = patient.needs.TryGetNeed<Need_Diaper>();

            if (diaper != null && Helper_Diaper.allowedByPolicy(patient, diaper) && need_diaper.CurLevel >= 0.5f)
            {
                JobFailReason.Is("No change needed.");
                return false;
            }
            if (patient.GetPosture() == PawnPosture.Standing)
            {
                JobFailReason.Is("Needs to lie down.");
                return false;
            }

            if (Helper_Regression.canChangeDiaperOrUnderwear(patient))
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

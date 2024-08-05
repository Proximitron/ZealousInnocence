using DubsBadHygiene;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

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
                foreach (Pawn pawn3 in pawn.Map.mapPawns.AllPawnsSpawned)
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

        public Job TryRunJob(Pawn pawn, Pawn patient, Need_Diaper thirst)
        {
            LocalTargetInfo a = null;
            /*if (/ != null)
            {
                a = pawn.inventory.innerContainer.FirstOrDefault((Thing x) => Helper_Diaper.isDiaper(x);
            }
            */
            /*if (a.IsValid && a.HasThing)
            {
                Job job = JobMaker.MakeJob(JobDefOf.ChangePatientDiaper, a.Thing, patient);
                job.count = 1;
                return job;
            }
            */
            a = FindBestDiaper(pawn);
            if (a == null || !a.IsValid)
            {
                return null;
            }
            if (a.HasThing)
            {
                Job job2 = JobMaker.MakeJob(JobDefOf.ChangePatientDiaper, a.Thing, patient);
                job2.count = 1;
                return job2;
            }
            return null;
        }

        private LocalTargetInfo FindBestDiaper(Pawn pawn)
        {
            foreach (Thing thing in pawn.Map.listerThings.AllThings)
            {
                if(thing is Apparel app)
                {
                    if (Helper_Diaper.isDiaper(app) && app.HitPoints > (app.MaxHitPoints / 2) && pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        return thing;
                    }
                }

            }
            return LocalTargetInfo.Invalid;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Pawn patient || patient == pawn)
            {
                return false;
            }

            Need_Diaper need_diaper = patient.needs.TryGetNeed<Need_Diaper>();
            if (need_diaper == null || need_diaper.CurLevel >= 0.5f || Helper_Diaper.getDiaper(patient) == null)
            {
                return false;
            }

            return FeedPatientUtility.ShouldBeFed(patient) && pawn.CanReserve(patient, 1, -1, null, forced) && TryRunJob(pawn, patient, need_diaper) != null;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Pawn patient || patient == pawn)
            {
                return null;
            }

            Need_Diaper need_diaper = patient.needs.TryGetNeed<Need_Diaper>();
            if (need_diaper == null || need_diaper.CurLevel >= 0.5f)
            {
                return null;
            }

            return TryRunJob(pawn, patient, need_diaper);
        }
    }
}

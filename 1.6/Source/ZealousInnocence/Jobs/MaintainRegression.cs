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
    public class JobGiver_FOY_MaintainRegression : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            var mem = pawn.TryGetComp<CompRegressionMemory>();
            if (mem == null) return 0f;
            return ShouldDoseNow(pawn, out _) ? 9.4f : 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            var mem = GetMemory(pawn);
            if (mem == null) return null;
            if (!ShouldDoseNow(pawn, out var why)) return null;

            // 1) Inventory first
            Thing vial = pawn.inventory?.innerContainer?
                .FirstOrDefault(t => t.def == ThingDefOf.ZI_Foy_Vial && t.stackCount > 0);

            // 2) Else map search (nearby and reachable)
            if (vial == null)
            {
                vial = GenClosest.ClosestThingReachable(
                    pawn.Position, pawn.Map,
                    ThingRequest.ForDef(ThingDefOf.ZI_Foy_Vial),
                    PathEndMode.Touch,
                    TraverseParms.For(pawn),
                    40f, // search radius
                    t => !t.IsForbidden(pawn) && pawn.CanReserve(t));
            }

            if (vial == null) return null;


            var job = JobMaker.MakeJob(RimWorld.JobDefOf.Ingest, vial);
            job.count = 1;                      // force a single 10% dose
            job.checkOverrideOnExpire = true;

            // Record cooldown
            mem.desiredAgeDoseTick = Find.TickManager.TicksGame;

            return job;
        }


        public const int MinIntervalTicks = 6000; // 1 in-game hour
        public const float ExtraHysteresis = 0.005f;

        public static CompRegressionMemory GetMemory(Pawn pawn)
        {
            return pawn.TryGetComp<CompRegressionMemory>();
        }
        public static float GetFoySeverityPerDose()
        {
            var def = ThingDefOf.ZI_Foy_Vial;
            var doers = def?.ingestible?.outcomeDoers;
            if (doers != null)
                foreach (var d in doers)
                    if (d is IngestionOutcomeDoer_GiveHediff gh && gh.hediffDef == HediffDefOf.RegressionDamage)
                        return gh.severity;
            return 0.10f; // fallback
        }
        public static bool ShouldDoseNow(Pawn p, out string reason)
        {
            reason = null;
            if (p == null || p.Dead || p.Downed || p.InMentalState) return false;
            if (!p.Awake() || p.Drafted) return false;
            CompRegressionMemory mem = GetMemory(p);
            if (mem == null)
            {
                reason = "nomem";
                return false;
            }

            if (mem.desiredAgeDoseTick > 0 && Find.TickManager.TicksGame - mem.desiredAgeDoseTick < MinIntervalTicks)
            {
                reason = "cooldown";
                return false;
            }

            // FOY vial is a drug; avoid if pawn is vomiting or has major overdose, etc.
            var overdose = p.health?.hediffSet?.GetFirstHediffOfDef(RimWorld.HediffDefOf.DrugOverdose);
            if (overdose != null && overdose.Severity >= 0.4f) { reason = "overdose"; return false; }


            var reg = Hediff_RegressionDamage.HediffByPawn(p);
            if (reg == null)
            {
                reason = "no-regression";
                return false;
            }
            float cur = reg?.Severity ?? 0f;

            if(mem.desiredAgeYears == -1)
            {
                reason = "no-desire";
                return false;
            }

            // Map desired age -> target severity S*
            // (Use your existing converter; shown as a method on the hediff or helper)
            float targetS = reg.SeverityForTargetYears(mem.desiredAgeYears);

            // Tolerance = half a pill (e.g., 5% if a pill is 10%)
            float doseDelta = GetFoySeverityPerDose();    // e.g., 0.10
            float tol = doseDelta * 0.5f;                 // e.g., 0.05

            float lower = targetS - tol;
            float upper = targetS + tol;

            // already too young/over-regressed? never dose
            if (cur > upper + ExtraHysteresis) { reason = "too-young"; return false; }

            // within window? no dose
            if (cur >= lower - ExtraHysteresis && cur <= upper + ExtraHysteresis)
            { reason = "in-window"; return false; }

            // Below window: consider 1 dose, but avoid overshooting past the upper bound.
            if (cur < lower - ExtraHysteresis)
            {
                float after = cur + doseDelta;
                if (after > upper + ExtraHysteresis)
                {
                    reason = "would-overshoot";
                    return false;
                }
                reason = "below-window-dose";
                return true;
            }

            return false;
        }
    }
}

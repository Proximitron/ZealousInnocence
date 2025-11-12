using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace ZealousInnocence
{
    public enum ZIStacking { Additive, Refresh, Exclusive }

    public class CompProperties_RegressionInfluence : HediffCompProperties
    {
        // most comps simply use the severity of the hediff as magnitude
        public bool useSeverityAsMagnitude = true;

        // Base runtime magnitude that then fans out into domain contributions via power multipliers.
        public float magnitudeBase = 0f;

        // Decay & caps
        public float decayPerDayBase = 0f;

        // Domain contribution caps
        public float maxContributionMental = 0.20f;
        public float maxContributionPhysical = 0.20f;

        public float totalCapMental = 0f;     // e.g., 1.00f to keep mental ≤ 1.0 while this hediff is active
        public float totalCapPhysical = 0f;

        // Domain contribution power (0f disables contribution to the sum for that domain)
        public float powerMental = 1f;
        public float powerPhysical = 0f;

        // Domain multipliers (0f disables this hediff for the domain entirely)
        // Use != 1f to create amplifier (>1) or dampener (<1) state effects.
        public float multiplierMental = 1f;
        public float multiplierPhysical = 1f;

        // Stacking rules
        public ZIStacking stacking = ZIStacking.Refresh;
        public string exclusiveGroup;

        // Aura feeding (if true and inArea==true, skip decay this tick)
        public bool isAreaAura = false;

        // Optional: accelerates decay of other sources (kept for future; your tick logic can read this)
        public float accelerateExternalDecay = 0f;

        // Optional: conditionally apply another hediff while active at/over threshold
        public string onThresholdHediff;
        public float severityThreshold = 0f;
        public float applyChance = 1f;

        public CompProperties_RegressionInfluence()
        {
            compClass = typeof(HediffComp_RegressionInfluence);
        }
    }

    public class HediffComp_RegressionInfluence : HediffComp
    {
        public CompProperties_RegressionInfluence Props => (CompProperties_RegressionInfluence)props;


        // set by aura system / map gizmo each tick for pawns inside an area emitter
        public bool inArea;

        private int _lastDecayTick;

        public float decayPerDay
        {
            get
            {
                float perDay = Props.decayPerDayBase;
                float mult = Settings.Regression_BaseRecoveryPerDay * 100f;
                return perDay * mult;
            }
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            // enforce exclusive groups
            if (Props.stacking == ZIStacking.Exclusive && !string.IsNullOrEmpty(Props.exclusiveGroup))
            {
                foreach (var h in Pawn.health.hediffSet.hediffs)
                {
                    if (h != parent && h.TryGetComp<HediffComp_RegressionInfluence>() is { } c
                        && c.Props.exclusiveGroup == Props.exclusiveGroup)
                    {
                        h.Severity = 0f; // remove older effect
                    }
                }
            }
        }
        private static ZealousInnocenceSettings Settings
    => LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Pawn == null || parent == null) return;
            if (!Pawn.IsHashIntervalTick(60)) return;

            int now = Find.TickManager.TicksGame;
            if (_lastDecayTick < 0) _lastDecayTick = now;

            int elapsed = now - _lastDecayTick;
            if (elapsed > 0)
            {
                _lastDecayTick = now;

                bool skip = Props.isAreaAura && inArea;
                if (!skip && parent.Severity > 0f)
                {
                    float days = elapsed / (float)GenDate.TicksPerDay;
                    float delta = decayPerDay * days;

                    // clamp
                    parent.Severity -= delta;
                }
            }
            // Ensure the main collector hediff exists
            if (parent.Severity > 0)
            {
                if (Props.powerMental > 0f && Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.RegressionMental) == null)
                {
                    Pawn.health.AddHediff(HediffDefOf.RegressionMental, Helper_Regression.GetFirstBrainRecord(Pawn));
                }
                if (Props.powerPhysical > 0f && Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.RegressionPhysical) == null)
                {
                    Pawn.health.AddHediff(HediffDefOf.RegressionPhysical);
                }
            }


            // threshold-applied hediff (once per tick check; caller can add extra gating if needed)
            if (!string.IsNullOrEmpty(Props.onThresholdHediff) &&
                parent.Severity >= Props.severityThreshold &&
                Rand.Value < Props.applyChance)
            {
                var def = DefDatabase<HediffDef>.GetNamedSilentFail(Props.onThresholdHediff);
                if (def != null && Pawn.health.hediffSet.GetFirstHediffOfDef(def) == null)
                    Pawn.health.AddHediff(def);
            }
        }

        public float GetExternalCappedContribution(bool mental)
        {
            if (mental)
            {
                if (Props.multiplierMental == 0f) return 0f;
                float raw = parent.Severity * Props.powerMental;
                return Math.Min(raw, Props.maxContributionMental);
            }
            else
            {
                if (Props.multiplierPhysical == 0f) return 0f;
                float raw = parent.Severity * Props.powerPhysical;
                return Math.Min(raw, Props.maxContributionPhysical);
            }
        }
        /// <summary>
        /// Domain multiplier (amplifier/dampener). 0f disables the hediff for the domain.
        /// </summary>
        public float GetMultiplierFactor(bool mental)
        {
            return mental ? Props.multiplierMental : Props.multiplierPhysical;
        }
        public float GetCap(bool mental)
        {
            return mental ? Props.totalCapMental : Props.totalCapPhysical;
        }
        public bool IsInfluence(bool mental)
        {
            return mental ? Props.multiplierMental != 0f : Props.multiplierPhysical != 0f;
        }
    }
}

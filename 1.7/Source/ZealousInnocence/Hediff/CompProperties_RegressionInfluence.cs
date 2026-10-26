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
        // Decay & caps
        public float decayPerDayBase = 0f;

        // Domain contribution caps
        public float maxContributionMental = -1f;
        public float maxContributionPhysical = -1f;

        // 0f will be no cap
        public float totalCapMental = 0f;     // e.g., 1.00f to keep mental ≤ 1.0 while this hediff is active
        public float totalCapPhysical = 0f;

        // Domain contribution power (0f disables contribution to the sum for that domain)
        public float powerMental = 0f;
        public float powerPhysical = 0f;

        // Domain multipliers (0f disables this hediff for the domain entirely)
        // Use != 1f to create amplifier (>1) or dampener (<1) state effects.
        public float multiplierMental = 1f;
        public float multiplierPhysical = 1f;

        // Stacking rules
        public ZIStacking stacking = ZIStacking.Additive;
        public string exclusiveGroup;

        // Aura feeding (if true and inArea==true, skip decay this tick)
        public bool isAreaAura = false;

        // Optional: accelerates decay of other sources (kept for future; your tick logic can read this)
        public float accelerateExternalDecay = 0f;

        // Optional: conditionally apply another hediff while active at/over threshold
        public string onThresholdHediff;
        public float severityThreshold = 0f;
        public float applyChance = 1f;

        public bool affectsBodyParts = false;

        // How much tending accelerates recovery:
        public float tendRecoveryBonusPerQuality = 0.0f;

        // If true, the injury stops contributing while tended (hard off switch)
        public bool suppressContributionWhileTended = false;

        public float bedRestRecoveryBonus = 0.0f;

        public float injuryHeal_ChancePerSeverity = 0f;
        public float injuryHeal_MinSeverity = 0f;

        public float ressurect_ChancePerSeverity = 0f;
        public float ressurect_MinSeverity = 0f;
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

        private int _lastDecayTick = -1;
        private float _lastSeverity = -1;

        public override void CompExposeData()
        {
            base.CompExposeData();

            Scribe_Values.Look(ref inArea, "ZI_inArea", false);
            Scribe_Values.Look(ref _lastDecayTick, "ZI_lastDecayTick", -1);
            Scribe_Values.Look(ref _lastSeverity, "ZI_lastSeverity", -1f);
        }

        public float decayPerDay
        {
            get
            {
                float perDay = Props.decayPerDayBase;
                float mult = Settings.Regression_BaseRecoveryPerDay;

                if (parent.def.tendable)
                {
                    var tend = parent.TryGetComp<HediffComp_TendDuration>();
                    if (tend != null && tend.IsTended)
                    {
                        float q = Mathf.Clamp01(tend.tendQuality); // 0..1
                        float bonus = Props.tendRecoveryBonusPerQuality * q;
                        mult += bonus; 
                    }
                }
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

        private void TriggerPostIncreaseEffects()
        {
            if (_lastSeverity < 0f)
                _lastSeverity = 0f;

            float current = parent.Severity;
            float min = Props.injuryHeal_MinSeverity;

            // Only count severity ABOVE the minimum threshold
            float prevOver = Mathf.Max(0f, _lastSeverity - min);
            float currOver = Mathf.Max(0f, current - min);
            float deltaOver = currOver - prevOver;

            _lastSeverity = current;

            //Log.Message($"[ZI] PostIncreaseEffects severity increase deltaOver '{deltaOver}' for '{parent.pawn.LabelCap}");
            if (deltaOver <= 0f)
                return;

            int times = TriggerPostIncreaseEffects_Times(deltaOver, Props.injuryHeal_ChancePerSeverity);
            if(times > 0) if (Settings.debugging && Settings.debuggingRegression) Log.Message($"[ZI] PostIncreaseEffects {times} for '{parent.LabelCap}' for '{parent.pawn.LabelCap}: {parent.Severity}");
            for (int i = 0; i < times; i++)
            {
                var list = Pawn.MissingBodyParts().InRandomOrder();
                if(list.Count() > 0)
                {
                    Log.Message($"[ZI] PostIncreaseEffects solving missing part '{list.First().LabelCap}' for '{parent.pawn.LabelCap}");
                    IntVec3 pos = Pawn.Spawned ? Pawn.Position : IntVec3.Invalid;
                    Map map = Pawn.Spawned ? Pawn.Map : null;
                    MedicalRecipesUtility.RestorePartAndSpawnAllPreviousParts(Pawn, list.First().Part, pos, map);
                    Pawn.health.hediffSet.DirtyCache();
                }
                else
                {
                    list = Pawn.PermanentInjuries().InRandomOrder();
                    if(list.Count() > 0)
                    {
                        Log.Message($"[ZI] PostIncreaseEffects solving injury '{list.First().LabelCap}' for '{parent.pawn.LabelCap}");
                        Pawn.health.hediffSet.hediffs.Remove(list.First());
                        Pawn.health.hediffSet.DirtyCache();
                    }
                }
            }
        }
        private int TriggerPostIncreaseEffects_Times(float delta, float c)
        {
            if (delta <= 0f || c <= 0f)
                return 0;

            // c = "expected triggers per +1.0 severity"
            float expected = c * delta;

            // Guaranteed triggers
            int triggers = Mathf.FloorToInt(expected);

            // Fractional chance for one extra
            float extraProb = expected - triggers;
            if (extraProb > 0f && Rand.Value < extraProb)
                triggers++;

            return triggers;
        }
        public override void CompPostMerged(Hediff other) => TriggerPostIncreaseEffects();
        public override void CompPostMake() => TriggerPostIncreaseEffects();
        
        public override void CompPostPostRemoved()
        {
            if (Settings.debugging && Settings.debuggingRegression)
            {
                Log.Message($"[ZI] Removing hediff '{parent.LabelCap}' for '{parent.pawn.LabelCap}: {parent.Severity}");
            }
        }

        private static ZealousInnocenceSettings Settings
    => LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Pawn == null || parent == null) return;
            if (!Pawn.IsHashIntervalTick(60)) return;

            int now = Find.TickManager.TicksGame;
            if (_lastSeverity < 0) _lastSeverity = parent.Severity;
            if (_lastSeverity < parent.Severity) TriggerPostIncreaseEffects();
            if (_lastDecayTick < 0) _lastDecayTick = now;

            int elapsed = now - _lastDecayTick;
            if (elapsed > 0)
            {
                _lastDecayTick = now;

                bool skip = Props.isAreaAura && inArea;
                if (!skip && parent.Severity > 0f)
                {
                    float days = elapsed / (float)GenDate.TicksPerDay;
                    float mult = 1f;

                    if (Props.bedRestRecoveryBonus != 0f && Pawn.InBed())
                    {
                        float strength = Settings.Regression_RestingMultiplier;
                        float bedBonus = Props.bedRestRecoveryBonus * strength; // scale ONLY the bonus
                        mult += bedBonus;
                    }

                    if (Settings.Regression_ChildMultiplier > 0f && !Pawn.ageTracker.Adult)
                        mult *= Settings.Regression_ChildMultiplier;

                    if (Settings.Regression_AnimalMultiplier > 0f && Pawn.IsAnimal)
                        mult *= Settings.Regression_AnimalMultiplier;
                    
                    mult = Mathf.Clamp(mult, 0f, 10f); // Safety limits

                    float delta = decayPerDay * mult * days;

                    //if(Settings.debugging && Settings.debuggingRegression) Log.Message($"[ZI] Delta {delta:F3} for '{parent.LabelCap}' for '{parent.pawn.LabelCap}: {parent.Severity}");
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
                return Math.Min(raw, Props.maxContributionMental == -1f ? float.MaxValue : Props.maxContributionMental);
            }
            else
            {
                if (Props.multiplierPhysical == 0f) return 0f;
                float raw = parent.Severity * Props.powerPhysical;
                return Math.Min(raw, Props.maxContributionPhysical == -1f ? float.MaxValue : Props.maxContributionPhysical);
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
        public override string CompTipStringExtra
        {
            get
            {
                var sb = new StringBuilder();

                // Show both domains if they are active
                AppendDomainInfo(sb, mental: true);
                AppendDomainInfo(sb, mental: false);

                // Generic info (decay, severity, etc.)
                if (decayPerDay > 0f)
                {
                    float remaining = parent.Severity / decayPerDay;
                    string time = Helper_Regression.FormatDays(remaining);
                    sb.AppendLine($"Decay: {decayPerDay:0.###}/day (≈ {time} remaining)");
                }
                else
                {
                    sb.AppendLine("Decay: ∞");
                }

                return sb.ToString().TrimEnd();
            }
        }
        private void AppendDomainInfo(StringBuilder sb, bool mental)
        {
            float power = mental ? Props.powerMental : Props.powerPhysical;
            float mult = mental ? Props.multiplierMental : Props.multiplierPhysical;
            float capDef = mental ? Props.maxContributionMental : Props.maxContributionPhysical;
            float totalCap = mental ? Props.totalCapMental : Props.totalCapPhysical;

            if (power <= 0f && Mathf.Approximately(mult, 1f) && capDef <= 0f && totalCap <= 0f)
                return; // nothing interesting for this domain

            string domainLabel = mental ? "Mental" : "Physical";
            sb.AppendLine($"{domainLabel} regression:");

            // this instance’s contribution, already external-capped
            float contrib = GetExternalCappedContribution(mental);
            if (contrib > 0f)
                sb.AppendLine($"  • Contribution: +{contrib:0.###}");

            if (!Mathf.Approximately(mult, 1f))
                sb.AppendLine($"  • Multiplier: ×{mult:0.###}");

            if (capDef > 0f)
                sb.AppendLine($"  • Per-hediff cap: ≤{capDef:0.###}");

            if (totalCap > 0f)
                sb.AppendLine($"  • Total domain cap while active: ≤{totalCap:0.###}");
        }

    }

    public class Hediff_RegressionInfluence : HediffWithComps
    {
        public override string LabelInBrackets
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(base.LabelInBrackets);
                int num = 0;
                for (; ; )
                {
                    int num2 = num;
                    List<HediffComp> list = this.comps;
                    int? num3 = (list != null) ? new int?(list.Count) : null;
                    if (!(num2 < num3.GetValueOrDefault() & num3 != null))
                    {
                        break;
                    }
                    string compLabelInBracketsExtra = this.comps[num].CompLabelInBracketsExtra;
                    if (!compLabelInBracketsExtra.NullOrEmpty())
                    {
                        if (stringBuilder.Length != 0)
                        {
                            stringBuilder.Append(", ");
                        }
                        stringBuilder.Append(compLabelInBracketsExtra);
                    }
                    num++;
                }
                var comp = this.TryGetComp<HediffComp_RegressionInfluence>();
                if (comp == null) return stringBuilder.ToString();

                if (stringBuilder.Length != 0)
                {
                    stringBuilder.Append(", ");
                }

                // compute remaining time
                float dec = comp.decayPerDay;
                if (dec <= 0f)
                {
                    stringBuilder.Append("∞");
                }
                else
                {
                    float days = Severity / dec;
                    stringBuilder.Append(Helper_Regression.FormatDays(days));
                }

                return stringBuilder.ToString();
            }
        }
    }
    public class Hediff_RegressionInfluence_Injury : Hediff_Injury
    {
        public override string LabelInBrackets
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                string baseStr = base.LabelInBrackets;

                if (!baseStr.NullOrEmpty())
                    sb.Append(baseStr);

                // Vanilla source info — prevent duplicates:
                if (sourceHediffDef != null)
                {
                    if (!sb.ToString().Contains(sourceHediffDef.label))
                    {
                        if (sb.Length != 0) sb.Append(", ");
                        sb.Append(sourceHediffDef.label);
                    }
                }
                else if (sourceDef != null)
                {
                    // sourceLabel-based duplication guard
                    if (!sb.ToString().Contains(sourceLabel))
                    {
                        if (sb.Length != 0) sb.Append(", ");
                        sb.Append(sourceLabel);
                    }
                }

                // permanent-wound info from vanilla
                HediffComp_GetsPermanent perm = this.TryGetComp<HediffComp_GetsPermanent>();
                if (perm != null && perm.IsPermanent && perm.PainCategory != PainCategory.Painless)
                {
                    if (sb.Length != 0) sb.Append(", ");
                    sb.Append(("PainCategory_" + perm.PainCategory).Translate());
                }

                // Your regression comp info
                var comp = this.TryGetComp<HediffComp_RegressionInfluence>();
                if (comp != null)
                {
                    if (sb.Length != 0) sb.Append(", ");

                    float dec = comp.decayPerDay;
                    if (dec <= 0f)
                        sb.Append("∞");
                    else
                    {
                        float days = Severity / dec;
                        sb.Append(Helper_Regression.FormatDays(days));
                    }
                }

                return sb.ToString();
            }
        }
    }
    
}

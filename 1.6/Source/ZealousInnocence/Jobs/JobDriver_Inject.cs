using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using Verse.Sound;
using static HarmonyLib.Code;

namespace ZealousInnocence
{
    /*public class CompProperties_UseEffectInjectNanites : CompProperties_UseEffect
    {
        public HediffDef hediffDef;   // e.g. RegressionDamage
        public float severity = 0.4f; // target severity
        public float minIncrease = 0.2f; // at least this much increase

        public CompProperties_UseEffectInjectNanites()
        {
            compClass = typeof(CompUseEffect_InjectNanites);
        }
    }

    public class CompUseEffect_InjectNanites : CompUseEffect
    {
        public CompProperties_UseEffectInjectNanites Props
            => (CompProperties_UseEffectInjectNanites)props;

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            if (usedBy == null || Props.hediffDef == null) return;
            var hediff = usedBy.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.RegressionDamage);

            float current = hediff == null ? 0 : hediff.Severity;
            Helper_Regression.SetIncreasedRegressionSeverity(usedBy, usedBy, Props.severity, Props.minIncrease);
            hediff = usedBy.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
            Messages.Message($"Injected nanites ({Props.hediffDef.label}). Severity: {current:F2} → {hediff.Severity:F2}",
                usedBy, MessageTypeDefOf.PositiveEvent);
        }

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            if (p?.health?.hediffSet == null) return false;
            if (Props.hediffDef == null) return "Missing hediffDef on comp";
            return true;
        }
    }*/

    public class CompProperties_TargetEffectFoyDose : CompProperties
    {
        public int minDose = 1;
        public int maxDose = 30;
        public int defaultDose = 1;
        public float severityPerDose = 0.05f;
        public ThingDef moteDef;
        public HediffDef hediffCaused;
        public CompProperties_TargetEffectFoyDose()
        {
            compClass = typeof(CompTargetEffectFoyDose);
        }
    }

    public class CompTargetEffectFoyDose : CompTargetEffect
    {
        public CompProperties_TargetEffectFoyDose Props
            => (CompProperties_TargetEffectFoyDose)props;

        
        // Called in other RW builds (and by CompTargetable’s workflow)
        public override void DoEffectOn(Pawn user, Thing target)
        {
            // Require a valid corpse target
            ThingWithComps targetEntity = null;
            if (target is Corpse corpse)
            {
                targetEntity = corpse;
            }
            else if (target is Pawn pawn)
            {
                targetEntity = pawn;
            }
            else return;

            // Determine dose bounds based on stack count
            int avail = parent.stackCount;
            int min = Math.Max(1, Props.minDose);
            int max = Math.Min(Math.Max(min, Props.maxDose), avail);
            int initial = Mathf.Clamp(Props.defaultDose, min, max);

            if (max < 1)
            {
                Messages.Message("Not enough doses in this stack.", parent, MessageTypeDefOf.RejectInput);
                return;
            }

            if (Props.maxDose > min)
            {
                Find.WindowStack.Add(new Dialog_DosePicker(
                    min, max, initial,
                    n => $"Dose: x{n} ({n * Props.severityPerDose * 100f:0}%)",
                    dose =>
                    {
                        CallJob(user,targetEntity,dose);
                    }));
            }
            else
            {
                CallJob(user, targetEntity, min);
            }
        }
        public void CallJob(Pawn user, ThingWithComps targetEntity, int count)
        {
            var job = JobMaker.MakeJob(JobDefOf.InjectRegression, targetEntity, parent);
            job.count = count;                 // pass chosen dose
            job.playerForced = true;
            job.checkOverrideOnExpire = true;
            // If 'user' is null (some call paths), find a suitable colonist:
            var actor = user ?? targetEntity.Map.mapPawns.FreeColonistsSpawned
                               .Where(p => p.CanReach(targetEntity, PathEndMode.Touch, Danger.Deadly))
                               .OrderBy(p => p.Position.DistanceTo(targetEntity.Position))
                               .FirstOrDefault();
            if (actor != null)
                actor.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            else
                Messages.Message("No usable colonist found for FOY injection.", targetEntity, MessageTypeDefOf.RejectInput);
        }
    }

    public class Dialog_DosePicker : Window
    {
        private readonly int min;
        private readonly int max;
        private readonly Func<int, string> labelFor;   // e.g. "Dose xN (NN0%)"
        private readonly Action<int> onAccept;

        private int cur;

        public override Vector2 InitialSize => new Vector2(420f, 160f);

        public Dialog_DosePicker(int min, int max, int initial, Func<int, string> labelFor, Action<int> onAccept)
        {
            this.min = Math.Max(1, min);
            this.max = Math.Max(this.min, max);
            this.cur = Mathf.Clamp(initial, this.min, this.max);
            this.labelFor = labelFor ?? (n => $"Dose: x{n}");
            this.onAccept = onAccept;
            forcePause = true;
            closeOnClickedOutside = true;
            draggable = true;
            doCloseX = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var labelRect = new Rect(inRect.x, inRect.y, inRect.width, 32f);
            Widgets.Label(labelRect, labelFor(cur));

            var sliderRect = new Rect(inRect.x, labelRect.yMax + 6f, inRect.width, 24f);
            float f = Widgets.HorizontalSlider(sliderRect, cur, min, max, middleAlignment: true, roundTo: 1f);
            cur = Mathf.RoundToInt(f);

            var btnW = (inRect.width - 12f) / 2f;
            var okRect = new Rect(inRect.x, sliderRect.yMax + 12f, btnW, 38f);
            var cancelRect = new Rect(okRect.xMax + 12f, okRect.y, btnW, 38f);

            if (Widgets.ButtonText(okRect, "OK"))
            {
                Close();
                onAccept?.Invoke(cur);
            }
            if (Widgets.ButtonText(cancelRect, "Cancel"))
            {
                Close();
            }
        }
    }

}

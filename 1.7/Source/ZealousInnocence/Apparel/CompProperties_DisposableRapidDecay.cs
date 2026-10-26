using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ZealousInnocence
{
    public class CompProperties_DisposableRapidDecay : CompProperties
    {
        public int startDelayTicks = 1;
        public int hpPerTick = 5;
        public CompProperties_DisposableRapidDecay()
        {
            compClass = typeof(CompDisposableRapidDecay);
        }
    }

    public class CompDisposableRapidDecay : ThingComp
    {
        private int decayStartTick = -1;
        public bool preventMerging; // ← read by the apparel class

        public CompProperties_DisposableRapidDecay Props => (CompProperties_DisposableRapidDecay)props;
        Apparel Apparel => parent as Apparel;
        bool IsWorn => Apparel?.Wearer != null;

        public override void Notify_Equipped(Pawn pawn)
        {
            // pause decay if re-equipped
            preventMerging = false;
            decayStartTick = -1;
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            // As soon as it’s taken off, flag as “don’t merge” and schedule decay
            preventMerging = true;
            decayStartTick = Find.TickManager.TicksGame + Mathf.Max(1, Props.startDelayTicks);
        }

        public override void CompTick()
        {
            if (decayStartTick < 0) return;
            if (Find.TickManager.TicksGame < decayStartTick) return;
            if (IsWorn) return; // safety


            // burn HP
            if (parent.HitPoints > 0)
                parent.HitPoints = Mathf.Max(0, parent.HitPoints - Mathf.Max(1, Props.hpPerTick));

            if (parent.HitPoints <= 0)
                parent.Destroy(DestroyMode.Vanish);
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref decayStartTick, "ZI_decayStartTick", -1);
            Scribe_Values.Look(ref preventMerging, "ZI_preventMerging", false);
        }
    }
}

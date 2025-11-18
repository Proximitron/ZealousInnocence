using NAudio.Codecs;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using static ZealousInnocence.Hediff_PhysicalRegression;

namespace ZealousInnocence
{
    public static class Patch_ToddlerUtility
    {

        public static bool IsToddler(Pawn p, ref bool __result)
        {
            if (p == null) return true;
            if (Current.Game == null || Current.Game.Maps == null || !Current.Game.Maps.Any()) return true;

            __result = p.isToddlerMental();
            return false;
        }

        public static bool PercentGrowth(Pawn p, ref float __result)
        {
            var band = Hediff_RegressionBase.ChildBands.Get(p);
            var childCore = band.core;
            //2 years * 60 days per year * 60000 ticks per day
            float toddlerStageInTicks = (childCore - GenDate.TicksPerYear);
            //age up at 1 yearold
            float ticksSinceBaby = (float)(p.getAgeStageMental() * GenDate.TicksPerYear) - band.toddler;

            /*Log.Message("MaxAge: " + childCore + ", toddlerStageInTicks: " + toddlerStageInTicks + ", ageStageMental: " + p.getAgeStageMental()
                + ", ticksSinceBaby: " + ticksSinceBaby + ", PercentGrowth: " + (ticksSinceBaby / toddlerStageInTicks));*/
            __result = ticksSinceBaby / toddlerStageInTicks;
            return false;
        }
    }
}

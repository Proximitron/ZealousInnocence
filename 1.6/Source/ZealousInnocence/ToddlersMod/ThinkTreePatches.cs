using DubsBadHygiene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace ZealousInnocence
{
    public class ThinkNode_ConditionalToddlerOverride : ThinkNode_Conditional
    {
        private static bool coreDebug = false;
        static ThinkNode_ConditionalToddlerOverride()
        {
            if(coreDebug) Log.Message("[ZI] ThinkNode_ConditionalToddlerOverride type loaded");
        }
        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return false;
            if (Helper_Toddlers.ToddlersLoaded)
            {
                if (pawn.isToddlerMental() && !pawn.isToddlerPhysical())
                {
                    if (coreDebug) Log.Message($"[ZI]ThinkNode_ConditionalToddlerOverride (toddlers loaded) in affect for {pawn.Name.ToStringShort}");
                    return true;
                }
            }
            return false;
        }
    }
    public class ThinkNode_ConditionalToddlerOverride_NoToddlersOnly : ThinkNode_Conditional
    {
        private static bool coreDebug = false;
        private static ZealousInnocenceSettings Settings => LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
        static ThinkNode_ConditionalToddlerOverride_NoToddlersOnly()
        {
            if (coreDebug) Log.Message("[ZI] ThinkNode_ConditionalToddlerOverride_NoToddlersOnly type loaded");
        }
        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return false;
            if (!Helper_Toddlers.ToddlersLoaded)
            {
                if (pawn.isToddlerMentalOrPhysical())
                {
                    if (coreDebug) Log.Message($"[ZI]ThinkNode_ConditionalToddlerOverride_NoToddlersOnly (toddlers missing) in affect for {pawn.Name.ToStringShort}");
                    return true;
                }
            }
            return false;
        }
    }
    


}

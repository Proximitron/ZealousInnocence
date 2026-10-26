using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ZealousInnocence
{
    public static class Patch_LearningUtility_LearningSatisfied
    {
        public static bool Prefix(Pawn pawn, ref bool __result)
        {
            if (pawn?.needs?.learning == null)
            {
                // Fallback behaviour: treat as satisfied
                __result = true;
                return false;
            }

            // All good — run the vanilla code
            return true;
        }
    }
}

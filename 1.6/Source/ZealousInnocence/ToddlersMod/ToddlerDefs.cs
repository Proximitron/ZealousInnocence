using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ZealousInnocence
{
    [DefOf]
    public static class ToddlersDefOf
    {
        [MayRequire("cyanobot.toddlers")]
        public static HediffDef LearningManipulation;

        [MayRequire("cyanobot.toddlers")]
        public static HediffDef LearningToWalk;

        static ToddlersDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ToddlersDefOf));
        }
    }
}

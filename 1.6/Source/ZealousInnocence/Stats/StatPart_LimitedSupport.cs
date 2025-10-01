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
    public class StatPart_LimitedSupport : StatPart
    {
        // Assuming mainStat is the absorbency and supportStat is the support.
        public StatDef mainStat;
        public StatDef supportStat;

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing)
            {
                Pawn pawn = req.Thing as Pawn;
                if (pawn != null)
                {
                    float mainValue = pawn.apparel.WornApparel.Sum(a => a.GetStatValue(mainStat));
                    float supportValue = pawn.apparel.WornApparel.Sum(a => a.GetStatValue(supportStat));

                    // Add support but limit it to not exceed the main stat value
                    val += Mathf.Min(supportValue, mainValue);
                }
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            return $"Support adds to absorbency but cannot increase it beyond the base absorbency value.";
        }
    }
}

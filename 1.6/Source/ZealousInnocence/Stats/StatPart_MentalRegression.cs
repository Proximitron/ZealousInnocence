using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ZealousInnocence
{
    /*public class StatPart_MentalRegression : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing)
            {
                Pawn pawn = req.Thing as Pawn;
                if (pawn != null)
                {
                    // Get the mental regression stat value
                    float regression = pawn.GetStatValue(StatDefOf.MentalRegression, true);
                    // Apply the regression effect to the mental age
                    val -= val * regression;

                    val = Math.Max(0, Math.Min(val, pawn.GetStatValue(StatDefOf.MentalAgeLimit, true)));
                }
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing)
            {
                Pawn pawn = req.Thing as Pawn;
                if (pawn != null)
                {
                    StringBuilder result = new StringBuilder();

                    float regression = pawn.GetStatValue(StatDefOf.MentalRegression, true);
                    result.AppendLine($"{StatDefOf.MentalRegression.label}: -{regression.ToStringPercent()}");
                    float maxAge = pawn.GetStatValue(StatDefOf.MentalAgeLimit, true);
                    result.AppendLine($"{StatDefOf.MentalAgeLimit.label}: {maxAge.ToStringByStyle(ToStringStyle.FloatOne)} years");

                    return result.ToString();
                }
            }
            return null;
        }
    }*/

}

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ZealousInnocence
{
    /*public class StatPart_PhysicalAge : StatPart
    {
        public float minConciousness;
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing)
            {
                Pawn pawn = req.Thing as Pawn;
                if (pawn != null)
                {
                    // Check if the pawn is intelligent enough or aware enough
                    if (IsIntelligentEnough(pawn))
                    {
                        // Add physical age to the mental age calculation
                        val += pawn.ageTracker.AgeBiologicalYearsFloat;
                    }
                }
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing)
            {
                Pawn pawn = req.Thing as Pawn;
                if (pawn != null && IsIntelligentEnough(pawn))
                {
                    return "StatPartPhysicalAgeDescriptor".Translate(pawn.ageTracker.AgeBiologicalYearsFloat);
                }
            }
            return "ShortReasonPhysicalAgeNoEffect".Translate();
        }

        private bool IsIntelligentEnough(Pawn pawn)
        {
            // Example condition: Check if the pawn has a consciousness level above a certain threshold
            return pawn.health.capacities.GetLevel(RimWorld.PawnCapacityDefOf.Consciousness) > minConciousness;
            // Alternatively, you could check for specific traits, hediffs, or other conditions
        }
    }*/
}

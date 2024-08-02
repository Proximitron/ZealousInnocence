using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ZealousInnocence
{
    public class ThoughtWorker_CribBed_Preferred : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            //ThoughtWorker_Precept_SlabBed_Preferred

            // Check if the pawn has a last bed definition and if it has the "Crib" tag
            if (p.mindState.lastBedDefSleptIn != null && p.mindState.lastBedDefSleptIn.building != null && p.mindState.lastBedDefSleptIn.building.buildingTags.Contains("Crib"))
            {
                if(p.ageTracker.Adult)
                {
                    // Check if the pawn's ideoligion includes the CribBed_Preferred precept
                    if (p.Ideo != null && p.Ideo.HasPrecept(PreceptDefOf.CribBed_Preferred))
                    {
                        return ThoughtState.ActiveAtStage(1); // Return stage 1 if they have the precept
                    }
                    else
                    {
                        return ThoughtState.ActiveAtStage(0); // Return stage 0 if they do not have the precept
                    }
                }
                else
                {
                    return ThoughtState.ActiveAtStage(2); // Return stage 2 for all children
                }
            }

            // Return Inactive if none of the conditions are met
            return ThoughtState.Inactive;
        }
    }
}

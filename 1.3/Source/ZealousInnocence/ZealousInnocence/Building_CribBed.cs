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
            return p.mindState.lastBedDefSleptIn != null && p.mindState.lastBedDefSleptIn.building != null && p.mindState.lastBedDefSleptIn.building.buildingTags.Contains("Crib");
        }
    }
}

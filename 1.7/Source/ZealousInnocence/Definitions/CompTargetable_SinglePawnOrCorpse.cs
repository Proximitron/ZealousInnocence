using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ZealousInnocence
{

    public class CompTargetable_SinglePawnOrCorpseOrAnimal : CompTargetable
    {
        // Token: 0x17002C37 RID: 11319
        // (get) Token: 0x0601116E RID: 69998 RVA: 0x000028E7 File Offset: 0x00000AE7
        protected override bool PlayerChoosesTarget
        {
            get
            {
                return true;
            }
        }

        // Token: 0x0601116F RID: 69999 RVA: 0x004E9666 File Offset: 0x004E7866
        protected override TargetingParameters GetTargetingParameters()
        {
            return new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                canTargetAnimals = true,
                canTargetMechs = false,
                canTargetHumans = true,
                canTargetCorpses = true,
            };
        }

        // Token: 0x06011170 RID: 70000 RVA: 0x004E9AC7 File Offset: 0x004E7CC7
        public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
        {
            yield return targetChosenByPlayer;
            yield break;
        }
    }
}

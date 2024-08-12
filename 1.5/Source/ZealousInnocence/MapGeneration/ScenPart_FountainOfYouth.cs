using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ZealousInnocence
{
    public class ScenPart_FountainOfYouthGeneration : ScenPart_DisableMapGen
    {
        // Token: 0x060083B5 RID: 33717 RVA: 0x002D81CE File Offset: 0x002D63CE
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<FountainGenerationMethod>(ref this.method, "method", FountainGenerationMethod.Disabled, false);
        }

        // Token: 0x060083B6 RID: 33718 RVA: 0x002D81E8 File Offset: 0x002D63E8
        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            if (Widgets.ButtonText(listing.GetScenPartRect(this, ScenPart.RowHeight), this.method.ToStringHuman(), true, true, true, null))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (object obj in Enum.GetValues(typeof(FountainGenerationMethod)))
                {
                    FountainGenerationMethod localM2 = (FountainGenerationMethod)obj;
                    FountainGenerationMethod localM = localM2;
                    list.Add(new FloatMenuOption(localM.ToStringHuman(), delegate ()
                    {
                        this.method = localM;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
                }
                Find.WindowStack.Add(new FloatMenu(list));
            }
        }

        // Token: 0x060083B7 RID: 33719 RVA: 0x002D82D0 File Offset: 0x002D64D0
        public override void PostMapGenerate(Map map)
        {
            if (this.method == FountainGenerationMethod.Disabled)
            {
                return;
            }
            if (Find.GameInfo.startingTile != map.Tile)
            {
                return;
            }
            IntVec3 loc;
            if (!LargeBuildingCellFinder.TryFindCell(out loc, map, ScenPart_FountainOfYouthGeneration.StructureSpawnParms.ForThing(ThingDefOf.FountainOfYouth), null, (IntVec3 c) => ScattererValidator_AvoidSpecialThings.IsValid(c, map), true) && !LargeBuildingCellFinder.TryFindCell(out loc, map, ScenPart_FountainOfYouthGeneration.StructureSpawnParmsLoose.ForThing(ThingDefOf.FountainOfYouth), null, (IntVec3 c) => ScattererValidator_AvoidSpecialThings.IsValid(c, map), true))
            {
                Log.Error("Failed to generate fountain of youth.");
                return;
            }
            GenStep_Monolith.GenerateMonolith(loc, map);
        }

        // Token: 0x060083B8 RID: 33720 RVA: 0x002D838D File Offset: 0x002D658D
        public override int GetHashCode()
        {
            return base.GetHashCode() ^ this.method.GetHashCode();
        }

        // Token: 0x04004BF4 RID: 19444
        private FountainGenerationMethod method = FountainGenerationMethod.NearColonists;

        // Token: 0x04004BF5 RID: 19445
        private static readonly LargeBuildingSpawnParms StructureSpawnParms = new LargeBuildingSpawnParms
        {
            minDistToEdge = 10,
            maxDistanceFromPlayerStartPosition = 20f,
            attemptSpawnLocationType = SpawnLocationType.Outdoors,
            attemptNotUnderBuildings = true,
            canSpawnOnImpassable = false
        };

        // Token: 0x04004BF6 RID: 19446
        private static readonly LargeBuildingSpawnParms StructureSpawnParmsLoose = new LargeBuildingSpawnParms
        {
            minDistToEdge = 10,
            attemptSpawnLocationType = SpawnLocationType.Outdoors,
            attemptNotUnderBuildings = true,
            canSpawnOnImpassable = false
        };
    }
    public enum FountainGenerationMethod
    {
        Disabled = 0,
        NearColonists = 1
    }
    public static class FountainGenerationMethodExtension
    {
        // Token: 0x060083B4 RID: 33716 RVA: 0x002D819E File Offset: 0x002D639E
        public static string ToStringHuman(this FountainGenerationMethod method)
        {
            if (method == FountainGenerationMethod.Disabled)
            {
                return "FountainOfYouthGenerationMethod_Disabled".Translate();
            }
            if (method != FountainGenerationMethod.NearColonists)
            {
                throw new NotImplementedException();
            }
            return "FountainOfYouthGenerationMethod_NearColonists".Translate();
        }
    }
}

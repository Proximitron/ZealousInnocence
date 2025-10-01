using RimWorld.Planet;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using System.Net.NetworkInformation;

namespace ZealousInnocence
{
    public class GenStep_FountainOfYouth : GenStep_Scatterer
    {
        public override int SeedPart
        {
            get
            {
                return 325163958;
            }
        }

        protected override bool CanScatterAt(IntVec3 loc, Map map)
        {
            if (!base.CanScatterAt(loc, map))
            {
                return false;
            }
            CellRect cellRect = CellRect.CenteredOn(loc, ClearRadius);
            int newZ = cellRect.minZ - 1;
            for (int i = cellRect.minX; i <= cellRect.maxX; i++)
            {
                IntVec3 c = new IntVec3(i, 0, newZ);
                if (!c.InBounds(map) || !c.Walkable(map))
                {
                    return false;
                }
            }
            return true;
        }

        protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int count = 1)
        {
            GenerateFountain(loc, map);
        }
        public static void GenerateFountain(IntVec3 loc, Map map)
        {
            GenSpawn.Spawn((Building_FountainOfYouth)ThingMaker.MakeThing(ThingDefOf.FountainOfYouth, null), loc, map, WipeMode.Vanish);
            foreach (IntVec3 c4 in GridShapeMaker.IrregularLump(loc, map, AsphaltSize, null))
            {
                map.terrainGrid.SetTerrain(c4, TerrainDefOf.FlagstoneSandstone);
            }
            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            int randomInRange = PeePoopFilth.RandomInRange;
            Predicate<IntVec3> validator = null;
            for (int i = 0; i < randomInRange; i++)
            {
                if (validator == null)
                {
                    validator = (IntVec3 c) => c.Standable(map);
                }

                IntVec3 c2;
                if (CellFinder.TryFindRandomCellNear(loc, map, DebrisRadius, validator, out c2))
                {
                    if(settings.faecesActive && i % 2 == 0)
                    {
                        FilthMaker.TryMakeFilth(c2, map, ThingDef.Named("FilthFaeces"), 1, FilthSourceFlags.Pawn, true);
                    }
                    else
                    {
                        FilthMaker.TryMakeFilth(c2, map, ThingDef.Named("FilthUrine"), 1, FilthSourceFlags.Pawn, true);
                    }                    
                }
            }

            /*int randomInRange3 = CorpsesCountRange.RandomInRange;
            Predicate<IntVec3> <> 9__2;
            for (int k = 0; k < randomInRange3; k++)
            {
                Map map4 = map;
                int squareRadius3 = 4;
                Predicate<IntVec3> validator3;
                if ((validator3 = <> 9__2) == null)
                {
                    validator3 = (<> 9__2 = ((IntVec3 c) => c.Standable(map)));
                }
                IntVec3 loc2;
                if (CellFinder.TryFindRandomCellNear(loc, map4, squareRadius3, validator3, out loc2, -1))
                {
                    Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Drifter, null, PawnGenerationContext.NonPlayer, -1, false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
                    pawn.health.SetDead();
                    pawn.apparel.DestroyAll(DestroyMode.Vanish);
                    pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
                    Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
                    Corpse corpse = pawn.MakeCorpse(null, null);
                    corpse.Age = Mathf.RoundToInt((float)(CorpseAgeRangeDays.RandomInRange * 60000));
                    corpse.GetComp<CompRottable>().RotProgress += (float)corpse.Age;
                    GenSpawn.Spawn(pawn.Corpse, loc2, map, WipeMode.Vanish);
                }
            }*/
        }

        private const int DebrisRadius = 4;
        private const int AsphaltSize = 30;
        private const int ClearRadius = 5;

        private static readonly IntRange PeePoopFilth = new IntRange(2, 6);
    }
}

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ZealousInnocence
{
    public static class Helper_Interactions
    {
        public static Pawn FindRandomLosNearbyColonist(Pawn pawn, float radius = 10f)
        {
            if (pawn?.Spawned != true || pawn.Map == null)
                return null;

            Map map = pawn.Map;

            List<Pawn> candidates = new List<Pawn>();

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, radius, true))
            {
                if (!cell.InBounds(map))
                    continue;

                var things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Pawn p = things[i] as Pawn;
                    if (p == null || p == pawn)
                        continue;

                    // Only humanlikes
                    if (!p.RaceProps.Humanlike)
                        continue;

                    // Same faction & not hostile
                    if (p.Faction == null || pawn.Faction == null)
                        continue;

                    if (p.HostileTo(pawn))
                        continue;

                    // Must be conscious / able to interact
                    if (!p.Awake() || p.Dead || p.Downed)
                        continue;

                    // skip mental breaks
                    if (p.InMentalState) 
                        continue;

                    if(!SocialInteractionUtility.CanInitiateRandomInteraction(p))
                        continue;

                    // Line of sight check
                    if (!GenSight.LineOfSight(pawn.Position, p.Position, map, skipFirstCell: true))
                        continue;

                    candidates.Add(p);
                }
            }

            if (candidates.Count == 0)
                return null;

            return candidates.RandomElement();
        }
        public static void ForceTalk(Pawn initiator, Pawn recipient, InteractionDef def)
        {
            if (initiator == null || recipient == null)
                return;

            initiator.interactions.TryInteractWith(recipient, def);
        }
        public static void TriggerRandomInteractionWithNearby(this Pawn initiator)
        {
            TriggerInteractionWithNearby(initiator,RimWorld.InteractionDefOf.Chitchat);
        }
        public static void TriggerPottyFailInteractionWithNearby(this Pawn initiator)
        {
            TriggerInteractionWithNearby(initiator, InteractionDefOf.Custom_PottyTraining_Failure);
        }
        public static void TriggerInteractionWithNearby(this Pawn initiator, InteractionDef def)
        {
            Pawn recipient = FindRandomLosNearbyColonist(initiator);
            if (recipient == null) return;
            ForceTalk(initiator, recipient, def);
        }

        
    }
}

using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ZealousInnocence
{
    public class DebugActions
    {
        [DebugAction("ZealousInnocence", "Reset Wetting Seed", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ResetWettingSeed()
        {
            List<Thing> selectedThings = Find.Selector.SelectedObjects.OfType<Thing>().ToList();
            foreach (var thing in selectedThings)
            {
                if (thing is Pawn pawn)
                {
                    var need = pawn.needs.TryGetNeed<Need_Diaper>();
                    if(need == null)
                    {
                        Log.Message($"Seed reset for {pawn.Name} not possible. No Need_Diaper.");
                        return;
                    }
                    if(need.bedwettingSeed == 0)
                    {
                        Log.Message($"Seed reset for {pawn.Name} not possible. Seed already 0.");
                        return;
                    }
                    need.bedwettingSeed = pawn.HashOffsetTicks();
                    Log.Message($"Seed reset for {pawn.Name} complete.");
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Reset wetting!",UnityEngine.Color.white, 3.85f);
                }
            }
        }
        [DebugAction("ZealousInnocence", "Reset Failure Seed", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ResetFailureSeed()
        {
            List<Thing> selectedThings = Find.Selector.SelectedObjects.OfType<Thing>().ToList();
            foreach (var thing in selectedThings)
            {
                if (thing is Pawn pawn)
                {
                    var need = pawn.needs.TryGetNeed<Need_Diaper>();
                    if (need == null)
                    {
                        Log.Message($"Seed reset for {pawn.Name} not possible. No Need_Diaper.");
                        return;
                    }
                    if (need.FailureSeed == 0)
                    {
                        Log.Message($"Seed reset for {pawn.Name} not possible. Seed already 0.");
                        return;
                    }
                    need.FailureSeed = 0;
                    Log.Message($"Seed reset for {pawn.Name} complete.");
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Reset failure seed!", UnityEngine.Color.white, 3.85f);
                }
            }
        }
    }
}

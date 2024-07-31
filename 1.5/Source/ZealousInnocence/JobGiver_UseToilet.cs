using DubsBadHygiene;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse.AI;
using Verse;

namespace ZealousInnocence
{
    public static class JobGiver_UseToilet_TryGiveJob_Patch
    {
        // Prefix to save runs in unnessesary cases. It tracks if the pawn notices 
        public static bool Prefix(JobGiver_UseToilet __instance, Pawn pawn)
        {
            return DiaperHelper.remembersPotty(pawn);
        }
        // Postfix to observe or modify the output of TryGiveJob
        public static void Postfix(JobGiver_UseToilet __instance, Pawn pawn, ref Job __result)
        {
            if (__result != null && pawn.RaceProps.Humanlike)
            {
                var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
                var debugging = settings.debugging && settings.debuggingJobs;
                var debugBedwetting = debugging || (settings.debugging && settings.debuggingBedwetting && !pawn.Awake());
                if (pawn.Awake())
                {
                    var liked = DiaperHelper.getDiaperPreference(pawn);
                    if (liked == DiaperLikeCategory.Liked)
                    {
                        var currDiapie = DiaperHelper.getDiaper(pawn);
                        if (currDiapie == null)
                        {
                            pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("HadToGoPotty"), null, null);
                        }
                        else
                        {
                            if (debugging) Log.Message($"JobGiver_UseToilet postfix null for {pawn.Name.ToStringShort}");
                            __result = null;
                            return;
                        }
                    }
                }
                else
                {
                    if (debugging || debugBedwetting) Log.Message($"JobGiver_UseToilet not awake for {pawn.Name.ToStringShort}");
                    __result = null;
                    return;
                }
                var diaperNeed = pawn.needs.TryGetNeed<Need_Diaper>();
                if (diaperNeed != null) diaperNeed.FailureSeed = 0; // resetting seed

                //Log.Message($"JobGiver_UseToilet attempting to assign a job to {pawn.Name.ToStringShort}");
            }
        }
    }
}

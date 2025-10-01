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
using HarmonyLib;

namespace ZealousInnocence
{
    public static class JobGiver_UseToilet_TryGiveJob_Patch
    {
        // Prefix to save runs in unnessesary cases. It tracks if the pawn notices 
        public static bool Prefix(JobGiver_UseToilet __instance, Pawn pawn)
        {
            return Helper_Diaper.remembersPotty(pawn);
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
                    var liked = Helper_Diaper.getDiaperPreference(pawn);
                    if (liked == DiaperLikeCategory.Liked)
                    {
                        var currDiapie = Helper_Diaper.getDiaper(pawn);
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
                    JobFailReason.Is("Not awake.");
                    __result = null;
                    return;
                }
                var diaperNeed = pawn.needs.TryGetNeed<Need_Diaper>();
                if (diaperNeed != null) diaperNeed.FailureSeed = 0; // resetting seed

                //Log.Message($"JobGiver_UseToilet attempting to assign a job to {pawn.Name.ToStringShort}");
            }
        }
    }
    [HarmonyPatch(typeof(Building_AssignableFixture), nameof(Building_AssignableFixture.PawnAllowed))]
    public static class Building_AssignableFixture_PawnAllowed_Patch
    {
        // Prefix to save runs in unnessesary cases. It tracks if the pawn notices 
        public static bool Prefix(Building_AssignableFixture __instance, Pawn p, ref AcceptanceReport __result)
        {
            var report = Helper_Regression.canUsePottyReport(p);
            if (!report.Accepted)
            {
                __result = report;
                return false;
            }
            return true;
        }

    }
}

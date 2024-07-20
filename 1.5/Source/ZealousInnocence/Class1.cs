using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.Sound;
using Multiplayer.API;
using System.Net;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using DubsBadHygiene;

namespace ZealousInnocence
{
    [StaticConstructorOnStartup]
    public static class ZealousInnocenceMultiplayer
    {
        static ZealousInnocenceMultiplayer()
        {
            if (!MP.enabled) return;

            // This is where the magic happens and your attributes
            // auto register, similar to Harmony's PatchAll.
            MP.RegisterAll();

            // You can choose to not auto register and do it manually
            // with the MP.Register* methods.

            // Use MP.IsInMultiplayer to act upon it in other places
            // user can have it enabled and not be in session
        }
    }

    public class ZealousInnocenceSettings : ModSettings
    {
        public float targetChronoAge = 10f;
        public bool reduceAge = false;
        public bool formerAdultsNeedLearning = false;
        public bool formerAdultsCanHaveIdeoRoles = true;
        public bool formerAdultsGetGrowthMoments = false;
        public bool debugging = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref targetChronoAge, "targetChronoAge", 10f);
            Scribe_Values.Look(ref reduceAge, "reduceAge", false);
            Scribe_Values.Look(ref formerAdultsNeedLearning, "formerAdultsNeedLearning", false);
            Scribe_Values.Look(ref formerAdultsCanHaveIdeoRoles, "formerAdultsCanHaveIdeoRoles", true);
            Scribe_Values.Look(ref formerAdultsGetGrowthMoments, "formerAdultsGetGrowthMoments", false);
            Scribe_Values.Look(ref debugging, "debugging", false);
        }
    }

    [StaticConstructorOnStartup]
    public class ZealousInnocence : Mod
    {
        ZealousInnocenceSettings settings;
        private static readonly Harmony harmony = new Harmony("proximo.zealousinnocence");
        public ZealousInnocence(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<ZealousInnocenceSettings>();

            Harmony.DEBUG = false;

            //harmony.PatchAll(Assembly.GetExecutingAssembly());
            harmony.Patch(
                 original: AccessTools.Method(typeof(JobGiver_OptimizeApparel), "ApparelScoreRaw"),
                 postfix: new HarmonyMethod(typeof(Patch_JobGiver_OptimizeApparel), nameof(Patch_JobGiver_OptimizeApparel.ApparelScoreRaw))
            );
            if(settings.debugging) Log.Message("ZealousInnocence harmony patching: JobGiver_OptimizeApparel.ApparelScoreGain");

            //Patch_SleepAndWakeup.Patch_PawsGoesToBed_Execute(harmony);
            harmony.Patch(
                original: AccessTools.Method(typeof(ApparelProperties), "PawnCanWear", new Type[] { typeof(Pawn), typeof(bool) }),
                postfix: new HarmonyMethod(typeof(ApparelProperties_PawnCanWear_Patch), nameof(ApparelProperties_PawnCanWear_Patch.Postfix))
            );
            if (settings.debugging) Log.Message("ZealousInnocence harmony patching: ApparelProperties.PawnCanWear");

            harmony.Patch(
                original: AccessTools.Method(typeof(JobGiver_UseToilet), "TryGiveJob"),
                prefix: new HarmonyMethod(typeof(JobGiver_UseToilet_TryGiveJob_Patch), nameof(JobGiver_UseToilet_TryGiveJob_Patch.Prefix))
            );
            if (settings.debugging) Log.Message("ZealousInnocence harmony patching: JobGiver_UseToilet.TryGiveJob Prefix");
            harmony.Patch(
                original: AccessTools.Method(typeof(JobGiver_UseToilet), "TryGiveJob"),
                postfix: new HarmonyMethod(typeof(JobGiver_UseToilet_TryGiveJob_Patch), nameof(JobGiver_UseToilet_TryGiveJob_Patch.Postfix))
            );
            if (settings.debugging) Log.Message("ZealousInnocence harmony patching: JobGiver_UseToilet.TryGiveJob Postfix");

            harmony.Patch(
                original: AccessTools.Method(typeof(HealthCardUtility), "GetPawnCapacityTip"),
                postfix: new HarmonyMethod(typeof(HealthCardUtility_GetPawnCapacityTip_Patch), nameof(HealthCardUtility_GetPawnCapacityTip_Patch.Postfix))
            );
            if (settings.debugging) Log.Message("ZealousInnocence harmony patching: HealthCardUtility.GetPawnCapacityTip Postfix");

            ModChecker.ZealousInnocenceActive();
            
            if (!ModChecker.ForeverYoungActive())
            {
                harmony.Patch(
                    original: AccessTools.PropertyGetter(typeof(Need_Learning), nameof(Need_Learning.Suspended)),
                    postfix: new HarmonyMethod(typeof(Need_Learning_IsFrozen), nameof(Need_Learning_IsFrozen.Postfix))
                );
                if (settings.debugging) Log.Message("ZealousInnocence harmony patching: Need_Learning.Suspended");
                harmony.Patch(
                   original: AccessTools.Method(typeof(Precept_Role), nameof(Precept_Role.RequirementsMet)),
                   prefix: new HarmonyMethod(typeof(PreceptRole_RequirementsMet), nameof(PreceptRole_RequirementsMet.Prefix))
                );
                if (settings.debugging) Log.Message("ZealousInnocence harmony patching: Precept_Role.RequirementsMet");
            }

        }

        public void DoCheckOnHarmonyMethode(MethodInfo originalMethod)
        {
            var patches = Harmony.GetPatchInfo(originalMethod);
            if (patches != null)
            {
                foreach (var prefix in patches.Prefixes)
                {
                    Log.Message($"Prefix patch from {prefix.owner}");
                }
                foreach (var postfix in patches.Postfixes)
                {
                    Log.Message($"Postfix patch from {postfix.owner}");
                }
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listStandard = new Listing_Standard();
            listStandard.Begin(inRect);
            

            listStandard.CheckboxLabeled("Reduce Age", ref settings.reduceAge, "If checked, the reincarnation ritual will reduce the age of the pawn to that of a child. Otherwise it will regress the pawn mentally.");

            listStandard.GapLine();
            listStandard.Label("Ritual Age Result: " + settings.targetChronoAge, tooltip: "The target age a pawn will be reduced to by the rebirth ritual. Only works if 'Reduce Age' is checked as well.");
            settings.targetChronoAge = (float)System.Math.Round((double)listStandard.Slider(settings.targetChronoAge, 3, 13));

            listStandard.GapLine();
            listStandard.TextEntry("Options after this point will ONLY work if 'Reduce Age' is checked and 'ForeverYoung' is NOT installed!");
            listStandard.TextEntry("If 'ForeverYoung' is installed, the setting of that mode will handle this settings instead!");
            listStandard.CheckboxLabeled("Extra Growth Moments", ref settings.formerAdultsGetGrowthMoments, "If off, former adults will NOT get extra growth moments at 7/10/13. If on, growth moments will work as normal.");

            if (ModsConfig.IdeologyActive)
            {
                listStandard.CheckboxLabeled("Ideology Roles", ref settings.formerAdultsCanHaveIdeoRoles, "Allow former adults to hold roles in their ideology.");
            }
            listStandard.CheckboxLabeled("Learning Need", ref settings.formerAdultsNeedLearning, "Controlles if a pawn has still the need to learn after being regressed to the age of a child.");

            listStandard.GapLine();
            listStandard.CheckboxLabeled("DEBUGGING Mode", ref settings.debugging, "Activates a lot of unnessessary logs and work, in case you want to find an error. Restart required!");
            if (settings.debugging && listStandard.ButtonText("Check ForeverYoung active"))
            {
                ModChecker.ZealousInnocenceActive();
                ModChecker.ForeverYoungActive();
            }
            listStandard.End();

            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Zealous Innocence";
        }
    }

    public class ModChecker
    {
        public static bool IsModActive(string packageId)
        {
            var debugging = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().debugging;
            foreach (ModMetaData mod in ModsConfig.ActiveModsInLoadOrder)
            {
                if (mod.PackageId == packageId)
                {
                    if (debugging) Log.Message("ZealousInnocence ModCheck: '" + packageId.ToString() + "' was found");
                    return true;
                }
            }
            if (debugging) Log.Message("ZealousInnocence ModCheck: '" + packageId.ToString() + "' was not found");
            return false;
        }
        public static bool ForeverYoungActive()
        {
            return ModLister.GetActiveModWithIdentifier("me.nanz.foreveryoung") != null;
        }
        public static bool ZealousInnocenceActive()
        {
            return ModLister.GetActiveModWithIdentifier("proximo.zealousinnocence") != null;
        }
    }

    [HarmonyPatch(typeof(Need_Learning), nameof(Need_Learning.Suspended), MethodType.Getter)]
    static class Need_Learning_IsFrozen
    {
        public static void Postfix(ref bool __result, ref Pawn ___pawn, ref Need_Learning __instance)
        {
            if (___pawn.ageTracker.AgeBiologicalYears < 13)
            {
                if (___pawn.records.GetValue(RecordDefOf.ResearchPointsResearched) > 0)
                {
                    if (!LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().formerAdultsNeedLearning)
                    {
                        __instance.CurLevel = 0.5f;
                        __result = true;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Precept_Role), nameof(Precept_Role.RequirementsMet))]
    static class PreceptRole_RequirementsMet
    {
        public static bool Prefix(ref Pawn p, ref bool __result, ref Precept_Role __instance)
        {
            if (ModsConfig.IdeologyActive)
            {
                if (!LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().formerAdultsCanHaveIdeoRoles || p.records.GetValue(RecordDefOf.ResearchPointsResearched) == 0)
                {
                    return true;
                }

                foreach (RoleRequirement roleRequirement in __instance.def.roleRequirements)
                {
                    if (!roleRequirement.Met(p, __instance))
                    {
                        __result = false;
                        return false;
                    }
                }
                __result = true;
                return false;
            }
            else
            {
                return true;
            }
        }
    }
    public class CapacityImpactorCustom : PawnCapacityUtility.CapacityImpactor
    {
        public string customLabel;
        public float customValue;

        public override string Readable(Pawn pawn)
        {
            return $"{customLabel}: {customValue.ToStringPercent()}";
        }
    }
    [HarmonyPatch(typeof(HealthCardUtility), "GetPawnCapacityTip")]
    public static class HealthCardUtility_GetPawnCapacityTip_Patch
    {
        public static void Postfix(Pawn pawn, PawnCapacityDef capacity, ref string __result)
        {
            List<PawnCapacityUtility.CapacityImpactor> list = new List<PawnCapacityUtility.CapacityImpactor>();
            float eff = PawnCapacityUtility.CalculateCapacityLevel(pawn.health.hediffSet, capacity, list, false);

            StringBuilder stringBuilder = new StringBuilder(__result);

            // Add custom impactors
            if (list.Count > 0)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is CapacityImpactorCustom customImpactor)
                    {
                        stringBuilder.AppendLine($"  {customImpactor.Readable(pawn)}");
                    }
                }
            }

            __result = stringBuilder.ToString();
        }
    }

    public static class JobGiver_UseToilet_TryGiveJob_Patch
    {
        // Prefix to save runs in unnessesary cases. It tracks if the pawn notices 
        public static bool Prefix(JobGiver_UseToilet __instance, Pawn pawn)
        {
            if(pawn != null && pawn.RaceProps.Humanlike)
            {
                var diaperNeed = pawn.needs.TryGetNeed<Need_Diaper>();
                if (diaperNeed == null) return true; // anything without that gets a free pass
                if (diaperNeed.IsHavingAccident) return true; // now we notice for sure
                if (!DiaperHelper.getBladderControlLevelCapable(pawn)) return false; // incapeable of noticing
                if (pawn.story.traits.HasTrait(TraitDef.Named("Potty_Rebel"))) return false; // will never run to a potty!
                
                var currDiapie = DiaperHelper.getDiaper(pawn);
                if (currDiapie != null && pawn.outfits.forcedHandler.IsForced(currDiapie)) return false; // forced in diapers
                

                float bladderControl = pawn.health.capacities.GetLevel(PawnCapacityDefOf.BladderControl);

                // 0.2 bladder control = 100%, 0.65 bladder control = 1%
                float probability = Mathf.Clamp01(-2 * bladderControl + 1.4f);

                // Use the in-game day combined with pawn's birth year and birth day as the seed
                //int seed = GenDate.DaysPassed + pawn.ageTracker.BirthYear + pawn.ageTracker.BirthDayOfYear;

                if (Rand.ChanceSeeded(probability, diaperNeed.FailureSeed))
                {
                    var debugging = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().debugging;
                    if (debugging) Log.Message($"JobGiver_UseToilet prefix false, {pawn.Name.ToStringShort} at propability {probability} and seed {diaperNeed.FailureSeed}");
                    return false; // depending on the level on control
                }
                else
                {
                    var debugging = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().debugging;
                    if (debugging) Log.Message($"JobGiver_UseToilet prefix true, {pawn.Name.ToStringShort} at propability {probability} and seed {diaperNeed.FailureSeed}");
                }
            }
            return true;
        }
        // Postfix to observe or modify the output of TryGiveJob
        public static void Postfix(JobGiver_UseToilet __instance, Pawn pawn, ref Job __result)
        {
            if (__result != null && pawn.RaceProps.Humanlike)
            {
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
                            var debugging = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().debugging;
                            if (debugging) Log.Message($"JobGiver_UseToilet postfix null for {pawn.Name.ToStringShort}");
                            __result = null;
                            return;
                        }
                    }                    
                }
                var diaperNeed = pawn.needs.TryGetNeed<Need_Diaper>();
                if (diaperNeed != null) diaperNeed.FailureSeed = 0; // resetting seed
                
                //Log.Message($"JobGiver_UseToilet attempting to assign a job to {pawn.Name.ToStringShort}");
            }
        }
    }
    public static class ApparelProperties_PawnCanWear_Patch
    {
        // Prefix for the second overload
        public static void Postfix(ApparelProperties __instance, Pawn pawn, bool ignoreGender, ref bool __result)
        {
            if (pawn.Map != null && pawn.Spawned && !pawn.Dead) return;

            if (__instance.tags.Contains("Onesies"))
            {
                OnesieLikeCategory pref = OnesieHelper.getOnesiePreference(pawn);
                if (pref == OnesieLikeCategory.Liked) __result = true;
                else if(pref == OnesieLikeCategory.Disliked) __result = false;
                else
                {
                    if(DiaperHelper.needsDiaper(pawn)) __result = true;
                }
            }
            else if (__instance.tags.Contains("DiapersNight"))
            {
                __result = DiaperHelper.needsDiaperNight(pawn) && DiaperHelper.getDiaperPreference(pawn) != DiaperLikeCategory.Disliked;
            }
            else if (__instance.tags.Contains("Diaper"))
            {

                __result = DiaperHelper.needsDiaper(pawn) || DiaperHelper.getDiaperPreference(pawn) == DiaperLikeCategory.Liked;
                               
            }
            else if (__instance.tags.Contains("Underwear"))
            {
                __result = (!DiaperHelper.needsDiaper(pawn) && !DiaperHelper.needsDiaperNight(pawn)) || DiaperHelper.getDiaperPreference(pawn) == DiaperLikeCategory.Disliked;
            }
        }
    }

    [HarmonyPatch(typeof(JobGiver_OptimizeApparel))]
    public static class Patch_JobGiver_OptimizeApparel
    {

        [HarmonyPostfix]
        [HarmonyPatch("ApparelScoreRaw")]
        public static float ApparelScoreRaw(float __result, Pawn pawn, Apparel ap)
        {
            var debugging = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().debugging;
            if (ap.HasThingCategory(ThingCategoryDefOf.Diapers))
            {
                __result -= 0.5f; // Diapers by default less likely to be worn

                if (DiaperHelper.needsDiaper(pawn))
                {
                    __result += 5f;
                }
                else
                {
                    if (DiaperHelper.isNightDiaper(ap) && DiaperHelper.needsDiaperNight(pawn))
                    {
                        __result += 5f;
                    }
                }

                var preference = DiaperHelper.getDiaperPreference(pawn);
                if (preference == DiaperLikeCategory.NonAdult)
                {
                    __result += 2f;
                }
                else if (preference == DiaperLikeCategory.Liked)
                {
                    __result += 10f;
                }
                else if (preference == DiaperLikeCategory.Disliked)
                {
                    __result -= 10f;
                }

                if (debugging) Log.Message("Apparel " + ap.Label + " is diaper and rated " + __result + " for " + pawn.LabelShort);
            }
            else if (ap.HasThingCategory(ThingCategoryDefOf.Underwear))
            {
                __result += 0.2f; // Underwear is default more likely to be worn
                var preference = DiaperHelper.getDiaperPreference(pawn);
                if (preference == DiaperLikeCategory.Liked)
                {
                    __result -= 2f;
                }
                else if (preference == DiaperLikeCategory.Disliked)
                {
                    __result += 2f;
                }
                if(pawn.gender == Gender.Male && ap.HasThingCategory(ThingCategoryDefOf.FemaleCloth))
                {
                    __result -= 1f;

                }
                else if (pawn.gender == Gender.Female && ap.HasThingCategory(ThingCategoryDefOf.MaleCloth))
                {
                   __result -= 1f;
                }
                if (debugging) Log.Message("Apparel " + ap.Label + " is underwear and rated " + __result + " for " + pawn.LabelShort);

            }
            else if (ap.HasThingCategory(ThingCategoryDefOf.Onesies))
            {
                __result -= 0.25f; // Onesies by default are less likely to be worn

                var preference = OnesieHelper.getOnesiePreference(pawn);
                if (preference == OnesieLikeCategory.NonAdult)
                {
                    __result += 1f;
                }
                else if (preference == OnesieLikeCategory.Liked)
                {
                    __result += 5f;
                }
                else if (preference == OnesieLikeCategory.Disliked)
                {
                    __result -= 10f;
                }
                if (debugging) Log.Message("Apparel " + ap.Label + " is onesie and rated " + __result + " for " + pawn.LabelShort);
            }
            //JobGiver_OptimizeApparel.ApparelScoreRaw
            //JobGiver_OptimizeApparel.ApparelScoreGain
            return __result;

        }
    }

    public enum DiaperSituationCategory : byte
    {
        Trashed,
        Spent,
        Used,
        Clean,
    }

    public enum DiaperLikeCategory : byte
    {
        Neutral,
        Liked,
        Disliked,
        NonAdult
    }


    [DefOf]
    public class PawnCapacityDefOf
    {
        public static PawnCapacityDef BladderControl;
    }
    [DefOf]
    public class BodyPartTagDefOf
    {
        public static BodyPartTagDef BladderControlSource;
    }
    [DefOf]
    public class BodyPartDefOf
    {
        public static BodyPartDef Bladder;
    }
    [DefOf]
    public class DiaperChangie
    {
        public static SoundDef Pee;
        public static SoundDef Poop;
    }

    [DefOf]
    public class StatDefOf
    {
        public static StatDef Absorbency;
        public static StatDef DiaperAbsorbency; // This value is a stat created from Absorbency and DiaperSupport
    }

    [DefOf]
    public class HediffDefOf
    {
        public static HediffDef RegressionState;
        public static HediffDef DiaperRash;
    }

    [DefOf]
    public class TraitDefOf
    {
        public static TraitDef Potty_Rebel;
        public static TraitDef Big_Boy;
    }
    [DefOf]
    public class JobDefOf
    {
        public static JobDef Unbladder;
        public static JobDef Rebirth;
        public static JobDef Phoenix;
        public static JobDef RegressedPlayAround;
        public static JobDef LayDown;
        //public static JobDef WearSleepwear;
       // public static JobDef OptimizeApparel;
        //public static JobDef ChangeSleepwear;
    }
    [DefOf]
    public class DutyDefOf
    {
        public static DutyDef RegressedPlayTime;
    }

    [DefOf]
    public class ThoughtDefOf
    {
        public static ThoughtDef RegressedGames;
    }


    [DefOf]
    public class HistoryEventDefOf
    {
        public static HistoryEventDef GotUnbladdered;
    }
    

    public enum BathroomDesireCategory : byte
    {
        Going,
        NeedsToGo,
        Fine,
    }

    
}
//-----------------------------------------------------------
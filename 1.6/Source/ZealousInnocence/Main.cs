﻿using DubsBadHygiene;
using HarmonyLib;
using Multiplayer.API;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Verse;
using Verse.AI;
using Verse.Noise;
using Verse.Sound;

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

            // Changes how pawns score gear based on behaviour and preferences
            patchFunctionPostfix(
                original: AccessTools.Method(typeof(JobGiver_OptimizeApparel), "ApparelScoreRaw"),
                postfix: new HarmonyMethod(typeof(Patch_JobGiver_OptimizeApparel), nameof(Patch_JobGiver_OptimizeApparel.ApparelScoreRaw)),
                info: "JobGiver_OptimizeApparel.ApparelScoreRaw"
            );

            // This patch doesn't limit what pawns can wear, but change what pawns wear on spawning
            patchFunctionPostfix(
                original: AccessTools.Method(typeof(ApparelProperties), "PawnCanWear", new Type[] { typeof(Pawn), typeof(bool) }),
                postfix: new HarmonyMethod(typeof(ApparelProperties_PawnCanWear_Patch), nameof(ApparelProperties_PawnCanWear_Patch.Postfix)),
                info: "ApparelProperties.PawnCanWear"
            );

            // The following 2 functions are patched to control the going to the toilet behaviour, based on bladder control(+)
            patchFunctionPrefix(
                original: AccessTools.Method(typeof(JobGiver_UseToilet), "TryGiveJob"),
                prefix: new HarmonyMethod(typeof(JobGiver_UseToilet_TryGiveJob_Patch), nameof(JobGiver_UseToilet_TryGiveJob_Patch.Prefix)),
                info: "DubsBadHygiene.JobGiver_UseToilet.TryGiveJob"
            );
            patchFunctionPostfix(
                original: AccessTools.Method(typeof(JobGiver_UseToilet), "TryGiveJob"),
                postfix: new HarmonyMethod(typeof(JobGiver_UseToilet_TryGiveJob_Patch), nameof(JobGiver_UseToilet_TryGiveJob_Patch.Postfix)),
                info: "DubsBadHygiene.JobGiver_UseToilet.TryGiveJob"
            );

            patchFunctionPostfix(
                original: AccessTools.Method(typeof(HealthCardUtility), "GetPawnCapacityTip"),
                postfix: new HarmonyMethod(typeof(HealthCardUtility_GetPawnCapacityTip_Patch), nameof(HealthCardUtility_GetPawnCapacityTip_Patch.Postfix)),
                info: "HealthCardUtility.GetPawnCapacityTip"
            );

            // Guests on spawn → add hidden flag hediff
            patchFunctionPostfix(
                original: AccessTools.Method(typeof(Pawn), nameof(Pawn.SpawnSetup), new[] { typeof(Map), typeof(bool) }),
                postfix: new HarmonyMethod(typeof(ZI_FlagGuestsOnSpawn), nameof(ZI_FlagGuestsOnSpawn.Postfix)),
                info: "Pawn.SpawnSetup"
            );

            // Guests on despawn → remove flag hediff
            patchFunctionPrefix(
                original: AccessTools.Method(typeof(Pawn), nameof(Pawn.DeSpawn)),
                prefix: new HarmonyMethod(typeof(ZI_UnflagGuestsOnDespawn), nameof(ZI_UnflagGuestsOnDespawn.Prefix)),
                info: "Pawn.DeSpawn"
            );
            patchFunctionPrefix(
                original: AccessTools.Method(typeof(Pawn_NeedsTracker), "ShouldHaveNeed"),
                prefix: new HarmonyMethod(typeof(Patch_ShouldHaveNeed), nameof(Patch_ShouldHaveNeed.Prefix)),
                info: "Pawn_NeedsTracker.ShouldHaveNeed"
            );
            patchFunctionPostfix(
                original: AccessTools.Method(typeof(Pawn_NeedsTracker), nameof(Pawn_NeedsTracker.AddOrRemoveNeedsAsAppropriate)),
                postfix: new HarmonyMethod(typeof(Patch_AddOrRemoveNeedsAsAppropriate), nameof(Patch_AddOrRemoveNeedsAsAppropriate.Postfix)),
                info: "Pawn_NeedsTracker.AddOrRemoveNeedsAsAppropriate"
            );

            // The category can be different, depending on if the paw can feel the need to go potty
            patchFunctionPostfix(
                original: AccessTools.PropertyGetter(typeof(Need_Bladder), "CurCategory"),
                postfix: new HarmonyMethod(typeof(Need_Bladder_Patch), nameof(Need_Bladder_Patch.CurCategory_Postfix)),
                info: "Property DubsBadHygiene.Need_Bladder.CurCategory"
            );

            // Patching the need bladder NeedInterval and completly replacing it by custom logic
            patchFunctionPrefix(
                original: AccessTools.Method(typeof(Need_Bladder), "NeedInterval"),
                prefix: new HarmonyMethod(typeof(Need_Bladder_Patch), nameof(Need_Bladder_Patch.NeedInterval_Prefix)),
                info: "Need_Bladder.NeedInterval"
            );

            patchFunctionPostfix(
                original: AccessTools.Method(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) }),
                postfix: new HarmonyMethod(typeof(PawnGenerator_GeneratePawn_Patch), nameof(PawnGenerator_GeneratePawn_Patch.Postfix)),
                info: "PawnGenerator.GeneratePawn"
            );

            // The following 3 function limits pawns from wearing underwear or diapers on their own and solve this by seeking medical rest for getting changed
            patchFunctionPrefix(
                original: AccessTools.Method(typeof(JobGiver_OptimizeApparel), "TryGiveJob"),
                prefix: new HarmonyMethod(typeof(JobGiver_OptimizeApparel_TryGiveJob_Patch), nameof(JobGiver_OptimizeApparel_TryGiveJob_Patch.Prefix)),
                info: "JobGiver_OptimizeApparel.TryGiveJob"
            );

            patchFunctionPrefix(
                original: AccessTools.Method(typeof(Building_AssignableFixture), nameof(Building_AssignableFixture.PawnAllowed)),
                prefix: new HarmonyMethod(typeof(Building_AssignableFixture_PawnAllowed_Patch), nameof(Building_AssignableFixture_PawnAllowed_Patch.Prefix)),
                info: "DubsBadHygiene.Building_AssignableFixture.PawnAllowed"
            );
            patchFunctionPrefix(
                original: AccessTools.Method(typeof(Pawn_JobTracker), "StartJob"),
                prefix: new HarmonyMethod(typeof(Pawn_JobTracker_StartJob_Patch), nameof(Pawn_JobTracker_StartJob_Patch.Prefix)),
                info: "Pawn_JobTracker.StartJob"
            );
            patchFunctionPostfix(
                original: AccessTools.Method(typeof(HealthAIUtility), nameof(HealthAIUtility.ShouldSeekMedicalRest)),
                postfix: new HarmonyMethod(typeof(Patch_HealthAIUtility), nameof(Patch_HealthAIUtility.ShouldSeekMedicalRest)),
                info: "HealthAIUtility.ShouldSeekMedicalRest"
            );

            patchFunctionPrefix(
                original: AccessTools.Method(
                    typeof(HealthCardUtility),
                    "DrawLeftRow",
                    new Type[] {
                        typeof(Rect),
                        typeof(float).MakeByRefType(), // ref float curY
                        typeof(string),
                        typeof(string),
                        typeof(Color),
                        typeof(TipSignal)
                    }
                ),
                prefix: new HarmonyMethod(typeof(Patch_HealthCardUtility_DrawLeftRow), nameof(Patch_HealthCardUtility_DrawLeftRow.Prefix)),
                info: "HealthCardUtility.DrawLeftRow"
            );

            patchFunctionPostfix(
                original: AccessTools.Method(typeof(SkillRecord), nameof(SkillRecord.GetLevel)),
                postfix: new HarmonyMethod(typeof(Patch_GetLevel_MaskByRegression), nameof(Patch_GetLevel_MaskByRegression.Postfix)),
                info: "SkillRecord.GetLevel"
            );
            patchFunctionPostfix(
                original: AccessTools.Method(typeof(SkillRecord), nameof(SkillRecord.GetLevelForUI)),
                postfix: new HarmonyMethod(typeof(Patch_GetLevelForUI_MaskByRegression), nameof(Patch_GetLevel_MaskByRegression.Postfix)),
                info: "SkillRecord.GetLevelForUI"
            );
            patchFunctionPrefix(
                original: AccessTools.Method(typeof(Pawn_AgeTracker), "TryChildGrowthMoment"),
                prefix: new HarmonyMethod(typeof(Patch_TryChildGrowthMoment), nameof(Patch_TryChildGrowthMoment.Prefix)),
                info: "Pawn_AgeTracker.TryChildGrowthMoment"
            );
            patchFunctionPrefix(
                original: AccessTools.Method(typeof(Pawn_AgeTracker), "BirthdayBiological"),
                prefix: new HarmonyMethod(typeof(Patch_SuppressBirthdayBiological), nameof(Patch_SuppressBirthdayBiological.Prefix)),
                info: "Pawn_AgeTracker.BirthdayBiological"
            );
            patchFunctionPostfix(
                original: AccessTools.Method(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.AgeTickMothballed)),
                postfix: new HarmonyMethod(typeof(Patch_AgeTickMothballed_Postfix), nameof(Patch_AgeTickMothballed_Postfix.Postfix)),
                info: "Pawn_AgeTracker.AgeTickMothballed"
            );

            ModChecker.ZealousInnocenceActive();

            if (!ModChecker.ForeverYoungActive())
            {
                // Can suspend learning need on pawns that got again young
                patchFunctionPostfix(
                    original: AccessTools.PropertyGetter(typeof(Need_Learning), nameof(Need_Learning.Suspended)),
                    postfix: new HarmonyMethod(typeof(Need_Learning_IsFrozen), nameof(Need_Learning_IsFrozen.Postfix)),
                    info: "Property Need_Learning.Suspended"
                );
                // Can allow or disallow Rols for children (again)
                patchFunctionPrefix(
                    original: AccessTools.Method(typeof(Precept_Role), nameof(Precept_Role.RequirementsMet)),
                    prefix: new HarmonyMethod(typeof(PreceptRole_RequirementsMet), nameof(PreceptRole_RequirementsMet.Prefix)),
                    info: "Precept_Role.RequirementsMet"
                );                
            }

        }
        private void patchFunctionPostfix(MethodInfo original, HarmonyMethod postfix, string info)
        {
            if (!settings.debugging) DoCheckOnHarmonyMethode(original, false, true);
            harmony.Patch(
                original: original,
                postfix: postfix
            );
             Log.Message($"[ZI]harmony patching: Postfix {info}");
            if (settings.debugging) DoCheckOnHarmonyMethode(original, false, true);
        }
        private void patchFunctionPrefix(MethodInfo original, HarmonyMethod prefix, string info)
        {
            if(!settings.debugging) DoCheckOnHarmonyMethode(original, true, false);
            harmony.Patch(
                original: original,
                prefix: prefix
            );
             Log.Message($"[ZI]harmony patching: Prefix {info}");
            if (settings.debugging) DoCheckOnHarmonyMethode(original,true,false);
        }

        public void DoCheckOnHarmonyMethode(MethodInfo originalMethod, bool checkPrefix = true, bool checkPostfix = true)
        {
            var patches = Harmony.GetPatchInfo(originalMethod);
            if (patches != null)
            {
                if (checkPrefix)
                {
                    foreach (var prefix in patches.Prefixes)
                    {
                         Log.Message($"[ZI]Prefix patch from {prefix.owner}");
                    }
                }
                if (checkPostfix)
                {
                    foreach (var postfix in patches.Postfixes)
                    {
                         Log.Message($"[ZI]Postfix patch from {postfix.owner}");
                    }
                }

            }
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoWindowContents(inRect);

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
        public string customLabel = "";
        public float customValue = 0.0f;
        public string customString = "";

        public override string Readable(Pawn pawn)
        {
            if (customString == "")
            {
                return $"{customLabel}: {customValue.ToStringPercent()}";
            }
            else
            {
                return $"{customString}";
            }
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

    [HarmonyPatch(typeof(Thing), "DrawGUIOverlay")]
    public static class Thing_DrawGUIOverlay_Patch
    {
        public static bool Prefix(Thing __instance)
        {
            if (__instance is Apparel apparel)
            {
                if (apparel.def.thingCategories != null && (apparel.def.thingCategories.Contains(ThingCategoryDefOf.Diapers) || apparel.def.thingCategories.Contains(ThingCategoryDefOf.Underwear)))
                {
                    
                    // Check the HP of the apparel
                    float hpPercentage = (float)apparel.HitPoints / (float)apparel.MaxHitPoints;

                    // Determine which texture to use based on HP percentage

                    // Determine the texture path based on HP percentage
                    string texPath = apparel.def.graphicData.texPath;
                    if (hpPercentage < 0.51f)
                    {
                        texPath += "_Dirty";
                    }

                    // Load the appropriate texture
                    Texture2D texture = ContentFinder<Texture2D>.Get(texPath, true);

                    // Draw the icon with the chosen texture
                    Vector2 screenPos = GenMapUI.LabelDrawPosFor(__instance, -0.6f);
                    GUI.DrawTexture(new Rect(screenPos.x, screenPos.y, 32f, 32f), texture);

                    // Optionally, draw the label as well
                    GenMapUI.DrawThingLabel(__instance, __instance.LabelShort);

                    // Skip the original method
                    return false;
                }
            }

            // Continue with the original method if not apparel
            return true;

        }
    }

    public static class ApparelProperties_PawnCanWear_Patch
    {
        // Prefix for the second overload
        public static void Postfix(ApparelProperties __instance, Pawn pawn, bool ignoreGender, ref bool __result)
        {
            if (pawn.Map != null && pawn.Spawned && !pawn.Dead) return;
            if (!__result) return; // we don't overwrite things that can't be worn with a positive response

            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debugging = settings.debugging && settings.debuggingApparelGenerator;

            if (__instance.tags.Contains("Onesies"))
            {
                OnesieLikeCategory pref = OnesieHelper.getOnesiePreference(pawn);
                if (pref == OnesieLikeCategory.Liked) __result = true;
                else if(pref == OnesieLikeCategory.Disliked) __result = false;
                else
                {
                    if(Helper_Diaper.needsDiaper(pawn) && Helper_Diaper.acceptsDiaper(pawn)) __result = true;
                }
                
                if (debugging)  Log.Message($"[ZI]Apparel DEBUG: DiapersNight {pawn.LabelShort} value {__result} based on not needsDiaper {Helper_Diaper.needsDiaper(pawn)} or cat based {pref} physical age {pawn.ageTracker.AgeBiologicalYears}");
            }
            else if (__instance.tags.Contains("DiapersNight"))
            {
                __result = Helper_Diaper.needsDiaperNight(pawn) && Helper_Diaper.acceptsDiaperNight(pawn);
                if (debugging)  Log.Message($"[ZI]Apparel DEBUG: DiapersNight {pawn.LabelShort} value {__result} based on needsNight {Helper_Diaper.needsDiaperNight(pawn)} and acceptance {Helper_Diaper.acceptsDiaperNight(pawn)} of {Helper_Diaper.getDiaperPreference(pawn) != DiaperLikeCategory.Disliked} physical age {pawn.ageTracker.AgeBiologicalYears}");
            }
            else if (__instance.tags.Contains("Diaper"))
            {
                __result = (Helper_Diaper.needsDiaper(pawn) && Helper_Diaper.acceptsDiaper(pawn)) || Helper_Diaper.getDiaperPreference(pawn) == DiaperLikeCategory.Liked;
                if (debugging)  Log.Message($"[ZI]Apparel DEBUG: Diaper {pawn.LabelShort} value {__result} based on needsDiaper {Helper_Diaper.needsDiaper(pawn)} or cat {Helper_Diaper.getDiaperPreference(pawn) == DiaperLikeCategory.Liked} physical age {pawn.ageTracker.AgeBiologicalYears}");

            }
            else if (__instance.tags.Contains("Underwear"))
            {
                __result = (!Helper_Diaper.needsDiaper(pawn) && !Helper_Diaper.needsDiaperNight(pawn)) || Helper_Diaper.getDiaperPreference(pawn) == DiaperLikeCategory.Disliked;
                if (debugging)  Log.Message($"[ZI]Apparel DEBUG: Underwear {pawn.LabelShort} value {__result} based on not needsDiaper {Helper_Diaper.needsDiaper(pawn)} not needsNight {Helper_Diaper.needsDiaperNight(pawn)} or cat {Helper_Diaper.getDiaperPreference(pawn) == DiaperLikeCategory.Disliked} physical age {pawn.ageTracker.AgeBiologicalYears}");
            }


        }
    }
}
//-----------------------------------------------------------
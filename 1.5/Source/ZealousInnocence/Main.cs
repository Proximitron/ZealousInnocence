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
using Verse.Noise;

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
                info: "JobGiver_UseToilet.TryGiveJob"
            );
            patchFunctionPostfix(
                original: AccessTools.Method(typeof(JobGiver_UseToilet), "TryGiveJob"),
                postfix: new HarmonyMethod(typeof(JobGiver_UseToilet_TryGiveJob_Patch), nameof(JobGiver_UseToilet_TryGiveJob_Patch.Postfix)),
                info: "JobGiver_UseToilet.TryGiveJob"
            );

            // By default only system defined CapacityImpactor are shown. This patch adds custom ones
            patchFunctionPostfix(
                original: AccessTools.Method(typeof(HealthCardUtility), "GetPawnCapacityTip"),
                postfix: new HarmonyMethod(typeof(HealthCardUtility_GetPawnCapacityTip_Patch), nameof(HealthCardUtility_GetPawnCapacityTip_Patch.Postfix)),
                info: "HealthCardUtility.GetPawnCapacityTip"
            );

            // The category can be different, depending on if the paw can feel the need to go potty
            patchFunctionPostfix(
                original: AccessTools.PropertyGetter(typeof(Need_Bladder), "CurCategory"),
                postfix: new HarmonyMethod(typeof(Need_Bladder_Patch), nameof(Need_Bladder_Patch.CurCategory_Postfix)),
                info: "Property Need_Bladder.CurCategory"
            );


            patchFunctionPostfix(
                original: AccessTools.Method(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) }),
                postfix: new HarmonyMethod(typeof(PawnGenerator_GeneratePawn_Patch), nameof(PawnGenerator_GeneratePawn_Patch.Postfix)),
                info: "PawnGenerator.GeneratePawn"
            );
            /*patchFunctionPrefix(
                original: AccessTools.Method(typeof(Thing), "DrawGUIOverlay"),
                prefix: new HarmonyMethod(typeof(Thing_DrawGUIOverlay_Patch), nameof(Thing_DrawGUIOverlay_Patch.Prefix)),
                info: "Thing.DrawGUIOverlay"
            );
            patchFunctionPrefix(
                original: AccessTools.Method(typeof(Thing), "DrawAt"),
                prefix: new HarmonyMethod(typeof(Patch_Thing_DrawAt), nameof(Patch_Thing_DrawAt.Prefix)),
                info: "Thing.DrawAt"
            );*/


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
            Log.Message($"ZealousInnocence harmony patching: Postfix {info}");
            if (settings.debugging) DoCheckOnHarmonyMethode(original, false, true);
        }
        private void patchFunctionPrefix(MethodInfo original, HarmonyMethod prefix, string info)
        {
            if(!settings.debugging) DoCheckOnHarmonyMethode(original, true, false);
            harmony.Patch(
                original: original,
                prefix: prefix
            );
            Log.Message($"ZealousInnocence harmony patching: Prefix {info}");
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
                        Log.Message($"Prefix patch from {prefix.owner}");
                    }
                }
                if (checkPostfix)
                {
                    foreach (var postfix in patches.Postfixes)
                    {
                        Log.Message($"Postfix patch from {postfix.owner}");
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
                    if(DiaperHelper.needsDiaper(pawn) && DiaperHelper.acceptsDiaper(pawn)) __result = true;
                }
                
                if (debugging) Log.Message($"Apparel DEBUG: DiapersNight {pawn.LabelShort} value {__result} based on not needsDiaper {DiaperHelper.needsDiaper(pawn)} or cat based {pref} age {pawn.ageTracker.AgeBiologicalYears}");
            }
            else if (__instance.tags.Contains("DiapersNight"))
            {
                __result = DiaperHelper.needsDiaperNight(pawn) && DiaperHelper.acceptsDiaperNight(pawn);
                if (debugging) Log.Message($"Apparel DEBUG: DiapersNight {pawn.LabelShort} value {__result} based on needsNight {DiaperHelper.needsDiaperNight(pawn)} and acceptance {DiaperHelper.acceptsDiaperNight(pawn)} of {DiaperHelper.getDiaperPreference(pawn) != DiaperLikeCategory.Disliked} age {pawn.ageTracker.AgeBiologicalYears}");
            }
            else if (__instance.tags.Contains("Diaper"))
            {
                __result = (DiaperHelper.needsDiaper(pawn) && DiaperHelper.acceptsDiaper(pawn)) || DiaperHelper.getDiaperPreference(pawn) == DiaperLikeCategory.Liked;
                if (debugging) Log.Message($"Apparel DEBUG: Diaper {pawn.LabelShort} value {__result} based on needsDiaper {DiaperHelper.needsDiaper(pawn)} or cat {DiaperHelper.getDiaperPreference(pawn) == DiaperLikeCategory.Liked} age {pawn.ageTracker.AgeBiologicalYears}");

            }
            else if (__instance.tags.Contains("Underwear"))
            {
                __result = (!DiaperHelper.needsDiaper(pawn) && !DiaperHelper.needsDiaperNight(pawn)) || DiaperHelper.getDiaperPreference(pawn) == DiaperLikeCategory.Disliked;
                if (debugging) Log.Message($"Apparel DEBUG: Underwear {pawn.LabelShort} value {__result} based on not needsDiaper {DiaperHelper.needsDiaper(pawn)} not needsNight {DiaperHelper.needsDiaperNight(pawn)} or cat {DiaperHelper.getDiaperPreference(pawn) == DiaperLikeCategory.Disliked} age {pawn.ageTracker.AgeBiologicalYears}");
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
            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debugging = settings.debugging && settings.debuggingCloth;
            if (ap.HasThingCategory(ThingCategoryDefOf.Diapers))
            {
                __result -= 0.5f; // Diapers by default less likely to be worn
                var isNightDiaper = DiaperHelper.isNightDiaper(ap);
                if (isNightDiaper) __result += 1f; // Usually better than diapers and better than no underwear
                if (DiaperHelper.needsDiaper(pawn))
                {
                    if (isNightDiaper) __result += 3f; // too thin
                    else __result += 5f;
                }
                else
                {
                    if (isNightDiaper && DiaperHelper.needsDiaperNight(pawn))
                    {
                        __result += 5f;
                    }
                }

                var preference = DiaperHelper.getDiaperPreference(pawn);
                if (preference == DiaperLikeCategory.NonAdult)
                {
                    __result += 0.5f;
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
                __result += 1.5f; // Underwear is default more likely to be worn
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
                __result -= 0.35f; // Onesies by default are less likely to be worn

                var preference = OnesieHelper.getOnesiePreference(pawn);
                if (preference == OnesieLikeCategory.NonAdult)
                {
                    __result += 0.35f; // children don't care
                }
                else if (preference == OnesieLikeCategory.Liked)
                {
                    __result += 5f;
                }
                else if (preference == OnesieLikeCategory.Disliked)
                {
                    __result -= 10f;
                }
                if (DiaperHelper.needsDiaper(pawn)) __result += 0.8f;
                else if (DiaperHelper.needsDiaperNight(pawn)) __result += 0.4f;
                if (debugging) Log.Message("Apparel " + ap.Label + " is onesie and rated " + __result + " for " + pawn.LabelShort);
            }
            //JobGiver_OptimizeApparel.ApparelScoreRaw
            //JobGiver_OptimizeApparel.ApparelScoreGain
            return __result;

        }
    }
}
//-----------------------------------------------------------
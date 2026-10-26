using DubsBadHygiene;
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
using Verse.Grammar;
using ZealousInnocence.Interactions;
using ZealousInnocence.ToddlersMod;

namespace ZealousInnocence
{
    [StaticConstructorOnStartup]
    public static class ZealousInnocenceMultiplayer
    {
        static ZealousInnocenceMultiplayer()
        {
            if (!MP.enabled) return;

            MP.RegisterAll();
        }
    }

    [StaticConstructorOnStartup]
    public class ZealousInnocence : Mod
    {
        int PrefixGenericCount = 0;
        int PrefixCount = 0;
        int PostfixCount = 0;
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
                original: AccessTools.Method(typeof(DubsBadHygiene.JobGiver_UseToilet), "TryGiveJob"),
                prefix: new HarmonyMethod(typeof(JobGiver_UseToilet_TryGiveJob_Patch), nameof(JobGiver_UseToilet_TryGiveJob_Patch.Prefix)),
                info: "DubsBadHygiene.JobGiver_UseToilet.TryGiveJob"
            );
            patchFunctionPostfix(
                original: AccessTools.Method(typeof(DubsBadHygiene.JobGiver_UseToilet), "TryGiveJob"),
                postfix: new HarmonyMethod(typeof(JobGiver_UseToilet_TryGiveJob_Patch), nameof(JobGiver_UseToilet_TryGiveJob_Patch.Postfix)),
                info: "DubsBadHygiene.JobGiver_UseToilet.TryGiveJob"
            );

            patchFunctionPostfix(
                original: AccessTools.Method(typeof(HealthCardUtility), "GetPawnCapacityTip"),
                postfix: new HarmonyMethod(typeof(HealthCardUtility_GetPawnCapacityTip_Patch), nameof(HealthCardUtility_GetPawnCapacityTip_Patch.Postfix)),
                info: "HealthCardUtility.GetPawnCapacityTip"
            );
            /*patchFunctionPostfix(
                original: AccessTools.Method(typeof(Pawn_NeedsTracker), "ShouldHaveNeed", new[] { typeof(NeedDef) }),
                postfix: new HarmonyMethod(typeof(Patch_ShouldHaveNeed), nameof(Patch_ShouldHaveNeed.Postfix)),
                info: "Pawn_NeedsTracker.ShouldHaveNeed"
            );*/
            patchFunctionPrefix(
                original: AccessTools.Method(typeof(Pawn_NeedsTracker), "ShouldHaveNeed", new[] { typeof(NeedDef) }),
                prefix: new HarmonyMethod(typeof(Patch_ShouldHaveNeed_Debug), nameof(Patch_ShouldHaveNeed_Debug.Prefix)),
                info: "Pawn_NeedsTracker.ShouldHaveNeed (DubsBadHygiene conflict solved)"
            );
            patchFunctionPostfix(
                original: AccessTools.Method(typeof(Pawn_NeedsTracker), "ShouldHaveNeed", new[] { typeof(NeedDef) }),
                postfix: new HarmonyMethod(typeof(Patch_ShouldHaveNeed_Debug), nameof(Patch_ShouldHaveNeed_Debug.Postfix)),
                info: "Pawn_NeedsTracker.ShouldHaveNeed (DubsBadHygiene conflict solved)"
            );

            var AddAndRemoveMethod = AccessTools.Method(typeof(PawnComponentsUtility), nameof(PawnComponentsUtility.AddAndRemoveDynamicComponents), new[] { typeof(Pawn), typeof(bool) });
            patchFunctionPrefix(
                original: AddAndRemoveMethod,
                prefix: new HarmonyMethod(typeof(Patch_PawnComponents_AddRemove), nameof(Patch_PawnComponents_AddRemove.Prefix)),
                info: "PawnComponentsUtility.AddAndRemoveDynamicComponents"
            );
            patchFunctionPostfix(
                original: AddAndRemoveMethod,
                postfix: new HarmonyMethod(typeof(Patch_PawnComponents_AddRemove), nameof(Patch_PawnComponents_AddRemove.Postfix)),
                info: "PawnComponentsUtility.AddAndRemoveDynamicComponents"
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

            // The following 4 function limits pawns from wearing underwear or diapers on their own and solve this by seeking medical rest for getting changed
            patchFunctionPostfix(
                original: AccessTools.Method(typeof(JobGiver_OptimizeApparel), "TryGiveJob"),
                postfix: new HarmonyMethod(typeof(JobGiver_OptimizeApparel_TryGiveJob_Patch), nameof(JobGiver_OptimizeApparel_TryGiveJob_Patch.Postfix)),
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
            patchFunctionPrefix(
                original: AccessTools.Method(typeof(CompStudiable), nameof(CompStudiable.StudyUnlocked)),
                prefix: new HarmonyMethod(typeof(CompStudiable_StudyUnlocked_ManualPrefix), nameof(CompStudiable_StudyUnlocked_ManualPrefix.Prefix)),
                info: "CompStudiable.StudyUnlocked"
            );
            patchFunctionPostfix(
                original: AccessTools.Method(typeof(Corpse), nameof(Corpse.TickRare)),
                postfix: new HarmonyMethod(typeof(Patch_Corpse_TickRare), nameof(Patch_Corpse_TickRare.Postfix)),
                info: "Corpse.TickRare"
            );
            patchFunctionPrefix(
                original: AccessTools.Method(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Wear)),
                prefix: new HarmonyMethod(typeof(Patch_Wear_SplitStack), nameof(Patch_Wear_SplitStack.Prefix)),
                info: "Pawn_ApparelTracker.Wear"
            );

            // Functions after this are all related to "LearningForAdults.cs"
            patchFunctionPrefix(
                original: AccessTools.Method(typeof(LearningGiver), nameof(LearningGiver.CanDo)),
                prefix: new HarmonyMethod(typeof(Patch_LearningGiver_CanDo), nameof(Patch_LearningGiver_CanDo.Prefix)),
                info: "LearningGiver.CanDo"
            );

            patchFunctionPrefix(
                original: AccessTools.Method(typeof(LearningUtility), nameof(LearningUtility.LearningSatisfied)),
                prefix: new HarmonyMethod(typeof(Patch_LearningUtility_LearningSatisfied), nameof(Patch_LearningUtility_LearningSatisfied.Prefix)),
                info: "LearningUtility.LearningSatisfied"
            );

            patchFunctionPostfix(
                original: AccessTools.Method(typeof(Pawn), nameof(Pawn.GetGizmos)),
                postfix: new HarmonyMethod(typeof(Patch_Pawn_GetGizmos), nameof(Patch_Pawn_GetGizmos.Postfix)),
                info: "Pawn.GetGizmos"
            );
            patchFunctionPostfix(
                original: AccessTools.PropertyGetter(typeof(Need), "ShowOnNeedList"),
                postfix: new HarmonyMethod(typeof(Patch_Learning_ShowOnNeedList), nameof(Patch_Learning_ShowOnNeedList.Postfix)),
                info: "Need(Learning).get_ShowOnNeedList"
            );
            patchFunctionPostfix(
                original: AccessTools.Method(typeof(ThinkNode_Priority_Learn), nameof(ThinkNode_Priority_Learn.GetPriority)),
                postfix: new HarmonyMethod(typeof(Patch_ThinkNode_Priority_Learn), nameof(Patch_ThinkNode_Priority_Learn.Postfix)),
                info: "ThinkNode_Priority_Learn.GetPriority"
            );

            // Protective functions to prevent unwanted behaviours
            patchFunctionPrefix(
                original: AccessTools.Method(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.ExistingLovePartner)),
                prefix: new HarmonyMethod(typeof(Patch_LovePartnerRelationUtility_ExistingLovePartner), nameof(Patch_LovePartnerRelationUtility_ExistingLovePartner.Prefix)),
                info: "LovePartnerRelationUtility.ExistingLovePartner"
            );
            patchFunctionPrefix(
                original: AccessTools.Method(typeof(JobGiver_MarryAdjacentPawn), "CanMarry"),
                prefix: new HarmonyMethod(typeof(Patch_JobGiver_MarryAdjacentPawn_CanMarry), nameof(Patch_JobGiver_MarryAdjacentPawn_CanMarry.Prefix)),
                info: "JobGiver_MarryAdjacentPawn.CanMarry"
            );
            patchFunctionPrefix(
                original: AccessTools.Method(typeof(InteractionWorker_RomanceAttempt), nameof(InteractionWorker_RomanceAttempt.SuccessChance)),
                prefix: new HarmonyMethod(typeof(Patch_InteractionWorker_RomanceAttempt), nameof(Patch_InteractionWorker_RomanceAttempt.SuccessChance_Prefix)),
                info: "InteractionWorker_RomanceAttempt.SuccessChance"
            );
            patchFunctionPrefix(
                original: AccessTools.Method(typeof(InteractionWorker_RomanceAttempt), nameof(InteractionWorker_RomanceAttempt.RandomSelectionWeight)),
                prefix: new HarmonyMethod(typeof(Patch_InteractionWorker_RomanceAttempt), nameof(Patch_InteractionWorker_RomanceAttempt.RandomSelectionWeight_Prefix)),
                info: "InteractionWorker_RomanceAttempt.RandomSelectionWeight"
            );


            Patch_FailOnChildLearningConditions_ForAllJobDrivers();


            var LearningDrawOnGui = AccessTools.Method(
                typeof(Need_Learning),
                nameof(Need_Learning.DrawOnGUI),
                new[]
                {
                    typeof(Rect),
                    typeof(int),
                    typeof(float),
                    typeof(bool),
                    typeof(bool),
                    typeof(Rect?).MakeByRefType().GetElementType() ?? typeof(Rect?), // safer for some compilers
                    typeof(bool)
                }
            );
            patchFunctionPrefix(
                original: LearningDrawOnGui,
                prefix: new HarmonyMethod(typeof(Patch_NeedLearning_DrawOnGUI), nameof(Patch_NeedLearning_DrawOnGUI.Prefix)),
                info: "Need_Learning.DrawOnGUI"
            );
            patchFunctionPostfix(
                original: AccessTools.PropertyGetter(typeof(Need_Learning), "IsFrozen"),
                postfix: new HarmonyMethod(typeof(Need_Learning_IsFrozen), nameof(Need_Learning_IsFrozen.Postfix)),
                info: "Property Need_Learning.IsFrozen"
            );
            // Functions until this are all related to "LearningForAdults.cs"

            // Functions after this are all related to "BabyInteractions.cs"
            patchFunctionPostfix(
                original: AccessTools.Method(typeof(ChildcareUtility), nameof(ChildcareUtility.CanSuckle)),
                postfix: new HarmonyMethod(typeof(Patch_ChildcareUtility_CanSuckle), nameof(Patch_ChildcareUtility_CanSuckle.Postfix)),
                info: "ChildcareUtility.CanSuckle"
            );
            patchFunctionPostfix(
                original: AccessTools.Method(typeof(WorkGiver_PlayWithBaby), nameof(WorkGiver_PlayWithBaby.HasJobOnThing)),
                postfix: new HarmonyMethod(typeof(Patch_WorkGiver_PlayWithBaby_HasJobOnThing), nameof(Patch_WorkGiver_PlayWithBaby_HasJobOnThing.Postfix)),
                info: "WorkGiver_PlayWithBaby.HasJobOnThing"
            );
            patchFunctionPrefix(
                original: AccessTools.Method(typeof(JobDriver_BottleFeedBaby), "MakeNewToils"),
                prefix: new HarmonyMethod(typeof(Patch_BottleFeedBaby_HandleDispenser), nameof(Patch_BottleFeedBaby_HandleDispenser.Prefix)),
                info: "JobDriver_BottleFeedBaby.MakeNewToils"
            );
            // Functions until this are all related to "BabyInteractions.cs"

            if (ModChecker.SpeakUpActive())
            {
                if (settings.debugging) Log.Message($"[ZI]ZealousInnocence is patching for SpeakUp.");
                /*var extraGrammarPawn = AccessTools.Method(AccessTools.TypeByName("SpeakUp.ExtraGrammarUtility")
               ?? AccessTools.TypeByName("ExtraGrammarUtility"),
               "ExtraRulesForPawn");
                patchFunctionPostfix(
                    original: extraGrammarPawn,
                    postfix: new HarmonyMethod(typeof(Patch_ExtraGrammarUtility_ExtraRulesForPawn), nameof(Patch_ExtraGrammarUtility_ExtraRulesForPawn.Postfix)),
                    info: "SpeakUp.ExtraGrammarUtility.ExtraRulesForPawn"
                );*/
                patchFunctionPrefix(
                    original: AccessTools.Method(typeof(SocialInteractionUtility), nameof(SocialInteractionUtility.CanInitiateRandomInteraction)),
                    prefix: new HarmonyMethod(typeof(Patch_SocialInteractionUtility_CanInitiateRandomInteraction), nameof(Patch_SocialInteractionUtility_CanInitiateRandomInteraction.Prefix)),
                    info: "SocialInteractionUtility.CanInitiateRandomInteraction"
                );
                patchFunctionPostfix(
                    original: AccessTools.Method(typeof(GrammarUtility), nameof(GrammarUtility.RulesForPawn), new System.Type[] { typeof(string), typeof(Pawn), typeof(Dictionary<string, string>), typeof(bool), typeof(bool) }),
                    postfix: new HarmonyMethod(typeof(Patch_GrammarUtility_RulesForPawn), nameof(Patch_GrammarUtility_RulesForPawn.Postfix)),
                    info: "GrammarUtility.RulesForPawn"
                );

                var speakUp = AccessTools.Method(AccessTools.TypeByName("SpeakUp.DialogManager")
               ?? AccessTools.TypeByName("DialogManager"),
               "FireStatement");
                if (speakUp != null)
                {
                    patchFunctionPostfix(
                        original: speakUp,
                        postfix: new HarmonyMethod(typeof(SpeakUp_DialogManager), nameof(SpeakUp_DialogManager.FireStatement_Postfix)),
                        info: "SpeakUp.DialogManager.FireStatement"
                    );
                }
                else Log.Warning($"[ZI] Bind failed for SpeakUp.DialogManager");
            }

            ModChecker.ZealousInnocenceActive();

            if (Helper_Toddlers.ToddlersLoaded)
            {
                if(settings.debugging) Log.Message($"[ZI]ZealousInnocence is patching for Toddlers.");
                var isToddler = AccessTools.Method(AccessTools.TypeByName("Toddlers.ToddlerUtility")
                               ?? AccessTools.TypeByName("ToddlerUtility"),
                               "IsToddler");
                if (isToddler != null)
                {
                    patchFunctionPrefix(
                        original: isToddler,
                        prefix: new HarmonyMethod(typeof(Patch_ToddlerUtility), nameof(Patch_ToddlerUtility.IsToddler)),
                        info: "Toddlers.ToddlerUtility.IsToddler"
                    );
                }
                else Log.Warning($"[ZI] Bind failed for Toddlers.ToddlerUtility.IsToddler");

                var percentGrowth = AccessTools.Method(AccessTools.TypeByName("Toddlers.ToddlerUtility")
                               ?? AccessTools.TypeByName("ToddlerUtility"),
                               "PercentGrowth");
                if (percentGrowth != null)
                {
                    patchFunctionPrefix(
                        original: percentGrowth,
                        prefix: new HarmonyMethod(typeof(Patch_ToddlerUtility), nameof(Patch_ToddlerUtility.PercentGrowth)),
                        info: "Toddlers.ToddlerUtility.PercentGrowth"
                    );
                }
                else Log.Warning($"[ZI] Bind failed for Toddlers.ToddlerUtility.PercentGrowth");

               var optimizeBabyApparel = AccessTools.Method(AccessTools.TypeByName("Toddlers.JobGiver_OptimizeBabyApparel")
               ?? AccessTools.TypeByName("JobGiver_OptimizeBabyApparel"),
               "TryGiveJob");
                if (optimizeBabyApparel != null)
                {
                    patchFunctionPostfix(
                        original: optimizeBabyApparel,
                        postfix: new HarmonyMethod(typeof(Patch_JobGiver_OptimizeBabyApparel_TryGiveJob), nameof(Patch_JobGiver_OptimizeBabyApparel_TryGiveJob.Postfix)),
                        info: "Toddlers.JobGiver_OptimizeBabyApparel.TryGiveJobg"
                    );
                }
                else Log.Warning($"[ZI] Bind failed for Toddlers.JobGiver_OptimizeBabyApparel.TryGiveJob");
            }

            if (!ModChecker.ForeverYoungActive())
            {
                if (settings.debugging) Log.Message($"[ZI]ZealousInnocence is patching because ForeverYoung is not available.");
                // Can allow or disallow Rols for children (again)
                patchFunctionPrefix(
                    original: AccessTools.Method(typeof(Precept_Role), nameof(Precept_Role.RequirementsMet)),
                    prefix: new HarmonyMethod(typeof(PreceptRole_RequirementsMet), nameof(PreceptRole_RequirementsMet.Prefix)),
                    info: "Precept_Role.RequirementsMet"
                );
            }
            Log.Message($"[ZI]ZealousInnocence executed {PostfixCount+PrefixCount+PrefixGenericCount} harmony patches. {PrefixCount} Prefix, {PostfixCount} Postfix, {PrefixGenericCount} Prefix Generics. Startup completed.");
        }
        private void Patch_FailOnChildLearningConditions_ForAllJobDrivers()
        {
            // 1) Get the OPEN generic definition: FailOnChildLearningConditions<T>(this Toil toil)
            var openGen = typeof(ToilFailConditions)
    .GetMethods(BindingFlags.Public | BindingFlags.Static)
    .First(m => m.Name == "FailOnChildLearningConditions"
             && m.IsGenericMethodDefinition
             && m.GetGenericArguments().Length == 1
             && m.GetParameters().Length == 1);

            if (openGen == null)
            {
                Log.Error("[ZI] Could not locate ToilFailConditions.FailOnChildLearningConditions<T>(Toil).");
                return;
            }

            // 2) Prepare your prefix HarmonyMethod
            var prefixMI = typeof(Patch_FailOnChildLearningConditions).GetMethod(nameof(Patch_FailOnChildLearningConditions.Prefix), BindingFlags.Static | BindingFlags.Public);
            if (prefixMI == null)
            {
                Log.Error("[ZI] Prefix method not found: Patch_FailOnChildLearningConditions.Prefix");
                return;
            }
            var prefixHM = new HarmonyMethod(prefixMI);

            // 3) Enumerate all concrete, non-generic JobDriver subclasses across loaded assemblies
            IEnumerable<Type> jobDriverTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
                })
                .Where(t => typeof(JobDriver).IsAssignableFrom(t)
                            && !t.IsAbstract
                            && !t.ContainsGenericParameters)
                .Distinct();
            /*
            // RimWorld itself has only the following drivers that cause problem, but mods can add a lot of trouble
            Type[] jobDriverTypes = {
                typeof(JobDriver_Floordrawing), typeof(JobDriver_Workwatching), typeof(JobDriver_Lessontaking), typeof(JobDriver_Skydreaming), typeof(JobDriver_Radiotalking), typeof(JobDriver_NatureRunning), typeof(JobDriver_Reading)
            };*/

            // 4) For each JobDriver<T>, construct the closed generic and patch it
            foreach (var jdType in jobDriverTypes)
            {
                MethodInfo constructed;
                try
                {
                    constructed = openGen.MakeGenericMethod(jdType);
                }
                catch
                {
                    // Skip odd types Harmony/CLR won't accept
                    continue;
                }

                patchFunctionPrefixGeneric(
                    original: constructed,
                    prefix: prefixMI,
                    info: "ToilFailConditions.FailOnChildLearningConditions<{jdType.FullName}>"
                );
            }
        }
        private void patchFunctionPostfix(MethodInfo original, HarmonyMethod postfix, string info)
        {
            PostfixCount++;
            bool checkResult = DoCheckOnHarmonyMethode(original, info, false, true);
            harmony.Patch(
                original: original,
                postfix: postfix
            );
            if (settings.debuggingHarmonyPatching || checkResult) Log.Message($"[ZI]Harmony patching: Postfix {info}");
        }
        private void patchFunctionPrefix(MethodInfo original, HarmonyMethod prefix, string info)
        {
            PrefixCount++;
            bool checkResult = DoCheckOnHarmonyMethode(original, info, true, false);
            harmony.Patch(
                original: original,
                prefix: prefix
            );
            if (settings.debuggingHarmonyPatching || checkResult) Log.Message($"[ZI]Harmony patching: Prefix {info}");
        }
        private void patchFunctionPrefixGeneric(MethodInfo original, HarmonyMethod prefix, string info)
        {
            PrefixGenericCount++;
            bool checkResult = DoCheckOnHarmonyMethode(original, info, true, false);
            harmony.Patch(
                original: original,
                prefix: prefix
            );
            if (settings.debuggingHarmonyPatching || checkResult) Log.Message($"[ZI]Harmony patching: Prefix {info}");
        }

        public bool DoCheckOnHarmonyMethode(MethodInfo originalMethod, string info, bool checkPrefix = true, bool checkPostfix = true)
        {
            var patches = Harmony.GetPatchInfo(originalMethod);
            bool patchOfSearched = false;
            if (patches != null)
            {
                if (checkPrefix)
                {
                    foreach (var prefix in patches.Prefixes)
                    {
                        Log.Warning($"[ZI]Already existing Prefix found on {info} from {prefix.owner}! Possible conflict detected!");
                        patchOfSearched = true;
                    }
                }
                if (checkPostfix)
                {
                    foreach (var postfix in patches.Postfixes)
                    {
                        Log.Warning($"[ZI]Already existing Postfix found on {info} from {postfix.owner}! Possible conflict detected!");
                        patchOfSearched = true;
                    }
                }
            }
            return patchOfSearched;
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
        public static bool ToddlersActive()
        {
            return ModLister.GetActiveModWithIdentifier("cyanobot.toddlers") != null;
        }
        public static bool SpeakUpActive()
        {
            return ModLister.GetActiveModWithIdentifier("JPT.speakup") != null;
        }
        public static bool ZealousInnocenceActive()
        {
            return ModLister.GetActiveModWithIdentifier("proximo.zealousinnocence") != null;
        }
    }



    [HarmonyPatch(typeof(Precept_Role), nameof(Precept_Role.RequirementsMet))]
    static class PreceptRole_RequirementsMet
    {
        public static bool Prefix(ref Pawn p, ref bool __result, ref Precept_Role __instance)
        {
            if (ModsConfig.IdeologyActive)
            {
                if (!Hediff_MentalRegression.CanHaveRole(p)) return true;

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
                if(customLabel == "")
                {
                    return $"{customString}";
                }
                return $"{customLabel}: {customString}";
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
                    if (apparel.stackCount == 1)
                    {
                        if (apparel.Wearer != null && apparel.def.thingCategories.Contains(ThingCategoryDefOf.Diapers))
                        {
                            texPath += "_Worn";
                        }
                        if (hpPercentage < 0.51f)
                        {
                            texPath += "_Dirty";
                        }

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
                __result = (Helper_Diaper.needsDiaper(pawn) && Helper_Diaper.acceptsDiaper(pawn)) || Helper_Diaper.getDiaperPreference(pawn) == DiaperLikeCategory.Liked || Helper_Diaper.getDiaperPreference(pawn) == DiaperLikeCategory.Diaper_Lover;
                if (debugging)  Log.Message($"[ZI]Apparel DEBUG: Diaper {pawn.LabelShort} value {__result} based on needsDiaper {Helper_Diaper.needsDiaper(pawn)} or cat {Helper_Diaper.getDiaperPreference(pawn) == DiaperLikeCategory.Liked} or cat {Helper_Diaper.getDiaperPreference(pawn) == DiaperLikeCategory.Diaper_Lover} physical age {pawn.ageTracker.AgeBiologicalYears}");

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
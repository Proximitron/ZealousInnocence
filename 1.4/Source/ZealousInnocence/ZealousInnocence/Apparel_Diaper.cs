using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ZealousInnocence
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            Harmony.DEBUG = false;
            Harmony harmony = new Harmony("ZealousInnocence");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("ZealousInnocence harmony patching: JobGiver_OptimizeApparel.ApparelScoreGain");
        }
    }
    /// <summary>
    /// Testimplementation
    /// </summary>

    [DefOf]
    public static class ThingCategoryDefOf
    {
        public static ThingCategoryDef Diapers;
        public static ThingCategoryDef Onesies;
        static ThingCategoryDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RimWorld.ThingCategoryDefOf));
        }
    }

    [HarmonyPatch(typeof(JobGiver_OptimizeApparel))]
    public static class Patch_JobGiver_OptimizeApparel
    {
        [HarmonyPostfix]
        [HarmonyPatch("ApparelScoreRaw")]
        public static float ApparelScoreRaw(float __result,Pawn pawn, Apparel ap)
        {
            
            if (ap.HasThingCategory(ThingCategoryDefOf.Diapers))
            {
                __result -= 0.5f; // Diapers by default less likely to be worn

                if (DiaperHelper.needsDiaper(pawn))
                {
                    __result += 5f;
                }
                var preference = DiaperHelper.getDiaperPreference(pawn);
                    
                if (preference == DiaperLikeCategory.Liked)
                {
                    __result += 10f;
                }
                else if (preference == DiaperLikeCategory.Disliked)
                {
                    __result -= 10f;
                }

                //Log.Message("Apparel " + ap.Label + " is diaper and rated " + __result + " for " + pawn.LabelShort);
                //Log.Message($"ApparelScoreRaw end Running for pawn {pawn?.LabelShort}");
            }
            else if (ap.HasThingCategory(ThingCategoryDefOf.Onesies))
            {
                __result -= 0.25f; // Onesies by default are less likely to be worn

                var preference = OnesieHelper.getOnesiePreference(pawn);
                if(preference == OnesieLikeCategory.Liked)
                {
                    __result += 5f;
                }
                else if(preference == OnesieLikeCategory.Disliked)
                {
                    __result -= 10f;
                }
                Log.Message("Apparel " + ap.Label + " is onesie and rated " + __result + " for " + pawn.LabelShort);
            }
            //JobGiver_OptimizeApparel.ApparelScoreRaw
            //JobGiver_OptimizeApparel.ApparelScoreGain
            return __result;

        }
    }
    //public static float ApparelScoreGain(Pawn pawn, Apparel ap, List<float> wornScoresCache)


    [DefOf]
    public static class PreceptDefOf
    {
        public static PreceptDef Diapers_Loved;
        public static PreceptDef Diapers_Neutral;
        public static PreceptDef Diapers_Hated;

        public static PreceptDef Onesies_Loved;
        public static PreceptDef Onesies_Neutral;
        public static PreceptDef Onesies_Hated;

        static PreceptDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RimWorld.PreceptDefOf));
        }
    }

    [StaticConstructorOnStartup]
    public class Apparel_Diaper_Base : Apparel
    {
        public override float GetSpecialApparelScoreOffset()
        {
            
            return base.GetSpecialApparelScoreOffset();
        }
    }

    public class ThoughtWorker_Diaper_Dressed : ThoughtWorker
    {
        public enum DiaperSituationCategoryThought : int
        {
            Not_Required_Clean,
            Not_Required_Used,
            Not_Required_Spent,
            Not_Required_Trashed,

            Required_Neutral_None,
            Required_Neutral_Clean,
            Required_Neutral_Used,
            Required_Neutral_Spent,
            Required_Neutral_Trashed,

            Loved_None,
            Loved_Clean,
            Loved_Used,
            Loved_Spent,
            Loved_Trashed,

            Hated_Clean,
            Hated_AnyOther
        }

        [DefOf]
        public static class BodyPartGroupDefOf
        {
            static BodyPartGroupDefOf()
            {
                DefOfHelper.EnsureInitializedInCtor(typeof(RimWorld.BodyPartGroupDefOf));
            }
            public static BodyPartGroupDef Waist;
        }

        [StaticConstructorOnStartup]
        public static class DiaperThoughts
        {
            public static ThingDef ApparelMakeableBase = DefDatabase<ThingDef>.GetNamedSilentFail("blabla");
        }

        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            var preference = DiaperHelper.getDiaperPreference(p);
            var currDiapie = DiaperHelper.getDiaper(p);
            var diaperRequired = DiaperHelper.needsDiaper(p);
            if (currDiapie != null)
            {
                switch (p.needs.TryGetNeed<Need_Diaper>().CurCategory)
                {
                    case DiaperSituationCategory.Clean:
                        switch (preference)
                        {
                            case DiaperLikeCategory.Liked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Loved_Clean);
                            case DiaperLikeCategory.Disliked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Hated_Clean);
                            default:
                                if (diaperRequired)
                                {
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Required_Neutral_Clean);
                                }
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Not_Required_Clean);
                        }
                    case DiaperSituationCategory.Used:
                        switch (preference)
                        {
                            case DiaperLikeCategory.Liked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Loved_Used);
                            case DiaperLikeCategory.Disliked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Hated_AnyOther);
                            default:
                                if (diaperRequired)
                                {
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Required_Neutral_Used);
                                }
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Not_Required_Used);
                        }
                    case DiaperSituationCategory.Spent:
                        switch (preference)
                        {
                            case DiaperLikeCategory.Liked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Loved_Spent);
                            case DiaperLikeCategory.Disliked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Hated_AnyOther);
                            default:
                                if (diaperRequired)
                                {
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Required_Neutral_Spent);
                                }
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Not_Required_Spent);
                        }
                    case DiaperSituationCategory.Trashed:
                        switch (preference)
                        {
                            case DiaperLikeCategory.Liked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Loved_Trashed);
                            case DiaperLikeCategory.Disliked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Hated_AnyOther);
                            default:
                                if (diaperRequired)
                                {
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Required_Neutral_Trashed);
                                }
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Not_Required_Trashed);
                        }
                    default:
                        throw new NotImplementedException();
                }
            }

            if (preference == DiaperLikeCategory.Liked)
            {
                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Loved_None);
            }
            else if (preference != DiaperLikeCategory.Disliked && diaperRequired)
            {
                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Required_Neutral_None);
            }
            return ThoughtState.Inactive;
        }
    }
}

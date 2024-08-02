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

    /// <summary>
    /// Testimplementation
    /// </summary>

    [DefOf]
    public static class ThingCategoryDefOf
    {
        public static ThingCategoryDef Diapers;
        public static ThingCategoryDef Onesies;
        public static ThingCategoryDef DiapersNight;
        public static ThingCategoryDef Underwear;
        public static ThingCategoryDef MaleCloth;
        public static ThingCategoryDef FemaleCloth;
        static ThingCategoryDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RimWorld.ThingCategoryDefOf));
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

        public static PreceptDef CribBed_Preferred;
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
    [StaticConstructorOnStartup]
    public class Apparel_Underwear_Base : Apparel
    {
        public override float GetSpecialApparelScoreOffset()
        {

            return base.GetSpecialApparelScoreOffset();
        }
    }
    public enum BedwettingSituationCategoryThought : int
    {
        // Default behaviour if nothing else applies
        Wet_Bed_Default,
        Wet_Bed_Default_Diaper,
        Wet_Bed_Default_Liked,

        // Thoughts of bedwetter
        Wet_Bed_Bedwetter,
        Wet_Bed_Bedwetter_Diaper,
        Wet_Bed_Bedwetter_Liked,

        // Thoughts of non-adult (child)
        Wet_Bed_Non_Adult,
        Wet_Bed_Non_Adult_Diaper,
    }

    public class ThoughtWorker_Diaper_Dressed : ThoughtWorker
    {
        public enum DiaperSituationCategoryThought : int
        {
            // Thoughts if diaper is not required but still worn
            Not_Required_Clean,
            Not_Required_Used,
            Not_Required_Spent,
            Not_Required_Trashed,

            // Thoughts if diaper is nessesary but otherwise not expecially liked
            Required_Neutral_None,
            Required_Neutral_Clean,
            Required_Neutral_Used,
            Required_Neutral_Spent,
            Required_Neutral_Trashed,

            // Thoughts if diaper is liked
            Loved_None,
            Loved_Clean,
            Loved_Used,
            Loved_Spent,
            Loved_Trashed,

            // Thoughts if diaper is hated
            Hated_Clean,
            Hated_AnyOther,

            // Thoughts of non-adult (child)
            Non_Adult_Required_None,
            Non_Adult_Required_Night_Protection_None,
            Non_Adult_Required_Night_Protection_Clean,
            Non_Adult_Clean,
            Non_Adult_Used,
            Non_Adult_Other,

            // Thoughts if night diaper is nessesary, but normal diaper is worn!
            Night_Protection_Diaper_Clean,
            Night_Protection_Diaper_Used,
            Night_Protection_Diaper_Worse,

            // Thoughts if diaper is nessesary but only at night and night diapers are worn
            Night_Protection_Required_None, // Thoughts if diaper is nessesary at night, not worn, but otherwise not expecially liked
            Night_Protection_Required_Clean,
            Night_Protection_Required_Used,
            Night_Protection_Required_Worse,

            // Thoughts if night diapers are unnessesary, but worn
            Night_Protection_Not_Required_Clean,
            Night_Protection_Not_Required_Used,
            Night_Protection_Not_Required_Worse,

            //START: Thoughts if underwear is requested/wanted or wrongfully worn
            Underwear_Not_Worn,
            Underwear_Worn,
            Underwear_Worn_Non_Adult,
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
            var diaperRequiredNight = DiaperHelper.needsDiaperNight(p);
            var nonAdult = !p.ageTracker.Adult;
            if (currDiapie != null)
            {
                switch (p.needs.TryGetNeed<Need_Diaper>().CurCategory)
                {
                    case DiaperSituationCategory.Clean:
                        switch (preference)
                        {
                            case DiaperLikeCategory.NonAdult:
                                if(DiaperHelper.isNightDiaper(currDiapie)) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Non_Adult_Required_Night_Protection_Clean);
                                else return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Non_Adult_Clean);
                            case DiaperLikeCategory.Liked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Loved_Clean);
                            case DiaperLikeCategory.Disliked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Hated_Clean);
                            default:
                                if (diaperRequired)
                                {
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Required_Neutral_Clean);
                                }
                                if (DiaperHelper.isNightDiaper(currDiapie))
                                {
                                    if (diaperRequiredNight)
                                    {
                                        return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Required_Clean);
                                    }
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Not_Required_Clean);
                                }
                                else
                                {
                                    if (diaperRequiredNight) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Diaper_Clean);
                                }
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Not_Required_Clean);
                        }
                    case DiaperSituationCategory.Used:
                        switch (preference)
                        {
                            case DiaperLikeCategory.NonAdult:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Non_Adult_Used);
                            case DiaperLikeCategory.Liked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Loved_Used);
                            case DiaperLikeCategory.Disliked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Hated_AnyOther);
                            default:
                                if (diaperRequired)
                                {
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Required_Neutral_Used);
                                }
                                if (DiaperHelper.isNightDiaper(currDiapie))
                                {
                                    if (diaperRequiredNight)
                                    {
                                        return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Required_Used);
                                    }
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Not_Required_Used);
                                }
                                else
                                {
                                    if (diaperRequiredNight) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Diaper_Used);
                                }
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Not_Required_Used);
                        }
                    case DiaperSituationCategory.Spent:
                        switch (preference)
                        {
                            case DiaperLikeCategory.NonAdult:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Non_Adult_Other);
                            case DiaperLikeCategory.Liked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Loved_Spent);
                            case DiaperLikeCategory.Disliked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Hated_AnyOther);
                            default:
                                if (diaperRequired)
                                {
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Required_Neutral_Spent);
                                }
                                if (DiaperHelper.isNightDiaper(currDiapie))
                                {
                                    if (diaperRequiredNight)
                                    {
                                        return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Required_Worse);
                                    }
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Not_Required_Worse);
                                }
                                else
                                {
                                    if (diaperRequiredNight) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Diaper_Worse);
                                }
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Not_Required_Spent);
                        }
                    case DiaperSituationCategory.Trashed:
                        switch (preference)
                        {
                            case DiaperLikeCategory.NonAdult:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Non_Adult_Other);
                            case DiaperLikeCategory.Liked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Loved_Trashed);
                            case DiaperLikeCategory.Disliked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Hated_AnyOther);
                            default:
                                if (diaperRequired)
                                {
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Required_Neutral_Trashed);
                                }
                                if (DiaperHelper.isNightDiaper(currDiapie))
                                {
                                    if (diaperRequiredNight)
                                    {
                                        return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Required_Worse);
                                    }
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Not_Required_Worse);
                                }
                                else
                                {
                                    if (diaperRequiredNight) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Diaper_Worse);
                                }
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Not_Required_Trashed);
                        }
                    default:
                        throw new NotImplementedException();
                }
            }

            // After this point it should be clear that no diaper is worn
            var currUnderpants = DiaperHelper.getUnderwear(p);

            if (preference == DiaperLikeCategory.NonAdult)
            {
                if(diaperRequired) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Non_Adult_Required_None);
                if(diaperRequiredNight) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Non_Adult_Required_Night_Protection_None);
                if(currUnderpants != null) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Underwear_Worn_Non_Adult);
                return ThoughtState.Inactive;
            }

            if (preference == DiaperLikeCategory.Liked)
            {
                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Loved_None);
            }
            else if (preference != DiaperLikeCategory.Disliked)
            {
                if(diaperRequired) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Required_None);
                if (diaperRequiredNight) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Required_Neutral_None);
            }
            if(currUnderpants == null) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Underwear_Not_Worn);
            return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Underwear_Worn);
        }
    }
}

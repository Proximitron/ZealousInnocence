using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ZealousInnocence
{
    public enum OnesieLikeCategory : int
    {
        Neutral,
        Liked,
        Disliked,
        Child,
        Toddler
    }
    public static class OnesieHelper
    {

        public static Apparel getOnesie(Pawn p)
        {
            List<Apparel> wornApparel = p?.apparel?.WornApparel;
            if (wornApparel != null)
            {
                for (int i = 0; i < wornApparel.Count; i++)
                {
                    List<ThingCategoryDef> cat = wornApparel[i].def.thingCategories;
                    if (cat == null)
                    {
                        return null;
                    }

                    if (cat.Contains(ThingCategoryDefOf.Onesies))
                    {
                        return wornApparel[i];
                    }
                }
            }
            return null;
        }
        public static OnesieLikeCategory getOnesiePreference(Pawn pawn)
        {
            if (!Helper_Regression.isAdultMental(pawn))
            {
                if(pawn.isBabyMental() || pawn.isToddlerMental())
                {
                    return OnesieLikeCategory.Toddler;
                }
                return OnesieLikeCategory.Child;
            }
                
        
            TraitSet traits = pawn?.story?.traits;
            if (traits == null)
            {
                return OnesieLikeCategory.Neutral;
            }

            if (pawn.story.traits.HasTrait(TraitDefOf.Potty_Rebel))
            {
                return OnesieLikeCategory.Liked;
            }
            if (pawn.story.traits.HasTrait(TraitDefOf.Big_Boy))
            {
                return OnesieLikeCategory.Disliked;
            }

            if (ModsConfig.IdeologyActive)
            {
                Ideo ideo = pawn.Ideo;
                if (ideo != null)
                {

                    if (ideo.HasPrecept(PreceptDefOf.Onesies_Loved))
                    {
                        return OnesieLikeCategory.Liked;
                    }
                    else if (ideo.HasPrecept(PreceptDefOf.Onesies_Hated))
                    {
                        return OnesieLikeCategory.Disliked;
                    }
                }
            }
            return OnesieLikeCategory.Neutral;
        }
    }

    public class ThoughtWorker_Onesie_Dressed : ThoughtWorker
    {
        public enum OnesieSituationCategoryThought : int
        {
            Neutral_Worn,
            Loved_Worn,
            Hated_Worn,
            Feels_Nice_Child,
            Feels_Nice_Toddler
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


        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (OnesieHelper.getOnesie(p) != null)
            {
                switch (OnesieHelper.getOnesiePreference(p))
                {
                    case OnesieLikeCategory.Neutral:
                        return ThoughtState.ActiveAtStage((int)OnesieSituationCategoryThought.Neutral_Worn);
                    case OnesieLikeCategory.Liked:
                        return ThoughtState.ActiveAtStage((int)OnesieSituationCategoryThought.Loved_Worn);
                    case OnesieLikeCategory.Disliked:
                        return ThoughtState.ActiveAtStage((int)OnesieSituationCategoryThought.Hated_Worn);
                    case OnesieLikeCategory.Child:
                        return ThoughtState.ActiveAtStage((int)OnesieSituationCategoryThought.Feels_Nice_Child);
                    case OnesieLikeCategory.Toddler:
                        return ThoughtState.ActiveAtStage((int)OnesieSituationCategoryThought.Feels_Nice_Toddler);
                    default:
                        throw new NotImplementedException();
                }
            }
            return ThoughtState.Inactive;
        }
    }
}

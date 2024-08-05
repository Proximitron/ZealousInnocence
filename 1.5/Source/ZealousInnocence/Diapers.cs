using DubsBadHygiene;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.Sound;
using Verse;
using static HarmonyLib.Code;
using Verse.AI;
using System.Drawing;
using System.Security.Cryptography;
using UnityEngine;

namespace ZealousInnocence
{
    public enum BedwettingDiaperThought : int
    {
        // Default behaviour if nothing else applies
        Wet_Diaper_Bed_Default,
        Wet_Diaper_Bed_Bedwetter,

        // Thoughts if diapers loved
        Wet_Diaper_Bed_Loved,
        Wet_Diaper_Bed_Bedwetter_Loved,

        // Thoughts of non-adult (child)
        Wet_Diaper_Bed_Non_Adult,
    }
    public enum WettingBedThought : int
    {
        // Default behaviour if nothing else applies
        Wet_Bed_Default,

        // Thoughts if bedwetter or low control be default
        Wet_Bed_Bedwetter,

        // Thoughts of non-adult (child)
        Wet_Bed_Non_Adult,
    }
    public enum WettingDiaperThought : int
    {
        // Default behaviour if nothing else applies
        Wet_Diaper_Default,

        // Thoughts if diapers loved
        Wet_Diaper_Loved,

        // Thoughts if diapers hated
        Wet_Diaper_Hated,

        // Thoughts of non-adult (child)
        Wet_Diaper_Non_Adult,
    }

    public enum WettingPantsThought : int
    {
        // Default behaviour if nothing else applies
        Wet_Pants_Default,

        // Thoughts if diapers loved
        Wet_Pants_Diaper_Loved,

        // Thoughts if diapers hated
        Wet_Pants_Diaper_Hated,

        // Thoughts of non-adult (child)
        Wet_Pants_Non_Adult,
    }

    public class Recipe_PutInDiaper : RecipeWorker
    {
        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {
            // we don't do that, we work on that ourselfs
        }
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            var liked = Helper_Diaper.getDiaperPreference(pawn);
            bool isOk = liked != DiaperLikeCategory.Disliked && (Helper_Diaper.needsDiaper(pawn) || Helper_Diaper.needsDiaperNight(pawn) || liked == DiaperLikeCategory.Liked);
            
            if (!isOk && IsViolationOnPawn(pawn, part, Faction.OfPlayer))
            {
                ReportViolation(pawn, billDoer, pawn.HomeFaction, liked == DiaperLikeCategory.Disliked ? -10 : - 5);
            }

            Apparel diapie = null;
            foreach (var thing in ingredients)
            {
                List<ThingCategoryDef> cat = thing.def.thingCategories;
                if (cat == null)
                {
                    continue;
                }
                
                if (cat.Contains(ThingCategoryDefOf.Diapers))
                {
                    diapie = (Apparel)thing;
                    break;
                }
            }
            //var thingWithComps2 = (Apparel)diapie;
            //thingWithComps2.DeSpawn();

            pawn.apparel.Wear(diapie, true);
            if (diapie.def.soundInteract != null)
            {
                diapie.def.soundInteract.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            }

            foreach (var curr in pawn.health.hediffSet.hediffs)
            {
                if (curr.def == RimWorld.HediffDefOf.Anesthetic)
                {
                    pawn.health.RemoveHediff(curr);
                    break;
                }
            }
        }
    }
   
    public class ThoughtWorker_Stink : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.Spawned)
            {
                float stinkLevel = 0.0f;
                foreach (Pawn pawn in p.Map.mapPawns.AllPawnsSpawned)
                {
                    if (pawn != p && pawn.needs.TryGetNeed<Need_Diaper>() != null)
                    {
                        if (IntVec3Utility.DistanceTo(p.Position, pawn.Position) <= 6 && pawn.needs.TryGetNeed<Need_Diaper>().CurLevel <= 0.6f)
                        {
                            stinkLevel += (pawn.needs.TryGetNeed<Need_Diaper>().CurLevel - 0.4f);
                        }
                    }
                }

                int targetTrait = p.story.traits.HasTrait(TraitDefOf.Potty_Rebel) ? 4 : 0;

                if (stinkLevel <= 0.0f)
                {
                    return ThoughtState.Inactive;
                }
                else if (stinkLevel > 2.5f)
                {
                    return ThoughtState.ActiveAtStage(targetTrait + 3);
                }
                else if (stinkLevel > 1.2f)
                {
                    return ThoughtState.ActiveAtStage(targetTrait + 2);
                }
                else
                {
                    return ThoughtState.ActiveAtStage(targetTrait + 1);
                }
            }
            else
            {
                return ThoughtState.Inactive;
            }
        }
    }
}

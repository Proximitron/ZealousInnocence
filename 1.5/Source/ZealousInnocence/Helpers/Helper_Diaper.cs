using DubsBadHygiene;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ZealousInnocence
{
    public static class Helper_Diaper
    {
        public static void addHediffToBladder(Pawn pawn, HediffDef defName)
        {
            BodyPartRecord bladder = Helper_Diaper.getBladderControlSourcePart(pawn);
            if (bladder == null)
            {
                return;
            }
            var newHediff = HediffMaker.MakeHediff(defName, pawn);
            pawn.health.AddHediff(newHediff, bladder);
        }
        public static void replaceBladderPart(Pawn pawn, HediffDef bladderSize)
        {
            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debugGenes = settings.debugging && settings.debuggingGenes;
            if (debugGenes) Log.Message($"Pawn {pawn.LabelShort} searching bladder");
            BodyPartRecord originalBladder = getBladderControlSourcePart(pawn);
            if (originalBladder != null)
            {
                // Remove the original Bladder part
                pawn.health.hediffSet.hediffs.RemoveAll(hediff => hediff.Part == originalBladder);
                if (debugGenes) Log.Message($"Pawn {pawn.LabelShort} removing old hediff");

                // Check if the new bladder part is already defined in the pawn's body
                BodyPartRecord newBladderPart = pawn.RaceProps.body.AllParts.Find(part => part.def == BodyPartDefOf.Bladder);

                if (newBladderPart != null)
                {
                    if (debugGenes) Log.Message($"Restore procedure to {pawn.LabelShort}");
                    pawn.health.AddHediff(RimWorld.HediffDefOf.MissingBodyPart, newBladderPart); // Mark the original bladder as missing
                    pawn.health.RestorePart(newBladderPart); // Restore the new bladder part
                    if (bladderSize == HediffDefOf.BigBladder)
                    {
                        addHediffToBladder(pawn, HediffDefOf.BigBladder);
                    }
                    if (bladderSize == HediffDefOf.SmallBladder)
                    {
                        addHediffToBladder(pawn, HediffDefOf.SmallBladder);
                    }

                }
            }
        }
        public static BodyPartRecord getBladderControlSourcePart(Pawn pawn)
        {
            foreach (var part in pawn.health.hediffSet.GetNotMissingParts())
            {
                if (part.def.tags.Contains(BodyPartTagDefOf.BladderControlSource))
                {
                    return part;
                }
            }
            return null;
        }
        public static void getMemory(Pawn pawn, ThoughtDef thought, int stage = 0)
        {
            var debugging = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().debugging;
            if (!pawn.health.capacities.CanBeAwake)
            {
                if (debugging) Log.Message("Can't gain Memory for unconcious " + pawn.Name + ", thought " + thought.defName + " at stage " + stage.ToString());
            }

            var memories = pawn.needs.mood.thoughts.memories;
            if (debugging) Log.Message("Creating Memory for " + pawn.Name + ", thought " + thought.defName + " at stage " + stage.ToString());

            Thought_Memory firstMemoryOfDef = memories.GetFirstMemoryOfDef(thought);
            if (firstMemoryOfDef != null)
            {
                firstMemoryOfDef.SetForcedStage(stage); // Making sure that merge works on TryGainMemory
            }
            var newThought = ThoughtMaker.MakeThought(thought, null);
            newThought.SetForcedStage(stage);
            memories.TryGainMemory(newThought);
        }
        public static void getMemory(Pawn pawn, WettingDiaperThought thought)
        {
            var defNormal = ThoughtDef.Named("DiaperPeed");
            Helper_Diaper.getMemory(pawn, defNormal, (int)thought);
        }
        public static void getMemory(Pawn pawn, BedwettingDiaperThought thought)
        {
            var defBed = ThoughtDef.Named("DiaperPeedBed");
            Helper_Diaper.getMemory(pawn, defBed, (int)thought);
        }
        public static void getMemory(Pawn pawn, WettingPantsThought thought)
        {
            var defPants = ThoughtDef.Named("PantsPeed");
            Helper_Diaper.getMemory(pawn, defPants, (int)thought);
        }
        public static void getMemory(Pawn pawn, WettingBedThought thought)
        {
            var defPants = ThoughtDef.Named("WetBed");
            Helper_Diaper.getMemory(pawn, defPants, (int)thought);
        }
        public static float CalculateProbability(float bladderControl)
        {
            // Base chance of 1%
            if (bladderControl > 1.2f) return 0.005f;

            // this only applies for bladder control under 100%
            bladderControl = Mathf.Clamp(bladderControl, 0f, 1f);

            if (bladderControl >= 0.8f)
            {
                // Range 1.0 to 0.75: 1%-5% chance
                return Mathf.Lerp(0.005f, 0.03f, Mathf.InverseLerp(1.2f, 0.8f, bladderControl));
            }
            else if (bladderControl >= 0.5f)
            {
                // Range 0.75 to 0.5: 5%-30% chance
                return Mathf.Lerp(0.05f, 0.20f, Mathf.InverseLerp(0.8f, 0.5f, bladderControl));
            }
            else if (bladderControl >= 0.15f)
            {
                // Range 0.5 to 0.25: 30%-80% chance
                return Mathf.Lerp(0.20f, 0.90f, Mathf.InverseLerp(0.5f, 0.15f, bladderControl));
            }
            else
            {
                // Range lower than 0.25: 100% chance
                return 1.0f;
            }
        }
        public static bool remembersPotty(Pawn pawn)
        {
            if (pawn != null && pawn.RaceProps.Humanlike)
            {
                var diaperNeed = pawn.needs.TryGetNeed<Need_Diaper>();
                if (diaperNeed == null) return true; // anything without that gets a free pass
                if (diaperNeed.IsHavingAccident) return true; // now we notice for sure
                if (!getBladderControlLevelCapable(pawn)) return false; // incapeable of noticing
                if (pawn.story.traits.HasTrait(TraitDef.Named("Potty_Rebel"))) return false; // will never run to a potty!

                // Mental states can make them unable to remember that
                MentalState mentalState = pawn.MentalState;
                if (mentalState != null && mentalState.def != null && (mentalState.def == MentalStateDefOf.PanicFlee || mentalState.def == MentalStateDefOf.Wander_Psychotic)) return false;

                var currDiapie = getDiaper(pawn);
                if (currDiapie != null && pawn.outfits.forcedHandler.IsForced(currDiapie)) return false; // forced in diapers


                float bladderControl = getBladderControlLevel(pawn);

                // 0.2 bladder control = 100%, 0.65 bladder control = 1%
                //float probability = Mathf.Clamp01(-2 * bladderControl + 1.4f);
                float probability = CalculateProbability(bladderControl);

                // Use the in-game day combined with pawn's birth year and birth day as the seed
                //int seed = GenDate.DaysPassed + pawn.ageTracker.BirthYear + pawn.ageTracker.BirthDayOfYear;
                var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
                var debugBedwetting = settings.debugging && settings.debuggingBedwetting && !pawn.Awake();
                var debug = settings.debugging && (settings.debuggingCapacities || debugBedwetting);
                if (Rand.ChanceSeeded(probability, diaperNeed.FailureSeed))
                {
                    if (debug) Log.Message($"JobGiver_UseToilet prefix: job denied, {pawn.Name.ToStringShort} at propability {probability} and seed {diaperNeed.FailureSeed}");
                    return false; // depending on the level on control
                }
                else
                {
                    if (debug) Log.Message($"JobGiver_UseToilet prefix: job given, {pawn.Name.ToStringShort} at propability {probability} and seed {diaperNeed.FailureSeed}");
                }
            }
            return true;
        }

        public static bool isWearingNightDiaper(Pawn pawn)
        {
            List<Apparel> wornApparel = pawn?.apparel?.WornApparel;
            if (wornApparel == null)
            {
                return false;
            }

            for (int i = 0; i < wornApparel.Count; i++)
            {
                Apparel apparel = wornApparel[i];

                if (isNightDiaper(apparel))
                {
                    return true;
                }
            }
            return false;
        }

        public static float NeedsDiaperBreakpoint { get => LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().needDiapers; }
        public static float NeedsDiaperNightBreakpoint { get => LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().needPullUp; }

        public static bool needsDiaper(Pawn pawn)
        {
            if (pawn.Awake()) return getBladderControlLevel(pawn) <= NeedsDiaperBreakpoint;

            var bladderControlWorker = new PawnCapacityWorker_BladderControl();
            return bladderControlWorker.SimulateBladderControlAwake(pawn) <= NeedsDiaperBreakpoint;
        }
        public static bool acceptsDiaper(Pawn pawn)
        {
            var pref = getDiaperPreference(pawn);
            return pref == DiaperLikeCategory.NonAdult || pref == DiaperLikeCategory.Liked || (pref != DiaperLikeCategory.Disliked && needsDiaper(pawn));
        }
        public static bool needsDiaperNight(Pawn pawn)
        {
            if (!pawn.Awake()) return getBladderControlLevel(pawn) <= NeedsDiaperNightBreakpoint;

            var bladderControlWorker = new PawnCapacityWorker_BladderControl();
            return bladderControlWorker.SimulateBladderControlDuringSleep(pawn) <= NeedsDiaperNightBreakpoint;
        }
        public static bool acceptsDiaperNight(Pawn pawn)
        {
            var pref = getDiaperPreference(pawn);
            return pref == DiaperLikeCategory.NonAdult || pref == DiaperLikeCategory.Liked || (pref != DiaperLikeCategory.Disliked && needsDiaperNight(pawn));
        }
        public static Apparel getUnderwearOrDiaper(Pawn pawn)
        {
            List<Apparel> wornApparel = pawn?.apparel?.WornApparel;
            if (wornApparel == null) return null;


            for (int i = 0; i < wornApparel.Count; i++)
            {
                Apparel apparel = wornApparel[i];

                if (isDiaper(apparel)) return apparel;
                if (isUnderwear(apparel)) return apparel;
            }
            return null;
        }
        public static Apparel getUnderwear(Pawn pawn)
        {
            List<Apparel> wornApparel = pawn?.apparel?.WornApparel;
            if (wornApparel == null) return null;


            for (int i = 0; i < wornApparel.Count; i++)
            {
                Apparel apparel = wornApparel[i];

                if (isUnderwear(apparel)) return apparel;
            }
            return null;
        }
        public static Apparel getDiaper(Pawn pawn)
        {
            List<Apparel> wornApparel = pawn?.apparel?.WornApparel;
            if (wornApparel == null) return null;


            for (int i = 0; i < wornApparel.Count; i++)
            {
                Apparel apparel = wornApparel[i];

                if (isDiaper(apparel)) return apparel;
            }
            return null;
        }
        public static bool isDiaper(Apparel cloth)
        {
            List<ThingCategoryDef> cat = cloth.def.thingCategories;
            if (cat == null) return false;

            return cat.Contains(ThingCategoryDefOf.Diapers);
        }
        public static bool isUnderwear(Apparel cloth)
        {
            List<ThingCategoryDef> cat = cloth.def.thingCategories;
            if (cat == null) return false;

            return cat.Contains(ThingCategoryDefOf.Underwear);
        }
        public static bool isNightDiaper(Apparel cloth)
        {
            List<ThingCategoryDef> cat = cloth.def.thingCategories;
            if (cat == null)
            {
                return false;
            }

            return cat.Contains(ThingCategoryDefOf.DiapersNight);
        }
        public static BodyPartRecord getBladder(Pawn pawn)
        {
            foreach (BodyPartRecord notMissingPart in pawn.health.hediffSet.GetNotMissingParts())
            {
                if (notMissingPart.def.tags.Contains(BodyPartTagDefOf.BladderControlSource))
                {
                    return notMissingPart;
                }
            }

            return null;
        }
        public static float fluidAmount(Pawn pawn, float units)
        {
            // Higher rate than 1 means that the bladder is smaller, so one unit is actually less
            float rateMultiplier = pawn.GetStatValue(DubDef.BladderRateMultiplier);
            return units / rateMultiplier;
        }
        public static float getBladderControlLevel(Pawn pawn)
        {
            return pawn.health.capacities.GetLevel(PawnCapacityDefOf.BladderControl);
        }
        public static bool getBladderControlLevelCapable(Pawn pawn)
        {
            return pawn.health.capacities.CapableOf(PawnCapacityDefOf.BladderControl);
        }

        public static float getBladderControlFailPoint(Pawn pawn)
        {
            var bladderControl = getBladderControlLevel(pawn);

            // Define the input and output ranges
            float x0 = 1.0f; // If full bladder control
            float y0 = 0.0f; // The failure will happen at 0.0 
            float x1 = 0.2f; // If 20% bladder control
            float y1 = 0.4f; // the failure will happen at 0.4 (60% filled bladder)

            // If the input is less than or equal to the lower bound, return the lower output bound
            if (bladderControl >= x0)
            {
                return y0;
            }

            // If the input is greater than or equal to the upper bound, return the upper output bound
            if (bladderControl <= x1)
            {
                return y1;
            }

            // Perform linear interpolation
            return y0 + (bladderControl - x0) * (y1 - y0) / (x1 - x0);
        }


        public static DiaperLikeCategory getDiaperPreference(Pawn pawn)
        {
            if (!pawn.ageTracker.Adult)
            {
                return DiaperLikeCategory.NonAdult;
            }
            TraitSet traits = pawn?.story?.traits;
            if (traits == null)
            {
                return DiaperLikeCategory.Neutral;
            }

            if (pawn.story.traits.HasTrait(TraitDefOf.Potty_Rebel))
            {
                return DiaperLikeCategory.Liked;
            }
            if (pawn.story.traits.HasTrait(TraitDefOf.Big_Boy))
            {
                return DiaperLikeCategory.Disliked;
            }

            if (ModsConfig.IdeologyActive)
            {
                Ideo ideo = pawn.Ideo;
                if (ideo != null)
                {

                    if (ideo.HasPrecept(PreceptDefOf.Diapers_Loved))
                    {
                        return DiaperLikeCategory.Liked;
                    }
                    else if (ideo.HasPrecept(PreceptDefOf.Diapers_Hated))
                    {
                        return DiaperLikeCategory.Disliked;
                    }
                }
            }
            return DiaperLikeCategory.Neutral;
        }
    }
}

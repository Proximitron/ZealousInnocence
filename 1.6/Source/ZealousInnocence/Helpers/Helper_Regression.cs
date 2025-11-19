using DubsBadHygiene;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ZealousInnocence
{
    public static class Helper_Regression
    {
        private static Dictionary<Pawn, AgeStageInfo> cachedMentalAgeStages = new Dictionary<Pawn, AgeStageInfo>();
        public static float getAgeStageMental(this Pawn pawn, bool force = false)
        {
            if (!cachedMentalAgeStages.TryGetValue(pawn, out var value) || (value.lastCheckTick + 60) < Find.TickManager.TicksGame || force)
            {
                refreshAgeStageMentalCache(pawn);
                cachedMentalAgeStages.TryGetValue(pawn, out value);
            }

            return value.cachedAgeStage;
        }
        public static int getAgeStageMentalInt(this Pawn pawn, bool force = false)
        {
            return Mathf.FloorToInt(getAgeStageMental(pawn,force));
        }
        public static int getAgeStagePhysicalInt(this Pawn pawn, bool force = false)
        {
            return Mathf.FloorToInt(getAgeStagePhysical(pawn, force));
        }
        private static void refreshAgeStageMentalCache(Pawn pawn)
        {
            Dictionary<Pawn, AgeStageInfo> dictionary = cachedMentalAgeStages;
            AgeStageInfo obj = new AgeStageInfo
            {
                cachedAgeStage = fetchAgeStageMental(pawn),
                lastCheckTick = Find.TickManager.TicksGame
            };
            dictionary[pawn] = obj;
        }

        private static float fetchAgeStageMental(Pawn pawn)
        {
            if (pawn == null)
            {
                return 14f;
            }

            foreach (var curr in pawn.health.hediffSet.hediffs)
            {
                if (curr.def == HediffDefOf.RegressionState)
                {
                    // RegressionState isn't a thing on children
                    if (pawn.ageTracker.AgeBiologicalYears < 13)
                    {
                        pawn.health.RemoveHediff(curr);
                        return pawn.ageTracker.AgeBiologicalYears;
                    }
                    return curr.CurStageIndex * 3;
                }
            }
            var hediff = Hediff_MentalRegression.HediffByPawn(pawn);
            if (hediff != null && hediff.LastMentalYears > 0f) return hediff.LastMentalYears;

            var hediff2 = Hediff_PhysicalRegression.HediffByPawn(pawn);
            if (hediff2 != null) return hediff2.BaseAgeYearFloat;

            return pawn.ageTracker.AgeBiologicalYearsFloat;
        }

        public static void refreshAllAgeStageCaches(this Pawn pawn)
        {
            /*
            float oldValuePhysical = -1;
            if(cachedMentalAgeStages.TryGetValue(pawn, out var value)) oldValuePhysical = value.cachedAgeStage;
            
            float oldValueMental = -1;
            if (cachedMentalAgeStages.TryGetValue(pawn, out var value2)) oldValueMental = value2.cachedAgeStage;
            */
            refreshAgeStagePhysicalCache(pawn);
            refreshAgeStageMentalCache(pawn);

            //Log.Message($"[ZI]Resetting ageStageCaches Mental {oldValueMental:F2} => {getAgeStageMental(pawn, false):F2} Physical {oldValuePhysical:F2} => {getAgeStagePhysical(pawn, false):F2} for {pawn.Name}");
        }
        public static float getAgeStagePhysicalMentalMin(this Pawn pawn, bool force = false)
        {
            return Mathf.Min(getAgeStagePhysical(pawn, force), getAgeStageMental(pawn, force));
        }

        private static Dictionary<Pawn, AgeStageInfo> cachedPhysicalAgeStages = new Dictionary<Pawn, AgeStageInfo>();
        public static float getAgeStagePhysical(this Pawn pawn, bool force = false)
        {
            if (!cachedPhysicalAgeStages.TryGetValue(pawn, out var value) || (value.lastCheckTick + 60) < Find.TickManager.TicksGame || force)
            {
                refreshAgeStagePhysicalCache(pawn);
                cachedPhysicalAgeStages.TryGetValue(pawn, out value);
            }

            return value.cachedAgeStage;
        }

        private static void refreshAgeStagePhysicalCache(Pawn pawn)
        {
            Dictionary<Pawn, AgeStageInfo> dictionary = cachedPhysicalAgeStages;
            AgeStageInfo obj = new AgeStageInfo
            {
                cachedAgeStage = fetchAgeStagePhysical(pawn),
                lastCheckTick = Find.TickManager.TicksGame
            };
            dictionary[pawn] = obj;
        }

        private static float fetchAgeStagePhysical(Pawn pawn)
        {
            if (pawn == null)
            {
                return 14f;
            }

            return pawn.ageTracker.AgeBiologicalYearsFloat;
        }
        public static bool ShouldHaveLearning(this Pawn p)
        {
            if (p?.health == null) return false;
            if (!p.RaceProps.Humanlike) return false;
            if (ShouldHavePlaying(p)) return false;
            return isChildMental(p);
        }
        public static bool ShouldHavePlaying(this Pawn p)
        {
            if (p?.health == null) return false;
            if (!p.RaceProps.Humanlike) return false;
            return isToddlerOrBabyMentalOrPhysical(p);
        }
        public static bool CanReachAdultToilet(this Pawn p)
        {
            return !p.isToddlerPhysical() && !p.isBabyPhysical();
        }
        public static bool UnderstandsToilets(this Pawn p)
        {
            return !p.isToddlerMental() && !p.isBabyMental();
        }
        public static int getAgeBehaviour(this Pawn pawn)
        {
            if(pawn.isBabyPhysical()) return 0;
            if (pawn.isToddlerPhysical()) return 4;
            if (pawn.isToddlerMental()) return 4;
            int minAge = Math.Min(pawn.getAgeStageMentalInt(),pawn.getAgeStagePhysicalInt());
            if (minAge < 6 && !pawn.isAdultAtAge(minAge)) return 4;

            if (pawn.isChildPhysical() && minAge < pawn.adultMinAge())
            {
                return Math.Min(pawn.getAgeStageMentalInt(), Mathf.FloorToInt(pawn.adultMinAge()) - 1);
            }

            return pawn.getAgeStageMentalInt();           
        }
        public static AgeStage GetAgeSocial(this Pawn pawn)
        {
            AgeStage s = AgeStage.None;

            if (pawn.getAgeSocialIsBaby())
                s |= AgeStage.Baby;

            if (pawn.getAgeSocialIsToddler())
                s |= AgeStage.Toddler;

            if (pawn.getAgeSocialIsChild())
                s |= AgeStage.Child;

            if (pawn.isTeen())
                s |= AgeStage.Teen;

            if (pawn.getAgeSocialIsAdult())
                s |= AgeStage.Adult;

            if (pawn.isOld())
                s |= AgeStage.Old;

            return s;
        }
        public static bool getAgeSocialIsBaby(this Pawn pawn)
        {
            return pawn.getAgeBehaviour() == 0;
        }
        public static bool getAgeSocialIsToddler(this Pawn pawn)
        {
            return pawn.getAgeBehaviour() == 4;
        }
        public static bool getAgeSocialIsChild(this Pawn pawn)
        {
            return pawn.getAgeBehaviour() > 4 && !pawn.getAgeSocialIsAdult();
        }
        public static bool getAgeSocialIsAdult(this Pawn pawn)
        {
            return pawn.isAdultAtAge(pawn.getAgeBehaviour());
        }
        public static bool canChangeDiaperOrUnderwear(this Pawn p)
        {
            if (p == null || p.Dead) return false;
            if (!p.RaceProps.Humanlike) return false;

            if (p.Downed) return false;

            var caps = p.health?.capacities;
            if (caps == null) return false;

            // Match common work thresholds: must be at least minimally conscious and able to move & manipulate.
            if (caps.GetLevel(RimWorld.PawnCapacityDefOf.Consciousness) < 0.3f) return false;
            if (caps.GetLevel(RimWorld.PawnCapacityDefOf.Moving) <= 0.25f) return false;
            if (caps.GetLevel(RimWorld.PawnCapacityDefOf.Manipulation) <= 0.25f) return false;

            if (p.InMentalState && p.MentalStateDef.blockNormalThoughts) return false;

            return !isToddlerOrBabyMentalOrPhysical(p);
        }

        public static RegressionComputeInfo ComputeTotalRegression(this Pawn pawn, bool mental)
        {
            float sum = 0f;
            float mult = 1f;
            float highestPostCap = -1f;

            foreach (var h in pawn.health.hediffSet.hediffs.OfType<HediffWithComps>())
            {
                var c = h.TryGetComp<HediffComp_RegressionInfluence>();
                if (c == null) continue;
                if (!c.IsInfluence(mental)) continue;

                sum += c.GetExternalCappedContribution(mental);
                mult *= c.GetMultiplierFactor(mental);

                float cap = c.GetCap(mental);
                if (cap > 0f) highestPostCap = Math.Max(highestPostCap, cap);
            }
            float sizeScaleMulti = SeveritySizeScale(pawn);
            mult *= sizeScaleMulti;
            float preCapTotal = sum * mult;
            float total = preCapTotal;
            if (highestPostCap >= 0f)
                total = Math.Min(total, highestPostCap);

            return new RegressionComputeInfo
            {
                sumContrib = sum,
                productMult = mult,
                sizeMult = sizeScaleMulti,
                preCapTotal = preCapTotal,
                postEffectCap = highestPostCap,
                finalTotal = Math.Max(0f, total)
            };
        }
        public struct RegressionComputeInfo
        {
            public float sumContrib;        // Σ of clamped contributions
            public float productMult;       // product of all multipliers
            public float sizeMult;          // Only the multiplier of size as reference
            public float preCapTotal;       // sum*mult before applying caps
            public float postEffectCap;     // highest effect cap (−1 = none)
            public float finalTotal;        // final value after caps
        }

        public static AcceptanceReport canUsePottyReport(Pawn pawn)
        {
            if (pawn.needs == null) Log.WarningOnce($"[ZI] {pawn} has null needs tracker.", 0x1A7E001);
            if (pawn.story == null) Log.WarningOnce($"[ZI] {pawn} has null story.", 0x1A7E002);
            if (pawn.story?.traits == null) Log.WarningOnce($"[ZI] {pawn} has null traits.", 0x1A7E003);
            if (TraitDefOf.Potty_Rebel == null) Log.WarningOnce("[ZI] TraitDefOf.Potty_Rebel is null (DefOf init?).", 0x1A7E004);

            var diaperNeed = pawn.needs.TryGetNeed<Need_Diaper>();
            if (diaperNeed == null) return true; // anything without that gets a free pass

            if (!pawn.UnderstandsToilets())
            {
                return "ShortReasonNotUnderstand".Translate();
            }
            if (!pawn.CanReachAdultToilet())
            {
                return "ShortReasonIsTooSmall".Translate();
            }

            if (pawn.story.traits.HasTrait(TraitDefOf.Potty_Rebel))
            {
                return "ShortReasonNotComply".Translate(); // will never run to a potty!
            }

            // Mental states can make them unable to remember that
            MentalState mentalState = pawn.MentalState;
            if (mentalState != null && mentalState.def != null && (mentalState.def == RimWorld.MentalStateDefOf.PanicFlee || mentalState.def == RimWorld.MentalStateDefOf.Wander_Psychotic))
            {
                return "ShortReasonWrongMentalState".Translate(); // wrong mental state
            }

            var currDiapie = Helper_Diaper.getDiaper(pawn);
            if (currDiapie != null)
            {
                if (pawn.outfits?.forcedHandler != null && pawn.outfits.forcedHandler.IsForced(currDiapie))
                { 
                    return "ShortReasonForcedInDiaper".Translate(); // forced in diapers
                }
                if (!canChangeDiaperOrUnderwear(pawn))
                {
                    return "ShortReasonCantChangeDiaperOrUnderwearSelf".Translate();
                }
            }
            return true;
        }
        public static bool isAdultInEveryWay(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return false;
            return !pawn.DevelopmentalStage.Juvenile() && pawn.isAdultMental() && pawn.isAdultPhysical();
        }
        public static bool isAdultMental(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return true;
            return isAdultAtAge(pawn, getAgeStageMental(pawn, forceRecheck));
        }
        public static bool isChildMental(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return false;
            return isChildAtAge(pawn, getAgeStageMental(pawn, forceRecheck));
        }
        public static bool isToddlerMental(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return false;
            return isToddlerAtAge(pawn, getAgeStageMental(pawn, forceRecheck));
        }
        public static bool isBabyMental(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return false;
            return isBabyAtAge(pawn, getAgeStageMental(pawn, forceRecheck));
        }

        public static bool isAdultPhysical(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return false;
            return isAdultAtAge(pawn, getAgeStagePhysical(pawn, forceRecheck));
        }
        public static bool isChildPhysical(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return false;
            return isChildAtAge(pawn, getAgeStagePhysical(pawn, forceRecheck));
        }
        public static bool isToddlerPhysical(this Pawn pawn, bool forceRecheck = false)
        {
            return isToddlerAtAge(pawn, getAgeStagePhysical(pawn, forceRecheck));
        }
        public static bool isBabyPhysical(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return false;
            return isBabyAtAge(pawn, getAgeStagePhysical(pawn, forceRecheck));
        }
        public static bool isOld(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return false;
            return isOldAtAge(pawn, getAgeStagePhysical(pawn, forceRecheck));
        }
        public static bool isTeen(this Pawn pawn, bool forceRecheck = false)
        {
            if (!pawn.IsColonist) return false;
            return isTeenAtAge(pawn, getAgeStagePhysical(pawn, forceRecheck));
        }

        public static bool isAdultMentalOrPhysical(this Pawn pawn, bool forceRecheck = false)
        {
            return isAdultMental(pawn, forceRecheck) || isAdultPhysical(pawn, forceRecheck);
        }
        public static bool isChildMentalOrPhysical(this Pawn pawn, bool forceRecheck = false)
        {
            return isChildMental(pawn, forceRecheck) || isChildPhysical(pawn, forceRecheck);
        }
        public static bool isToddlerMentalOrPhysical(this Pawn pawn, bool forceRecheck = false)
        {
            return isToddlerMental(pawn, forceRecheck) || isToddlerPhysical(pawn, forceRecheck);
        }
        public static bool isBabyMentalOrPhysical(this Pawn pawn, bool forceRecheck = false)
        {
            return isBabyMental(pawn, forceRecheck) || isBabyPhysical(pawn, forceRecheck);
        }

        public static bool isToddlerOrBabyMentalOrPhysical(this Pawn pawn, bool forceRecheck = false)
        {
            return isToddlerMentalOrPhysical(pawn, forceRecheck) || isBabyMentalOrPhysical(pawn, forceRecheck);
        }

        public static bool isBabyAtAge(this Pawn pawn, float ageYears)
        {
            return ageYears < toddlerMinAge(pawn);
        }
        public static float toddlerMinAge(this Pawn pawn)
        {
            
            return Hediff_RegressionBase.ChildBands.Get(pawn).toddler / GenDate.TicksPerYear;
        }
        public static float childMinAge(this Pawn pawn)
        {
            return Hediff_RegressionBase.ChildBands.Get(pawn).core / GenDate.TicksPerYear;
        }
        public static float adultMinAge(this Pawn pawn)
        {
            return Hediff_RegressionBase.ChildBands.Get(pawn).edge / GenDate.TicksPerYear;
        }
        public static float teenMaxAge(this Pawn pawn)
        {
            float adultStart = pawn.adultMinAge();
            float lifeExp = pawn.RaceProps.lifeExpectancy;

            // A human (~100y) has adultStart ≈14; teen window = 14–17 → 3y ≈3% of life
            float teenSpan = lifeExp * 0.03f;      // ~3% of lifespan as “teen”
            float teenEnd = adultStart + teenSpan;

            return Mathf.FloorToInt(teenEnd);
        }
        public static float oldMinAge(this Pawn pawn)
        {
            return Mathf.FloorToInt(pawn.RaceProps.lifeExpectancy * 0.65f);
        }


        public static bool isToddlerAtAge(this Pawn pawn, float ageYears)
        {
            return ageYears >= toddlerMinAge(pawn) && ageYears < childMinAge(pawn);
        }
        public static bool isChildAtAge(this Pawn pawn, float ageYears)
        {
            return ageYears >= childMinAge(pawn) && ageYears < adultMinAge(pawn);
        }
        public static bool isAdultAtAge(this Pawn pawn, float ageYears)
        {
            return ageYears >= adultMinAge(pawn);
        }
        public static bool isTeenAtAge(this Pawn pawn, float ageYears)
        {
            return ageYears <= teenMaxAge(pawn) && isAdultAtAge(pawn, ageYears) && !isOldAtAge(pawn,ageYears);
        }
        public static bool isOldAtAge(this Pawn pawn, float ageYears)
        {
            return ageYears >= oldMinAge(pawn) && isAdultAtAge(pawn,ageYears);
        }

        public static BodyPartRecord GetFirstBrainRecord(Pawn pawn)
        {
            if (pawn?.RaceProps?.body == null) return null;

            // Try literal "Brain" part first
            var brain = pawn.health.hediffSet.GetBrain();
            if (brain != null) return brain;

            // Fallback: look for anything tagged as ConsciousnessSource
            var consciousnessParts = pawn.RaceProps.body.AllParts
                .Where(p => p.def.tags != null &&
                            p.def.tags.Contains(RimWorld.BodyPartTagDefOf.ConsciousnessSource));

            foreach (var part in consciousnessParts)
                return part;

            // As a final fallback, look for a part explicitly named "Brain"
            var brainByName = pawn.RaceProps.body.AllParts
                .FirstOrDefault(p => p.def.defName.ToLowerInvariant().Contains("brain"));
            if (brainByName != null) return brainByName;

            return null;
        }
        private static bool IsBrainRelated(BodyPartRecord part)
        {
            if (part == null) return false;

            // anything tagged as affecting consciousness (brain, modded neural cores, etc.)
            if (part.def.tags != null &&
                part.def.tags.Contains(RimWorld.BodyPartTagDefOf.ConsciousnessSource)) return true;

            // anything *inside* the head (walk ancestors)
            for (var p = part.parent; p != null; p = p.parent)
                if (p.def == RimWorld.BodyPartDefOf.Head) return true;

            return false;
        }
        public static void healPawnBrain(Pawn pawn)
        {
            var hediffs = pawn.health.hediffSet.hediffs;
            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                var h = hediffs[i];
                if (!IsBrainRelated(h.Part)) continue;

                // keep regression system bits
                if (h.TryGetComp<HediffComp_RegressionInfluence>() != null) continue;
                if (h is Hediff_RegressionBase) continue;

                // always preserve implants/added parts/missing parts
                if (h.def.countsAsAddedPartOrImplant) continue;

                // if (!(h is Hediff_Injury)) continue;

                pawn.health.RemoveHediff(h);
            }
            pawn.health.hediffSet.DirtyCache();

            /*Hediff hediff = HediffMaker.MakeHediff(RimWorld.HediffDefOf.ResurrectionSickness, innerPawn);
            if (!innerPawn.health.WouldDieAfterAddingHediff(hediff))
            {
                innerPawn.health.AddHediff(hediff);
            }*/

        }
        public static List<Hediff_Injury> PermanentInjuries(this Pawn pawn)
        {
            var result = new List<Hediff_Injury>();

            if (pawn?.health?.hediffSet == null)
                return result;

            // Copy list to avoid enumeration issues
            var hediffs = pawn.health.hediffSet.hediffs.ToList();

            foreach (var h in hediffs)
            {
                if (h is Hediff_Injury inj)
                {
                    var perm = inj.TryGetComp<HediffComp_GetsPermanent>();
                    if (perm != null && perm.IsPermanent)
                    {
                        result.Add(inj);
                    }
                }
            }

            return result;
        }
        public static List<Hediff>MissingBodyParts(this Pawn pawn)
        {
            var resultList = new List<Hediff>();
            if (pawn?.health?.hediffSet != null)
            {
                var hediffs = pawn.health.hediffSet.hediffs.ToList();
                foreach (var h in hediffs)
                {
                    if (h is Hediff_MissingPart miss && miss.Part != null)
                    {
                        resultList.Add(h);
                    }
                }
            }
            return resultList;
        }
        private static void healPawnMissingBodyparts(Pawn pawn, bool healAddictions = false)
        {
            // Restore missing natural parts (no implant replacement)
            foreach (var h in MissingBodyParts(pawn))
            {
                if (h is Hediff_MissingPart miss && miss.Part != null)
                {
                    IntVec3 pos = pawn.Spawned ? pawn.Position : IntVec3.Invalid;
                    Map map = pawn.Spawned ? pawn.Map : null;
                    MedicalRecipesUtility.RestorePartAndSpawnAllPreviousParts(pawn, miss.Part, pos, map);
                }
            }

            pawn.health.hediffSet.DirtyCache();
        }

        public static bool regressOrReincarnateToChild(Pawn pawn,ThingDef cause,  bool targetMentalRegression, out List<Hediff> removedHediffs, out float pawnAgeDelta)
        {
            removedHediffs = new List<Hediff>();
            pawnAgeDelta = 0f;
            if (LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().reduceAge && !targetMentalRegression)
            {
                return reincarnateToChildPawn(pawn, cause, out removedHediffs, out pawnAgeDelta);
            }
            else
            {
                return mentalRegressPawn(pawn);
            }
        }
        public static bool mentalRegressPawn(Pawn pawn)
        {
            healPawnBrain(pawn);
            SetRegressionHediff(pawn, pawn.def, HediffDefOf.MentalRegressionDamage, 1.3f);

            healPawnMissingBodyparts(pawn, true);
            refreshAgeStageMentalCache(pawn);
            Messages.Message("MessagePawnRegressed".Translate(pawn), pawn, MessageTypeDefOf.CautionInput);
            return true;
        }

        public static void dropAllUnwearable(Pawn pawn)
        {
            if (pawn?.apparel == null) return;

            // Snapshot list since Drop() mutates the collection.
            var worn = pawn.apparel.WornApparel.ListFullCopy();
            IntVec3 pos = pawn.PositionHeld;

            foreach (var apparel in worn)
            {
                if (!apparel.PawnCanWear(pawn, true) || !Helper_Diaper.allowedByPolicy(pawn, apparel))
                {
                    // Drop it on the ground but keep all other apparel.
                    pawn.apparel.TryDrop(apparel, out Apparel dropped, pos, forbid: false);
                }
            }
        }
        public static bool InCrib(this Pawn p)
        {
            if (!(p.ParentHolder is Map) || p.pather.Moving) return false;

            if (GetCurrentCrib(p) == null) return false;
            else return true;
        }
        public static Building_Bed GetCurrentCrib(this Pawn p)
        {
            if (!p.Spawned) return null;
            Building_Bed bed = null;
            List<Thing> thingList = p.Position.GetThingList(p.Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                bed = thingList[i] as Building_Bed;
                if (bed != null && IsCrib(bed)) return bed;
            }
            return null;
        }
        public static bool IsCrib(Building_Bed bed)
        {
            if (bed == null) return false;
            ThingDef bedDef = bed.def;
            if (bedDef.defName.Contains("Crib") || bedDef.defName.Contains("crib")
                || bedDef.label.Contains("Crib") || bedDef.label.Contains("crib")
                || bedDef.defName.Contains("Cradle") || bedDef.defName.Contains("cradle")
                || bedDef.label.Contains("Cradle") || bedDef.label.Contains("cradle")
                )
            {
                return true;
            }
            return false;
        }
        public static string FormatDays(float days)
        {
            if (float.IsInfinity(days))
                return "∞";

            if (days < 0f)
                days = 0f;

            // 1 year = 60 days in RimWorld (default), change to 365 if you prefer realism
            const float DaysPerYear = 60f;

            // YEARS
            if (days >= DaysPerYear)
            {
                float years = days / DaysPerYear;
                return $"~{years:0.#} y";
            }

            // DAYS
            if (days >= 1f)
            {
                return $"{days:0.#} d";
            }

            // HOURS
            float hours = days * 24f;
            return $"{hours:0.#} h";
        }
        public static bool TrySetHediffSeverity(Pawn pawn, string hediffDefName, float severity)
        {
            if (pawn?.health?.hediffSet == null) return false;

            // Find the def by name
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            if (def == null) return false;

            // Find an existing instance of that hediff
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (existing == null) return false;

            existing.Severity = severity;
            return true;
        }

        public static bool IsSurfaceToPlacePawn(Thing thing)
        {
            if (thing?.def?.building == null) return false;

            var props = thing.def.building;

            return props.isSittable || thing is Building_Bed; // catches counters, benches, etc.
        }
        public static Thing MoveSingleFromInventoryToHands(this Pawn pawn, Thing itemOrStack)
        {
            if (pawn?.carryTracker == null || pawn?.inventory?.innerContainer == null || itemOrStack == null)
                return null;

            Thing toCarry = itemOrStack;

            if (itemOrStack.stackCount > 1)
            {
                // Split off the exact amount; the split has no owner container
                toCarry = itemOrStack.SplitOff(1);
            }
            else
            {
                // Remove the single stack from its current ThingOwner (inventory)
                itemOrStack.holdingOwner?.Remove(itemOrStack);
            }

            if (!pawn.carryTracker.TryStartCarry(toCarry))
            {
                // If carrying failed, try to return it to inventory (best-effort)
                if (toCarry.holdingOwner == null)
                {
                    pawn.inventory.innerContainer.TryAdd(toCarry, canMergeWithExistingStacks: true);
                }
                return null;
            }
            return toCarry;
        }

        public static bool reincarnateToChildPawn(Pawn pawn, ThingDef cause, out List<Hediff> removedHediffs, out float pawnAgeDelta)
        {
            Helper_Regression.SetRegressionHediff(pawn, pawn.def, HediffDefOf.RegressionDamage_FoyWater_Ingested, 1.1f);
            removedHediffs = new List<Hediff>();
            pawnAgeDelta = 0;
            return true;
        }

        public static bool CanPolitelyInterrupt(this Pawn pawn)
        {
            if (pawn == null) return false;
            var curJob = pawn?.CurJob;
            if (curJob == null) return true;
            if (pawn.Downed || pawn.InMentalState) return false;
            if (pawn.CurJob?.playerForced == true) return false;

            // If job def says it’s NOT interruptible for the player, treat it as essential
            if (curJob.def?.playerInterruptible == false) return false;
            if (curJob.locomotionUrgency == LocomotionUrgency.Sprint) return false;
            // Avoid interrupting critical things
            if (IsEssentialJob(curJob)) return false;
            if (ForbidsLongNeeds(pawn)) return false;

            return true;
        }
        private static bool ForbidsLongNeeds(Pawn pawn)
        {
            var duty = pawn?.mindState?.duty;
            if (duty == null) return false;

            // Try common names across versions
            var t = duty.GetType();
            var f = t.GetField("allowSatisfyLongNeeds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f?.FieldType == typeof(bool)) return !(bool)f.GetValue(duty);
            var p = t.GetProperty("allowSatisfyLongNeeds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p?.PropertyType == typeof(bool)) return !(bool)p.GetValue(duty);

            return false; // unknown => don’t block
        }

        public static bool IsEssentialJob(this Job job)
        {
            if (job?.def == null) return false;
            var def = job.def;

            // conservative deny-list: combat, emergency, tending, rescuing, hauling a downed pawn, arrest, manhunter-like, fleeing, ingest while starving, etc.
            if (def == RimWorld.JobDefOf.AttackMelee || def == RimWorld.JobDefOf.AttackStatic || def == RimWorld.JobDefOf.Wait_Combat || def == RimWorld.JobDefOf.Hunt)
                return true;
            if (def == RimWorld.JobDefOf.Flee || def == RimWorld.JobDefOf.TakeWoundedPrisonerToBed || def == RimWorld.JobDefOf.Rescue || def == RimWorld.JobDefOf.Capture || def == RimWorld.JobDefOf.Arrest)
                return true;
            if (def == RimWorld.JobDefOf.TendPatient || def == RimWorld.JobDefOf.TendEntity || def == RimWorld.JobDefOf.BeatFire || def == RimWorld.JobDefOf.ExtinguishSelf)
                return true;

            return false;
        }
        public static bool CanBeRegressed(Pawn pawn)
        {
            if (pawn?.RaceProps?.IsFlesh == true) return true;
            return false;
        }
        /*public static DamageInfo ReduceDamage(DamageInfo dInfo, Pawn pawn) //we want to reduce the damage so weapons look more damage then they actually are 
        {
            float reduceAmount = 1f;
            var extensionInfo = dInfo.Def.GetModExtension<RegressionDamageExtension>();
            if (extensionInfo != null)
            {
                reduceAmount = extensionInfo.reduceValue;
            }

            float reducedDamage = dInfo.Amount - (dInfo.Amount * reduceAmount);
            reducedDamage *= Worker_RegressionDamage.SeveritySizeScale(pawn);
            dInfo = new DamageInfo(dInfo.Def, reducedDamage, dInfo.ArmorPenetrationInt, dInfo.Angle, dInfo.Instigator,
                                   dInfo.HitPart, dInfo.Weapon, dInfo.Category, dInfo.intendedTargetInt);
            return dInfo;
        }*/
        private static readonly SimpleCurve SpeciesSizeToSeverityMult = new SimpleCurve
        {
            new CurvePoint(0.25f, 1.35f), // tiny: +35%
            new CurvePoint(1.00f, 1.00f), // human baseline
            new CurvePoint(2.00f, 0.75f), // big: -25%
            new CurvePoint(4.00f, 0.55f), // very big: -45%
        };
        public static float SeveritySizeScale(Pawn p)
        {
            if (p?.RaceProps == null) return 1f;

            float speciesBase = Mathf.Max(p.RaceProps.baseBodySize, 0.1f);

            // Evaluate raw multiplier
            float mult = SpeciesSizeToSeverityMult.Evaluate(speciesBase);

            // Normalize so that humans (baseBodySize≈1) are exactly 1×
            float humanRef = SpeciesSizeToSeverityMult.Evaluate(RimWorld.ThingDefOf.Human.race.baseBodySize);
            mult /= humanRef;

            return Mathf.Clamp(mult, 0.25f, 2f);
        }
        public static void SetRegressionHediff(Pawn pawn, ThingDef source, HediffDef hediffDef, float severityAmount)
        {
            severityAmount = Math.Min(severityAmount, hediffDef.maxSeverity);
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                pawn.health.AddHediff(hediff);
            }
            hediff.Severity = severityAmount;
            hediff.sourceDef = source;
        }
        public static void IncreaseRegressionHediff(Pawn pawn, ThingDef source, HediffDef hediffDef, float severityAmount)
        {

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                pawn.health.AddHediff(hediff);
                hediff.Severity = severityAmount;
            }
            else
            {
                hediff.Severity = Math.Min(hediff.Severity + severityAmount, hediffDef.maxSeverity);
            }
            hediff.sourceDef = source;
        }
        /*public static void ApplyRegressionPoolDamage(DamageInfo dInfo, [NotNull] Pawn pawn, DamageWorker.DamageResult result, float originalDamage)
        {
            var ext = dInfo.Def.GetModExtension<RegressionDamageExtension>();
            if (ext == null)
            {
                Log.Warning("Damage caused by regression weapon does not contain damage extension!");
                return;
            }

            var def = ext.hediffCaused;
            float amount = Mathf.Clamp(originalDamage * ext.magnitudePerDamage, 0f, def.maxSeverity);

            var props = def.CompProps<CompProperties_RegressionInfluence>();
            var hitPart = dInfo.HitPart;
            var stacking = props?.stacking ?? ZIStacking.Refresh;
            string exGroup = props?.exclusiveGroup;

            // decide target part depending on affectsBodyParts
            BodyPartRecord targetPart = (props?.affectsBodyParts == true) ? hitPart : null;

            // Helper: add a new instance
            HediffWithComps AddNew(float sev, BodyPartRecord part)
            {
                var h = (HediffWithComps)HediffMaker.MakeHediff(def, pawn, part);
                ApplyAmount(h, sev, setNotAdd: true);
                h.sourceDef = dInfo.Weapon;
                pawn.health.AddHediff(h, part);
                return h;
            }

            // Helper: write amount into Severity/currentMagnitude
            void ApplyAmount(HediffWithComps h, float sev, bool setNotAdd)
            {
                float cap = h.def.maxSeverity > 0 ? h.def.maxSeverity : float.MaxValue;
                if (setNotAdd) h.Severity = Mathf.Min(cap, sev);
                else h.Severity = Mathf.Min(cap, h.Severity + sev);
            }

            // If EXCLUSIVE: only one hediff in this group may exist (optionally per-part)
            if (stacking == ZIStacking.Exclusive && !string.IsNullOrEmpty(exGroup))
            {
                var groupHediffs = pawn.health.hediffSet.hediffs
                    .OfType<HediffWithComps>()
                    .Where(h =>
                    {
                        var c = h.TryGetComp<HediffComp_RegressionInfluence>();
                        if (c == null || c.Props.exclusiveGroup != exGroup) return false;
                        // if body-part-scoped, only consider same part
                        return (props?.affectsBodyParts == true) ? h.Part == targetPart : true;
                    })
                    .ToList();

                var survivor = groupHediffs.FirstOrDefault(h => h.def == def);
                if (survivor == null)
                {
                    survivor = AddNew(amount, targetPart);
                    foreach (var h in groupHediffs)
                        if (h != survivor) pawn.health.RemoveHediff(h);
                }
                else
                {
                    ApplyAmount(survivor, amount, setNotAdd: true);
                    foreach (var h in groupHediffs)
                        if (h != survivor) pawn.health.RemoveHediff(h);
                }

                Log.Message($"[ZI]original damage:{originalDamage}, reducedDamage:{dInfo.Amount}, severityAdded:{amount}, part:{targetPart?.Label ?? "whole body"} (exclusive).");
            }

            // Not exclusive: check for an existing instance of this exact def (optionally per-part)
            HediffWithComps existing = null;
            if (props?.affectsBodyParts == true)
            {
                existing = pawn.health.hediffSet.hediffs
                    .OfType<HediffWithComps>()
                    .FirstOrDefault(h => h.def == def && h.Part == targetPart);
            }
            else
            {
                existing = pawn.health.hediffSet.GetFirstHediffOfDef(def) as HediffWithComps;
            }

            switch (stacking)
            {
                case ZIStacking.Additive:
                    if (existing == null) AddNew(amount, targetPart);
                    else ApplyAmount(existing, amount, setNotAdd: false);
                    break;

                case ZIStacking.Refresh:
                default:
                    if (existing == null) AddNew(amount, targetPart);
                    else ApplyAmount(existing, amount, setNotAdd: true);
                    break;
            }

            //IncreaseRegressionHediff(pawn, dInfo.Weapon, ext.hediffCaused, amount);

            Log.Message($"[ZI]original damage:{originalDamage}, reducedDamage: {dInfo.Amount}, severityAdded: {amount},");
        }*/
    }

    public class AgeStageInfo
    {
        public float cachedAgeStage;

        public int lastCheckTick;
    }

    public class Recipe_Regression : Recipe_Surgery
    {
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            yield return pawn.health.hediffSet.GetBrain();
        }
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (IsViolationOnPawn(pawn, part, Faction.OfPlayer))
            {
                ReportViolation(pawn, billDoer, pawn.HomeFaction, -30);
            }
            SoundDefOf.MechSerumUsed.PlayOneShot(SoundInfo.InMap(pawn));

            bool targetMentalRegression = false;
            if (this.recipe.HasModExtension<RecipeExtension_RegressionParameter>())
            {
                targetMentalRegression = this.recipe.GetModExtension<RecipeExtension_RegressionParameter>().targetMentalRegression;
            }
            var hediffsRemoved = new List<Hediff>();
            float ageDifference = 0f;
            Helper_Regression.regressOrReincarnateToChild(pawn,billDoer.def, targetMentalRegression,  out hediffsRemoved, out ageDifference);

        }
    }

    public class RecipeExtension_RegressionParameter : DefModExtension
    {
        public bool targetMentalRegression = false;
    }

    [HarmonyPatch(typeof(HealthAIUtility))]
    public static class Patch_HealthAIUtility
    {

        [HarmonyPostfix]
        [HarmonyPatch(nameof(HealthAIUtility.ShouldSeekMedicalRest))]
        public static void ShouldSeekMedicalRest(Pawn pawn, ref bool __result)
        {
            if (__result == false)
            {
                Need_Diaper need_diaper = pawn.needs.TryGetNeed<Need_Diaper>();
                if (need_diaper == null) return;
                if (Helper_Regression.canChangeDiaperOrUnderwear(pawn)) return;

                var diaper = Helper_Diaper.getUnderwearOrDiaper(pawn);
                if (diaper == null || !Helper_Diaper.allowedByPolicy(pawn, diaper) || need_diaper.CurLevel < 0.5f)
                {
                    var cloth = need_diaper.getCachedBestDiaperOrUndie();
                    __result = cloth != null && cloth.IsValid && cloth.HasThing;
                }
            }
        }
    }
    [HarmonyPatch(typeof(JobGiver_OptimizeApparel), "TryGiveJob")]
    public static class JobGiver_OptimizeApparel_TryGiveJob_Patch
    {
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            // Add your condition here to check if the pawn should not dress themselves
            if (!Helper_Regression.canChangeDiaperOrUnderwear(pawn))
            {
                JobFailReason.Is("Can't change their own diaper or underwear.");
                __result = null; // Prevent the job from being given
                return false;    // Skip the original method
            }
            return true; // Continue with the original method
        }
    }
    [HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
    public static class Pawn_JobTracker_StartJob_Patch
    {
        public static bool Prefix(Pawn_JobTracker __instance, Job newJob)
        {
            
            // Check if the job is Wear and if the pawn should not dress themselves
            if (newJob.def == RimWorld.JobDefOf.Wear)
            {
                Pawn pawn = (Pawn)AccessTools.Field(typeof(Pawn_JobTracker), "pawn").GetValue(__instance);
                if (Helper_Regression.canChangeDiaperOrUnderwear(pawn)) return true;
                JobFailReason.Is("Can't change their own diaper or underwear.");

                Messages.Message("This pawn can't change their own diaper or underwear.", pawn, MessageTypeDefOf.RejectInput, historical: false);
                return false; // Prevent the job from being started
            }
            if(newJob.def == DubDef.UseToilet)
            {
                Pawn pawn = (Pawn)AccessTools.Field(typeof(Pawn_JobTracker), "pawn").GetValue(__instance);
                var report = Helper_Regression.canUsePottyReport(pawn);
                if (!report.Accepted)
                {
                    JobFailReason.Is(report.Reason);
                    Messages.Message(report.Reason, pawn, MessageTypeDefOf.RejectInput, historical: false);
                    return false; // can't change their own diaper
                }

            }
            return true; // Continue with the original method
        }
    }

    /*public class Hediff_RegressionDamage : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
             Log.Message($"[ZI]Hit by {dinfo.Value}");
            base.PostAdd(dinfo);
            // Additional logic when this hediff is added, like notifications or initial setup
        }

        public override void Tick()
        {
            base.Tick();
            // Custom logic to be executed every tick if needed
        }

        public override void PostTick()
        {
            base.PostTick();
            // You can add logic here if you want something to happen at each tick.
        }

        public override string TipStringExtra
        {
            get
            {
                return $"Regression Level: {Severity * 100:F1}%";
            }
        }
    }*/
}


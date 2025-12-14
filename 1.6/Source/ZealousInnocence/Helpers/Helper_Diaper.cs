using DubsBadHygiene;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using Verse;
using Verse.AI;
using static HarmonyLib.Code;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

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
#if DEBUG
            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debugGenes = settings.debugging && settings.debuggingGenes;
            if (debugGenes)  Log.Message($"[ZI]Pawn {pawn.LabelShort} searching bladder");
#endif
            BodyPartRecord originalBladder = getBladderControlSourcePart(pawn);
            if (originalBladder != null)
            {
                // Remove the original Bladder part
                pawn.health.hediffSet.hediffs.RemoveAll(hediff => hediff.Part == originalBladder);
#if DEBUG
                if (debugGenes)  Log.Message($"[ZI]Pawn {pawn.LabelShort} removing old hediff");
#endif
                // Check if the new bladder part is already defined in the pawn's body
                BodyPartRecord newBladderPart = pawn.RaceProps.body.AllParts.Find(part => part.def == BodyPartDefOf.Bladder);

                if (newBladderPart != null)
                {
#if DEBUG
                    if (debugGenes)  Log.Message($"[ZI]Restore procedure to {pawn.LabelShort}");
#endif
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
#if DEBUG
            var debugging = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().debugging;
#endif
            if (!pawn.health.capacities.CanBeAwake)
            {
#if DEBUG
                if (debugging) Log.Message("Can't gain Memory for unconcious " + pawn.Name + ", thought " + thought.defName + " at stage " + stage.ToString());
#endif
                return;
            }

            var memories = pawn.needs.mood.thoughts.memories;

            Thought_Memory firstMemoryOfDef = memories.GetFirstMemoryOfDef(thought);
            if (firstMemoryOfDef != null)
            {
                firstMemoryOfDef.SetForcedStage(stage); // Making sure that merge works on TryGainMemory
#if DEBUG
                if (debugging) Log.Message("Forcing state for Memory of " + pawn.Name + ", thought " + thought.defName + " at stage " + stage.ToString());
#endif
            }
#if DEBUG
            if (debugging) Log.Message("Creating Memory for " + pawn.Name + ", thought " + thought.defName + " at stage " + stage.ToString());
#endif
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
                // Range 1.0 to 0.75: 0.5%-3% chance
                return Mathf.Lerp(0.005f, 0.03f, Mathf.InverseLerp(1.2f, 0.8f, bladderControl));
            }
            else if (bladderControl >= 0.5f)
            {
                // Range 0.75 to 0.5: 3%-20% chance
                return Mathf.Lerp(0.03f, 0.20f, Mathf.InverseLerp(0.8f, 0.5f, bladderControl));
            }
            else if (bladderControl >= 0.15f)
            {
                // Range 0.5 to 0.25: 20%-90% chance
                return Mathf.Lerp(0.20f, 0.90f, Mathf.InverseLerp(0.5f, 0.15f, bladderControl));
            }
            else
            {
                // Range lower than 0.25: 100% chance
                return 1.0f;
            }
        }
        public static bool shouldStayPut(Pawn pawn)
        {
            if (pawn == null) return false;
            if (!pawn.IsColonist) return false;
            if (!pawn.Spawned) return false;

            return HealthAIUtility.ShouldSeekMedicalRestUrgent(pawn) || pawn.Downed || (pawn.health?.capacities != null && pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) < 0.3f) || (pawn.CurJob != null &&  pawn.CurJob.playerForced && pawn.InBed()) || (pawn.health?.capacities != null && !pawn.health.capacities.CanBeAwake);
        }
        public static bool accidentOngoing(Pawn pawn)
        {
            var diaperNeed = pawn.needs.TryGetNeed<Need_Diaper>();
            if (diaperNeed == null) return false;
            if(!diaperNeed.IsHavingAccident) return false;
            return true;
        }
        public static bool? accidentTypePee(Pawn pawn)
        {
            if (pawn == null) return null;
            var diaperNeed = pawn.needs.TryGetNeed<Need_Diaper>();
            if (diaperNeed == null) return null;
            return diaperNeed.IsPeeing;
        }

        public static bool remembersPotty(Pawn pawn)
        {
            if (pawn != null && pawn.RaceProps.Humanlike)
            {
                var diaperNeed = pawn.needs.TryGetNeed<Need_Diaper>();
                if (diaperNeed == null) return true; // anything without that gets a free pass
                if (!getBladderControlLevelCapable(pawn)) return false; // incapeable of noticing

                var report = Helper_Regression.canUsePottyReport(pawn);
                if(!report.Accepted)
                {
                    JobFailReason.Is(report.Reason);
                    return false; // can't change their own diaper
                }

                if (diaperNeed.IsHavingAccident) return true; // now we notice for sure

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
                    if (debug)  Log.Message($"[ZI]remembersPotty (JobGiver_UseToilet prefix): job denied, {pawn.Name.ToStringShort} at propability {probability} and seed {diaperNeed.FailureSeed}");
                    return false; // depending on the level on control
                }
                else
                {
                    if (debug)  Log.Message($"[ZI]remembersPotty (JobGiver_UseToilet prefix): job given, {pawn.Name.ToStringShort} at propability {probability} and seed {diaperNeed.FailureSeed}");
                }
            }
            return true;
        }

        public static void triggerDiaperChangeInteractionResult(Pawn initiator, Pawn recipient)
        {
            if(JobDebugEnabled)  Log.Message($"[ZI]triggerDiaperChangeInteractionResult: primary trigger");
            triggerDiaperChangeInteractionResult_Ideology(initiator,recipient);
            triggerDiaperChangeInteractionResult_Enslavement(initiator, recipient);
            triggerDiaperChangeInteractionResult_Recruit(initiator, recipient);
        }

        public static void triggerDiaperChangeInteractionResult_Ideology(Pawn initiator, Pawn recipient)
        {
            
            if (!ModsConfig.IdeologyActive || Find.IdeoManager.classicMode)
            {
                return;
            }
            if (initiator.Ideo == null || !recipient.RaceProps.Humanlike || initiator.Ideo == recipient.Ideo)
            {
                return;
            }
            if (recipient.guest == null) return;
            if (!recipient.guest.IsInteractionEnabled(RimWorld.PrisonerInteractionModeDefOf.Convert)) return;

            if (JobDebugEnabled)  Log.Message($"[ZI]triggerDiaperChangeInteractionResult_Ideology: all checks met for conversion");

            float num = InteractionWorker_ConvertIdeoAttempt.CertaintyReduction(initiator, recipient);
            recipient.ideo.IdeoConversionAttempt(num, initiator.Ideo, true);
        }
        public static void triggerDiaperChangeInteractionResult_Enslavement(Pawn initiator, Pawn recipient)
        {
            if (recipient.guest == null) return;
            if (!recipient.guest.IsInteractionEnabled(RimWorld.PrisonerInteractionModeDefOf.Enslave) && !recipient.guest.IsInteractionEnabled(RimWorld.PrisonerInteractionModeDefOf.ReduceWill)) return;
            if (recipient.guest.will > 0f)
            {
                if (JobDebugEnabled)  Log.Message($"[ZI]triggerDiaperChangeInteractionResult_Enslavement: all checks met for enslavement");
                float num = 1f;
                num *= initiator.GetStatValue(RimWorld.StatDefOf.NegotiationAbility, true, -1);
                num = Mathf.Min(num, recipient.guest.will);
                float will = recipient.guest.will;
                recipient.guest.will = Mathf.Max(0f, recipient.guest.will - num);
                float will2 = recipient.guest.will;
                string text = "TextMote_WillReduced".Translate(will.ToString("F1"), recipient.guest.will.ToString("F1"));
                if (recipient.needs.mood != null && recipient.needs.mood.CurLevelPercentage < 0.4f)
                {
                    text += "\n(" + "lowMood".Translate() + ")";
                }
                MoteMaker.ThrowText((initiator.DrawPos + recipient.DrawPos) / 2f, initiator.Map, text, 8f);
                if (recipient.guest.will == 0f)
                {
                    TaggedString taggedString = "MessagePrisonerWillBroken".Translate(initiator, recipient);
                    if (recipient.guest.IsInteractionEnabled(RimWorld.PrisonerInteractionModeDefOf.AttemptRecruit))
                    {
                        taggedString += " " + "MessagePrisonerWillBroken_RecruitAttempsWillBegin".Translate();
                    }
                    Messages.Message(taggedString, recipient, MessageTypeDefOf.PositiveEvent, true);
                }
            }
        }
        public static void triggerDiaperChangeInteractionResult_Recruit(Pawn initiator, Pawn recipient)
        {
            if (recipient.guest == null) return;
            if (!recipient.guest.IsInteractionEnabled(RimWorld.PrisonerInteractionModeDefOf.AttemptRecruit) && !recipient.guest.IsInteractionEnabled(RimWorld.PrisonerInteractionModeDefOf.ReduceResistance) && !recipient.guest.IsInteractionEnabled(RimWorld.PrisonerInteractionModeDefOf.MaintainOnly)) return;
            if (recipient.AnimalOrWildMan()) return;
            Pawn_RelationsTracker relations = recipient.relations;
            int num = (relations != null) ? relations.OpinionOf(initiator) : 0;
            if (recipient.guest.resistance > 0f)
            {
                if (JobDebugEnabled)  Log.Message($"[ZI]triggerDiaperChangeInteractionResult_Recruit: all checks met for recruitment");
                float num2 = ResistanceImpactFactorCurve_Mood.Evaluate((recipient.needs.mood == null) ? 1f : recipient.needs.mood.CurInstantLevelPercentage);
                float num3 = ResistanceImpactFactorCurve_Opinion.Evaluate((float)num);
                float statValue = initiator.GetStatValue(RimWorld.StatDefOf.NegotiationAbility, true, -1);
                float num4 = 1f;
                num4 *= statValue;
                num4 *= num2;
                num4 *= num3;
                float resistanceReduce = num4;
                num4 = Mathf.Min(num4, recipient.guest.resistance);
                float resistance = recipient.guest.resistance;
                recipient.guest.resistance = Mathf.Max(0f, recipient.guest.resistance - num4);
                float resistance2 = recipient.guest.resistance;
                if (recipient.guest.resistance <= 0f)
                {
                    recipient.guest.SetLastResistanceReduceData(initiator, resistanceReduce, statValue, num2, num3);
                }
                float num5 = (resistance > 0f) ? Mathf.Max(0.1f, resistance) : 0f;
                float num6 = (recipient.guest.resistance > 0f) ? Mathf.Max(0.1f, recipient.guest.resistance) : 0f;
                string text = "TextMote_ResistanceReduced".Translate(num5.ToString("F1"), num6.ToString("F1"));
                if (recipient.needs.mood != null && recipient.needs.mood.CurLevelPercentage < 0.4f)
                {
                    text += "\n(" + "lowMood".Translate() + ")";
                }
                if (recipient.relations != null && (float)recipient.relations.OpinionOf(initiator) < -0.01f)
                {
                    text += "\n(" + "lowOpinion".Translate() + ")";
                }
                MoteMaker.ThrowText((initiator.DrawPos + recipient.DrawPos) / 2f, initiator.Map, text, 8f);
                if (recipient.guest.resistance == 0f)
                {
                    TaggedString taggedString = "MessagePrisonerResistanceBroken".Translate(recipient.LabelShort, initiator.LabelShort, initiator.Named("WARDEN"), recipient.Named("PRISONER"));
                    if (recipient.guest.IsInteractionEnabled(RimWorld.PrisonerInteractionModeDefOf.AttemptRecruit))
                    {
                        taggedString += " " + "MessagePrisonerResistanceBroken_RecruitAttempsWillBegin".Translate();
                    }
                    Messages.Message(taggedString, recipient, MessageTypeDefOf.PositiveEvent, true);
                    return;
                }
            }
        }
        private static readonly SimpleCurve ResistanceImpactFactorCurve_Mood = new SimpleCurve
        {
            {
                new CurvePoint(0f, 0.2f),
                true
            },
            {
                new CurvePoint(0.5f, 1f),
                true
            },
            {
                new CurvePoint(1f, 1.5f),
                true
            }
        };

        // Token: 0x040088B0 RID: 34992
        private static readonly SimpleCurve ResistanceImpactFactorCurve_Opinion = new SimpleCurve
        {
            {
                new CurvePoint(-100f, 0.5f),
                true
            },
            {
                new CurvePoint(0f, 1f),
                true
            },
            {
                new CurvePoint(100f, 1.5f),
                true
            }
        };

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
        public static bool JobDebugEnabled
        {
            get
            {
                var debugging = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
                return debugging.debugging && debugging.debuggingJobs;
            }
        }
        public static bool needsDiaper(Pawn pawn)
        {
            if (shouldStayPut(pawn)) return true;
            if (pawn.Awake()) return getBladderControlLevel(pawn) <= NeedsDiaperBreakpoint;

            var bladderControlWorker = new PawnCapacityWorker_BladderControl();
            return bladderControlWorker.SimulateBladderControlAwake(pawn) <= NeedsDiaperBreakpoint;
        }
        public static bool acceptsDiaper(Pawn pawn)
        {
            var pref = getDiaperPreference(pawn);
            return pref == DiaperLikeCategory.Toddler || pref == DiaperLikeCategory.Child || pref == DiaperLikeCategory.Liked || (pref != DiaperLikeCategory.Disliked && needsDiaper(pawn)) || (pref == DiaperLikeCategory.Diaper_Lover);
        }
        public static bool prefersDiaper(Pawn pawn)
        {
            var pref = getDiaperPreference(pawn);
            return pref == DiaperLikeCategory.Toddler || pref == DiaperLikeCategory.Liked || (needsDiaper(pawn) && pref != DiaperLikeCategory.Disliked) || (pref == DiaperLikeCategory.Diaper_Lover);
        }
        public static bool needsDiaperNight(Pawn pawn)
        {
            if (!pawn.Awake()) return getBladderControlLevel(pawn) <= NeedsDiaperNightBreakpoint; // Shortcut for when the pawn is sleeping. Because now there is no sim nessesary.

            var bladderControlWorker = new PawnCapacityWorker_BladderControl();
            var simResult = bladderControlWorker.SimulateBladderControlDuringSleep(pawn);
            //Log.Message($"[ZI] Sim result for '{pawn.LabelShort}' is '{simResult}");
            return simResult <= NeedsDiaperNightBreakpoint;
        }
        public static bool acceptsDiaperNight(Pawn pawn)
        {
            var pref = getDiaperPreference(pawn);
            return pref == DiaperLikeCategory.Toddler || pref == DiaperLikeCategory.Child || pref == DiaperLikeCategory.Liked || (pref != DiaperLikeCategory.Disliked && needsDiaperNight(pawn)) || (pref == DiaperLikeCategory.Diaper_Lover);
        }
        public static bool canHaveBladder(this Pawn pawn)
        {
            return pawn.RaceProps.IsFlesh || pawn.RaceProps.EatsFood;
        }
        public static bool canWearDiaper(this Pawn pawn)
        {
            return pawn.RaceProps.Humanlike && pawn.canHaveBladder();
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
            List<ThingCategoryDef> cat = cloth?.def?.thingCategories;
            if (cat == null) return false;

            return cat.Contains(ThingCategoryDefOf.Diapers);
        }
        public static bool isUnderwear(Apparel cloth)
        {
            List<ThingCategoryDef> cat = cloth?.def?.thingCategories;
            if (cat == null) return false;

            return cat.Contains(ThingCategoryDefOf.Underwear);
        }
        public static bool isNightDiaper(Apparel cloth)
        {
            List<ThingCategoryDef> cat = cloth?.def?.thingCategories;
            if (cat == null)
            {
                return false;
            }

            return cat.Contains(ThingCategoryDefOf.DiapersNight);
        }
        public static bool isDisposable(Apparel cloth)
        {
            return cloth is Apparel_Disposable_Diaper;
        }
        public static bool needsDiaperChange(this Pawn pawn)
        {
            var oldDiaper = getUnderwearOrDiaper(pawn);
            Need_Diaper need_diaper = pawn.needs.TryGetNeed<Need_Diaper>();
            if (need_diaper == null) return false;
            var needDiaper = needsDiaper(pawn);
            var needDiaperNight = isNightDiaper(oldDiaper);
            if (oldDiaper == null && (needDiaper || needDiaperNight)) return true;
            return need_diaper.CurLevel < 0.5f;
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
        public static bool allowedByPolicy(Pawn pawn, Apparel ap)
        {
            var policy = pawn.IsPrisoner ? null : pawn.outfits?.CurrentApparelPolicy;
            if (policy != null)
            {
                if (!policy.filter.Allows(ap))
                {
                    JobFailReason.Is("Not allowed to wear that by policy.");
                    return false;
                }
            }
            else
            {
                if (ap.HasThingCategory(ThingCategoryDefOf.Diapers) && pawn.guest.IsInteractionDisabled(PrisonerInteractionModeDefOf.DiaperChangesAllowed))
                {
                    JobFailReason.Is("Diaper changes are not allowed.");
                    return false;
                }
                else if (ap.HasThingCategory(ThingCategoryDefOf.Underwear) && pawn.guest.IsInteractionDisabled(PrisonerInteractionModeDefOf.UnderwearChangesAllowed))
                {
                    JobFailReason.Is("Underwear changes are not allowed.");
                    return false;
                }
            } 

            return true;
        }
        public static float diaperCapacityLeft(Apparel app)
        {
            int topHalf = (int)Math.Ceiling(app.MaxHitPoints / 2f);
            int bottomHalf = app.MaxHitPoints - topHalf;

            int topHalfHp = Math.Min(app.HitPoints - bottomHalf, 0);
            int bottomHalfHp = Math.Min(app.HitPoints - topHalfHp, 0);

            float topHalfPercent = topHalfHp / topHalf * 0.75f;
            float bottomHalfPercent = bottomHalfHp / bottomHalf * 0.25f;

            return topHalfPercent + bottomHalfPercent;
        }
        public static void reduceCapacity(Pawn pawn, float amount)
        {
            float absorbency = pawn.GetStatValue(StatDefOf.DiaperAbsorbency) / 3;

            if (absorbency < 0.1f) absorbency = 0.1f;
            float hitPointDamage = amount / (0.05f * absorbency);

            var app = getUnderwearOrDiaper(pawn);
            if(app != null)
            {
                int topHalf = (int)Math.Ceiling(app.MaxHitPoints / 2f);
                int bottomHalf = app.MaxHitPoints - topHalf;

                int chanced = chancedDamage(pawn, hitPointDamage);

                int topHalfHp = Math.Min(app.HitPoints - bottomHalf, 0);
                int bottomHalfHp = Math.Min(app.HitPoints - topHalfHp, 0);

                int topHalfImpact = Math.Min(topHalfHp, chanced);
                chanced -= topHalfImpact;

                int bottomHalfImpact = Math.Min(bottomHalfHp, chanced * 2);

                app.HitPoints -= Math.Min(app.HitPoints, topHalfImpact + bottomHalfImpact);
                if (app.HitPoints < 1)
                {
                    app.Notify_LordDestroyed();
                    Messages.Message("MessageClothDestroyedByAccident".Translate(pawn.Named("PAWN"), app.Named("CLOTH")), pawn, MessageTypeDefOf.NegativeEvent, true);
                    app.Destroy(DestroyMode.Vanish);
                }
            }
        }
        public static int chancedDamage(Pawn pawn, float damage)
        {
            float halfHitpoints = damage - (float)Math.Floor(damage);

            if (halfHitpoints > 0.0f && Rand.ChanceSeeded(halfHitpoints, pawn.HashOffsetTicks() + 4))
            {
                return (int)Math.Ceiling(damage);
            }

            return (int)Math.Floor(damage);
        }
        public static float getDiaperOrUndiesRating(this Pawn pawn, Apparel ap)
        {
            float rating = 0.0f;
            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            bool debugging = settings.debugging && settings.debuggingCloth;
            if (!ap.PawnCanWear(pawn, true)) return -100f;
            if (CompBiocodable.IsBiocoded(ap) && !CompBiocodable.IsBiocodedFor(ap, pawn)) return -100f;
            if (!allowedByPolicy(pawn, ap)) return -100f;
            
            if (ap.HasThingCategory(ThingCategoryDefOf.Diapers))
            {
                rating -= 0.5f; // Diapers by default less likely to be worn
                bool isNightDp = isNightDiaper(ap);
                if (pawn.IsPrisoner)
                {
                    rating += 1.5f; // If allowed for prisoner, we give it a blanket pass but don't overwrite underwear
                    if (isNightDp) rating -= 0.3f; // We don't give prisoners special diapers for the night if we have other choises
                    if(ap.def?.apparel?.defaultOutfitTags != null && ap.def.apparel.defaultOutfitTags.Contains("Slave"))
                    {
                        rating += 0.4f; // We prefer cheap cloth for this case if available
                    }
                }
                else
                {
                    if (isNightDp) rating += 1f; // Usually better than diapers and better than no underwear
                }
                if (prefersDiaper(pawn) || needsDiaper(pawn))
                {
                    if (isNightDp) rating -= 2f; // too thin
                    else rating += 5f;
                }
                else
                {
                    // If we don't allow underwear, we also don't really intend to put prisoners in night diapers
                    if (isNightDp && needsDiaperNight(pawn) && acceptsDiaperNight(pawn) && (!pawn.IsPrisoner || pawn.guest?.IsInteractionEnabled(PrisonerInteractionModeDefOf.UnderwearChangesAllowed) == true))
                    {
                        rating += 3f;
                    }
                }

                if (ap.HitPoints < (ap.MaxHitPoints / 2))
                {
                    rating -= 4f;
                }

                var preference = getDiaperPreference(pawn);
                if (preference == DiaperLikeCategory.Toddler)
                {
                    rating += 10f;
                }
                else if (preference == DiaperLikeCategory.Child)
                {
                    rating += 0.5f;
                }
                else if (preference == DiaperLikeCategory.Liked || preference == DiaperLikeCategory.Diaper_Lover)
                {
                    rating += 10f;
                }
                else if (preference == DiaperLikeCategory.Disliked)
                {
                    rating -= 10f;
                }

                if (debugging) Log.Message("Apparel " + ap.Label + " is diaper and rated " + rating + " for " + pawn.LabelShort);
            }
            else if (ap.HasThingCategory(ThingCategoryDefOf.Underwear))
            {
                rating += 2.0f; // Underwear is default more likely to be worn
                var preference = getDiaperPreference(pawn);
                if (preference == DiaperLikeCategory.Liked)
                {
                    rating -= 2f;
                }
                else if (preference == DiaperLikeCategory.Disliked)
                {
                    rating += 2f;
                }
                if (pawn.gender == Gender.Male && ap.HasThingCategory(ThingCategoryDefOf.FemaleCloth))
                {
                    rating -= 1f;

                }
                else if (pawn.gender == Gender.Female && ap.HasThingCategory(ThingCategoryDefOf.MaleCloth))
                {
                    rating -= 1f;
                }
                if (ap.HitPoints < (ap.MaxHitPoints / 2))
                {
                    rating -= 2f;
                }
                if (debugging) Log.Message("Apparel " + ap.Label + " is underwear and rated " + rating + " for " + pawn.LabelShort);

            }
            else
            {
                return -100f;
            }
            return rating;
        }
        public static float getOnesieRating(Pawn pawn, Apparel ap)
        {
            float rating = 0.0f;
            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debugging = settings.debugging && settings.debuggingCloth;
            if (!ap.PawnCanWear(pawn, true)) return -100f;
            if (!allowedByPolicy(pawn, ap)) return -100f;

            if (!ap.HasThingCategory(ThingCategoryDefOf.Onesies)) return -100f;
            
            rating -= 0.35f; // Onesies by default are less likely to be worn

            var preference = OnesieHelper.getOnesiePreference(pawn);
            if (preference == OnesieLikeCategory.Child)
            {
                rating += 0.35f; // children don't care
            }
            else if (preference == OnesieLikeCategory.Liked || preference == OnesieLikeCategory.Toddler)
            {
                rating += 5f;
            }
            else if (preference == OnesieLikeCategory.Disliked)
            {
                rating -= 10f;
            }
            if (needsDiaper(pawn)) rating += 0.8f;
            else if (needsDiaperNight(pawn)) rating += 0.4f;
            if (debugging) Log.Message("Apparel " + ap.Label + " is onesie and rated " + rating + " for " + pawn.LabelShort);

            return rating;
        }

        public static DiaperLikeCategory getDiaperPreference(Pawn pawn)
        {
            if (!pawn.isAdultMental())
            {
                if(pawn.isToddlerMental() || pawn.isBabyMental())
                {
                    return DiaperLikeCategory.Toddler;
                }
                return DiaperLikeCategory.Child;
            }
            TraitSet traits = pawn?.story?.traits;
            if (traits == null)
            {
                return DiaperLikeCategory.Neutral;
            }

            if (traits.HasTrait(TraitDefOf.Potty_Rebel))
            {
                return DiaperLikeCategory.Liked;
            }
            if (traits.HasTrait(TraitDefOf.Diaper_Lover))
            {
                return DiaperLikeCategory.Diaper_Lover;
            }
            if (traits.HasTrait(TraitDefOf.Big_Boy))
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
                    else if (ideo.HasPrecept(PreceptDefOf.Diapers_Liked))
                    {
                        return DiaperLikeCategory.Diaper_Lover;
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

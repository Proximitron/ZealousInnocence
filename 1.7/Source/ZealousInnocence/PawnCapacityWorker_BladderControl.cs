using DubsBadHygiene;
using HarmonyLib;
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
    public class PawnCapacityWorker_BladderControl : PawnCapacityWorker
    {
        static ZealousInnocenceSettings settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
        public bool simulateSleep = false;
        public bool simulateAwake = false;
        public override float CalculateCapacityLevel(HediffSet diffSet,
                                                      List<PawnCapacityUtility.CapacityImpactor> impactors = null)
        {
            float num2 = PawnCapacityUtility.CalculateTagEfficiency(
                diffSet,
                BodyPartTagDefOf.BladderControlSource,
                float.MaxValue,
                default(FloatRange),
                impactors) * Mathf.Min(CalculateCapacityAndRecord(diffSet, RimWorld.PawnCapacityDefOf.Consciousness, impactors), 1f);
            Pawn pawn = diffSet.pawn;
            if (pawn != null)
            {
                float strFactor = GetStrenghFactor(pawn);
                if (strFactor != 1f && impactors != null)
                {
                    impactors.Add(new CapacityImpactorCustom { customLabel = "PhraseBladderStrength".Translate(), customValue = strFactor });
                }
                num2 *= strFactor;

                float ageFactor = GetAgeFactor(pawn);
                if (ageFactor != 1f && impactors != null)
                {
                    impactors.Add(new CapacityImpactorCustom { customLabel = "PhraseAge".Translate(), customValue = ageFactor});
                }
                num2 *= ageFactor;

                num2 *= LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().generalBladderControlFactor;

                bool needsDiaper = num2 <= Helper_Diaper.NeedsDiaperBreakpoint;
                bool awake = (pawn.Awake() || simulateAwake) && !simulateSleep;
                
                float whileAsleepFactor = GetSleepingFactor(pawn, false);
                float whileAwakeTotal = num2;
                float whileAsleepTotal = whileAwakeTotal * whileAsleepFactor;

                float sleepFactor = awake ? GetSleepingFactor(pawn, awake) : whileAsleepFactor;

                if (sleepFactor < 0.99f && impactors != null)
                {
                    impactors.Add(new CapacityImpactorCustom { customLabel = "StateSleeping".Translate(), customValue = sleepFactor });
                }
                num2 *= sleepFactor;
                var currDiaper = Helper_Diaper.getDiaper(pawn);
                bool canChange = currDiaper == null || pawn.canChange(currDiaper,false);

                float bedwettingChance = Helper_Diaper.CalculateProbability(whileAsleepTotal);
                if (impactors != null)
                {
                    string bedwetting = "StateWordLow";
                    if (bedwettingChance > 0.1f)
                    {
                        if (bedwettingChance > 0.6f)
                        {
                            bedwetting = "StateWordVeryHigh";
                        }
                        else if (bedwettingChance > 0.4f)
                        {
                            bedwetting = "StateWordHigh";
                        }
                        else
                        {
                            bedwetting = "StateWordMedium";
                        }
                    }
                    if (needsDiaper)
                    {
                        impactors.Add(new CapacityImpactorCustom { customString = "StateNeedsDiapers".Translate() });
                    }
                    else
                    {
                        if(whileAsleepTotal <= Helper_Diaper.NeedsDiaperNightBreakpoint)
                        {
                            impactors.Add(new CapacityImpactorCustom { customString = "StateNeedsPullups".Translate() });
                        }
                    }
                    if(!canChange && impactors != null) impactors.Add(new CapacityImpactorCustom { customString = "ShortCapReasonCantRemove".Translate(currDiaper.def.LabelCap) });
                    impactors.Add(new CapacityImpactorCustom { customLabel = "PhraseDaytimeAccidents".Translate(), customValue = canChange ? Helper_Diaper.CalculateProbability(whileAwakeTotal) : 1f });
                    string wordBedwetting = "PhraseBedwetting".Translate();
                    impactors.Add(new CapacityImpactorCustom { customLabel = $"{wordBedwetting} ({bedwetting.Translate()})", customValue = bedwettingChance });

                    /*impactors.Add(new CapacityImpactorCustom { customLabel = $"Mental", customString = Helper_Regression.getAgeStageMental(pawn) });
                    impactors.Add(new CapacityImpactorCustom { customLabel = $"Physical", customString = Helper_Regression.getAgeStagePhysical(pawn) });*/
                    if (settings.debugging)
                    {
                        impactors.Add(new CapacityImpactorCustom { customLabel = $"Debug Values", customString = "---" });
                        if (pawn.isAdultMental()) impactors.Add(new CapacityImpactorCustom { customLabel = $"Mental State", customString = "Adult" });
                        if (pawn.isChildMental()) impactors.Add(new CapacityImpactorCustom { customLabel = $"Mental State", customString = "Child" });
                        if (pawn.isToddlerMental()) impactors.Add(new CapacityImpactorCustom { customLabel = $"Mental State", customString = "Toddler" });
                        if (pawn.isBabyMental()) impactors.Add(new CapacityImpactorCustom { customLabel = $"Mental State", customString = "Baby" });

                        AgeStage stages = pawn.GetAgeSocial();
                        foreach (AgeStage st in Enum.GetValues(typeof(AgeStage)))
                        {
                            if(st == AgeStage.None) continue;
                            if(stages.HasFlag(st)) impactors.Add(new CapacityImpactorCustom { customLabel = $"Social Behaviour", customString = st.ToString() });
                        }

                        impactors.Add(new CapacityImpactorCustom { customLabel = $"Mental Age", customString = pawn.getAgeStageMental().ToString("F2") });
                        impactors.Add(new CapacityImpactorCustom { customLabel = $"Physical Age", customString = pawn.getAgeStagePhysical().ToString("F2") });
                        impactors.Add(new CapacityImpactorCustom { customLabel = $"Bedwetting Chance", customString = (Helper_Bedwetting.PawnBedwettingChance(pawn, Helper_Regression.getAgeStagePhysicalMentalMin(pawn)) * 100).ToString("F2") + "%" });
                        
                        impactors.Add(new CapacityImpactorCustom { customLabel = $"Age list", customString = $"{pawn.toddlerMinAge()},{pawn.childMinAge()},{pawn.adultMinAge()}" });
                        impactors.Add(new CapacityImpactorCustom { customLabel = $"Play/Learn/Joy", customString = (pawn.ShouldHavePlaying() ? "Play" : "") + (pawn.ShouldHaveLearning() ? "Learn" : "") + (!pawn.ShouldHaveLearning() && !pawn.ShouldHavePlaying() ? "Joy" : "") });
                    }
                }
            }
            else
            {
                Log.Error($"there was no pawn defined for PawnCapacityWorker_BladderControl in CalculateCapacityLevel?");
            }

            return num2;
        }

        public override bool CanHaveCapacity(BodyDef body)
        {
            return body.HasPartWithTag(BodyPartTagDefOf.BladderControlSource);
        }

        private float GetAgeFactor(Pawn pawn)
        {
            if (!pawn.RaceProps.Humanlike)
                return 1.0f;

            float ageYears = pawn.getAgeStagePhysicalMentalMin(); //pawn.ageTracker.AgeBiologicalYearsFloat;

            // Per-race band boundaries (in years)
            float toddlerStart = pawn.toddlerMinAge();   // start of toddler band
            float childStart = pawn.childMinAge();     // start of child band
            float adultStart = pawn.adultMinAge();     // start of adult/teen band
            float teenEnd = pawn.teenMaxAge();      // end of teen window (inside adult)
            float oldStart = pawn.oldMinAge();       // start of "old" band
            float lifeExp = pawn.RaceProps.lifeExpectancy;

            // Make sure the bands are in sensible order even for weird races
            if (teenEnd < adultStart) teenEnd = adultStart;
            if (oldStart < teenEnd) oldStart = teenEnd;
            if (lifeExp < oldStart) lifeExp = oldStart;

            float factor;

            // 0) Babies (before toddlerBand) → no control
            if (ageYears < toddlerStart)
            {
                factor = 0f;
            }
            // 1) Toddler band: 0 → 0.3
            else if (ageYears < childStart)
            {
                float t = Mathf.InverseLerp(toddlerStart, childStart, ageYears);
                factor = Mathf.Lerp(0f, 0.5f, t);
            }
            // 2) Child band: 0.3 → 0.9
            else if (ageYears < adultStart)
            {
                float t = Mathf.InverseLerp(childStart, adultStart, ageYears);
                factor = Mathf.Lerp(0.5f, 0.9f, t);
            }
            // 3) Teen / early adult: 0.9 → 1.0 (adultMin..teenMax)
            else if (ageYears < teenEnd)
            {
                float t = Mathf.InverseLerp(adultStart, teenEnd, ageYears);
                factor = Mathf.Lerp(0.9f, 1.0f, t);
            }
            // 4) Adult (after teenMax until oldStart): full control
            else if (ageYears < oldStart)
            {
                factor = 1.0f;
            }
            // 5) Old age: gently drop from 1.0 → 0.8 between oldStart and lifeExpectancy
            else if (ageYears < lifeExp)
            {
                float t = Mathf.InverseLerp(oldStart, lifeExp, ageYears);
                factor = Mathf.Lerp(1.0f, 0.8f, t);
            }
            // 6) Very old (beyond life expectancy): keep reduced but stable
            else
            {
                factor = 0.8f;
            }

            // Round to 2 decimals
            factor = Mathf.Round(factor * 100f) / 100f;
            return factor;
        }
        private float GetSleepingFactor(Pawn pawn, bool isAwake)
        {
            float total = 1.0f;
            if (!isAwake)
            {
                if (pawn.health.hediffSet.HasHediff(HediffDefOf.BedWetting))
                {
                    total -= 0.6f;
                }
                else
                {
                    total -= 0.17f;

                    if (pawn.isChildPhysical())
                    {
                        float inChildStagePosition = pawn.getAgeStagePhysical() - pawn.childMinAge();
                        float childEndPosition = pawn.adultMinAge();
                        float pct = Mathf.InverseLerp(pawn.childMinAge(), childEndPosition, pawn.getAgeStagePhysical());
                        total -= (0.15f * pct);
                    }
                }
                
            }
            return Math.Min(1f, total * LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().generalNighttimeControlFactor);
        }
        private float GetStrenghFactor(Pawn pawn)
        {
            return pawn.GetStatValue(StatDefOf.BladderStrengh, true);
        }

        public bool CapableOf(Pawn pawn)
        {
            float bladderControlLevel = pawn.health.capacities.GetLevel(PawnCapacityDefOf.BladderControl);
            return bladderControlLevel >= PawnCapacityDefOf.BladderControl.minForCapable;
        }
        public float SimulateBladderControlDuringSleep(Pawn pawn)
        {
            simulateSleep = true;
            float simulatedLevel = CalculateCapacityLevel(pawn.health.hediffSet, null);
            simulateSleep = false;
            return simulatedLevel;
        }
        public float SimulateBladderControlAwake(Pawn pawn)
        {
            simulateAwake = true;
            float simulatedLevel = CalculateCapacityLevel(pawn.health.hediffSet, null);
            simulateAwake = false;
            return simulatedLevel;
        }
    }
}

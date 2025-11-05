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
                bool canChange = Helper_Regression.canChangeDiaperOrUnderwear(pawn);
                float bedwettingChance = canChange ? Helper_Diaper.CalculateProbability(whileAsleepTotal) : 1f;
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
                    if (needsDiaper || !canChange)
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
                    if(!canChange && impactors != null) impactors.Add(new CapacityImpactorCustom { customString = "ShortReasonCantChangeDiaperOrUnderwearSelf".Translate() });
                    impactors.Add(new CapacityImpactorCustom { customLabel = "PhraseDaytimeAccidents".Translate(), customValue = canChange ? Helper_Diaper.CalculateProbability(whileAwakeTotal) : 1f });
                    string wordBedwetting = "PhraseBedwetting".Translate();
                    impactors.Add(new CapacityImpactorCustom { customLabel = $"{wordBedwetting} ({bedwetting.Translate()})", customValue = bedwettingChance });

                    /*impactors.Add(new CapacityImpactorCustom { customLabel = $"Mental", customString = Helper_Regression.getAgeStageMental(pawn) });
                    impactors.Add(new CapacityImpactorCustom { customLabel = $"Physical", customString = Helper_Regression.getAgeStagePhysical(pawn) });*/
                    if (settings.debugging)
                    {
                        if (pawn.isAdultMental()) impactors.Add(new CapacityImpactorCustom { customLabel = $"Mental State", customString = "Adult" });
                        if (pawn.isChildMental()) impactors.Add(new CapacityImpactorCustom { customLabel = $"Mental State", customString = "Child" });
                        if (pawn.isToddlerMental()) impactors.Add(new CapacityImpactorCustom { customLabel = $"Mental State", customString = "Toddler" });
                        if (pawn.isBabyMental()) impactors.Add(new CapacityImpactorCustom { customLabel = $"Mental State", customString = "Baby" });
                        impactors.Add(new CapacityImpactorCustom { customLabel = $"Age list", customString = $"{pawn.toddlerMinAge()},{pawn.childMinAge()},{pawn.adultMinAge()}" } );
                        impactors.Add(new CapacityImpactorCustom { customLabel = $"Mental Age", customString = pawn.getAgeStageMental().ToString("F2") });
                        impactors.Add(new CapacityImpactorCustom { customLabel = $"Physical Age", customString = pawn.getAgeStagePhysical().ToString("F2") });
                        impactors.Add(new CapacityImpactorCustom { customLabel = $"Bedwetting Chance", customString = $"{ Helper_Bedwetting.PawnBedwettingChance(pawn, Helper_Regression.getAgeStagePhysicalMentalMin(pawn)) }:F2" });

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
            if (!pawn.RaceProps.Humanlike) return 1.0f;
            int age = Helper_Regression.getAgeStageMentalInt(pawn);
            float factor;

            // Young age factor calculation
            if (age <= 3)
            {
                factor = 0f; // Toddlers have no bladder control
            }
            else if (age <= 6)
            {
                factor = Mathf.Lerp(0f, 0.5f, Mathf.InverseLerp(3, 6, age));
            }
            else if (age <= 9)
            {
                factor = Mathf.Lerp(0.5f, 0.75f, Mathf.InverseLerp(6, 9, age));
            }
            else if (age <= 17)
            {
                factor = Mathf.Lerp(0.75f, 1.0f, Mathf.InverseLerp(9, 17, age));
            }
            // Senior age factor calculation
            else if (age >= 50)
            {
                if (age <= 70)
                {
                    factor = 1.0f - (age - 50) / 20f * 0.2f; // Linear decrease from 1.0 at age 50 to 0.75 at age 70
                }
                else
                {
                    factor = 0.80f; // Maximum reduction at age 70 and beyond
                }
            }
            else
            {
                factor = 1.0f; // Full control for ages 14 to 50
            }

            // Round to 2 decimal places
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

﻿using DubsBadHygiene;
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
                    impactors.Add(new CapacityImpactorCustom { customLabel = "Bladder Strength", customValue = strFactor });
                }
                num2 *= strFactor;

                float ageFactor = GetAgeFactor(pawn);
                if (ageFactor != 1f && impactors != null)
                {
                    impactors.Add(new CapacityImpactorCustom { customLabel = "Age", customValue = ageFactor});
                }
                num2 *= ageFactor;

                num2 *= LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().generalBladderControlFactor;

                bool needsDiaper = num2 <= DiaperHelper.NeedsDiaperBreakpoint;
                bool awake = (pawn.Awake() || simulateAwake) && !simulateSleep;
                
                float whileAsleepFactor = GetSleepingFactor(pawn, false);
                float whileAwakeTotal = num2;
                float whileAsleepTotal = whileAwakeTotal * whileAsleepFactor;

                float sleepFactor = awake ? GetSleepingFactor(pawn, awake) : whileAsleepFactor;

                if (sleepFactor < 0.99f && impactors != null)
                {
                    impactors.Add(new CapacityImpactorCustom { customLabel = "Sleeping", customValue = sleepFactor });
                }
                num2 *= sleepFactor;

                float bedwettingChance = DiaperHelper.CalculateProbability(whileAsleepTotal);
                if (impactors != null)
                {
                    string bedwetting = "(low)";
                    if (bedwettingChance > 0.2f)
                    {
                        if (bedwettingChance > 0.6f)
                        {
                            bedwetting = $"(very high)";
                        }
                        else if (bedwettingChance > 0.4f)
                        {
                            bedwetting = $"(high)";
                        }
                        else
                        {
                            bedwetting = $"(medium)";
                        }
                    }
                    if (needsDiaper)
                    {
                        impactors.Add(new CapacityImpactorCustom { customString = "Needs Diapers" });
                    }
                    else
                    {
                        if(whileAsleepTotal <= DiaperHelper.NeedsDiaperNightBreakpoint)
                        {
                            impactors.Add(new CapacityImpactorCustom { customString = "Needs Pull-Ups" });
                        }
                    }
                    impactors.Add(new CapacityImpactorCustom { customLabel = "Daytime Accidents", customValue = DiaperHelper.CalculateProbability(whileAwakeTotal) });
                    impactors.Add(new CapacityImpactorCustom { customLabel = $"Bedwetting {bedwetting}", customValue = DiaperHelper.CalculateProbability(whileAsleepTotal) });
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
            int age = pawn.ageTracker.AgeBiologicalYears;
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
                    total -= 0.7f;
                }
                else
                {
                    total -= 0.17f;
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

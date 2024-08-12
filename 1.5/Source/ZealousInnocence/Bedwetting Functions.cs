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
    public class HediffGiver_BedWetting : HediffGiver
    {
        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            if (!pawn.IsColonist) return;
            var def = HediffDefOf.BedWetting;
            var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if(hediff != null)
            {
                hediff.Severity = BedWetting_Helper.BedwettingSeverity(pawn);
            }
            
            if (pawn.health.hediffSet.HasHediff(HediffDefOf.Incontinent)) return;

            bool shouldWet = BedWetting_Helper.BedwettingAtAge(pawn, Helper_Regression.getAgeStageInt(pawn));
            //Log.Message($"ZealousInnocence bedwetting interval {Find.TickManager.TicksGame}: Pawn {pawn.LabelShort} {shouldWet}");
            
            var needDiaper = Helper_Diaper.needsDiaper(pawn);


            if (shouldWet) { 
                if (!pawn.health.hediffSet.HasHediff(def)) {
                    if (!needDiaper)
                    {
                        BedWetting_Helper.AddHediff(pawn);
                        Messages.Message($"{pawn.Name.ToStringShort} has developed a bedwetting condition.", MessageTypeDefOf.NegativeEvent, true);
                    } 
                }
            }
            else
            {
                if (pawn.health.hediffSet.HasHediff(def))
                {
                    if (!needDiaper){
                        pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(def));

                        Messages.Message($"{pawn.Name.ToStringShort} has outgrown their bedwetting condition.", MessageTypeDefOf.PositiveEvent, true);
                    }
                }
            }
        }
    }
    public static class BedWetting_Helper
    {
        public static Hediff AddHediff(Pawn pawn)
        {
            var newHediff = HediffMaker.MakeHediff(HediffDefOf.BedWetting, pawn);
            newHediff.Severity = BedWetting_Helper.BedwettingSeverity(pawn);
            pawn.health.AddHediff(newHediff);
            return newHediff;
        }
        public static float BedwettingModifier(Pawn pawn)
        {
            return pawn.GetStatValue(StatDefOf.BedwettingChance, true) - 1.0f;
        }
        public static float BedwettingSeverity(Pawn pawn)
        {
            var age = Helper_Regression.getAgeStageInt(pawn);
            if (age <= 12)
            {
                return 0.1f; // Child stage
            }
            else if (age <= 16)
            {
                return 0.4f; // Teen stage
            }
            else
            {
                return 0.7f; // Adult stage
            }
        }
        public static bool BedwettingAtAge(Pawn pawn, int age)
        {
            float chance;

            var diaperNeed = pawn.needs.TryGetNeed<Need_Diaper>();
            if (diaperNeed == null)
            {
                Log.WarningOnce($"ZealousInnocence Warning: Pawn {pawn.LabelShort} has no Need_Diaper for BedwettingAtAge check, skipping all bedwetting related features.",pawn.thingIDNumber - 9242);
                return false;
            }
            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            
            if (age <= 3)
            {
                chance = 1f; // 100% chance for ages 0 to 3
            }
            else if(age <= 6)
            {
                chance = Mathf.Lerp(0.95f, 0.8f, Mathf.InverseLerp(3, 6, age));
            }
            else if (age <= 9)
            {
                chance = Mathf.Lerp(0.8f, 0.39f, Mathf.InverseLerp(6, 9, age));
            }
            else if (age <= 16)
            {
                chance = Mathf.Lerp(0.8f, settings.adultBedwetters + 0.05f, Mathf.InverseLerp(9, 16, age));
            }
            else if (age <= 65)
            {
                chance = settings.adultBedwetters; // Constant 0.05 chance from age 16 to 65, now changed to dynamic
            }
            else if (age <= 80)
            {
                chance = Mathf.Lerp(settings.adultBedwetters, 0.35f, Mathf.InverseLerp(65, 80, age)); // Gradually increase from 0.05 to 0.3 from age 65 to 80
            }
            else
            {
                chance = 0.3f; // Constant 0.3 chance from age 81+
            }
            if (chance < settings.adultBedwetters) chance = settings.adultBedwetters;
            if(settings.adultBedwetters <= 0f)
            {
                chance = 0.0f;
            }
            else if(settings.adultBedwetters >= 1f)
            {
                chance = 1.0f;
            }
            else
            {
                chance += BedwettingModifier(pawn);
            }
            
            chance = Math.Max(0f, Math.Min(1f, chance));
            if (diaperNeed.bedwettingSeed == 0)
            {
                int tries = 0;
                diaperNeed.bedwettingSeed = pawn.thingIDNumber;
                var def = HediffDefOf.BedWetting;
                
                var debugging = settings.debugging && settings.debuggingBedwetting;
                if (pawn.health.hediffSet.HasHediff(def))
                {
                    var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def);
                    if (hediff.Part != null)
                    {
                        if (debugging) Log.Message($"ZealousInnocence MIGRATION: Migrating organ related bedwetting to body for {pawn.LabelShort}!");
                        pawn.health.RemoveHediff(hediff);

                        AddHediff(pawn);
                    }
                }
                if (pawn.health.hediffSet.HasHediff(HediffDefOf.SmallBladder))
                {
                    var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def);
                    if (hediff.Part == null)
                    {
                        if (debugging) Log.Message($"ZealousInnocence MIGRATION: Migrating full body small bladder to new bladder size system for {pawn.LabelShort}!");
                        pawn.health.RemoveHediff(hediff);
                    }
                    Helper_Diaper.replaceBladderPart(pawn, HediffDefOf.SmallBladder);
                }
                if (chance < 0.99f && chance > 0.01f) // if chances are too small or too big. We go with random instead.
                {

                    while (true)
                    {
                        if (pawn.health.hediffSet.HasHediff(def) == BedwettingAtAge(pawn, Helper_Regression.getAgeStageInt(pawn)))
                        {
                            if (debugging) Log.Message($"ZealousInnocence MIGRATION DEBUG: Bedwetting seed {diaperNeed.bedwettingSeed} assigned with matching requirement {pawn.health.hediffSet.HasHediff(def)} for {pawn.LabelShort} after {tries} tries!");
                            break;
                        }
                        diaperNeed.bedwettingSeed++;
                        if (tries++ > 1000)
                        {
                            Log.Error($"ZealousInnocence MIGRATION Error: Could not resolve bedwetting seed to match requirement {pawn.health.hediffSet.HasHediff(def)} for {pawn.LabelShort} seed {diaperNeed.bedwettingSeed} after {tries} tries");
                            break;
                        }
                    }
                }
            }

            return Rand.ChanceSeeded(chance, diaperNeed.bedwettingSeed);
        }
    }
}

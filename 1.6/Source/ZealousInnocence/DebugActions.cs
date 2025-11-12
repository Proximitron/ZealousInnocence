using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ZealousInnocence
{
    public class DebugActions
    {
        [DebugAction("ZealousInnocence", "Reset Wetting Seed", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ResetWettingSeed()
        {
            List<Thing> selectedThings = Find.Selector.SelectedObjects.OfType<Thing>().ToList();
            foreach (var thing in selectedThings)
            {
                if (thing is Pawn pawn)
                {
                    var need = pawn.needs.TryGetNeed<Need_Diaper>();
                    if (need == null)
                    {
                         Log.Message($"[ZI]Seed reset for {pawn.Name} not possible. No Need_Diaper.");
                        return;
                    }
                    Helper_Bedwetting.RandomizeBedwettingSeed(pawn);
                    Log.Message($"[ZI]Seed reset for {pawn.Name} complete.");
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Reset wetting!", UnityEngine.Color.white, 3.85f);
                }
            }
        }
        [DebugAction("ZealousInnocence", "Reset Failure Seed", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ResetFailureSeed()
        {
            List<Thing> selectedThings = Find.Selector.SelectedObjects.OfType<Thing>().ToList();
            foreach (var thing in selectedThings)
            {
                if (thing is Pawn pawn)
                {
                    var need = pawn.needs.TryGetNeed<Need_Diaper>();
                    if (need == null)
                    {
                         Log.Message($"[ZI]Seed reset for {pawn.Name} not possible. No Need_Diaper.");
                        return;
                    }
                    if (need.FailureSeed == 0)
                    {
                         Log.Message($"[ZI]Seed reset for {pawn.Name} not possible. Seed already 0.");
                        return;
                    }
                    need.FailureSeed = 0;
                     Log.Message($"[ZI]Seed reset for {pawn.Name} complete.");
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Reset failure seed!", UnityEngine.Color.white, 3.85f);
                }
            }
        }

        [DebugAction("ZealousInnocence", "Apply regression (10 dmg)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        static void DBG_Apply10() { var p = Find.Selector.SingleSelectedThing as Pawn; if (p != null) ZI_DebugRegression.ApplyByDamage(p, 10f); }

        [DebugAction("ZealousInnocence", "Set physical regression (50%)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        static void DBG_SetPhysical50() { var p = Find.Selector.SingleSelectedThing as Pawn; if (p != null) ZI_DebugRegression.ApplyBySeverityPhysical(p, 0.5f); }

        [DebugAction("ZealousInnocence", "Heal physical regression (5%)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        static void DBG_HealPhysicalRegression5() { var p = Find.Selector.SingleSelectedThing as Pawn; if (p != null) ZI_DebugRegression.ApplyBySeverityPhysical(p, ZI_DebugRegression.GetCurrentSeverityPhysical(p) - 0.05f); }
        
        [DebugAction("ZealousInnocence", "Add physical regression (5%)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        static void DBG_AddPhysicalRegression5() { var p = Find.Selector.SingleSelectedThing as Pawn; if (p != null) ZI_DebugRegression.ApplyBySeverityPhysical(p, ZI_DebugRegression.GetCurrentSeverityPhysical(p) + 0.05f); }

        [DebugAction("ZealousInnocence", "Set mental regression (50%)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        static void DBG_SetMental50() { var p = Find.Selector.SingleSelectedThing as Pawn; if (p != null) ZI_DebugRegression.ApplyBySeverityMental(p, 0.5f); }

        [DebugAction("ZealousInnocence", "Heal mental regression (5%)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        static void DBG_HealMentalRegression5() { var p = Find.Selector.SingleSelectedThing as Pawn; if (p != null) ZI_DebugRegression.ApplyBySeverityMental(p, ZI_DebugRegression.GetCurrentSeverityMental(p) - 0.05f); }

        [DebugAction("ZealousInnocence", "Add mental regression (5%)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        static void DBG_AddMentalRegression5() { var p = Find.Selector.SingleSelectedThing as Pawn; if (p != null) ZI_DebugRegression.ApplyBySeverityMental(p, ZI_DebugRegression.GetCurrentSeverityMental(p) + 0.05f); }
        public static class ZI_DebugRegression
        {
            /*private static DamageDef FindRegressionDamageDef()
                => DefDatabase<DamageDef>.AllDefsListForReading
                    .FirstOrDefault(d => d.GetModExtension<RegressionDamageExtension>() != null);
            */
            private static DamageDef FindRegressionDamageDef() => DefDatabase<DamageDef>.GetNamedSilentFail("RegressionEffectDamage");
            private static DamageDef FindRegressionEffectDamageMentalDef() => DefDatabase<DamageDef>.GetNamedSilentFail("RegressionEffectDamageMental");
            public static void ApplyByDamage(Pawn p, float dmgAmount)
            {
                var def = FindRegressionDamageDef();
                if (def == null)
                {
                    Log.Warning("[ZI] No DamageDef with RegressionDamageExtension found.");
                    return;
                }

                // Minimal DamageInfo for debug
                var dinfo = new DamageInfo(def, dmgAmount, armorPenetration: 0f, angle: -1f, instigator: null, weapon: null);
                var res = new DamageWorker.DamageResult();
                Helper_Regression.ApplyPureRegressionDamage(dinfo, p, res, dmgAmount);
            }
            public static float GetCurrentSeverityMental(Pawn p)
            {
                var def = FindRegressionEffectDamageMentalDef();
                if (def == null)
                {
                    Log.Warning("[ZI] No DamageDef with RegressionDamageExtension found.");
                    return 0.0f;
                }
                var ext = def.GetModExtension<RegressionDamageExtension>();
                if (ext == null) return 0.0f;

                var hd = p.health.hediffSet.GetFirstHediffOfDef(ext.hediffCaused);
                if (hd == null) return 0.0f;
                return hd.Severity;
            }
            public static float GetCurrentSeverityPhysical(Pawn p)
            {
                var def = FindRegressionDamageDef();
                if (def == null)
                {
                    Log.Warning("[ZI] No DamageDef with RegressionDamageExtension found.");
                    return 0.0f;
                }
                var ext = def.GetModExtension<RegressionDamageExtension>();
                if (ext == null) return 0.0f;

                var hd = p.health.hediffSet.GetFirstHediffOfDef(ext.hediffCaused);
                if (hd == null) return 0.0f;
                return hd.Severity;
            }
            public static void ApplyBySeverityMental(Pawn p, float targetSeverity)
            {
                var def = FindRegressionEffectDamageMentalDef();
                if (def == null)
                {
                    Log.Warning("[ZI] No DamageDef with RegressionDamageExtension found.");
                    return;
                }
                var ext = def.GetModExtension<RegressionDamageExtension>();
                if (ext == null) return;

                Helper_Regression.SetRegressionHediff(p, p.def, ext.hediffCaused, targetSeverity);
                Log.Message($"[ZI][DBG] Set regression severity → {targetSeverity:0.###}");
            }
            public static void ApplyBySeverityPhysical(Pawn p, float targetSeverity)
            {
                var def = FindRegressionDamageDef();
                if (def == null)
                {
                    Log.Warning("[ZI] No DamageDef with RegressionDamageExtension found.");
                    return;
                }
                var ext = def.GetModExtension<RegressionDamageExtension>();
                if (ext == null) return;

                Helper_Regression.SetRegressionHediff(p, p.def, ext.hediffCaused, targetSeverity);
                Log.Message($"[ZI][DBG] Set regression severity → {targetSeverity:0.###}");
            }
        }
    }
}

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

        [DebugAction("ZealousInnocence", "Set regression (50%)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        static void DBG_Set50() { var p = Find.Selector.SingleSelectedThing as Pawn; if (p != null) ZI_DebugRegression.ApplyBySeverity(p, 0.5f); }

        [DebugAction("ZealousInnocence", "Heal regression (5%)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        static void DBG_HealRegression5() { var p = Find.Selector.SingleSelectedThing as Pawn; if (p != null) ZI_DebugRegression.ApplyBySeverity(p, ZI_DebugRegression.GetCurrentSeverity(p) - 0.05f); }

        public static class ZI_DebugRegression
        {
            private static DamageDef FindRegressionDamageDef()
                => DefDatabase<DamageDef>.AllDefsListForReading
                    .FirstOrDefault(d => d.GetModExtension<RegressionDamageExtension>() != null);

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
            public static float GetCurrentSeverity(Pawn p)
            {
                var def = FindRegressionDamageDef();
                if (def == null)
                {
                    Log.Warning("[ZI] No DamageDef with RegressionDamageExtension found.");
                    return 0.0f;
                }
                var ext = def.GetModExtension<RegressionDamageExtension>();
                if (ext == null) return 0.0f;

                var hd = p.health.hediffSet.GetFirstHediffOfDef(ext.regressionBuildup);
                if (hd == null) return 0.0f;
                return hd.Severity;
            }

            public static void ApplyBySeverity(Pawn p, float targetSeverity)
            {
                var def = FindRegressionDamageDef();
                if (def == null)
                {
                    Log.Warning("[ZI] No DamageDef with RegressionDamageExtension found.");
                    return;
                }
                var ext = def.GetModExtension<RegressionDamageExtension>();
                if (ext == null) return;

                var hd = p.health.hediffSet.GetFirstHediffOfDef(ext.regressionBuildup);
                if (hd == null)
                {
                    hd = HediffMaker.MakeHediff(ext.regressionBuildup, p);
                    p.health.AddHediff(hd);
                }
                hd.Severity = Mathf.Clamp(targetSeverity, 0f, Mathf.Min(ext.regressionBuildup.maxSeverity, ext.maxSeverity));
                Log.Message($"[ZI][DBG] Set regression severity → {hd.Severity:0.###}");
            }
        }
    }
}

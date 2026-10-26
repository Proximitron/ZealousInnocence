using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ZealousInnocence.Stats
{
    public class StatPart_BladderAge : StatPart
    {
        static float ageFactorMin = 3.0f;
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (!(req.Thing is Pawn pawn))
                return;

            float age = pawn.getAgeStagePhysical();

          
            float t = Mathf.Clamp01(age / pawn.teenMaxAge());
            float ageFactor = Mathf.Lerp(ageFactorMin, 1.0f, t);

            val *= ageFactor;
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (!(req.Thing is Pawn pawn))
                return null;

            float age = pawn.getAgeStagePhysical();
            float t = Mathf.Clamp01(age / pawn.teenMaxAge());
            float ageFactor = Mathf.Lerp(ageFactorMin, 1.0f, t);

            if (Mathf.Approximately(ageFactor, 1f))
                return null;

            return $"Age factor ({age:0.#} years): x{ageFactor:0.##}";
        }
    }
    [StaticConstructorOnStartup]
    public static class BladderRateMultiplier_Patch
    {
        static BladderRateMultiplier_Patch()
        {
            StatDef stat = DefDatabase<StatDef>.GetNamedSilentFail("BladderRateMultiplier");
            if (stat == null)
            {
                Log.Error("[ZI] Could not find StatDef 'BladderRateMultiplier' to patch.");
                return;
            }

            if (stat.parts == null)
                stat.parts = new List<StatPart>();

            stat.parts.Add(new StatPart_BladderAge());

            Log.Message("[ZI] Added StatPart_BladderAge to BladderRateMultiplier.");
        }
    }
}

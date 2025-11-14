using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ZealousInnocence
{
    /*public class RegressionDamageExtension : DefModExtension
    {

        /// <summary>
        /// The hediff severity per damage point
        /// </summary>
        public float magnitudePerDamage = 0.01f;

        /// <summary>the amount by which to reduce the raw damage to the pawn</summary>
        public float reduceValue = 1 / 3f;

        public HediffDef hediffCaused;

        /// <summary>
        /// gets all Configuration errors with this instance.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string configError in base.ConfigErrors())
            {
                yield return configError;
            }
            
            /* This is acceptable as this causes the damage to be directly applied and the pool mechanic to be skipped
             * if (hediffCaused == null)
                yield return "[ZI] RegressionDamageExtension: hediffCaused is null.";
            *//*
            if (reduceValue < 0f || reduceValue > 1f)
                yield return "[ZI] RegressionDamageExtension: reduceValue must be in [0,1].";

            if (hediffCaused != null &&
                hediffCaused.CompProps<CompProperties_RegressionInfluence>() == null)
            {
                yield return $"[ZI] RegressionDamageExtension: hediffCaused '{hediffCaused.defName}' " +
                             "has no CompProperties_RegressionInfluence (it should).";
            }

            if (magnitudePerDamage < 0f)
                yield return "[ZI] RegressionDamageExtension: magnitudePerDamage should be >= 0.";
        }
    }*/
}

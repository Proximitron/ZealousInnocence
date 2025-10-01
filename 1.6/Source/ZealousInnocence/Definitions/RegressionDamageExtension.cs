using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ZealousInnocence
{
    public class RegressionDamageExtension : DefModExtension
    {
        /// <summary>
        /// The regression hediff that should buildup 
        /// </summary>
        public HediffDef regressionBuildup;

        /// <summary>
        /// The hediff severity per damage point
        /// </summary>
        public float severityPerDamage = 0.01f;

        /// <summary>the amount by which to reduce the raw damage to the pawn</summary>
        public float reduceValue = 1 / 3f;


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

            if (regressionBuildup == null) yield return "no regression buildup hediff set";
        }
    }
}

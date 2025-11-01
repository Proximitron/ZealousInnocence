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
        /// The hediff severity per damage point
        /// </summary>
        public float mentalSeverityPerDamage = 0.01f;

        public float physicalSeverityPerDamage = 0.01f;

        /// <summary>the amount by which to reduce the raw damage to the pawn</summary>
        public float reduceValue = 1 / 3f;

        /// <summary>the maximum severity this damage can cause. 1.0f is 3 years for humans. Anything over that would be a baby.</summary>
        public float mentalMaxSeverity = 1.0f;

        public float physicalMaxSeverity = 1.2f;

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

        }
    }
}

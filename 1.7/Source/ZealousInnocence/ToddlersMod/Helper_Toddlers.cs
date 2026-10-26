using HarmonyLib;
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
    public static class Helper_Toddlers
    {
        private static bool? _toddlersLoaded;
        public static bool ToddlersLoaded
        {
            get
            {
                if (_toddlersLoaded == null)
                {
                    _toddlersLoaded = ModChecker.ToddlersActive();
                }
                return _toddlersLoaded.Value;
            }
        }


        private static readonly Type SettingsType = AccessTools.TypeByName("Toddlers.Toddlers_Settings");

        private static readonly System.Reflection.FieldInfo ManipulationFactorField = 
            SettingsType != null ? AccessTools.Field(SettingsType, "learningFactor_Manipulation") : null;

        private static readonly System.Reflection.FieldInfo WalkingFactorField =
            SettingsType != null ? AccessTools.Field(SettingsType, "learningFactor_Walk") : null;

        public static bool TryGetManipulationFactor(out float factor)
        {
            factor = 0.8f; // fallback default, should never be used
            if (ManipulationFactorField == null) return false;
            try
            {
                factor = Convert.ToSingle(ManipulationFactorField.GetValue(null));// static field => null instance
                return true;
            }
            catch { return false; }
        }

        public static bool TryGetWalkingFactor(out float factor)
        {
            factor = 0.8f; // fallback default, should never be used
            if (WalkingFactorField == null) return false;
            try
            {
                factor = Convert.ToSingle(WalkingFactorField.GetValue(null));// static field => null instance
                return true;
            }
            catch { return false; }
        }
        public static bool AdjustToddlersHediffs(Pawn pawn)
        {
            if (!ToddlersLoaded || !pawn.isToddlerMentalOrPhysical()) return false;
            float PercentGrowth = -1000f;
            Patch_ToddlerUtility.PercentGrowth(pawn, ref PercentGrowth);

            if (PercentGrowth == -1000f) throw new Exception("Error in AdjustToddlersHediffs as result of failure of PercentGrowth");

            if (PercentGrowth < 0f || PercentGrowth > 1f) return false;

            if (TryGetManipulationFactor(out float manipulationFactor))
            {
                var manipulationSeverity = Mathf.Min(1f, PercentGrowth / manipulationFactor);
                Hediff learningHediff = pawn.health.hediffSet.GetFirstHediffOfDef(ToddlersDefOf.LearningManipulation);

                if (learningHediff == null && manipulationSeverity < 1f && manipulationSeverity > 0f)
                {
                    TryResetHediffsForAge(pawn, false);
                    learningHediff = pawn.health.hediffSet.GetFirstHediffOfDef(ToddlersDefOf.LearningManipulation);
                }

                if (learningHediff != null) learningHediff.Severity = manipulationSeverity;
            }

            // Its possible a pawn has no need to learn walking, depending on race, so we don't force anything here
            if (TryGetWalkingFactor(out float walkingFactor))
            {
                Hediff learnWalkHediff = pawn.health.hediffSet.GetFirstHediffOfDef(ToddlersDefOf.LearningToWalk);
                if (learnWalkHediff != null)
                {
                    learnWalkHediff.Severity = Mathf.Min(1f, PercentGrowth / walkingFactor);
                }
            }

            return true;
        }

        private static Action<Pawn, bool> _reset;
        private static bool _lookedUp;

        public static void TryResetHediffsForAge(Pawn p, bool v)
        {
            if (!_lookedUp)
            {
                _lookedUp = true;
                if (ToddlersLoaded)
                {
                    var mi = AccessTools.Method(
                        "Toddlers.ToddlerLearningUtility:ResetHediffsForAge",
                        new[] { typeof(Pawn), typeof(bool) });

                    if (mi != null)
                        _reset = (Action<Pawn, bool>)Delegate.CreateDelegate(typeof(Action<Pawn, bool>), mi);
                }
            }

            _reset?.Invoke(p, v); // safe no-op if mod/method missing
        }
    }
}

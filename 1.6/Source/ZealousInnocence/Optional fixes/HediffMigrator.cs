using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace ZealousInnocence
{
    public abstract class Hediff_ZI_LegacyBridgeBase : HediffWithComps
    {
        private bool migrated;
        protected static ZealousInnocenceSettings Settings => LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
        protected abstract HediffDef NewInfluenceDef { get; }
        protected abstract BodyPartRecord BodyPart { get; }  // ok to return null for whole-body

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref migrated, "ZI_migrated", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && !migrated)
            {
                TryMigrate();
            }
        }

        // Guarantee removal on the next health pass even if immediate RemoveHediff() doesn't fire.
        public override bool ShouldRemove => migrated || base.ShouldRemove;

        private void TryMigrate()
        {
            if (migrated || pawn == null) return;
            migrated = true; // Important for ExposeData

            if (NewInfluenceDef != null && Severity > 0f)
            {
                var pool = pawn.health.hediffSet.GetFirstHediffOfDef(NewInfluenceDef);
                if (pool == null)
                {
                    pool = HediffMaker.MakeHediff(NewInfluenceDef, pawn, BodyPart);
                    pool.Severity = Severity;
                    pawn.health.AddHediff(pool, BodyPart);
                }
                else
                {
                    pool.Severity += Severity;
                }
            }
            DataMigrator();

            Severity = 0f;
            pawn.health.RemoveHediff(this);
            pawn.health.hediffSet.DirtyCache();
            if(Settings.debugging) Log.Message($"[ZI] Migrated legacy hediff '{def?.defName}' on {pawn.LabelShort}.");
        }

        public static void CopyCommonFields(object from, object to, IEnumerable<string> allowedFieldNames)
        {
            if (from == null || to == null || allowedFieldNames == null) return;

            var allowed = new HashSet<string>(allowedFieldNames);

            var src = from.GetType();
            var dst = to.GetType();

            foreach (var sf in src.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!allowed.Contains(sf.Name)) continue; // only copy whitelisted fields

                var df = dst.GetField(sf.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (df != null && df.FieldType.IsAssignableFrom(sf.FieldType))
                {
                    var oldValue = df.GetValue(to);
                    var newValue = sf.GetValue(from);

                    // safe string conversion for nulls / complex objects
                    string oldStr = oldValue != null ? oldValue.ToString() : "null";
                    string newStr = newValue != null ? newValue.ToString() : "null";

                    if (Settings.debugging) Log.Message($"[ZI] Migrated field '{sf.Name}': {oldStr} => {newStr}");

                    df.SetValue(to, newValue);
                }
            }
        }


        public abstract Hediff DataMigrator();
    }

    public class Hediff_RegressionDamageMental : Hediff_ZI_LegacyBridgeBase
    {
        protected override HediffDef NewInfluenceDef => HediffDefOf.MentalRegressionDamage;
        protected override BodyPartRecord BodyPart
        {
            get
            {
                return Helper_Regression.GetFirstBrainRecord(pawn);
            }
        }
        public override Hediff DataMigrator()
        {
            var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.RegressionMental) ?? pawn.health.AddHediff(HediffDefOf.RegressionMental, BodyPart);
            return hediff;
        }
    }

    public class Hediff_RegressionDamage : Hediff_ZI_LegacyBridgeBase
    {
        protected override HediffDef NewInfluenceDef => HediffDefOf.PhysicalRegressionDamage;
        protected override BodyPartRecord BodyPart => null;
        private int _lastHealTick;

        // Persisted state so age can return to what it was
        private long baselineBioTicks = -1;     // original biological age (ticks) when effect first applied
        private long lastBioSeen = -1;       // The last chronological age we have seen/updates ourself. The time difference to it is what we need to advance on the baseline
        public bool forceTick = false;
        private BeardDef lastBeardType = null;
        private TickTimer resurrectTimer = new TickTimer();
        private TickTimer resurrectTimerLetter = new TickTimer();
        // Growth moments
        public static bool AllowBirthdayThrough = false;
        public bool suppressVanillaBirthdays = true;
        private HashSet<int> deferredBirthdays = new();
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref forceTick, "ZI_reg_forceTick", false);
            Scribe_Values.Look(ref _lastHealTick, "ZI_reg_lastHealTick", 0);

            Scribe_Values.Look(ref baselineBioTicks, "ZI_reg_baselineBioTicks", -1);
            Scribe_Values.Look(ref lastBioSeen, "ZI_reg_lastBioSeen", -1);

            Scribe_Defs.Look(ref lastBeardType, "ZI_reg_lastBeardType");
            Scribe_Deep.Look<TickTimer>(ref resurrectTimerLetter, "ZI_reg_resurrectTimerLetter", Array.Empty<object>());
            Scribe_Deep.Look<TickTimer>(ref resurrectTimer, "ZI_reg_resurrectTimer", Array.Empty<object>());


            Scribe_Collections.Look(ref deferredBirthdays, "ZI_deferredBirthdays", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && deferredBirthdays == null) deferredBirthdays = new HashSet<int>();
        }
        public override Hediff DataMigrator()
        {
            var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.RegressionPhysical) ?? pawn.health.AddHediff(HediffDefOf.RegressionPhysical, BodyPart);

            CopyCommonFields(this, hediff, new[] { "_lastHealTick", "baselineBioTicks", "lastBioSeen", "lastBeardType", "resurrectTimerLetter", "resurrectTimer", "deferredBirthdays" });

            return hediff;
        }
    }
}

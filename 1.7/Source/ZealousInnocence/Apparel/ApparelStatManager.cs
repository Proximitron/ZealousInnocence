using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ZealousInnocence
{
    [StaticConstructorOnStartup]
    public static class ApparelStatManager
    {
        private static Dictionary<ThingDef, List<StatModifier>> originalOffsets = new Dictionary<ThingDef, List<StatModifier>>();
        private static Dictionary<ThingDef, List<StatModifier>> streamlinedOffsets = new Dictionary<ThingDef, List<StatModifier>>();

        static ApparelStatManager()
        {
            Initialize();
        }

        public static void Initialize()
        {
            // Resolve all apparel defs
            var defs = new Dictionary<string, ThingDef>();
            string[] defNames = new string[]
            {
                "Apparel_Diaper_Flimsy",
                "Apparel_Diaper_Night",
                "Apparel_Diaper",
                "Apparel_Premium_Diaper",
                "Apparel_Diaper_BabyDiaper",
                "Apparel_Diaper_Disposable",
                "Apparel_Simplie",
                "Apparel_Onesie",
                "Apparel_Gunsie",
                "ZI_Pacifier",
                "Apparel_Underwear_Loincloth",
                "Apparel_Underwear_Kids",
                "Apparel_Underwear_Boxers",
                "Apparel_Underwear_Panties",
            };

            foreach (var name in defNames)
            {
                var def = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                if (def != null)
                    defs[name] = def;
                else
                    Log.Warning($"[ZI] ApparelStatManager: Could not find ThingDef '{name}'");
            }

            // Cache original offsets (deep copy)
            foreach (var kvp in defs)
            {
                var def = kvp.Value;
                if (def.equippedStatOffsets != null)
                    originalOffsets[def] = def.equippedStatOffsets.Select(s => new StatModifier { stat = s.stat, value = s.value }).ToList();
                else
                    originalOffsets[def] = new List<StatModifier>();
            }

            // Build streamlined offsets
            // Stats that may not exist without DLC
            var slaveSuppression = DefDatabase<StatDef>.GetNamedSilentFail("SlaveSuppressionOffset");
            var suppressionPower = DefDatabase<StatDef>.GetNamedSilentFail("SuppressionPower");
            var diaperSupport = DefDatabase<StatDef>.GetNamedSilentFail("DiaperSupport");

            // 1. Flimsy Diaper
            if (defs.TryGetValue("Apparel_Diaper_Flimsy", out var flimsyDiaper))
            {
                var list = new List<StatModifier>
                {
                    MakeStat(RimWorld.StatDefOf.MoveSpeed, -0.08f),
                    MakeStat(RimWorld.StatDefOf.MeleeDodgeChance, -0.05f),
                    MakeStat(RimWorld.StatDefOf.PawnTrapSpringChance, 0.03f),
                    MakeStat(RimWorld.StatDefOf.HuntingStealth, -0.05f),
                };
                AddIfExists(list, slaveSuppression, 0.2f);
                streamlinedOffsets[flimsyDiaper] = list;
            }

            // 2. Pull-ups (Night)
            if (defs.TryGetValue("Apparel_Diaper_Night", out var nightDiaper))
            {
                var list = new List<StatModifier>
                {
                    MakeStat(RimWorld.StatDefOf.HuntingStealth, -0.01f),
                };
                AddIfExists(list, slaveSuppression, 0.1f);
                streamlinedOffsets[nightDiaper] = list;
            }

            // 3. Diaper
            if (defs.TryGetValue("Apparel_Diaper", out var diaper))
            {
                var list = new List<StatModifier>
                {
                    MakeStat(RimWorld.StatDefOf.MoveSpeed, -0.08f),
                    MakeStat(RimWorld.StatDefOf.MeleeDodgeChance, -0.05f),
                    MakeStat(RimWorld.StatDefOf.PawnTrapSpringChance, 0.03f),
                    MakeStat(RimWorld.StatDefOf.HuntingStealth, -0.03f),
                };
                AddIfExists(list, slaveSuppression, 0.1f);
                streamlinedOffsets[diaper] = list;
            }

            // 4. Premium Diaper
            if (defs.TryGetValue("Apparel_Premium_Diaper", out var premiumDiaper))
            {
                var list = new List<StatModifier>
                {
                    MakeStat(RimWorld.StatDefOf.MentalBreakThreshold, -0.05f),
                    MakeStat(RimWorld.StatDefOf.MoveSpeed, -0.01f),
                };
                AddIfExists(list, slaveSuppression, 0.15f);
                streamlinedOffsets[premiumDiaper] = list;
            }

            // 5. Baby Diaper
            if (defs.TryGetValue("Apparel_Diaper_BabyDiaper", out var babyDiaper))
            {
                var list = new List<StatModifier>
                {
                    MakeStat(RimWorld.StatDefOf.MoveSpeed, -0.02f),
                };
                streamlinedOffsets[babyDiaper] = list;
            }

            // 6. Disposable Diaper
            if (defs.TryGetValue("Apparel_Diaper_Disposable", out var disposableDiaper))
            {
                var list = new List<StatModifier>
                {
                    MakeStat(RimWorld.StatDefOf.MoveSpeed, -0.04f),
                };
                streamlinedOffsets[disposableDiaper] = list;
            }

            // 7. Simplie
            if (defs.TryGetValue("Apparel_Simplie", out var simplie))
            {
                var list = new List<StatModifier>
                {
                    MakeStat(RimWorld.StatDefOf.MoveSpeed, 0.1f),
                };
                AddIfExists(list, slaveSuppression, 0.2f);
                streamlinedOffsets[simplie] = list;
            }

            // 8. Onesie
            if (defs.TryGetValue("Apparel_Onesie", out var onesie))
            {
                var list = new List<StatModifier>
                {
                    MakeStat(RimWorld.StatDefOf.MoveSpeed, 0.1f),
                };
                AddIfExists(list, slaveSuppression, 0.1f);
                streamlinedOffsets[onesie] = list;
            }

            // 9. Worksie — NO CHANGES (excluded from streamlined dict)

            // 10. Gunsie
            if (defs.TryGetValue("Apparel_Gunsie", out var gunsie))
            {
                var list = new List<StatModifier>
                {
                    MakeStat(RimWorld.StatDefOf.MoveSpeed, 0.1f),
                    MakeStat(RimWorld.StatDefOf.MeleeDodgeChance, 0.4f),
                };
                AddIfExists(list, diaperSupport, 0.4f);
                AddIfExists(list, suppressionPower, 0.08f);
                streamlinedOffsets[gunsie] = list;
            }

            // 11. Pacifier
            if (defs.TryGetValue("ZI_Pacifier", out var pacifier))
            {
                var list = new List<StatModifier>
                {
                    MakeStat(RimWorld.StatDefOf.MentalBreakThreshold, -0.1f),
                    MakeStat(RimWorld.StatDefOf.NegotiationAbility, -0.15f),
                    MakeStat(RimWorld.StatDefOf.TradePriceImprovement, -0.15f),
                };
                AddIfExists(list, slaveSuppression, 0.15f);
                streamlinedOffsets[pacifier] = list;
            }

            // 12-15. All underwear — empty lists
            string[] underwearNames = { "Apparel_Underwear_Loincloth", "Apparel_Underwear_Kids", "Apparel_Underwear_Boxers", "Apparel_Underwear_Panties" };
            foreach (var name in underwearNames)
            {
                if (defs.TryGetValue(name, out var underwear))
                    streamlinedOffsets[underwear] = new List<StatModifier>();
            }

            // Apply based on current setting
            Apply();

            Log.Message("[ZI] ApparelStatManager initialized.");
        }

        public static void Apply()
        {
            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            bool streamlined = settings.streamlinedApparelStats;

            foreach (var kvp in originalOffsets)
            {
                var def = kvp.Key;
                if (streamlined && streamlinedOffsets.ContainsKey(def))
                    def.equippedStatOffsets = streamlinedOffsets[def];
                else
                    def.equippedStatOffsets = kvp.Value;
            }
        }

        private static StatModifier MakeStat(StatDef stat, float value)
        {
            return new StatModifier { stat = stat, value = value };
        }

        private static void AddIfExists(List<StatModifier> list, StatDef stat, float value)
        {
            if (stat != null)
                list.Add(MakeStat(stat, value));
        }
    }
}

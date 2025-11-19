using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.Grammar;

namespace ZealousInnocence
{
    /*public static class Patch_ExtraGrammarUtility_ExtraRulesForPawn
    {
        private static readonly Type ExtraType;
        private static readonly FieldInfo TempRulesField;

        static Patch_ExtraGrammarUtility_ExtraRulesForPawn()
        {
            // Get the SpeakUp ExtraGrammarUtility type
            ExtraType = AccessTools.TypeByName("SpeakUp.ExtraGrammarUtility")
                       ?? AccessTools.TypeByName("ExtraGrammarUtility");

            if (ExtraType == null)
            {
                Log.Warning("[ZI] Could not find SpeakUp.ExtraGrammarUtility – tempRules access disabled.");
                return;
            }

            // private static List<Rule_String> tempRules
            TempRulesField = ExtraType.GetField("tempRules", BindingFlags.NonPublic | BindingFlags.Static);
            if (TempRulesField == null)
            {
                Log.Warning("[ZI] Could not reflect ExtraGrammarUtility.tempRules – tempRules access disabled.");
            }
        }

        private static List<Rule_String> GetTempRules()
        {
            if (TempRulesField == null)
                return null;

            try
            {
                return TempRulesField.GetValue(null) as List<Rule_String>;
            }
            catch (Exception e)
            {
                Log.Error($"[ZI] Error reading ExtraGrammarUtility.tempRules: {e}");
                return null;
            }
        }

        // Your postfix on ExtraRulesForPawn
        public static void Postfix(string symbol, Pawn pawn, Pawn other)
        {
            if (pawn == null)
                return;

            var rules = GetTempRules();
            if (rules == null || rules.Count == 0)
                return;

            AddKeyUnique(rules, symbol + "age", pawn.getAgeBehaviour().ToString());
            AddKeyUnique(rules, symbol + "ageMental", pawn.getAgeStageMentalInt().ToString());
            AddKeyUnique(rules, symbol + "agePhysical", pawn.getAgeStagePhysicalInt().ToString());

            string speechStage = symbol + "stage";
            if (pawn.isBabyMentalOrPhysical())
            {
                AddKey(rules, speechStage, "Baby");
            }
            else
            {
                AddKey(rules, speechStage, "NotBaby");
                if (pawn.isToddlerMentalOrPhysical() || pawn.getAgeBehaviour() == 4)
                {
                    AddKey(rules, speechStage, "Toddler");
                }
                else if (pawn.isChildMental() || pawn.getAgeBehaviour() < pawn.adultMinAge())
                {
                    AddKey(rules, speechStage, "Child");
                }
                else
                {
                    AddKey(rules, speechStage, "Adult");
                }
            }
            if (pawn.isOld()) AddKey(rules, speechStage, "Old");
            if (pawn.isTeen()) AddKey(rules, speechStage, "Teen");

            string underwear = symbol + "underwearType";
            var current = Helper_Diaper.getUnderwearOrDiaper(pawn);
            if (current != null)
            {
                if (Helper_Diaper.isDiaper(current)) AddKeyUnique(rules, underwear, "Diaper");
                if (Helper_Diaper.isNightDiaper(current)) AddKey(rules, underwear, "DiaperNight");
                if (Helper_Diaper.isUnderwear(current)) AddKey(rules, underwear, "Underwear");
            }
            else
            {
                AddKeyUnique(rules, underwear, "None");
            }

            string pottyTraining = symbol + "pottyTraining";
            if (Helper_Diaper.needsDiaper(pawn)) AddKey(rules, pottyTraining, "NeedsDiaper");
            else if (Helper_Diaper.needsDiaperNight(pawn)) AddKey(rules, pottyTraining, "NeedsDiaperNight");
            else AddKey(rules, pottyTraining, "Trained");

            string stanceKey = symbol + "diaperStance";
            if (Helper_Diaper.prefersDiaper(pawn))
            {
                AddKeyUnique(rules, stanceKey, "LikeDiaper");
            }
            else if (Helper_Diaper.acceptsDiaper(pawn))
            {
                AddKeyUnique(rules, stanceKey, "AcceptDiaper");
            }
            else if (Helper_Diaper.acceptsDiaperNight(pawn))
            {
                AddKeyUnique(rules, stanceKey, "AcceptDiaperNight");
            }
            else
            {
                AddKeyUnique(rules, stanceKey, "HateDiaper");
            }
        }

        public static void RemoveKey(List<Rule_String> rules, string key)
        {
            rules.RemoveAll(r => r.keyword == key);
        }
        private static void AddKey(List<Rule_String> rules, string key, string value)
        {
            rules.Add(new Rule_String(key, value));
        }
        private static void AddKeyUnique(List<Rule_String> rules, string key, string value)
        {
            RemoveKey(rules, key);
            AddKey(rules, key, value);
        }
    }*/

    public static class Patch_SocialInteractionUtility_CanInitiateRandomInteraction
    {
        public static bool Prefix(Pawn p, ref bool __result)
        {
            __result = SocialInteractionUtility.CanInitiateInteraction(p, null) && p.RaceProps.Humanlike && !p.Downed && !p.InAggroMentalState && !p.IsInteractionBlocked(null, true, true) && p.Faction != null && !p.isBabyMentalOrPhysical() && !p.Inhumanized();
            return false;
        }
    }

    

    public static class Patch_GrammarUtility_RulesForPawn
    {
        public static void Postfix(
            string pawnSymbol,
            Pawn pawn,
            Dictionary<string, string> constants,
            bool addRelationInfoSymbol,
            bool addTags,
            ref IEnumerable<Rule> __result)
        {
            if (pawn == null)
                return;

            // Convert to a modifiable list
            var rules = __result != null ? new List<Rule>(__result) : new List<Rule>();

            AddKeyUnique(rules, pawnSymbol + "_age", pawn.getAgeBehaviour().ToString());
            AddKeyUnique(rules, pawnSymbol + "_ageMental", pawn.getAgeStageMentalInt().ToString());
            AddKeyUnique(rules, pawnSymbol + "_agePhysical", pawn.getAgeStagePhysicalInt().ToString());

            AgeStage stages = pawn.GetAgeSocial();
            string speechStage = pawnSymbol + "_stage";
            foreach (AgeStage st in Enum.GetValues(typeof(AgeStage)))
            {
                if (st == AgeStage.None)
                    continue;

                if (stages.HasFlag(st))
                    AddKey(rules, speechStage, st.ToString());
            }
            if (!stages.HasFlag(AgeStage.Baby))
            {
                AddKey(rules, speechStage, "Baby");
            }

            string underwear = pawnSymbol + "_underwearType";
            var current = Helper_Diaper.getUnderwearOrDiaper(pawn);
            if (current != null)
            {
                if (Helper_Diaper.isDiaper(current)) AddKeyUnique(rules, underwear, "Diaper");
                if (Helper_Diaper.isNightDiaper(current)) AddKey(rules, underwear, "DiaperNight");
                if (Helper_Diaper.isUnderwear(current)) AddKey(rules, underwear, "Underwear");
            }
            else
            {
                AddKeyUnique(rules, underwear, "None");
            }

            string pottyTraining = pawnSymbol + "_pottyTraining";
            if (Helper_Diaper.needsDiaper(pawn)) AddKey(rules, pottyTraining, "NeedsDiaper");
            else if (Helper_Diaper.needsDiaperNight(pawn)) AddKey(rules, pottyTraining, "NeedsDiaperNight");
            else AddKey(rules, pottyTraining, "Trained");

            string stanceKey = pawnSymbol + "_diaperStance";
            if (Helper_Diaper.prefersDiaper(pawn)) {
                AddKeyUnique(rules, stanceKey, "LikeDiaper");
            }
            else if (Helper_Diaper.acceptsDiaper(pawn))
            {
                AddKeyUnique(rules, stanceKey, "AcceptDiaper");
            }
            else if(Helper_Diaper.acceptsDiaperNight(pawn))
            {
                AddKeyUnique(rules, stanceKey, "AcceptDiaperNight");
            }
            else
            {
                AddKeyUnique(rules, stanceKey, "HateDiaper");
            }

            // Return modified rule list
            __result = rules;
        }

        public static void RemoveKey(List<Rule> rules, string key)
        {
            rules.RemoveAll(r => r.keyword == key);
        }
        private static void AddKey(List<Rule> rules,string key,string value)
        {
            rules.Add(new Rule_String(key, value));
        }
        private static void AddKeyUnique(List<Rule> rules, string key, string value)
        {
            RemoveKey(rules, key);
            AddKey(rules,key, value);
        }
    }
    /*
    [HarmonyPatch(typeof(GrammarUtility),
       nameof(GrammarUtility.RulesForPawn),
       new Type[] { typeof(string), typeof(Pawn), typeof(Dictionary<string, string>), typeof(bool), typeof(bool) })]
    public static class Patch_GrammarUtility_RulesForPawn
    {
        // flip this on/off at runtime by exposing a ModSetting, or just leave true while testing
        public static bool DebugEnabled = true;

        // simple anti-spam: remember the last tick we logged for a given symbol
        private static readonly Dictionary<string, int> _lastLogTickBySymbol = new();

        public static void Postfix(
            string pawnSymbol,
            Pawn pawn,
            Dictionary<string, string> constants,
            bool addRelationInfoSymbol,
            bool addTags,
            ref IEnumerable<Rule> __result)
        {
            if (pawn == null)
                return;

            // Keep a snapshot of the incoming rules for debugging BEFORE we mutate them
            var incoming = __result != null ? __result.ToList() : new List<Rule>();

            // ---- DEBUG OUTPUT (before change) ----
            if (DebugEnabled && ShouldLog(pawnSymbol))
            {
                try
                {
                    LogIncoming(pawnSymbol, pawn, incoming);
                }
                catch (Exception e)
                {
                    Log.Warning($"[ZI] Debug logging failed: {e}");
                }
            }

            // ---- Do your replacement ----
            var rules = incoming; // reuse the list we already made
            string key = pawnSymbol + "age"; // e.g. "INITIATOR_age"

            // capture old values (if any) for the log
            var oldAgeValues = rules
                .Where(r => r.keyword == key)
                .Select(r => r is Rule_String rs ? "" : r.ToString())
                .ToList();

            // remove all existing
            rules.RemoveAll(r => r.keyword == key);

            // your virtual age (use whatever method you prefer)
            int virtualAge = pawn.getAgeStageMentalInt(); // your custom method
            rules.Add(new Rule_String(key, virtualAge.ToString()));

            // ---- DEBUG OUTPUT (after change) ----
            if (DebugEnabled && ShouldLog(pawnSymbol))
            {
                try
                {
                    var newAgeValues = rules
                        .Where(r => r.keyword == key)
                        .Select(r => r is Rule_String rs ? "" : r.ToString())
                        .ToList();

                    var sb = new StringBuilder();
                    sb.AppendLine($"[YourMod] Replaced '{key}' for {pawn.LabelShortCap} ({pawn.ThingID})");
                    sb.AppendLine($"    Old values: {(oldAgeValues.Count > 0 ? string.Join(", ", oldAgeValues) : "<none>")}");
                    sb.AppendLine($"    New value : {(newAgeValues.Count > 0 ? string.Join(", ", newAgeValues) : "<missing?!>")}");
                    Log.Message(sb.ToString());
                }
                catch (Exception e)
                {
                    Log.Warning($"[YourMod] Debug post-change logging failed: {e}");
                }
            }

            __result = rules;
        }

        private static bool ShouldLog(string pawnSymbol)
        {
            // one log per symbol per tick
            int tick = Find.TickManager?.TicksGame ?? 0;
            if (!_lastLogTickBySymbol.TryGetValue(pawnSymbol, out var last) || last != tick)
            {
                _lastLogTickBySymbol[pawnSymbol] = tick;
                return true;
            }
            return false;
        }

        private static void LogIncoming(string pawnSymbol, Pawn pawn, List<Rule> rules)
        {
            string keyPrefix = pawnSymbol; // usually ends with "_" already
            var sb = new StringBuilder();
            sb.AppendLine($"[YourMod] RulesForPawn Postfix — incoming");
            sb.AppendLine($"    Pawn: {pawn.LabelShortCap} ({pawn.ThingID})  Faction: {pawn.Faction?.Name ?? "none"}");
            sb.AppendLine($"    pawnSymbol: '{pawnSymbol}'");
            sb.AppendLine($"    rules.Count: {rules.Count}");

            // Show all keywords that share the same prefix (INITIATOR_/RECIPIENT_/etc.)
            var samePrefix = rules
                .Select(r => r.keyword)
                .Where(k => k != null && k.StartsWith(keyPrefix))
                .Distinct()
                .OrderBy(k => k)
                .ToList();

            sb.AppendLine($"    Keywords starting with '{keyPrefix}': {samePrefix.Count}");
            foreach (var k in samePrefix)
                sb.AppendLine($"        - {k}");

            // Specifically inspect the 'age' rule(s) if present
            string ageKey = pawnSymbol + "age";
            var ageRules = rules.Where(r => r.keyword == ageKey).ToList();

            sb.AppendLine($"    Existing '{ageKey}' entries: {ageRules.Count}");
            foreach (var r in ageRules)
            {
                if (r is Rule_String rs)
                    sb.AppendLine($"        -> Rule_String: \"{rs.keyword}\" = \"{rs.ToString()}\"");
                else
                    sb.AppendLine($"        -> {r.GetType().Name}: {r}");
            }

            // If you want to peek at the text value of other common fields:
            // Try: nameFull / nameShort / gender / kind / title / faction
            string[] peekKeys =
            {
                "nameFull", "nameShort", "gender", "kind", "title", "faction_name", "label", "jobDefName"
            };
            foreach (var shortKey in peekKeys)
            {
                string fullKey = pawnSymbol + shortKey;
                var hit = rules.FirstOrDefault(r => r.keyword == fullKey);
                if (hit is Rule_String rs)
                    sb.AppendLine($"    Peek {fullKey} = \"{rs.ToString()}\"");
            }

            Log.Message(sb.ToString());
        }
    }*/
    
}

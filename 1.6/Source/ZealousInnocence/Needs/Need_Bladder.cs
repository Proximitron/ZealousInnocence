using DubsBadHygiene;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace ZealousInnocence
{
    public static class Need_Bladder_Patch
    {
        /*public static void BladderRate_Postfix(Need_Bladder __instance, ref float __result)
        {
            // Access the private pawn field
            Pawn pawn = (Pawn)AccessTools.Field(typeof(Need), "pawn").GetValue(__instance);
            // Add your custom logic here
            // For example, modify the bladder rate based on certain conditions
            float bladderStrength = pawn.GetStatValue(StatDefOf.BladderStrengh, true);
            var debugging = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var dbug = true; // debugging.debugging && debugging.debuggingCapacities;
            if (dbug)  Log.Message($"[ZI]BladderRate_Postfix: {pawn.Name} changing rate from {__result} on {bladderStrength}");
            if (bladderStrength != 1f)
            {
                float factor = 1f / bladderStrength;
               
                if (dbug)  Log.Message($"[ZI]BladderRate_Postfix: {pawn.Name} changing rate from {__result} to {__result*factor}");
                
                __result *= factor;
            }

        }*/

        public static bool NeedInterval_Prefix(Need_Bladder __instance)
        {
            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debugging = settings.debugging && settings.debuggingCapacities;
            if (__instance.IsFrozen)
            {
                return false;
            }

            Pawn pawn = (Pawn)AccessTools.Field(typeof(Need), "pawn").GetValue(__instance);
            float fallPerTick = (float)AccessTools.Property(typeof(Need_Bladder), "FallPerTick").GetValue(__instance);
            __instance.CurLevel -= fallPerTick * 150f * ModOption.BladderRateD.Val;
            if (__instance.CurLevel < 0f)
            {
                __instance.CurLevel = 0f;
            }

            if (__instance.CurCategory <= BowelCategory.needBathroom && pawn.CurrentBed() != null && pawn.health.capacities.CanBeAwake && (HealthAIUtility.ShouldSeekMedicalRest(pawn) || HealthAIUtility.ShouldSeekMedicalRestUrgent(pawn) || pawn.Downed || pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) < 0.3f || pawn.CurJob.playerForced))
            {
                if (settings.useBedPan && (settings.useBedPanIfDiaperEquipped || Helper_Diaper.getDiaper(pawn) == null))
                {
                    __instance.CurLevel = 1f;
                    Thing thing;
                    GenThing.TryDropAndSetForbidden(ThingMaker.MakeThing(DubDef.BedPan, null), pawn.Position, pawn.Map, ThingPlaceMode.Near, out thing, false);
                }

            }
            return false;
        }

        public static void CurCategory_Postfix(Need_Bladder __instance, ref BowelCategory __result)
        {

            // We only need to fix this if the bladder is very low and the pawn doesn't notice
            if (__instance.CurLevel < 0.15f)
            {
                // Access the private pawn field
                Pawn pawn = (Pawn)AccessTools.Field(typeof(Need), "pawn").GetValue(__instance);
                if (!Helper_Diaper.remembersPotty(pawn)) __result = BowelCategory.Empty;
            }
            // Add your custom logic here
            /*if (pawn.story.traits.HasTrait(TraitDefOf.AnotherTrait))
            {
                if (__result == BowelCategory.needBathroom && __instance.CurLevel < 0.1f)
                {
                    __result = BowelCategory.Busting; // Make the pawn more likely to feel urgent if they have another trait
                }
            }*/
        }
    }

    [HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
    public class Patch_ShouldHaveNeed
    {
        // Access private field "pawn" from Pawn_NeedsTracker
        private static readonly AccessTools.FieldRef<Pawn_NeedsTracker, Pawn> pawnRef =
            AccessTools.FieldRefAccess<Pawn_NeedsTracker, Pawn>("pawn");


        public static void Postfix(Pawn_NeedsTracker __instance, ref bool __result, NeedDef nd)
        {
            // Only care about the DBH need named "Bladder"
            if (nd == null || nd.defName != "Bladder")
                return; // run vanilla

            if (__result) return; // We don't touch anything that is already allowed

            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debug = settings.debugging && settings.debuggingCapacities;

            if (!settings.bladderForRaidCaravanVisitors) return; // Deactivated by setting. We keep it untouched!

            var pawn = pawnRef(__instance);
            if (pawn == null || pawn.Dead)
                return; // let vanilla decide / avoid NRE during load

            // If Guest or everyone don't get bladder need, this overwrite will also not be used
            if (!DubsBadHygieneMod.Settings.BladderNeed) return;
            if (!DubsBadHygieneMod.Settings.GuestsGetBladder) return; 

            // We only extend to humanlike visitors; animals/raiders unchanged.
            if (pawn.RaceProps?.Humanlike != true)
                return;

            // Skip pawns vanilla already handles (colonists/prisoners/slaves)
            if (pawn.IsColonist || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony)
                return;

            var bs = DubsBadHygieneMod.Settings.OverrideNd ? DubsBadHygieneMod.Settings.BladderRace : DubDef.Bladder.Exemptions.RaceDefs;
            if (bs?.Contains(pawn.def?.defName) == true) return;

            bs = DubsBadHygieneMod.Settings.OverrideNd ? DubsBadHygieneMod.Settings.BladderHediff : DubDef.Bladder.Exemptions.Hediffs;
            bool HasAnyHediffByDefName(List<string> list, out string hit)
            {
                hit = null;
                var hediffs = pawn?.health?.hediffSet?.hediffs;
                if (list == null || hediffs == null) return false;
                foreach (var h in hediffs)
                {
                    var dn = h?.def?.defName;
                    if (dn != null && list.Contains(dn))
                    {
                        hit = dn;
                        return true;
                    }
                }
                return false;
            }
            if (HasAnyHediffByDefName(bs, out var hitH)) return;
            if (pawn.RaceProps?.EatsFood != true) return;
            if (pawn.ageTracker?.CurLifeStage == LifeStageDefOf.HumanlikeBaby) return;

            // All vanilla preconditions (intelligence, dev stage, etc.) have already
            // been applied in the original method. We’re explicitly overriding the
            // remaining blockers (e.g. onlyIfCausedByHediff) for this one need.
            if (debug) Log.Message($"[ZI]Patch_ForceBladderForGuests: Pawn {pawn.LabelShort} force true");
            __result = true;
        }
        /*public static void Postfix(Pawn_NeedsTracker __instance, ref bool __result, NeedDef nd)
        {
            // Resolve pawn (private field)
            var pawnRef = AccessTools.FieldRefAccess<Pawn_NeedsTracker, Pawn>("pawn");
            var pawn = pawnRef != null ? pawnRef(__instance) : null;

            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debug = settings.debugging && settings.debuggingCapacities;

            if (nd == null || pawn == null) return;

            bool IsHygieneNeed(NeedDef n) => n == DubDef.DBHThirst || n == DubDef.Bladder || n == DubDef.Hygiene;
            if (!IsHygieneNeed(nd)) return;

            // ===== Trace helpers (do NOT touch __result here) =====
            string TAG() => $"[ZI/TRACE/{nd.defName}]";
            string PawnStr() => pawn != null ? $"{pawn.ThingID}:{pawn.LabelShort} ({pawn.def?.defName})" : "(null pawn)";
            void LogIf(string msg) { if (debug) Log.Message($"{TAG()} {msg}"); }

            bool HasAnyHediffByDefName(List<string> list, out string hit)
            {
                hit = null;
                var hediffs = pawn?.health?.hediffSet?.hediffs;
                if (list == null || hediffs == null) return false;
                foreach (var h in hediffs)
                {
                    var dn = h?.def?.defName;
                    if (dn != null && list.Contains(dn))
                    {
                        hit = dn;
                        return true;
                    }
                }
                return false;
            }

            // These helpers only set a local forced result; we assign __result right before returning.
            bool? forced = null; // null = don't override; true/false = override value
            void Reject(string reason)
            {
                if (debug) Log.Message($"{TAG()} REJECT {PawnStr()} -> {reason}");
                forced = false;
            }
            void Accept(string reason)
            {
                if (debug) Log.Message($"{TAG()} ACCEPT {PawnStr()} -> {reason}");
                forced = true;
            }

            // Snapshot
            try
            {
                LogIf($"Pawn: {PawnStr()} | Humanlike={pawn.RaceProps?.Humanlike} Animal={pawn.RaceProps?.Animal} EatsFood={pawn.RaceProps?.EatsFood} Int={pawn.RaceProps?.intelligence}");
                LogIf($"Guest: hasGuest={(pawn.guest != null)} HostFaction={(pawn.guest?.HostFaction != null ? pawn.guest.HostFaction.Name : "null")} | IsPrisoner={pawn.IsPrisoner} IsPrisonerOfColony={pawn.IsPrisonerOfColony} IsColonist={pawn.IsColonist}");
                LogIf($"LifeStage={pawn.ageTracker?.CurLifeStage?.defName} | Body={pawn.def?.race?.body?.defName} | Faction={(pawn.Faction?.Name ?? "null")} | training={(pawn.training != null)}");
                LogIf($"DBH Settings: BladderNeed={DubsBadHygieneMod.Settings.BladderNeed}, HygieneNeed={DubsBadHygieneMod.Settings.HygieneNeed}, ThirstNeed={Settings.ThirstNeed}");
                LogIf($"Overrides: OverrideNd={DubsBadHygieneMod.Settings.OverrideNd}, AllowNonHuman={DubsBadHygieneMod.Settings.AllowNonHuman}");
                LogIf($"Guest flags: GuestsGetBladder={DubsBadHygieneMod.Settings.GuestsGetBladder}, GuestsGetHygiene={DubsBadHygieneMod.Settings.GuestsGetHygiene}");
                LogIf($"Prisoner flags: PrisonersGetBladder={DubsBadHygieneMod.Settings.PrisonersGetBladder}, PrisonersGetHygiene={DubsBadHygieneMod.Settings.PrisonersGetHygiene}");
                LogIf($"Animal flags: AnimalsGetBladder={DubsBadHygieneMod.Settings.AnimalsGetBladder}, PetsGetBladder={DubsBadHygieneMod.Settings.PetsGetBladder}, PetsGetThirst={Settings.PetsGetThirst}");
            }
            catch { /* ignore *//* }

            try
            {
                // ===== BLADDER =====
                if (nd == DubDef.Bladder)
                {
                    if (!DubsBadHygieneMod.Settings.BladderNeed) { Reject("BladderNeed=false"); __result = forced.Value; return; }

                    if (!DubsBadHygieneMod.Settings.OverrideNd && !DubsBadHygieneMod.Settings.AllowNonHuman
                        && pawn.def?.race?.body != BodyDefOf.Human && (pawn.RaceProps?.Animal != true))
                    { Reject("NonHuman disallowed and not animal"); __result = forced.Value; return; }

                    if (pawn.RaceProps?.EatsFood != true) { Reject("Race does not eat food"); __result = forced.Value; return; }
                    if (pawn.ageTracker?.CurLifeStage == LifeStageDefOf.HumanlikeBaby) { Reject("HumanlikeBaby excluded"); __result = forced.Value; return; }

                    var bs = DubsBadHygieneMod.Settings.OverrideNd ? DubsBadHygieneMod.Settings.BladderBody : DubDef.Bladder.Exemptions.BodyDefs;
                    if (bs?.Contains(pawn.def?.race?.body?.defName) == true) { Reject($"Body exempt: {pawn.def?.race?.body?.defName}"); __result = forced.Value; return; }

                    bs = DubsBadHygieneMod.Settings.OverrideNd ? DubsBadHygieneMod.Settings.BladderRace : DubDef.Bladder.Exemptions.RaceDefs;
                    if (bs?.Contains(pawn.def?.defName) == true) { Reject($"Race exempt: {pawn.def?.defName}"); __result = forced.Value; return; }

                    bs = DubsBadHygieneMod.Settings.OverrideNd ? DubsBadHygieneMod.Settings.BladderHediff : DubDef.Bladder.Exemptions.Hediffs;
                    if (HasAnyHediffByDefName(bs, out var hitH)) { Reject($"Hediff exempt: {hitH}"); __result = forced.Value; return; }

                    if (pawn.RaceProps?.intelligence < nd.minIntelligence) { Reject($"Intelligence {pawn.RaceProps?.intelligence} < {nd.minIntelligence}"); __result = forced.Value; return; }

                    if (!DubsBadHygieneMod.Settings.GuestsGetBladder
                        && pawn.guest != null && pawn.guest.HostFaction == Faction.OfPlayerSilentFail && !pawn.IsPrisonerOfColony)
                    { Reject("Guest in player colony but GuestsGetBladder=false"); __result = forced.Value; return; }

                    if (pawn.guest != null && pawn.guest.HostFaction == null && !pawn.IsPrisoner && !pawn.IsColonist)
                    { Reject("Non-hosted non-prisoner non-colonist guest"); __result = forced.Value; return; }

                    if (!DubsBadHygieneMod.Settings.PrisonersGetBladder && pawn.IsPrisoner)
                    { Reject("Prisoner but PrisonersGetBladder=false"); __result = forced.Value; return; }

                    if (pawn.RaceProps?.Animal == true && pawn.training == null && !DubsBadHygieneMod.Settings.AnimalsGetBladder)
                    { Reject("Animal(no training) but AnimalsGetBladder=false"); __result = forced.Value; return; }

                    if (pawn.RaceProps?.Animal == true && pawn.training != null && !DubsBadHygieneMod.Settings.PetsGetBladder)
                    { Reject("Pet(trained) but PetsGetBladder=false"); __result = forced.Value; return; }

                    Accept("All Bladder checks passed"); __result = forced.Value; return;
                }

                // ===== HYGIENE =====
                if (nd == DubDef.Hygiene)
                {
                    if (!DubsBadHygieneMod.Settings.HygieneNeed) { Reject("HygieneNeed=false"); __result = forced.Value; return; }

                    if (!DubsBadHygieneMod.Settings.OverrideNd && !DubsBadHygieneMod.Settings.AllowNonHuman
                        && pawn.def?.race?.body != BodyDefOf.Human && (pawn.RaceProps?.Animal != true))
                    { Reject("NonHuman disallowed and not animal"); __result = forced.Value; return; }

                    var bs2 = DubsBadHygieneMod.Settings.OverrideNd ? DubsBadHygieneMod.Settings.HygieneBody : DubDef.Hygiene.Exemptions.BodyDefs;
                    if (bs2?.Contains(pawn.def?.race?.body?.defName) == true) { Reject($"Body exempt: {pawn.def?.race?.body?.defName}"); __result = forced.Value; return; }

                    bs2 = DubsBadHygieneMod.Settings.OverrideNd ? DubsBadHygieneMod.Settings.HygieneRace : DubDef.Hygiene.Exemptions.RaceDefs;
                    if (bs2?.Contains(pawn.def?.defName) == true) { Reject($"Race exempt: {pawn.def?.defName}"); __result = forced.Value; return; }

                    bs2 = DubsBadHygieneMod.Settings.OverrideNd ? DubsBadHygieneMod.Settings.HygieneHediff : DubDef.Hygiene.Exemptions.Hediffs;
                    if (HasAnyHediffByDefName(bs2, out var hitH2)) { Reject($"Hediff exempt: {hitH2}"); __result = forced.Value; return; }

                    if (pawn.RaceProps?.intelligence < nd.minIntelligence) { Reject($"Intelligence {pawn.RaceProps?.intelligence} < {nd.minIntelligence}"); __result = forced.Value; return; }

                    if (!DubsBadHygieneMod.Settings.GuestsGetHygiene
                        && pawn.guest != null && pawn.guest.HostFaction == Faction.OfPlayerSilentFail && !pawn.IsPrisonerOfColony)
                    { Reject("Guest in player colony but GuestsGetHygiene=false"); __result = forced.Value; return; }

                    if (pawn.guest != null && pawn.guest.HostFaction == null && !pawn.IsPrisoner && !pawn.IsColonist)
                    { Reject("Non-hosted non-prisoner non-colonist guest"); __result = forced.Value; return; }

                    if (!DubsBadHygieneMod.Settings.PrisonersGetHygiene && pawn.IsPrisoner)
                    { Reject("Prisoner but PrisonersGetHygiene=false"); __result = forced.Value; return; }

                    Accept("All Hygiene checks passed"); __result = forced.Value; return;
                }

                // ===== THIRST =====
                if (nd == DubDef.DBHThirst)
                {
                    if (!Settings.ThirstNeed) { Reject("ThirstNeed=false"); __result = forced.Value; return; }

                    if (!DubsBadHygieneMod.Settings.OverrideNd && !DubsBadHygieneMod.Settings.AllowNonHuman
                        && pawn.def?.race?.body != BodyDefOf.Human && (pawn.RaceProps?.Animal != true))
                    { Reject("NonHuman disallowed and not animal"); __result = forced.Value; return; }

                    if (pawn.ageTracker?.CurLifeStage == LifeStageDefOf.HumanlikeBaby) { Reject("HumanlikeBaby excluded"); __result = forced.Value; return; }

                    // NOTE: DBH uses *Bladder* lists for Thirst; this may be the hidden blocker.
                    var bs3 = DubsBadHygieneMod.Settings.OverrideNd ? DubsBadHygieneMod.Settings.BladderBody : DubDef.Bladder.Exemptions.BodyDefs;
                    if (bs3?.Contains(pawn.def?.race?.body?.defName) == true) { Reject($"(Thirst) Body exempt via *Bladder* list: {pawn.def?.race?.body?.defName}"); __result = forced.Value; return; }

                    bs3 = DubsBadHygieneMod.Settings.OverrideNd ? DubsBadHygieneMod.Settings.BladderRace : DubDef.Bladder.Exemptions.RaceDefs;
                    if (bs3?.Contains(pawn.def?.defName) == true) { Reject($"(Thirst) Race exempt via *Bladder* list: {pawn.def?.defName}"); __result = forced.Value; return; }

                    bs3 = DubsBadHygieneMod.Settings.OverrideNd ? DubsBadHygieneMod.Settings.BladderHediff : DubDef.Bladder.Exemptions.Hediffs;
                    if (HasAnyHediffByDefName(bs3, out var hitH3)) { Reject($"(Thirst) Hediff exempt via *Bladder* list: {hitH3}"); __result = forced.Value; return; }

                    if (pawn.RaceProps?.intelligence < nd.minIntelligence) { Reject($"Intelligence {pawn.RaceProps?.intelligence} < {nd.minIntelligence}"); __result = forced.Value; return; }
                    if (pawn.RaceProps?.EatsFood != true) { Reject("Race does not eat food"); __result = forced.Value; return; }

                    // Also reuses Bladder guest/prisoner flags for Thirst.
                    if (!DubsBadHygieneMod.Settings.GuestsGetBladder
                        && pawn.guest != null && pawn.guest.HostFaction == Faction.OfPlayerSilentFail && !pawn.IsPrisonerOfColony)
                    { Reject("(Thirst) Guest blocked by GuestsGetBladder=false"); __result = forced.Value; return; }

                    if (pawn.guest != null && pawn.guest.HostFaction == null && !pawn.IsPrisoner && !pawn.IsColonist)
                    { Reject("(Thirst) Non-hosted non-prisoner non-colonist guest"); __result = forced.Value; return; }

                    if (!DubsBadHygieneMod.Settings.PrisonersGetBladder && pawn.IsPrisoner)
                    { Reject("(Thirst) Prisoner blocked by PrisonersGetBladder=false"); __result = forced.Value; return; }

                    if (pawn.RaceProps?.Animal == true)
                    {
                        if (!Settings.PetsGetThirst) { Reject("(Thirst) PetsGetThirst=false"); __result = forced.Value; return; }
                        if (pawn.training == null) { Reject("(Thirst) Animal untrained"); __result = forced.Value; return; }
                        if (pawn.Faction != Faction.OfPlayerSilentFail) { Reject("(Thirst) Animal not in player faction"); __result = forced.Value; return; }
                    }

                    Accept("All Thirst checks passed"); __result = forced.Value; return;
                }
            }
            catch (Exception e)
            {
                Log.Error($"{TAG()} Exception while tracing ShouldHaveNeed: {e}");
                // fall through: do not override __result
            }
        }*/
    }
}



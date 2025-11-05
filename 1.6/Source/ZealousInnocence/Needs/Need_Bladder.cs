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
using UnityEngine;
using Verse;
using Verse.AI;
using static RimWorld.PsychicRitualRoleDef;

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

    public static class Patch_PawnComponents_AddRemove
    {
        // Preserving tracker
        public static void Prefix(Pawn pawn, bool actAsIfSpawned, out Pawn_LearningTracker __state)
        {
            __state = pawn?.learning;
        }
        // Runs after vanilla has added/removed components
        public static void Postfix(Pawn pawn, bool actAsIfSpawned, Pawn_LearningTracker __state)
        {
            if (pawn == null || pawn.Dead) return;
            if (!pawn.RaceProps.Humanlike) return;

            if(pawn.needs?.learning != null && pawn.learning == null)
            {
                if (__state != null)
                {
                    // Only reassign if vanilla cleared/replaced it.
                    if (!ReferenceEquals(pawn.learning, __state))
                        pawn.learning = __state;

                    return;
                }
                pawn.learning = new Pawn_LearningTracker(pawn);
            }
        }
    }

    public class DevStageExtension : DefModExtension
    {
        public bool allowBaby = false;
        public bool allowChild = false;
        public bool allowAdult = false;

        // Optional mutual-exclusion group
        public string group;
        public int priority;

        public bool mentalStat;
        public bool physicalStat;

        public bool Allows(Pawn pawn)
        {
            /*DevelopmentalStage s = pawn.DevelopmentalStage;
            var stageBase = (s == DevelopmentalStage.Baby && allowBaby)
            || (s == DevelopmentalStage.Child && allowChild)
            || (s == DevelopmentalStage.Adult && allowAdult);
            */

            if(pawn == null || pawn.ageTracker == null) return false;
            float age = pawn.ageTracker.AgeBiologicalYearsFloat;
            pawn.refreshAllAgeStageCaches();
            if (mentalStat && physicalStat)
            {
                age = pawn.getAgeStagePhysicalMentalMin();
            }
            else if (mentalStat)
            {
                age = pawn.getAgeStageMentalInt();
            }
            else if(physicalStat)
            {
                age = pawn.getAgeStageMentalInt();
            }

            // Babies and toddlers share the baby age stage. Toddler often handled seperatly by mod "Toddlers"
            if(pawn.isBabyAtAge(age) || pawn.isToddlerAtAge(age))
            {
                return allowBaby;
            }
            else if(pawn.isChildAtAge(age))
            {
                return allowChild;
            }
            else
            {
                return allowAdult;
            }
        }

    }

    public static class Patch_ShouldHaveNeed_Debug
    {
        // Toggle at runtime from dev console or add a settings UI later
        public static bool Enabled = false;

        // We capture the pawn via reflection (Pawn_NeedsTracker.pawn is a private field)
        private static Pawn GetPawn(Pawn_NeedsTracker tracker)
        {
            return AccessTools.FieldRefAccess<Pawn_NeedsTracker, Pawn>(tracker, "pawn");
        }

        public static void Prefix(Pawn_NeedsTracker __instance, NeedDef nd, ref Pawn __state)
        {
            if (!Enabled || nd == null || __instance == null) return;
            __state = GetPawn(__instance);
        }

        public static void Postfix(Pawn_NeedsTracker __instance, NeedDef nd, ref bool __result, Pawn __state)
        {
            if (nd == null || __instance == null) return;

            var pawn = __state ?? GetPawn(__instance);
            if (pawn == null) return;
            if (Bladder_RaidCaravanVisitor_Postfix(__instance, ref __result, nd)) return;
            if (Postfix_StageTracker(__instance, nd, ref __result)) return;

            if (!Enabled) return;
            string reason = ExplainDecision(pawn, nd, __result);
            Log.Message($"[ZI]Postfix pawn={pawn.LabelShortCap} need={nd.defName} -> {(__result ? "TRUE" : "FALSE")} {reason}");
        }
        public static bool Postfix_StageTracker(Pawn_NeedsTracker __instance, NeedDef nd, ref bool __result)
        {
            if (__instance == null || nd == null) return false;
            var pawn = GetPawn(__instance);
            if (pawn == null || !pawn.RaceProps.Humanlike) return false;

            var ext = nd.GetModExtension<DevStageExtension>();
            if (ext == null) return false; // No custom rule → leave vanilla outcome.

            // Stage gating (mandatory now that vanilla filter is gone)
            __result = ext.Allows(pawn);
            if(Enabled) Log.Message($"[ZI]Postfix_StageTracker pawn={pawn.LabelShortCap} need={nd.defName} -> {(__result ? "TRUE" : "FALSE")} StageTracker");
            return true;
        }

        public static bool Bladder_RaidCaravanVisitor_Postfix(Pawn_NeedsTracker __instance, ref bool __result, NeedDef nd)
        {
            // Only care about the DBH need named "Bladder"
            if (nd == null || nd.defName != "Bladder")
                return false; // run vanilla

            if (__result) return false; // We don't touch anything that is already allowed

            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debug = settings.debugging && settings.debuggingCapacities;

            if (!settings.bladderForRaidCaravanVisitors) return false; // Deactivated by setting. We keep it untouched!

            var pawn = GetPawn(__instance);
            if (pawn == null || pawn.Dead)
                return false; // let vanilla decide / avoid NRE during load

            // If Guest or everyone don't get bladder need, this overwrite will also not be used
            if (!DubsBadHygieneMod.Settings.BladderNeed) return false;
            if (!DubsBadHygieneMod.Settings.GuestsGetBladder) return false;

            // We only extend to humanlike visitors; animals/raiders unchanged.
            if (pawn.RaceProps?.Humanlike != true)
                return false;

            // Skip pawns vanilla already handles (colonists/prisoners/slaves)
            if (pawn.IsColonist || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony)
                return false;

            var bs = DubsBadHygieneMod.Settings.OverrideNd ? DubsBadHygieneMod.Settings.BladderRace : DubDef.Bladder.Exemptions.RaceDefs;
            if (bs?.Contains(pawn.def?.defName) == true) return false;

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
            if (HasAnyHediffByDefName(bs, out var hitH)) return false;
            if (pawn.RaceProps?.EatsFood != true) return false;
            if (pawn.ageTracker?.CurLifeStage == LifeStageDefOf.HumanlikeBaby) return false;

            // All vanilla preconditions (intelligence, dev stage, etc.) have already
            // been applied in the original method. We’re explicitly overriding the
            // remaining blockers (e.g. onlyIfCausedByHediff) for this one need.
            if (debug) Log.Message($"[ZI]Patch_ForceBladderForGuests: Pawn {pawn.LabelShort} force true");
            __result = true;
            return true;
        }
        private static string ExplainDecision(Pawn pawn, NeedDef nd, bool result)
        {
            // Mirror vanilla order; return first blocking/allowing reason
            // 1) Intelligence
            if (pawn.RaceProps.intelligence < nd.minIntelligence)
                return "(blocked by minIntelligence)";

            // 2) Dev stage filter
            if (!nd.developmentalStageFilter.Has(pawn.DevelopmentalStage))
                return "(blocked by developmentalStageFilter)";

            // 3) Colonist-only
            if (nd.colonistsOnly && (pawn.Faction == null || !pawn.Faction.IsPlayer))
                return "(blocked: colonistsOnly)";

            // 4) Player mechs only
            if (nd.playerMechsOnly && (!pawn.RaceProps.IsMechanoid || pawn.Faction != Faction.OfPlayer || pawn.OverseerSubject == null))
                return "(blocked: playerMechsOnly)";

            // 5) Colonist+Prisoners only
            if (nd.colonistAndPrisonersOnly &&
                (pawn.Faction == null || !pawn.Faction.IsPlayer) &&
                (pawn.HostFaction == null || pawn.HostFaction != Faction.OfPlayer))
                return "(blocked: colonistAndPrisonersOnly)";

            // 6) Hediff disables
            if (pawn.health.hediffSet.DisablesNeed(nd))
                return "(blocked: hediff disables need)";

            // 7) Gene disables
            if (ModsConfig.BiotechActive && pawn.genes != null && pawn.genes.DisablesNeed(nd))
                return "(blocked: gene disables need)";

            // 8) Trait disables
            if (pawn.story?.traits != null && pawn.story.traits.DisablesNeed(nd))
                return "(blocked: trait disables need)";

            // 9) Ideo disables
            if (pawn.Ideo != null && pawn.Ideo.DisablesNeed(nd))
                return "(blocked: ideo disables need)";

            // 10–13) only-if-caused flags
            bool gatingPresent = false;
            bool gatingEnabled = false;

            if (nd.onlyIfCausedByHediff)
            {
                gatingPresent = true;
                if (pawn.health.hediffSet.EnablesNeed(nd)) gatingEnabled = true;
            }
            if (ModsConfig.BiotechActive && nd.onlyIfCausedByGene)
            {
                gatingPresent = true;
                if (pawn.genes != null && pawn.genes.EnablesNeed(nd)) gatingEnabled |= true;
            }
            if (ModsConfig.IdeologyActive && nd.onlyIfCausedByIdeo)
            {
                gatingPresent = true;
                if (pawn.Ideo != null && pawn.Ideo.EnablesNeed(nd)) gatingEnabled |= true;
            }
            if (nd.onlyIfCausedByTrait)
            {
                gatingPresent = true;
                if (pawn.story?.traits != null && pawn.story.traits.EnablesNeed(nd)) gatingEnabled |= true;
            }
            if (gatingPresent && !gatingEnabled)
                return "(blocked: onlyIfCaused* gating not satisfied)";

            // 14) Prisoner
            if (nd.neverOnPrisoner && pawn.IsPrisoner)
                return "(blocked: neverOnPrisoner)";

            // 15) Slave
            if (nd.neverOnSlave && pawn.IsSlave)
                return "(blocked: neverOnSlave)";

            // 16) Mutant whitelist
            if (pawn.IsMutant && pawn.mutant.Def.disableNeeds &&
                (pawn.mutant.Def.needWhitelist == null || !pawn.mutant.Def.needWhitelist.Contains(nd)))
                return "(blocked: mutant disables need)";

            // 17) Title required
            if (nd.titleRequiredAny != null)
            {
                if (pawn.royalty == null) return "(blocked: title required, no royalty)";
                bool hasTitle = pawn.royalty.AllTitlesInEffectForReading.Any(t => nd.titleRequiredAny.Contains(t.def));
                if (!hasTitle) return "(blocked: titleRequiredAny not held)";
            }

            // 18) Nullifying precepts
            if (nd.nullifyingPrecepts != null && pawn.Ideo != null)
            {
                bool hasNullifier = nd.nullifyingPrecepts.Any(p => pawn.Ideo.HasPrecept(p));
                if (hasNullifier) return "(blocked: nullifying precept)";
            }

            // 19) Hediff required
            if (nd.hediffRequiredAny != null)
            {
                bool hasReqHediff = nd.hediffRequiredAny.Any(h => pawn.health.hediffSet.HasHediff(h, false));
                if (!hasReqHediff) return "(blocked: missing required hediff)";
            }

            // 20) Hard-coded Authority off
            if (nd.defName == "Authority")
                return "(blocked: defName==Authority)";

            // 21) Slaves-only
            if (nd.slavesOnly && !pawn.IsSlave)
                return "(blocked: slavesOnly)";

            // 22) Required comps (Anomaly)
            if (ModsConfig.AnomalyActive && nd.requiredComps != null)
            {
                foreach (var props in nd.requiredComps)
                {
                    if (pawn.TryGetComp(props) == null)
                        return "(blocked: missing required comp)";
                }
            }

            // 23) Food special-casing
            if (nd == NeedDefOf.Food)
                return pawn.RaceProps.EatsFood ? "(allowed: eats food)" : "(blocked: race does not eat food)";

            // 24) Rest special-casing
            if (nd == NeedDefOf.Rest)
                return pawn.RaceProps.needsRest ? "(allowed: needs rest)" : "(blocked: race does not need rest)";

            // If we reach here, vanilla would allow it (or we’re in the allowed path).
            return result ? "(allowed)" : "(blocked: unknown)"; // should rarely hit unknown
        }
    }

}



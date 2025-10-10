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
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class ZI_FlagGuestsOnSpawn
    {
        public static void Postfix(Pawn __instance, Map map, bool respawningAfterLoad)
        {
            var p = __instance;
            if (p == null || !p.Spawned || p.Dead) return;
            if (p.RaceProps?.Humanlike != true) return;

            // exclude colony-controlled pawns
            if (p.IsColonist || p.IsPrisonerOfColony || p.IsSlaveOfColony) return;

            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debug = settings.debugging && settings.debuggingCapacities;
            if (debug) Log.Message($"[ZI]ZI_FlagGuestsOnSpawn: Pawn {__instance.LabelShort} is considered for patch");

            // visiting/trading guests hosted by the player
            /*var g = p.guest;
            bool isVisitor = g != null &&
                             (g.GuestStatus == GuestStatus.Guest || g.HostFaction == Faction.OfPlayer);
            if (!isVisitor) return;
            */

            var flag = DefDatabase<HediffDef>.GetNamedSilentFail("ZI_GuestHygieneFlag");
            if (flag == null) return;

            if (!p.health.hediffSet.HasHediff(flag))
            {
                p.health.AddHediff(flag);
                if (debug) Log.Message($"[ZI]ZI_FlagGuestsOnSpawn: Pawn {__instance.LabelShort} was patched");
                if (debug) Log.Message(p.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("ZI_GuestHygieneFlag")).ToString());
            }
        }
    }
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.DeSpawn))]
    public static class ZI_UnflagGuestsOnDespawn
    {
        public static void Prefix(Pawn __instance)
        {
            var p = __instance;
            if (p == null) return;

            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debug = settings.debugging && settings.debuggingCapacities;
            if (debug) Log.Message($"[ZI]ZI_UnflagGuestsOnDespawn: Pawn {__instance.LabelShort} is prepared for patch removal");

            var flag = DefDatabase<HediffDef>.GetNamedSilentFail("ZI_GuestHygieneFlag");
            if (flag == null) return;

            var h = p.health?.hediffSet?.GetFirstHediffOfDef(flag);
            if (h != null) p.health.RemoveHediff(h);
            if (debug) Log.Message($"[ZI]ZI_UnflagGuestsOnDespawn: Pawn {__instance.LabelShort} patch removed");
        }
    }

    public static class Patch_ShouldHaveNeed
    {
        // Access private field "pawn" from Pawn_NeedsTracker
        private static readonly AccessTools.FieldRef<Pawn_NeedsTracker, Pawn> pawnRef =
            AccessTools.FieldRefAccess<Pawn_NeedsTracker, Pawn>("pawn");

        /// <summary>
        /// Intercept only the "Bladder" NeedDef for player-hosted guests (visitors/traders),
        /// and force true; otherwise let vanilla run.
        /// </summary>
        public static bool Prefix(Pawn_NeedsTracker __instance, NeedDef nd, ref bool __result)
        {
            // Only care about the DBH need named "Bladder"
            if (nd == null || nd.defName != "Bladder")
                return true; // run vanilla

            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debug = settings.debugging && settings.debuggingCapacities;



            var pawn = pawnRef(__instance);
            if (debug) Log.Message($"[ZI]Patch_ForceBladderForGuests: Pawn {pawn.LabelShort} processing");
            if (pawn == null || pawn.Dead)
                return true; // let vanilla decide / avoid NRE during load

            // We only extend to humanlike visitors; animals/raiders unchanged.
            if (pawn.RaceProps?.Humanlike != true)
                return true;

            // Skip pawns vanilla already handles (colonists/prisoners/slaves)
            if (pawn.IsColonist || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony)
                return true;

            // All vanilla preconditions (intelligence, dev stage, etc.) have already
            // been applied in the original method. We’re explicitly overriding the
            // remaining blockers (e.g. onlyIfCausedByHediff) for this one need.
            if (debug) Log.Message($"[ZI]Patch_ForceBladderForGuests: Pawn {pawn.LabelShort} force true");
            __result = true;
            return false; // skip original for Bladder on player-hosted guests
        }
    }

    [HarmonyPatch(typeof(Pawn_NeedsTracker), nameof(Pawn_NeedsTracker.AddOrRemoveNeedsAsAppropriate))]
    public static class Patch_AddOrRemoveNeedsAsAppropriate
    {
        static readonly MethodInfo MI_AddNeed =
            AccessTools.Method(typeof(Pawn_NeedsTracker), "AddNeed", new[] { typeof(NeedDef) });

        public static void Postfix(Pawn_NeedsTracker __instance)
        {
            // Never touch during load/scribe or worldgen/init — this caused your SaveableFromNode crash
            if (Scribe.mode != LoadSaveMode.Inactive) return;
            if (Current.ProgramState != ProgramState.Playing) return;

            // Resolve pawn safely
            var pawn = AccessTools.FieldRefAccess<Pawn_NeedsTracker, Pawn>("pawn")(__instance);
            if (pawn == null || pawn.Dead) return;

            
            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debug = settings.debugging && settings.debuggingCapacities;
            if (debug) Log.Message($"[ZI]Patch_AddOrRemoveNeedsAsAppropriate: Pawn {pawn.LabelShort} is considered for patch");

            if (!pawn.Spawned) return;

            // Already has it?
            if (pawn.needs?.TryGetNeed<Need_Bladder>() != null) return;

            // Only humanlikes; and don't touch colonists/prisoners/slaves
            if (pawn.RaceProps?.Humanlike != true) return;
            if (pawn.IsColonist || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony) return;
            if (debug) Log.Message($"[ZI]Patch_AddOrRemoveNeedsAsAppropriate: Pawn {pawn.LabelShort} hurdles done, going to final");
            // Add via vanilla path
            var def = DefDatabase<NeedDef>.GetNamedSilentFail("Bladder");
            if (def == null || MI_AddNeed == null) return;

            try
            {
                if (debug) Log.Message($"[ZI]Patch_AddOrRemoveNeedsAsAppropriate: Pawn {pawn.LabelShort} invoked add!");
                MI_AddNeed.Invoke(__instance, new object[] { def });
            }
            catch (Exception e)
            {
#if DEBUG
                Log.Warning($"[ZI] AddNeed(Bladder) failed: {e}");
#endif
            }
        }
    }
}



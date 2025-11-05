using DubsBadHygiene;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ZealousInnocence
{
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) })]
    public static class PawnGenerator_GeneratePawn_Patch
    {

        public static void AddGene(Pawn pawn, GeneDef geneDef)
        {
            // Add the gene to the pawn
            pawn.genes.AddGene(geneDef, false);
        }
        private static bool HasGeneWithExclusionTag(Pawn pawn, string exclusionTag)
        {
            foreach (var gene in pawn.genes.GenesListForReading)
            {
                if (gene.def.exclusionTags != null && gene.def.exclusionTags.Contains(exclusionTag))
                {
                    return true;
                }
            }
            return false;
        }
        private static bool HasGene(Pawn pawn, GeneDef geneDef)
        {
            return pawn.genes != null && pawn.genes.GetGene(geneDef) != null;
        }
        public static void Postfix(Pawn __result, PawnGenerationRequest request)
        {
            if (!__result.IsColonist) return;

            __result.refreshAllAgeStageCaches();

            Need bladder = __result.needs.TryGetNeed<Need_Bladder>();
            if (bladder != null)
            {
                bladder.CurLevel = Rand.RangeSeeded(0.7f, 1.0f, __result.HashOffsetTicks());
            }

            var diaperNeed = __result.needs.TryGetNeed<Need_Diaper>();
            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            if (diaperNeed != null)
            {

                diaperNeed.bedwettingSeed = __result.thingIDNumber;
                // This should not affect newborns, as we assume they could be born naturally and that would compound the genes
                if (settings.dynamicGenetics && request.AllowedDevelopmentalStages != DevelopmentalStage.Newborn)
                {
                    float rand;
                    if (!HasGeneWithExclusionTag(__result, "BladderSize"))
                    {
                        rand = Rand.ValueSeeded(diaperNeed.bedwettingSeed + 4213);
                        if (rand < 0.15f)
                        {
                            if (rand < 0.10f)
                            {
                                AddGene(__result, GeneDefOf.BladderSizeSmall);
                            }
                            else
                            {
                                AddGene(__result, GeneDefOf.BladderSizeBig);

                            }
                        }
                    }
                    if (!HasGeneWithExclusionTag(__result, "BladderBedwetting"))
                    {
                        rand = Rand.ValueSeeded(diaperNeed.bedwettingSeed + 8292);
                        // If they are prone to bedwetting by seed as adult, they carry the gene
                        if (Helper_Bedwetting.BedwettingAtAge(__result, 20))
                        {
                            if (rand < 0.3f)
                            {
                                AddGene(__result, GeneDefOf.BladderBedwettingAlways);
                            }
                            else if (rand < 0.8f)
                            {
                                AddGene(__result, GeneDefOf.BladderBedwettingLate);
                            }
                        }
                        else
                        {
                            if (rand < 0.1f)
                            {
                                AddGene(__result, GeneDefOf.BladderBedwettingEarly);
                            }
                        }
                    }

                    if (!HasGeneWithExclusionTag(__result, "BladderStrength"))
                    {
                        rand = Rand.ValueSeeded(diaperNeed.bedwettingSeed + 5811);
                        if (rand < 0.14f)
                        {
                            if (rand < 0.1f)
                            {
                                AddGene(__result, GeneDefOf.BladderStrenghWeak);
                            }
                            else
                            {
                                AddGene(__result, GeneDefOf.BladderStrenghStrong);
                            }
                        }
                    }
                }
            }

            if (HasGeneWithExclusionTag(__result, "BladderSize"))
            {
                var debugGenes = settings.debugging && settings.debuggingGenes;
                if (debugGenes)  Log.Message($"[ZI]Pawn {__result.LabelShort} has a bladder size gene");
                if (HasGene(__result, GeneDefOf.BladderSizeBig))
                {
                    if (debugGenes)  Log.Message($"[ZI]Pawn {__result.LabelShort} patching big");
                    Helper_Diaper.replaceBladderPart(__result, HediffDefOf.BigBladder);
                }
                if (HasGene(__result, GeneDefOf.BladderSizeSmall))
                {
                    if (debugGenes)  Log.Message($"[ZI]Pawn {__result.LabelShort} patching small");
                    Helper_Diaper.replaceBladderPart(__result, HediffDefOf.SmallBladder);
                }
            }


            if (__result.health.hediffSet.HasHediff(HediffDefOf.Incontinent)) return;


            if (Helper_Bedwetting.BedwettingAtAge(__result))
            {
                Helper_Bedwetting.AddHediff(__result);
            }

            var underwear = __result.apparel.WornApparel.FirstOrDefault(a => a.def.apparel.layers.Contains(ApparelLayerDefOf.Underwear));
            if (underwear != null)
            {
                if((Helper_Diaper.needsDiaper(__result) && Helper_Diaper.acceptsDiaper(__result)) || Helper_Diaper.prefersDiaper(__result))
                {
                    if ((!Helper_Diaper.isDiaper(underwear) || Helper_Diaper.isNightDiaper(underwear)) && Helper_Diaper.acceptsDiaper(__result))
                    {
                        if (settings.debugging)  Log.Message($"[ZI]PawnGenerator: Removing wrong underwear (needs/prefers diapers, is not, accepts) for {__result.LabelShort} of {underwear.LabelShort}");
                        __result.apparel.Remove(underwear);
                        underwear = null;
                    }

                }
                else if(Helper_Diaper.needsDiaperNight(__result) && Helper_Diaper.acceptsDiaperNight(__result))
                {
                    if (!Helper_Diaper.isNightDiaper(underwear))
                    {
                        if (settings.debugging)  Log.Message($"[ZI]PawnGenerator: Removing wrong underwear (needs pull-ups, is not, accepts) for {__result.LabelShort} of {underwear.LabelShort}");
                        __result.apparel.Remove(underwear);
                        underwear = null;
                    }
                }
                else
                {
                    if(Helper_Diaper.isNightDiaper(underwear) || Helper_Diaper.isDiaper(underwear)){
                        if (settings.debugging) Log.Message($"[ZI]PawnGenerator: Removing wrong underwear (needs no diapers, is not preferd) for {__result.LabelShort} of {underwear.LabelShort}");
                        __result.apparel.Remove(underwear);
                        underwear = null;
                    }
                }
            }
            if (underwear == null)
            {
                ThingDef underwearDef = ChooseUnderwearFor(__result);
                if (underwearDef != null)
                {
                    if (Helper_Diaper.isDisposable(underwear))
                    {
                        underwear = (Apparel)ThingMaker.MakeThing(underwearDef, null);
                    }
                    else
                    {
                        underwear = (Apparel)ThingMaker.MakeThing(underwearDef, ChooseMaterialFor(__result, underwearDef));
                    }
                    underwear.SetStyleDef(__result.StyleDef);
                    __result.apparel.Wear(underwear, true);
                    if(settings.debugging)  Log.Message($"[ZI]PawnGenerator: Fixed underwear issue for {__result.LabelShort} with {underwear.LabelShort}");
                }
                else
                {
                    if (settings.debugging)  Log.Message($"[ZI]PawnGenerator: Could not fix underwear issue for {__result.LabelShort}");
                }
            }
            if(underwear != null && Helper_Diaper.isDisposable(underwear))
            {
                var inv = __result?.inventory?.innerContainer;
                if (inv == null)
                {
                    Log.Message($"[ZI]PawnGenerator: No inventory for {__result.LabelShort} with {underwear.LabelShort}");
                    return;
                }
                int have = inv.Count(t => t is Apparel_Disposable_Diaper && t.def == underwear.def);
                if(have < 2)
                {
                    var toAdd = (Apparel)ThingMaker.MakeThing(underwear.def,null);
                    
                    if (toAdd != null)
                    {
                        toAdd.stackCount = 2 - have;
                        if (settings.debugging) Log.Message($"[ZI]PawnGenerator: Adding spare disposables for {__result.LabelShort} with {underwear.LabelShort}");
                        inv.TryAdd(toAdd,2,true);
                    }
                    
                }
            }

        }
        private static ThingDef ChooseMaterialFor(Pawn pawn, ThingDef thing)
        {
            if (pawn.Faction == null || pawn.Faction.def.techLevel == TechLevel.Medieval || pawn.Faction.def.techLevel == TechLevel.Neolithic)
            {
                return RimWorld.ThingDefOf.Leather_Plain;
            }
            if(pawn.Faction.def.techLevel == TechLevel.Spacer) return DefDatabase<ThingDef>.GetNamed("Synthread");
            if (pawn.Faction.def.techLevel == TechLevel.Industrial) return DefDatabase<ThingDef>.GetNamed("Cloth");
            return DefDatabase<ThingDef>.GetNamed("Hyperweave");
        }
        private static ThingDef ChooseUnderwearFor(Pawn pawn)
        {
            if (pawn.ageTracker.AgeBiologicalYears < 3) return null;

            var lowTech = pawn.Faction == null || pawn.Faction.def.techLevel == TechLevel.Medieval || pawn.Faction.def.techLevel == TechLevel.Neolithic;

            if ((Helper_Diaper.needsDiaper(pawn) || Helper_Diaper.prefersDiaper(pawn)) && Helper_Diaper.acceptsDiaper(pawn))
            {
                if(lowTech) return ThingDefOf.Apparel_Diaper;
                if(Rand.RangeSeeded(0.0f, 1.0f, pawn.HashOffsetTicks() + 479) > 0.25f)
                {
                    return ThingDefOf.Apparel_Diaper_Disposable;
                }
                return ThingDefOf.Apparel_Diaper;
            }
            else if (Helper_Diaper.needsDiaperNight(pawn) && Helper_Diaper.acceptsDiaperNight(pawn))
            {
                if (lowTech) return ThingDefOf.Apparel_Diaper;
                return ThingDefOf.Apparel_Diaper_Night;
            }
            else
            {
                if (lowTech)
                {
                    return ThingDefOf.Apparel_Underwear_Loincloth;
                }
                else
                {
                    if(!pawn.ageTracker.Adult)
                    {
                        return ThingDefOf.Apparel_Underwear_Kids;
                    }
                    if (pawn.ageTracker.Adult)
                    {
                        if (pawn.gender == Gender.Female)
                        {
                            return ThingDefOf.Apparel_Underwear_Panties;
                        }
                        else
                        {
                            return ThingDefOf.Apparel_Underwear_Boxers;
                        }
                    }
                }
            }
            return null;
        }
    }
}

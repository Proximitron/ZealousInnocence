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

            var diaperNeed = __result.needs.TryGetNeed<Need_Diaper>();
            var def = HediffDef.Named("BedWetting");
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
                        if (rand < 0.25f)
                        {
                            if (rand < 0.15f)
                            {
                                /*if (rand < 0.02f)
                                {
                                    AddGene(__result, GeneDefOf.BladderSizeTiny);
                                }
                                else
                                {
                                    AddGene(__result, GeneDefOf.BladderSizeSmall);
                                }*/
                                AddGene(__result, GeneDefOf.BladderSizeSmall);
                            }
                            else
                            {
                                /*if (rand < 0.12f)
                                {
                                    AddGene(__result, GeneDefOf.BladderSizeHuge);
                                }
                                else
                                {
                                    
                                }*/
                                AddGene(__result, GeneDefOf.BladderSizeBig);

                            }
                        }
                    }
                    if (!HasGeneWithExclusionTag(__result, "BladderBedwetting"))
                    {
                        rand = Rand.ValueSeeded(diaperNeed.bedwettingSeed + 8292);
                        // If they are prone to bedwetting by seed as adult, they carry the gene
                        if (BedWetting_Helper.BedwettingAtAge(__result, 20))
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
                            if (rand < 0.2f)
                            {
                                AddGene(__result, GeneDefOf.BladderBedwettingEarly);
                            }
                        }
                    }

                    if (!HasGeneWithExclusionTag(__result, "BladderStrength"))
                    {
                        rand = Rand.ValueSeeded(diaperNeed.bedwettingSeed + 5811);
                        if (rand < 0.2f)
                        {
                            if (rand < 0.1f)
                            {
                                AddGene(__result, GeneDefOf.BladderStrenghWeak);
                            }
                            else
                            {
                                if (rand < 0.03f)
                                {
                                    AddGene(__result, GeneDefOf.BladderStrenghStrong);
                                }

                            }
                        }
                    }
                }
            }

            if (HasGeneWithExclusionTag(__result, "BladderSize"))
            {
                var debugGenes = settings.debugging && settings.debuggingGenes;
                if (debugGenes) Log.Message($"Pawn {__result.LabelShort} has a bladder size gene");
                if (HasGene(__result, GeneDefOf.BladderSizeBig))
                {
                    if (debugGenes) Log.Message($"Pawn {__result.LabelShort} patching big");
                    DiaperHelper.replaceBladderPart(__result, HediffDefOf.BigBladder);
                }
                if (HasGene(__result, GeneDefOf.BladderSizeSmall))
                {
                    if (debugGenes) Log.Message($"Pawn {__result.LabelShort} patching small");
                    DiaperHelper.replaceBladderPart(__result, HediffDefOf.SmallBladder);
                }
            }


            if (__result.health.hediffSet.HasHediff(HediffDef.Named("Incontinent"))) return;


            if (BedWetting_Helper.BedwettingAtAge(__result, __result.ageTracker.AgeBiologicalYears))
            {
                __result.health.AddHediff(def);
                var hediff = __result.health.hediffSet.GetFirstHediffOfDef(def);
                hediff.Severity = BedWetting_Helper.BedwettingSeverity(__result);
            }

            var underwear = __result.apparel.WornApparel.FirstOrDefault(a => a.def.apparel.layers.Contains(ApparelLayerDefOf.Underwear));
            if (underwear != null)
            {
                if(DiaperHelper.needsDiaper(__result))
                {
                    if (!DiaperHelper.isDiaper(underwear) && DiaperHelper.acceptsDiaper(__result))
                    {
                        if (settings.debugging) Log.Message($"PawnGenerator: Removing wrong underwear for {__result.LabelShort} of {underwear.LabelShort}");
                        __result.apparel.Remove(underwear);
                        underwear = null;
                    }

                }
                else if(DiaperHelper.needsDiaperNight(__result))
                {
                    if (!DiaperHelper.isNightDiaper(underwear) && DiaperHelper.acceptsDiaperNight(__result))
                    {
                        if (settings.debugging) Log.Message($"PawnGenerator: Removing wrong underwear for {__result.LabelShort} of {underwear.LabelShort}");
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
                    underwear = (Apparel)ThingMaker.MakeThing(underwearDef, ChooseMaterialFor(__result, underwearDef));
                    underwear.SetStyleDef(__result.StyleDef);
                    __result.apparel.Wear(underwear, true);
                    if(settings.debugging) Log.Message($"PawnGenerator: Fixed underwear issue for {__result.LabelShort} with {underwear.LabelShort}");
                }
                else
                {
                    if (settings.debugging) Log.Message($"PawnGenerator: Could not fix underwear issue for {__result.LabelShort}");
                }
            }

        }
        private static ThingDef ChooseMaterialFor(Pawn pawn, ThingDef thing)
        {
            if (pawn.Faction == null || pawn.Faction.def.techLevel == TechLevel.Medieval || pawn.Faction.def.techLevel == TechLevel.Neolithic)
            {
                return ThingDefOf.Leather_Plain;
            }
            if(pawn.Faction.def.techLevel == TechLevel.Spacer) return DefDatabase<ThingDef>.GetNamed("Synthread");
            if (pawn.Faction.def.techLevel == TechLevel.Industrial) return DefDatabase<ThingDef>.GetNamed("Cloth");
            return DefDatabase<ThingDef>.GetNamed("Hyperweave");
        }
        private static ThingDef ChooseUnderwearFor(Pawn pawn)
        {
            if (DiaperHelper.needsDiaper(pawn) && DiaperHelper.acceptsDiaper(pawn))
            {
                return DefDatabase<ThingDef>.GetNamed("Apparel_Diaper");
            }
            else if (DiaperHelper.needsDiaperNight(pawn) && DiaperHelper.acceptsDiaperNight(pawn))
            {
                return DefDatabase<ThingDef>.GetNamed("Apparel_Diaper_Night");
            }
            else
            {
                if (pawn.Faction == null || pawn.Faction.def.techLevel == TechLevel.Medieval || pawn.Faction.def.techLevel == TechLevel.Neolithic)
                {
                    return DefDatabase<ThingDef>.GetNamed("Apparel_Underwear_Loincloth");
                }
                else
                {
                    if(!pawn.ageTracker.Adult && pawn.ageTracker.AgeBiologicalYears >= 2)
                    {
                        return DefDatabase<ThingDef>.GetNamed("Apparel_Underwear_Kids");
                    }
                    if (pawn.ageTracker.Adult)
                    {
                        if (pawn.gender == Gender.Female)
                        {
                            return DefDatabase<ThingDef>.GetNamed("Apparel_Underwear_Panties");
                        }
                        else
                        {
                            return DefDatabase<ThingDef>.GetNamed("Apparel_Underwear_Boxers");
                        }
                    }
                }
            }
            return null;
        }
    }
}

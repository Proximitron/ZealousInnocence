using DubsBadHygiene;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
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
                        if (rand < 0.8f)
                        {
                            if (rand < 0.4f)
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
                        if(BedWetting_Helper.BedwettingAtAge(__result, 20)){
                            if (rand < 0.3f)
                            {
                                AddGene(__result, GeneDefOf.BladderBedwettingAlways);
                            }
                            else if(rand < 0.8f)
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

            if(HasGeneWithExclusionTag(__result, "BladderSize"))
            {
                if (settings.debugging) Log.Message($"Pawn {__result.LabelShort} has a bladder size gene");
                if (HasGene(__result, GeneDefOf.BladderSizeBig))
                {
                    if (settings.debugging) Log.Message($"Pawn {__result.LabelShort} patching big");
                    DiaperHelper.replaceBladderPart(__result, HediffDefOf.BigBladder);
                }
                if (HasGene(__result, GeneDefOf.BladderSizeSmall))
                {
                    if (settings.debugging) Log.Message($"Pawn {__result.LabelShort} patching small");
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

        }
    }
}

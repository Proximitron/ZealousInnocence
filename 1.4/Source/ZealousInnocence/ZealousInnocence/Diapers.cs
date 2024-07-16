using DubsBadHygiene;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.Sound;
using Verse;
using static HarmonyLib.Code;

namespace ZealousInnocence
{
    public class Need_Diaper : Need
    {
        public bool isPeeing;

        public void StartSound(bool pee = true)
        {
            if (pee)
            {
                SoundStarter.PlayOneShotOnCamera(DiaperChangie.Pee, pawn.Map);
            }
            else
            {
                SoundStarter.PlayOneShotOnCamera(DiaperChangie.Poop, pawn.Map);
            }
        }

        public override void SetInitialLevel()
        {
            base.CurLevelPercentage = 1f;
        }

        public bool hasDiaper()
        {
            return DiaperHelper.getDiaper(pawn) != null;
        }

        public Need_Diaper(Pawn pawn) : base(pawn)
        {
            threshPercents = new List<float>();
            threshPercents.Add(0.8f);
            threshPercents.Add(0.5f);
            threshPercents.Add(0.2f);
        }


        public DiaperSituationCategory CurCategory
        {
            get
            {
                if (CurLevel > 0.8f)
                {
                    return DiaperSituationCategory.Clean;
                }
                if (CurLevel > 0.2f && CurLevel < 0.5f)
                {
                    return DiaperSituationCategory.Spent;
                }
                if (CurLevel < 0.2f)
                {
                    return DiaperSituationCategory.Trashed;
                }
                return DiaperSituationCategory.Used;
            }
        }

        public override float CurLevel
        {
            get => base.CurLevel;
            set
            {
                DiaperSituationCategory curCategory = CurCategory;
                base.CurLevel = value;
                if (CurCategory != curCategory)
                {
                    this.CategoryChanged();
                }
            }
        }

        public override bool ShowOnNeedList
        {
            get
            {
                return hasDiaper();
            }
        }

        public void crapPants()
        {
            Log.Message("debug: doing (crapPants) for " + pawn.Name);
            Need bladder = pawn.needs.TryGetNeed<Need_Bladder>();
            Need_Hygiene need_Hygiene = this.pawn.needs.TryGetNeed<Need_Hygiene>();
            if (need_Hygiene != null)
            {
                need_Hygiene.CurLevel = 0f;
            }
            if (bladder != null)
            {
                bladder.CurLevel = 1f;
            }
            if (pawn.InBed())
            {
                var bed = pawn.CurrentBed();
                bed.HitPoints -= Math.Min(bed.HitPoints-1,(bed.MaxHitPoints / 3));
                foreach (var curr in pawn.CurrentBed().CurOccupants)
                {
                    if (curr != pawn)
                    {
                        curr.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("PeedOnMe"), pawn, null);
                        curr.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("SoakingWet"), pawn, null);
                    }
                }
                pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("WetBed"), pawn, null);
                pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("SoakingWet"), pawn, null);
            }
            if (pawn.needs.mood != null)
            {

                if (pawn.story.traits.HasTrait(TraitDef.Named("Potty_Rebel")))
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("SoiledSelfGood"), pawn, null);
                }
                else
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("SoiledSelf"), pawn, null);
                }
            }
            /*if (Rand.Chance(0.5f))
            {
                FilthMaker.TryMakeFilth(this.pawn.Position, this.pawn.Map, ThingDef.Named("FilthFaeces"), 1, 0);
            }
            else
            {*/
            FilthMaker.TryMakeFilth(this.pawn.Position, this.pawn.Map, ThingDef.Named("FilthUrine"), 1, 0);
            //}
        }

        public void startAccident(bool pee = true)
        {
            StartSound(pee);
            isPeeing = true;
            Log.Message("debug: starting accident '" + (pee ? "pee" : "poop") + "' for " + pawn.Name);
            if (DiaperHelper.getDiaper(pawn) != null)
            {
                var liked = DiaperHelper.getDiaperPreference(pawn);
                switch (liked)
                {
                    case DiaperLikeCategory.Neutral:
                        pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("DiaperPeedNeutral"), pawn, null);
                        break;
                    case DiaperLikeCategory.Liked:
                        pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("DiaperPeedGood"), pawn, null);
                        break;
                    case DiaperLikeCategory.Disliked:
                        pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("DiaperPeedHated"), pawn, null);
                        break;
                    default:
                        break;
                }
            }
        }

        // Triggers every 150 ticks! (2.5 seconds)
        // 2500 ticks in an ingame hour, 60000 ticks in a day (day is 1000 realtime seconds)
        // That means there are 400 need intervals in a day, meaning a chance of 0.0025 happens once a day
        public override void NeedInterval()
        {
            if (pawn.Dead || !pawn.RaceProps.Humanlike || !pawn.RaceProps.IsFlesh) return;
            Need bladder = pawn.needs.TryGetNeed<Need_Bladder>();
            if (bladder == null) return;

            var currDiapie = DiaperHelper.getDiaper(pawn);
            if (currDiapie != null)
            {
                CurLevel = (float)currDiapie.HitPoints / (float)currDiapie.MaxHitPoints;
            }
            var currControlLevel = DiaperHelper.getBladderControlLevel(pawn);
            List<Hediff> health = pawn.health.hediffSet.hediffs;

            if (!isPeeing && bladder.CurLevel <= 0f)
            {
                startAccident();
            }


            // the current control level of 0.0 to 1.0 minus the inverted bladder value from 0.0 to 1.0 give a value of -1.0 to 1.0.
            // A negativ value means there is a chance of bladder failure
            float remainingControl = (currControlLevel - (1.0f - bladder.CurLevel));

            if (!isPeeing && remainingControl < 0f)
            {

                //float failChance = ((remainingControl * -1.0f) * 0.08f)-0.01f;
                
                //
                float failChance = ((remainingControl * -1.0f) * 0.08f) - 0.01f;
                Log.Message("debug: bladder control of " + remainingControl + " lower then 0, calculated fail of " + failChance.ToString() + " for " + pawn.Name);

                bool trigger = Rand.ChanceSeeded(failChance, pawn.HashOffsetTicks());
                if (trigger)
                {
                    startAccident();
                    Log.Message("doing fail of bladder control at fail chance of " + failChance.ToString() + " for " + pawn.Name);
                }
            }

            if (!isPeeing && bladder.CurLevel <= 0.30f)
            {
                if (pawn.Awake())
                {
                    if (currDiapie == null)
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("HadToHoldIt"), null, null);
                    }
                    else
                    {
                        if (pawn.story.traits.HasTrait(TraitDef.Named("Potty_Rebel")))
                        {
                            startAccident();
                            Log.Message("doing trait (Potty_Rebel) for " + pawn.Name);
                        }
                        else if (currDiapie != null && DiaperHelper.getDiaperPreference(pawn) == DiaperLikeCategory.Liked)
                        {
                            startAccident();
                            Log.Message("doing (peeing diaper by preference) for " + pawn.Name);
                        }
                    }
                }

            }
            if (isPeeing && currDiapie != null)
            {
                Log.Message("peeing in diaper: " + DiaperHelper.getBladderControlLevel(pawn).ToString() + " for " + pawn.LabelShort);

                var peeAmount = Math.Min(1.0f - bladder.CurLevel, 0.15f);

                bladder.CurLevel += peeAmount;

                float absorbency = pawn.GetStatValue(StatDefOf.DiaperAbsorbency) / 3; // we always calculate at 1/3 the value. 3 liter capacity is the base for this calculation
                //float totalDiaperAbsorbency = currDiapie.GetStatValue(StatDefOf.Absorbency);
                //Log.Message("absorbency: " + absorbency);
                // Log.Message("absorbency2: " + currDiapie.GetStatValue(StatDefOf.Absorbency));
                 
                float hitPointDamage = peeAmount / (0.05f * absorbency);

                float halfHitpoints = hitPointDamage - (float)Math.Floor(hitPointDamage);

                int realPointDamage = (int)Math.Floor(hitPointDamage);
                //Log.Message("halfhitpoints: " + halfHitpoints + " from " + realPointDamage + " at pee amount " + peeAmount + " and bladder " + bladder.CurLevel + " at absorbency " + absorbency);
                if (halfHitpoints > 0.0f && Rand.ChanceSeeded(halfHitpoints, pawn.HashOffsetTicks() + 4))
                {
                    //Log.Message("chance hit at: " + halfHitpoints + " doing ceiling of " + realPointDamage);
                    realPointDamage = (int)Math.Ceiling(hitPointDamage);
                }

                currDiapie.HitPoints -= realPointDamage;

                if (bladder.CurLevel >= 0.95f)
                {
                    isPeeing = false;
                }
            }
            else if (isPeeing)
            {
                crapPants();
                isPeeing = false;
            }
            if (currDiapie != null && !currDiapie.DestroyedOrNull())
            {
                CurLevel = (float)currDiapie.HitPoints / (float)currDiapie.MaxHitPoints;

                if(currDiapie.HitPoints < currDiapie.MaxHitPoints / 2)
                {
                    // 0.5 - (0.0 - 0.5) makes the total chance higher, the higher the amount of missing hitpoints
                    float chance = 0.5f - (currDiapie.HitPoints / currDiapie.MaxHitPoints);
                    chance = 0.001f * chance * Find.Storyteller.difficulty.playerPawnInfectionChanceFactor;

                    if (Rand.ChanceSeeded(chance, pawn.HashOffsetTicks()+8))
                    {
                        if (!pawn.health.hediffSet.HasHediff(HediffDefOf.DiaperRash))
                        {
                            Log.Message("doing rash incident at fail chance " + chance.ToString() + " for " + pawn.Name);
                            BodyPartRecord part = pawn.RaceProps.body.AllParts.FirstOrFallback((BodyPartRecord p) => p.def == RimWorld.BodyPartDefOf.Torso);
                            var hediff = pawn.health.AddHediff(HediffDefOf.DiaperRash, part);
                            if (PawnUtility.ShouldSendNotificationAbout(pawn))
                            {
                                Messages.Message("LetterDiaperRash".Translate(pawn.LabelShort, hediff.Label, pawn.Named("PAWN")), pawn, MessageTypeDefOf.NegativeHealthEvent);
                            }
                        }
                        else
                        {
                            Log.Message("skipping rash incident at fail chance " + chance.ToString() + " for " + pawn.Name);
                        }
                        
                    }
                }
            }
            else
            {
                SetInitialLevel();
            }


        }
        void CategoryChanged()
        {
        }
    }
    public class Recipe_PutInDiaper : RecipeWorker
    {
        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {
            // we don't do that, we work on that ourselfs
        }
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (IsViolationOnPawn(pawn, part, Faction.OfPlayer))
            {
                ReportViolation(pawn, billDoer, pawn.HomeFaction, -5);
            }

            Apparel diapie = null;
            foreach (var thing in ingredients)
            {
                if (thing.def.thingCategories.Contains(ThingCategoryDefOf.Diapers))
                {
                    diapie = (Apparel)thing;
                    break;
                }
            }
            //var thingWithComps2 = (Apparel)diapie;
            //thingWithComps2.DeSpawn();

            pawn.apparel.Wear(diapie, true);
            if (diapie.def.soundInteract != null)
            {
                diapie.def.soundInteract.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            }

            foreach (var curr in pawn.health.hediffSet.hediffs)
            {
                if (curr.def == RimWorld.HediffDefOf.Anesthetic)
                {
                    pawn.health.RemoveHediff(curr);
                    break;
                }
            }
        }
    }
    public static class DiaperHelper
    {
        public static bool needsDiaper(Pawn pawn)
        {
            List<Hediff> health = pawn?.health?.hediffSet?.hediffs;
            if (health == null) return false;
            for (int i = 0; i < health.Count; i++)
            {

                switch (health[i].def.defName)
                {
                    case "BedWetting":
                        return true;
                    default:
                        continue;
                }

            }
            return pawn.health.capacities.GetLevel(PawnCapacityDefOf.BladderControl) <= 0.5;
        }

        public static Apparel getDiaper(Pawn p)
        {
            List<Apparel> wornApparel = p?.apparel?.WornApparel;
            if (wornApparel != null)
            {
                for (int i = 0; i < wornApparel.Count; i++)
                {
                    if (wornApparel[i].def.thingCategories.Contains(ThingCategoryDefOf.Diapers))
                    {
                        return wornApparel[i];
                    }
                }
            }
            return null;
        }
        public static BodyPartRecord getBladder(Pawn pawn)
        {
            foreach (BodyPartRecord notMissingPart in pawn.health.hediffSet.GetNotMissingParts())
            {
                if (notMissingPart.def.tags.Contains(BodyPartTagDefOf.BladderControlSource))
                {
                    return notMissingPart;
                }
            }

            return null;
        }
        public static float getBladderControlLevel(Pawn pawn)
        {
            if (!pawn.Awake())
            {
                List<Hediff> health = pawn.health.hediffSet.hediffs;
                for (int i = 0; i < health.Count; i++)
                {

                    switch (health[i].def.defName)
                    {
                        case "BedWetting":
                            return 0f;
                        default:
                            continue;
                    }

                }
            }
            return pawn.health.capacities.GetLevel(PawnCapacityDefOf.BladderControl);
        }

        public static DiaperLikeCategory getDiaperPreference(Pawn pawn)
        {
            TraitSet traits = pawn?.story?.traits;
            if (traits == null)
            {
                return DiaperLikeCategory.Neutral;
            }

            if (pawn.story.traits.HasTrait(TraitDefOf.Potty_Rebel))
            {
                return DiaperLikeCategory.Liked;
            }
            if (pawn.story.traits.HasTrait(TraitDefOf.Big_Boy))
            {
                return DiaperLikeCategory.Disliked;
            }

            if (ModsConfig.IdeologyActive)
            {
                Ideo ideo = pawn.Ideo;
                if (ideo != null)
                {

                    if (ideo.HasPrecept(PreceptDefOf.Diapers_Loved))
                    {
                        return DiaperLikeCategory.Liked;
                    }
                    else if (ideo.HasPrecept(PreceptDefOf.Diapers_Hated))
                    {
                        return DiaperLikeCategory.Disliked;
                    }
                }
            }
            return DiaperLikeCategory.Neutral;
        }
    }
    public class ThoughtWorker_Stink : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.Spawned)
            {
                float stinkLevel = 0.0f;
                foreach (Pawn pawn in p.Map.mapPawns.AllPawnsSpawned)
                {
                    if (pawn != p && pawn.needs.TryGetNeed<Need_Diaper>() != null)
                    {
                        if (IntVec3Utility.DistanceTo(p.Position, pawn.Position) <= 6 && pawn.needs.TryGetNeed<Need_Diaper>().CurLevel <= 0.8f)
                        {
                            stinkLevel += (pawn.needs.TryGetNeed<Need_Diaper>().CurLevel - 0.2f);
                        }
                    }
                }

                int targetTrait = p.story.traits.HasTrait(TraitDefOf.Potty_Rebel) ? 4 : 0;

                if (stinkLevel <= 0.0f)
                {
                    return ThoughtState.Inactive;
                }
                else if (stinkLevel > 2.5f)
                {
                    return ThoughtState.ActiveAtStage(targetTrait + 3);
                }
                else if (stinkLevel > 1.2f)
                {
                    return ThoughtState.ActiveAtStage(targetTrait + 2);
                }
                else
                {
                    return ThoughtState.ActiveAtStage(targetTrait + 1);
                }
            }
            else
            {
                return ThoughtState.Inactive;
            }
        }
    }
}

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
using Verse.AI;
using System.Drawing;
using System.Security.Cryptography;
using UnityEngine;

namespace ZealousInnocence
{
    public enum BedwettingDiaperThought : int
    {
        // Default behaviour if nothing else applies
        Wet_Diaper_Bed_Default,
        Wet_Diaper_Bed_Bedwetter,

        // Thoughts if diapers loved
        Wet_Diaper_Bed_Loved,
        Wet_Diaper_Bed_Bedwetter_Loved,

        // Thoughts of non-adult (child)
        Wet_Diaper_Bed_Non_Adult,
    }
    public enum WettingBedThought : int
    {
        // Default behaviour if nothing else applies
        Wet_Bed_Default,

        // Thoughts if bedwetter or low control be default
        Wet_Bed_Bedwetter,

        // Thoughts of non-adult (child)
        Wet_Bed_Non_Adult,
    }
    public enum WettingDiaperThought : int
    {
        // Default behaviour if nothing else applies
        Wet_Diaper_Default,

        // Thoughts if diapers loved
        Wet_Diaper_Loved,

        // Thoughts if diapers hated
        Wet_Diaper_Hated,

        // Thoughts of non-adult (child)
        Wet_Diaper_Non_Adult,
    }

    public enum WettingPantsThought : int
    {
        // Default behaviour if nothing else applies
        Wet_Pants_Default,

        // Thoughts if diapers loved
        Wet_Pants_Diaper_Loved,

        // Thoughts if diapers hated
        Wet_Pants_Diaper_Hated,

        // Thoughts of non-adult (child)
        Wet_Pants_Non_Adult,
    }

    public class Need_Diaper : Need
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.isHavingAccident, "isHavingAccident", false);
            Scribe_Values.Look(ref this.peeing, "peeing", false);
            Scribe_Values.Look(ref this.failureSeed, "failureSeed", 0);
            Scribe_Values.Look(ref this.bedwettingSeed, "bedwettingSeed", 0);
            Scribe_Values.Look(ref this.sleepTrack, "sleepTrack", 0);
        }
        private bool isHavingAccident;
        public bool IsHavingAccident { get => isHavingAccident; }

        private bool peeing;
        public bool IsPeeing { get => peeing; }

        private int failureSeed = 0;

        public int bedwettingSeed = 0;

        private int sleepTrack = 0;
        public int FailureSeed
        {
            get
            {
                if (failureSeed == 0)
                {
                    failureSeed = pawn.HashOffsetTicks() + pawn.ageTracker.BirthDayOfYear + pawn.ageTracker.AgeChronologicalYears + pawn.ageTracker.BirthYear;
                }
                return failureSeed;
            }
            set => failureSeed = value;
        }


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
        public bool hasUnderwearOrDiaper()
        {
            return DiaperHelper.getUnderwearOrDiaper(pawn) != null;
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
                return hasUnderwearOrDiaper();
            }
        }

        public void crapPants()
        {
            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debugging = settings.debugging && settings.debuggingCapacities;
            if(debugging) Log.Message("debug: doing (crapPants) for " + pawn.Name);
            
            Need bladder = pawn.needs.TryGetNeed<Need_Bladder>();
            var peeAmountPercentile = Math.Min(1.0f - bladder.CurLevel, 0.10f);
            var fluidAmount = DiaperHelper.fluidAmount(pawn, peeAmountPercentile);
            int filth = 1;
            Need_Hygiene need_Hygiene = this.pawn.needs.TryGetNeed<Need_Hygiene>();
            if (need_Hygiene != null)
            {
                need_Hygiene.CurLevel -= Math.Min(need_Hygiene.CurLevel, fluidAmount);
            }
            if (bladder != null)
            {
                bladder.CurLevel += peeAmountPercentile;
            }
            List<Apparel> legApparel = pawn.apparel.WornApparel.Where(apparel => apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs)).ToList();
            int splitOfDamage = legApparel.Count;
            int damageBase = 25; // damage for a full bladder in hp
            for (int i = legApparel.Count - 1; i >= 0; i--)
            {
                Apparel apparel = legApparel[i];
                float damage = damageBase * (peeAmountPercentile / splitOfDamage);
                int finDamage = chancedDamage(damage);
                if (debugging) Log.Message($"crapPants: causing {finDamage} damage to {apparel.LabelShort} for {pawn.LabelShort} " + pawn.Name);
                apparel.HitPoints -= Math.Min(apparel.HitPoints, finDamage);
                pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("SoakingWet"), pawn, null);

                // Check if the apparel gets destroyed during this process
                if (apparel.HitPoints < 1)
                {
                    apparel.Notify_LordDestroyed();
                    apparel.Destroy(DestroyMode.Vanish);
                }
            }

            if (pawn.InBed())
            {
                var bed = pawn.CurrentBed();
                float damage = ((float)bed.MaxHitPoints / 2f) * fluidAmount;
                bed.HitPoints -= Math.Min(bed.HitPoints-1, chancedDamage(damage));
                foreach (var curr in pawn.CurrentBed().CurOccupants)
                {
                    if (curr != pawn)
                    {
                        curr.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("PeedOnMe"), pawn, null);
                        curr.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("SoakingWet"), pawn, null);
                    }
                }
                if (!pawn.ageTracker.Adult) DiaperHelper.getMemory(pawn, WettingBedThought.Wet_Bed_Non_Adult);
                else if (DiaperHelper.needsDiaperNight(pawn)) DiaperHelper.getMemory(pawn, WettingBedThought.Wet_Bed_Bedwetter);
                else DiaperHelper.getMemory(pawn, WettingBedThought.Wet_Bed_Default);
                pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("SoakingWet"), pawn, null);
            }

            if (peeing) FilthMaker.TryMakeFilth(this.pawn.Position, this.pawn.Map, ThingDef.Named("FilthUrine"), filth, FilthSourceFlags.Pawn);
            else FilthMaker.TryMakeFilth(this.pawn.Position, this.pawn.Map, ThingDef.Named("FilthFaeces"), filth, FilthSourceFlags.Pawn);
        }

        public void startAccident(bool pee = true)
        {
            StartSound(pee);
            FailureSeed = 0;
            isHavingAccident = true;
            peeing = pee;
            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debugging = settings.debugging && settings.debuggingCapacities;
            if(debugging) Log.Message("debug: starting accident '" + (peeing ? "pee" : "poop") + "' for " + pawn.Name);

            var liked = DiaperHelper.getDiaperPreference(pawn);
            if (DiaperHelper.getDiaper(pawn) != null)
            {
                
                switch (liked)
                {
                    case DiaperLikeCategory.NonAdult:
                        if (pawn.Awake()) DiaperHelper.getMemory(pawn,WettingDiaperThought.Wet_Diaper_Non_Adult);
                        else DiaperHelper.getMemory(pawn, BedwettingDiaperThought.Wet_Diaper_Bed_Non_Adult);
                        break;
                    case DiaperLikeCategory.Neutral:
                        if (!pawn.Awake())
                        {
                            if (DiaperHelper.needsDiaperNight(pawn)) DiaperHelper.getMemory(pawn, BedwettingDiaperThought.Wet_Diaper_Bed_Bedwetter);
                            else DiaperHelper.getMemory(pawn, BedwettingDiaperThought.Wet_Diaper_Bed_Default);
                        }
                        else DiaperHelper.getMemory(pawn, WettingDiaperThought.Wet_Diaper_Default);
                        break;
                    case DiaperLikeCategory.Liked:
                        if (!pawn.Awake())
                        {
                            if (DiaperHelper.needsDiaperNight(pawn)) DiaperHelper.getMemory(pawn, BedwettingDiaperThought.Wet_Diaper_Bed_Bedwetter_Loved);
                            else DiaperHelper.getMemory(pawn, BedwettingDiaperThought.Wet_Diaper_Bed_Loved);
                        }
                        else DiaperHelper.getMemory(pawn, WettingDiaperThought.Wet_Diaper_Loved);
                        break;
                    case DiaperLikeCategory.Disliked:
                        DiaperHelper.getMemory(pawn, WettingDiaperThought.Wet_Diaper_Hated);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                switch (liked)
                {

                    case DiaperLikeCategory.NonAdult:
                        DiaperHelper.getMemory(pawn, WettingPantsThought.Wet_Pants_Non_Adult);
                        break;
                    case DiaperLikeCategory.Neutral:
                        DiaperHelper.getMemory(pawn, WettingPantsThought.Wet_Pants_Default);
                        break;
                    case DiaperLikeCategory.Liked:
                        DiaperHelper.getMemory(pawn, WettingPantsThought.Wet_Pants_Diaper_Loved);
                        break;
                    case DiaperLikeCategory.Disliked:
                        DiaperHelper.getMemory(pawn, WettingPantsThought.Wet_Pants_Diaper_Hated);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        // Triggers every 150 ticks! (2.5 seconds)
        // 2500 ticks in an ingame hour, 60000 ticks in a day (day is 1000 realtime seconds)
        // That means there are 400 need intervals in a day, meaning a chance of 0.0025 happens once a day
        public override void NeedInterval()
        {
            if (pawn.Dead || !pawn.RaceProps.Humanlike || !pawn.RaceProps.IsFlesh) return;
            pawn.health.capacities.Notify_CapacityLevelsDirty(); // Yes, nessesary. The caching doesn't keep track of changes like sleeping and things that not cause hediffs

            Need bladder = pawn.needs.TryGetNeed<Need_Bladder>();
            if (bladder == null) return;

            var currProtection = DiaperHelper.getUnderwearOrDiaper(pawn);
            if (currProtection != null && DiaperHelper.isDiaper(currProtection))
            {
                CurLevel = (float)currProtection.HitPoints / (float)currProtection.MaxHitPoints;
            }

            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debugging = settings.debugging && settings.debuggingCapacities;
            var debugBedwetting = settings.debugging && settings.debuggingBedwetting;
            if (pawn.Awake())
            {
                if(sleepTrack > 0)
                {
                    if (debugging) Log.Message($"Ending sleep at {bladder.CurLevel} failpoint {DiaperHelper.getBladderControlFailPoint(pawn)} for {pawn.Name} after {sleepTrack} ticks");
                }
                sleepTrack = 0;
            }
            else
            {
                if(sleepTrack == 0)
                {
                    if (debugging) Log.Message($"Starting sleep at {bladder.CurLevel} failpoint {DiaperHelper.getBladderControlFailPoint(pawn)} for {pawn.Name}");
                }
                sleepTrack++;

                // every 10 ticks of sleeping
                if (bladder.CurLevel <= 0.75f && sleepTrack % 10 == 0)
                {
                    // 7% every 10 ticks. Sleep roughly 100 ticks, makes it a 10 times 7% chance (70% times the remember potty chance of 5%-100%)
                    if (Rand.ChanceSeeded(0.07f, pawn.HashOffsetTicks()) && !DiaperHelper.remembersPotty(pawn))
                    {
                        if(debugBedwetting) Log.Message($"Triggering bedwetting incident at {bladder.CurLevel} failpoint {DiaperHelper.getBladderControlFailPoint(pawn)} for {pawn.Name}");
                        startAccident();
                    }
                }

            }

            //var currControlLevel = DiaperHelper.getBladderControlLevel(pawn);
            //List<Hediff> health = pawn.health.hediffSet.hediffs;

            if (!isHavingAccident && bladder.CurLevel <= 0f)
            {
                startAccident();
            }


            // the current control level of 0.0 to 1.0 minus the inverted bladder value from 0.0 to 1.0 give a value of -1.0 to 1.0.
            // A negativ value means there is a chance of bladder failure
            //float remainingControl = (currControlLevel - (1.0f - bladder.CurLevel));
            //if (!isHavingAccident && remainingControl < 0f)

            /* This testing was done to find the issue with caching
            if (DiaperHelper.needsDiaperNight(pawn))
            {
                Log.Message($"testing night control: {DiaperHelper.getBladderControlLevel(pawn)} {pawn.health.capacities.GetLevel(PawnCapacityDefOf.BladderControl)} at level {bladder.CurLevel} failpoint {DiaperHelper.getBladderControlFailPoint(pawn)} for {pawn.Name}");
            }*/



            if (!isHavingAccident && bladder.CurLevel <= 0.5f && bladder.CurLevel <= DiaperHelper.getBladderControlFailPoint(pawn))
            {
                bool startPottyRun = false;
                if (pawn.CurJobDef == JobDefOf.LayDown && pawn.Awake() == false && DiaperHelper.remembersPotty(pawn))
                {
                    // Interrupt the current sleep job
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);

                    JobGiver_UseToilet jobGiver = new JobGiver_UseToilet();

                    // Get a toilet job for the pawn
                    Job job = jobGiver.TryGiveJob(pawn);
                    if (debugBedwetting) Log.Message($"conditions met for wakeup call at level {bladder.CurLevel} failpoint {DiaperHelper.getBladderControlFailPoint(pawn)} for {pawn.Name}");
                    // If a job is found, start the job
                    if (job != null)
                    {
                        startPottyRun = true;
                        pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                    }
                }
                //float failChance = ((remainingControl * -1.0f) * 0.06f) - 0.01f;
                /*Log.Message("debug: bladder control of " + remainingControl + " lower then 0, calculated fail of " + failChance.ToString() + " for " + pawn.Name);

                bool trigger = Rand.ChanceSeeded(failChance, pawn.HashOffsetTicks());
                if (trigger)
                {
                    */
                if (startPottyRun)
                {
                    if (debugBedwetting) Log.Message($"prevent fail of bladder control at level {bladder.CurLevel} failpoint {DiaperHelper.getBladderControlFailPoint(pawn)} for {pawn.Name}");
                }
                else
                {
                    startAccident();
                    if (debugBedwetting) Log.Message($"doing fail of bladder control at level {bladder.CurLevel} failpoint {DiaperHelper.getBladderControlFailPoint(pawn)} for {pawn.Name}");
                }

            }

            if (!isHavingAccident && bladder.CurLevel <= 0.30f)
            {
                if (pawn.Awake())
                {
                    if (pawn.story.traits.HasTrait(TraitDef.Named("Potty_Rebel")))
                    {
                        startAccident();
                        if (debugging) Log.Message("doing trait (Potty_Rebel) for " + pawn.Name);
                    }
                    else
                    {
                        var liked = DiaperHelper.getDiaperPreference(pawn);
                        if (liked == DiaperLikeCategory.Liked)
                        {
                            if (currProtection == null || !DiaperHelper.isDiaper(currProtection))
                            {
                                pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("HadToHoldIt"), null, null);
                            }
                            else
                            {
                                startAccident();
                                if (debugging) Log.Message("doing (peeing diaper by preference) for " + pawn.Name);
                            }
                        }

                    }
                }
            }
            if (isHavingAccident)
            {
                if (currProtection != null)
                {
                    

                    var peeAmountPercentile = Math.Min(1.0f - bladder.CurLevel, 0.10f);
                    var fluidAmount = DiaperHelper.fluidAmount(pawn, peeAmountPercentile);

                    if (debugging) Log.Message($"peeing in protection: {DiaperHelper.getBladderControlLevel(pawn)} for {pawn.LabelShort} percentile {peeAmountPercentile} => {fluidAmount} amount");

                    bladder.CurLevel += peeAmountPercentile;
                    float absorbency = pawn.GetStatValue(StatDefOf.DiaperAbsorbency) / 3; // we always calculate at 1/3 the value. 3 liter capacity is the base for this calculation
                                                                                          //float totalDiaperAbsorbency = currDiapie.GetStatValue(StatDefOf.Absorbency);
                                                                                          //Log.Message("absorbency: " + absorbency);
                                                                                          // Log.Message("absorbency2: " + currDiapie.GetStatValue(StatDefOf.Absorbency));
                    if (absorbency < 0.1f) absorbency = 0.1f;
                    float hitPointDamage = fluidAmount / (0.05f * absorbency);

                    currProtection.HitPoints -= chancedDamage(hitPointDamage);
                    if (currProtection.HitPoints < 1)
                    {
                        currProtection.Notify_LordDestroyed();
                        currProtection.Destroy(DestroyMode.Vanish);
                    }
                }
                else
                {
                    crapPants();
                }
                if (bladder.CurLevel >= 0.95f)
                {
                    isHavingAccident = false;
                }
            }
            if (currProtection != null && !currProtection.DestroyedOrNull())
            {
                CurLevel = (float)currProtection.HitPoints / (float)currProtection.MaxHitPoints;

                if(currProtection.HitPoints < currProtection.MaxHitPoints / 2)
                {
                    // That means there are 400 need intervals in a day, meaning a chance of 0.0025 happens once a day
                    // 0.5 - (0.0 - 0.5) makes the total chance higher, the higher the amount of missing hitpoints
                    float chance = Math.Min(0.5f, 0.5f - (currProtection.HitPoints / currProtection.MaxHitPoints));
                    chance = 0.003f * chance * Find.Storyteller.difficulty.playerPawnInfectionChanceFactor; // infection factors are 0.3,"Easy" 0.5,"Medium" 0.75 and 1.1 

                    //Log.Message("DEBUG: playerPawnInfectionChange = " + Find.Storyteller.difficulty.playerPawnInfectionChanceFactor);
                    if (Rand.ChanceSeeded(chance, pawn.HashOffsetTicks()+8))
                    {
                        if (!pawn.health.hediffSet.HasHediff(HediffDefOf.DiaperRash))
                        {
                            if (debugging) Log.Message("doing rash incident at fail chance " + chance.ToString() + " for " + pawn.Name);
                            BodyPartRecord part = pawn.RaceProps.body.AllParts.FirstOrFallback((BodyPartRecord p) => p.def == RimWorld.BodyPartDefOf.Torso);
                            var hediff = pawn.health.AddHediff(HediffDefOf.DiaperRash, part);
                            if (PawnUtility.ShouldSendNotificationAbout(pawn))
                            {
                                Messages.Message("LetterDiaperRash".Translate(pawn.LabelShort, hediff.Label, pawn.Named("PAWN")), pawn, MessageTypeDefOf.NegativeHealthEvent);
                            }
                        }
                        else
                        {
                            if (debugging) Log.Message("skipping rash incident at fail chance " + chance.ToString() + " for " + pawn.Name);
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
        int chancedDamage(float damage)
        {
            float halfHitpoints = damage - (float)Math.Floor(damage);

            if (halfHitpoints > 0.0f && Rand.ChanceSeeded(halfHitpoints, pawn.HashOffsetTicks() + 4))
            {
                return (int)Math.Ceiling(damage);
            }

            return (int)Math.Floor(damage);
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
                List<ThingCategoryDef> cat = thing.def.thingCategories;
                if (cat == null)
                {
                    continue;
                }
                
                if (cat.Contains(ThingCategoryDefOf.Diapers))
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
        public static void addHediffToBladder(Pawn pawn, HediffDef defName)
        {
            BodyPartRecord bladder = DiaperHelper.getBladderControlSourcePart(pawn);
            if (bladder == null)
            {
                return;
            }
            var newHediff = HediffMaker.MakeHediff(defName, pawn);
            pawn.health.AddHediff(newHediff, bladder);
        }
        public static void replaceBladderPart(Pawn pawn, HediffDef bladderSize)
        {
            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debugGenes = settings.debugging && settings.debuggingGenes;
            if (debugGenes) Log.Message($"Pawn {pawn.LabelShort} searching bladder");
            BodyPartRecord originalBladder = getBladderControlSourcePart(pawn);
            if (originalBladder != null)
            {
                // Remove the original Bladder part
                pawn.health.hediffSet.hediffs.RemoveAll(hediff => hediff.Part == originalBladder);
                if (debugGenes) Log.Message($"Pawn {pawn.LabelShort} removing old hediff");

                // Check if the new bladder part is already defined in the pawn's body
                BodyPartRecord newBladderPart = pawn.RaceProps.body.AllParts.Find(part => part.def == BodyPartDefOf.Bladder);

                if (newBladderPart != null)
                {
                    if (debugGenes) Log.Message($"Restore procedure to {pawn.LabelShort}");
                    pawn.health.AddHediff(RimWorld.HediffDefOf.MissingBodyPart, newBladderPart); // Mark the original bladder as missing
                    pawn.health.RestorePart(newBladderPart); // Restore the new bladder part
                    if (bladderSize == HediffDefOf.BigBladder)
                    {
                        addHediffToBladder(pawn, HediffDefOf.BigBladder);
                    }
                    if (bladderSize == HediffDefOf.SmallBladder)
                    {
                        addHediffToBladder(pawn, HediffDefOf.SmallBladder);
                    }
                        
                }
            }
        }
        public static BodyPartRecord getBladderControlSourcePart(Pawn pawn)
        {
            foreach (var part in pawn.health.hediffSet.GetNotMissingParts())
            {
                if (part.def.tags.Contains(BodyPartTagDefOf.BladderControlSource))
                {
                    return part;
                }
            }
            return null;
        }
        public static void getMemory(Pawn pawn, ThoughtDef thought, int stage = 0)
        {
            var debugging = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>().debugging;
            if (!pawn.health.capacities.CanBeAwake)
            {
                if (debugging) Log.Message("Can't gain Memory for unconcious " + pawn.Name + ", thought " + thought.defName + " at stage " + stage.ToString());
            }
            
            var memories = pawn.needs.mood.thoughts.memories;
            if (debugging) Log.Message("Creating Memory for "+pawn.Name+", thought "+thought.defName+" at stage "+stage.ToString());

            Thought_Memory firstMemoryOfDef = memories.GetFirstMemoryOfDef(thought);
            if(firstMemoryOfDef != null)
            {
                firstMemoryOfDef.SetForcedStage(stage); // Making sure that merge works on TryGainMemory
            }
            var newThought = ThoughtMaker.MakeThought(thought, null);
            newThought.SetForcedStage(stage);
            memories.TryGainMemory(newThought);
        }
        public static void getMemory(Pawn pawn, WettingDiaperThought thought)
        {
            var defNormal = ThoughtDef.Named("DiaperPeed");
            DiaperHelper.getMemory(pawn, defNormal, (int)thought);
        }
        public static void getMemory(Pawn pawn, BedwettingDiaperThought thought)
        {
            var defBed = ThoughtDef.Named("DiaperPeedBed");
            DiaperHelper.getMemory(pawn, defBed, (int)thought);
        }
        public static void getMemory(Pawn pawn, WettingPantsThought thought)
        {
            var defPants = ThoughtDef.Named("PantsPeed");
            DiaperHelper.getMemory(pawn, defPants, (int)thought);
        }
        public static void getMemory(Pawn pawn, WettingBedThought thought)
        {
            var defPants = ThoughtDef.Named("WetBed");
            DiaperHelper.getMemory(pawn, defPants, (int)thought);
        }
        public static float CalculateProbability(float bladderControl)
        {
            // Base chance of 1%
            if (bladderControl > 1f) return 0.005f;

            // this only applies for bladder control under 100%
            bladderControl = Mathf.Clamp(bladderControl, 0f, 1f);

            if (bladderControl >= 0.75f)
            {
                // Range 1.0 to 0.75: 1%-5% chance
                return Mathf.Lerp(0.01f, 0.05f, Mathf.InverseLerp(1.0f, 0.75f, bladderControl));
            }
            else if (bladderControl >= 0.5f)
            {
                // Range 0.75 to 0.5: 5%-30% chance
                return Mathf.Lerp(0.05f, 0.30f, Mathf.InverseLerp(0.75f, 0.5f, bladderControl));
            }
            else if (bladderControl >= 0.25f)
            {
                // Range 0.5 to 0.25: 30%-80% chance
                return Mathf.Lerp(0.30f, 0.80f, Mathf.InverseLerp(0.5f, 0.25f, bladderControl));
            }
            else
            {
                // Range lower than 0.25: 100% chance
                return 1.0f;
            }
        }
        public static bool remembersPotty(Pawn pawn)
        {
            if (pawn != null && pawn.RaceProps.Humanlike)
            {
                var diaperNeed = pawn.needs.TryGetNeed<Need_Diaper>();
                if (diaperNeed == null) return true; // anything without that gets a free pass
                if (diaperNeed.IsHavingAccident) return true; // now we notice for sure
                if (!getBladderControlLevelCapable(pawn)) return false; // incapeable of noticing
                if (pawn.story.traits.HasTrait(TraitDef.Named("Potty_Rebel"))) return false; // will never run to a potty!

                var currDiapie = getDiaper(pawn);
                if (currDiapie != null && pawn.outfits.forcedHandler.IsForced(currDiapie)) return false; // forced in diapers


                float bladderControl = getBladderControlLevel(pawn);

                // 0.2 bladder control = 100%, 0.65 bladder control = 1%
                //float probability = Mathf.Clamp01(-2 * bladderControl + 1.4f);
                float probability = CalculateProbability(bladderControl);

                // Use the in-game day combined with pawn's birth year and birth day as the seed
                //int seed = GenDate.DaysPassed + pawn.ageTracker.BirthYear + pawn.ageTracker.BirthDayOfYear;
                var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
                var debugBedwetting = settings.debugging && settings.debuggingBedwetting && !pawn.Awake();
                var debug = settings.debugging && (settings.debuggingCapacities || debugBedwetting);
                if (Rand.ChanceSeeded(probability, diaperNeed.FailureSeed))
                {
                    if (debug) Log.Message($"JobGiver_UseToilet prefix: job denied, {pawn.Name.ToStringShort} at propability {probability} and seed {diaperNeed.FailureSeed}");
                    return false; // depending on the level on control
                }
                else
                {
                    if (debug) Log.Message($"JobGiver_UseToilet prefix: job given, {pawn.Name.ToStringShort} at propability {probability} and seed {diaperNeed.FailureSeed}");
                }
            }
            return true;
        }


        public static bool AcceptableNightTimeOld(Pawn pawn)
        {
            Need_Rest restNeed = pawn.needs.TryGetNeed<Need_Rest>();
            if (restNeed != null && restNeed.CurLevel < 0.3) return true;

            // Check the pawn's current schedule
            TimeAssignmentDef currentAssignment = pawn.timetable?.CurrentAssignment;

            // If the schedule is not defined or the pawn is scheduled to sleep, return true
            return currentAssignment == TimeAssignmentDefOf.Sleep && restNeed.CurLevel < 0.8;
        }
        public static bool isWearingNightDiaper(Pawn pawn)
        {
            List<Apparel> wornApparel = pawn?.apparel?.WornApparel;
            if (wornApparel == null)
            {
                return false;
            }

            for (int i = 0; i < wornApparel.Count; i++)
            {
                Apparel apparel = wornApparel[i];

                if (isNightDiaper(apparel))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool needsDiaper(Pawn pawn)
        {            
            return getBladderControlLevel(pawn) <= 0.5f;
        }
        public static bool acceptsDiaper(Pawn pawn)
        {
            var pref = getDiaperPreference(pawn);
            return pref == DiaperLikeCategory.NonAdult || pref == DiaperLikeCategory.Liked || (pref != DiaperLikeCategory.Disliked && needsDiaper(pawn));
        }
        public static bool needsDiaperNight(Pawn pawn)
        {
            var bladderControlWorker = new PawnCapacityWorker_BladderControl();
            return bladderControlWorker.SimulateBladderControlDuringSleep(pawn) <= 0.5f;
        }
        public static bool acceptsDiaperNight(Pawn pawn)
        {
            var pref = getDiaperPreference(pawn);
            return pref == DiaperLikeCategory.NonAdult || pref == DiaperLikeCategory.Liked || (pref != DiaperLikeCategory.Disliked && needsDiaperNight(pawn));
        }
        public static Apparel getUnderwearOrDiaper(Pawn pawn)
        {
            List<Apparel> wornApparel = pawn?.apparel?.WornApparel;
            if (wornApparel == null) return null;


            for (int i = 0; i < wornApparel.Count; i++)
            {
                Apparel apparel = wornApparel[i];

                if (isDiaper(apparel)) return apparel;
                if (isUnderwear(apparel)) return apparel;
            }
            return null;
        }
        public static Apparel getUnderwear(Pawn pawn)
        {
            List<Apparel> wornApparel = pawn?.apparel?.WornApparel;
            if (wornApparel == null) return null;


            for (int i = 0; i < wornApparel.Count; i++)
            {
                Apparel apparel = wornApparel[i];

                if (isUnderwear(apparel)) return apparel;
            }
            return null;
        }
        public static Apparel getDiaper(Pawn pawn)
        {
            List<Apparel> wornApparel = pawn?.apparel?.WornApparel;
            if (wornApparel == null) return null;
            
            
            for (int i = 0; i < wornApparel.Count; i++)
            {
                Apparel apparel = wornApparel[i];
                    
                if (isDiaper(apparel)) return apparel;
            }
            return null;
        }
        public static bool isDiaper(Apparel cloth)
        {
            List<ThingCategoryDef> cat = cloth.def.thingCategories;
            if (cat == null) return false;

            return cat.Contains(ThingCategoryDefOf.Diapers);
        }
        public static bool isUnderwear(Apparel cloth)
        {
            List<ThingCategoryDef> cat = cloth.def.thingCategories;
            if (cat == null) return false;

            return cat.Contains(ThingCategoryDefOf.Underwear);
        }
        public static bool isNightDiaper(Apparel cloth)
        {
            List<ThingCategoryDef> cat = cloth.def.thingCategories;
            if (cat == null)
            {
                return false;
            }

            return cat.Contains(ThingCategoryDefOf.DiapersNight);
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
        public static float fluidAmount(Pawn pawn, float units)
        {
            // Higher rate than 1 means that the bladder is smaller, so one unit is actually less
            float rateMultiplier = pawn.GetStatValue(DubDef.BladderRateMultiplier);
            return units / rateMultiplier;
        }
        public static float getBladderControlLevel(Pawn pawn)
        {
            return pawn.health.capacities.GetLevel(PawnCapacityDefOf.BladderControl);
        }
        public static bool getBladderControlLevelCapable(Pawn pawn)
        {
            return pawn.health.capacities.CapableOf(PawnCapacityDefOf.BladderControl);
        }

        public static float getBladderControlFailPoint(Pawn pawn)
        {
            var bladderControl = getBladderControlLevel(pawn);

            // Define the input and output ranges
            float x0 = 1.0f; // If full bladder control
            float y0 = 0.0f; // The failure will happen at 0.0 
            float x1 = 0.2f; // If 20% bladder control
            float y1 = 0.4f; // the failure will happen at 0.4 (60% filled bladder)

            // If the input is less than or equal to the lower bound, return the lower output bound
            if (bladderControl >= x0)
            {
                return y0;
            }

            // If the input is greater than or equal to the upper bound, return the upper output bound
            if (bladderControl <= x1)
            {
                return y1;
            }

            // Perform linear interpolation
            return y0 + (bladderControl - x0) * (y1 - y0) / (x1 - x0);
        }


        public static DiaperLikeCategory getDiaperPreference(Pawn pawn)
        {
            if (!pawn.ageTracker.Adult)
            {
                return DiaperLikeCategory.NonAdult;
            }
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
                        if (IntVec3Utility.DistanceTo(p.Position, pawn.Position) <= 6 && pawn.needs.TryGetNeed<Need_Diaper>().CurLevel <= 0.6f)
                        {
                            stinkLevel += (pawn.needs.TryGetNeed<Need_Diaper>().CurLevel - 0.4f);
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
    public class StatPart_LimitedSupport : StatPart
    {
        // Assuming mainStat is the absorbency and supportStat is the support.
        public StatDef mainStat;
        public StatDef supportStat;

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing)
            {
                Pawn pawn = req.Thing as Pawn;
                if (pawn != null)
                {
                    float mainValue = pawn.apparel.WornApparel.Sum(a => a.GetStatValue(mainStat));
                    float supportValue = pawn.apparel.WornApparel.Sum(a => a.GetStatValue(supportStat));

                    // Add support but limit it to not exceed the main stat value
                    val += Mathf.Min(supportValue, mainValue);
                }
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            return $"Support adds to absorbency but cannot increase it beyond the base absorbency value.";
        }
    }
}

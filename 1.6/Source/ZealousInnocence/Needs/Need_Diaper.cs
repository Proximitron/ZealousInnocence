using DubsBadHygiene;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ZealousInnocence
{
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
        private bool isAdditionalCrapPants;
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
        private LocalTargetInfo cachedDiaperTarget;
        private float lastCacheUpdateTime;
        private const float CacheUpdateInterval = 1f; // 1 second

        public LocalTargetInfo getCachedBestDiaperOrUndie()
        {
            if (Time.realtimeSinceStartup - lastCacheUpdateTime > CacheUpdateInterval)
            {
                cachedDiaperTarget = getBestDiaperOrUndie(null, pawn);
                lastCacheUpdateTime = Time.realtimeSinceStartup;
            }
            return cachedDiaperTarget;
        }
        public static LocalTargetInfo getBestDiaperOrUndie(Pawn caretaker, Pawn patient)
        {
            Need_Diaper need_diaper = patient.needs.TryGetNeed<Need_Diaper>();
            Apparel oldDiaper = Helper_Diaper.getUnderwearOrDiaper(patient);
            bool oldIsAllowed = oldDiaper == null ? false : Helper_Diaper.allowedByPolicy(patient, oldDiaper);
            if(need_diaper == null)
            {
                JobFailReason.Is("Need diaper was null?!");
                return null;
            }
            if (oldDiaper != null && oldIsAllowed && need_diaper.CurLevel >= 0.5f)
            {
                if(caretaker != null) JobFailReason.Is("No change needed.");
                return LocalTargetInfo.Invalid;
            }

            float minRating = -0.1f;
            if (oldIsAllowed)
            {
                if (patient.outfits?.forcedHandler != null && !patient.outfits.forcedHandler.AllowedToAutomaticallyDrop(oldDiaper))
                {
                    if (caretaker != null) CantRemoveUnderwearReason(oldDiaper);
                    return LocalTargetInfo.Invalid;
                }
                if (patient.apparel != null && patient.apparel.IsLocked(oldDiaper))
                {
                    if (caretaker != null) CantRemoveUnderwearReason(oldDiaper);
                    return LocalTargetInfo.Invalid;
                }
                minRating = Helper_Diaper.getDiaperOrUndiesRating(patient, oldDiaper);
            }
            //var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            //var debugging = settings.debugging && settings.debuggingCloth;
            
            LocalTargetInfo a = findBestUndieOrDiaper(caretaker, patient, minRating);
            if (a == null || !a.IsValid || !a.HasThing)
            {
                if (caretaker != null) JobFailReason.Is("No allowed cloth found.");
                return LocalTargetInfo.Invalid;
            }
            return a;
        }
        private static void CantRemoveUnderwearReason(Apparel apparel)
        {
            if (Helper_Diaper.isDiaper(apparel))
            {
                JobFailReason.Is("Can't remove diaper.");
            }
            else if (Helper_Diaper.isNightDiaper(apparel))
            {
                JobFailReason.Is("Can't remove pull-ups.");
            }
            else
            {
                JobFailReason.Is("Can't remove underwear.");
            }
        }
        private static LocalTargetInfo findBestUndieOrDiaper(Pawn caretaker, Pawn patient, float minRating = -0.1f)
        {
            Thing bestThing = null;
            float bestRating = minRating;

            //var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            //var debugging = settings.debugging && settings.debuggingCloth;
            
            if (caretaker == null || patient == null) return LocalTargetInfo.Invalid;
            if (!caretaker.IsColonist || (!patient.IsColonist && !patient.IsPrisoner)) return LocalTargetInfo.Invalid;
            if (!caretaker.Spawned || !patient.Spawned) return LocalTargetInfo.Invalid;
            if (caretaker.Map == null || patient.Map == null) return LocalTargetInfo.Invalid;
            foreach (Thing thing in patient.Map.listerThings.AllThings)
            {
                if (thing is Apparel app)
                {
                    if (app.HitPoints < (app.MaxHitPoints / 2)) continue;

                    float rating = Helper_Diaper.getDiaperOrUndiesRating(patient, app);
                    if (rating > bestRating && caretaker.CanReserveAndReach(thing, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        bestRating = rating;
                        bestThing = thing;
                    }
                }
            }
            return bestThing != null ? new LocalTargetInfo(bestThing) : LocalTargetInfo.Invalid;
        }
        public override void SetInitialLevel()
        {
            base.CurLevelPercentage = 0f;
        }

        public bool hasDiaper()
        {
            return Helper_Diaper.getDiaper(pawn) != null;
        }
        public bool hasUnderwearOrDiaper()
        {
            return Helper_Diaper.getUnderwearOrDiaper(pawn) != null;
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
            get {
                if (currProtection != null)
                {
                    return Math.Max(0f, (float)currProtection.HitPoints / (float)currProtection.MaxHitPoints);
                }
                else
                {
                    return 0.0f;
                }
                //return base.CurLevel;
            }
            set
            {
                /*DiaperSituationCategory curCategory = CurCategory;
                base.CurLevel = value;
                if (CurCategory != curCategory)
                {
                    this.CategoryChanged();
                }*/
            }
        }

        public override bool ShowOnNeedList
        {
            get
            {
                return true;// hasUnderwearOrDiaper();
            }
        }

        public void crapPants()
        {
            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debugging = settings.debugging && settings.debuggingCapacities;
            if (debugging) Log.Message("debug: doing (crapPants) for " + pawn.Name);

            Need bladder = pawn.needs.TryGetNeed<Need_Bladder>();
            var peeAmountPercentile = Math.Min(1.0f - bladder.CurLevel, 0.10f);
            var fluidAmount = Helper_Diaper.fluidAmount(pawn, peeAmountPercentile);
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
            if (!pawn.InBed())
            {
                List<Apparel> legApparel = pawn.apparel.WornApparel.Where(apparel => apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs)).ToList();
                int splitOfDamage = legApparel.Count;
                int damageBase = 25; // damage for a full bladder in hp
                for (int i = legApparel.Count - 1; i >= 0; i--)
                {
                    Apparel apparel = legApparel[i];
                    float damage = damageBase * (peeAmountPercentile / splitOfDamage);
                    int finDamage = chancedDamage(damage);
                    if (debugging)  Log.Message($"[ZI]crapPants: causing {finDamage} damage to {apparel.LabelShort} for {pawn.LabelShort} " + pawn.Name);
                    apparel.HitPoints -= Math.Min(apparel.HitPoints, finDamage);
                    pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("SoakingWet"), pawn, null);

                    // Check if the apparel gets destroyed during this process
                    if (apparel.HitPoints < 1)
                    {
                        apparel.Notify_LordDestroyed();
                        Messages.Message("MessageClothDestroyedByAccident".Translate(pawn.Named("PAWN"), apparel.Named("CLOTH")), pawn, MessageTypeDefOf.NegativeEvent, true);
                        apparel.Destroy(DestroyMode.Vanish);
                    }
                }
            }

            if (pawn.InBed())
            {
                var bed = pawn.CurrentBed();
                float damage = ((float)bed.MaxHitPoints / 2f) * fluidAmount;
                bed.HitPoints -= Math.Min(bed.HitPoints - 1, chancedDamage(damage));
                if (!isAdditionalCrapPants)
                {
                    foreach (var curr in pawn.CurrentBed().CurOccupants)
                    {
                        if (curr != pawn)
                        {
                            curr.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("PeedOnMe"), pawn, null);
                            curr.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("SoakingWet"), pawn, null);
                        }
                    }
                    if (!Helper_Regression.isAdult(pawn)) Helper_Diaper.getMemory(pawn, WettingBedThought.Wet_Bed_Non_Adult);
                    else if (Helper_Diaper.needsDiaperNight(pawn)) Helper_Diaper.getMemory(pawn, WettingBedThought.Wet_Bed_Bedwetter);
                    else Helper_Diaper.getMemory(pawn, WettingBedThought.Wet_Bed_Default);
                    pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("SoakingWet"), pawn, null);
                }

            }

            if (peeing) FilthMaker.TryMakeFilth(this.pawn.Position, this.pawn.Map, ThingDef.Named("FilthUrine"), filth, FilthSourceFlags.Pawn);
            else FilthMaker.TryMakeFilth(this.pawn.Position, this.pawn.Map, ThingDef.Named("FilthFaeces"), filth, FilthSourceFlags.Pawn);

            isAdditionalCrapPants = true; // Additional triggers of this function will be ignored until current accident is done
        }

        public void startAccident(bool pee = true)
        {
            if (IsHavingAccident) return;
            StartSound(pee);
            FailureSeed = 0;
            isHavingAccident = true;
            peeing = pee;
            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debugging = settings.debugging && settings.debuggingCapacities;
            if (debugging) Log.Message("debug: starting accident '" + (peeing ? "pee" : "poop") + "' for " + pawn.Name);

            var liked = Helper_Diaper.getDiaperPreference(pawn);
            if (Helper_Diaper.getDiaper(pawn) != null)
            {
                switch (liked)
                {
                    case DiaperLikeCategory.NonAdult:
                        if (pawn.Awake()) Helper_Diaper.getMemory(pawn, WettingDiaperThought.Wet_Diaper_Non_Adult);
                        else Helper_Diaper.getMemory(pawn, BedwettingDiaperThought.Wet_Diaper_Bed_Non_Adult);
                        break;
                    case DiaperLikeCategory.Neutral:
                        if (!pawn.Awake())
                        {
                            if (Helper_Diaper.needsDiaperNight(pawn)) Helper_Diaper.getMemory(pawn, BedwettingDiaperThought.Wet_Diaper_Bed_Bedwetter);
                            else Helper_Diaper.getMemory(pawn, BedwettingDiaperThought.Wet_Diaper_Bed_Default);
                        }
                        else Helper_Diaper.getMemory(pawn, WettingDiaperThought.Wet_Diaper_Default);
                        break;
                    case DiaperLikeCategory.Liked:
                        if (!pawn.Awake())
                        {
                            if (Helper_Diaper.needsDiaperNight(pawn)) Helper_Diaper.getMemory(pawn, BedwettingDiaperThought.Wet_Diaper_Bed_Bedwetter_Loved);
                            else Helper_Diaper.getMemory(pawn, BedwettingDiaperThought.Wet_Diaper_Bed_Loved);
                        }
                        else Helper_Diaper.getMemory(pawn, WettingDiaperThought.Wet_Diaper_Loved);
                        break;
                    case DiaperLikeCategory.Disliked:
                        Helper_Diaper.getMemory(pawn, WettingDiaperThought.Wet_Diaper_Hated);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else if (pawn.InBed())
            {
                // We ignore this case and assume the pawn has no pants on in bed
                // Thoughts will be handled by the wet bed incidents
            }
            else
            {
                switch (liked)
                {

                    case DiaperLikeCategory.NonAdult:
                        Helper_Diaper.getMemory(pawn, WettingPantsThought.Wet_Pants_Non_Adult);
                        break;
                    case DiaperLikeCategory.Neutral:
                        Helper_Diaper.getMemory(pawn, WettingPantsThought.Wet_Pants_Default);
                        break;
                    case DiaperLikeCategory.Liked:
                        Helper_Diaper.getMemory(pawn, WettingPantsThought.Wet_Pants_Diaper_Loved);
                        break;
                    case DiaperLikeCategory.Disliked:
                        Helper_Diaper.getMemory(pawn, WettingPantsThought.Wet_Pants_Diaper_Hated);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        private Apparel_UnderwearSlot currProtectionCache;

        private Apparel_UnderwearSlot currProtection
        {
            get
            {
                if (currProtectionCache == null)
                {
                    if (pawn.Dead || !pawn.RaceProps.Humanlike || !pawn.RaceProps.IsFlesh) return null;
                    Apparel diaperSlot = Helper_Diaper.getUnderwearOrDiaper(pawn);
                    if (diaperSlot is Apparel_UnderwearSlot diaperOrUndies)
                    {
                        currProtectionCache = diaperOrUndies;
                    }

                }
                return currProtectionCache;
            }
            set
            {
                currProtectionCache = value;
            }
        }
        // Triggers every 150 ticks! (2.5 seconds)
        // 2500 ticks in an ingame hour, 60000 ticks in a day (day is 1000 realtime seconds)
        // That means there are 400 need intervals in a day, meaning a chance of 0.0025 happens once a day
        public override void NeedInterval()
        {
            if (pawn.Dead || !pawn.RaceProps.Humanlike || !pawn.RaceProps.IsFlesh) return;
            pawn.health.capacities.Notify_CapacityLevelsDirty(); // Yes, nessesary. The caching doesn't keep track of changes like sleeping and things that not cause hediffs
            currProtectionCache = null;

            var bedwettingDef = HediffDefOf.BedWetting;
            Need bladder = pawn.needs.TryGetNeed<Need_Bladder>();
            if (bladder == null)
            {
                if (pawn.health.hediffSet.HasHediff(bedwettingDef))
                {
                    pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(bedwettingDef));
                }
                return;
            }
            
            var settings = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            var debugging = settings.debugging && settings.debuggingCapacities;
            var debugBedwetting = settings.debugging && settings.debuggingBedwetting;

            new HediffGiver_BedWetting().OnIntervalPassed(pawn, null);
            if (pawn.Awake())
            {
                if (sleepTrack > 0)
                {
                    if (debugging)  Log.Message($"[ZI]Ending sleep at {bladder.CurLevel} failpoint {Helper_Diaper.getBladderControlFailPoint(pawn)} for {pawn.Name} after {sleepTrack} ticks");
                }
                sleepTrack = 0;
            }
            else
            {
                if (sleepTrack == 0)
                {
                    if (debugging)  Log.Message($"[ZI]Starting sleep at {bladder.CurLevel} failpoint {Helper_Diaper.getBladderControlFailPoint(pawn)} for {pawn.Name}");
                }
                sleepTrack++;

                // every 10 ticks of sleeping
                if (bladder.CurLevel <= 0.75f && sleepTrack % 10 == 0)
                {
                    // 7% every 10 ticks. Sleep roughly 100 ticks, makes it a 10 times 10% chance (to remember potty chance of 5%-100%)
                    if (Rand.ChanceSeeded(0.07f, pawn.HashOffsetTicks()) && !Helper_Diaper.remembersPotty(pawn))
                    {
                        if (debugBedwetting)  Log.Message($"[ZI]Triggering bedwetting incident at {bladder.CurLevel} failpoint {Helper_Diaper.getBladderControlFailPoint(pawn)} for {pawn.Name}");
                        startAccident(true);
                    }
                }

            }

            //var currControlLevel = DiaperHelper.getBladderControlLevel(pawn);
            //List<Hediff> health = pawn.health.hediffSet.hediffs;

            if (!isHavingAccident && bladder.CurLevel <= 0f)
            {
                startAccident(!settings.faecesActive);
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



            if (!isHavingAccident && bladder.CurLevel <= 0.5f && pawn.CurJobDef != DubDef.UseToilet)
            {
                if (bladder.CurLevel <= Helper_Diaper.getBladderControlFailPoint(pawn))
                {
                    bool startPottyRunBed = false;
                    bool startPottyRun = false;
                    if (pawn.health.capacities.CanBeAwake && !Helper_Diaper.shouldStayPut(pawn) && Helper_Diaper.remembersPotty(pawn))
                    {
                        if (pawn.CurJobDef == JobDefOf.LayDown && pawn.Awake() == false)
                        {
                            // Interrupt the current sleep job
                            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);

                            var jobGiver = new DubsBadHygiene.JobGiver_UseToilet();

                            // Get a toilet job for the pawn
                            Job job = jobGiver.TryGiveJob(pawn);
                            if (debugBedwetting) Log.Message($"[ZI]conditions met for wakeup call at level {bladder.CurLevel} failpoint {Helper_Diaper.getBladderControlFailPoint(pawn)} for {pawn.Name}");
                            // If a job is found, start the job
                            if (job != null)
                            {
                                startPottyRunBed = true;
                                pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                            }
                        }
                        else if (CanPolitelyInterrupt(pawn, pawn.CurJob))
                        {
                            if (debugging) Log.Message($"[ZI] polite override: interrupting {pawn.CurJobDef?.defName} for toilet ({pawn.LabelShort})");

                            // End the current trivial job
                            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: true);

                            var jobGiver = new DubsBadHygiene.JobGiver_UseToilet();
                            Job toiletJob = jobGiver.TryGiveJob(pawn);
                            if (toiletJob != null)
                            {
                                startPottyRun = true;
                                pawn.jobs.StartJob(toiletJob, JobCondition.InterruptForced, cancelBusyStances: true);
                            }
                            else
                            {
                                // If we failed to create a toilet job, no harm done: the Lord/think tree will give them a new wander/Wait job.
                                if (debugging) Log.Message($"[ZI] no toilet job available for {pawn.LabelShort} after polite override");
                            }
                        }
                    }

                    if (startPottyRunBed)
                    {
                        if (debugBedwetting) Log.Message($"[ZI]Bedtime prevent fail of bladder control at level {bladder.CurLevel} failpoint {Helper_Diaper.getBladderControlFailPoint(pawn)} for {pawn.Name}");
                    }
                    else if (startPottyRun)
                    {
                        if (debugging) Log.Message($"[ZI]prevent fail of bladder control at level {bladder.CurLevel} failpoint {Helper_Diaper.getBladderControlFailPoint(pawn)} for {pawn.Name}");
                    }
                    else
                    {
                        var pee = true;
                        if (settings.faecesActive && Rand.ChanceSeeded(0.5f, pawn.HashOffsetTicks() + 922)) pee = false;

                        startAccident(pee);
                        if (debugBedwetting) Log.Message($"[ZI]doing fail of bladder control at level {bladder.CurLevel} failpoint {Helper_Diaper.getBladderControlFailPoint(pawn)} for {pawn.Name}");
                    }
                }
                else
                {
                    if (bladder.CurLevel <= 0.28f && Rand.Chance(1f / 32f) && bladder.CurLevel <= (Helper_Diaper.getBladderControlFailPoint(pawn) + 0.1f) && Helper_Diaper.remembersPotty(pawn) && CanPolitelyInterrupt(pawn, pawn.CurJob))
                    {
                        var jobGiver = new DubsBadHygiene.JobGiver_UseToilet();
                        Job toiletJob = jobGiver.TryGiveJob(pawn);
                        if (toiletJob != null)
                        {
                            pawn.jobs.jobQueue.EnqueueFirst(toiletJob);
                            if (debugging) Log.Message($"[ZI]random potty assignment: interrupting {pawn.CurJobDef?.defName} for toilet ({pawn.LabelShort})");
                        }
                    }
                }
            }

            if (!isHavingAccident && bladder.CurLevel <= 0.30f)
            {
                if (pawn.Awake())
                {
                    var pee = true;
                    if (settings.faecesActive && Rand.ChanceSeeded(0.5f, pawn.HashOffsetTicks() + 922)) pee = false;
                    if (pawn.story.traits.HasTrait(TraitDefOf.Potty_Rebel))
                    {
                        startAccident(pee);
                        if (debugging) Log.Message("doing trait (Potty_Rebel) for " + pawn.Name);
                    }
                    else
                    {
                        var liked = Helper_Diaper.getDiaperPreference(pawn);
                        if (liked == DiaperLikeCategory.Liked)
                        {
                            if (currProtection == null || !Helper_Diaper.isDiaper(currProtection))
                            {
                                pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("HadToHoldIt"), null, null);
                            }
                            else
                            {
                                startAccident(pee);
                                if (debugging) Log.Message("doing (accident in diaper by preference) for " + pawn.Name);
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
                    var fluidAmount = Helper_Diaper.fluidAmount(pawn, peeAmountPercentile);
                    if (debugging)  Log.Message($"[ZI]peeing in protection: {Helper_Diaper.getBladderControlLevel(pawn)} for {pawn.LabelShort} percentile {peeAmountPercentile} => {fluidAmount} amount");
                    bladder.CurLevel += peeAmountPercentile;
                    float absorbency = pawn.GetStatValue(StatDefOf.DiaperAbsorbency) / 3; // we always calculate at 1/3 the value. 3 liter capacity is the base for this calculation
                                                                                          //float totalDiaperAbsorbency = currDiapie.GetStatValue(StatDefOf.Absorbency);
                                                                                          //Log.Message("absorbency: " + absorbency);
                                                                                          // Log.Message("absorbency2: " + currDiapie.GetStatValue(StatDefOf.Absorbency));
                    if (absorbency < 0.1f) absorbency = 0.1f;
                    float hitPointDamage = fluidAmount / (0.05f * absorbency);

                    currProtection.HitPoints -= Math.Min(currProtection.HitPoints, chancedDamage(hitPointDamage));
                    if (currProtection.HitPoints < 1)
                    {
                        currProtection.Notify_LordDestroyed();
                        Messages.Message("MessageClothDestroyedByAccident".Translate(pawn.Named("PAWN"), currProtection.Named("CLOTH")), pawn, MessageTypeDefOf.NegativeEvent,true);
                        currProtection.Destroy(DestroyMode.Vanish);
                        currProtection = null;
                    }
                }
                if (bladder.CurLevel >= 0.95f)
                {
                    isHavingAccident = false;
                    isAdditionalCrapPants = false;
                }
                if (currProtection == null && isHavingAccident)
                {
                    crapPants();
                }

            }
            if (!isHavingAccident && currProtection != null && !currProtection.DestroyedOrNull())
            {
                //CurLevel = (float)currProtection.HitPoints / (float)currProtection.MaxHitPoints;

                if (currProtection.HitPoints < currProtection.MaxHitPoints / 2)
                {
                    // That means there are 400 need intervals in a day, meaning a chance of 0.0025 happens once a day
                    // 0.5 - (0.0 - 0.5) makes the total chance higher, the higher the amount of missing hitpoints
                    float chance = Math.Min(0.5f, 0.5f - (currProtection.HitPoints / currProtection.MaxHitPoints));
                    chance = 0.003f * chance * Find.Storyteller.difficulty.playerPawnInfectionChanceFactor; // infection factors are 0.3,"Easy" 0.5,"Medium" 0.75 and 1.1 

                    //Log.Message("DEBUG: playerPawnInfectionChange = " + Find.Storyteller.difficulty.playerPawnInfectionChanceFactor);
                    if (Rand.ChanceSeeded(chance, pawn.HashOffsetTicks() + 8))
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

        private static bool CanPolitelyInterrupt(Pawn pawn, Job curJob)
        {
            if (pawn == null || curJob == null) return false;
            if (pawn.Downed || pawn.InMentalState) return false;
            if(pawn.CurJob?.playerForced == true) return false;

            // If job def says it’s NOT interruptible for the player, treat it as essential
            if (curJob.def?.playerInterruptible == false) return false;
            if (curJob.locomotionUrgency == LocomotionUrgency.Sprint) return false;
            // Avoid interrupting critical things
            if (IsEssentialJob(curJob)) return false;
            if(ForbidsLongNeeds(pawn)) return false;

            return true;
        }
        private static bool ForbidsLongNeeds(Pawn pawn)
        {
            var duty = pawn?.mindState?.duty;
            if (duty == null) return false;

            // Try common names across versions
            var t = duty.GetType();
            var f = t.GetField("allowSatisfyLongNeeds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f?.FieldType == typeof(bool)) return !(bool)f.GetValue(duty);
            var p = t.GetProperty("allowSatisfyLongNeeds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p?.PropertyType == typeof(bool)) return !(bool)p.GetValue(duty);

            return false; // unknown => don’t block
        }

        private static bool IsEssentialJob(Job job)
        {
            if (job?.def == null) return false;
            var def = job.def;

            // conservative deny-list: combat, emergency, tending, rescuing, hauling a downed pawn, arrest, manhunter-like, fleeing, ingest while starving, etc.
            if (def == RimWorld.JobDefOf.AttackMelee || def == RimWorld.JobDefOf.AttackStatic || def == RimWorld.JobDefOf.Wait_Combat || def == RimWorld.JobDefOf.Hunt)
                return true;
            if (def == RimWorld.JobDefOf.Flee || def == RimWorld.JobDefOf.TakeWoundedPrisonerToBed || def == RimWorld.JobDefOf.Rescue || def == RimWorld.JobDefOf.Capture || def == RimWorld.JobDefOf.Arrest)
                return true;
            if (def == RimWorld.JobDefOf.TendPatient || def == RimWorld.JobDefOf.TendEntity || def == RimWorld.JobDefOf.BeatFire || def == RimWorld.JobDefOf.ExtinguishSelf)
                return true;

            return false;
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
}

using RimWorld.QuestGen;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ZealousInnocence
{
    public class GameComponent_RegressionGame : GameComponent
    {
        public bool FountainSpawned
        {
            get
            {
                return this.fountain != null && this.fountain.Spawned;
            }
        }

        public LargeBuildingSpawnParms FountainSpawnParams
        {
            get
            {
                return this.fountainSpawnParams;
            }
        }

        public int Level
        {
            get
            {
                if (!this.FountainSpawned)
                {
                    return 0;
                }
                return this.level;
            }
        }

        public int HighestLevelReached
        {
            get
            {
                return this.highestLevelReached;
            }
        }

        public FountainOfYouthLevelDef LevelDef
        {
            get
            {
                if (!this.FountainSpawned)
                {
                    return FountainOfYouthLevelDefOf.Inactive;
                }
                return this.levelDef ?? FountainOfYouthLevelDefOf.Inactive;
            }
        }

        public FountainOfYouthLevelDef NextLevelDef
        {
            get
            {
                if (!this.LevelDef.advanceThroughActivation)
                {
                    return null;
                }
                return DefDatabase<FountainOfYouthLevelDef>.AllDefs.FirstOrDefault((FountainOfYouthLevelDef x) => x.level == this.Level + 1);
            }
        }

        public int TicksSinceLastLevelChange
        {
            get
            {
                return Find.TickManager.TicksGame - this.lastLevelChangeTick;
            }
        }

        public IReadOnlyList<ChoiceLetter> FountainLetters
        {
            get
            {
                return this.fountainLetters;
            }
        }

        public bool FountainStuddyCompleted
        {
            get
            {
                return this.fountainLetters.Count == 3;
            }
        }

        public int MonolithNextIndex
        {
            get
            {
                return this.fountainNextIndex;
            }
        }

        public int MonolithStudyProgress
        {
            get
            {
                return this.fountainStudyProgress;
            }
        }

        public bool FountainStudyEnabled
        {
            get
            {
                return this.Level > 0;
            }
        }


        // Token: 0x0600B670 RID: 46704 RVA: 0x004155C0 File Offset: 0x004137C0
        public GameComponent_RegressionGame(Game game)
        {
            this.levelDef = DefDatabase<FountainOfYouthLevelDef>.AllDefs.FirstOrDefault((FountainOfYouthLevelDef x) => x.level == this.level);
            this.level = this.levelDef.level;
            this.fountainSpawnParams = new LargeBuildingSpawnParms
            {
                minDistanceToColonyBuilding = 30f,
                minDistToEdge = 10,
                attemptSpawnLocationType = SpawnLocationType.Outdoors,
                attemptNotUnderBuildings = true,
                canSpawnOnImpassable = false,
                allowFogged = true,
                overrideSize = new IntVec2?(new IntVec2(ThingDefOf.FountainOfYouth.size.x + 2, ThingDefOf.FountainOfYouth.size.z + 2))
            };
        }

        // Token: 0x0600B671 RID: 46705 RVA: 0x00415718 File Offset: 0x00413918
        public override void StartedNewGame()
        {
            base.StartedNewGame();
            this.Notify_LevelChanged(true);
            this.fountain = (Find.AnyPlayerHomeMap.listerThings.ThingsOfDef(ThingDefOf.FountainOfYouth).FirstOrDefault<Thing>() as Building_FountainOfYouth);
        }

        // Token: 0x0600B672 RID: 46706 RVA: 0x00415788 File Offset: 0x00413988
        public override void GameComponentTick()
        {
            if (this.fountain != null && this.fountain.quest == null && this.Level > 0)
            {
                this.fountain.CheckAndGenerateQuest();
            }
            if (this.fountain != null && !this.fountain.Spawned)
            {
                this.fountain = null;
            }
           
            this.UpdateHypnotized();
        }

        // Token: 0x0600B674 RID: 46708 RVA: 0x00415C81 File Offset: 0x00413E81
        public void Notify_MonolithStudyIncreased(ChoiceLetter letter, int nextIndex, int studyProgress)
        {
            this.fountainLetters.Add(letter);
            this.fountainNextIndex = nextIndex;
            this.fountainStudyProgress = studyProgress;
        }

        // Token: 0x0600B675 RID: 46709 RVA: 0x00415CA0 File Offset: 0x00413EA0
        private void UpdateHypnotized()
        {
            foreach (KeyValuePair<Pawn, int> tuple in this.hypnotisedPawns)
            {
                Pawn pawn;
                int num;
                tuple.Deconstruct(out pawn, out num);
                Pawn item = pawn;
                int num2 = num;
                if (GenTicks.TicksGame > num2)
                {
                    toRemove.Add(item);
                }
            }
            foreach (Pawn key in toRemove)
            {
                this.hypnotisedPawns.Remove(key);
            }
            toRemove.Clear();
        }



        // Token: 0x0600B678 RID: 46712 RVA: 0x00415F15 File Offset: 0x00414115
        public void IncrementLevel()
        {
            if (this.LevelDef == FountainOfYouthLevelDefOf.Inactive)
            {
                this.level = 0;
            }
            this.level++;
            this.Notify_LevelChanged(false);
        }

        // Token: 0x0600B679 RID: 46713 RVA: 0x00415F40 File Offset: 0x00414140
        public void Hypnotize(Pawn pawn, Pawn instigator, int ticks)
        {
            Find.BattleLog.Add(new BattleLogEntry_Event(pawn, RulePackDefOf.Event_Hypnotized, instigator));
            this.hypnotisedPawns[pawn] = GenTicks.TicksGame + ticks;
        }

        // Token: 0x0600B67A RID: 46714 RVA: 0x00415F6B File Offset: 0x0041416B
        public void EndHypnotize(Pawn pawn)
        {
            if (this.hypnotisedPawns.ContainsKey(pawn))
            {
                this.hypnotisedPawns.Remove(pawn);
            }
        }

        // Token: 0x0600B67B RID: 46715 RVA: 0x00415F88 File Offset: 0x00414188
        public bool IsPawnHypnotized(Pawn pawn)
        {
            return this.hypnotisedPawns.ContainsKey(pawn);
        }

        // Token: 0x0600B67C RID: 46716 RVA: 0x00415F96 File Offset: 0x00414196
        public void SetLevel(FountainOfYouthLevelDef levelDef, bool silent = false)
        {
            int num = this.level;
            this.level = levelDef.level;
            if (num != this.level)
            {
                this.Notify_LevelChanged(silent);
            }
        }

        // Token: 0x0600B67D RID: 46717 RVA: 0x00415FBC File Offset: 0x004141BC
        private void Notify_LevelChanged(bool silent = false)
        {
            if (!ModLister.CheckAnomaly("Monolith level"))
            {
                return;
            }
            this.highestLevelReached = Mathf.Max(this.highestLevelReached, this.level);
            this.lastLevelChangeTick = Find.TickManager.TicksGame;
            this.levelDef = DefDatabase<FountainOfYouthLevelDef>.AllDefs.FirstOrDefault((FountainOfYouthLevelDef x) => x.level == this.level);
            Building_FountainOfYouth buildin_FountainOfYouth = this.fountain;
            if (this.fountain != null)
            {
                this.fountain.SetLevel(this.levelDef);
            }
            Find.ResearchManager.Notify_MonolithLevelChanged(this.level);
            if (!silent)
            {
                if (this.Level > 0)
                {
                    Find.CameraDriver.shaker.DoShake(0.05f, 300);
                }
            }
            Find.SignalManager.SendSignal(new Signal("MonolithLevelChanged", true));
            if (this.level == 1)
            {
                LessonAutoActivator.TeachOpportunity(ConceptDefOf.StudyingEntities, OpportunityType.Important);
                LessonAutoActivator.TeachOpportunity(ConceptDefOf.EntityCodex, OpportunityType.GoodToKnow);
                IncidentParms parms = new IncidentParms
                {
                    target = this.fountain.Map,
                    points = StorytellerUtility.DefaultThreatPointsNow(this.fountain.Map),
                    forced = true
                };
                Find.Storyteller.incidentQueue.Add(IncidentDefOf.VoidCuriosity, Find.TickManager.TicksGame + Mathf.RoundToInt(GameComponent_RegressionGame.RegressionCuriosityIncidentDelayRangeDays.RandomInRange * 60000f), parms, 0);
            }
        }


        private void TriggerVoidAwakening()
        {
            Slate slate = new Slate();
            slate.Set<Map>("map", this.fountain.Map, false);
            QuestUtility.GenerateQuestAndMakeAvailable(QuestScriptDefOf.EndGame_VoidAwakening, slate);
        }

        public bool TryGetCellForMonolithSpawn(Map map, out IntVec3 cell)
        {
            LargeBuildingSpawnParms largeBuildingSpawnParms = this.FountainSpawnParams.ForThing(ThingDefOf.FountainOfYouth);
            LargeBuildingSpawnParms parms = largeBuildingSpawnParms;
            parms.minDistanceToColonyBuilding = 1f;
            return LargeBuildingCellFinder.TryFindCell(out cell, map, largeBuildingSpawnParms, null, (IntVec3 c) => ScattererValidator_AvoidSpecialThings.IsValid(c, map), false) || LargeBuildingCellFinder.TryFindCell(out cell, map, parms, null, (IntVec3 c) => ScattererValidator_AvoidSpecialThings.IsValid(c, map), false);
        }
        public Building_FountainOfYouth SpawnNewMonolith(IntVec3 cell, Map map)
        {
            this.fountain = (ThingMaker.MakeThing(ThingDefOf.FountainOfYouth, null) as Building_FountainOfYouth);
            this.levelDef = DefDatabase<FountainOfYouthLevelDef>.AllDefs.FirstOrDefault((FountainOfYouthLevelDef x) => x.level == this.level);
            GenSpawn.Spawn(this.fountain, cell, map, WipeMode.Vanish);
            EffecterDefOf.VoidStructureSpawningSkipSequence.SpawnMaintained(this.fountain, map, 1f);
            if (this.Level > 0)
            {
                this.fountain.CheckAndGenerateQuest();
            }
            return this.fountain;
        }

        public void ResetMonolith()
        {
            this.SetLevel(FountainOfYouthLevelDefOf.Inactive, false);
            if (this.fountain != null)
            {
                this.fountain.Reset();
            }
            this.lastLevelChangeTick = -99999;
        }

        // Token: 0x0600B683 RID: 46723 RVA: 0x004162EC File Offset: 0x004144EC
        public bool VoidAwakeningActive()
        {
            foreach (Quest quest in Find.QuestManager.questsInDisplayOrder)
            {
                if (!quest.Historical && quest.root == QuestScriptDefOf.EndGame_VoidAwakening)
                {
                    return true;
                }
            }
            return false;
        }


        public void Notify_MapRemoved(Map map)
        {
            if (this.fountain == null || this.fountain.Map != map)
            {
                return;
            }
            if (this.fountain.quest != null && !this.fountain.quest.Historical)
            {
                this.fountain.quest.End(QuestEndOutcome.Unknown, true, true);
            }

            this.fountain = null;
        }

        // Token: 0x0600B690 RID: 46736 RVA: 0x0041671D File Offset: 0x0041491D
        public override void AppendDebugString(StringBuilder sb)
        {
            sb.AppendLine("  " + base.GetType().Name + ":");
            sb.AppendLine("    level:  " + this.level);
        }

        // Token: 0x0600B691 RID: 46737 RVA: 0x0041675C File Offset: 0x0041495C
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                foreach (KeyValuePair<Pawn, int> tuple in (from kvp in this.hypnotisedPawns
                                                           where kvp.Key.DestroyedOrNull() || kvp.Key.Dead
                                                           select kvp).ToList<KeyValuePair<Pawn, int>>())
                {
                    Pawn pawn;
                    int num;
                    tuple.Deconstruct(out pawn, out num);
                    Pawn key = pawn;
                    this.hypnotisedPawns.Remove(key);
                }
            }
            Scribe_Values.Look<int>(ref level, "level", 0, false);
            Scribe_Values.Look<int>(ref highestLevelReached, "highestLevelReached", 0, false);
            Scribe_Defs.Look<FountainOfYouthLevelDef>(ref levelDef, "levelDef");
            Scribe_References.Look<Building_FountainOfYouth>(ref fountain, "fountain", false);
            Scribe_Values.Look<int>(ref lastLevelChangeTick, "lastLevelChangeTick", 0, false);
            Scribe_Values.Look<int>(ref lastLevelActivationLetterSent, "lastLevelActivationLetterSent", -1, false);
            Scribe_References.Look<Pawn>(ref voidNodeActivator, "voidNodeActivator", false);
            Scribe_Values.Look<bool>(ref this.hasBuiltHoldingPlatform, "hasBuiltHoldingPlatform", false, false);
            Scribe_Collections.Look<Pawn, int>(ref this.hypnotisedPawns, "hypnotisedPawns", LookMode.Reference, LookMode.Value, ref this.workingHypnotizedList, ref this.workingHypnotizedTickList, true, false, false);
            Scribe_Collections.Look<ChoiceLetter>(ref this.fountainLetters, "fountainLetters", LookMode.Deep, Array.Empty<object>());
            Scribe_Values.Look<int>(ref this.fountainStudyProgress, "fountainStudyProgress", 0, false);
            Scribe_Values.Look<int>(ref this.fountainNextIndex, "fountainNextIndex", 0, false);
            Scribe_Values.Look<float>(ref this.fountainAnomalyKnowledge, "fountainAnomalyKnowledge", 0f, false);
        }

        // Token: 0x0600B692 RID: 46738 RVA: 0x004169F0 File Offset: 0x00414BF0
        public const string MonolithLevelChangedSignal = "MonolithLevelChanged";

        private const int LevelChangeScreenShakeDuration = 300;

        private const int MonolithStudyNoteLetters = 3;

        private const float LevelChangeScreenShakeMagnitude = 0.05f;

        public const int PostEndGameReliefPeriod = 300000;

        private static readonly FloatRange MonolithLevelIncidentDelayRangeHours = new FloatRange(12f, 36f);

        private static readonly FloatRange GrayPallConditionDaysRange = new FloatRange(1f, 3f);

        private static readonly FloatRange RegressionCuriosityIncidentDelayRangeDays = new FloatRange(15f, 20f);

        private int level;

        private int highestLevelReached;

        private FountainOfYouthLevelDef levelDef;

        public Building_FountainOfYouth fountain;

        private int lastLevelChangeTick = -99999;

        public bool hasBuiltHoldingPlatform;

        private Dictionary<Pawn, int> hypnotisedPawns = new Dictionary<Pawn, int>();

        public int lastLevelActivationLetterSent = -1;

        private List<ChoiceLetter> fountainLetters = new List<ChoiceLetter>();

        private int fountainNextIndex;

        private int fountainStudyProgress;

        public float fountainAnomalyKnowledge;

        public Pawn voidNodeActivator;

        private LargeBuildingSpawnParms fountainSpawnParams;

        private List<Pawn> workingPawnList;

        private List<Pawn> workingHypnotizedList;

        private List<int> workingHypnotizedTickList;

        // Token: 0x040071F5 RID: 29173
        private static readonly List<Pawn> toRemove = new List<Pawn>();
    }
}

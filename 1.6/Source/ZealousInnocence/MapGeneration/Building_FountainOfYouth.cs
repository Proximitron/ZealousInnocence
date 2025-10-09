using RimWorld.QuestGen;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse.AI;
using Verse;
using Verse.Sound;

namespace ZealousInnocence
{
    public class Building_FountainOfYouth : Building, ITargetingSource, IThingGlower
    {

        public bool IsAutoActivating
        {
            get
            {
                return this.autoActivateTick > 0 && Find.TickManager.TicksGame > this.autoActivateTick - 300000;
            }
        }

        public int TicksUntilAutoActivate
        {
            get
            {
                return this.autoActivateTick - Find.TickManager.TicksGame;
            }
        }

        private TargetInfo EffecterInfo
        {
            get
            {
                return new TargetInfo(base.Position, base.Map, false);
            }
        }

        public override CellRect? CustomRectForSelector
        {
            get
            {
                return new CellRect?(GenAdj.OccupiedRect(base.Position, Rot4.North, regression.LevelDef.sizeIncludingAttachments ?? this.def.Size));
            }
        }

        public override Texture UIIconOverride
        {
            get
            {
                return regression.LevelDef.UIIcon;
            }
        }

        public override string LabelNoCount
        {
            get
            {
                if (regression.LevelDef.fountainLabel != null)
                {
                    return regression.LevelDef.fountainLabel;
                }
                return base.LabelNoCount;
            }
        }

        public override string DescriptionFlavor
        {
            get
            {
                return base.DescriptionFlavor;
            }
        }

        public bool ShouldBeLitNow()
        {
            return false;
        }


        public bool CasterIsPawn
        {
            get
            {
                return true;
            }
        }

        public bool IsMeleeAttack
        {
            get
            {
                return false;
            }
        }

        public bool Targetable
        {
            get
            {
                return true;
            }
        }

        public bool MultiSelect
        {
            get
            {
                return false;
            }
        }

        public bool HidePawnTooltips
        {
            get
            {
                return false;
            }
        }

        public Thing Caster
        {
            get
            {
                return this;
            }
        }

        public Pawn CasterPawn
        {
            get
            {
                return null;
            }
        }

        public Verb GetVerb
        {
            get
            {
                return null;
            }
        }

        public TargetingParameters targetParams
        {
            get
            {
                return Building_FountainOfYouth.targetParmsInt;
            }
        }

        public virtual ITargetingSource DestinationSelector
        {
            get
            {
                return null;
            }
        }

        public Texture2D UIIcon
        {
            get
            {
                return ContentFinder<Texture2D>.Get("UI/Commands/ActivateMonolith", true);
            }
        }

        public GameComponent_RegressionGame regression
        {
            get
            {
                return Current.Game.GetComponent<GameComponent_RegressionGame>();
            }
        }

        // Token: 0x06008AF3 RID: 35571 RVA: 0x002F7684 File Offset: 0x002F5884
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Quest>(ref this.quest, "quest", false);
            Scribe_Values.Look<int>(ref this.disturbingVisionTick, "disturbingVisionTick", 0, false);
            Scribe_Values.Look<int>(ref this.autoActivateTick, "autoActivateTick", 0, false);
            Scribe_Collections.Look<Thing>(ref this.fountainAttachements, "fountainAttachements", LookMode.Reference, Array.Empty<object>());
            Scribe_Values.Look<int>(ref this.activatedDialogTick, "activatedDialogTick", 0, false);
            Scribe_References.Look<Pawn>(ref this.activatorPawn, "activatorPawn", false);
            Scribe_References.Look<Thing>(ref this.pyramidThing, "pyramidThing", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.fountainAttachements == null)
            {
                this.fountainAttachements = new List<Thing>();
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            if (!respawningAfterLoad)
            {
                if (regression.Level > 0)
                {
                    this.TryGetComp<CompProximityLetter>().letterSent = true;
                }
                if (regression.fountain == null)
                {
                    regression.fountain = this;
                }
                this.disturbingVisionTick = Find.TickManager.TicksGame + Mathf.RoundToInt(DisturbingVisionRangeDaysRange.RandomInRange * 60000f);
            }
            base.SpawnSetup(map, respawningAfterLoad);
            this.UpdateAttachments();
        }

        protected override void Tick()
        {
            base.Tick();
            if (GenTicks.IsTickInterval(30))
            {
                if (regression.Level == 0 && !base.Map.reservationManager.IsReserved(this))
                {
                    if (this.level0HintEffecter == null)
                    {
                        this.level0HintEffecter = EffecterDefOf.MonolithL0Glow.Spawn(this.EffecterInfo, this.EffecterInfo, 1f);
                    }
                }
                else
                {
                    Effecter effecter = this.level0HintEffecter;
                    if (effecter != null)
                    {
                        effecter.Cleanup();
                    }
                    this.level0HintEffecter = null;
                }
            }
            Effecter effecter2 = this.level0HintEffecter;
            if (effecter2 != null)
            {
                effecter2.EffectTick(this.EffecterInfo, this.EffecterInfo);
            }
            if (regression.Level == 0 && this.disturbingVisionTick > 0 && Find.TickManager.TicksGame > this.disturbingVisionTick)
            {
                Pawn arg;
                if (base.Map.mapPawns.FreeColonistsSpawned.TryRandomElement(out arg))
                {
                    Find.LetterStack.ReceiveLetter("VoidMonolithVisionLabel".Translate(), "VoidMonolithVisionText".Translate(arg.Named("PAWN")), LetterDefOf.NeutralEvent, this, null, null, null, null, 0, true);
                    this.disturbingVisionTick = -99999;
                }
                else
                {
                    this.disturbingVisionTick += 15000;
                }
            }
            if (this.autoActivateTick > 0)
            {
                if (Find.TickManager.TicksGame == this.autoActivateTick - 300000)
                {
                    Find.LetterStack.ReceiveLetter("MonolithAutoActivatingLabel".Translate(), "MonolithAutoActivatingText".Translate(), LetterDefOf.NegativeEvent, this, null, null, null, null, 0, true);
                }
                if (Find.TickManager.TicksGame == this.autoActivateTick - 60)
                {
                    Find.LetterStack.ReceiveLetter("MonolithAutoActivatedLabel".Translate(), "MonolithAutoActivatedText".Translate(), LetterDefOf.ThreatBig, this, null, null, null, null, 0, true);
                }
                Pawn pawn;
                if (Find.TickManager.TicksGame > this.autoActivateTick && base.Map.mapPawns.FreeColonists.TryRandomElement(out pawn))
                {
                    this.Activate(pawn);
                }
            }
            if (this.IsAutoActivating)
            {
                if (this.autoActivateEffecter == null)
                {
                    this.autoActivateEffecter = EffecterDefOf.MonolithAutoActivating.Spawn();
                }
                this.autoActivateEffecter.EffectTick(this.EffecterInfo, this.EffecterInfo);
            }
            else if (this.autoActivateEffecter != null)
            {
                this.autoActivateEffecter.Cleanup();
                this.autoActivateEffecter = null;
            }
            /*if (Find.Anomaly.Level == MonolithLevelDefOf.Gleaming.level && Find.CurrentMap == base.MapHeld)
            {
                if (this.gleamingEffecter == null)
                {
                    this.gleamingEffecter = EffecterDefOf.MonolithGleaming_Sustained.Spawn();
                }
                this.gleamingEffecter.EffectTick(this.EffecterInfo, this.EffecterInfo);
                if (this.gleamingVoidNodeEffecter == null)
                {
                    this.gleamingVoidNodeEffecter = EffecterDefOf.MonolithGleamingVoidNode.Spawn(this, base.Map, 1f);
                }
                this.gleamingVoidNodeEffecter.EffectTick(this.EffecterInfo, this.EffecterInfo);
            }
            else if (this.gleamingEffecter != null || Find.CurrentMap != base.MapHeld)
            {
                Effecter effecter3 = this.gleamingEffecter;
                if (effecter3 != null)
                {
                    effecter3.Cleanup();
                }
                this.gleamingEffecter = null;
                Effecter effecter4 = this.gleamingVoidNodeEffecter;
                if (effecter4 != null)
                {
                    effecter4.Cleanup();
                }
                this.gleamingVoidNodeEffecter = null;
            }*/
            if (this.activatedDialogTick > 0 && Find.TickManager.TicksGame > this.activatedDialogTick)
            {
                this.OpenActivatedDialog();
            }
        }

        public bool CanActivate(out string reason, out string reasonShort)
        {
            reason = "";
            reasonShort = "";
            if (!regression.LevelDef.advanceThroughActivation)
            {
                return false;
            }
            var nextLevelDef = regression.NextLevelDef;
            if (nextLevelDef == null)
            {
                return false;
            }
            /*if (nextLevelDef.entityCatagoryCompletionRequired != null && Find.EntityCodex.DiscoveredCount(nextLevelDef.entityCatagoryCompletionRequired) < nextLevelDef.entityCountCompletionRequired)
            {
                int value = nextLevelDef.entityCountCompletionRequired - Find.EntityCodex.DiscoveredCount(nextLevelDef.entityCatagoryCompletionRequired);
                reason = string.Format("{0}:\n  - {1}", "VoidMonolithRequiresDiscovery".Translate(), "VoidMonolithRequiresCategory".Translate(value, nextLevelDef.entityCatagoryCompletionRequired.label));
                reasonShort = "VoidMonolithRequiresDiscoveryShort".Translate();
                return false;
            }*/
            foreach (GameCondition gameCondition in base.Map.GameConditionManager.ActiveConditions)
            {
                List<GameConditionDef> unreachableDuringConditions = nextLevelDef.unreachableDuringConditions;
                if (unreachableDuringConditions != null && unreachableDuringConditions.Contains(gameCondition.def))
                {
                    reason = gameCondition.def.LabelCap;
                    reasonShort = gameCondition.def.LabelCap;
                    return false;
                }
            }
            return true;
        }
        public void Activate(Pawn pawn)
        {
            this.CheckAndGenerateQuest();
            regression.IncrementLevel();
            EffecterDefOf.MonolithLevelChanged.Spawn().Trigger(this.EffecterInfo, this.EffecterInfo, -1);
            this.activatedDialogTick = Find.TickManager.TicksGame + 360;
            
            this.activatorPawn = pawn;
            
            if (regression.Level == 1)
            {
                Helper_Bedwetting.ForceBedwetting(pawn);
                CompStudiable comp = base.GetComp<CompStudiable>();
                if (comp != null)
                {
                    comp.SetStudyEnabled(true);
                }
            }
            else if (regression.Level == 2)
            {
                Helper_Bedwetting.RandomizeBedwettingSeed(pawn);
                var removedHediffs = new List<Hediff>();
                var pawnAgeDelta = 0f;
                Helper_Regression.regressOrReincarnateToChild(pawn, this, false, out removedHediffs,out pawnAgeDelta);

            }
            this.autoActivateTick = -99999;
        }

        public void CheckAndGenerateQuest()
        {
            if (regression.fountain == null)
            {
                regression.fountain = this;
            }
        }

        private void OpenActivatedDialog()
        {
           
            DiaNode diaNode = new DiaNode(this.regression.LevelDef.activatedLetterText.Formatted(this.activatorPawn.Named("PAWN")));
            diaNode.options.Add(new DiaOption("VoidMonolithViewQuest".Translate()) // "View quest"
            {
                action = delegate ()
                {
                    Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Quests, true);
                    ((MainTabWindow_Quests)MainButtonDefOf.Quests.TabWindow).Select(this.quest);
                },
                resolveTree = true
            });
            // TODO: Research
            /*if (regression.Level < 2)
            {
                List<DiaOption> options = diaNode.options;
                DiaOption diaOption = new DiaOption("FountainOfYouthViewResearch".Translate());
                diaOption.action = delegate ()
                {
                    Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Research, true);
                    ((MainTabWindow_Research)MainButtonDefOf.Research.TabWindow).CurTab = ResearchTabDefOf.Regression;
                };
                diaOption.resolveTree = true;
                options.Add(diaOption);
            }*/
            diaNode.options.Add(new DiaOption("Close".Translate())
            {
                resolveTree = true
            });
            Dialog_NodeTree dialog_NodeTree = new Dialog_NodeTree(diaNode, false, false, null);
            dialog_NodeTree.forcePause = true;
            Find.WindowStack.Add(dialog_NodeTree);
            this.activatedDialogTick = -99999;
            this.activatorPawn = null;
        }

        public void Investigate(Pawn pawn)
        {
            Dialog_NodeTree dialog_NodeTree = new Dialog_NodeTree(new DiaNode("FountainOfYouthActivateConfirmationText".Translate(pawn.Named("PAWN")))
            {
                options =
                {
                    new DiaOption("FountainOfYouthInvestigate".Translate(pawn.Named("PAWN")))
                    {
                        action = delegate()
                        {
                            if (pawn != null)
                            {
                                Job job = JobMaker.MakeJob(JobDefOf.FountainOfYouthActivate, this);
                                pawn.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
                            }
                        },
                        resolveTree = true
                    },
                    new DiaOption("FountainOfYouthWalkAway".Translate())
                    {
                        resolveTree = true
                    }
                }
            }, false, false, null);
            dialog_NodeTree.forcePause = true;
            Find.WindowStack.Add(dialog_NodeTree);
        }

        public void SetLevel(FountainOfYouthLevelDef levelDef)
        {
            CompGlower comp = base.GetComp<CompGlower>();
            int monolithGlowRadiusOverride = levelDef.fountainGlowRadiusOverride;
            if (monolithGlowRadiusOverride != -1)
            {
                comp.GlowRadius = (float)monolithGlowRadiusOverride;
            }
            comp.UpdateLit(base.Map);
            if (comp.Glows)
            {
                comp.ForceRegister(base.Map);
            }
            if (base.Spawned)
            {
                this.UpdateAttachments();
                base.DirtyMapMesh(base.Map);
            }
            if (regression.LevelDef.activatedSound != null)
            {
                regression.LevelDef.activatedSound.PlayOneShot(this);
            }
        }

        // Token: 0x06008AFC RID: 35580 RVA: 0x002F8074 File Offset: 0x002F6274
        private void UpdateAttachments()
        {
            /*TerrainThreshold terrainThreshold = base.Map.Biome.terrainsByFertility.Find((TerrainThreshold t) => t.terrain.affordances.Contains(this.def.terrainAffordanceNeeded));
            TerrainDef newTerr = ((terrainThreshold != null) ? terrainThreshold.terrain : null) ?? TerrainDefOf.Soil;
            Thing.allowDestroyNonDestroyable = true;
            foreach (Thing thing in this.monolithAttachments)
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
            Thing.allowDestroyNonDestroyable = false;
            this.monolithAttachments.Clear();
            if (Find.Anomaly.LevelDef.attachments == null)
            {
                return;
            }
            foreach (MonolithAttachment monolithAttachment in Find.Anomaly.LevelDef.attachments)
            {
                IntVec3 intVec = base.Position + monolithAttachment.offset.ToIntVec3;
                foreach (IntVec3 c in GenAdj.OccupiedRect(intVec, Rot4.North, monolithAttachment.def.Size))
                {
                    if (!c.GetTerrain(base.Map).affordances.Contains(this.def.terrainAffordanceNeeded))
                    {
                        base.Map.terrainGrid.RemoveTopLayer(c, false);
                        base.Map.terrainGrid.SetTerrain(c, newTerr);
                    }
                }
                Thing thing2 = ThingMaker.MakeThing(monolithAttachment.def, null);
                thing2.TryGetComp<CompSelectProxy>().thingToSelect = this;
                GenSpawn.Spawn(thing2, intVec, base.Map, Rot4.North, WipeMode.FullRefund, false, true);
                thing2.overrideGraphicIndex = new int?(monolithAttachment.graphicIndex);
                thing2.DirtyMapMesh(base.Map);
                this.monolithAttachments.Add(thing2);
            }*/
        }

        // Token: 0x06008AFD RID: 35581 RVA: 0x002F82B8 File Offset: 0x002F64B8
        public void Collapse()
        {
            /*Map map = Find.Anomaly.monolith.Map;
            Predicate<IntVec3> validator = null;
            for (int i = 0; i < 3; i++)
            {
                IntVec3 position = Find.Anomaly.monolith.Position;
                Map map3 = map;
                int squareRadius = CollapseScatterRadius;

                if (validator == null)
                {
                    validator = (IntVec3 c) => c.Standable(map);
                }
                IntVec3 loc;
                if (CellFinder.TryFindRandomCellNear(position, map3, squareRadius, validator, out loc, -1))
                {
                    GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.MonolithFragment, null), loc, Find.Anomaly.monolith.Map, WipeMode.Vanish);
                }
            }
            int j = 10;

            validator = null;
            while (j > 0)
            {
                IntVec3 position2 = Find.Anomaly.monolith.Position;
                Map map2 = map;
                int squareRadius2 = CollapseScatterRadius;

                if (validator == null)
                {
                    validator = (IntVec3 c) => c.Standable(map);
                }
                IntVec3 loc2;
                if (CellFinder.TryFindRandomCellNear(position2, map2, squareRadius2, validator, out loc2, -1))
                {
                    Thing thing = ThingMaker.MakeThing(ThingDefOf.Shard, null);
                    thing.stackCount = Mathf.Min(Rand.RangeInclusive(1, 2), j);
                    j -= thing.stackCount;
                    GenSpawn.Spawn(thing, loc2, Find.Anomaly.monolith.Map, WipeMode.Vanish);
                }
            }*/
        }

        // Token: 0x06008AFE RID: 35582 RVA: 0x002F83E4 File Offset: 0x002F65E4
        public void Reset()
        {
            this.quest = null;
            base.GetComp<CompVoidStructure>().Reset();
            this.SetLevel(regression.LevelDef);
        }

        public bool CanHitTarget(LocalTargetInfo target)
        {
            return this.ValidateTarget(target, false);
        }

        // Token: 0x06008B00 RID: 35584 RVA: 0x002F8414 File Offset: 0x002F6614
        public bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!target.IsValid || target.Pawn == null)
            {
                return false;
            }
            if (target.Pawn.Downed)
            {
                if (showMessages)
                {
                    Messages.Message("VoidMonolithActivatorDowned".Translate(), target.Pawn, MessageTypeDefOf.RejectInput, false); // "Target is down"
                }
                return false;
            }
            if (target.Pawn.InMentalState)
            {
                if (showMessages)
                {
                    Messages.Message("VoidMonolithActivatorMentalState".Translate(), target.Pawn, MessageTypeDefOf.RejectInput, false); // "Target in mental state"
                }
                return false;
            }
            if (!target.Pawn.CanCasuallyInteractNow(false, false, false, true))
            {
                if (showMessages)
                {
                    Messages.Message("VoidMonolithActivatorBusy".Translate(), target.Pawn, MessageTypeDefOf.RejectInput, false); // "Target is busy"
                }
                return false;
            }
            if (!target.Pawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
            {
                if (showMessages)
                {
                    Messages.Message("NoPath".Translate(), target.Pawn, MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            return true;
        }

        public void DrawHighlight(LocalTargetInfo target)
        {
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }

        public void OrderForceTarget(LocalTargetInfo target)
        {
            if (this.ValidateTarget(target, false))
            {
                Pawn pawn = target.Pawn;
                if (Find.Anomaly.Level == 0)
                {
                    Job job = JobMaker.MakeJob(JobDefOf.FountainOfYouthInvestigate, this);
                    pawn.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
                    Messages.Message("FountainOfYouthInvestigate".Translate(pawn.Named("PAWN")), this, MessageTypeDefOf.NeutralEvent, true);
                    this.TryGetComp<CompProximityLetter>().letterSent = true;
                    return;
                }
                this.OrderActivation(pawn, true);
            }
        }

        private void OrderActivation(Pawn pawn, bool sendMessage)
        {
            Job job = JobMaker.MakeJob(JobDefOf.FountainOfYouthActivate, this);
            pawn.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
            if (sendMessage)
            {
                Messages.Message(Find.Anomaly.LevelDef.pawnSentToActivateMessage.Formatted(pawn.Named("PAWN")), this, MessageTypeDefOf.NeutralEvent, true);
            }
        }

        public void AutoActivate(int tick)
        {
            this.autoActivateTick = tick;
            this.TryGetComp<CompProximityLetter>().letterSent = true;
            this.disturbingVisionTick = -99999;
        }

        public void OnGUI(LocalTargetInfo target)
        {
            Widgets.MouseAttachedLabel("VoidMonolithChooseActivator".Translate(), 0f, 0f); // "Choose who should do this"
            if (this.ValidateTarget(target, false) && this.targetParams.CanTarget(target.Pawn, this))
            {
                GenUI.DrawMouseAttachment(this.UIIcon);
                return;
            }
            GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
        }

        public override string GetInspectString()
        {
            string inspectString = base.GetInspectString();
            StringBuilder stringBuilder = new StringBuilder();
            if (!regression.LevelDef.levelInspectText.NullOrEmpty())
            {
                stringBuilder.Append(regression.LevelDef.levelInspectText);
            }
            if (regression.Level > 0)
            {
                string text;
                string text2;
                if (this.CanActivate(out text, out text2))
                {
                    stringBuilder.AppendLineIfNotEmpty().Append(regression.LevelDef.fountainCanBeActivatedText);
                }
                else if (!text.NullOrEmpty())
                {
                    stringBuilder.AppendLineIfNotEmpty().Append(text);
                }
            }
            else
            {
                stringBuilder.AppendLineIfNotEmpty().Append("VoidMonolithUndiscovered".Translate()); // "Investigate to learn more."
            }
            if (!inspectString.NullOrEmpty())
            {
                stringBuilder.AppendLineIfNotEmpty().Append(inspectString);
            }
            return stringBuilder.ToString();
        }

        // Token: 0x06008B07 RID: 35591 RVA: 0x002F87A2 File Offset: 0x002F69A2
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            IEnumerator<Gizmo> enumerator = null;
            foreach (Gizmo gizmo2 in QuestUtility.GetQuestRelatedGizmos(this))
            {
                yield return gizmo2;
            }
            enumerator = null;
            if (regression.LevelDef.advanceThroughActivation) //Find.Anomaly.LevelDef.advanceThroughActivation
            {
                string text = null;
                string text2;
                string text3;
                if (!this.CanActivate(out text2, out text3))
                {
                    text = text2;
                }
                Command_Action command_Action = new Command_Action
                {
                    defaultLabel = regression.LevelDef.activateGizmoText.CapitalizeFirst() + "...",
                    defaultDesc = regression.LevelDef.activateGizmoDescription,
                    icon = this.UIIcon,
                    Disabled = !text.NullOrEmpty(),
                    disabledReason = text,
                    action = delegate ()
                    {
                        RimWorld.SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                        Find.Targeter.BeginTargeting(this, null, false, null, null, true);
                    }
                };
                yield return command_Action;
                if (DebugSettings.ShowDevGizmos)
                {
                    if (base.Map.mapPawns.AnyFreeColonistSpawned)
                    {
                        yield return new Command_Action
                        {
                            defaultLabel = "DEV: Activate",
                            action = delegate ()
                            {
                                Pawn pawn;
                                if (base.Map.mapPawns.FreeColonists.TryRandomElement(out pawn))
                                {
                                    this.Activate(pawn);
                                }
                            }
                        };
                    }
                    if (!regression.FountainSpawned)
                    {
                        yield return new Command_Action
                        {
                            defaultLabel = "DEV: Relink fountain",
                            action = delegate ()
                            {
                                regression.fountain = this;
                            }
                        };
                    }
                }
            }
            yield break;
            yield break;
        }

        // Token: 0x06008B08 RID: 35592 RVA: 0x002F87B2 File Offset: 0x002F69B2
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(selPawn))
            {
                yield return floatMenuOption;
            }
            IEnumerator<FloatMenuOption> enumerator = null;
            if (regression.Level == 0)
            {
                yield return new FloatMenuOption(regression.LevelDef.activateFloatMenuText.Formatted(this.Label).CapitalizeFirst(), delegate ()
                {
                    Job job = JobMaker.MakeJob(JobDefOf.FountainOfYouthInvestigate, this);
                    selPawn.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
                    this.TryGetComp<CompProximityLetter>().letterSent = true;
                }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            string text;
            string t;
            if (this.CanActivate(out text, out t))
            {
                if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
                {
                    yield return new FloatMenuOption("FountainOfYouthCantActivate".Translate(regression.LevelDef.activateGizmoText) + ": " + "NoPath".Translate().CapitalizeFirst(), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                }
                else
                {
                    yield return new FloatMenuOption(regression.LevelDef.activateFloatMenuText.Formatted(this.Label).CapitalizeFirst(), delegate ()
                    {
                        this.OrderActivation(selPawn, false);
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                }
            }
            else if (regression.LevelDef.advanceThroughActivation)
            {
                yield return new FloatMenuOption("FountainOfYouthCantActivate".Translate(regression.LevelDef.activateGizmoText) + ": " + t, null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
            }
            yield break;
            yield break;
        }


        private static readonly FloatRange DisturbingVisionRangeDaysRange = new FloatRange(13f, 16f);

        private const int DisturbingVisionRetryTicks = 15000;

        public const int ActivateLetterDelayTicks = 360;

        private const int CollapseScatterRadius = 5;

        public Quest quest;

        private int disturbingVisionTick = -99999;

        private int autoActivateTick = -99999;

        private List<Thing> fountainAttachements = new List<Thing>();

        private Thing pyramidThing;

        private int activatedDialogTick = -99999;

        private Pawn activatorPawn;


        private Effecter autoActivateEffecter;

        private Effecter level0HintEffecter;

        private static readonly TargetingParameters targetParmsInt = new TargetingParameters
        {
            canTargetBuildings = false,
            canTargetAnimals = false,
            canTargetMechs = false,
            onlyTargetColonists = true
        };
    }
}

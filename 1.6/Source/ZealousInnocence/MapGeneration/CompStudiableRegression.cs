using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ZealousInnocence
{
    public class CompStudiableRegression : CompStudiable
    {
        public override float AnomalyKnowledge
        {
            get
            {
                /*if (!Find.Anomaly.QuestlineEnded)
                {
                    return base.AnomalyKnowledge;
                }
                return 4f;
                */
                return base.AnomalyKnowledge;
            }
        }
        public GameComponent_RegressionGame regression
        {
            get
            {
                return Current.Game.GetComponent<GameComponent_RegressionGame>();
            }
        }

        public override KnowledgeCategoryDef KnowledgeCategory
        {
            get
            {
                return regression.LevelDef.monolithStudyCategory;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            this.anomalyKnowledgeGained = regression.fountainAnomalyKnowledge;
        }

        public override void Study(Pawn studier, float studyAmount, float anomalyKnowledgeAmount = 0f)
        {
            base.Study(studier, studyAmount, anomalyKnowledgeAmount);
            regression.fountainAnomalyKnowledge = this.anomalyKnowledgeGained;
        }
    }
    public static class CompStudiable_StudyUnlocked_ManualPrefix
    {
        public static bool Prefix(object __instance, ref bool __result)
        {
            if (__instance is ThingComp comp && comp.parent?.def?.defName == "FountainOfYouth")
            {
                bool anomalyActive = ModsConfig.IsActive("Ludeon.RimWorld.Anomaly")
                    || ModLister.GetActiveModWithIdentifier("ludeon.rimworld.anomaly", ignorePostfix: true) != null;

                int level = Current.Game?.GetComponent<GameComponent_RegressionGame>()?.Level ?? 0;

                __result = level >= 2;
                return false; // overwrite
            }
            return true; // vanilla for others
        }
    }
    public class CompStudyUnlocksRegressionh : CompStudyUnlocks
    {
        protected new CompProperties_StudyUnlocksRegression Props
        {
            get
            {
                return (CompProperties_StudyUnlocksRegression)this.props;
            }
        }

        public GameComponent_RegressionGame regression
        {
            get
            {
                return Current.Game.GetComponent<GameComponent_RegressionGame>();
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            this.nextIndex = regression.MonolithNextIndex;
            this.studyProgress = regression.MonolithStudyProgress;
        }

        protected override void Notify_StudyLevelChanged(ChoiceLetter keptLetter)
        {
            regression.Notify_FountainStudyIncreased(keptLetter, this.nextIndex, this.studyProgress);
            this.letters.Clear();
        }
    }
    public class CompProperties_StudyUnlocksRegression : CompProperties_StudyUnlocks
    {
        public CompProperties_StudyUnlocksRegression()
        {
            this.compClass = typeof(CompStudyUnlocksRegressionh);
        }
    }
}

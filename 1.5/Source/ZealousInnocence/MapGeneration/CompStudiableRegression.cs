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
}

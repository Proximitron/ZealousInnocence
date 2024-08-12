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
        // Token: 0x17001B5C RID: 7004
        // (get) Token: 0x06009A61 RID: 39521 RVA: 0x00345531 File Offset: 0x00343731
        public override float AnomalyKnowledge
        {
            get
            {
                if (!Find.Anomaly.QuestlineEnded)
                {
                    return base.AnomalyKnowledge;
                }
                return 4f;
            }
        }

        // Token: 0x17001B5D RID: 7005
        // (get) Token: 0x06009A62 RID: 39522 RVA: 0x0034554B File Offset: 0x0034374B
        public override KnowledgeCategoryDef KnowledgeCategory
        {
            get
            {
                return Find.Anomaly.LevelDef.monolithStudyCategory;
            }
        }

        // Token: 0x06009A63 RID: 39523 RVA: 0x0034555C File Offset: 0x0034375C
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            this.anomalyKnowledgeGained = Find.Anomaly.monolithAnomalyKnowledge;
        }

        // Token: 0x06009A64 RID: 39524 RVA: 0x00345575 File Offset: 0x00343775
        public override void Study(Pawn studier, float studyAmount, float anomalyKnowledgeAmount = 0f)
        {
            base.Study(studier, studyAmount, anomalyKnowledgeAmount);
            Find.Anomaly.monolithAnomalyKnowledge = this.anomalyKnowledgeGained;
        }

        // Token: 0x04005635 RID: 22069
        private const float FinishedStudyAmount = 4f;
    }
}

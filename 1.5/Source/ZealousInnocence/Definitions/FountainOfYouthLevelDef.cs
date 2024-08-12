using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ZealousInnocence
{
    public class FountainOfYouthLevelDef : Def
    {
        public Texture2D UIIcon
        {
            get
            {
                return this.uiIcon;
            }
        }

        public override void PostLoad()
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                if (!this.uiIconPath.NullOrEmpty())
                {
                    this.uiIcon = ContentFinder<Texture2D>.Get(this.uiIconPath, true);
                }
            });
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string text in base.ConfigErrors())
            {
                yield return text;
            }

            if (this.entityCatagoryCompletionRequired != null && this.entityCountCompletionRequired <= 0)
            {
                yield return "entityCatagoryCompletionRequired is set but entityCountCompletionRequired is not";
            }
            yield break;
            yield break;
        }

        public int level;

        // Token: 0x040034A3 RID: 13475
        public int graphicIndex;

        // Token: 0x040034A4 RID: 13476
        public bool monolithGlows;

        // Token: 0x040034A5 RID: 13477
        public int anomalyThreatTier;

        // Token: 0x040034A6 RID: 13478
        public bool useInactiveAnomalyThreatFraction;

        // Token: 0x040034A7 RID: 13479
        public bool useActiveAnomalyThreatFraction;

        // Token: 0x040034A8 RID: 13480
        public float anomalyThreatFractionFactor = 1f;

        // Token: 0x040034A9 RID: 13481
        public float anomalyThreatFraction;

        // Token: 0x040034AA RID: 13482
        public List<IncidentDef> incidentsOnReached;

        // Token: 0x040034AB RID: 13483
        public bool triggersGrayPall;

        // Token: 0x040034AC RID: 13484
        public List<GameConditionDef> unreachableDuringConditions;

        // Token: 0x040034AD RID: 13485
        public bool advanceThroughActivation;

        // Token: 0x040034AE RID: 13486
        public int desiredHarbingerTreeCount;

        // Token: 0x040034AF RID: 13487
        public bool postEndgame;

        // Token: 0x040034B0 RID: 13488
        public int monolithGlowRadiusOverride = -1;

        // Token: 0x040034B1 RID: 13489
        public KnowledgeCategoryDef monolithStudyCategory;

        // Token: 0x040034B2 RID: 13490
        public EntityCategoryDef entityCatagoryCompletionRequired;

        // Token: 0x040034B3 RID: 13491
        public int entityCountCompletionRequired;

        // Token: 0x040034B4 RID: 13492
        public float anomalyMentalBreakChance;

        // Token: 0x040034B5 RID: 13493
        public SoundDef activateSound;

        // Token: 0x040034B6 RID: 13494
        public SoundDef activatedSound;

        // Token: 0x040034B7 RID: 13495
        public List<MonolithAttachment> attachments;

        // Token: 0x040034B8 RID: 13496
        public IntVec2? sizeIncludingAttachments;

        // Token: 0x040034B9 RID: 13497
        public string uiIconPath;

        // Token: 0x040034BA RID: 13498
        [MustTranslate]
        public string monolithLabel;

        // Token: 0x040034BB RID: 13499
        [MustTranslate]
        public string monolithDescription;

        // Token: 0x040034BC RID: 13500
        [MustTranslate]
        public string levelInspectText;

        // Token: 0x040034BD RID: 13501
        [MustTranslate]
        public string extraQuestDescription;

        // Token: 0x040034BE RID: 13502
        [MustTranslate]
        public string activateGizmoText;

        // Token: 0x040034BF RID: 13503
        [MustTranslate]
        public string activateFloatMenuText;

        // Token: 0x040034C0 RID: 13504
        [MustTranslate]
        public string activateGizmoDescription;

        // Token: 0x040034C1 RID: 13505
        [MustTranslate]
        public string pawnSentToActivateMessage;

        // Token: 0x040034C2 RID: 13506
        [MustTranslate]
        public string monolithCanBeActivatedText;

        // Token: 0x040034C3 RID: 13507
        [MustTranslate]
        public string activateQuestText;

        // Token: 0x040034C4 RID: 13508
        [MustTranslate]
        public string activatableLetterLabel;

        // Token: 0x040034C5 RID: 13509
        [MustTranslate]
        public string activatableLetterText;

        // Token: 0x040034C6 RID: 13510
        [MustTranslate]
        public string activatedLetterText;

        // Token: 0x040034C7 RID: 13511
        private Texture2D uiIcon;
    }
}

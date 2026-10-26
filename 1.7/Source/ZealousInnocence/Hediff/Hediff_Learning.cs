using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ZealousInnocence
{
    abstract class Hediff_ToddlerLearning : HediffWithComps
    {
        private const int updateInterval = 2500; //1h

        public abstract string SettingName { get; }

        public override bool ShouldRemove => Severity >= 1f | !pawn.isToddlerMentalOrPhysical();

        private static readonly Lazy<ZealousInnocenceSettings> _settings = new Lazy<ZealousInnocenceSettings>(() => LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>());
        public static ZealousInnocenceSettings Settings => _settings.Value;
        public override string SeverityLabel
        {
            get
            {
                if (Severity == 0f)
                {
                    return null;
                }
                return Severity.ToStringPercent();
            }
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (pawn.IsHashIntervalTick(updateInterval, delta))
            {
                InnerTick();
            }
        }

        public void InnerTick()
        {
            int prevStage = CurStageIndex;

            this.OnUpdate(CurStageIndex);

            //Log.Message("InnerTick for " + pawn + ", GetLearningPerTickBase: " + ToddlerUtility.GetLearningPerTickBase(pawn));

            float age = pawn.getAgeStagePhysicalMentalMin();

            // Should define the moment where the pawn reaches 0.5f, the ability to do it
            //Severity = Mathf.Min(1f, pawn.getAgeStagePhysicalMentalMin() / SettingWhatever.ageSelfLearned);

            // Should define the moment where the pawn reaches 1.0f, the ability to do it to others
            //Severity = Mathf.Min(1f, pawn.getAgeStagePhysicalMentalMin() / SettingWhatever.ageFullyLearned);

            if (CurStageIndex != prevStage)
            {
                this.OnStageUp(CurStageIndex);
            }

        }

        public virtual void OnUpdate(int stageIndex) { }

        public virtual void OnStageUp(int newStageIndex) { }
    }
}

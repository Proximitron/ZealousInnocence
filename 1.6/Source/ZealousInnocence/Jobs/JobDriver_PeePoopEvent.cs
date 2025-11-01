using DubsBadHygiene;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace ZealousInnocence
{
    public class JobDriver_PeePoopEvent : JobDriver
    {
        public override string GetReport()
        {
            var need = DiaperNeed;
            if (need == null) return "having an accident";

            var diaper = Helper_Diaper.getDiaper(pawn);
            if (diaper != null) return need.IsPeeing ? "PhraseDiaperWetting".Translate(diaper.def.label) : "PhraseDiaperSoiling".Translate(diaper.def.label);

            var underwear = Helper_Diaper.getUnderwear(pawn);
            if (underwear != null) return need.IsPeeing ? "PhraseUnderwearWetting".Translate(underwear.def.label) : "PhraseUnderwearSoiling".Translate(underwear.def.label);

            return need.IsPeeing ? "PhrasePantsWetting".Translate() : "PhrasePantsSoiling".Translate();
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        public Need_Diaper DiaperNeed =>
            pawn?.needs != null ? pawn.needs.TryGetNeed<Need_Diaper>() : null;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // End if drafted/downed, no longer having an accident, or pawn turns aggro
            this.FailOn(() => pawn.Drafted || pawn.Downed);
            this.FailOn(() => DiaperNeed?.IsHavingAccident != true);

            int dur = job.expiryInterval > 0 ? job.expiryInterval : 120;

            var wait = Toils_General.Wait(dur);
            wait.WithProgressBarToilDelay(TargetIndex.None);       // progress above pawn
            wait.socialMode = RandomSocialMode.Off;

            wait.initAction = () =>
            {

            };

            yield return wait;
        }
    }
}

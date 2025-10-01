using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace ZealousInnocence
{
    [DefOf]
    public class ThingDefOf
    {
        public static ThingDef FountainOfYouth;
    }
    [DefOf]
    public class FountainOfYouthLevelDefOf
    {
        public static FountainOfYouthLevelDef Inactive;
        public static FountainOfYouthLevelDef Stirring;
        public static FountainOfYouthLevelDef Embraced;
        public static FountainOfYouthLevelDef Disrupted;
    }
    [DefOf]
    public class ResearchTabDefOf
    {
        public static ResearchTabDef Regression;
    }
    [DefOf]
    public class PrisonerInteractionModeDefOf
    {
        public static PrisonerInteractionModeDef DiaperChangesAllowed;
        public static PrisonerInteractionModeDef UnderwearChangesAllowed;
    }

    [DefOf]
    public class PawnCapacityDefOf
    {
        public static PawnCapacityDef BladderControl;
        public static PawnCapacityDef Moving;
    }
    [DefOf]
    public class BodyPartTagDefOf
    {
        public static BodyPartTagDef BladderControlSource;
    }
    [DefOf]
    public class BodyPartDefOf
    {
        public static BodyPartDef Bladder;
    }
    [DefOf]
    public class DiaperChangie
    {
        public static SoundDef Pee;
        public static SoundDef Poop;
        public static SoundDef Diapertape;
    }

    [DefOf]
    public class StatDefOf
    {
        public static StatDef Absorbency;
        public static StatDef DiaperAbsorbency; // This value is a stat created from Absorbency and DiaperSupport
        public static StatDef BladderStrengh; // Influences bladder control of the pawn
        public static StatDef BedwettingChance; // Influences bedwetting chance (-1.0 to 1.0). At any end it guaranties it one way or the other.

        public static StatDef MentalAge;
        public static StatDef MentalRegression;
        public static StatDef MentalAgeLimit;
    }

    [DefOf]
    public class HediffDefOf
    {
        public static HediffDef RegressionState;
        public static HediffDef RegressionDamage;
        public static HediffDef DiaperRash;

        public static HediffDef BigBladder;
        public static HediffDef SmallBladder;

        public static HediffDef BedWetting;
        public static HediffDef Incontinent;
    }

    [DefOf]
    public class GeneDefOf
    {
        //public static GeneDef BladderSizeTiny;
        public static GeneDef BladderSizeSmall;
        public static GeneDef BladderSizeBig;
        //public static GeneDef BladderSizeHuge;

        public static GeneDef BladderBedwettingEarly;
        public static GeneDef BladderBedwettingLate;
        public static GeneDef BladderBedwettingAlways;

        public static GeneDef BladderStrenghWeak;
        public static GeneDef BladderStrenghStrong;
    }

    [DefOf]
    public class TraitDefOf
    {
        public static TraitDef Potty_Rebel;
        public static TraitDef Big_Boy;
    }
    [DefOf]
    public class JobDefOf
    {
        public static JobDef Unbladder;
        public static JobDef Rebirth;
        public static JobDef Phoenix;
        public static JobDef RegressedPlayAround;
        public static JobDef LayDown;
        public static JobDef ChangePatientDiaper;

        public static JobDef FountainOfYouthInvestigate;
        public static JobDef FountainOfYouthActivate;
        //public static JobDef WearSleepwear;
        // public static JobDef OptimizeApparel;
        //public static JobDef ChangeSleepwear;
    }
    [DefOf]
    public class DutyDefOf
    {
        public static DutyDef RegressedPlayTime;
    }

    [DefOf]
    public class ThoughtDefOf
    {
        public static ThoughtDef RegressedGames;
    }


    [DefOf]
    public class HistoryEventDefOf
    {
        public static HistoryEventDef GotUnbladdered;
    }

    [DefOf]
    public class ApparelLayerDefOf
    {
        public static ApparelLayerDef Underwear;
    }

    [DefOf]
    public static class ThingCategoryDefOf
    {
        public static ThingCategoryDef Diapers;
        public static ThingCategoryDef Onesies;
        public static ThingCategoryDef DiapersNight;
        public static ThingCategoryDef Underwear;
        public static ThingCategoryDef MaleCloth;
        public static ThingCategoryDef FemaleCloth;
        static ThingCategoryDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RimWorld.ThingCategoryDefOf));
        }
    }

    [DefOf]
    public static class PreceptDefOf
    {
        public static PreceptDef Diapers_Loved;
        public static PreceptDef Diapers_Neutral;
        public static PreceptDef Diapers_Hated;

        public static PreceptDef Onesies_Loved;
        public static PreceptDef Onesies_Neutral;
        public static PreceptDef Onesies_Hated;

        public static PreceptDef CribBed_Preferred;
        static PreceptDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RimWorld.PreceptDefOf));
        }
    }

}

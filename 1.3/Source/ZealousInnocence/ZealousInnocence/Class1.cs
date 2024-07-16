using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using DubsBadHygiene;
using Verse;
using Verse.AI;
using Verse.Sound;
using Multiplayer.API;
using System.Net;
using UnityEngine;

namespace ZealousInnocence
{
    [StaticConstructorOnStartup]
    public static class ZealousInnocenceMultiplayer
    {
        static ZealousInnocenceMultiplayer()
        {
            if (!MP.enabled) return;

            // This is where the magic happens and your attributes
            // auto register, similar to Harmony's PatchAll.
            MP.RegisterAll();

            // You can choose to not auto register and do it manually
            // with the MP.Register* methods.

            // Use MP.IsInMultiplayer to act upon it in other places
            // user can have it enabled and not be in session
        }
    }


    public enum DiaperSituationCategory : byte
    {
        Trashed,
        Spent,
        Used,
        Clean,
    }

    public enum DiaperLikeCategory : byte
    {
        Neutral,
        Liked,
        Disliked,
    }


    [DefOf]
    public class PawnCapacityDefOf
    {
        public static PawnCapacityDef BladderControl;
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
    }

    [DefOf]
    public class StatDefOf
    {
        public static StatDef Absorbency;
        public static StatDef DiaperAbsorbency;
    }

    [DefOf]
    public class HediffDefOf
    {
        public static HediffDef RegressionState;
        public static HediffDef DiaperRash;
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
    public class PawnCapacityWorker_BladderControl : PawnCapacityWorker
    {
        public override float CalculateCapacityLevel(HediffSet diffSet,
                                                      List<PawnCapacityUtility.CapacityImpactor> impactors = null)
        {
            return PawnCapacityUtility.CalculateTagEfficiency(
                diffSet,
                BodyPartTagDefOf.BladderControlSource,
                float.MaxValue,
                default(FloatRange),
                impactors) * Mathf.Min(CalculateCapacityAndRecord(diffSet, RimWorld.PawnCapacityDefOf.Consciousness, impactors), 1f);
        }

        public override bool CanHaveCapacity(BodyDef body)
        {
            return body.HasPartWithTag(BodyPartTagDefOf.BladderControlSource);
        }
    }

    public enum BathroomDesireCategory : byte
    {
        Going,
        NeedsToGo,
        Fine,
    }

    
}
//-----------------------------------------------------------
/*
public class CompDiaperFullnes : ThingComp
{  
    public DiaperFullnesProperties Props => (DiaperFullnesProperties)this.props;

    public float ExampleFloat => Props.diaperFullnes;
}

public class DiaperFullnesProperties : CompProperties
{
    public float diaperFullnes;

    public DiaperFullnesProperties()
    {
        this.compClass = typeof(CompDiaperFullnes);
    }

    public DiaperFullnesProperties(Type compClass) : base(compClass)
    {
        this.compClass = compClass;
    }
}
*/

/*public class Recipe_Neuter : Recipe_Surgery
{

    public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
    {
        Debug("GetPartsToApplyOn");
        if (!pawn.health.hediffSet.HasHediff(HediffDefOf.Neutered) && !pawn.health.hediffSet.PartIsMissing(pawn.ReproductiveOrgans()))
            yield return pawn.ReproductiveOrgans();
    }

    // TODO: Verify working for A18 (added bill argument)
    public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
    {
        Debug("ApplyOnPawn");
        if (billDoer != null)
        {
            if (CheckSurgeryFail(billDoer, pawn, ingredients, pawn.ReproductiveOrgans(), bill))
                return;
            TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
        }
        pawn.health.AddHediff(recipe.addsHediff, part, null);

        GiveThoughtsForPawnNeutered(pawn);
    }

    public void GiveThoughtsForPawnNeutered(Pawn victim)
    {
        // animals are fine
        if (!victim.RaceProps.Humanlike)
            return;

        int stage;

        // should we really be doing this?
        if (victim.IsColonist)
            stage = 3;
        else if (victim.IsPrisonerOfColony)
            stage = 1;
        else
            stage = 2;

        // guilty had it coming
        if (victim.guilt.IsGuilty)
            stage = 0;

        foreach (
            Pawn pawn in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_NoCryptosleep)
            pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(ThoughtDefOf.SomeoneNeutered, stage));
    }
}*/


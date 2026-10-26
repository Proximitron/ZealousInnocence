using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ZealousInnocence
{
    [StaticConstructorOnStartup]
    public class Apparel_UnderwearSlot : Apparel
    {
        public override float GetSpecialApparelScoreOffset()
        {

            return base.GetSpecialApparelScoreOffset();
        }
        public override Graphic Graphic
        {
            get
            {
                float hpPercentage = (float)this.HitPoints / (float)this.MaxHitPoints;

                bool needOverwrite = false;

                var texPath = this.def.graphicData.texPath;
                if (this.WearerPawn != null && this.def.thingCategories.Contains(ThingCategoryDefOf.Diapers))
                {
                    texPath += "_Worn";
                    needOverwrite = true;
                }
                if (hpPercentage < 0.51f)
                {
                    texPath += "_Dirty";
                    needOverwrite = true;
                }

                if (needOverwrite)
                {
                    var graphic = GraphicDatabase.Get<Graphic_Single>(texPath, ShaderDatabase.Transparent, this.DrawSize, this.DrawColor, this.DrawColorTwo);
                    return new Graphic_RandomRotated(graphic, this.def.graphicData.onGroundRandomRotateAngle);
                }
                return base.Graphic;
            }
        }
        // Suppress the "got destroyed" message of things in the underwear slot if not worn
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {

            if (this.WearerPawn == null)
            {
                if (this.HitPoints <= this.MaxHitPoints * 0.51f)
                {
                    if (mode == DestroyMode.KillFinalize)
                    {
                        mode = DestroyMode.Vanish;
                    }
                }
            }

            base.Destroy(mode);
        }
        private Pawn WearerPawn
        {
            get
            {
                if (Wearer != null)
                {
                    return Wearer;
                }
                if (this.ParentHolder is Pawn_ApparelTracker apparelTracker)
                {
                    return apparelTracker.pawn;
                }
                return null;
            }
        }
        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            Map?.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Things);
            base.PostApplyDamage(dinfo, totalDamageDealt);
        }
    }

    [StaticConstructorOnStartup]
    public class Apparel_Diaper_Base : Apparel_UnderwearSlot
    {

    }
    [StaticConstructorOnStartup]
    public class Apparel_Disposable_Diaper : Apparel_Diaper_Base
    {
        private CompDisposableRapidDecay DecayComp => this.TryGetComp<CompDisposableRapidDecay>();
        public override bool CanStackWith(Thing other)
        {
            if (!base.CanStackWith(other)) return false;

            if (DecayComp?.preventMerging == true) return false;

            if (other.HitPoints != HitPoints) return false;

            return true;
        }

        public override bool TryAbsorbStack(Thing other, bool respectStackLimit)
        {
            if (!CanStackWith(other)) return false;
            return base.TryAbsorbStack(other, respectStackLimit);
        }

        public override Graphic Graphic
        {
            get
            {
                var comp = this.TryGetComp<CompDisposableRapidDecay>();
                if (DecayComp?.preventMerging == true && Wearer == null)
                {
                    
                    return GraphicDatabase.Get<Graphic_Single>(
                        def.apparel.wornGraphicPath,  // can be any valid path; not drawn due to Color.clear
                        ShaderDatabase.Transparent,
                        this.DrawSize,
                        Color.clear,
                        Color.clear
                    );
                }
                return base.Graphic;
            }
        }
        public CompRegressionMemory Memory
        {
            get
            {
                return GetMemory(Wearer);
            }
        }
        public static CompRegressionMemory GetMemory(Pawn pawn)
        {
            return pawn?.TryGetComp<CompRegressionMemory>();
        }
        public int DesiredSpareCount
        {
            get => Mathf.Clamp(GetDesiredSpareCountFor(Wearer), 0, 10);
            set => SetDesiredSpareCountFor(Wearer, Mathf.Clamp(value, 0, 10));
        }
        public static int GetDesiredSpareCountFor(Pawn pawn, int fallback = 2)
        {
            CompRegressionMemory Memory = GetMemory(pawn);
            if (Memory == null) return fallback;
            return Memory.targetDisposablesCount;
        }
        public static void SetDesiredSpareCountFor(Pawn pawn, int target)
        {
            CompRegressionMemory Memory = GetMemory(pawn);
            if (Memory == null) return;
            Memory.targetDisposablesCount = target;
        }
        public static int SparesOfDiaper(Pawn pawn, Apparel_Disposable_Diaper diaper)
        {
            return (pawn?.inventory?.innerContainer ?? Enumerable.Empty<Thing>())
                        .OfType<Apparel_Disposable_Diaper>()
                        .Where(t => t.def == diaper.def)
                        .Sum(t => t.stackCount);
        }
        public static int SparesOfCurrentDiaper(Pawn pawn)
        {
            Apparel_Disposable_Diaper worn = (Apparel_Disposable_Diaper)pawn.apparel?.WornApparel?.FirstOrDefault(a => a is Apparel_Disposable_Diaper);
            if(worn == null) return 0;
            return SparesOfDiaper(pawn, worn);
        }

        public override IEnumerable<Gizmo> GetWornGizmos()
        {
            foreach (var g in base.GetWornGizmos())
                yield return g;

            // Only show to player when worn by a player pawn
            if (Wearer is not Pawn pawn || Wearer.Faction != Faction.OfPlayer) yield break;

            Apparel_Disposable_Diaper worn = (Apparel_Disposable_Diaper)pawn.apparel?.WornApparel?.FirstOrDefault(a => a is Apparel_Disposable_Diaper);
            // There is no point in this until the pawn can change their own pullups or diapers
            if (worn == null || !pawn.canChange(worn)) yield break;

            int count = SparesOfCurrentDiaper(pawn);

            // Display (click to open quick set menu)
            yield return new Command_Action
            {
                defaultLabel = $"Spare diapers: {count}/{DesiredSpareCount}",
                defaultDesc = "Set how many fresh disposables this pawn should carry in inventory.",
                action = () => ShowDesiredMenu(),
                icon = ContentFinder<Texture2D>.Get("Things/Item/Disposables/Disposable_a", false)
            };
        }

        private void ShowDesiredMenu()
        {
            var opts = new List<FloatMenuOption>();
            // Offer a handful of sensible choices
            for (int i = 0; i <= 10; i++)
            {
                int val = i;
                string label = (val == DesiredSpareCount) ? $"• {val}" : val.ToString();
                opts.Add(new FloatMenuOption(label, () => DesiredSpareCount = val));
            }
            Find.WindowStack.Add(new FloatMenu(opts));
        }
    }

    [StaticConstructorOnStartup]
    public class Apparel_Underwear_Base : Apparel_UnderwearSlot
    {

    }

    public enum BedwettingSituationCategoryThought : int
    {
        // Default behaviour if nothing else applies
        Wet_Bed_Default,
        Wet_Bed_Default_Diaper,
        Wet_Bed_Default_Liked,

        // Thoughts of bedwetter
        Wet_Bed_Bedwetter,
        Wet_Bed_Bedwetter_Diaper,
        Wet_Bed_Bedwetter_Liked,

        // Thoughts of non-adult (child)
        Wet_Bed_Non_Adult,
        Wet_Bed_Non_Adult_Diaper,
    }

    public class ThoughtWorker_Diaper_Dressed : ThoughtWorker
    {
        public enum DiaperSituationCategoryThought : int
        {
            // Thoughts if diaper is not required but still worn
            Not_Required_Clean,
            Not_Required_Used,
            Not_Required_Spent,
            Not_Required_Trashed,

            // Thoughts if diaper is nessesary but otherwise not expecially liked
            Required_Neutral_None,
            Required_Neutral_Clean,
            Required_Neutral_Used,
            Required_Neutral_Spent,
            Required_Neutral_Trashed,

            // Thoughts if diaper is liked
            Loved_None,
            Loved_Clean,
            Loved_Used,
            Loved_Spent,
            Loved_Trashed,

            // Thoughts if diaper is hated
            Hated_Clean,
            Hated_AnyOther,

            // Thoughts of non-adult (child)
            Non_Adult_Required_None,
            Non_Adult_Required_Night_Protection_None,
            Non_Adult_Required_Night_Protection_Clean,
            Non_Adult_Clean,
            Non_Adult_Used,
            Non_Adult_Other,

            // Thoughts of toddler or baby
            Toddler_None,
            Toddler_Clean,
            Toddler_Used,
            Toddler_Spent,
            Toddler_Trashed,


            // Thoughts if night diaper is nessesary, but normal diaper is worn!
            Night_Protection_Diaper_Clean,
            Night_Protection_Diaper_Used,
            Night_Protection_Diaper_Worse,

            // Thoughts if diaper is nessesary but only at night and night diapers are worn
            Night_Protection_Required_None, // Thoughts if diaper is nessesary at night, not worn, but otherwise not expecially liked
            Night_Protection_Required_Clean,
            Night_Protection_Required_Used,
            Night_Protection_Required_Worse,

            // Thoughts if night diapers are unnessesary, but worn
            Night_Protection_Not_Required_Clean,
            Night_Protection_Not_Required_Used,
            Night_Protection_Not_Required_Worse,

            //START: Thoughts if underwear is requested/wanted or wrongfully worn
            Underwear_Not_Worn,
            Underwear_Worn,
            Underwear_Worn_Non_Adult,

            // Diaper lover's thoughts
            DL_Loved_None,
            DL_Loved_Clean,
            DL_Loved_Used,
            DL_Loved_Spent,
            DL_Loved_Trashed,

        }

        [DefOf]
        public static class BodyPartGroupDefOf
        {
            static BodyPartGroupDefOf()
            {
                DefOfHelper.EnsureInitializedInCtor(typeof(RimWorld.BodyPartGroupDefOf));
            }
            public static BodyPartGroupDef Waist;
        }

        [StaticConstructorOnStartup]
        public static class DiaperThoughts
        {
            public static ThingDef ApparelMakeableBase = DefDatabase<ThingDef>.GetNamedSilentFail("blabla");
        }

        /*private static readonly Dictionary<int, (ThoughtState state, int expireTick)> cache = new();
        private const int CacheTicks = 250; //

        protected  ThoughtState NewCurrentStateInternal(Pawn p)
        {
            int id = p.thingIDNumber;
            int now = Find.TickManager.TicksGame;

            // Return cached value if still valid
            if (cache.TryGetValue(id, out var c) && now < c.expireTick)
                return c.state;

            // Recompute & refresh cache
            ThoughtState result = RecomputeState(p);
            cache[id] = (result, now + CacheTicks);
            return ThoughtState.ActiveAtStage(2);
        }*/
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            var preference = Helper_Diaper.getDiaperPreference(p);
            var currDiapie = Helper_Diaper.getDiaper(p);
            var diaperRequired = Helper_Diaper.needsDiaper(p);
            var diaperRequiredNight = Helper_Diaper.needsDiaperNight(p);
            if (currDiapie != null)
            {
                switch (p.needs.TryGetNeed<Need_Diaper>().CurCategory)
                {
                    case DiaperSituationCategory.Clean:
                        switch (preference)
                        {
                            case DiaperLikeCategory.Toddler:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Toddler_Clean);
                            case DiaperLikeCategory.Child:
                                if (Helper_Diaper.isNightDiaper(currDiapie)) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Non_Adult_Required_Night_Protection_Clean);
                                else return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Non_Adult_Clean);
                            case DiaperLikeCategory.Liked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Loved_Clean);
                            case DiaperLikeCategory.Disliked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Hated_Clean);
                            case DiaperLikeCategory.Diaper_Lover:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.DL_Loved_Clean);
                            default:
                                if (diaperRequired)
                                {
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Required_Neutral_Clean);
                                }
                                if (Helper_Diaper.isNightDiaper(currDiapie))
                                {
                                    if (diaperRequiredNight)
                                    {
                                        return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Required_Clean);
                                    }
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Not_Required_Clean);
                                }
                                else
                                {
                                    if (diaperRequiredNight) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Diaper_Clean);
                                }
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Not_Required_Clean);
                        }
                    case DiaperSituationCategory.Used:
                        switch (preference)
                        {
                            case DiaperLikeCategory.Toddler:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Toddler_Used);
                            case DiaperLikeCategory.Child:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Non_Adult_Used);
                            case DiaperLikeCategory.Liked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Loved_Used);
                            case DiaperLikeCategory.Disliked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Hated_AnyOther);
                            case DiaperLikeCategory.Diaper_Lover:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.DL_Loved_Used);
                            default:
                                if (diaperRequired)
                                {
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Required_Neutral_Used);
                                }
                                if (Helper_Diaper.isNightDiaper(currDiapie))
                                {
                                    if (diaperRequiredNight)
                                    {
                                        return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Required_Used);
                                    }
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Not_Required_Used);
                                }
                                else
                                {
                                    if (diaperRequiredNight) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Diaper_Used);
                                }
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Not_Required_Used);
                        }
                    case DiaperSituationCategory.Spent:
                        switch (preference)
                        {
                            case DiaperLikeCategory.Toddler:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Toddler_Spent);
                            case DiaperLikeCategory.Child:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Non_Adult_Other);
                            case DiaperLikeCategory.Liked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Loved_Spent);
                            case DiaperLikeCategory.Disliked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Hated_AnyOther);
                            case DiaperLikeCategory.Diaper_Lover:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.DL_Loved_Spent);
                            default:
                                if (diaperRequired)
                                {
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Required_Neutral_Spent);
                                }
                                if (Helper_Diaper.isNightDiaper(currDiapie))
                                {
                                    if (diaperRequiredNight)
                                    {
                                        return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Required_Worse);
                                    }
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Not_Required_Worse);
                                }
                                else
                                {
                                    if (diaperRequiredNight) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Diaper_Worse);
                                }
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Not_Required_Spent);
                        }
                    case DiaperSituationCategory.Trashed:
                        switch (preference)
                        {
                            case DiaperLikeCategory.Toddler:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Toddler_Trashed);
                            case DiaperLikeCategory.Child:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Non_Adult_Other);
                            case DiaperLikeCategory.Liked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Loved_Trashed);
                            case DiaperLikeCategory.Disliked:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Hated_AnyOther);
                            case DiaperLikeCategory.Diaper_Lover:
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.DL_Loved_Trashed);
                            default:
                                if (diaperRequired)
                                {
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Required_Neutral_Trashed);
                                }
                                if (Helper_Diaper.isNightDiaper(currDiapie))
                                {
                                    if (diaperRequiredNight)
                                    {
                                        return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Required_Worse);
                                    }
                                    return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Not_Required_Worse);
                                }
                                else
                                {
                                    if (diaperRequiredNight) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Diaper_Worse);
                                }
                                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Not_Required_Trashed);
                        }
                    default:
                        throw new NotImplementedException();
                }
            }

            // After this point it should be clear that no diaper is worn
            if(preference == DiaperLikeCategory.Toddler) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Toddler_None);

            var currUnderpants = Helper_Diaper.getUnderwear(p);

            if (preference == DiaperLikeCategory.Child)
            {
                if (diaperRequired) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Non_Adult_Required_None);
                if (diaperRequiredNight) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Non_Adult_Required_Night_Protection_None);
                if (currUnderpants != null) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Underwear_Worn_Non_Adult);
                return ThoughtState.Inactive;
            }

            if (preference == DiaperLikeCategory.Liked)
            {
                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Loved_None);
            }
            else if (preference == DiaperLikeCategory.Diaper_Lover)
            {
                return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.DL_Loved_None);
            }
            else if (preference != DiaperLikeCategory.Disliked)
            {
                if (diaperRequired) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Night_Protection_Required_None);
                if (diaperRequiredNight) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Required_Neutral_None);
            }
            if (currUnderpants == null) return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Underwear_Not_Worn);
            return ThoughtState.ActiveAtStage((int)DiaperSituationCategoryThought.Underwear_Worn);
        }
    }
}

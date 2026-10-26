using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ZealousInnocence
{
    public class CompProperties_FOYReservoir : CompProperties
    {
        public int capacity = 20;           // max bottled units stored
        public int ticksPerUnit = 6000;    // 10 units per day
        public int extractTicks = 1200;     // job duration (~20s)
        public ThingDef productDef;         // ZI_Foy_Vial
        public int productCount = 1;
        public int minLevel = 0;

        public CompProperties_FOYReservoir()
        {
            compClass = typeof(CompFOYReservoir);
        }
    }

    public class CompFOYReservoir : ThingComp
    {
        public CompProperties_FOYReservoir Props => (CompProperties_FOYReservoir)props;

        public int RegressionLevel => Current.Game?.GetComponent<GameComponent_RegressionGame>()?.Level ?? 0;
        public float foyProductionMultiplier => Current.Game?.GetComponent<GameComponent_RegressionGame>()?.LevelDef?.foyProductionMultiplier ?? 0.0f;

        public int ticksPerUnit => Mathf.RoundToInt(Props.ticksPerUnit / foyProductionMultiplier);

        public bool allowExtract = true;
        public int stored;          // current units
        private int progressTicks;  // regen progress
        private static ZealousInnocenceSettings Settings
    => LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
        public override void CompTickRare()
        {
            base.CompTickRare();

            if (foyProductionMultiplier <= 0) return;
            if (stored >= Props.capacity) return;
            progressTicks += 250; // rare tick
            if (progressTicks >= ticksPerUnit)
            {
                progressTicks -= ticksPerUnit;
                stored = Math.Min(stored + 1, Props.capacity);
                parent.Map?.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (!parent.Spawned) return;
            if (!parent.IsHashIntervalTick(250)) return; // simulate “rare” cadence

            if (foyProductionMultiplier <= 0) return;
            if (stored >= Props.capacity) return;
            progressTicks += 250;
            if (progressTicks >= ticksPerUnit)
            {
#if DEBUG
                if (Settings.debugging && Settings.debuggingJobs)
                    Log.Message($"[ZI] Generating FOY (norm) {stored}/{Props.capacity} prog={progressTicks}");
#endif
                progressTicks -= ticksPerUnit;
                stored = Math.Min(stored + 1, Props.capacity);
                parent.Map?.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);
            }
        }

        public bool CanExtractNow(Pawn p)
        {
            if (!allowExtract || stored <= 0) return false;
            if (foyProductionMultiplier <= 0) return false;
            if (parent.IsForbidden(p) || !p.CanReserveAndReach(parent, PathEndMode.InteractionCell, Danger.Some)) return false;
            return true;
        }

        public void ConsumeOneUnit() => stored = Math.Max(0, stored - 1);

        public void Produce(Faction fac, Map map)
        {
            if (Props.productDef == null) return;
            var thing = ThingMaker.MakeThing(Props.productDef);
            thing.stackCount = Props.productCount;
            GenPlace.TryPlaceThing(thing, parent.InteractionCell, map, ThingPlaceMode.Near);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (foyProductionMultiplier > 0)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "Allow extracting",
                    defaultDesc = "If enabled, colonists will bottle water from the fountain.",
                    isActive = () => allowExtract,
                    toggleAction = () => allowExtract = !allowExtract,
                    icon = ContentFinder<Texture2D>.Get("Things/Item/Flask", true)
                };

                // tiny stat readout
                yield return new Command_Action
                {
                    defaultLabel = $"Stored: {stored}/{Props.capacity}",
                    defaultDesc = ticksPerUnit / 60000f < 1f
                        ? $"Regenerates every {ticksPerUnit / 2500f:0.#} hours."   // 1 hour = 2500 ticks
                        : $"Regenerates every {ticksPerUnit / 60000f:0.##} days.",
                    icon = ContentFinder<Texture2D>.Get("Things/Item/Vial", true),
                    action = () => { }
                };
            }

        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref allowExtract, "allowExtract", true);
            Scribe_Values.Look(ref stored, "stored", 0);
            Scribe_Values.Look(ref progressTicks, "progressTicks", 0);
        }
    }

    public class JobDriver_ExtractFOY : JobDriver
    {
        public CompFOYReservoir Reservoir => TargetA.Thing.TryGetComp<CompFOYReservoir>();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
            => pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => Reservoir == null || Reservoir.stored <= 0 || !Reservoir.allowExtract);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            var wait = Toils_General.Wait(Reservoir?.Props.extractTicks ?? 1200)
                         .WithProgressBarToilDelay(TargetIndex.A)
                         .FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);

            yield return wait;

            yield return Toils_General.Do(() =>
            {
                var r = Reservoir;
                if (r == null || r.stored <= 0) return;
                r.ConsumeOneUnit();
                r.Produce(Faction.OfPlayer, pawn.Map);
                SoundDefOf.EmergeFromWater.PlayOneShot(SoundInfo.InMap(pawn));
                MoteMaker.ThrowText(pawn.Position.ToVector3(), pawn.Map, "Bottled FOY", 1.9f);
            });
        }
    }

    public class WorkGiver_ExtractFOY : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            var things = pawn.Map.listerThings.ThingsOfDef(ThingDefOf.FountainOfYouth);
            return things ?? (IEnumerable<Thing>)System.Array.Empty<Thing>();
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t == null || t.Map != pawn.Map) return false;
            var comp = t.TryGetComp<CompFOYReservoir>();
            if (comp == null || !comp.CanExtractNow(pawn)) return false;
            if (!pawn.CanReserve(t, 1, -1, null, forced)) return false;
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
            => JobMaker.MakeJob(JobDefOf.ZI_ExtractFOY, t);
    }
}

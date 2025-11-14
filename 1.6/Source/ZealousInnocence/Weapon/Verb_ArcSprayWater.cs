using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ZealousInnocence
{
    public class Verb_ArcSprayWater : Verb_ShootBeam
    {
        // --- CONFIG (tweak to taste) ---
        // Width (in cells) around the beam line that receives damage
        [TweakValue("ArcSprayWater", 0f, 10f)] public static float DamageHalfWidth = 1.5f;

        [TweakValue("ArcSprayWater", 0f, 10f)] public static float DamagePerHit = 3.5f;
        private const string SquirtDamageDefName = "SquirtSpray";

        private static DamageDef SquirtSprayDef
            => DefDatabase<DamageDef>.GetNamed(SquirtDamageDefName, errorOnFail: true);

        [TweakValue("ArcSprayWater", 0f, 10f)] public static float DistanceToLifetimeScalar = 5f;
        [TweakValue("ArcSprayWater", -2f, 7f)] public static float BarrelOffset = 5f;

        private IncineratorSpray sprayer;

        public override void WarmupComplete()
        {
            this.sprayer = (GenSpawn.Spawn(RimWorld.ThingDefOf.IncineratorSpray, this.caster.Position, this.caster.Map, WipeMode.Vanish) as IncineratorSpray);
            base.WarmupComplete();
            var battleLog = Find.BattleLog;
            Thing casterThing = this.caster;
            Thing targetThing = this.currentTarget.HasThing ? this.currentTarget.Thing : null;
            ThingWithComps equipmentSource = base.EquipmentSource;
            battleLog.Add(new BattleLogEntry_RangedFire(casterThing, targetThing, equipmentSource != null ? equipmentSource.def : null, null, false));
        }

        protected override bool TryCastShot()
        {
            bool result = base.TryCastShot();

            Vector3 worldTarget = base.InterpolatedPosition.Yto0();
            IntVec3 cellTarget = worldTarget.ToIntVec3();
            Vector3 worldSource = this.caster.DrawPos;
            Vector3 dir = (worldTarget - worldSource).normalized;
            worldSource += dir * BarrelOffset;
            IntVec3 casterCell = this.caster.Position;

            MoteDualAttached mote = MoteMaker.MakeInteractionOverlay(
                ThingDefOf.Mote_FoyBurst,
                new TargetInfo(casterCell, this.caster.Map, false),
                new TargetInfo(cellTarget, this.caster.Map, false));

            float dist = Vector3.Distance(worldTarget, worldSource);
            float scaleMul = (dist < BarrelOffset) ? 0.5f : 1f;

            if (this.sprayer != null)
            {
                this.sprayer.Add(new IncineratorProjectileMotion
                {
                    mote = mote,
                    targetDest = cellTarget,
                    worldSource = worldSource,
                    worldTarget = worldTarget,
                    moveVector = (worldTarget - worldSource).normalized,
                    startScale = 1f * scaleMul,
                    endScale = (1f + Rand.Range(0.1f, 0.4f)) * scaleMul,
                    lifespanTicks = Mathf.FloorToInt(dist * DistanceToLifetimeScalar)
                });
            }

            ApplyBeamDamageLine(worldSource, worldTarget);

            return result;
        }

        private void ApplyBeamDamageLine(Vector3 worldSource, Vector3 worldTarget)
        {
            var map = caster.Map;
            if (map == null) return;

            // Step through cells between caster and target
            foreach (var cell in GenSight.BresenhamCellsBetween(caster.Position, worldTarget.ToIntVec3()))
            {
                if (!cell.InBounds(map)) continue;

                // keep within a small width around the geometric line (matches visuals)
                var center = cell.ToVector3Shifted();
                float lateral = DistancePointToSegment(center, worldSource, worldTarget);
                if (lateral > DamageHalfWidth) continue;

                const float ExtinguishPerCell = 30f;

                // Damage pawns in the cell and puts out fires
                var things = map.thingGrid.ThingsListAt(cell);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is Fire f && !f.Destroyed)
                    {
                        f.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, ExtinguishPerCell, instigator: caster));
                    }

                    if (things[i] is Pawn p && p.Spawned && p != caster)
                    {
                        var dinfo = new DamageInfo(
                            SquirtSprayDef,
                            DamagePerHit,
                            instigator: caster,
                            hitPart: null,
                            weapon: EquipmentSource?.def);

                        p.TakeDamage(dinfo);
                    }
                }
            }
        }

        // Utility: shortest distance from a point to segment AB (world coords)
        private static float DistancePointToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float ab2 = ab.sqrMagnitude;
            if (ab2 <= 0.0001f) return Vector3.Distance(point, a);
            float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / ab2);
            Vector3 proj = a + t * ab;
            return Vector3.Distance(point, proj);
        }
    }
}

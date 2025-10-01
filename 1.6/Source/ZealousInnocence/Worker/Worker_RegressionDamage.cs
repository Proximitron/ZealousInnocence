﻿using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ZealousInnocence
{
    public class Worker_RegressionDamage : DamageWorker_AddInjury
    {
        /// <summary>
        ///     values below this should be considered 0
        /// </summary>
        protected const float EPSILON = 0.001f;


        /// <summary>
        ///     Applies the specified dinfo.
        /// </summary>
        /// <param name="dinfo">The dinfo.</param>
        /// <param name="thing">The thing.</param>
        /// <returns></returns>
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            if (thing is Pawn pawn)
            {
                if (Helper_Regression.CanBeRegressed(pawn))
                {
                    return ApplyToPawn(dinfo, pawn);
                }
            }

            return base.Apply(dinfo, thing);
        }


        /// <summary>
        /// does explosive damage to a thing 
        /// </summary>
        /// <param name="explosion">The explosion.</param>
        /// <param name="t">The t.</param>
        /// <param name="damagedThings">The damaged things.</param>
        /// <param name="ignoredThings">The ignored things.</param>
        /// <param name="cell">The cell.</param>
        protected override void ExplosionDamageThing(Explosion explosion, Thing t, List<Thing> damagedThings, List<Thing> ignoredThings, IntVec3 cell)
        {
            if (t.def.category == ThingCategory.Mote || t.def.category == ThingCategory.Ethereal) return;
            if (damagedThings.Contains(t)) return;
            damagedThings.Add(t);
            //it puts out fires 
            if (def == DamageDefOf.Bomb && t.def == RimWorld.ThingDefOf.Fire && !t.Destroyed)
            {
                t.Destroy();
                return;
            }
            //only affects pawns 

            if (!(t is Pawn pawn))
            {
                ignoredThings?.Add(t);
                return;
            }

            if (!Helper_Regression.CanBeRegressed(pawn))
            {

                ignoredThings?.Add(pawn);
                return; //only affects susceptible pawns 
            }


            float num;
            if (t.Position == explosion.Position)
                num = Rand.RangeInclusive(0, 359);
            else
                num = (t.Position - explosion.Position).AngleFlat;
            DamageDef damageDef = def;
            var amount = (float)explosion.GetDamageAmountAt(cell);
            float armorPenetrationAt = explosion.GetArmorPenetrationAt(cell);
            float angle = num;
            Thing instigator = explosion.instigator;
            ThingDef weapon = explosion.weapon;
            var dinfo = new DamageInfo(damageDef, amount, armorPenetrationAt, angle, instigator, null, weapon,
                                       DamageInfo.SourceCategory.ThingOrUnknown, explosion.intendedTarget);

            Helper_Regression.ApplyPureRegressionDamage(dinfo, pawn,null, amount);

            Find.BattleLog.Add(new BattleLogEntry_ExplosionImpact(explosion.instigator, t, explosion.weapon, explosion.projectile, def));

            pawn.stances?.stagger?.StaggerFor(95);
        }




        /// <summary>Reduces the damage.</summary>
        /// <param name="dInfo">The d information.</param>
        /// <param name="pawn">The pawn.</param>
        /// <returns></returns>
        protected DamageInfo ReduceDamage(DamageInfo dInfo, Pawn pawn)
        {
            return Helper_Regression.ReduceDamage(dInfo);
        }

        private DamageResult ApplyToPawn(DamageInfo dinfo, Pawn pawn)
        {
            //reduce the amount to make it less likely to kill the pawn 
            float originalDamage = dinfo.Amount;

            dinfo = ReduceDamage(dinfo, pawn);

            DamageResult res = base.Apply(dinfo, pawn);
            //float severityPerDamage = dinfo.GetSeverityPerDamage();

            Helper_Regression.ApplyPureRegressionDamage(dinfo, pawn, res, originalDamage);

            return res;
        }
    }
}

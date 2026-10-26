using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static Verse.DamageWorker;

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

            DamageResult res = base.Apply(dinfo, pawn);
            //Helper_Regression.ApplyRegressionPoolDamage(dinfo, pawn, res, SeveritySizeScale(pawn) * amount);

            Find.BattleLog.Add(new BattleLogEntry_ExplosionImpact(explosion.instigator, t, explosion.weapon, explosion.projectile, def));

            pawn.stances?.stagger?.StaggerFor(95);
        }




        /*/// <summary>Reduces the damage.</summary>
        /// <param name="dInfo">The d information.</param>
        /// <param name="pawn">The pawn.</param>
        /// <returns></returns>
        protected DamageInfo ReduceDamage(DamageInfo dInfo, Pawn pawn)
        {
            return Helper_Regression.ReduceDamage(dInfo,pawn);
        }*/

        private DamageResult ApplyToPawn(DamageInfo dInfo, Pawn pawn)
        {
            /*            var emptyRes = new DamageInfo(dInfo.Def, 0f, dInfo.ArmorPenetrationInt, dInfo.Angle, dInfo.Instigator,
                       dInfo.HitPart, dInfo.Weapon, dInfo.Category, dInfo.intendedTargetInt);*/
            var emptyRes = new DamageResult();


            //dinfo = ReduceDamage(dinfo, pawn);
            var hdef = def.hediff;
            var props = hdef?.CompProps<CompProperties_RegressionInfluence>();
            bool wantGlobal = props != null && !props.affectsBodyParts;

            if (wantGlobal)
            {
                // Apply global hediff, skip injury flow entirely
                if (hdef != null && dInfo.Amount > 0f)
                {
                    bool isBeam = dInfo.Weapon is ThingDef weap && weap.Verbs?.Any(v => v.beamDamageDef != null) == true;
                    var hd = pawn.health.AddHediff(hdef, part: null,dInfo, emptyRes);
                    hd.Severity += dInfo.Amount;
                    emptyRes.AddHediff(hd);
                    if (dInfo.Amount > 0)
                    {
                        pawn.MapHeld.damageWatcher.Notify_DamageTaken(pawn, dInfo.Amount);
                    }

                    float blunt = Mathf.Max(dInfo.Amount * 0.1f, 0.1f);
                    dInfo.Def = DamageDefOf.Blunt;
                    dInfo.SetAmount(blunt);
                    dInfo.SetIgnoreArmor(true);
                }
                //return emptyRes; // IMPORTANT: don’t call base.Apply
            }

            DamageResult res = base.Apply(dInfo, pawn);
            //Helper_Regression.ApplyRegressionPoolDamage(dinfo, pawn, res, SeveritySizeScale(pawn) * originalDamage);

            return res;
        }

        /*protected override BodyPartRecord ChooseHitPart(DamageInfo dinfo, Pawn pawn)
        {
            if (def.hediff.CompProps<CompProperties_RegressionInfluence>() is { } c)
            {
                return c.affectsBodyParts ? base.ChooseHitPart(dinfo, pawn) : null;

            }
            return base.ChooseHitPart(dinfo, pawn);
        }*/
    }
}

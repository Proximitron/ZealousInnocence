using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ZealousInnocence
{
    public class LaserGunDef : ThingDef
    {
        public static LaserGunDef defaultObj = new LaserGunDef();

        public float barrelLength = 0.9f;
        public bool supportsColors = false;
    }
    public interface IBeamColorThing
    {
        int BeamColor
        {
            get;
            set;
        }
    }
    interface IDrawnWeaponWithRotation
    {
        float RotationOffset
        {
            get;
            set;
        }
    }
    public class LaserBeamDecoration
    {
        public ThingDef mote;
        public float spacing = 1.0f;
        public float initialOffset = 0;
        public float speed = 1.0f;
        public float speedJitter;
        public float speedJitterOffset;

    }

    public class LaserBeamDef : ThingDef
    {
        public float capSize = 1.0f;
        public float capOverlap = 1.1f / 64;

        public int lifetime = 30;
        public float impulse = 4.0f;

        public float beamWidth = 1.0f;
        public float shieldDamageMultiplier = 0.5f;
        public float seam = -1f;

        public List<LaserBeamDecoration> decorations;

        public EffecterDef explosionEffect;
        public EffecterDef hitLivingEffect;
        public ThingDef beamGraphic;

        public List<string> textures;
        private List<Material> materials = new List<Material>();

        void CreateGraphics()
        {
            for (int i = 0; i < textures.Count; i++)
            {
                materials.Add(MaterialPool.MatFrom(textures[i], ShaderDatabase.TransparentPostLight));
            }
        }

        public Material GetBeamMaterial(int index)
        {
            if (materials.Count == 0 && textures.Count != 0)
                CreateGraphics();

            if (materials.Count == 0)
            {
                return null;
            }

            if (index >= materials.Count || index < 0)
                index = 0;

            return materials[index];
        }

        public bool IsWeakToShields
        {
            get { return shieldDamageMultiplier < 1f; }
        }

    }
    public class SpinningLaserGunDef : LaserGunDef
    {
        public List<GraphicData> frames;
        public float rotationSpeed = 1.0f;
    }
    public abstract class SpinningLaserGunBase : LaserGun
    {
        public enum State
        {
            Idle = 0,
            Spinup = 1,
            Spinning = 2
        };

        int previousTick = 0;
        public State state = State.Idle;

        float rotation = 0;
        float rotationSpeed = 0;
        float targetRotationSpeed;
        float rotationAcceleration = 0;
        int rotationAccelerationTicksRemaing = 0;

        public new SpinningLaserGunDef def
        {
            get { return base.def as SpinningLaserGunDef; }
        }

        public void ReachRotationSpeed(float target, int ticksUntil)
        {
            targetRotationSpeed = target;

            if (ticksUntil <= 0)
            {
                rotationAccelerationTicksRemaing = 0;
                rotationSpeed = target;
            }

            rotationAccelerationTicksRemaing = ticksUntil;
            rotationAcceleration = (target - rotationSpeed) / ticksUntil;
        }

        private Graphic GetGraphicForTick(int ticksPassed)
        {
            if (rotationAccelerationTicksRemaing > 0)
            {
                if (ticksPassed > rotationAccelerationTicksRemaing)
                    ticksPassed = rotationAccelerationTicksRemaing;

                rotationAccelerationTicksRemaing -= ticksPassed;
                rotationSpeed += ticksPassed * rotationAcceleration;

                if (rotationAccelerationTicksRemaing <= 0)
                {
                    rotationSpeed = targetRotationSpeed;
                }
            }

            rotation += rotationSpeed * ticksPassed;

            int frame = ((int)rotation) % def.frames.Count;
            return def.frames[frame].Graphic;
        }

        public abstract void UpdateState();

        public override Graphic Graphic
        {
            get
            {
                if (def.frames.Count == 0) return DefaultGraphic;

                UpdateState();

                var tick = Find.TickManager.TicksGame;
                var res = GetGraphicForTick(tick - previousTick);
                previousTick = tick;

                return res;
            }
        }
    }
    class SpinningLaserGun : SpinningLaserGunBase
    {
        bool IsBrusting(Pawn pawn)
        {
            if (pawn.CurrentEffectiveVerb == null) return false;
            return pawn.CurrentEffectiveVerb.Bursting;
        }

        public override void UpdateState()
        {
            var holder = ParentHolder as Pawn_EquipmentTracker;
            if (holder == null) return;

            Stance stance = holder.pawn.stances.curStance;
            Stance_Warmup warmup;

            switch (state)
            {
                case State.Idle:
                    warmup = stance as Stance_Warmup;
                    if (warmup != null)
                    {
                        state = State.Spinup;
                        ReachRotationSpeed(def.rotationSpeed, warmup.ticksLeft);
                    }
                    break;
                case State.Spinup:
                    if (IsBrusting(holder.pawn))
                    {
                        state = State.Spinning;
                    }
                    else
                    {
                        warmup = stance as Stance_Warmup;
                        if (warmup == null)
                        {
                            state = State.Idle;
                            ReachRotationSpeed(0.0f, 30);
                        }
                    }
                    break;
                case State.Spinning:
                    if (!IsBrusting(holder.pawn))
                    {
                        state = State.Idle;
                        Stance_Cooldown cooldown = stance as Stance_Cooldown;
                        if (cooldown != null)
                            ReachRotationSpeed(0.0f, cooldown.ticksLeft);
                        else
                            ReachRotationSpeed(0.0f, 0);
                    }
                    break;
            }
        }
    }
}

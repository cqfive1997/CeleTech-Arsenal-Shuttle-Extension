using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// CE outbound interceptor. CE continues to own trajectory/collision; this class adds the
    /// authored homing acceleration and target-specific interception result only.
    /// </summary>
    public sealed class CeShuttlePointDefenseBullet : BulletCE
    {
        public override void Tick()
        {
            ShuttlePointDefenseProjectileExtension extension = this.PointDefenseExtension;
            if (this.PointDefenseTarget != null &&
                extension != null && extension.homingAcceleration > 0f &&
                !float.IsNaN(extension.homingAcceleration) &&
                !float.IsInfinity(extension.homingAcceleration))
            {
                this.homingAcceleration = extension.homingAcceleration;
            }

            base.Tick();
        }

        public override void Impact(Thing hitThing)
        {
            Thing target = this.PointDefenseTarget;
            if (target == null)
            {
                base.Impact(hitThing);
                return;
            }

            ShuttlePointDefenseProjectileExtension extension = this.PointDefenseExtension;
            if (!target.Destroyed && target.Spawned && target.Map == this.Map &&
                extension != null && this.IsWithinInterceptionRadius(target, extension))
            {
                this.ThrowInterceptFleck(target, extension);
                float chance = Mathf.Clamp01(extension.interceptionChance);
                if (chance > 0f && Rand.Chance(chance))
                {
                    ProjectileCE ceTarget = target as ProjectileCE;
                    if (ceTarget != null)
                    {
                        ceTarget.InterceptProjectile(this, ceTarget.ExactPosition, true);
                    }
                    else
                    {
                        target.Destroy(DestroyMode.Vanish);
                    }
                }
            }

            if (!this.Destroyed)
            {
                this.Destroy(DestroyMode.Vanish);
            }
        }

        private ShuttlePointDefenseProjectileExtension PointDefenseExtension
        {
            get
            {
                return this.def != null
                    ? this.def.GetModExtension<ShuttlePointDefenseProjectileExtension>()
                    : null;
            }
        }

        private Thing PointDefenseTarget
        {
            get
            {
                Thing target = this.intendedTarget.HasThing
                    ? this.intendedTarget.Thing
                    : null;
                return target is ProjectileCE || target is Projectile
                    ? target
                    : null;
            }
        }

        private bool IsWithinInterceptionRadius(
            Thing target,
            ShuttlePointDefenseProjectileExtension extension)
        {
            float radius = extension.interceptionRadius;
            if (radius <= 0f || float.IsNaN(radius) || float.IsInfinity(radius))
            {
                return false;
            }

            Vector3 delta = target.DrawPos - this.ExactPosition;
            return delta.x * delta.x + delta.z * delta.z <= radius * radius;
        }

        private void ThrowInterceptFleck(
            Thing target,
            ShuttlePointDefenseProjectileExtension extension)
        {
            if (extension.impactFleck == null || this.Map == null)
            {
                return;
            }

            float scale = extension.impactFleckScale > 0f &&
                !float.IsNaN(extension.impactFleckScale) &&
                !float.IsInfinity(extension.impactFleckScale)
                    ? extension.impactFleckScale
                    : 1f;
            FleckMaker.Static(target.DrawPos, this.Map, extension.impactFleck, scale);
        }
    }
}

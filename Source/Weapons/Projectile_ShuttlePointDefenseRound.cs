using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Weapons
{
    /// <summary>
    /// Native outbound interceptor. Projectile-domain impacts only ever evaluate the one assigned
    /// target; ordinary Thing targets retain the vanilla Bullet impact path.
    /// </summary>
    public sealed class Projectile_ShuttlePointDefenseRound : Bullet
    {
        protected override void Tick()
        {
            Thing target = this.PointDefenseTarget;
            if (target != null && !target.Destroyed && target.Spawned && target.Map == this.Map)
            {
                this.destination = target.DrawPos;
            }

            base.Tick();
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Thing target = this.PointDefenseTarget;
            if (target == null)
            {
                base.Impact(hitThing, blockedByShield);
                return;
            }

            ShuttlePointDefenseProjectileExtension extension =
                this.def.GetModExtension<ShuttlePointDefenseProjectileExtension>();
            if (!target.Destroyed && target.Spawned && target.Map == this.Map &&
                extension != null && this.IsWithinInterceptionRadius(target, extension))
            {
                this.ThrowInterceptFleck(target, extension);
                float chance = Mathf.Clamp01(extension.interceptionChance);
                if (chance > 0f && Rand.Chance(chance))
                {
                    target.Destroy(DestroyMode.Vanish);
                }
            }

            if (!this.Destroyed)
            {
                this.Destroy(DestroyMode.Vanish);
            }
        }

        private Thing PointDefenseTarget
        {
            get
            {
                Thing target = this.intendedTarget.HasThing
                    ? this.intendedTarget.Thing
                    : null;
                return target is Projectile ? target : null;
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

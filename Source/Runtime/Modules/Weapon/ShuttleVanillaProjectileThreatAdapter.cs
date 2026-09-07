using System;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Translates vanilla Verse projectiles without private-field reflection or launch patches.
    /// ETA is derived from remaining horizontal distance and the authored projectile speed.
    /// </summary>
    internal sealed class ShuttleVanillaProjectileThreatAdapter : IShuttleProjectileThreatAdapter
    {
        public string AdapterId
        {
            get { return "vanilla"; }
        }

        public bool TryCreateThreat(Thing thing, out ShuttleProjectileThreat threat)
        {
            threat = null;
            Projectile projectile = thing as Projectile;
            ProjectileProperties properties = projectile != null && projectile.def != null
                ? projectile.def.projectile
                : null;
            if (projectile == null || properties == null ||
                projectile.Destroyed || !projectile.Spawned || projectile.Map == null)
            {
                return false;
            }

            LocalTargetInfo intendedTarget = projectile.intendedTarget.IsValid
                ? projectile.intendedTarget
                : projectile.usedTarget;
            if (!intendedTarget.IsValid || !intendedTarget.Cell.IsValid)
            {
                return false;
            }

            float speedTilesPerTick = properties.SpeedTilesPerTick;
            if (!IsFinitePositive(speedTilesPerTick))
            {
                return false;
            }

            Vector3 destination = intendedTarget.HasThing && intendedTarget.Thing != null
                ? intendedTarget.Thing.DrawPos
                : intendedTarget.Cell.ToVector3Shifted();
            Vector3 remaining = destination - projectile.ExactPosition;
            float horizontalDistance = (float)Math.Sqrt(
                remaining.x * remaining.x + remaining.z * remaining.z);
            int ticksToImpact = Math.Max(
                1,
                (int)Math.Ceiling(horizontalDistance / speedTilesPerTick));

            threat = new ShuttleProjectileThreat(
                projectile,
                projectile.Launcher,
                intendedTarget.Cell,
                ticksToImpact,
                properties.speed,
                projectile.DamageAmount,
                properties.flyOverhead || properties.explosionRadius > 0f);
            return true;
        }

        private static bool IsFinitePositive(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

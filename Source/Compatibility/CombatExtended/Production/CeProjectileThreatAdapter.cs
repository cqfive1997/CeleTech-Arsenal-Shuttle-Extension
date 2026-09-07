using System;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Typed CE projectile translation. No CE type crosses into the main assembly contract.
    /// </summary>
    internal sealed class CeProjectileThreatAdapter : IShuttleProjectileThreatAdapter
    {
        public string AdapterId
        {
            get { return "combat-extended"; }
        }

        public bool TryCreateThreat(Thing thing, out ShuttleProjectileThreat threat)
        {
            threat = null;
            ProjectileCE projectile = thing as ProjectileCE;
            ProjectileProperties properties = projectile != null && projectile.def != null
                ? projectile.def.projectile
                : null;
            if (projectile == null || properties == null || projectile.Destroyed ||
                !projectile.Spawned || projectile.Map == null ||
                !projectile.intendedTarget.IsValid ||
                !projectile.intendedTarget.Cell.IsValid)
            {
                return false;
            }

            float speed = properties.speed;
            if (speed <= 0f || float.IsNaN(speed) || float.IsInfinity(speed))
            {
                return false;
            }

            threat = new ShuttleProjectileThreat(
                projectile,
                projectile.launcher,
                projectile.intendedTarget.Cell,
                Math.Max(1, projectile.ticksToImpact),
                speed,
                projectile.DamageAmount,
                properties.flyOverhead || properties.explosionRadius > 0f);
            return true;
        }
    }
}

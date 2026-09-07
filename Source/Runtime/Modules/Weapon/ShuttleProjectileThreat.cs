using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Detached, backend-neutral observation of one live projectile. It is rebuilt during a
    /// scheduled target scan and is never saved or placed in ShuttleProfile.
    /// </summary>
    internal sealed class ShuttleProjectileThreat
    {
        internal ShuttleProjectileThreat(
            Thing projectile,
            Thing launcher,
            IntVec3 intendedImpactCell,
            int ticksToImpact,
            float speedCellsPerSecond,
            float damageAmount,
            bool explosiveOrOverhead)
        {
            this.Projectile = projectile;
            this.Launcher = launcher;
            this.IntendedImpactCell = intendedImpactCell;
            this.TicksToImpact = ticksToImpact;
            this.SpeedCellsPerSecond = speedCellsPerSecond;
            this.DamageAmount = damageAmount;
            this.ExplosiveOrOverhead = explosiveOrOverhead;
        }

        internal Thing Projectile { get; private set; }

        internal Thing Launcher { get; private set; }

        internal IntVec3 IntendedImpactCell { get; private set; }

        internal int TicksToImpact { get; private set; }

        internal float SpeedCellsPerSecond { get; private set; }

        internal float DamageAmount { get; private set; }

        internal bool ExplosiveOrOverhead { get; private set; }
    }

    internal interface IShuttleProjectileThreatAdapter
    {
        string AdapterId { get; }

        bool TryCreateThreat(Thing thing, out ShuttleProjectileThreat threat);
    }
}

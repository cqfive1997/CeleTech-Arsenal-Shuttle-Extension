using System;
using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed class CompProperties_ShuttlePointDefenseAssignments : CompProperties
    {
        public CompProperties_ShuttlePointDefenseAssignments()
        {
            this.compClass = typeof(CompShuttlePointDefenseAssignments);
        }
    }

    /// <summary>
    /// Per-shuttle, transient assignment leases used only to avoid point-defense overkill.
    /// It does not enumerate threats, choose targets, tick weapons, or persist references.
    /// </summary>
    public sealed class CompShuttlePointDefenseAssignments : ThingComp
    {
        private readonly List<AssignmentLease> leases = new List<AssignmentLease>();

        internal int CountAssignments(Thing projectile, int ticksGame)
        {
            this.Prune(ticksGame);
            if (projectile == null || projectile.Destroyed)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < this.leases.Count; i++)
            {
                if (ReferenceEquals(this.leases[i].Projectile, projectile))
                {
                    count++;
                }
            }

            return count;
        }

        internal bool TryClaim(
            Thing projectile,
            string moduleInstanceID,
            int maximumAssignments,
            int ticksGame,
            int leaseTicks)
        {
            this.Prune(ticksGame);
            if (projectile == null || projectile.Destroyed ||
                string.IsNullOrEmpty(moduleInstanceID) || maximumAssignments <= 0)
            {
                return false;
            }

            int expiresAtTick = SaturatingAdd(ticksGame, Math.Max(1, leaseTicks));
            for (int i = this.leases.Count - 1; i >= 0; i--)
            {
                AssignmentLease lease = this.leases[i];
                if (lease.ModuleInstanceID != moduleInstanceID)
                {
                    continue;
                }

                if (ReferenceEquals(lease.Projectile, projectile))
                {
                    lease.ExpiresAtTick = expiresAtTick;
                    return true;
                }

                this.leases.RemoveAt(i);
            }

            if (this.CountAssignments(projectile, ticksGame) >= maximumAssignments)
            {
                return false;
            }

            this.leases.Add(new AssignmentLease(
                projectile,
                moduleInstanceID,
                expiresAtTick));
            return true;
        }

        internal bool TryRefresh(
            Thing projectile,
            string moduleInstanceID,
            int ticksGame,
            int leaseTicks)
        {
            this.Prune(ticksGame);
            if (projectile == null || projectile.Destroyed ||
                string.IsNullOrEmpty(moduleInstanceID))
            {
                return false;
            }

            int expiresAtTick = SaturatingAdd(ticksGame, Math.Max(1, leaseTicks));
            for (int i = 0; i < this.leases.Count; i++)
            {
                AssignmentLease lease = this.leases[i];
                if (lease.ModuleInstanceID == moduleInstanceID &&
                    ReferenceEquals(lease.Projectile, projectile))
                {
                    lease.ExpiresAtTick = expiresAtTick;
                    return true;
                }
            }

            return false;
        }

        private void Prune(int ticksGame)
        {
            for (int i = this.leases.Count - 1; i >= 0; i--)
            {
                AssignmentLease lease = this.leases[i];
                if (lease == null || lease.Projectile == null || lease.Projectile.Destroyed ||
                    lease.ExpiresAtTick <= ticksGame)
                {
                    this.leases.RemoveAt(i);
                }
            }
        }

        private static int SaturatingAdd(int left, int right)
        {
            if (left < 0)
            {
                left = 0;
            }

            if (right <= 0)
            {
                return left;
            }

            return left > int.MaxValue - right ? int.MaxValue : left + right;
        }

        private sealed class AssignmentLease
        {
            internal AssignmentLease(
                Thing projectile,
                string moduleInstanceID,
                int expiresAtTick)
            {
                this.Projectile = projectile;
                this.ModuleInstanceID = moduleInstanceID;
                this.ExpiresAtTick = expiresAtTick;
            }

            internal Thing Projectile;
            internal string ModuleInstanceID;
            internal int ExpiresAtTick;
        }
    }
}

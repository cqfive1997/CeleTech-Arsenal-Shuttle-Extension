using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Diagnostics;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns
{
    internal sealed class ShuttlePawnPresenceSnapshotCache
    {
        private readonly ShuttlePawnPresenceBuilder builder =
            new ShuttlePawnPresenceBuilder();

        private ShuttlePawnPresenceSnapshot cachedSnapshot;
        private ShuttlePawnPresenceSnapshotCacheKey cachedKey;
        private bool dirty = true;

        internal void MarkDirty()
        {
            this.dirty = true;
        }

        internal ShuttlePawnPresenceSnapshot GetSnapshotForUI(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot,
            int cargoSnapshotRevision)
        {
            ShuttlePawnPresenceSnapshotCacheKey key;
            try
            {
                key = ShuttlePawnPresenceSnapshotCacheKey.Create(
                    controlModel,
                    cargoSnapshot,
                    cargoSnapshotRevision);
            }
            catch (Exception exception)
            {
                ShuttleReadModelFailureLogger.LogOnce(
                    "CreateShuttlePawnPresenceSnapshotCacheKey",
                    exception);
                return this.cachedSnapshot ?? ShuttlePawnPresenceSnapshot.Empty;
            }

            if (!this.dirty &&
                this.cachedSnapshot != null &&
                this.cachedKey != null &&
                this.cachedKey.Matches(key))
            {
                return this.cachedSnapshot;
            }

            ShuttlePawnPresenceSnapshot refreshedSnapshot;
            if (!this.TryBuildSnapshotSafe(
                controlModel,
                cargoSnapshot,
                cargoSnapshotRevision,
                out refreshedSnapshot))
            {
                return this.cachedSnapshot ?? ShuttlePawnPresenceSnapshot.Empty;
            }

            this.cachedSnapshot =
                refreshedSnapshot ?? ShuttlePawnPresenceSnapshot.Empty;
            this.cachedKey = key;
            this.dirty = false;
            return this.cachedSnapshot;
        }

        private bool TryBuildSnapshotSafe(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot,
            int cargoSnapshotRevision,
            out ShuttlePawnPresenceSnapshot snapshot)
        {
            snapshot = ShuttlePawnPresenceSnapshot.Empty;
            try
            {
                using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.PawnPresenceBuild))
                {
                    snapshot = this.builder.Build(
                        controlModel,
                        cargoSnapshot,
                        cargoSnapshotRevision);
                    return true;
                }
            }
            catch (Exception exception)
            {
                ShuttleReadModelFailureLogger.LogOnce(
                    "BuildShuttlePawnPresenceSnapshot",
                    exception);
                return false;
            }
        }
    }
}

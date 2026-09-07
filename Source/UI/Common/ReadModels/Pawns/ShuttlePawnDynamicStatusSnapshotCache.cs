using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Diagnostics;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns
{
    internal sealed class ShuttlePawnDynamicStatusSnapshotCache
    {
        private readonly ShuttlePawnDynamicStatusBuilder builder =
            new ShuttlePawnDynamicStatusBuilder();

        private ShuttlePawnDynamicStatusSnapshot cachedSnapshot;
        private ShuttlePawnDynamicStatusSnapshotCacheKey cachedKey;
        private bool dirty = true;

        internal void MarkDirty()
        {
            this.dirty = true;
        }

        internal ShuttlePawnDynamicStatusSnapshot GetSnapshotForUI(
            ShuttlePawnPresenceSnapshot presenceSnapshot,
            ShuttlePawnPresenceKind includedKinds)
        {
            ShuttlePawnDynamicStatusSnapshotCacheKey key;
            try
            {
                key = ShuttlePawnDynamicStatusSnapshotCacheKey.Create(
                    presenceSnapshot,
                    includedKinds);
            }
            catch (Exception exception)
            {
                ShuttleReadModelFailureLogger.LogOnce(
                    "CreateShuttlePawnDynamicStatusSnapshotCacheKey",
                    exception);
                return this.cachedSnapshot ?? ShuttlePawnDynamicStatusSnapshot.Empty;
            }

            if (!this.dirty &&
                this.cachedSnapshot != null &&
                this.cachedKey != null &&
                this.cachedKey.Matches(key))
            {
                return this.cachedSnapshot;
            }

            ShuttlePawnDynamicStatusSnapshot refreshedSnapshot;
            if (!this.TryBuildSnapshotSafe(
                presenceSnapshot,
                includedKinds,
                out refreshedSnapshot))
            {
                return this.cachedSnapshot ?? ShuttlePawnDynamicStatusSnapshot.Empty;
            }

            this.cachedSnapshot =
                refreshedSnapshot ?? ShuttlePawnDynamicStatusSnapshot.Empty;
            this.cachedKey = key;
            this.dirty = false;
            return this.cachedSnapshot;
        }

        private bool TryBuildSnapshotSafe(
            ShuttlePawnPresenceSnapshot presenceSnapshot,
            ShuttlePawnPresenceKind includedKinds,
            out ShuttlePawnDynamicStatusSnapshot snapshot)
        {
            snapshot = ShuttlePawnDynamicStatusSnapshot.Empty;
            try
            {
                using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.PawnDynamicStatusBuild))
                {
                    snapshot = this.builder.Build(
                        presenceSnapshot,
                        includedKinds);
                    return true;
                }
            }
            catch (Exception exception)
            {
                ShuttleReadModelFailureLogger.LogOnce(
                    "BuildShuttlePawnDynamicStatusSnapshot",
                    exception);
                return false;
            }
        }
    }
}

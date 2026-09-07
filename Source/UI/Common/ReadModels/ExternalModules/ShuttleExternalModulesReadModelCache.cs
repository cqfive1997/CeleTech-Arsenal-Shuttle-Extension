using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Diagnostics;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.ExternalModules
{
    internal sealed class ShuttleExternalModulesReadModelCache
    {
        private readonly IShuttleExternalModuleUIReadPort readPort;
        private readonly ShuttleControlReadModelRefreshPolicy refreshPolicy =
            new ShuttleControlReadModelRefreshPolicy();
        private readonly IReadOnlyList<ExternalModuleUIReadModel> emptyModels =
            new List<ExternalModuleUIReadModel>();

        private IReadOnlyList<ExternalModuleUIReadModel> cachedFullModels;
        private ShuttleExternalModulesReadModelCacheKey cachedFullKey;
        private int cachedFullTick = int.MinValue;
        private bool fullDirty = true;

        private IReadOnlyList<ExternalModuleUIReadModel> cachedBadgeModels;
        private ShuttleExternalModulesReadModelCacheKey cachedBadgeKey;
        private int cachedBadgeTick = int.MinValue;
        private bool badgeDirty = true;

        internal ShuttleExternalModulesReadModelCache(
            IShuttleExternalModuleUIReadPort readPort)
        {
            this.readPort = readPort;
        }

        internal void MarkDirty()
        {
            this.fullDirty = true;
            this.badgeDirty = true;
        }

        internal IReadOnlyList<ExternalModuleUIReadModel> GetExternalModuleModelsForUI(
            ShuttleControlReadModel controlModel,
            string selectedModuleId,
            string selectedRuntimeSystemKey)
        {
            ShuttleExternalModulesReadModelCacheKey key =
                ShuttleExternalModulesReadModelCacheKey.Create(
                    controlModel,
                    selectedModuleId,
                    selectedRuntimeSystemKey);
            bool cacheMissing = this.cachedFullModels == null;
            if (!this.NeedsRefresh(
                this.cachedFullModels,
                this.cachedFullKey,
                key,
                this.cachedFullTick,
                this.fullDirty))
            {
                return this.cachedFullModels;
            }

            if (!this.refreshPolicy.CanRefreshHeavyUINow(cacheMissing))
            {
                return this.cachedFullModels ?? this.emptyModels;
            }

            IReadOnlyList<ExternalModuleUIReadModel> refreshedModels;
            if (!this.TryBuildFullModelsSafe(out refreshedModels))
            {
                return this.cachedFullModels ?? this.emptyModels;
            }

            this.cachedFullModels = refreshedModels;
            this.cachedFullKey = key;
            this.cachedFullTick = this.refreshPolicy.GetUITicks();
            this.fullDirty = false;
            return this.cachedFullModels;
        }

        internal IReadOnlyList<ExternalModuleUIReadModel> GetExternalModuleBadgeModelsForUI(
            ShuttleControlReadModel controlModel)
        {
            ShuttleExternalModulesReadModelCacheKey key =
                ShuttleExternalModulesReadModelCacheKey.Create(
                    controlModel,
                    null,
                    null);
            bool cacheMissing = this.cachedBadgeModels == null;
            if (!this.NeedsRefresh(
                this.cachedBadgeModels,
                this.cachedBadgeKey,
                key,
                this.cachedBadgeTick,
                this.badgeDirty))
            {
                return this.cachedBadgeModels;
            }

            if (!this.refreshPolicy.CanRefreshHeavyUINow(cacheMissing))
            {
                return this.cachedBadgeModels ?? this.emptyModels;
            }

            IReadOnlyList<ExternalModuleUIReadModel> refreshedModels;
            if (!this.TryBuildBadgeModelsSafe(out refreshedModels))
            {
                return this.cachedBadgeModels ?? this.emptyModels;
            }

            this.cachedBadgeModels = refreshedModels;
            this.cachedBadgeKey = key;
            this.cachedBadgeTick = this.refreshPolicy.GetUITicks();
            this.badgeDirty = false;
            return this.cachedBadgeModels;
        }

        private bool NeedsRefresh(
            IReadOnlyList<ExternalModuleUIReadModel> cachedModels,
            ShuttleExternalModulesReadModelCacheKey cachedKey,
            ShuttleExternalModulesReadModelCacheKey currentKey,
            int cachedTick,
            bool dirty)
        {
            return cachedModels == null ||
                cachedKey == null ||
                !cachedKey.Matches(currentKey) ||
                this.refreshPolicy.ShouldRefresh(
                    cachedTick,
                    this.refreshPolicy.GetHeavyUICacheRefreshTicks(),
                    dirty);
        }

        private bool TryBuildFullModelsSafe(
            out IReadOnlyList<ExternalModuleUIReadModel> models)
        {
            models = this.emptyModels;
            try
            {
                using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.ExternalModulesFullRead))
                {
                    models = this.readPort != null
                        ? (this.readPort.BuildExternalModuleUIReadModels() ?? this.emptyModels)
                        : this.emptyModels;
                    return true;
                }
            }
            catch (Exception exception)
            {
                ShuttleReadModelFailureLogger.LogOnce(
                    "BuildExternalModuleUIReadModels",
                    exception);
                return false;
            }
        }

        private bool TryBuildBadgeModelsSafe(
            out IReadOnlyList<ExternalModuleUIReadModel> models)
        {
            models = this.emptyModels;
            try
            {
                using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.ExternalModulesBadgeRead))
                {
                    models = this.readPort != null
                        ? (this.readPort.BuildExternalModuleUIBadgeReadModels() ?? this.emptyModels)
                        : this.emptyModels;
                    return true;
                }
            }
            catch (Exception exception)
            {
                ShuttleReadModelFailureLogger.LogOnce(
                    "BuildExternalModuleUIBadgeReadModels",
                    exception);
                return false;
            }
        }
    }
}

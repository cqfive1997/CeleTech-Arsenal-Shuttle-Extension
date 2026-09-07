using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.External;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.ExternalModules
{
    internal sealed class ShuttleExternalModulesReadModelCacheKey
    {
        internal readonly int RuntimeRegistryRevision;
        internal readonly int PanelRegistryRevision;
        internal readonly int ControlFingerprint;
        internal readonly string SelectedModuleId;
        internal readonly string SelectedRuntimeSystemKey;

        private ShuttleExternalModulesReadModelCacheKey(
            int runtimeRegistryRevision,
            int panelRegistryRevision,
            int controlFingerprint,
            string selectedModuleId,
            string selectedRuntimeSystemKey)
        {
            this.RuntimeRegistryRevision = runtimeRegistryRevision;
            this.PanelRegistryRevision = panelRegistryRevision;
            this.ControlFingerprint = controlFingerprint;
            this.SelectedModuleId = selectedModuleId;
            this.SelectedRuntimeSystemKey = selectedRuntimeSystemKey;
        }

        internal static ShuttleExternalModulesReadModelCacheKey Create(
            ShuttleControlReadModel controlModel,
            string selectedModuleId,
            string selectedRuntimeSystemKey)
        {
            return new ShuttleExternalModulesReadModelCacheKey(
                ExternalShuttleRuntimeRegistry.Revision,
                ExternalModulePanelRegistry.Revision,
                BuildControlFingerprint(controlModel),
                NormalizeOptionalKey(selectedModuleId),
                NormalizeOptionalKey(selectedRuntimeSystemKey));
        }

        internal bool Matches(ShuttleExternalModulesReadModelCacheKey other)
        {
            return other != null &&
                this.RuntimeRegistryRevision == other.RuntimeRegistryRevision &&
                this.PanelRegistryRevision == other.PanelRegistryRevision &&
                this.ControlFingerprint == other.ControlFingerprint &&
                string.Equals(this.SelectedModuleId, other.SelectedModuleId, StringComparison.Ordinal) &&
                string.Equals(this.SelectedRuntimeSystemKey, other.SelectedRuntimeSystemKey, StringComparison.Ordinal);
        }

        private static int BuildControlFingerprint(ShuttleControlReadModel controlModel)
        {
            unchecked
            {
                int hash = 17;
                if (controlModel == null)
                {
                    return hash;
                }

                hash = AddHash(hash, controlModel.ProfileRevision);
                hash = AddHash(hash, controlModel.IsProfileDirty ? 1 : 0);
                hash = AddHash(hash, controlModel.SegmentSlotCount);
                hash = AddHash(hash, controlModel.InstalledSegmentCount);
                hash = AddHash(hash, controlModel.ModuleSlotCount);
                hash = AddHash(hash, controlModel.InstalledModuleCount);
                hash = AddHash(
                    hash,
                    controlModel.PageAvailability != null &&
                    controlModel.PageAvailability.ExternalModules ? 1 : 0);

                if (controlModel.SegmentSlots == null)
                {
                    return hash;
                }

                hash = AddHash(hash, controlModel.SegmentSlots.Count);
                for (int i = 0; i < controlModel.SegmentSlots.Count; i++)
                {
                    hash = AddSegmentHash(hash, controlModel.SegmentSlots[i]);
                }

                return hash;
            }
        }

        private static int AddSegmentHash(
            int hash,
            ShuttleControlSegmentSlotModel segment)
        {
            unchecked
            {
                if (segment == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddStringHash(hash, segment.SlotID);
                hash = AddStringHash(hash, segment.SlotTypeID);
                hash = AddStringHash(hash, segment.InstalledSegmentInstanceID);
                hash = AddStringHash(hash, segment.InstalledSegmentDefName);
                hash = AddStringHash(hash, segment.SegmentTypeID);
                hash = AddHash(hash, segment.IsLocked ? 1 : 0);
                hash = AddHash(hash, segment.IsRemovalInProgress ? 1 : 0);

                if (segment.ModuleSlots == null)
                {
                    return hash;
                }

                hash = AddHash(hash, segment.ModuleSlots.Count);
                for (int i = 0; i < segment.ModuleSlots.Count; i++)
                {
                    hash = AddModuleHash(hash, segment.ModuleSlots[i]);
                }

                return hash;
            }
        }

        private static int AddModuleHash(
            int hash,
            ShuttleControlModuleSlotModel module)
        {
            unchecked
            {
                if (module == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddStringHash(hash, module.SlotID);
                hash = AddStringHash(hash, module.SlotTypeID);
                hash = AddStringHash(hash, module.InstalledModuleInstanceID);
                hash = AddStringHash(hash, module.InstalledModuleDefName);
                hash = AddStringHash(hash, module.InstalledModuleTypeID);
                hash = AddHash(hash, module.InstalledModuleEnabled ? 1 : 0);
                hash = AddHash(hash, module.IsLocked ? 1 : 0);
                hash = AddHash(hash, module.IsRemovalInProgress ? 1 : 0);
                return hash;
            }
        }

        private static int AddHash(int hash, int value)
        {
            unchecked
            {
                return (hash * 397) ^ value;
            }
        }

        private static int AddStringHash(int hash, string value)
        {
            unchecked
            {
                return AddHash(hash, StableStringHash(value));
            }
        }

        private static int StableStringHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            unchecked
            {
                int hash = 23;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = (hash * 397) ^ value[i];
                }

                return hash;
            }
        }

        private static string NormalizeOptionalKey(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}

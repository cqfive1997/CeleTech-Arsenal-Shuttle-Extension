using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoProjectionKeyBuilder
    {
        internal V3CargoProjectionKey Build(V3CargoPageInputs inputs)
        {
            ShuttleControlReadModel controlModel =
                inputs != null ? inputs.ControlModel : null;
            ShuttleCargoSnapshot cargoSnapshot =
                inputs != null ? inputs.CargoSnapshot : null;
            IReadOnlyList<ShuttleCargoRegionReadModel> cargoRegionSettings =
                inputs != null ? inputs.CargoRegionSettings : null;

            return new V3CargoProjectionKey(
                controlModel != null ? controlModel.ProfileRevision : 0,
                inputs != null ? inputs.CargoSnapshotRevision : 0,
                this.BuildCargoSnapshotShapeKey(cargoSnapshot),
                this.BuildControlShapeKey(controlModel),
                this.BuildCargoRegionSettingsShapeKey(cargoRegionSettings),
                this.BuildUISettingShapeKey());
        }

        private int BuildCargoSnapshotShapeKey(
            ShuttleCargoSnapshot cargoSnapshot)
        {
            if (cargoSnapshot == null)
            {
                return 0;
            }

            int key = 17;
            key = AddHash(key, BoolHash(cargoSnapshot.HasTransporter));
            key = AddHash(key, cargoSnapshot.CargoRegionCount);
            key = AddHash(key, cargoSnapshot.StackCount);
            key = AddHash(key, cargoSnapshot.ThingCount);
            key = AddHash(key, cargoSnapshot.LoadedStackCount);
            key = AddHash(key, cargoSnapshot.LoadedThingCount);
            key = AddHash(key, cargoSnapshot.AssignedStackCount);
            key = AddHash(key, cargoSnapshot.AssignedThingCount);
            key = AddHash(key, cargoSnapshot.BlockedStackCount);
            key = AddHash(key, cargoSnapshot.RefrigeratedStackCount);
            key = AddHash(key, cargoSnapshot.RefrigeratedThingCount);
            key = AddHash(key, cargoSnapshot.MedicalBayPatientCount);
            key = AddHash(key, cargoSnapshot.ExternalRuntimeMassContributionCount);
            key = AddHash(
                key,
                cargoSnapshot.Items != null ? cargoSnapshot.Items.Count : 0);
            key = AddHash(
                key,
                cargoSnapshot.RefrigeratedCargoModules != null
                    ? cargoSnapshot.RefrigeratedCargoModules.Count
                    : 0);
            key = AddHash(key, FloatHash(cargoSnapshot.StructuralMass));
            key = AddHash(key, FloatHash(cargoSnapshot.LoadedMassKg));
            key = AddHash(key, FloatHash(cargoSnapshot.QueuedMassKg));
            key = AddHash(key, FloatHash(cargoSnapshot.TotalPlannedMassKg));
            key = AddHash(key, FloatHash(cargoSnapshot.ExternalRuntimeMassKg));
            key = AddHash(key, FloatHash(cargoSnapshot.CargoMassUsage));
            key = AddHash(key, FloatHash(cargoSnapshot.MassUsage));
            key = AddHash(key, FloatHash(cargoSnapshot.MassCapacity));
            key = AddHash(key, FloatHash(cargoSnapshot.RefrigeratedMassKg));
            key = AddHash(
                key,
                BoolHash(cargoSnapshot.RefrigeratedMassSharesOverallCapacity));
            key = AddHash(
                key,
                FloatHash(cargoSnapshot.RefrigeratedMassCapacityKg));
            key = AddHash(key, FloatHash(cargoSnapshot.MedicalBayPatientMassKg));
            return key;
        }

        private int BuildControlShapeKey(ShuttleControlReadModel model)
        {
            if (model == null)
            {
                return 0;
            }

            int key = 17;
            key = AddHash(key, BoolHash(model.InternalBusPowered));
            key = AddHash(key, BoolHash(model.HasCargoLogistics));
            key = AddHash(
                key,
                BoolHash(model.CargoLogisticsSupportsItemTransfer));
            key = AddHash(
                key,
                BoolHash(model.CargoLogisticsSupportsItemConsumption));
            key = AddHash(
                key,
                BoolHash(model.CargoLogisticsSupportsItemDeposit));
            key = AddHash(
                key,
                BoolHash(model.CargoLogisticsSupportsNutritionDistribution));
            key = AddHash(key, model.CargoLogisticsMaxStacksMovedPerTick);
            key = AddHash(key, (int)model.CargoRegionCount);
            key = AddHash(key, FloatHash(model.CargoMassCapacityKg));
            return key;
        }

        private int BuildCargoRegionSettingsShapeKey(
            IReadOnlyList<ShuttleCargoRegionReadModel> settings)
        {
            if (settings == null)
            {
                return 0;
            }

            int key = 17;
            key = AddHash(key, settings.Count);
            for (int i = 0; i < settings.Count; i++)
            {
                ShuttleCargoRegionReadModel region = settings[i];
                if (region == null)
                {
                    key = AddHash(key, 0);
                    continue;
                }

                int regionKey = 17;
                regionKey = AddHash(regionKey, region.RegionIndex);
                regionKey = AddHash(regionKey, StringHash(region.Label));
                regionKey = AddHash(regionKey, BoolHash(region.HasCustomLabel));
                regionKey = AddHash(regionKey, BoolHash(region.AllowHumans));
                regionKey = AddHash(regionKey, BoolHash(region.AllowAnimals));
                regionKey = AddHash(regionKey, BoolHash(region.AllowMechs));
                regionKey = AddHash(
                    regionKey,
                    this.BuildThingFilterShapeKey(region.ItemFilter));
                key = AddHash(key, regionKey);
            }

            return key;
        }

        private int BuildUISettingShapeKey()
        {
            string activeLanguageFolder = LanguageDatabase.activeLanguage != null
                ? LanguageDatabase.activeLanguage.folderName
                : null;
            return StringHash(activeLanguageFolder);
        }

        private int BuildThingFilterShapeKey(ThingFilter filter)
        {
            if (filter == null)
            {
                return 0;
            }

            int key = 17;
            key = AddHash(key, BoolHash(filter.OnlySpecialFilters));
            key = AddHash(key, filter.AllowedDefCount);
            key = AddHash(key, filter.AllowedHitPointsPercents.GetHashCode());
            key = AddHash(key, filter.AllowedMentalBreakChance.GetHashCode());
            key = AddHash(key, filter.AllowedQualityLevels.GetHashCode());
            key = AddHash(key, BoolHash(filter.CaresAboutHitPoints));
            return key;
        }

        private static int AddHash(int key, int value)
        {
            unchecked
            {
                return (key * 31) + value;
            }
        }

        private static int BoolHash(bool value)
        {
            return value ? 1 : 0;
        }

        private static int FloatHash(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0;
            }

            return Mathf.RoundToInt(value * 100f);
        }

        private static int StringHash(string value)
        {
            return !string.IsNullOrEmpty(value)
                ? StringComparer.Ordinal.GetHashCode(value)
                : 0;
        }

    }
}

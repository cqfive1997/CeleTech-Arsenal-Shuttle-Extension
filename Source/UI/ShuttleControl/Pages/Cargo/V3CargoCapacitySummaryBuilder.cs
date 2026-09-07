using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoCapacitySummaryBuilder
    {
        internal void ApplyRefrigeratedCapacitySummary(
            V3CargoPageReadModel pageModel,
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot)
        {
            if (pageModel == null ||
                cargoSnapshot == null ||
                cargoSnapshot.RefrigeratedCargoModules == null ||
                cargoSnapshot.RefrigeratedCargoModules.Count <= 0)
            {
                return;
            }

            pageModel.HasRefrigeratedCapacitySummary = true;
            pageModel.RefrigeratedCapacityTooltip = ShuttleUIText.Tr(
                "CT_Shuttle_Cargo_RefrigeratedCapacityTooltip");
            if (cargoSnapshot.RefrigeratedMassSharesOverallCapacity)
            {
                pageModel.RefrigeratedCapacitySummary = ShuttleUIText.Tr(
                    "CT_Shuttle_Cargo_RefrigeratedCapacityShared");
                return;
            }

            float baseCapacityKg = controlModel != null
                ? Mathf.Max(0f, controlModel.CargoMassCapacityKg)
                : 0f;
            float refrigeratedCapacityKg =
                Mathf.Max(0f, cargoSnapshot.RefrigeratedMassCapacityKg);
            int capacityPercent = baseCapacityKg > 0f
                ? Mathf.RoundToInt((refrigeratedCapacityKg / baseCapacityKg) * 100f)
                : 0;
            pageModel.RefrigeratedCapacitySummary = ShuttleUIText.Tr(
                "CT_Shuttle_Cargo_RefrigeratedCapacityIndependent",
                ShuttleUIMetricFormatter.FormatKgCompact(
                    Mathf.Max(0f, cargoSnapshot.RefrigeratedMassKg)),
                ShuttleUIMetricFormatter.FormatKgCompact(refrigeratedCapacityKg),
                capacityPercent);
        }

        internal int GetNormalCargoRegionCount(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot)
        {
            if (cargoSnapshot != null && cargoSnapshot.CargoRegionCount > 0)
            {
                return cargoSnapshot.CargoRegionCount;
            }

            if (controlModel != null && controlModel.CargoRegionCount > 0u)
            {
                return (int)controlModel.CargoRegionCount;
            }

            return 0;
        }

        internal float GetNormalBayCapacity(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot,
            int regionCount)
        {
            float totalCapacity = controlModel != null
                ? Mathf.Max(0f, controlModel.CargoMassCapacityKg)
                : 0f;
            if (totalCapacity <= 0f && cargoSnapshot != null)
            {
                totalCapacity = Mathf.Max(0f, cargoSnapshot.MassCapacity);
            }

            return totalCapacity;
        }

        internal bool HasOrdinaryLoadedCargo(ShuttleCargoSnapshot cargoSnapshot)
        {
            if (cargoSnapshot == null || cargoSnapshot.Items == null)
            {
                return false;
            }

            for (int i = 0; i < cargoSnapshot.Items.Count; i++)
            {
                ShuttleCargoItemSnapshot item = cargoSnapshot.Items[i];
                if (item != null && item.IsLoaded && !item.IsPawn)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

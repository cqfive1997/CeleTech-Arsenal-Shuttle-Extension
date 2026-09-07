using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKCargoReadSnapshotBuilder
    {
        private const int MaxRows = 512;

        private readonly ExternalSDKCargoRowBuilder rowBuilder =
            new ExternalSDKCargoRowBuilder();
        private readonly ExternalSDKCargoSummaryBuilder summaryBuilder =
            new ExternalSDKCargoSummaryBuilder();

        internal ShuttleExternalCargoReadSnapshot Build(ShuttleController controller)
        {
            if (controller == null)
            {
                return Unavailable("shuttle controller is unavailable");
            }

            ShuttleCargoSnapshot cargoSnapshot = controller.BuildLaunchCargoSnapshot();
            if (cargoSnapshot == null)
            {
                return Unavailable("cargo snapshot is unavailable");
            }

            int sourceRowCount = this.CountTotalSourceRows(cargoSnapshot);
            List<ShuttleExternalCargoItemRowSnapshot> rows = this.BuildRows(cargoSnapshot);
            bool rowsTruncated = sourceRowCount > rows.Count;
            ExternalSDKCargoSummaryBuildResult summary =
                this.summaryBuilder.Build(rows);
            bool summaryComplete =
                !rowsTruncated &&
                summary != null &&
                !summary.SummaryRowsTruncated;
            float capacity = cargoSnapshot.MassCapacity;
            float used = cargoSnapshot.CargoMassUsage > 0f
                ? cargoSnapshot.CargoMassUsage
                : cargoSnapshot.TotalPlannedMassKg;
            float free = capacity - used;
            if (free < 0f)
            {
                free = 0f;
            }

            return new ShuttleExternalCargoReadSnapshot(
                true,
                null,
                controller.ProfileRevision,
                ShuttleTickUtility.TicksGameOrMinusOne(),
                cargoSnapshot.TotalPlannedMassKg,
                capacity,
                used,
                free,
                cargoSnapshot.LoadedStackCount,
                cargoSnapshot.LoadedThingCount,
                cargoSnapshot.AssignedStackCount,
                cargoSnapshot.AssignedThingCount,
                cargoSnapshot.BlockedStackCount,
                cargoSnapshot.RefrigeratedStackCount,
                cargoSnapshot.RefrigeratedThingCount,
                cargoSnapshot.RefrigeratedMassKg,
                sourceRowCount,
                rows.Count,
                MaxRows,
                rowsTruncated,
                summary != null ? summary.SourceSummaryCount : 0,
                summary != null ? summary.ReturnedSummaryCount : 0,
                summary != null ? summary.MaxSummaryRows : ExternalSDKCargoSummaryBuilder.MaxSummaryRows,
                summary != null && summary.SummaryRowsTruncated,
                summaryComplete,
                rows,
                summary != null ? summary.Rows : null);
        }

        internal static ShuttleExternalCargoReadSnapshot Unavailable(string reason)
        {
            return new ShuttleExternalCargoReadSnapshot(
                false,
                reason,
                0,
                ShuttleTickUtility.TicksGameOrMinusOne(),
                0f,
                0f,
                0f,
                0f,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0f,
                0,
                0,
                MaxRows,
                false,
                0,
                0,
                ExternalSDKCargoSummaryBuilder.MaxSummaryRows,
                false,
                false,
                null,
                null);
        }

        private List<ShuttleExternalCargoItemRowSnapshot> BuildRows(
            ShuttleCargoSnapshot cargoSnapshot)
        {
            List<ShuttleExternalCargoItemRowSnapshot> rows =
                new List<ShuttleExternalCargoItemRowSnapshot>();
            this.AddRegularRows(cargoSnapshot, rows);
            this.AddRefrigeratedRows(cargoSnapshot, rows);
            return rows;
        }

        private int CountTotalSourceRows(ShuttleCargoSnapshot cargoSnapshot)
        {
            return this.CountRegularSourceRows(cargoSnapshot) +
                this.CountRefrigeratedSourceRows(cargoSnapshot);
        }

        private int CountRegularSourceRows(ShuttleCargoSnapshot cargoSnapshot)
        {
            if (cargoSnapshot == null || cargoSnapshot.Items == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < cargoSnapshot.Items.Count; i++)
            {
                if (cargoSnapshot.Items[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountRefrigeratedSourceRows(ShuttleCargoSnapshot cargoSnapshot)
        {
            if (cargoSnapshot == null || cargoSnapshot.RefrigeratedCargoModules == null)
            {
                return 0;
            }

            int count = 0;
            for (int moduleIndex = 0;
                moduleIndex < cargoSnapshot.RefrigeratedCargoModules.Count;
                moduleIndex++)
            {
                ShuttleRefrigeratedCargoModuleSnapshot module =
                    cargoSnapshot.RefrigeratedCargoModules[moduleIndex];
                if (module == null || module.Items == null)
                {
                    continue;
                }

                for (int itemIndex = 0; itemIndex < module.Items.Count; itemIndex++)
                {
                    if (module.Items[itemIndex] != null)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private void AddRegularRows(
            ShuttleCargoSnapshot cargoSnapshot,
            List<ShuttleExternalCargoItemRowSnapshot> rows)
        {
            if (cargoSnapshot == null || cargoSnapshot.Items == null || rows == null)
            {
                return;
            }

            for (int i = 0; i < cargoSnapshot.Items.Count && rows.Count < MaxRows; i++)
            {
                ShuttleExternalCargoItemRowSnapshot row =
                    this.rowBuilder.BuildRegularRow(cargoSnapshot.Items[i]);
                if (row != null)
                {
                    rows.Add(row);
                }
            }
        }

        private void AddRefrigeratedRows(
            ShuttleCargoSnapshot cargoSnapshot,
            List<ShuttleExternalCargoItemRowSnapshot> rows)
        {
            if (cargoSnapshot == null ||
                cargoSnapshot.RefrigeratedCargoModules == null ||
                rows == null)
            {
                return;
            }

            for (int moduleIndex = 0;
                moduleIndex < cargoSnapshot.RefrigeratedCargoModules.Count && rows.Count < MaxRows;
                moduleIndex++)
            {
                ShuttleRefrigeratedCargoModuleSnapshot module =
                    cargoSnapshot.RefrigeratedCargoModules[moduleIndex];
                if (module == null || module.Items == null)
                {
                    continue;
                }

                for (int itemIndex = 0; itemIndex < module.Items.Count && rows.Count < MaxRows; itemIndex++)
                {
                    ShuttleExternalCargoItemRowSnapshot row =
                        this.rowBuilder.BuildRefrigeratedRow(module, module.Items[itemIndex]);
                    if (row != null)
                    {
                        rows.Add(row);
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.CargoUnloading;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoUnloadRowModel
    {
        internal string Key;
        internal string Label;
        internal string DefName;
        internal string SourceKey;
        internal string SourceLabel;
        internal string SearchText;
        internal string EditBuffer;
        internal string StandardFilterId;
        internal string CustomFilterId;
        internal ShuttleCargoUnloadSourceKind SourceKind;
        internal int CargoRegionIndex = -1;
        internal int TransporterIndex = -1;
        internal int LoadedIndex = -1;
        internal string ModuleInstanceID;
        internal int ColdIndex = -1;
        internal int ThingIDNumber;
        internal int AvailableCount;
        internal float TotalMassKg;
        internal float UnitMassKg;
        internal Thing DisplayThing;
    }

    internal sealed class V3CargoUnloadModel
    {
        internal readonly List<V3CargoUnloadRowModel> Rows =
            new List<V3CargoUnloadRowModel>();
        internal int TotalThingCount;
        internal float TotalMassKg;
        internal float CapacityKg;
    }

    internal sealed class V3CargoUnloadModelBuilder
    {
        internal V3CargoUnloadModel Build(
            IShuttleCargoReadPort readPort,
            V3CargoLoadCategoryResolver categoryResolver)
        {
            V3CargoUnloadModel model = new V3CargoUnloadModel();
            ShuttleCargoSnapshot snapshot = readPort != null
                ? readPort.BuildCargoSnapshot()
                : null;
            if (snapshot == null)
            {
                return model;
            }

            model.CapacityKg = snapshot.MassCapacity;

            this.AddRegularRows(model, snapshot, readPort, categoryResolver);
            this.AddRefrigeratedRows(model, snapshot, categoryResolver);
            model.Rows.Sort(this.CompareRows);
            return model;
        }

        internal bool TryReadCurrentCapacity(
            IShuttleCargoReadPort readPort,
            out float currentMassKg,
            out float capacityKg)
        {
            currentMassKg = 0f;
            capacityKg = 0f;
            ShuttleCargoSnapshot snapshot = readPort != null
                ? readPort.BuildCargoSnapshot()
                : null;
            if (snapshot == null)
            {
                return false;
            }

            capacityKg = snapshot.MassCapacity;
            for (int i = 0; snapshot.Items != null && i < snapshot.Items.Count; i++)
            {
                ShuttleCargoItemSnapshot item = snapshot.Items[i];
                if (item != null && item.IsLoaded && !item.IsPawn &&
                    !(item.DisplayThing is Pawn))
                {
                    currentMassKg += item.Mass;
                }
            }

            currentMassKg += snapshot.RefrigeratedMassKg;
            return true;
        }

        private void AddRegularRows(
            V3CargoUnloadModel model,
            ShuttleCargoSnapshot snapshot,
            IShuttleCargoReadPort readPort,
            V3CargoLoadCategoryResolver categoryResolver)
        {
            Dictionary<int, string> sourceLabelsByRegion =
                new Dictionary<int, string>();
            for (int i = 0; snapshot.Items != null && i < snapshot.Items.Count; i++)
            {
                ShuttleCargoItemSnapshot item = snapshot.Items[i];
                if (item == null || !item.IsLoaded || item.IsPawn ||
                    item.DisplayThing is Pawn || item.StackCount <= 0)
                {
                    continue;
                }

                string sourceLabel = this.ResolveRegularSourceLabel(
                    readPort,
                    item.CargoRegionIndex,
                    snapshot.CargoRegionCount,
                    sourceLabelsByRegion);
                V3CargoUnloadRowModel row = this.CreateRow(
                    item.DisplayThing,
                    item.Label,
                    item.DefName,
                    item.StackCount,
                    item.Mass,
                    this.BuildRegularSourceKey(
                        item.CargoRegionIndex,
                        item.TransporterIndex),
                    sourceLabel,
                    ShuttleCargoUnloadSourceKind.LoadedCargo,
                    categoryResolver);
                row.CargoRegionIndex = item.CargoRegionIndex;
                row.TransporterIndex = item.TransporterIndex;
                row.LoadedIndex = item.LoadedIndex;
                row.ThingIDNumber = item.ThingIDNumber;
                row.Key = "regular:" + item.TransporterIndex + ":" +
                    item.ThingIDNumber;
                this.AddRow(model, row);
            }
        }

        private void AddRefrigeratedRows(
            V3CargoUnloadModel model,
            ShuttleCargoSnapshot snapshot,
            V3CargoLoadCategoryResolver categoryResolver)
        {
            for (int moduleIndex = 0;
                snapshot.RefrigeratedCargoModules != null &&
                    moduleIndex < snapshot.RefrigeratedCargoModules.Count;
                moduleIndex++)
            {
                ShuttleRefrigeratedCargoModuleSnapshot module =
                    snapshot.RefrigeratedCargoModules[moduleIndex];
                if (module == null)
                {
                    continue;
                }

                string sourceLabel = ShuttleAssemblyDisplayTextResolver
                    .ResolveRefrigeratedCargoDisplayName(
                        module.ModuleDefName,
                        module.Label,
                        module.HasCustomLabel,
                        V3CargoLoadText.Tr("CT_Shuttle_Module_RefrigeratedCargo_Name") +
                            " " + (moduleIndex + 1));
                for (int i = 0; module.Items != null && i < module.Items.Count; i++)
                {
                    ShuttleRefrigeratedCargoItemSnapshot item = module.Items[i];
                    if (item == null || item.DisplayThing is Pawn || item.StackCount <= 0)
                    {
                        continue;
                    }

                    V3CargoUnloadRowModel row = this.CreateRow(
                        item.DisplayThing,
                        item.Label,
                        item.DefName,
                        item.StackCount,
                        item.Mass,
                        this.BuildRefrigeratedSourceKey(
                            module.ModuleInstanceID,
                            moduleIndex),
                        sourceLabel,
                        ShuttleCargoUnloadSourceKind.RefrigeratedCargo,
                        categoryResolver);
                    row.ModuleInstanceID = module.ModuleInstanceID;
                    row.ColdIndex = item.ColdIndex;
                    row.ThingIDNumber = item.ThingIDNumber;
                    row.Key = "cold:" + (module.ModuleInstanceID ?? string.Empty) +
                        ":" + item.ThingIDNumber;
                    this.AddRow(model, row);
                }
            }
        }

        private V3CargoUnloadRowModel CreateRow(
            Thing thing,
            string label,
            string defName,
            int count,
            float totalMass,
            string sourceKey,
            string sourceLabel,
            ShuttleCargoUnloadSourceKind sourceKind,
            V3CargoLoadCategoryResolver categoryResolver)
        {
            V3CargoLoadCategoryMatch match = categoryResolver != null
                ? categoryResolver.ResolveCargoThing(thing)
                : null;
            V3CargoUnloadRowModel row = new V3CargoUnloadRowModel();
            row.Label = !string.IsNullOrEmpty(label) ? label : defName;
            row.DefName = defName;
            row.SourceKey = sourceKey;
            row.SourceLabel = sourceLabel;
            row.AvailableCount = count;
            row.TotalMassKg = totalMass;
            row.UnitMassKg = count > 0 ? totalMass / count : 0f;
            row.SourceKind = sourceKind;
            row.DisplayThing = thing;
            row.StandardFilterId = match != null
                ? match.StandardFilterId
                : V3CargoLoadFilterIds.CargoOther;
            row.CustomFilterId = match != null
                ? V3CargoLoadFilterIds.ForCustomCategory(match.CustomCategory)
                : null;
            row.SearchText = ((row.Label ?? string.Empty) + " " +
                (row.DefName ?? string.Empty) + " " +
                (sourceLabel ?? string.Empty)).ToLowerInvariant();
            return row;
        }

        private string BuildRegularSourceKey(int regionIndex, int transporterIndex)
        {
            return regionIndex >= 0
                ? "regular-region:" + regionIndex
                : "regular-transporter:" + transporterIndex;
        }

        private string BuildRefrigeratedSourceKey(
            string moduleInstanceID,
            int moduleIndex)
        {
            return !string.IsNullOrEmpty(moduleInstanceID)
                ? "cold-module:" + moduleInstanceID
                : "cold-module-index:" + moduleIndex;
        }

        private string ResolveRegularSourceLabel(
            IShuttleCargoReadPort readPort,
            int regionIndex,
            int regionCount,
            Dictionary<int, string> sourceLabelsByRegion)
        {
            if (regionIndex < 0)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_UI_BlockedUnassigned");
            }

            string sourceLabel;
            if (sourceLabelsByRegion != null &&
                sourceLabelsByRegion.TryGetValue(regionIndex, out sourceLabel))
            {
                return sourceLabel;
            }

            ShuttleCargoRegionReadModel region = readPort != null
                ? readPort.GetCargoRegionSettings(regionIndex, regionCount)
                : null;
            sourceLabel =
                ShuttleAssemblyDisplayTextResolver.ResolveCargoRegionDisplayName(
                    regionIndex,
                    region != null ? region.Label : null,
                    region != null && region.HasCustomLabel);
            if (sourceLabelsByRegion != null)
            {
                sourceLabelsByRegion[regionIndex] = sourceLabel;
            }

            return sourceLabel;
        }

        private void AddRow(V3CargoUnloadModel model, V3CargoUnloadRowModel row)
        {
            if (model == null || row == null || string.IsNullOrEmpty(row.Key))
            {
                return;
            }

            model.Rows.Add(row);
            model.TotalThingCount += row.AvailableCount;
            model.TotalMassKg += row.TotalMassKg;
        }

        private int CompareRows(V3CargoUnloadRowModel left, V3CargoUnloadRowModel right)
        {
            int source = string.Compare(
                left != null ? left.SourceLabel : string.Empty,
                right != null ? right.SourceLabel : string.Empty,
                StringComparison.CurrentCultureIgnoreCase);
            return source != 0
                ? source
                : string.Compare(
                    left != null ? left.Label : string.Empty,
                    right != null ? right.Label : string.Empty,
                    StringComparison.CurrentCultureIgnoreCase);
        }
    }
}

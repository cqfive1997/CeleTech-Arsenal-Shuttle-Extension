using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.CargoUnloading
{
    internal sealed class ShuttleCargoUnloadPlanValidator
    {
        internal bool TryBuildRecords(
            IList<ShuttleCargoUnloadIntent> intents,
            ShuttleCargoSnapshot cargoSnapshot,
            out List<ShuttleCargoUnloadRecord> records,
            out string failureReason)
        {
            records = new List<ShuttleCargoUnloadRecord>();
            failureReason = null;
            if (cargoSnapshot == null)
            {
                failureReason = "CT_Shuttle_Cargo_UnloadSnapshotUnavailable".Translate().ToString();
                return false;
            }

            if (intents == null || intents.Count == 0)
            {
                failureReason = "CT_Shuttle_Cargo_UnloadNothingSelected".Translate().ToString();
                return false;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < intents.Count; i++)
            {
                ShuttleCargoUnloadIntent intent = intents[i];
                if (!this.IsValidIntent(intent))
                {
                    failureReason = "CT_Shuttle_Cargo_UnloadEntryUnavailable".Translate().ToString();
                    return false;
                }

                string identityKey = ((int)intent.SourceKind).ToString() + ":" +
                    intent.ThingIDNumber.ToString();
                if (!seen.Add(identityKey))
                {
                    failureReason = "CT_Shuttle_Cargo_UnloadDuplicateEntry".Translate().ToString();
                    return false;
                }

                if (intent.SourceKind == ShuttleCargoUnloadSourceKind.LoadedCargo)
                {
                    ShuttleCargoItemSnapshot item =
                        this.FindLoadedItem(cargoSnapshot, intent);
                    if (item == null || intent.Count > item.StackCount)
                    {
                        failureReason = "CT_Shuttle_Cargo_UnloadEntryUnavailable".Translate().ToString();
                        return false;
                    }
                }
                else if (intent.SourceKind == ShuttleCargoUnloadSourceKind.RefrigeratedCargo)
                {
                    ShuttleRefrigeratedCargoItemSnapshot item =
                        this.FindColdItem(cargoSnapshot, intent);
                    if (item == null || intent.Count > item.StackCount)
                    {
                        failureReason = "CT_Shuttle_Cargo_UnloadEntryUnavailable".Translate().ToString();
                        return false;
                    }
                }
                else
                {
                    failureReason = "CT_Shuttle_Cargo_UnloadEntryUnavailable".Translate().ToString();
                    return false;
                }

                records.Add(new ShuttleCargoUnloadRecord(
                    intent.SourceKind,
                    intent.TransporterIndex,
                    intent.LoadedIndex,
                    intent.ModuleInstanceID,
                    intent.ColdIndex,
                    intent.ThingIDNumber,
                    intent.ExpectedDefName,
                    intent.Count));
            }

            if (records.Count == 0)
            {
                failureReason = "CT_Shuttle_Cargo_UnloadNothingSelected".Translate().ToString();
                return false;
            }

            return true;
        }

        private bool IsValidIntent(ShuttleCargoUnloadIntent intent)
        {
            if (intent == null ||
                intent.ThingIDNumber <= 0 ||
                string.IsNullOrEmpty(intent.ExpectedDefName) ||
                intent.Count <= 0)
            {
                return false;
            }

            if (intent.SourceKind == ShuttleCargoUnloadSourceKind.LoadedCargo)
            {
                return intent.TransporterIndex >= 0 && intent.LoadedIndex >= 0;
            }

            return intent.SourceKind == ShuttleCargoUnloadSourceKind.RefrigeratedCargo &&
                !string.IsNullOrEmpty(intent.ModuleInstanceID) &&
                intent.ColdIndex >= 0;
        }

        private ShuttleCargoItemSnapshot FindLoadedItem(
            ShuttleCargoSnapshot cargoSnapshot,
            ShuttleCargoUnloadIntent intent)
        {
            for (int i = 0;
                cargoSnapshot.Items != null && i < cargoSnapshot.Items.Count;
                i++)
            {
                ShuttleCargoItemSnapshot item = cargoSnapshot.Items[i];
                if (item != null &&
                    item.IsLoaded &&
                    !item.IsPawn &&
                    item.TransporterIndex == intent.TransporterIndex &&
                    item.LoadedIndex == intent.LoadedIndex &&
                    item.ThingIDNumber == intent.ThingIDNumber &&
                    string.Equals(
                        item.DefName,
                        intent.ExpectedDefName,
                        StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        private ShuttleRefrigeratedCargoItemSnapshot FindColdItem(
            ShuttleCargoSnapshot cargoSnapshot,
            ShuttleCargoUnloadIntent intent)
        {
            for (int moduleIndex = 0;
                cargoSnapshot.RefrigeratedCargoModules != null &&
                    moduleIndex < cargoSnapshot.RefrigeratedCargoModules.Count;
                moduleIndex++)
            {
                ShuttleRefrigeratedCargoModuleSnapshot module =
                    cargoSnapshot.RefrigeratedCargoModules[moduleIndex];
                if (module == null ||
                    !string.Equals(
                        module.ModuleInstanceID,
                        intent.ModuleInstanceID,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                for (int itemIndex = 0;
                    module.Items != null && itemIndex < module.Items.Count;
                    itemIndex++)
                {
                    ShuttleRefrigeratedCargoItemSnapshot item = module.Items[itemIndex];
                    if (item != null &&
                        item.ColdIndex == intent.ColdIndex &&
                        item.ThingIDNumber == intent.ThingIDNumber &&
                        string.Equals(
                            item.DefName,
                            intent.ExpectedDefName,
                            StringComparison.Ordinal))
                    {
                        return item;
                    }
                }
            }

            return null;
        }
    }
}

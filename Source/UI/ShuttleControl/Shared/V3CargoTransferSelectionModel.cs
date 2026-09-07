using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoTransferSelectionModel
    {
        private const float MassEpsilon = 0.001f;

        private readonly ShuttleCargoPageActionContext pageContext;
        private readonly List<ShuttleCargoBayActionTarget> destinationBays =
            new List<ShuttleCargoBayActionTarget>();
        private int selectedDestinationIndex = -1;

        internal V3CargoTransferSelectionModel(
            ShuttleCargoStackActionTarget stack,
            ShuttleCargoPageActionContext pageContext)
        {
            this.Stack = stack;
            this.pageContext = pageContext;
            this.Count = this.MaxCount();
            this.BuildDestinationBays();
            this.selectedDestinationIndex = this.FindInitialDestinationIndex();
        }

        internal ShuttleCargoStackActionTarget Stack { get; private set; }

        internal int Count { get; private set; }

        internal bool IsToRefrigerated()
        {
            return this.Stack != null &&
                this.Stack.SourceKind == ShuttleCargoStackActionSourceKind.LoadedCargo;
        }

        internal bool IsToLoadedCargo()
        {
            return this.Stack != null &&
                this.Stack.SourceKind == ShuttleCargoStackActionSourceKind.RefrigeratedCargo;
        }

        internal int DestinationBayCount()
        {
            return this.destinationBays.Count;
        }

        internal ShuttleCargoBayActionTarget GetDestinationBay(int index)
        {
            return index >= 0 && index < this.destinationBays.Count
                ? this.destinationBays[index]
                : null;
        }

        internal ShuttleCargoBayActionTarget GetSelectedDestinationBay()
        {
            return this.GetDestinationBay(this.selectedDestinationIndex);
        }

        internal bool IsSelectedDestination(int index)
        {
            return index == this.selectedDestinationIndex;
        }

        internal void SelectDestination(int index)
        {
            if (index >= 0 && index < this.destinationBays.Count)
            {
                this.selectedDestinationIndex = index;
            }
        }

        internal int MaxCount()
        {
            return this.Stack != null ? Mathf.Max(0, this.Stack.StackCount) : 0;
        }

        internal void SetCount(int count)
        {
            int max = this.MaxCount();
            this.Count = max > 0 ? Mathf.Clamp(count, 1, max) : 0;
        }

        internal float SelectedMassKg()
        {
            return this.GetMoveMassKg(this.Count);
        }

        internal float DestinationCapacityKg()
        {
            if (this.IsToLoadedCargo())
            {
                return this.SumNormalBayCapacityKg();
            }

            ShuttleCargoBayActionTarget bay = this.GetSelectedDestinationBay();
            return bay != null ? Mathf.Max(0f, bay.CapacityKg) : 0f;
        }

        internal float DestinationUsedMassKg()
        {
            if (this.IsToLoadedCargo())
            {
                return this.SumNormalBayUsedMassKg();
            }

            ShuttleCargoBayActionTarget bay = this.GetSelectedDestinationBay();
            return bay != null ? Mathf.Max(0f, bay.UsedMassKg) : 0f;
        }

        internal float DestinationAvailableMassKg()
        {
            float available = this.DestinationCapacityKg() - this.DestinationUsedMassKg();
            return available > 0f ? available : 0f;
        }

        internal string GetSelectedBlocker()
        {
            if (this.IsToLoadedCargo())
            {
                return this.GetLoadedDestinationBlocker(this.Count);
            }

            return this.GetBayBlocker(this.GetSelectedDestinationBay(), this.Count);
        }

        internal string GetBayBlocker(ShuttleCargoBayActionTarget bay, int count)
        {
            if (bay == null)
            {
                return V3CargoTransferText.Tr("CT_Shuttle_Command_RefrigeratedCargoModuleMissing");
            }

            if (!bay.IsRefrigerated || string.IsNullOrEmpty(bay.ModuleInstanceID))
            {
                return V3CargoTransferText.Tr("CT_Shuttle_Command_RefrigeratedCargoModuleMissing");
            }

            if (!bay.IsEnabled)
            {
                return !string.IsNullOrEmpty(bay.StatusText)
                    ? bay.StatusText
                    : V3CargoTransferText.Tr("CT_Shuttle_Command_RefrigeratedCargoModuleDisabled");
            }

            if (!bay.CoolingActive)
            {
                return !string.IsNullOrEmpty(bay.InactiveReason)
                    ? bay.InactiveReason
                    : !string.IsNullOrEmpty(bay.StatusText)
                        ? bay.StatusText
                        : V3CargoTransferText.Tr("CT_Shuttle_Logistics_StatusUnavailable");
            }

            if (count <= 0 || count > this.MaxCount())
            {
                return V3CargoTransferText.Tr("CT_Shuttle_Cargo_LoadedEntryNoStack");
            }

            float capacityKg = Mathf.Max(0f, bay.CapacityKg);
            float availableKg = capacityKg - Mathf.Max(0f, bay.UsedMassKg);
            if (availableKg < 0f)
            {
                availableKg = 0f;
            }

            if (this.GetMoveMassKg(count) > availableKg + MassEpsilon)
            {
                return V3CargoTransferText.Tr("CT_Shuttle_Cargo_SelectedExceedsCapacity");
            }

            return null;
        }

        internal ShuttleCargoTransferActionTarget CreateActionTarget()
        {
            return ShuttleCargoTransferTargetBuilder.Create(
                this.Stack,
                this.pageContext,
                this.GetSelectedDestinationBay(),
                this.Count);
        }

        private void BuildDestinationBays()
        {
            if (!this.IsToRefrigerated() ||
                this.pageContext == null ||
                this.pageContext.Bays == null)
            {
                return;
            }

            for (int i = 0; i < this.pageContext.Bays.Count; i++)
            {
                ShuttleCargoBayActionTarget bay = this.pageContext.Bays[i];
                if (bay != null && bay.IsRefrigerated)
                {
                    this.destinationBays.Add(bay);
                }
            }
        }

        private int FindInitialDestinationIndex()
        {
            for (int i = 0; i < this.destinationBays.Count; i++)
            {
                if (string.IsNullOrEmpty(this.GetBayBlocker(
                        this.destinationBays[i],
                        this.Count)))
                {
                    return i;
                }
            }

            return this.destinationBays.Count > 0 ? 0 : -1;
        }

        private string GetLoadedDestinationBlocker(int count)
        {
            if (count <= 0 || count > this.MaxCount())
            {
                return V3CargoTransferText.Tr("CT_Shuttle_Cargo_LoadedEntryNoStack");
            }

            float selectedMassKg = this.GetMoveMassKg(count);
            float capacityKg = this.SumNormalBayCapacityKg();
            if (capacityKg <= MassEpsilon && selectedMassKg > MassEpsilon)
            {
                return V3CargoTransferText.Tr("CT_Shuttle_Cargo_CapacityUnavailable");
            }

            if (selectedMassKg > this.DestinationAvailableMassKg() + MassEpsilon)
            {
                return V3CargoTransferText.Tr("CT_Shuttle_Cargo_SelectedExceedsCapacity");
            }

            return null;
        }

        private float GetMoveMassKg(int count)
        {
            if (this.Stack == null ||
                this.Stack.StackCount <= 0 ||
                count <= 0 ||
                this.Stack.MassKg <= 0f)
            {
                return 0f;
            }

            return Mathf.Max(0f, this.Stack.MassKg) *
                Mathf.Min(count, this.Stack.StackCount) /
                this.Stack.StackCount;
        }

        private float SumNormalBayCapacityKg()
        {
            float total = 0f;
            for (int i = 0; this.pageContext != null &&
                this.pageContext.Bays != null &&
                i < this.pageContext.Bays.Count; i++)
            {
                ShuttleCargoBayActionTarget bay = this.pageContext.Bays[i];
                if (bay != null && !bay.IsRefrigerated)
                {
                    total += Mathf.Max(0f, bay.CapacityKg);
                }
            }

            return total;
        }

        private float SumNormalBayUsedMassKg()
        {
            float total = 0f;
            for (int i = 0; this.pageContext != null &&
                this.pageContext.Bays != null &&
                i < this.pageContext.Bays.Count; i++)
            {
                ShuttleCargoBayActionTarget bay = this.pageContext.Bays[i];
                if (bay != null && !bay.IsRefrigerated)
                {
                    total += Mathf.Max(0f, bay.UsedMassKg);
                }
            }

            return total;
        }
    }
}

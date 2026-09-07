using System.Collections.Generic;
using RimWorld;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal enum V3CargoLoadTab
    {
        Passengers,
        Cargo
    }

    internal sealed class V3CargoLoadAvailablePanelState
    {
        internal V3CargoLoadTab ActiveTab = V3CargoLoadTab.Passengers;
        internal Vector2 Scroll;
        internal string SearchText = string.Empty;
        internal string PassengerFilterId = V3CargoLoadFilterIds.All;
        internal string CargoFilterId = V3CargoLoadFilterIds.All;
        internal readonly List<int> FilteredRowIndices = new List<int>();
        internal List<TransferableOneWay> LastFilteredSource;
        internal V3CargoLoadTab LastFilteredTab = V3CargoLoadTab.Passengers;
        internal string LastSearchNeedle;
        internal string LastCategoryFilterId;
        internal int LastFilteredCount = -1;
        internal bool FilterDirty = true;
        internal bool FilterMatchesAllRows;

        internal string ActiveFilterId
        {
            get
            {
                return this.ActiveTab == V3CargoLoadTab.Passengers
                    ? this.PassengerFilterId
                    : this.CargoFilterId;
            }
        }

        internal void SetActiveFilterId(string filterId)
        {
            string resolvedFilterId = string.IsNullOrEmpty(filterId)
                ? V3CargoLoadFilterIds.All
                : filterId;
            if (this.ActiveTab == V3CargoLoadTab.Passengers)
            {
                this.PassengerFilterId = resolvedFilterId;
            }
            else
            {
                this.CargoFilterId = resolvedFilterId;
            }
        }

        internal void MarkFilterDirty()
        {
            this.FilterDirty = true;
        }
    }

    internal sealed class V3CargoLoadSelectedPanelState
    {
        internal Vector2 Scroll;
    }
}

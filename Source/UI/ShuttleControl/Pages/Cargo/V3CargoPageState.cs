using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    /// <summary>
    /// Native V3 owner for Cargo scroll, focus, and category state. Dialog-local
    /// state remains inside the existing V2 Cargo dialogs.
    /// </summary>
    internal sealed class V3CargoPageState
    {
        internal Vector2 CargoStackListScroll;
        internal Vector2 CargoStatsScroll;
        internal Vector2 CargoLogisticsScroll;
        internal Vector2 CargoMessageScroll;
        internal readonly V3SharedMessagePanelState MessagePanelState =
            new V3SharedMessagePanelState();
        internal V3CargoCategory SelectedCargoCategory = V3CargoCategory.None;
        internal string SelectedBayKey;
        internal string SelectedStackKey;
        internal int LastCargoSnapshotRevision = int.MinValue;

        internal void TrackCargoSnapshotRevision(int revision)
        {
            if (this.LastCargoSnapshotRevision == revision)
            {
                return;
            }

            this.LastCargoSnapshotRevision = revision;
        }
    }
}

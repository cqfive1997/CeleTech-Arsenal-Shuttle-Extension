using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3QueuedLoadDialogModel
    {
        internal bool SnapshotAvailable;
        internal int RowCount;
        internal int QueuedThingCount;
        internal float QueuedMassKg;
        internal List<V3QueuedLoadRowModel> Rows = new List<V3QueuedLoadRowModel>();

        internal bool HasRows
        {
            get { return this.Rows != null && this.Rows.Count > 0; }
        }
    }

    internal sealed class V3QueuedLoadRowModel
    {
        internal string Label;
        internal string DefName;
        internal string Category;
        internal int CargoRegionIndex = -1;
        internal int TransporterIndex;
        internal int QueueIndex;
        internal int StackCount;
        internal float MassKg;
        internal bool IsPawn;
        internal Thing DisplayThing;

        internal bool CanCancel
        {
            get
            {
                return this.TransporterIndex >= 0 &&
                    this.QueueIndex >= 0 &&
                    this.StackCount > 0;
            }
        }
    }
}

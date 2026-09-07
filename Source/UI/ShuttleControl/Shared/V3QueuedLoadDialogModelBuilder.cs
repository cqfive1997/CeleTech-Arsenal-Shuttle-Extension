using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3QueuedLoadDialogModelBuilder
    {
        internal V3QueuedLoadDialogModel Build(IShuttleCargoReadPort cargoReadPort)
        {
            V3QueuedLoadDialogModel model = new V3QueuedLoadDialogModel();
            if (cargoReadPort == null)
            {
                return model;
            }

            try
            {
                ShuttleCargoSnapshot snapshot = cargoReadPort.BuildCargoSnapshot();
                model.SnapshotAvailable = snapshot != null;
                if (snapshot == null || snapshot.Items == null)
                {
                    return model;
                }

                model.QueuedThingCount = Mathf.Max(0, snapshot.AssignedThingCount);
                model.QueuedMassKg = Mathf.Max(0f, snapshot.QueuedMassKg);

                for (int i = 0; i < snapshot.Items.Count; i++)
                {
                    ShuttleCargoItemSnapshot item = snapshot.Items[i];
                    if (item == null || !item.IsAssignedToLoad)
                    {
                        continue;
                    }

                    V3QueuedLoadRowModel row = new V3QueuedLoadRowModel();
                    row.Label = item.Label;
                    row.DefName = item.DefName;
                    row.Category = item.Category;
                    row.CargoRegionIndex = item.CargoRegionIndex;
                    row.TransporterIndex = item.TransporterIndex;
                    row.QueueIndex = item.QueueIndex;
                    row.StackCount = Mathf.Max(0, item.StackCount);
                    row.MassKg = Mathf.Max(0f, item.Mass);
                    row.IsPawn = item.IsPawn;
                    row.DisplayThing = item.DisplayThing;
                    model.Rows.Add(row);
                }

                model.RowCount = model.Rows.Count;
                this.FillFallbackTotals(model);
                return model;
            }
            catch
            {
                model.SnapshotAvailable = false;
                model.Rows.Clear();
                model.RowCount = 0;
                model.QueuedThingCount = 0;
                model.QueuedMassKg = 0f;
                return model;
            }
        }

        private void FillFallbackTotals(V3QueuedLoadDialogModel model)
        {
            if (model == null || model.Rows == null)
            {
                return;
            }

            int count = 0;
            float mass = 0f;
            for (int i = 0; i < model.Rows.Count; i++)
            {
                V3QueuedLoadRowModel row = model.Rows[i];
                if (row == null)
                {
                    continue;
                }

                count += Mathf.Max(0, row.StackCount);
                mass += Mathf.Max(0f, row.MassKg);
            }

            if (model.QueuedThingCount <= 0)
            {
                model.QueuedThingCount = count;
            }

            if (model.QueuedMassKg <= 0f)
            {
                model.QueuedMassKg = mass;
            }
        }
    }
}

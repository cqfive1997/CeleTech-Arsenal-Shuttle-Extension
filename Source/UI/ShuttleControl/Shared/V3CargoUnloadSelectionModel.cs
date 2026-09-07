using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoUnloadSelectionModel
    {
        private readonly Dictionary<string, int> selected =
            new Dictionary<string, int>();

        internal int Get(V3CargoUnloadRowModel row)
        {
            int count;
            return row != null && !string.IsNullOrEmpty(row.Key) &&
                this.selected.TryGetValue(row.Key, out count)
                    ? count
                    : 0;
        }

        internal void Set(V3CargoUnloadRowModel row, int count)
        {
            if (row == null || string.IsNullOrEmpty(row.Key))
            {
                return;
            }

            int clamped = count < 0
                ? 0
                : count > row.AvailableCount ? row.AvailableCount : count;
            row.EditBuffer = clamped.ToString();
            if (clamped <= 0)
            {
                this.selected.Remove(row.Key);
            }
            else
            {
                this.selected[row.Key] = clamped;
            }
        }

        internal void Clear(V3CargoUnloadModel model)
        {
            this.selected.Clear();
            for (int i = 0; model != null && i < model.Rows.Count; i++)
            {
                V3CargoUnloadRowModel row = model.Rows[i];
                if (row != null)
                {
                    row.EditBuffer = "0";
                }
            }
        }

        internal int SelectedStackCount(V3CargoUnloadModel model)
        {
            int count = 0;
            for (int i = 0; model != null && i < model.Rows.Count; i++)
            {
                if (this.Get(model.Rows[i]) > 0)
                {
                    count++;
                }
            }

            return count;
        }

        internal int SelectedThingCount(V3CargoUnloadModel model)
        {
            int count = 0;
            for (int i = 0; model != null && i < model.Rows.Count; i++)
            {
                count += this.Get(model.Rows[i]);
            }

            return count;
        }

        internal float SelectedMassKg(V3CargoUnloadModel model)
        {
            float mass = 0f;
            for (int i = 0; model != null && i < model.Rows.Count; i++)
            {
                V3CargoUnloadRowModel row = model.Rows[i];
                mass += this.Get(row) * row.UnitMassKg;
            }

            return mass;
        }

        internal List<ShuttleCargoUnloadIntent> BuildIntents(V3CargoUnloadModel model)
        {
            List<ShuttleCargoUnloadIntent> result =
                new List<ShuttleCargoUnloadIntent>();
            for (int i = 0; model != null && i < model.Rows.Count; i++)
            {
                V3CargoUnloadRowModel row = model.Rows[i];
                int count = this.Get(row);
                if (row == null || count <= 0)
                {
                    continue;
                }

                result.Add(new ShuttleCargoUnloadIntent(
                    row.SourceKind,
                    row.TransporterIndex,
                    row.LoadedIndex,
                    row.ModuleInstanceID,
                    row.ColdIndex,
                    row.ThingIDNumber,
                    row.DefName,
                    count));
            }

            return result;
        }
    }
}

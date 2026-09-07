using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3QueuedLoadSummaryDrawer
    {
        internal void Draw(Rect rect, V3QueuedLoadDialogModel model)
        {
            V3CargoLoadDialogStyle.DrawSurface(rect);
            V3CargoLoadDialogStyle.DrawSectionHeader(
                rect,
                V3CargoLoadText.Tr("CT_Shuttle_QueuedLoad_QueueSummary"));

            float y = rect.y + 42f;
            this.DrawLine(
                ref y,
                rect,
                V3CargoLoadText.Tr("CT_Shuttle_QueuedLoad_Rows"),
                this.GetRowText(model),
                V3CargoLoadDialogStyle.AccentColor);
            this.DrawLine(
                ref y,
                rect,
                V3CargoLoadText.Tr("CT_Shuttle_QueuedLoad_TotalCount"),
                this.GetThingCountText(model),
                V3CargoLoadDialogStyle.AccentColor);
            this.DrawLine(
                ref y,
                rect,
                V3CargoLoadText.Tr("CT_Shuttle_QueuedLoad_TotalMass"),
                this.GetMassText(model),
                V3CargoLoadDialogStyle.YellowColor);
            this.DrawLine(
                ref y,
                rect,
                V3CargoLoadText.Tr("CT_Shuttle_QueuedLoad_Pawns"),
                this.GetPawnCountText(model),
                ShuttleUIStyle.MutedTextColor);
            this.DrawLine(
                ref y,
                rect,
                V3CargoLoadText.Tr("CT_Shuttle_QueuedLoad_Cargo"),
                this.GetCargoCountText(model),
                ShuttleUIStyle.MutedTextColor);
            this.DrawLine(
                ref y,
                rect,
                V3CargoLoadText.Tr("CT_Shuttle_QueuedLoad_State"),
                this.GetStatusText(model),
                this.GetStatusColor(model));

            Rect hintRect = new Rect(rect.x + 12f, y + 10f, rect.width - 24f, 96f);
            ShuttleUILayout.DrawCardBackground(
                hintRect,
                false,
                false,
                ShuttleUIStyle.DisabledColor);
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(hintRect.x + 8f, hintRect.y + 8f, hintRect.width - 16f, hintRect.height - 16f),
                V3CargoLoadText.Tr("CT_Shuttle_QueuedLoad_SummaryHint"));

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawLine(
            ref float y,
            Rect panelRect,
            string label,
            string value,
            Color valueColor)
        {
            Rect rowRect = new Rect(panelRect.x + 12f, y, panelRect.width - 24f, 22f);
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(rowRect.x, rowRect.y, rowRect.width * 0.48f, rowRect.height),
                V3CargoLoadDialogStyle.FitLabelText(label, rowRect.width * 0.48f));
            GUI.color = valueColor;
            ShuttleUILayout.SafeLabel(
                new Rect(
                    rowRect.x + rowRect.width * 0.50f,
                    rowRect.y,
                    rowRect.width * 0.50f,
                    rowRect.height),
                V3CargoLoadDialogStyle.FitLabelText(value, rowRect.width * 0.50f));
            y += 26f;
        }

        private string GetRowText(V3QueuedLoadDialogModel model)
        {
            if (model == null || !model.SnapshotAvailable)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            return model.RowCount.ToString();
        }

        private string GetThingCountText(V3QueuedLoadDialogModel model)
        {
            if (model == null || !model.SnapshotAvailable)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            return model.QueuedThingCount.ToString();
        }

        private string GetMassText(V3QueuedLoadDialogModel model)
        {
            if (model == null || !model.SnapshotAvailable)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            return ShuttleUIMetricFormatter.FormatKgCompact(model.QueuedMassKg);
        }

        private string GetPawnCountText(V3QueuedLoadDialogModel model)
        {
            if (model == null || !model.SnapshotAvailable)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            return this.CountQueuedThings(model, true).ToString();
        }

        private string GetCargoCountText(V3QueuedLoadDialogModel model)
        {
            if (model == null || !model.SnapshotAvailable)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            return this.CountQueuedThings(model, false).ToString();
        }

        private string GetStatusText(V3QueuedLoadDialogModel model)
        {
            if (model == null || !model.SnapshotAvailable)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_Unavailable");
            }

            if (!model.HasRows)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_QueuedLoad_NoQueue");
            }

            return this.HasAbnormalQueuedRows(model)
                ? V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_Unavailable")
                : V3CargoLoadText.Tr("CT_Shuttle_QueuedLoad_Waiting");
        }

        private Color GetStatusColor(V3QueuedLoadDialogModel model)
        {
            if (model == null || !model.SnapshotAvailable)
            {
                return V3CargoLoadDialogStyle.YellowColor;
            }

            if (!model.HasRows)
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            return this.HasAbnormalQueuedRows(model)
                ? V3CargoLoadDialogStyle.YellowColor
                : V3CargoLoadDialogStyle.AccentColor;
        }

        private int CountQueuedThings(V3QueuedLoadDialogModel model, bool pawns)
        {
            int count = 0;
            if (model == null || model.Rows == null)
            {
                return count;
            }

            for (int i = 0; i < model.Rows.Count; i++)
            {
                V3QueuedLoadRowModel row = model.Rows[i];
                if (row != null && row.IsPawn == pawns)
                {
                    count += Mathf.Max(0, row.StackCount);
                }
            }

            return count;
        }

        private bool HasAbnormalQueuedRows(V3QueuedLoadDialogModel model)
        {
            if (model == null || model.Rows == null)
            {
                return false;
            }

            for (int i = 0; i < model.Rows.Count; i++)
            {
                V3QueuedLoadRowModel row = model.Rows[i];
                if (row != null && !row.CanCancel)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

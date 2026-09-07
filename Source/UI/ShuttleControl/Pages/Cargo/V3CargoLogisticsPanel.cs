using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoLogisticsPanel
    {
        private const float PanelContentTopOffset = 44f;
        private const float HeaderIconSize = 58f;
        private const float ConnectionRowStride = 58f;
        private const float ConnectionRowHeight = 52f;
        private const float ConnectionIconSize = 34f;
        private const float ConnectionStatusWidth = 82f;

        private readonly V3CargoText text;
        private readonly V3CargoPanelDrawer panel;

        internal V3CargoLogisticsPanel(
            V3CargoText text,
            V3CargoPanelDrawer panel)
        {
            this.text = text;
            this.panel = panel;
        }

        internal void Draw(
            Rect rect,
            V3CargoPageModel pageModel,
            V3CargoPageState state,
            ShuttlePageDrawContext context)
        {
            this.panel.DrawPanelTitle(rect, this.text.Tr("CT_Shuttle_Cargo_LogisticsModule"));
            Rect inner = this.panel.GetPanelInnerRect(rect, PanelContentTopOffset);
            V3CargoPageReadModel cargoModel = pageModel != null ? pageModel.CargoPageModel : null;
            bool installed = cargoModel != null && cargoModel.HasCargoLogisticsModule;
            bool enabled = cargoModel != null && cargoModel.CargoLogisticsModuleEnabled;

            float contentY = this.DrawHeader(inner, cargoModel, installed, enabled, context);

            List<V3CargoLogisticsConnectionModel> connections =
                cargoModel != null ? cargoModel.LogisticsConnections : null;
            int count = connections != null ? connections.Count : 0;
            if (count <= 0)
            {
                return;
            }

            Rect listRect = new Rect(
                inner.x,
                contentY,
                inner.width,
                Mathf.Max(0f, inner.yMax - contentY));
            Rect viewRect = new Rect(
                0f,
                0f,
                listRect.width - 16f,
                Mathf.Max(listRect.height, count * ConnectionRowStride));
            Widgets.BeginScrollView(listRect, ref state.CargoLogisticsScroll, viewRect);
            int firstIndex;
            int lastIndexExclusive;
            GetVisibleRowRange(
                state.CargoLogisticsScroll,
                listRect.height,
                count,
                ConnectionRowStride,
                out firstIndex,
                out lastIndexExclusive);
            for (int i = firstIndex; i < lastIndexExclusive; i++)
            {
                this.DrawConnectionRow(
                    new Rect(0f, i * ConnectionRowStride, viewRect.width, ConnectionRowHeight),
                    connections[i],
                    context);
            }

            Widgets.EndScrollView();
        }

        private float DrawHeader(
            Rect inner,
            V3CargoPageReadModel cargoModel,
            bool installed,
            bool enabled,
            ShuttlePageDrawContext context)
        {
            Rect iconRect = new Rect(inner.x + 6f, inner.y + 4f, HeaderIconSize, HeaderIconSize);
            Color headerIconColor = installed && enabled
                ? V3CargoText.GreenColor
                : installed
                ? V3CargoText.YellowColor
                : new Color(0.46f, 0.49f, 0.52f, 1f);
            this.panel.DrawFramelessIcon(
                iconRect,
                context,
                "cargo_loader",
                "L",
                headerIconColor,
                !installed);

            float textX = iconRect.xMax + 10f;
            float textWidth = Mathf.Max(40f, inner.xMax - textX - 6f);
            string status = cargoModel != null &&
                !string.IsNullOrEmpty(cargoModel.CargoLogisticsStatusLabel)
                    ? cargoModel.CargoLogisticsStatusLabel
                    : installed
                    ? enabled
                    ? this.text.Tr("CT_Shuttle_Cargo_LogisticsStatusEnabled")
                    : this.text.Tr("CT_Shuttle_Cargo_LogisticsStatusDisabled")
                    : this.text.Tr("CT_Shuttle_Cargo_LogisticsStatusMissing");
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(textX, inner.y + 3f, textWidth, 24f),
                status,
                GameFont.Small,
                GameFont.Tiny,
                installed && enabled ? Color.white : ShuttleUIStyle.MutedTextColor,
                status,
                TextAnchor.MiddleLeft);

            string description = cargoModel != null &&
                !string.IsNullOrEmpty(cargoModel.CargoLogisticsDescription)
                    ? cargoModel.CargoLogisticsDescription
                    : installed
                    ? this.text.Tr("CT_Shuttle_Cargo_LogisticsDescription")
                    : this.text.Tr("CT_Shuttle_Cargo_LogisticsModuleNotInstalled");
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(textX, inner.y + 29f, textWidth, 22f),
                description,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                description,
                TextAnchor.MiddleLeft);

            if (installed &&
                cargoModel != null &&
                !string.IsNullOrEmpty(cargoModel.CargoLogisticsCapabilitySummary))
            {
                bool allCapabilitiesAvailable =
                    cargoModel.CargoLogisticsInternalBusPowered &&
                    cargoModel.CargoLogisticsSupportsItemTransfer &&
                    cargoModel.CargoLogisticsSupportsItemConsumption &&
                    cargoModel.CargoLogisticsSupportsItemDeposit;
                Color summaryColor = allCapabilitiesAvailable
                    ? V3CargoText.GreenColor
                    : V3CargoText.YellowColor;
                ShuttleUILayout.DrawFittedSingleLineLabel(
                    new Rect(textX, inner.y + 52f, textWidth, 18f),
                    cargoModel.CargoLogisticsCapabilitySummary,
                    GameFont.Tiny,
                    GameFont.Tiny,
                    summaryColor,
                    cargoModel.CargoLogisticsCapabilitySummary,
                    TextAnchor.MiddleLeft);
            }

            return inner.y + (installed ? 78f : 58f);
        }

        private void DrawConnectionRow(
            Rect rect,
            V3CargoLogisticsConnectionModel connection,
            ShuttlePageDrawContext context)
        {
            if (connection == null)
            {
                return;
            }

            this.DrawConnectionBackground(rect, connection);
            Rect iconRect = new Rect(
                rect.x + 7f,
                rect.y + Mathf.Floor((rect.height - ConnectionIconSize) * 0.5f),
                ConnectionIconSize,
                ConnectionIconSize);
            Color connectionColor = this.text.GetLogisticsColor(connection.StatusKey);
            this.panel.DrawFramelessIcon(
                iconRect,
                context,
                !string.IsNullOrEmpty(connection.ResolvedIconKey)
                    ? connection.ResolvedIconKey
                    : ResolveConnectionIconKey(connection),
                "L",
                connectionColor,
                !connection.Installed);

            Rect statusRect = new Rect(
                rect.xMax - ConnectionStatusWidth - 6f,
                rect.y + 13f,
                ConnectionStatusWidth,
                22f);
            float textX = iconRect.xMax + 8f;
            float textWidth = Mathf.Max(40f, statusRect.x - textX - 8f);

            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(textX, rect.y + 7f, textWidth, 20f),
                connection.Label,
                GameFont.Tiny,
                GameFont.Tiny,
                Color.white,
                connection.Tooltip,
                TextAnchor.MiddleLeft);
            this.DrawStatusText(statusRect, connection);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(textX, rect.y + 27f, textWidth, 18f),
                connection.Description,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                connection.Tooltip,
                TextAnchor.MiddleLeft);
            this.text.AddTooltip(rect, connection.Tooltip);
        }

        private void DrawConnectionBackground(
            Rect rect,
            V3CargoLogisticsConnectionModel connection)
        {
            bool hovered = Mouse.IsOver(rect);
            bool installed = connection != null && connection.Installed;
            bool enabled = connection != null && connection.Enabled;
            Color background = installed
                ? new Color(0.052f, 0.095f, 0.116f, hovered ? 0.96f : 0.88f)
                : new Color(0.042f, 0.052f, 0.068f, hovered ? 0.92f : 0.82f);
            Widgets.DrawBoxSolid(rect, background);

            Color accent = installed
                ? (enabled ? ShuttleUIStyle.BlueStatusColor : ShuttleUIStyle.YellowStatusColor)
                : ShuttleUIStyle.MutedTextColor;
            Widgets.DrawBoxSolid(
                new Rect(rect.x, rect.y + 4f, 2f, Mathf.Max(0f, rect.height - 8f)),
                ShuttleUIStyle.WithAlpha(accent, installed ? 0.72f : 0.34f));

            Color border = installed
                ? ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, hovered ? 0.52f : 0.34f)
                : ShuttleUIStyle.WithAlpha(ShuttleUIStyle.SubtleBorderColor, hovered ? 0.76f : 0.48f);
            ShuttleUILayout.DrawRectBorder(rect, border, ShuttleUIStyle.ThinBorder);
        }

        private void DrawStatusText(Rect rect, V3CargoLogisticsConnectionModel connection)
        {
            Color color = this.text.GetLogisticsColor(connection != null ? connection.StatusKey : null);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect,
                connection != null ? connection.StatusLabel : null,
                GameFont.Tiny,
                GameFont.Tiny,
                color,
                connection != null ? connection.Tooltip : null,
                TextAnchor.MiddleLeft);
        }

        private static string ResolveConnectionIconKey(V3CargoLogisticsConnectionModel connection)
        {
            if (connection == null)
            {
                return "module_option";
            }

            if (connection.Key == "standard-cargo")
            {
                return "module_cargo";
            }

            return !string.IsNullOrEmpty(connection.IconKey)
                ? connection.IconKey
                : "module_option";
        }

        private static void GetVisibleRowRange(
            Vector2 scroll,
            float viewportHeight,
            int rowCount,
            float rowStride,
            out int firstIndex,
            out int lastIndexExclusive)
        {
            if (rowCount <= 0 || rowStride <= 0f)
            {
                firstIndex = 0;
                lastIndexExclusive = 0;
                return;
            }

            firstIndex = Mathf.Clamp(Mathf.FloorToInt(scroll.y / rowStride), 0, rowCount - 1);
            int visibleRows = Mathf.CeilToInt(viewportHeight / rowStride) + 2;
            lastIndexExclusive = Mathf.Clamp(firstIndex + visibleRows, firstIndex, rowCount);
        }
    }
}

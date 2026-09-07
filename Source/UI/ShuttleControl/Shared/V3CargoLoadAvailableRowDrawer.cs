using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoLoadAvailableRowDrawer
    {
        private const float LockedLabelWidth = 58f;
        private const float LockedLabelGap = 4f;

        private readonly V3CargoLoadTransferableMetricsResolver metricsResolver;
        private readonly V3CargoLoadSelectionModel selectionModel;
        private readonly V3CargoLoadQuantityEditor quantityEditor;
        private readonly Func<bool> compactIconModeProvider;

        internal V3CargoLoadAvailableRowDrawer(
            V3CargoLoadTransferableMetricsResolver metricsResolver,
            V3CargoLoadSelectionModel selectionModel,
            Action<TransferableOneWay, int> adjustSelection,
            Action<TransferableOneWay, int> setSelectionCount,
            Action<TransferableOneWay> clearSelection,
            Action<TransferableOneWay> selectMaximumFit,
            Func<bool> compactIconModeProvider)
        {
            this.metricsResolver = metricsResolver;
            this.selectionModel = selectionModel;
            this.quantityEditor = new V3CargoLoadQuantityEditor(
                selectionModel,
                adjustSelection,
                setSelectionCount,
                clearSelection,
                selectMaximumFit);
            this.compactIconModeProvider = compactIconModeProvider;
        }

        internal void Draw(
            Rect rect,
            TransferableOneWay transferable,
            int visibleRowIndex)
        {
            V3CargoLoadTransferableMetrics metrics =
                this.metricsResolver != null
                    ? this.metricsResolver.GetMetrics(transferable)
                    : V3CargoLoadTransferableMetrics.Empty;
            int selectedCount = transferable != null ? transferable.CountToTransfer : 0;
            bool selected = selectedCount > 0;
            bool canAddOne = this.selectionModel != null &&
                this.selectionModel.CanAddCountWithUnitMass(
                    transferable,
                    1,
                    metrics.UnitMass);
            bool locked = !selected && !canAddOne;

            this.DrawRowBackground(rect, visibleRowIndex, selected, locked);
            bool hovered = Mouse.IsOver(rect);
            if (hovered)
            {
                Widgets.DrawHighlight(rect);
            }

            V3CargoLoadAvailableTableLayout layout =
                V3CargoLoadAvailableTableLayout.Create(rect);
            V3CargoLoadDialogStyle.DrawThingIcon(
                layout.IconRect,
                metrics.DisplayThing,
                this.UseCompactIconMode());
            if (layout.ShowInfoButton && metrics.DisplayThing != null)
            {
                if (hovered)
                {
                    Widgets.InfoCardButtonCentered(layout.InfoRect, metrics.DisplayThing);
                }
                else
                {
                    this.DrawStaticInfoMarker(layout.InfoRect);
                }
            }

            this.DrawName(layout.NameRect, metrics.LabelWithoutCount, locked);
            this.DrawCell(
                layout.AvailableRect,
                V3CargoLoadText.FormatCount(metrics),
                ShuttleUIStyle.MutedTextColor,
                TextAnchor.MiddleRight);
            if (layout.ShowUnitMass)
            {
                this.DrawCell(
                    layout.UnitMassRect,
                    ShuttleUIMetricFormatter.FormatKgCompact(metrics.UnitMass),
                    ShuttleUIStyle.MutedTextColor,
                    TextAnchor.MiddleRight);
            }

            if (layout.ShowDestination)
            {
                this.DrawCell(
                    layout.DestinationRect,
                    V3CargoLoadText.GetRowDetailText(
                        this.selectionModel,
                        transferable,
                        metrics),
                    ShuttleUIStyle.MutedTextColor,
                    TextAnchor.MiddleCenter);
            }

            this.quantityEditor.Draw(
                layout.QuantityRect,
                transferable,
                metrics,
                hovered,
                locked);
            if (hovered)
            {
                Rect detailsTooltipRect = new Rect(
                    rect.x,
                    rect.y,
                    Mathf.Max(0f, layout.QuantityRect.x - rect.x - 2f),
                    rect.height);
                TooltipHandler.TipRegion(
                    detailsTooltipRect,
                    V3CargoLoadText.GetTransferableTooltip(
                        this.selectionModel,
                        transferable,
                        metrics));
            }

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawRowBackground(
            Rect rect,
            int visibleRowIndex,
            bool selected,
            bool locked)
        {
            Color background = locked
                ? V3CargoLoadDialogStyle.DisabledCardColor
                : selected
                    ? V3CargoLoadDialogStyle.SelectedCardColor
                    : V3CargoLoadDialogStyle.CardColor;
            Widgets.DrawBoxSolid(rect, background);
            if (!selected && !locked && visibleRowIndex % 2 == 1)
            {
                Widgets.DrawLightHighlight(rect);
            }

            Widgets.DrawBoxSolid(
                new Rect(rect.x, rect.yMax - 1f, rect.width, 1f),
                ShuttleUIStyle.SubtleBorderColor);
            if (selected)
            {
                Widgets.DrawBoxSolid(
                    new Rect(rect.x, rect.y, 2f, rect.height),
                    V3CargoLoadDialogStyle.AccentColor);
            }
        }

        private void DrawName(Rect rect, string label, bool locked)
        {
            Rect labelRect = rect.ContractedBy(2f);
            if (locked)
            {
                float lockedWidth = Mathf.Min(
                    LockedLabelWidth,
                    Mathf.Max(0f, labelRect.width * 0.38f));
                Rect lockedRect = new Rect(
                    labelRect.xMax - lockedWidth,
                    labelRect.y,
                    lockedWidth,
                    labelRect.height);
                labelRect.width = Mathf.Max(
                    24f,
                    lockedRect.x - labelRect.x - LockedLabelGap);
                this.DrawCell(
                    lockedRect,
                    V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_Locked"),
                    V3CargoLoadDialogStyle.RedColor,
                    TextAnchor.MiddleRight);
                TooltipHandler.TipRegion(
                    lockedRect,
                    V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_LockedTooltip"));
            }

            this.DrawCell(
                labelRect,
                label,
                Color.white,
                TextAnchor.MiddleLeft,
                GameFont.Small);
        }

        private void DrawCell(
            Rect rect,
            string text,
            Color color,
            TextAnchor anchor,
            GameFont font = GameFont.Tiny)
        {
            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect,
                string.IsNullOrEmpty(text) ? "-" : text,
                font,
                GameFont.Tiny,
                color,
                null,
                anchor);
        }

        private void DrawStaticInfoMarker(Rect rect)
        {
            this.DrawCell(
                rect,
                "i",
                ShuttleUIStyle.MutedTextColor,
                TextAnchor.MiddleCenter,
                GameFont.Small);
        }

        private bool UseCompactIconMode()
        {
            return this.compactIconModeProvider != null && this.compactIconModeProvider();
        }
    }
}

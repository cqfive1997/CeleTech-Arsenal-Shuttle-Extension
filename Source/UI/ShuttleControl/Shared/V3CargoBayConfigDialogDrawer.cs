using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoBayConfigDialogDrawer
    {
        private const float Gap = 10f;
        private const float TitleHeight = 46f;
        private const float ButtonHeight = 30f;
        private const float ButtonWidth = 96f;
        private const float FooterPadding = 12f;

        private readonly ShuttleCargoBayActionTarget bay;
        private readonly ShuttleCargoBayConfigActionTarget workingConfig;
        private readonly V3CargoBayConfigEditorDrawer editorDrawer;
        private Vector2 filterSummaryScroll;

        internal V3CargoBayConfigDialogDrawer(
            ShuttleCargoBayActionTarget bay,
            ShuttleCargoBayConfigActionTarget originalConfig,
            ShuttleCargoBayConfigActionTarget workingConfig)
        {
            this.bay = bay;
            this.workingConfig = workingConfig;
            this.editorDrawer = new V3CargoBayConfigEditorDrawer(
                bay,
                originalConfig,
                workingConfig);
        }

        internal void Draw(
            Rect inRect,
            bool canApply,
            string applyTooltip,
            Action apply,
            Action reset,
            Action close)
        {
            V3CargoBayConfigDialogChrome.DrawWindowBackground(inRect);

            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, TitleHeight);
            Rect buttonRect = new Rect(
                inRect.x + FooterPadding,
                inRect.yMax - FooterPadding - ButtonHeight,
                Mathf.Max(0f, inRect.width - (FooterPadding * 2f)),
                ButtonHeight);
            float bodyTop = titleRect.yMax + Gap;
            Rect bodyRect = new Rect(
                inRect.x,
                bodyTop,
                inRect.width,
                Mathf.Max(0f, buttonRect.y - bodyTop - Gap));

            this.DrawTitle(titleRect);
            this.DrawBody(bodyRect);
            this.DrawButtons(buttonRect, canApply, applyTooltip, apply, reset, close);
        }

        private void DrawTitle(Rect rect)
        {
            string title = ShuttleUIText.Tr(
                this.IsRefrigerated()
                    ? "CT_Shuttle_Cargo_Config_RefrigeratedTitle"
                    : "CT_Shuttle_Cargo_Config_NormalTitle",
                this.GetSubmittedLabel(this.workingConfig.Label));
            V3CargoBayConfigDialogChrome.DrawTitle(rect, title, this.GetFilterStatusText());
        }

        private void DrawBody(Rect rect)
        {
            float leftWidth = Mathf.Floor((rect.width - Gap) * 0.50f);
            Rect basicRect = new Rect(rect.x, rect.y, leftWidth, 146f);
            Rect filterRect = new Rect(
                rect.x,
                basicRect.yMax + Gap,
                leftWidth,
                Mathf.Max(0f, rect.height - basicRect.height - Gap));
            Rect editorRect = new Rect(
                basicRect.xMax + Gap,
                rect.y,
                rect.width - leftWidth - Gap,
                rect.height);

            this.DrawBasicInfoSection(basicRect);
            this.DrawFilterSection(filterRect);
            this.editorDrawer.Draw(editorRect);
        }

        private void DrawBasicInfoSection(Rect rect)
        {
            V3CargoBayConfigDialogChrome.DrawPanelTitle(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_BasicInfo"));
            Rect inner = V3CargoBayConfigDialogChrome.GetPanelInnerRect(rect);
            float y = inner.y;
            V3CargoBayConfigDialogChrome.DrawInfoRow(
                ref y,
                inner,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_Name"),
                this.GetSubmittedLabel(this.workingConfig.Label));
            V3CargoBayConfigDialogChrome.DrawInfoRow(
                ref y,
                inner,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_Type"),
                this.GetBayTypeText());
            V3CargoBayConfigDialogChrome.DrawInfoRow(
                ref y,
                inner,
                ShuttleUIText.Tr(
                    this.IsRefrigerated()
                        ? "CT_Shuttle_Cargo_Config_StoredMass"
                        : "CT_Shuttle_Cargo_Config_UsedCapacity"),
                this.GetMassText());
            V3CargoBayConfigDialogChrome.DrawInfoRow(
                ref y,
                inner,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_Status"),
                this.GetStatusText());
        }

        private void DrawFilterSection(Rect rect)
        {
            V3CargoBayConfigDialogChrome.DrawPanelTitle(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_FilterPlan"));
            Rect inner = V3CargoBayConfigDialogChrome.GetPanelInnerRect(rect);
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(0f, inner.width - 16f), 260f);
            Widgets.BeginScrollView(inner, ref this.filterSummaryScroll, viewRect);

            float y = 0f;
            V3CargoBayConfigDialogChrome.DrawWrappedInfoBlock(
                new Rect(0f, y, viewRect.width, 54f),
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_CurrentSummary"),
                this.GetFilterStatusText());
            y += 62f;

            if (this.IsRefrigerated())
            {
                this.DrawRefrigeratedSummary(ref y, viewRect);
            }
            else if (this.bay != null && this.bay.HasPawnFilterDetails)
            {
                V3CargoBayConfigDialogChrome.DrawInfoLine(
                    ref y,
                    viewRect,
                    ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_ItemFilter"),
                    V3CargoBayConfigDialogChrome.TextOrUnavailable(this.bay.ItemFilterSummary),
                    ShuttleV3DialogStyle.MutedTextColor);
            }
            else
            {
                V3CargoBayConfigDialogChrome.DrawWrappedInfoBlock(
                    new Rect(0f, y, viewRect.width, 62f),
                    ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_DetailedFilter"),
                    ShuttleUIText.Tr(
                        "CT_Shuttle_Cargo_Config_DetailedFilterUnavailable"));
            }

            Widgets.EndScrollView();
        }

        private void DrawRefrigeratedSummary(ref float y, Rect viewRect)
        {
            V3CargoBayConfigDialogChrome.DrawInfoLine(
                ref y,
                viewRect,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_Cooling"),
                this.bay != null && this.bay.CoolingActive
                    ? ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_CoolingActive")
                    : ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_CoolingInactive"),
                this.bay != null && this.bay.CoolingActive
                    ? ShuttleV3DialogStyle.BlueStatusColor
                    : ShuttleV3DialogStyle.YellowStatusColor);
            if (this.bay != null && !string.IsNullOrEmpty(this.bay.InactiveReason))
            {
                V3CargoBayConfigDialogChrome.DrawInfoLine(
                    ref y,
                    viewRect,
                    ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_InactiveReason"),
                    this.bay.InactiveReason,
                    ShuttleV3DialogStyle.YellowStatusColor);
            }

            V3CargoBayConfigDialogChrome.DrawInfoLine(
                ref y,
                viewRect,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_ColdRoutingOnLoad"),
                this.bay != null && this.bay.HasAutoTransferDetails
                    ? this.GetBoolLabel(this.workingConfig.AutoTransferEnabled)
                    : ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_DetailsUnavailable"),
                ShuttleV3DialogStyle.MutedTextColor);
            V3CargoBayConfigDialogChrome.DrawInfoLine(
                ref y,
                viewRect,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_ColdFilter"),
                V3CargoBayConfigDialogChrome.TextOrUnavailable(
                    this.bay != null ? this.bay.AutoTransferFilterSummary : null),
                ShuttleV3DialogStyle.MutedTextColor);
        }

        private void DrawButtons(
            Rect rect,
            bool canApply,
            string applyTooltip,
            Action apply,
            Action reset,
            Action close)
        {
            Rect closeRect = new Rect(rect.xMax - ButtonWidth, rect.y, ButtonWidth, rect.height);
            Rect resetRect = new Rect(closeRect.x - Gap - ButtonWidth, rect.y, ButtonWidth, rect.height);
            Rect applyRect = new Rect(resetRect.x - Gap - ButtonWidth, rect.y, ButtonWidth, rect.height);

            if (V3CargoBayConfigDialogChrome.DrawFeedbackButton(
                applyRect,
                ShuttleUIText.Tr("CT_Shuttle_UI_Apply"),
                canApply,
                ShuttleV3DialogButtonKind.Primary,
                applyTooltip) &&
                apply != null)
            {
                apply();
            }

            if (V3CargoBayConfigDialogChrome.DrawFeedbackButton(
                resetRect,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_Reset"),
                true,
                ShuttleV3DialogButtonKind.Normal,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_FilterResetTooltip")) &&
                reset != null)
            {
                reset();
            }

            if (V3CargoBayConfigDialogChrome.DrawFeedbackButton(
                closeRect,
                ShuttleUIText.Tr("CT_Shuttle_UI_Close"),
                true,
                ShuttleV3DialogButtonKind.Normal,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_CloseWithoutApply")) &&
                close != null)
            {
                close();
            }
        }

        private bool IsRefrigerated()
        {
            return this.bay != null && this.bay.IsRefrigerated;
        }

        private string GetBayTypeText()
        {
            return this.IsRefrigerated()
                ? ShuttleUIText.Tr("CT_Shuttle_Cargo_Cold")
                : ShuttleUIText.Tr("CT_Shuttle_Cargo_Bay");
        }

        private string GetBayLabel()
        {
            return this.bay != null && !string.IsNullOrEmpty(this.bay.Label)
                ? this.bay.Label
                : ShuttleUIText.Tr("CT_Shuttle_Cargo_Bay");
        }

        private string GetSubmittedLabel(string label)
        {
            string trimmed = (label ?? string.Empty).Trim();
            return !string.IsNullOrEmpty(trimmed)
                ? trimmed
                : this.GetBayLabel();
        }

        private string GetMassText()
        {
            if (this.bay == null)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Cargo_CapacityUnavailable");
            }

            return this.IsRefrigerated()
                ? ShuttleUIMetricFormatter.FormatKgCompact(
                    Mathf.Max(0f, this.bay.UsedMassKg))
                : ShuttleUIMetricFormatter.FormatKgPair(
                    Mathf.Max(0f, this.bay.UsedMassKg),
                    Mathf.Max(0f, this.bay.CapacityKg));
        }

        private string GetStatusText()
        {
            return this.bay != null && !string.IsNullOrEmpty(this.bay.StatusText)
                ? this.bay.StatusText
                : ShuttleUIText.Tr("CT_Shuttle_Cargo_Unknown");
        }

        private string GetFilterStatusText()
        {
            if (this.IsRefrigerated())
            {
                string mode = this.workingConfig.AutoTransferEnabled
                    ? ShuttleUIText.Tr("CT_Shuttle_Cargo_AutoTransferOn")
                    : ShuttleUIText.Tr("CT_Shuttle_Cargo_AutoTransferOff");
                string filter = this.bay != null &&
                    !string.IsNullOrEmpty(this.bay.AutoTransferFilterSummary)
                        ? this.bay.AutoTransferFilterSummary
                        : ShuttleUIText.Tr("CT_Shuttle_Cargo_FilterUnavailable");
                return mode + " / " + filter;
            }

            return this.bay != null && !string.IsNullOrEmpty(this.bay.FilterSummary)
                ? this.bay.FilterSummary
                : ShuttleUIText.Tr("CT_Shuttle_Cargo_FilterUnavailable");
        }

        private string GetBoolLabel(bool value)
        {
            return value
                ? ShuttleUIText.Tr("CT_Shuttle_Yes")
                : ShuttleUIText.Tr("CT_Shuttle_No");
        }
    }
}

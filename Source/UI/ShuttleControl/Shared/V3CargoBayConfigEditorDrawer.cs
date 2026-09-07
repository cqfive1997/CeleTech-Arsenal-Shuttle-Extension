using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoBayConfigEditorDrawer
    {
        private const float NoticeHeight = 40f;

        private readonly ShuttleCargoBayActionTarget bay;
        private readonly ShuttleCargoBayConfigActionTarget originalConfig;
        private readonly ShuttleCargoBayConfigActionTarget workingConfig;
        private readonly ThingFilterUI.UIState filterState =
            new ThingFilterUI.UIState();
        private readonly V3CargoBayConfigFilterDrawer filterDrawer =
            new V3CargoBayConfigFilterDrawer();

        internal V3CargoBayConfigEditorDrawer(
            ShuttleCargoBayActionTarget bay,
            ShuttleCargoBayConfigActionTarget originalConfig,
            ShuttleCargoBayConfigActionTarget workingConfig)
        {
            this.bay = bay;
            this.originalConfig = originalConfig;
            this.workingConfig = workingConfig;
        }

        internal void Draw(Rect rect)
        {
            V3CargoBayConfigDialogChrome.DrawPanelTitle(
                rect,
                this.IsRefrigerated()
                    ? ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_ColdTransfer")
                    : ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_FilterEditor"));
            Rect inner = V3CargoBayConfigDialogChrome.GetPanelInnerRect(rect);
            if (this.IsRefrigerated())
            {
                this.DrawRefrigeratedEditor(inner);
            }
            else
            {
                this.DrawNormalEditor(inner);
            }
        }

        private void DrawNormalEditor(Rect inner)
        {
            if (this.workingConfig.RegionIndex < 0 ||
                this.workingConfig.ItemFilter == null)
            {
                V3CargoBayConfigDialogChrome.DrawUnavailableEditor(
                    inner,
                    ShuttleUIText.Tr("CT_Shuttle_Cargo_FilterSettingsUnavailable"));
                return;
            }

            Rect noticeRect = new Rect(inner.x, inner.y, inner.width, NoticeHeight);
            V3CargoBayConfigDialogChrome.DrawNotice(
                noticeRect,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_FilterEditorNotice"));

            Rect captionRect = new Rect(inner.x, noticeRect.yMax + 8f, inner.width, 18f);
            V3CargoBayConfigDialogChrome.DrawTinyCaption(
                captionRect,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_CargoBayName"));

            Rect labelRect = new Rect(inner.x, captionRect.yMax + 2f, inner.width, 26f);
            this.workingConfig.Label = Widgets.TextField(
                labelRect,
                this.workingConfig.Label ?? string.Empty);
            TooltipHandler.TipRegion(
                labelRect,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_FilterLabelTooltip"));

            float nextY = labelRect.yMax + 10f;
            Rect bulkRect = new Rect(inner.x, nextY, inner.width, 28f);
            this.DrawNormalBulkButtons(bulkRect);

            Rect filterRect = new Rect(
                inner.x,
                bulkRect.yMax + 8f,
                inner.width,
                Mathf.Max(0f, inner.yMax - bulkRect.yMax - 8f));
            this.filterDrawer.Draw(
                filterRect,
                this.filterState,
                this.workingConfig.ItemFilter,
                true);
        }

        private void DrawRefrigeratedEditor(Rect inner)
        {
            if (string.IsNullOrEmpty(this.workingConfig.ModuleInstanceID) ||
                this.workingConfig.AutoTransferFilter == null)
            {
                V3CargoBayConfigDialogChrome.DrawUnavailableEditor(
                    inner,
                    ShuttleUIText.Tr("CT_Shuttle_Cargo_ColdSettingsUnavailable"));
                return;
            }

            Rect noticeRect = new Rect(inner.x, inner.y, inner.width, NoticeHeight);
            V3CargoBayConfigDialogChrome.DrawNotice(
                noticeRect,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_ColdEditorNotice"));

            Rect captionRect = new Rect(inner.x, noticeRect.yMax + 8f, inner.width, 18f);
            V3CargoBayConfigDialogChrome.DrawTinyCaption(
                captionRect,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_RefrigeratedBayName"));

            Rect labelRect = new Rect(inner.x, captionRect.yMax + 2f, inner.width, 26f);
            this.workingConfig.Label = Widgets.TextField(
                labelRect,
                this.workingConfig.Label ?? string.Empty);
            TooltipHandler.TipRegion(
                labelRect,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_ColdLabelTooltip"));

            bool controlsEnabled = this.bay != null && this.bay.IsEnabled;
            Rect autoRect = new Rect(inner.x, labelRect.yMax + 8f, inner.width, 24f);
            this.DrawCheckbox(
                autoRect,
                ShuttleUIText.Tr("CT_Shuttle_UI_RefrigeratedAutoTransfer"),
                ref this.workingConfig.AutoTransferEnabled,
                controlsEnabled,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_ColdConfigTooltip"));

            Rect customRect = new Rect(inner.x, autoRect.yMax + 4f, inner.width, 24f);
            this.DrawCheckbox(
                customRect,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Config_UseCustomFilter"),
                ref this.workingConfig.HasCustomAutoTransferFilter,
                controlsEnabled,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_ColdCustomFilterTooltip"));

            Rect bulkRect = new Rect(inner.x, customRect.yMax + 8f, inner.width, 28f);
            this.DrawColdBulkButtons(bulkRect, controlsEnabled);

            Rect filterRect = new Rect(
                inner.x,
                bulkRect.yMax + 8f,
                inner.width,
                Mathf.Max(0f, inner.yMax - bulkRect.yMax - 8f));
            this.filterDrawer.Draw(
                filterRect,
                this.filterState,
                this.workingConfig.AutoTransferFilter,
                controlsEnabled && this.workingConfig.HasCustomAutoTransferFilter);
            if (!controlsEnabled || !this.workingConfig.HasCustomAutoTransferFilter)
            {
                TooltipHandler.TipRegion(
                    filterRect,
                    ShuttleUIText.Tr("CT_Shuttle_Cargo_ColdCustomFilterTooltip"));
            }
        }

        private void DrawNormalBulkButtons(Rect rect)
        {
            Rect allowRect = new Rect(rect.x, rect.y, 120f, rect.height);
            Rect disallowRect = new Rect(allowRect.xMax + 8f, rect.y, 120f, rect.height);
            if (V3CargoBayConfigDialogChrome.DrawFeedbackButton(
                allowRect,
                ShuttleUIText.Tr("CT_Shuttle_UI_AllowAllItems"),
                true,
                ShuttleV3DialogButtonKind.Primary,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_FilterEditorNotice")))
            {
                this.workingConfig.ItemFilter.CopyAllowancesFrom(
                    ThingFilter.CreateOnlyEverStorableThingFilter());
            }

            if (V3CargoBayConfigDialogChrome.DrawFeedbackButton(
                disallowRect,
                ShuttleUIText.Tr("CT_Shuttle_UI_DisallowItems"),
                true,
                ShuttleV3DialogButtonKind.Danger,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_FilterEditorNotice")))
            {
                this.workingConfig.ItemFilter.SetDisallowAll(null, null);
            }
        }

        private void DrawColdBulkButtons(Rect rect, bool controlsEnabled)
        {
            Rect allowRect = new Rect(rect.x, rect.y, 112f, rect.height);
            Rect disallowRect = new Rect(allowRect.xMax + 8f, rect.y, 112f, rect.height);
            Rect clearRect = new Rect(rect.xMax - 96f, rect.y, 96f, rect.height);

            if (V3CargoBayConfigDialogChrome.DrawFeedbackButton(
                allowRect,
                ShuttleUIText.Tr("CT_Shuttle_UI_AllowAllItems"),
                controlsEnabled,
                ShuttleV3DialogButtonKind.Primary,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_ColdEditorNotice")))
            {
                this.workingConfig.HasCustomAutoTransferFilter = true;
                this.workingConfig.AutoTransferFilter.CopyAllowancesFrom(
                    ThingFilter.CreateOnlyEverStorableThingFilter());
            }

            if (V3CargoBayConfigDialogChrome.DrawFeedbackButton(
                disallowRect,
                ShuttleUIText.Tr("CT_Shuttle_UI_DisallowItems"),
                controlsEnabled,
                ShuttleV3DialogButtonKind.Danger,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_ColdEditorNotice")))
            {
                this.workingConfig.HasCustomAutoTransferFilter = true;
                this.workingConfig.AutoTransferFilter.SetDisallowAll(null, null);
            }

            if (V3CargoBayConfigDialogChrome.DrawFeedbackButton(
                clearRect,
                ShuttleUIText.Tr("CT_Shuttle_UI_ClearFilter"),
                controlsEnabled && this.workingConfig.HasCustomAutoTransferFilter,
                ShuttleV3DialogButtonKind.Danger,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_ColdClearFilterTooltip")))
            {
                this.workingConfig.HasCustomAutoTransferFilter = false;
                this.workingConfig.AutoTransferFilter.CopyAllowancesFrom(
                    this.originalConfig.AutoTransferFilter);
            }
        }

        private void DrawCheckbox(
            Rect rect,
            string label,
            ref bool value,
            bool enabled,
            string tooltip)
        {
            bool oldEnabled = GUI.enabled;
            GUI.enabled = oldEnabled && enabled;
            Widgets.CheckboxLabeled(
                rect,
                V3CargoBayConfigDialogChrome.FitLabelText(label, rect.width),
                ref value);
            GUI.enabled = oldEnabled;
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }
        }

        private bool IsRefrigerated()
        {
            return this.bay != null && this.bay.IsRefrigerated;
        }
    }
}

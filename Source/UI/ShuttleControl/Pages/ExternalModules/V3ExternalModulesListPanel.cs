using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.ExternalModules;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    internal sealed class V3ExternalModulesListPanel
    {
        private const float HeaderHeight = 34f;
        private const float RowHeight = 76f;
        private const float RowGap = 6f;

        private readonly V3ExternalModulesText text;
        private readonly V3ExternalModulesPanelDrawer panel;
        private readonly V3ExternalModulesSelection selection;

        internal V3ExternalModulesListPanel(
            V3ExternalModulesText text,
            V3ExternalModulesPanelDrawer panel,
            V3ExternalModulesSelection selection)
        {
            this.text = text;
            this.panel = panel;
            this.selection = selection;
        }

        internal void Draw(
            Rect rect,
            V3ExternalModulesPageModel model,
            V3ExternalModulesPageState state,
            ShuttlePageDrawContext context,
            System.Action onCommandCompleted)
        {
            this.panel.DrawPanelTitle(rect, this.text.Tr("CT_Shuttle_ExternalRuntime_InstalledHosts"));
            Rect innerRect = this.panel.GetPanelInnerRect(rect, HeaderHeight);
            IReadOnlyList<ExternalModuleUIReadModel> models =
                model != null ? model.ExternalModuleModels : null;
            if (models == null || models.Count == 0)
            {
                this.panel.DrawEmptyPanelMessage(
                    innerRect,
                    this.text.Tr("CT_Shuttle_ExternalRuntime_NoneInstalled"));
                return;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(0f, innerRect.width - 16f),
                Mathf.Max(innerRect.height, models.Count * (RowHeight + RowGap)));
            Widgets.BeginScrollView(innerRect, ref state.ModuleListScroll, viewRect);
            float y = 0f;
            for (int i = 0; i < models.Count; i++)
            {
                ExternalModuleUIReadModel externalModel = models[i];
                this.DrawRow(
                    new Rect(0f, y, viewRect.width, RowHeight),
                    externalModel,
                    model,
                    state,
                    context,
                    onCommandCompleted);
                y += RowHeight + RowGap;
            }

            Widgets.EndScrollView();
        }

        private void DrawRow(
            Rect rect,
            ExternalModuleUIReadModel externalModel,
            V3ExternalModulesPageModel pageModel,
            V3ExternalModulesPageState state,
            ShuttlePageDrawContext context,
            System.Action onCommandCompleted)
        {
            bool selected = this.selection.IsSelected(state, externalModel);
            Color color = this.text.GetStatusColor(externalModel);
            this.panel.DrawCardBackground(rect, selected, externalModel != null && !externalModel.RuntimeEnabled, color);
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            Rect toggleRect = new Rect(rect.xMax - 78f, rect.y + 8f, 68f, 22f);
            Rect detailsRect = new Rect(rect.xMax - 78f, rect.y + 38f, 68f, 22f);
            Event current = Event.current;
            bool overButton = current != null &&
                (toggleRect.Contains(current.mousePosition) ||
                    detailsRect.Contains(current.mousePosition));
            if (!overButton && Widgets.ButtonInvisible(rect))
            {
                this.selection.Select(state, externalModel);
            }

            this.DrawRowText(rect, externalModel, pageModel, context, color);
            this.panel.DrawStatusDot(
                new Rect(rect.xMax - 92f, rect.y + 10f, 10f, 10f),
                color);
            this.DrawToggleButton(toggleRect, externalModel, pageModel, context, onCommandCompleted);
            this.DrawDetailsButton(detailsRect, externalModel, pageModel, state, onCommandCompleted);
        }

        private void DrawRowText(
            Rect rect,
            ExternalModuleUIReadModel externalModel,
            V3ExternalModulesPageModel pageModel,
            ShuttlePageDrawContext context,
            Color color)
        {
            Rect iconRect = new Rect(rect.x + 8f, rect.y + 14f, 38f, 38f);
            this.DrawModuleIcon(iconRect, externalModel, context);

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                new Rect(iconRect.xMax + 8f, rect.y + 6f, rect.width - 158f, 21f),
                this.text.FitLabelText(
                    externalModel != null ? externalModel.Label : null,
                    rect.width - 158f));
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            string segment = this.GetSegmentLabel(pageModel, externalModel);
            ShuttleUILayout.SafeLabel(
                new Rect(iconRect.xMax + 8f, rect.y + 28f, rect.width - 158f, 18f),
                this.text.FitLabelText(segment, rect.width - 158f));
            string metric = externalModel != null
                ? this.text.Tr(
                    "CT_Shuttle_ExternalRuntime_HostMetric",
                    this.text.FormatRuntimeLabel(externalModel.RuntimeSystemKey),
                    this.text.FormatMs(externalModel.AverageTickCostMs))
                : "-";
            ShuttleUILayout.SafeLabel(
                new Rect(iconRect.xMax + 8f, rect.y + 48f, rect.width - 158f, 18f),
                this.text.FitLabelText(metric, rect.width - 158f));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawToggleButton(
            Rect rect,
            ExternalModuleUIReadModel externalModel,
            V3ExternalModulesPageModel pageModel,
            ShuttlePageDrawContext context,
            System.Action onCommandCompleted)
        {
            if (externalModel == null || pageModel == null || pageModel.RuntimeEnablementActions == null)
            {
                return;
            }

            bool targetEnabled = !externalModel.RuntimeEnabled;
            string disabledReason =
                pageModel.RuntimeEnablementActions.GetRuntimeEnablementDisabledReason(
                    externalModel,
                    targetEnabled);
            bool enabled = string.IsNullOrEmpty(disabledReason);
            string label = targetEnabled
                ? this.text.Tr("CT_Shuttle_ExternalModule_EnableModule")
                : this.text.Tr("CT_Shuttle_ExternalModule_DisableModule");
            if (this.panel.DrawButton(rect, label, enabled, targetEnabled ? V3ExternalModulesText.GreenColor : V3ExternalModulesText.RedColor, disabledReason))
            {
                pageModel.RuntimeEnablementActions.SetRuntimeEnabled(
                    externalModel,
                    targetEnabled,
                    onCommandCompleted);
            }
        }

        private void DrawDetailsButton(
            Rect rect,
            ExternalModuleUIReadModel externalModel,
            V3ExternalModulesPageModel pageModel,
            V3ExternalModulesPageState state,
            System.Action onCommandCompleted)
        {
            IShuttleExternalModuleDetailsUIActions detailsActions =
                pageModel != null ? pageModel.ExternalModuleDetailsActions : null;
            string disabledReason = detailsActions != null
                ? detailsActions.GetOpenDetailsDisabledReason(externalModel)
                : this.text.Tr("CT_Shuttle_ExternalRuntime_PanelStateUnavailable");
            bool canOpen = string.IsNullOrEmpty(disabledReason);
            if (this.panel.DrawButton(
                rect,
                this.text.Tr("CT_Shuttle_ExternalModule_OpenDetails"),
                canOpen,
                V3ExternalModulesText.BlueColor,
                canOpen ? null : disabledReason))
            {
                this.selection.Select(state, externalModel);
                detailsActions.OpenDetailsDialog(externalModel, onCommandCompleted);
            }
        }

        private void DrawModuleIcon(
            Rect rect,
            ExternalModuleUIReadModel externalModel,
            ShuttlePageDrawContext context)
        {
            Texture2D icon = this.ResolveModuleIcon(externalModel, context);

            Widgets.DrawBoxSolid(rect, ShuttleUIStyle.IconFallbackBackgroundColor);
            ShuttleUILayout.DrawRectBorder(rect, ShuttleUIStyle.SubtleBorderColor, ShuttleUIStyle.ThinBorder);
            ShuttleUILayout.DrawIconOrFallback(rect.ContractedBy(4f), icon, "EX", 0.78f);
        }

        private Texture2D ResolveModuleIcon(
            ExternalModuleUIReadModel externalModel,
            ShuttlePageDrawContext context)
        {
            if (context == null ||
                context.Services == null ||
                context.Services.Icons == null)
            {
                return null;
            }

            Texture2D icon =
                context.Services.Icons.GetIcon(this.text.GetModuleSpecificIconKey(externalModel));
            return icon != null
                ? icon
                : context.Services.Icons.GetIcon(this.text.GetFallbackIconKey(externalModel));
        }

        private string GetSegmentLabel(
            V3ExternalModulesPageModel pageModel,
            ExternalModuleUIReadModel externalModel)
        {
            string label;
            return pageModel != null &&
                pageModel.SegmentLabelsByModuleId != null &&
                externalModel != null &&
                pageModel.SegmentLabelsByModuleId.TryGetValue(externalModel.ModuleInstanceID, out label)
                    ? this.text.ValueOrDash(label)
                    : "-";
        }
    }
}

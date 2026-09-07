using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.ExternalModules;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    internal sealed class V3ExternalModulesDetailPanel
    {
        private const float HeaderHeight = 34f;
        private const float DetailLineHeight = 23f;

        private readonly V3ExternalModulesText text;
        private readonly V3ExternalModulesPanelDrawer panel;

        internal V3ExternalModulesDetailPanel(
            V3ExternalModulesText text,
            V3ExternalModulesPanelDrawer panel)
        {
            this.text = text;
            this.panel = panel;
        }

        internal void Draw(
            Rect rect,
            V3ExternalModulesPageModel model,
            V3ExternalModulesPageState state,
            ShuttlePageDrawContext context,
            ExternalModuleUIReadModel selected,
            System.Action onCommandCompleted)
        {
            this.panel.DrawPanelTitle(rect, this.text.Tr("CT_Shuttle_ExternalRuntime_ModuleHost"));
            Rect innerRect = this.panel.GetPanelInnerRect(rect, HeaderHeight);
            if (selected == null)
            {
                this.panel.DrawEmptyPanelMessage(
                    innerRect,
                    this.text.Tr("CT_Shuttle_ExternalRuntime_SelectRuntime"));
                return;
            }

            Rect cardRect = new Rect(innerRect.x, innerRect.y, innerRect.width, Mathf.Min(330f, innerRect.height));
            this.DrawSelectedCard(cardRect, model, context, selected, onCommandCompleted);
        }

        private void DrawSelectedCard(
            Rect rect,
            V3ExternalModulesPageModel pageModel,
            ShuttlePageDrawContext context,
            ExternalModuleUIReadModel selected,
            System.Action onCommandCompleted)
        {
            Color color = this.text.GetStatusColor(selected);
            this.panel.DrawCardBackground(
                rect,
                false,
                !selected.RuntimeEnabled,
                color,
                ShuttleUIStyle.RightBottomCardColor);
            Rect iconRect = new Rect(rect.x + 12f, rect.y + 10f, 44f, 44f);
            this.DrawModuleIcon(iconRect, selected, context);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                new Rect(iconRect.xMax + 10f, rect.y + 10f, rect.width - 76f, 22f),
                this.text.FitLabelText(selected.Label, rect.width - 76f));
            this.panel.DrawStatusBadge(
                new Rect(iconRect.xMax + 10f, rect.y + 35f, Mathf.Min(152f, rect.width - 76f), 18f),
                this.text.ValueOrDash(selected.StatusText),
                color);

            float y = iconRect.yMax + 10f;
            this.DrawDetailLine(rect, ref y, this.text.Tr("CT_Shuttle_ExternalRuntime_Runtime"), this.text.FormatRuntimeLabel(selected.RuntimeSystemKey));
            this.DrawDetailLine(rect, ref y, this.text.Tr("CT_Shuttle_ExternalRuntime_Segment"), this.GetSegmentLabel(pageModel, selected));
            this.DrawDetailLine(rect, ref y, this.text.Tr("CT_Shuttle_ExternalRuntime_Instance"), this.text.ValueOrDash(selected.ModuleInstanceID));
            this.DrawDetailLine(rect, ref y, this.text.Tr("CT_Shuttle_ExternalRuntime_TickAvg"), this.text.FormatMs(selected.AverageTickCostMs));
            this.DrawDetailLine(rect, ref y, this.text.Tr("CT_Shuttle_ExternalRuntime_LifePeak"), this.text.FormatMs(selected.PeakTickCostMs));
            this.DrawDetailLine(rect, ref y, this.text.Tr("CT_Shuttle_ExternalRuntime_DemandW"), this.text.FormatOne(selected.LastKnownPowerDemandWatts));
            this.DrawDetailLine(rect, ref y, this.text.Tr("CT_Shuttle_ExternalRuntime_EnergyWd"), this.text.FormatOne(selected.TotalStoredEnergyConsumedWd));
            if (!string.IsNullOrEmpty(selected.DisabledReason))
            {
                this.DrawDetailLine(rect, ref y, this.text.Tr("CT_Shuttle_ExternalRuntime_Reason"), selected.DisabledReason);
            }

            this.DrawActions(rect, pageModel, context, selected, onCommandCompleted);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        private void DrawActions(
            Rect rect,
            V3ExternalModulesPageModel pageModel,
            ShuttlePageDrawContext context,
            ExternalModuleUIReadModel selected,
            System.Action onCommandCompleted)
        {
            float gap = 6f;
            float buttonWidth = (rect.width - 24f - gap) * 0.5f;
            Rect toggleRect = new Rect(rect.x + 12f, rect.yMax - 36f, buttonWidth, 26f);
            Rect detailRect = new Rect(toggleRect.xMax + gap, toggleRect.y, buttonWidth, 26f);
            this.DrawToggleButton(toggleRect, pageModel, selected, onCommandCompleted);

            IShuttleExternalModuleDetailsUIActions detailsActions =
                pageModel != null ? pageModel.ExternalModuleDetailsActions : null;
            string detailsDisabledReason = detailsActions != null
                ? detailsActions.GetOpenDetailsDisabledReason(selected)
                : this.text.Tr("CT_Shuttle_ExternalRuntime_PanelStateUnavailable");
            bool canOpen = string.IsNullOrEmpty(detailsDisabledReason);
            if (this.panel.DrawButton(
                detailRect,
                this.text.Tr("CT_Shuttle_ExternalRuntime_OpenPanel"),
                canOpen,
                V3ExternalModulesText.BlueColor,
                canOpen ? null : detailsDisabledReason))
            {
                detailsActions.OpenDetailsDialog(selected, onCommandCompleted);
            }
        }

        private void DrawToggleButton(
            Rect rect,
            V3ExternalModulesPageModel pageModel,
            ExternalModuleUIReadModel selected,
            System.Action onCommandCompleted)
        {
            if (pageModel == null || pageModel.RuntimeEnablementActions == null)
            {
                return;
            }

            bool targetEnabled = !selected.RuntimeEnabled;
            string disabledReason =
                pageModel.RuntimeEnablementActions.GetRuntimeEnablementDisabledReason(
                    selected,
                    targetEnabled);
            string label = targetEnabled
                ? this.text.Tr("CT_Shuttle_ExternalRuntime_EnableRuntime")
                : this.text.Tr("CT_Shuttle_ExternalRuntime_DisableRuntime");
            if (this.panel.DrawButton(
                rect,
                label,
                string.IsNullOrEmpty(disabledReason),
                targetEnabled ? V3ExternalModulesText.GreenColor : V3ExternalModulesText.RedColor,
                disabledReason))
            {
                pageModel.RuntimeEnablementActions.SetRuntimeEnabled(
                    selected,
                    targetEnabled,
                    onCommandCompleted);
            }
        }

        private void DrawDetailLine(Rect container, ref float y, string label, string value)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(container.x + 16f, y, 84f, 20f),
                this.text.FitLabelText(label, 84f));
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                new Rect(container.x + 104f, y, container.width - 120f, 20f),
                this.text.FitLabelText(value, container.width - 120f));
            y += DetailLineHeight;
        }

        private void DrawModuleIcon(
            Rect rect,
            ExternalModuleUIReadModel selected,
            ShuttlePageDrawContext context)
        {
            Texture2D icon = this.ResolveModuleIcon(selected, context);

            Widgets.DrawBoxSolid(rect, ShuttleUIStyle.IconFallbackBackgroundColor);
            ShuttleUILayout.DrawRectBorder(rect, ShuttleUIStyle.SubtleBorderColor, ShuttleUIStyle.ThinBorder);
            ShuttleUILayout.DrawIconOrFallback(rect.ContractedBy(4f), icon, "EX", 0.84f);
        }

        private Texture2D ResolveModuleIcon(
            ExternalModuleUIReadModel selected,
            ShuttlePageDrawContext context)
        {
            if (context == null ||
                context.Services == null ||
                context.Services.Icons == null)
            {
                return null;
            }

            Texture2D icon =
                context.Services.Icons.GetIcon(this.text.GetModuleSpecificIconKey(selected));
            return icon != null
                ? icon
                : context.Services.Icons.GetIcon(this.text.GetFallbackIconKey(selected));
        }

        private string GetSegmentLabel(
            V3ExternalModulesPageModel pageModel,
            ExternalModuleUIReadModel selected)
        {
            string label;
            return pageModel != null &&
                pageModel.SegmentLabelsByModuleId != null &&
                selected != null &&
                pageModel.SegmentLabelsByModuleId.TryGetValue(selected.ModuleInstanceID, out label)
                    ? this.text.ValueOrDash(label)
                    : "-";
        }
    }
}

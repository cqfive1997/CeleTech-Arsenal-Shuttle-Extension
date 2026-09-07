using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    internal sealed class V3ExternalModuleDetailsSummaryDrawer
    {
        private const float DetailLineHeight = 23f;

        private readonly V3ExternalModulesText text;
        private readonly V3ExternalModulesPanelDrawer panel;

        internal V3ExternalModuleDetailsSummaryDrawer(
            V3ExternalModulesText text,
            V3ExternalModulesPanelDrawer panel)
        {
            this.text = text;
            this.panel = panel;
        }

        internal void DrawSummary(Rect rect, ExternalModuleUIReadModel selected)
        {
            this.panel.DrawCardBackground(
                rect,
                false,
                selected != null && !selected.RuntimeEnabled,
                selected != null ? this.text.GetStatusColor(selected) : ShuttleUIStyle.MutedTextColor);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 22f),
                this.text.FitLabelText(
                    selected != null ? selected.Label : null,
                    rect.width - 24f));

            float y = rect.y + 38f;
            this.DrawDetailLine(
                rect,
                ref y,
                this.text.Tr("CT_Shuttle_ExternalModule_ThirdPartyModule"),
                this.GetTitle(selected));
            this.DrawDetailLine(
                rect,
                ref y,
                this.text.Tr("CT_Shuttle_ExternalRuntime_Status"),
                selected != null ? this.text.ValueOrDash(selected.StatusText) : "-");
            this.DrawDetailLine(
                rect,
                ref y,
                this.text.Tr("CT_Shuttle_ExternalRuntime_DemandW"),
                selected != null ? this.text.FormatOne(selected.LastKnownPowerDemandWatts) : "-");
        }

        internal void DrawDevDetails(Rect rect, ExternalModuleUIReadModel selected)
        {
            this.panel.DrawCardBackground(rect, false, false, V3ExternalModulesText.AccentColor);
            float y = rect.y + 8f;
            this.DrawDetailLine(rect, ref y, this.text.Tr("CT_Shuttle_ExternalRuntime_Module"), selected != null ? selected.Label : "-");
            this.DrawDetailLine(rect, ref y, this.text.Tr("CT_Shuttle_ExternalRuntime_Instance"), selected != null ? selected.ModuleInstanceID : "-");
            this.DrawDetailLine(rect, ref y, this.text.Tr("CT_Shuttle_ExternalRuntime_Def"), selected != null ? selected.ModuleDefName : "-");
            this.DrawDetailLine(rect, ref y, this.text.Tr("CT_Shuttle_ExternalRuntime_Type"), selected != null ? selected.ModuleTypeID : "-");
            this.DrawDetailLine(rect, ref y, this.text.Tr("CT_Shuttle_ExternalRuntime_Runtime"), selected != null ? selected.RuntimeSystemKey : "-");
            this.DrawDetailLine(
                rect,
                ref y,
                this.text.Tr("CT_Shuttle_ExternalRuntime_Registered"),
                selected != null && selected.RuntimeRegistered
                    ? this.text.Tr("CT_Shuttle_Common_Yes")
                    : this.text.Tr("CT_Shuttle_Common_No"));
            this.DrawDetailLine(
                rect,
                ref y,
                this.text.Tr("CT_Shuttle_ExternalRuntime_State"),
                selected != null && selected.RuntimeStateExists
                    ? this.text.Tr("CT_Shuttle_ExternalRuntime_State_Present")
                    : this.text.Tr("CT_Shuttle_ExternalRuntime_State_Missing"));
        }

        internal string GetTitle(ExternalModuleUIReadModel selected)
        {
            if (selected != null && !string.IsNullOrEmpty(selected.Label))
            {
                return selected.Label;
            }

            return selected != null && !string.IsNullOrEmpty(selected.RuntimeSystemKey)
                ? this.text.FormatRuntimeLabel(selected.RuntimeSystemKey)
                : this.text.Tr("CT_Shuttle_ExternalModule_Details");
        }

        private void DrawDetailLine(Rect container, ref float y, string label, string value)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(container.x + 12f, y, 116f, 20f),
                this.text.FitLabelText(label, 116f));
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                new Rect(container.x + 132f, y, container.width - 144f, 20f),
                this.text.FitLabelText(value, container.width - 144f));
            y += DetailLineHeight;
        }
    }
}

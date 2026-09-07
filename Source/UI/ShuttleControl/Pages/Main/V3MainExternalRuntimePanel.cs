using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainExternalRuntimePanel
    {
        private readonly V3MainText text;

        internal V3MainExternalRuntimePanel(V3MainText text)
        {
            this.text = text;
        }

        internal void DrawSummaryCard(
            Rect rect,
            V3MainPageModel model,
            V3MainPageState state,
            ShuttlePageDrawContext context,
            V3MainPanelDrawer panel)
        {
            int total;
            int enabled;
            int withPanels;
            this.CountExternalModules(model, out total, out enabled, out withPanels);
            string value = enabled.ToString() + "/" + total.ToString();
            string tooltip = this.text.Tr("CT_Shuttle_ExternalRuntime_Tooltip_Count", total) +
                "\n" +
                this.text.Tr("CT_Shuttle_ExternalRuntime_Tooltip_Panels", withPanels);
            panel.DrawSummaryCard(
                rect,
                this.text.Tr("CT_Shuttle_Page_ExternalModules"),
                value,
                enabled > 0 ? V3MainText.GreenColor : V3MainText.AccentColor,
                tooltip);
            this.HandleOpenDetails(rect, model, state, context, total);
        }

        private void CountExternalModules(
            V3MainPageModel model,
            out int total,
            out int enabled,
            out int withPanels)
        {
            total = 0;
            enabled = 0;
            withPanels = 0;
            if (model == null || model.ExternalModuleModels == null)
            {
                return;
            }

            for (int i = 0; i < model.ExternalModuleModels.Count; i++)
            {
                ExternalModuleUIReadModel externalModel = model.ExternalModuleModels[i];
                if (externalModel == null)
                {
                    continue;
                }

                total++;
                if (externalModel.RuntimeEnabled)
                {
                    enabled++;
                }

                if (externalModel.HasExternalPanel)
                {
                    withPanels++;
                }
            }
        }

        private void HandleOpenDetails(
            Rect rect,
            V3MainPageModel model,
            V3MainPageState state,
            ShuttlePageDrawContext context,
            int total)
        {
            if (total <= 0 ||
                context == null ||
                context.MainPageContext == null ||
                context.MainPageContext.OpenExternalRuntime == null)
            {
                return;
            }

            ExternalModuleUIReadModel target = this.FindExternalModuleTarget(model);
            if (target == null)
            {
                return;
            }

            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            if (Widgets.ButtonInvisible(rect))
            {
                state.SelectedExternalModuleInstanceID = target.ModuleInstanceID;
                state.SelectedExternalRuntimeSystemKey = target.RuntimeSystemKey;
                context.MainPageContext.OpenExternalRuntime(
                    target.ModuleInstanceID,
                    target.RuntimeSystemKey);
            }
        }

        private ExternalModuleUIReadModel FindExternalModuleTarget(V3MainPageModel model)
        {
            if (model == null || model.ExternalModuleModels == null)
            {
                return null;
            }

            for (int i = 0; i < model.ExternalModuleModels.Count; i++)
            {
                ExternalModuleUIReadModel externalModel = model.ExternalModuleModels[i];
                if (externalModel != null &&
                    externalModel.HasExternalPanel &&
                    !string.IsNullOrEmpty(externalModel.ModuleInstanceID) &&
                    !string.IsNullOrEmpty(externalModel.RuntimeSystemKey))
                {
                    return externalModel;
                }
            }

            for (int i = 0; i < model.ExternalModuleModels.Count; i++)
            {
                ExternalModuleUIReadModel externalModel = model.ExternalModuleModels[i];
                if (externalModel != null &&
                    !string.IsNullOrEmpty(externalModel.ModuleInstanceID) &&
                    !string.IsNullOrEmpty(externalModel.RuntimeSystemKey))
                {
                    return externalModel;
                }
            }

            return null;
        }
    }
}

using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    internal sealed class V3ExternalModulesProviderPanelHost
    {
        private const float HeaderHeight = 42f;
        private const float MinPanelHeight = 160f;

        private readonly V3ExternalModulesText text;
        private readonly V3ExternalModulesPanelDrawer panel;
        private readonly V3ExternalModulesProviderCommandStrip commandStrip;

        internal V3ExternalModulesProviderPanelHost(
            V3ExternalModulesText text,
            V3ExternalModulesPanelDrawer panel)
        {
            this.text = text;
            this.panel = panel;
            this.commandStrip =
                new V3ExternalModulesProviderCommandStrip(text, panel);
        }

        internal void Draw(
            Rect rect,
            ExternalModuleUIReadModel selected,
            V3ExternalModulesPageState state,
            ShuttlePageDrawContext context,
            System.Action onCommandCompleted)
        {
            this.panel.DrawPanelTitle(rect, this.text.Tr("CT_Shuttle_ExternalRuntime_ReadOnlyPanel"));
            Rect contentRect = new Rect(
                rect.x + 12f,
                rect.y + HeaderHeight,
                Mathf.Max(0f, rect.width - 24f),
                Mathf.Max(0f, rect.height - HeaderHeight - 10f));

            IV3ExternalModulePanelModelProvider modelProvider =
                context != null && context.ExternalModulesPageContext != null
                    ? context.ExternalModulesPageContext.PanelModelProvider
                    : null;
            V3ExternalModulesProviderPanelModel panelModel = modelProvider != null
                ? modelProvider.BuildProviderPanel(
                    selected,
                    contentRect.width,
                    MinPanelHeight,
                    context != null && context.ExternalModulesPageContext != null
                        ? context.ExternalModulesPageContext.PanelCommandActions
                        : null,
                    onCommandCompleted)
                : this.BuildUnavailablePanel(MinPanelHeight);
            if (panelModel.Registration == null || panelModel.Context == null)
            {
                this.panel.DrawEmptyPanelMessage(contentRect, panelModel.Message);
                return;
            }

            List<ShuttleExternalPanelCommandContribution> commands = panelModel.Commands;
            float stripHeight = this.commandStrip.GetHeight(commands, contentRect.width);
            Rect panelContentRect = contentRect;
            Rect commandStripRect = Rect.zero;
            if (stripHeight > 0f)
            {
                commandStripRect = new Rect(
                    contentRect.x,
                    contentRect.yMax - stripHeight,
                    contentRect.width,
                    stripHeight);
                panelContentRect = new Rect(
                    contentRect.x,
                    contentRect.y,
                    contentRect.width,
                    Mathf.Max(40f, contentRect.height - stripHeight - 6f));
            }

            this.DrawProviderPanelContent(panelContentRect, panelModel, state);
            this.commandStrip.Draw(
                commandStripRect,
                selected,
                commands,
                context != null && context.ExternalModulesPageContext != null
                    ? context.ExternalModulesPageContext.PanelCommandActions
                    : null,
                onCommandCompleted);
        }

        private V3ExternalModulesProviderPanelModel BuildUnavailablePanel(float fallbackHeight)
        {
            V3ExternalModulesProviderPanelModel model =
                new V3ExternalModulesProviderPanelModel();
            model.PanelHeight = fallbackHeight;
            model.Message = this.text.Tr("CT_Shuttle_ExternalRuntime_PanelStateUnavailable");
            return model;
        }

        private void DrawProviderPanelContent(
            Rect rect,
            V3ExternalModulesProviderPanelModel panelModel,
            V3ExternalModulesPageState state)
        {
            float viewWidth = Mathf.Max(0f, rect.width - 18f);
            Rect viewRect = new Rect(
                0f,
                0f,
                viewWidth,
                Mathf.Max(rect.height, panelModel.PanelHeight));
            Vector2 scroll = state != null ? state.PanelScroll : Vector2.zero;
            Widgets.BeginScrollView(rect, ref scroll, viewRect);
            ExternalModulePanelGuard.SafeDrawPanel(
                panelModel.Registration,
                new Rect(0f, 0f, viewRect.width, panelModel.PanelHeight),
                panelModel.Context);
            Widgets.EndScrollView();
            if (state != null)
            {
                state.PanelScroll = scroll;
            }
        }
    }
}

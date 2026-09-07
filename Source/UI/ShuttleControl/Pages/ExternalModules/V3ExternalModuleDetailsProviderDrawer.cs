using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.ExternalModules;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.External;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    internal sealed class V3ExternalModuleDetailsProviderDrawer
    {
        private readonly V3ExternalModulesText text;
        private readonly V3ExternalModulesPanelDrawer panel;
        private readonly V3ExternalModulesProviderCommandStrip commandStrip;

        internal V3ExternalModuleDetailsProviderDrawer(
            V3ExternalModulesText text,
            V3ExternalModulesPanelDrawer panel)
        {
            this.text = text;
            this.panel = panel;
            this.commandStrip =
                new V3ExternalModulesProviderCommandStrip(text, panel);
        }

        internal float GetCommandStripHeight(
            V3ExternalModulesProviderPanelModel model,
            float width)
        {
            return this.commandStrip.GetHeight(
                model != null ? model.Commands : null,
                width);
        }

        internal void DrawProviderPanel(
            Rect rect,
            V3ExternalModulesProviderPanelModel model,
            ref Vector2 scroll)
        {
            ShuttleUILayout.DrawCardBackground(
                rect,
                false,
                false,
                ShuttleUIStyle.RightBottomCardColor);
            Rect innerRect = new Rect(
                rect.x + 8f,
                rect.y + 8f,
                Mathf.Max(0f, rect.width - 16f),
                Mathf.Max(0f, rect.height - 16f));
            if (model == null || model.Registration == null || model.Context == null)
            {
                this.panel.DrawEmptyPanelMessage(
                    innerRect,
                    model != null ? model.Message : this.text.Tr("CT_Shuttle_ExternalRuntime_PanelStateUnavailable"));
                return;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(0f, innerRect.width - 18f),
                Mathf.Max(innerRect.height, model.PanelHeight));
            Widgets.BeginScrollView(innerRect, ref scroll, viewRect);
            ExternalModulePanelGuard.SafeDrawPanel(
                model.Registration,
                new Rect(0f, 0f, viewRect.width, model.PanelHeight),
                model.Context);
            Widgets.EndScrollView();
        }

        internal void DrawCommandStrip(
            Rect rect,
            ExternalModuleUIReadModel selected,
            V3ExternalModulesProviderPanelModel model,
            IShuttleExternalPanelCommandUIActions actions,
            System.Action onCommandCompleted)
        {
            this.commandStrip.Draw(
                rect,
                selected,
                model != null ? model.Commands : null,
                actions,
                onCommandCompleted);
        }
    }
}

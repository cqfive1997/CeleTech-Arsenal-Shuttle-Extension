using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Diagnostics;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    /// <summary>
    /// Native V3 ExternalModules page. Provider-owned panels are hosted through
    /// neutral provider and command surfaces.
    /// </summary>
    internal sealed class V3ExternalModulesPage : IShuttleIntegratedHeaderPageV3
    {
        private const float ListPanelWidth = 350f;
        private const float DetailPanelWidth = ShuttleUIStyle.PageRightPanelWidth;

        private readonly V3ExternalModulesText text = new V3ExternalModulesText();
        private readonly V3ExternalModulesPageModel model = new V3ExternalModulesPageModel();
        private readonly V3ExternalModulesPageModelBuilder modelBuilder =
            new V3ExternalModulesPageModelBuilder();
        private readonly V3ExternalModulesPageState fallbackState =
            new V3ExternalModulesPageState();
        private readonly V3ExternalModulesSelection selection =
            new V3ExternalModulesSelection();
        private readonly V3ExternalModulesProviderPanelHost providerPanelHost;
        private readonly V3SharedMessagePanel messagePanel =
            new V3SharedMessagePanel();
        private readonly V3ExternalModulesHeader header;
        private readonly V3ExternalModulesPanelDrawer panel;
        private readonly V3ExternalModulesListPanel listPanel;
        private readonly V3ExternalModulesDetailPanel detailPanel;
        private readonly Action commandCompletedHandler;
        private ShuttlePageDrawContext currentContext;
        private Action markExternalModuleCommandCompleted;
        private Action markDirty;

        internal V3ExternalModulesPage()
        {
            this.panel = new V3ExternalModulesPanelDrawer(this.text);
            this.header = new V3ExternalModulesHeader(this.text);
            this.listPanel =
                new V3ExternalModulesListPanel(this.text, this.panel, this.selection);
            this.providerPanelHost =
                new V3ExternalModulesProviderPanelHost(this.text, this.panel);
            this.detailPanel =
                new V3ExternalModulesDetailPanel(
                    this.text,
                    this.panel);
            this.commandCompletedHandler = this.MarkExternalModuleCommandCompleted;
        }

        public ShuttleControlPageId Page
        {
            get { return ShuttleControlPageId.ExternalModules; }
        }

        public void OnEnter(ShuttlePageDrawContext context)
        {
        }

        public void OnExit(ShuttlePageDrawContext context)
        {
            this.currentContext = null;
        }

        public void Draw(Rect rect, ShuttlePageDrawContext context)
        {
            if (context == null)
            {
                return;
            }

            this.currentContext = context;
            V3ExternalModulesPageContext pageContext =
                context.ExternalModulesPageContext;
            this.markExternalModuleCommandCompleted =
                pageContext != null ? pageContext.MarkCommandCompleted : null;
            this.markDirty = pageContext != null ? pageContext.MarkDirty : null;
            V3ExternalModulesPageState state = this.GetPageState(context);
            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.ExternalModulesPageProjection))
            {
                this.modelBuilder.Fill(this.model, context.ExternalModulesInputs, context);
            }

            ExternalModuleUIReadModel selected =
                this.selection.EnsureSelected(state, this.model.ExternalModuleModels);

            ShuttlePageFrameLayout layout = ShuttlePageFrameLayout.HeaderBody(rect);
            this.header.Draw(layout.HeaderRect, this.model, context);
            this.DrawBody(layout.BodyRect, context, state, selected);
        }

        private V3ExternalModulesPageState GetPageState(ShuttlePageDrawContext context)
        {
            if (context != null && context.State != null && context.State.ExternalModules != null)
            {
                return context.State.ExternalModules;
            }

            return this.fallbackState;
        }

        private void DrawBody(
            Rect rect,
            ShuttlePageDrawContext context,
            V3ExternalModulesPageState state,
            ExternalModuleUIReadModel selected)
        {
            float gap = ShuttleUIStyle.Gap;
            float rightX = rect.xMax - DetailPanelWidth;
            float leftCenterWidth = rect.width - DetailPanelWidth - gap;
            ShuttlePageBottomMessageLayout messageLayout =
                ShuttlePageBottomMessageLayout.FromBodyHeight(rect.height, gap);
            float upperHeight = messageLayout.ContentHeight;

            Rect listRect = new Rect(rect.x, rect.y, ListPanelWidth, upperHeight);
            Rect providerRect = new Rect(
                listRect.xMax + gap,
                rect.y,
                Mathf.Max(0f, leftCenterWidth - ListPanelWidth - gap),
                upperHeight);
            Rect messageRect = new Rect(
                rect.x,
                listRect.yMax + gap,
                leftCenterWidth,
                messageLayout.MessageHeight);
            Rect detailRect = new Rect(
                rightX,
                rect.y,
                DetailPanelWidth,
                rect.height);

            this.listPanel.Draw(
                listRect,
                this.model,
                state,
                context,
                this.commandCompletedHandler);
            this.providerPanelHost.Draw(
                providerRect,
                selected,
                state,
                context,
                this.commandCompletedHandler);
            this.detailPanel.Draw(
                detailRect,
                this.model,
                state,
                context,
                selected,
                this.commandCompletedHandler);
            this.messagePanel.Draw(
                messageRect,
                context,
                state.MessagePanelState,
                ref state.MessageScroll);
        }

        private void MarkExternalModuleCommandCompleted()
        {
            Action commandCompleted =
                this.currentContext != null &&
                this.currentContext.ExternalModulesPageContext != null
                ? this.currentContext.ExternalModulesPageContext.MarkCommandCompleted
                : this.markExternalModuleCommandCompleted;
            if (commandCompleted != null)
            {
                commandCompleted();
                return;
            }

            Action dirty =
                this.currentContext != null &&
                this.currentContext.ExternalModulesPageContext != null
                ? this.currentContext.ExternalModulesPageContext.MarkDirty
                : this.markDirty;
            if (dirty != null)
            {
                dirty();
            }
        }
    }
}

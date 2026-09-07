using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.ExternalModules;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    internal sealed class Dialog_ShuttleExternalModuleDetailsV3 : Window
    {
        private const float TargetWidth = 720f;
        private const float TargetHeight = 620f;
        private const float SummaryHeight = 112f;
        private const float MinPanelHeight = 220f;
        private const float DevDetailsHeight = 170f;
        private const float Gap = 8f;

        private readonly ExternalModuleUIReadModel selected;
        private readonly IV3ExternalModulePanelModelProvider providerModelProvider;
        private readonly IShuttleExternalPanelCommandUIActions panelCommandActions;
        private readonly System.Action onCommandCompleted;
        private readonly V3ExternalModulesText text = new V3ExternalModulesText();
        private readonly V3ExternalModulesPanelDrawer panel;
        private readonly V3ExternalModuleDetailsSummaryDrawer summaryDrawer;
        private readonly V3ExternalModuleDetailsProviderDrawer providerDrawer;

        private Vector2 scrollPosition;
        private Vector2 providerScroll;

        internal Dialog_ShuttleExternalModuleDetailsV3(
            ExternalModuleUIReadModel selected,
            IV3ExternalModulePanelModelProvider providerModelProvider,
            IShuttleExternalPanelCommandUIActions panelCommandActions,
            System.Action onCommandCompleted)
        {
            this.selected = selected;
            this.providerModelProvider = providerModelProvider;
            this.panelCommandActions = panelCommandActions;
            this.onCommandCompleted = onCommandCompleted;
            this.panel = new V3ExternalModulesPanelDrawer(this.text);
            this.summaryDrawer =
                new V3ExternalModuleDetailsSummaryDrawer(this.text, this.panel);
            this.providerDrawer =
                new V3ExternalModuleDetailsProviderDrawer(this.text, this.panel);
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = false;
        }

        public override Vector2 InitialSize
        {
            get
            {
                float safeWidth = Mathf.Max(520f, Verse.UI.screenWidth - 80f);
                float safeHeight = Mathf.Max(460f, Verse.UI.screenHeight - 80f);
                return new Vector2(
                    Mathf.Min(TargetWidth, safeWidth),
                    Mathf.Min(TargetHeight, safeHeight));
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            this.DrawTitle(new Rect(inRect.x, inRect.y, inRect.width, 32f));
            Rect bodyRect = new Rect(
                inRect.x,
                inRect.y + 40f,
                inRect.width,
                Mathf.Max(0f, inRect.height - 40f));
            V3ExternalModulesProviderPanelModel providerModel =
                this.providerModelProvider != null
                    ? this.providerModelProvider.BuildProviderPanel(
                        this.selected,
                        bodyRect.width,
                        MinPanelHeight,
                        this.panelCommandActions,
                        this.onCommandCompleted)
                    : this.BuildUnavailablePanel(MinPanelHeight);
            float commandStripHeight =
                this.providerDrawer.GetCommandStripHeight(providerModel, bodyRect.width);
            Rect scrollOutRect = new Rect(
                bodyRect.x,
                bodyRect.y,
                bodyRect.width,
                Mathf.Max(80f, bodyRect.height - commandStripHeight - (commandStripHeight > 0f ? Gap : 0f)));
            Rect commandRect = commandStripHeight > 0f
                ? new Rect(bodyRect.x, scrollOutRect.yMax + Gap, bodyRect.width, commandStripHeight)
                : Rect.zero;
            this.DrawScrollableBody(scrollOutRect, providerModel);
            if (commandStripHeight > 0f)
            {
                this.providerDrawer.DrawCommandStrip(
                    commandRect,
                    this.selected,
                    providerModel,
                    this.panelCommandActions,
                    this.onCommandCompleted);
            }
        }

        private void DrawTitle(Rect rect)
        {
            Text.Font = GameFont.Medium;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                rect,
                this.text.FitLabelText(this.summaryDrawer.GetTitle(this.selected), rect.width - 38f));
            Text.Font = GameFont.Small;
        }

        private void DrawScrollableBody(
            Rect outRect,
            V3ExternalModulesProviderPanelModel providerModel)
        {
            float viewWidth = Mathf.Max(0f, outRect.width - 18f);
            float contentHeight = this.EstimateContentHeight(providerModel);
            Rect viewRect = new Rect(0f, 0f, viewWidth, contentHeight);
            Widgets.BeginScrollView(outRect, ref this.scrollPosition, viewRect);
            float y = 0f;
            this.summaryDrawer.DrawSummary(
                new Rect(0f, y, viewRect.width, SummaryHeight),
                this.selected);
            y += SummaryHeight + Gap;
            this.providerDrawer.DrawProviderPanel(
                new Rect(0f, y, viewRect.width, providerModel.PanelHeight),
                providerModel,
                ref this.providerScroll);
            y += providerModel.PanelHeight + Gap;
            if (Prefs.DevMode)
            {
                this.summaryDrawer.DrawDevDetails(
                    new Rect(0f, y, viewRect.width, DevDetailsHeight),
                    this.selected);
            }

            Widgets.EndScrollView();
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        private float EstimateContentHeight(
            V3ExternalModulesProviderPanelModel providerModel)
        {
            float providerHeight = providerModel != null ? providerModel.PanelHeight : MinPanelHeight;
            float height = SummaryHeight + Gap + providerHeight + Gap;
            if (Prefs.DevMode)
            {
                height += DevDetailsHeight + Gap;
            }

            return Mathf.Max(height, 260f);
        }

        private V3ExternalModulesProviderPanelModel BuildUnavailablePanel(float fallbackHeight)
        {
            V3ExternalModulesProviderPanelModel model =
                new V3ExternalModulesProviderPanelModel();
            model.PanelHeight = fallbackHeight;
            model.Message = this.text.Tr("CT_Shuttle_ExternalRuntime_PanelStateUnavailable");
            return model;
        }
    }
}

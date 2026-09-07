using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    /// <summary>
    /// Native V3 Defense page. This class owns page state, layout, and drawing.
    /// </summary>
    internal sealed class V3DefensePage : IShuttleIntegratedHeaderPageV3
    {
        private const float RightPanelWidth = ShuttleUIStyle.PageRightPanelWidth;

        private readonly V3DefenseText text = new V3DefenseText();
        private readonly V3DefensePageModel model = new V3DefensePageModel();
        private readonly V3DefensePageModelBuilder modelBuilder =
            new V3DefensePageModelBuilder();
        private readonly V3DefensePageState fallbackState = new V3DefensePageState();
        private readonly V3SharedMessagePanel messagePanel =
            new V3SharedMessagePanel();
        private readonly V3DefenseHeader header;
        private readonly V3DefensePanelDrawer panel;
        private readonly V3DefenseSystemPanel systemPanel;
        private readonly V3DefenseWeaponGroupPanel weaponGroupPanel;
        private readonly V3DefenseShieldInfoPanel shieldInfoPanel;
        private readonly V3DefenseWeaponMapPanel weaponMapPanel;

        internal V3DefensePage()
        {
            this.panel = new V3DefensePanelDrawer(this.text);
            this.header = new V3DefenseHeader(this.text);
            this.systemPanel = new V3DefenseSystemPanel(this.text, this.panel);
            this.weaponGroupPanel = new V3DefenseWeaponGroupPanel(this.text, this.panel);
            this.shieldInfoPanel = new V3DefenseShieldInfoPanel(this.text, this.panel);
            this.weaponMapPanel = new V3DefenseWeaponMapPanel(this.text, this.panel);
        }

        public ShuttleControlPageId Page
        {
            get { return ShuttleControlPageId.Defense; }
        }

        public void OnEnter(ShuttlePageDrawContext context)
        {
        }

        public void OnExit(ShuttlePageDrawContext context)
        {
        }

        public void Draw(Rect rect, ShuttlePageDrawContext context)
        {
            if (context == null)
            {
                return;
            }

            V3DefensePageState state = this.GetPageState(context);
            this.modelBuilder.Fill(
                this.model,
                context.DefenseInputs,
                state.SelectedWeaponId);
            this.EnsureSelectedWeapon(this.model.DefenseModel, state);

            ShuttlePageFrameLayout layout = ShuttlePageFrameLayout.HeaderBody(rect);
            this.header.Draw(layout.HeaderRect, this.model, context);
            this.DrawBody(layout.BodyRect, this.model, context, state);
        }

        private void DrawBody(
            Rect rect,
            V3DefensePageModel pageModel,
            ShuttlePageDrawContext context,
            V3DefensePageState state)
        {
            float gap = ShuttleUIStyle.Gap;
            float rightX = rect.xMax - RightPanelWidth;
            float leftCenterWidth = rect.width - RightPanelWidth - gap;
            ShuttlePageBottomMessageLayout messageLayout =
                ShuttlePageBottomMessageLayout.FromBodyHeight(rect.height, gap);
            float upperHeight = messageLayout.ContentHeight;

            Rect weaponListRect = new Rect(rect.x, rect.y, leftCenterWidth, upperHeight);
            Rect messageRect = new Rect(
                rect.x,
                weaponListRect.yMax + gap,
                leftCenterWidth,
                messageLayout.MessageHeight);
            const float groupPanelHeight = 118f;
            const float shieldPanelHeight = 222f;
            Rect groupRect = new Rect(rightX, rect.y, RightPanelWidth, groupPanelHeight);
            Rect shieldRect = new Rect(
                rightX,
                groupRect.yMax + gap,
                RightPanelWidth,
                shieldPanelHeight);
            Rect mapRect = new Rect(
                rightX,
                shieldRect.yMax + gap,
                RightPanelWidth,
                Mathf.Max(
                    0f,
                    rect.yMax - shieldRect.yMax - gap));

            this.RegisterTutorialTargets(
                context,
                rect,
                weaponListRect,
                groupRect,
                shieldRect,
                mapRect,
                messageRect);

            this.systemPanel.DrawWeaponBrief(weaponListRect, pageModel, state, context);
            this.weaponGroupPanel.Draw(groupRect, pageModel, context);
            this.shieldInfoPanel.Draw(shieldRect, pageModel);
            this.weaponMapPanel.Draw(mapRect, pageModel, state, context);
            this.messagePanel.Draw(
                messageRect,
                context,
                state.MessagePanelState,
                ref state.MessageScroll);
        }

        private V3DefensePageState GetPageState(ShuttlePageDrawContext context)
        {
            if (context != null && context.State != null && context.State.Defense != null)
            {
                return context.State.Defense;
            }

            return this.fallbackState;
        }

        private void EnsureSelectedWeapon(
            V3DefensePageReadModel defenseModel,
            V3DefensePageState state)
        {
            if (defenseModel == null || state == null)
            {
                return;
            }

            if (defenseModel.SelectedWeapon != null)
            {
                state.SelectedWeaponId = defenseModel.SelectedWeapon.Id;
            }
            else if (defenseModel.Weapons == null || defenseModel.Weapons.Count == 0)
            {
                state.SelectedWeaponId = null;
            }
        }

        private void RegisterTutorialTargets(
            ShuttlePageDrawContext context,
            Rect overviewRect,
            Rect weaponListRect,
            Rect groupRect,
            Rect shieldRect,
            Rect weaponMapRect,
            Rect messageRect)
        {
            IShuttleTutorialTargetService tutorialTargets = GetTutorialTargets(context);
            if (tutorialTargets == null)
            {
                return;
            }

            tutorialTargets.Register(
                ShuttleTutorialTargetIds.DefenseOverviewPanel,
                overviewRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.DefenseWeaponListPanel, weaponListRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.DefenseFireControlPanel, groupRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.DefenseShieldPanel, shieldRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.DefenseSelectedWeaponPanel, weaponMapRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.DefenseMessagesPanel, messageRect);
        }

        private static IShuttleTutorialTargetService GetTutorialTargets(
            ShuttlePageDrawContext context)
        {
            return context != null && context.DefensePageContext != null
                ? context.DefensePageContext.TutorialTargets
                : null;
        }
    }
}

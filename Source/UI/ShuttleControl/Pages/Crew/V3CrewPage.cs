using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Diagnostics;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewPage : IShuttleIntegratedHeaderPageV3
    {
        private const float RightPanelWidth = ShuttleUIStyle.PageRightPanelWidth;

        private readonly V3CrewText text = new V3CrewText();
        private readonly V3CrewPageModel model = new V3CrewPageModel();
        private readonly V3CrewPageModelBuilder modelBuilder =
            new V3CrewPageModelBuilder();
        private readonly V3CrewPageState fallbackState = new V3CrewPageState();
        private readonly V3SharedMessagePanel messagePanel =
            new V3SharedMessagePanel();
        private readonly V3CrewHeader header;
        private readonly V3CrewListPanel listPanel;
        private readonly V3CrewDetailPanel detailPanel;
        private readonly V3CrewActionPanel actionPanel;

        internal V3CrewPage()
        {
            this.header = new V3CrewHeader(this.text);
            this.listPanel = new V3CrewListPanel(this.text);
            this.detailPanel = new V3CrewDetailPanel(this.text);
            this.actionPanel = new V3CrewActionPanel(this.text);
        }

        public ShuttleControlPageId Page
        {
            get { return ShuttleControlPageId.Crew; }
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

            V3CrewPageState state = this.GetPageState(context);
            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.CrewPageProjection))
            {
                this.modelBuilder.Fill(this.model, context.CrewInputs);
            }

            ShuttlePageFrameLayout layout = ShuttlePageFrameLayout.HeaderBody(rect);
            this.header.Draw(layout.HeaderRect, this.model, context);
            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.CrewBodyDraw))
            {
                this.DrawBody(layout.BodyRect, this.model, context, state);
            }
        }

        private void DrawBody(
            Rect rect,
            V3CrewPageModel pageModel,
            ShuttlePageDrawContext context,
            V3CrewPageState state)
        {
            float gap = ShuttleUIStyle.Gap;
            float rightX = rect.xMax - RightPanelWidth;
            float leftMiddleWidth = rect.width - RightPanelWidth - gap;
            ShuttlePageBottomMessageLayout messageLayout =
                ShuttlePageBottomMessageLayout.FromBodyHeight(rect.height, gap);
            float upperHeight = messageLayout.ContentHeight;
            float leftWidth = Mathf.Floor(leftMiddleWidth * 0.50f) - (gap * 0.5f);
            float middleWidth = leftMiddleWidth - leftWidth - gap;
            float habitatOpsHeight = 122f;
            float rightStackHeight = Mathf.Max(0f, rect.height - habitatOpsHeight - gap);

            Rect humanRect = new Rect(rect.x, rect.y, leftWidth, upperHeight);
            Rect mechRect = new Rect(humanRect.xMax + gap, rect.y, middleWidth, upperHeight);
            Rect messageRect = new Rect(rect.x, humanRect.yMax + gap, leftMiddleWidth, messageLayout.MessageHeight);
            Rect habitatOpsRect = new Rect(rightX, rect.y, RightPanelWidth, habitatOpsHeight);
            Rect animalRect = new Rect(rightX, habitatOpsRect.yMax + gap, RightPanelWidth, Mathf.Floor(rightStackHeight * 0.34f));
            Rect entityRect = new Rect(rightX, animalRect.yMax + gap, RightPanelWidth, rect.yMax - animalRect.yMax - gap);
            V3CrewCardModel selectedCrew = V3CrewSelection.FindSelectedCrewCard(
                pageModel != null ? pageModel.CrewData : null,
                state);

            this.RegisterTutorialTargets(
                context,
                rect,
                humanRect,
                mechRect,
                animalRect,
                entityRect,
                habitatOpsRect,
                messageRect);

            this.DrawHumans(humanRect, pageModel, state);
            this.DrawMechs(mechRect, pageModel, state);

            if (selectedCrew != null)
            {
                this.DrawSelectedCrewDetail(
                    new Rect(rightX, rect.y, RightPanelWidth, rect.height),
                    selectedCrew,
                    context,
                    state);
            }
            else
            {
                this.DrawCrewOperations(habitatOpsRect, pageModel, context);
                this.DrawAnimals(animalRect, pageModel, state);
                this.DrawEntities(entityRect, pageModel, state);
            }

            this.messagePanel.Draw(
                messageRect,
                context,
                state.MessagePanelState,
                ref state.MessageScroll);
        }

        private void DrawHumans(
            Rect rect,
            V3CrewPageModel pageModel,
            V3CrewPageState state)
        {
            this.listPanel.DrawGroup(
                rect,
                this.text.Tr("CT_Shuttle_Crew_Humans"),
                pageModel != null && pageModel.CrewData != null ? pageModel.CrewData.Humans : null,
                V3CrewGroupKind.Human,
                ref state.HumanCrewScroll,
                false,
                state,
                delegate(Rect actionRect, V3CrewCardModel card)
                {
                    this.DrawCrewActionButton(actionRect, card, state);
                });
        }

        private void DrawMechs(
            Rect rect,
            V3CrewPageModel pageModel,
            V3CrewPageState state)
        {
            this.listPanel.DrawGroup(
                rect,
                this.text.Tr("CT_Shuttle_Crew_Mechanoids"),
                pageModel != null && pageModel.CrewData != null ? pageModel.CrewData.Mechs : null,
                V3CrewGroupKind.Mech,
                ref state.MechCrewScroll,
                false,
                state,
                delegate(Rect actionRect, V3CrewCardModel card)
                {
                    this.DrawCrewActionButton(actionRect, card, state);
                });
        }

        private void DrawAnimals(
            Rect rect,
            V3CrewPageModel pageModel,
            V3CrewPageState state)
        {
            this.listPanel.DrawGroup(
                rect,
                this.text.Tr("CT_Shuttle_Crew_Animals"),
                pageModel != null && pageModel.CrewData != null ? pageModel.CrewData.Animals : null,
                V3CrewGroupKind.Animal,
                ref state.AnimalCrewScroll,
                true,
                state,
                delegate(Rect actionRect, V3CrewCardModel card)
                {
                    this.DrawCrewActionButton(actionRect, card, state);
                });
        }

        private void DrawEntities(
            Rect rect,
            V3CrewPageModel pageModel,
            V3CrewPageState state)
        {
            this.listPanel.DrawGroup(
                rect,
                this.text.Tr("CT_Shuttle_Crew_Entities"),
                pageModel != null && pageModel.CrewData != null ? pageModel.CrewData.Entities : null,
                V3CrewGroupKind.Entity,
                ref state.EntityCrewScroll,
                true,
                state,
                delegate(Rect actionRect, V3CrewCardModel card)
                {
                    this.DrawCrewActionButton(actionRect, card, state);
                });
        }

        private void DrawCrewOperations(
            Rect rect,
            V3CrewPageModel pageModel,
            ShuttlePageDrawContext context)
        {
            this.actionPanel.DrawCrewOperations(
                rect,
                pageModel != null ? pageModel.ControlModel : null,
                context,
                delegate
                {
                    this.OpenUnloadDialog(pageModel, context);
                });
        }

        private void OpenUnloadDialog(
            V3CrewPageModel pageModel,
            ShuttlePageDrawContext context)
        {
            IShuttleCrewUnloadDialogActions actions = GetUnloadDialogActions(context);
            if (actions == null)
            {
                return;
            }

            V3CrewPageContext pageContext =
                context != null ? context.CrewPageContext : null;
            actions.Open(
                pageModel != null ? pageModel.CrewData : null,
                pageContext != null ? pageContext.LoadedCrewActions : null,
                pageContext != null ? pageContext.HabitatActions : null,
                pageContext != null ? pageContext.MechChargerActions : null);
        }

        private void DrawSelectedCrewDetail(
            Rect rect,
            V3CrewCardModel selectedCrew,
            ShuttlePageDrawContext context,
            V3CrewPageState state)
        {
            this.detailPanel.Draw(
                rect,
                selectedCrew,
                state,
                delegate { V3CrewSelection.Clear(state); },
                delegate { this.actionPanel.OpenVanillaInfo(selectedCrew); },
                delegate { this.actionPanel.RemoveCrewCard(selectedCrew, context); },
                this.actionPanel.CanRemoveCrewCard(selectedCrew, context),
                this.actionPanel.GetCrewRemoveTooltip(selectedCrew, context));
        }

        private void DrawCrewActionButton(
            Rect rect,
            V3CrewCardModel card,
            V3CrewPageState state)
        {
            if (this.actionPanel.DrawCrewCardActionButton(
                rect,
                this.text.Tr("CT_ShuttleCrew_ViewDetails")))
            {
                V3CrewSelection.Select(state, card);
            }
        }

        private V3CrewPageState GetPageState(ShuttlePageDrawContext context)
        {
            if (context != null && context.State != null && context.State.Crew != null)
            {
                return context.State.Crew;
            }

            return this.fallbackState;
        }

        private void RegisterTutorialTargets(
            ShuttlePageDrawContext context,
            Rect pageRect,
            Rect humanRect,
            Rect mechRect,
            Rect animalRect,
            Rect entityRect,
            Rect habitatOpsRect,
            Rect messageRect)
        {
            IShuttleTutorialTargetService tutorialTargets = GetTutorialTargets(context);
            if (tutorialTargets == null)
            {
                return;
            }

            tutorialTargets.Register(ShuttleTutorialTargetIds.CrewOverviewPanel, pageRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.CrewHumansPanel, humanRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.CrewMechanoidsPanel, mechRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.CrewAnimalsPanel, animalRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.CrewEntitiesPanel, entityRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.CrewHabitatOpsPanel, habitatOpsRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.CrewMessagesPanel, messageRect);
        }

        private static IShuttleTutorialTargetService GetTutorialTargets(
            ShuttlePageDrawContext context)
        {
            return context != null && context.CrewPageContext != null
                ? context.CrewPageContext.TutorialTargets
                : null;
        }

        private static IShuttleCrewUnloadDialogActions GetUnloadDialogActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.CrewPageContext != null
                ? context.CrewPageContext.UnloadDialogActions
                : null;
        }
    }
}

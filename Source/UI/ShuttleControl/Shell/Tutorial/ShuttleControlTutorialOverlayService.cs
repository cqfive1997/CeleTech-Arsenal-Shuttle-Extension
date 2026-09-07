using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.State;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttleControlTutorialOverlayService
    {
        private readonly IShuttleAssemblyEventReadPort assemblyEventReadPort;
        private readonly IShuttleTutorialDefinitionProvider definitions;
        private readonly ShuttleControlState state;
        private readonly ShuttleControlTutorialTargetService tutorialTargets;
        private readonly ShuttleTutorialSession tutorialSession;
        private readonly ShuttleTutorialOverlayDrawer tutorialOverlay =
            new ShuttleTutorialOverlayDrawer();
        private readonly ShuttleTutorialPreActionRunner preActionRunner;

        internal ShuttleControlTutorialOverlayService(
            IShuttleAssemblyEventReadPort assemblyEventReadPort,
            ShuttleControlState state,
            ShuttleControlTutorialTargetService tutorialTargets)
        {
            this.assemblyEventReadPort = assemblyEventReadPort;
            this.state = state;
            this.tutorialTargets = tutorialTargets;
            this.definitions = new ShuttleTutorialDefinitionProvider();
            this.tutorialSession =
                new ShuttleTutorialSession(this.definitions);
            this.preActionRunner = new ShuttleTutorialPreActionRunner(state);
        }

        internal bool ShouldPauseSimulation
        {
            get
            {
                if (this.tutorialSession.IsActive)
                {
                    return true;
                }

                if (this.state == null ||
                    !ShuttleTutorialProgress.TutorialsEnabled ||
                    !ShuttleTutorialProgress.AutoStartEnabled ||
                    ShuttleTutorialProgress.HasCompletedPage(this.state.CurrentPage))
                {
                    return false;
                }

                ShuttleTutorialDefinition definition =
                    this.definitions.GetDefinition(this.state.CurrentPage);
                return definition != null &&
                    definition.Steps != null &&
                    definition.Steps.Count > 0;
            }
        }

        internal void DrawWithTutorial(
            Rect rect,
            ShuttleControlPageId currentPage,
            ShuttleControlReadModel controlModel,
            Action drawAction)
        {
            this.DrainTutorialEvents();
            this.tutorialOverlay.HandleInputBeforePageDraw(
                this.tutorialSession,
                currentPage);
            this.preActionRunner.Apply(
                this.tutorialSession,
                currentPage,
                controlModel);
            if (this.tutorialTargets != null)
            {
                this.tutorialTargets.BeginFrame(currentPage, rect);
            }

            if (drawAction != null)
            {
                drawAction();
            }

            if (this.tutorialTargets == null)
            {
                return;
            }

            this.tutorialSession.UpdateAfterPageDraw(
                currentPage,
                this.tutorialTargets);
            this.tutorialOverlay.Draw(
                rect,
                this.tutorialSession,
                this.tutorialTargets);
        }

        private void DrainTutorialEvents()
        {
            if (this.assemblyEventReadPort == null)
            {
                return;
            }

            ShuttleAssemblyEvent assemblyEvent;
            int guard = 0;
            while (guard < 8 &&
                this.assemblyEventReadPort.TryDequeueAssemblyEvent(out assemblyEvent))
            {
                ShuttleTutorialEvent tutorialEvent =
                    this.ConvertToTutorialEvent(assemblyEvent);
                if (tutorialEvent != null)
                {
                    this.tutorialSession.EnqueueContextualEvent(tutorialEvent);
                }

                guard++;
            }
        }

        private ShuttleTutorialEvent ConvertToTutorialEvent(
            ShuttleAssemblyEvent assemblyEvent)
        {
            if (assemblyEvent == null ||
                assemblyEvent.Kind != ShuttleAssemblyEventKind.SegmentInstalled)
            {
                return null;
            }

            return new ShuttleTutorialEvent(
                ShuttleTutorialEventKind.SegmentInstalled,
                assemblyEvent.EventID,
                assemblyEvent.SegmentSlotID,
                assemblyEvent.SegmentDefName,
                assemblyEvent.SegmentKindKey,
                assemblyEvent.TicksGame);
        }
    }
}

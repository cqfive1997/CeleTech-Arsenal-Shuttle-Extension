using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.State;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttleTutorialPreActionRunner
    {
        private readonly ShuttleControlState state;

        internal ShuttleTutorialPreActionRunner(ShuttleControlState state)
        {
            this.state = state;
        }

        internal void Apply(
            ShuttleTutorialSession session,
            ShuttleControlPageId currentPage,
            ShuttleControlReadModel controlModel)
        {
            if (this.state == null ||
                session == null ||
                !session.IsActive ||
                session.CurrentPage != currentPage)
            {
                return;
            }

            ShuttleTutorialStep step = session.CurrentStep;
            if (step == null || currentPage != ShuttleControlPageId.Main)
            {
                return;
            }

            if (step.PreAction == ShuttleTutorialPreAction.MainShowCurrentIssues)
            {
                this.state.Main.MessagePanelState.CurrentTab =
                    V3SharedMessagePanelTab.CurrentIssues;
                return;
            }

            if (step.PreAction == ShuttleTutorialPreAction.MainShowLaunchChecklist)
            {
                this.state.Main.MessagePanelState.CurrentTab =
                    V3SharedMessagePanelTab.LaunchDiagnostics;
                return;
            }

            if (step.PreAction == ShuttleTutorialPreAction.MainResetSegmentScroll)
            {
                this.state.Main.SegmentScroll = Vector2.zero;
                return;
            }

            if (step.PreAction == ShuttleTutorialPreAction.MainFocusOptionalSegment)
            {
                this.FocusFirstOptionalSegment(controlModel);
                return;
            }

            if (step.PreAction == ShuttleTutorialPreAction.MainSelectContextSegment)
            {
                this.SelectContextSegment(session, controlModel);
            }
        }

        private void SelectContextSegment(
            ShuttleTutorialSession session,
            ShuttleControlReadModel controlModel)
        {
            ShuttleTutorialEvent tutorialEvent = session.CurrentContextEvent;
            if (tutorialEvent == null ||
                string.IsNullOrEmpty(tutorialEvent.SegmentSlotID) ||
                controlModel == null ||
                controlModel.FindSegmentSlot(tutorialEvent.SegmentSlotID) == null)
            {
                return;
            }

            this.state.Main.SelectedSegmentSlotID = tutorialEvent.SegmentSlotID;
            this.state.Main.FocusedSegmentSlotID = tutorialEvent.SegmentSlotID;
        }

        private void FocusFirstOptionalSegment(ShuttleControlReadModel controlModel)
        {
            if (controlModel == null || controlModel.SegmentSlots == null)
            {
                return;
            }

            for (int i = 0; i < controlModel.SegmentSlots.Count; i++)
            {
                ShuttleControlSegmentSlotModel slot = controlModel.SegmentSlots[i];
                if (slot == null || slot.IsRequired || string.IsNullOrEmpty(slot.SlotID))
                {
                    continue;
                }

                this.state.Main.SelectedSegmentSlotID = slot.SlotID;
                this.state.Main.FocusedSegmentSlotID = slot.SlotID;
                return;
            }
        }
    }
}

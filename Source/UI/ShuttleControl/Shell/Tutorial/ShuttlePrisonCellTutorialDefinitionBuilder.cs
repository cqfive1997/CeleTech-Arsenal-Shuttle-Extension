using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttlePrisonCellTutorialDefinitionBuilder
    {
        internal ShuttleTutorialDefinition Build()
        {
            List<ShuttleTutorialStep> steps = new List<ShuttleTutorialStep>();
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.PrisonOverviewPanel,
                "CT_Shuttle_Tutorial_Prison_Overview_Title",
                "CT_Shuttle_Tutorial_Prison_Overview_Body",
                ShuttleTutorialPlacement.Auto,
                6f,
                null,
                ShuttleTutorialPreAction.None,
                1.8f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.PrisonPrisonerListPanel,
                "CT_Shuttle_Tutorial_Prison_PrisonerList_Title",
                "CT_Shuttle_Tutorial_Prison_PrisonerList_Body",
                ShuttleTutorialPlacement.Right,
                6f,
                ShuttleTutorialTargetIds.PrisonOverviewPanel,
                ShuttleTutorialPreAction.None,
                2.0f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.PrisonAdmissionPanel,
                "CT_Shuttle_Tutorial_Prison_Admission_Title",
                "CT_Shuttle_Tutorial_Prison_Admission_Body",
                ShuttleTutorialPlacement.Right,
                6f,
                ShuttleTutorialTargetIds.PrisonActionsPanel,
                ShuttleTutorialPreAction.None,
                2.4f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.PrisonCellStatusPanel,
                "CT_Shuttle_Tutorial_Prison_CellStatus_Title",
                "CT_Shuttle_Tutorial_Prison_CellStatus_Body",
                ShuttleTutorialPlacement.Left,
                6f,
                ShuttleTutorialTargetIds.PrisonActionsPanel,
                ShuttleTutorialPreAction.None,
                2.2f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.PrisonMessagesPanel,
                "CT_Shuttle_Tutorial_Prison_Messages_Title",
                "CT_Shuttle_Tutorial_Prison_Messages_Body",
                ShuttleTutorialPlacement.Above,
                6f,
                ShuttleTutorialTargetIds.PrisonOverviewPanel,
                ShuttleTutorialPreAction.None,
                2.2f,
                true);
            return new ShuttleTutorialDefinition(ShuttleControlPageId.PrisonCell, steps);
        }
    }
}

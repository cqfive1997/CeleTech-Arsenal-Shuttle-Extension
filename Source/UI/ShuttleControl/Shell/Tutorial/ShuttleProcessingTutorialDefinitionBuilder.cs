using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttleProcessingTutorialDefinitionBuilder
    {
        internal ShuttleTutorialDefinition Build()
        {
            List<ShuttleTutorialStep> steps = new List<ShuttleTutorialStep>();
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.ProcessingOverviewPanel,
                "CT_Shuttle_Tutorial_Processing_Overview_Title",
                "CT_Shuttle_Tutorial_Processing_Overview_Body",
                ShuttleTutorialPlacement.Auto,
                6f,
                null,
                ShuttleTutorialPreAction.None,
                1.8f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.ProcessingWorktableListPanel,
                "CT_Shuttle_Tutorial_Processing_WorktableList_Title",
                "CT_Shuttle_Tutorial_Processing_WorktableList_Body",
                ShuttleTutorialPlacement.Right,
                6f,
                ShuttleTutorialTargetIds.ProcessingOverviewPanel,
                ShuttleTutorialPreAction.None,
                2.0f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.ProcessingRecipePanel,
                "CT_Shuttle_Tutorial_Processing_Recipes_Title",
                "CT_Shuttle_Tutorial_Processing_Recipes_Body",
                ShuttleTutorialPlacement.Left,
                6f,
                ShuttleTutorialTargetIds.ProcessingWorktableListPanel,
                ShuttleTutorialPreAction.None,
                2.0f,
                true);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.ProcessingQueuePanel,
                "CT_Shuttle_Tutorial_Processing_Queue_Title",
                "CT_Shuttle_Tutorial_Processing_Queue_Body",
                ShuttleTutorialPlacement.Left,
                6f,
                ShuttleTutorialTargetIds.ProcessingInputOutputPanel,
                ShuttleTutorialPreAction.None,
                2.0f,
                true);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.ProcessingMessagesPanel,
                "CT_Shuttle_Tutorial_Processing_Messages_Title",
                "CT_Shuttle_Tutorial_Processing_Messages_Body",
                ShuttleTutorialPlacement.Above,
                6f,
                ShuttleTutorialTargetIds.ProcessingStatusPanel,
                ShuttleTutorialPreAction.None,
                2.0f,
                true);
            return new ShuttleTutorialDefinition(ShuttleControlPageId.Processing, steps);
        }
    }
}

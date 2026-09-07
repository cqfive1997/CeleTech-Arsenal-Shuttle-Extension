using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttleCargoLoadDialogTutorialDefinitionBuilder
    {
        internal const string ProgressKey = "CargoLoadDialog";

        internal ShuttleTutorialDefinition Build()
        {
            List<ShuttleTutorialStep> steps = new List<ShuttleTutorialStep>();
            ShuttleTutorialStepFactory.Add(
                steps,
                null,
                "CT_Shuttle_Tutorial_LoadDialog_Overview_Title",
                "CT_Shuttle_Tutorial_LoadDialog_Overview_Body",
                ShuttleTutorialPlacement.Center,
                0f,
                null,
                ShuttleTutorialPreAction.None,
                0.6f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.LoadDialogQueuePanel,
                "CT_Shuttle_Tutorial_LoadDialog_Queue_Title",
                "CT_Shuttle_Tutorial_LoadDialog_Queue_Body",
                ShuttleTutorialPlacement.Left,
                5f,
                null,
                ShuttleTutorialPreAction.None,
                0.6f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.LoadDialogAvailablePanel,
                "CT_Shuttle_Tutorial_LoadDialog_Available_Title",
                "CT_Shuttle_Tutorial_LoadDialog_Available_Body",
                ShuttleTutorialPlacement.Right,
                5f,
                null,
                ShuttleTutorialPreAction.None,
                0.6f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.LoadDialogPlanPanel,
                "CT_Shuttle_Tutorial_LoadDialog_Plan_Title",
                "CT_Shuttle_Tutorial_LoadDialog_Plan_Body",
                ShuttleTutorialPlacement.Left,
                5f,
                null,
                ShuttleTutorialPreAction.None,
                0.6f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.LoadDialogSummary,
                "CT_Shuttle_Tutorial_LoadDialog_Summary_Title",
                "CT_Shuttle_Tutorial_LoadDialog_Summary_Body",
                ShuttleTutorialPlacement.Above,
                5f,
                null,
                ShuttleTutorialPreAction.None,
                0.6f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.LoadDialogStartButton,
                "CT_Shuttle_Tutorial_LoadDialog_Start_Title",
                "CT_Shuttle_Tutorial_LoadDialog_Start_Body",
                ShuttleTutorialPlacement.Above,
                5f,
                null,
                ShuttleTutorialPreAction.None,
                0.6f);
            return new ShuttleTutorialDefinition(ShuttleControlPageId.Cargo, steps);
        }
    }
}

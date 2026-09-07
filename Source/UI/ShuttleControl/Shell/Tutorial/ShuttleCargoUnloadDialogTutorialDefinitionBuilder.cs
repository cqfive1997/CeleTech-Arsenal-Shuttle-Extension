using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttleCargoUnloadDialogTutorialDefinitionBuilder
    {
        internal const string ProgressKey = "CargoUnloadDialog";

        internal ShuttleTutorialDefinition Build()
        {
            List<ShuttleTutorialStep> steps = new List<ShuttleTutorialStep>();
            ShuttleTutorialStepFactory.Add(
                steps,
                null,
                "CT_Shuttle_Tutorial_UnloadDialog_Overview_Title",
                "CT_Shuttle_Tutorial_UnloadDialog_Overview_Body",
                ShuttleTutorialPlacement.Center,
                0f,
                null,
                ShuttleTutorialPreAction.None,
                0.6f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.UnloadDialogLoadBar,
                "CT_Shuttle_Tutorial_UnloadDialog_LoadBar_Title",
                "CT_Shuttle_Tutorial_UnloadDialog_LoadBar_Body",
                ShuttleTutorialPlacement.Left,
                5f,
                null,
                ShuttleTutorialPreAction.None,
                0.6f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.UnloadDialogToolbar,
                "CT_Shuttle_Tutorial_UnloadDialog_Toolbar_Title",
                "CT_Shuttle_Tutorial_UnloadDialog_Toolbar_Body",
                ShuttleTutorialPlacement.Below,
                5f,
                null,
                ShuttleTutorialPreAction.None,
                0.6f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.UnloadDialogAvailablePanel,
                "CT_Shuttle_Tutorial_UnloadDialog_Available_Title",
                "CT_Shuttle_Tutorial_UnloadDialog_Available_Body",
                ShuttleTutorialPlacement.Right,
                5f,
                null,
                ShuttleTutorialPreAction.None,
                0.6f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.UnloadDialogSelectedPanel,
                "CT_Shuttle_Tutorial_UnloadDialog_Selected_Title",
                "CT_Shuttle_Tutorial_UnloadDialog_Selected_Body",
                ShuttleTutorialPlacement.Left,
                5f,
                null,
                ShuttleTutorialPreAction.None,
                0.6f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.UnloadDialogActionButtons,
                "CT_Shuttle_Tutorial_UnloadDialog_Start_Title",
                "CT_Shuttle_Tutorial_UnloadDialog_Start_Body",
                ShuttleTutorialPlacement.Above,
                5f,
                null,
                ShuttleTutorialPreAction.None,
                0.6f);
            return new ShuttleTutorialDefinition(ShuttleControlPageId.Cargo, steps);
        }
    }
}

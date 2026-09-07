using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttleLoadingTutorialDefinitionBuilder
    {
        internal ShuttleTutorialDefinition Build()
        {
            List<ShuttleTutorialStep> steps = new List<ShuttleTutorialStep>();
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.LoadingOverviewPanel,
                "CT_Shuttle_Tutorial_Loading_Overview_Title",
                "CT_Shuttle_Tutorial_Loading_Overview_Body",
                ShuttleTutorialPlacement.Auto,
                6f,
                null,
                ShuttleTutorialPreAction.None,
                1.8f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.LoadingCargoBaysPanel,
                "CT_Shuttle_Tutorial_Loading_Bays_Title",
                "CT_Shuttle_Tutorial_Loading_Bays_Body",
                ShuttleTutorialPlacement.Right,
                6f,
                ShuttleTutorialTargetIds.LoadingCapacityStatsPanel,
                ShuttleTutorialPreAction.None,
                2.0f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.LoadingInventoryPanel,
                "CT_Shuttle_Tutorial_Loading_ItemStats_Title",
                "CT_Shuttle_Tutorial_Loading_ItemStats_Body",
                ShuttleTutorialPlacement.Left,
                6f,
                ShuttleTutorialTargetIds.LoadingOverviewPanel,
                ShuttleTutorialPreAction.None,
                2.0f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.LoadingCategorySummaryPanel,
                "CT_Shuttle_Tutorial_Loading_CategorySummary_Title",
                "CT_Shuttle_Tutorial_Loading_CategorySummary_Body",
                ShuttleTutorialPlacement.Below,
                4f,
                ShuttleTutorialTargetIds.LoadingOverviewPanel,
                ShuttleTutorialPreAction.None,
                2.0f,
                true);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.LoadingLogisticsModulePanel,
                "CT_Shuttle_Tutorial_Loading_LogisticsModule_Title",
                "CT_Shuttle_Tutorial_Loading_LogisticsModule_Body",
                ShuttleTutorialPlacement.Above,
                6f,
                ShuttleTutorialTargetIds.LoadingCargoBaysPanel,
                ShuttleTutorialPreAction.None,
                2.0f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.LoadingMessagesPanel,
                "CT_Shuttle_Tutorial_Loading_Messages_Title",
                "CT_Shuttle_Tutorial_Loading_Messages_Body",
                ShuttleTutorialPlacement.Above,
                6f,
                ShuttleTutorialTargetIds.LoadingOverviewPanel,
                ShuttleTutorialPreAction.None,
                2.0f,
                true);
            return new ShuttleTutorialDefinition(ShuttleControlPageId.Cargo, steps);
        }
    }
}

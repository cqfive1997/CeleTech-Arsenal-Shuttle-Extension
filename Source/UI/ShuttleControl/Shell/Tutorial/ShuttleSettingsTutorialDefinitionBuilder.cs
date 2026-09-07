using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttleSettingsTutorialDefinitionBuilder
    {
        internal ShuttleTutorialDefinition Build()
        {
            List<ShuttleTutorialStep> steps = new List<ShuttleTutorialStep>();
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.SettingsNavigationPanel,
                "CT_Shuttle_Tutorial_Settings_Overview_Title",
                "CT_Shuttle_Tutorial_Settings_Overview_Body",
                ShuttleTutorialPlacement.Right,
                6f,
                null,
                ShuttleTutorialPreAction.None,
                1.8f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.SettingsPerformanceButton,
                "CT_Shuttle_Tutorial_Settings_ConfigButtons_Title",
                "CT_Shuttle_Tutorial_Settings_ConfigButtons_Body",
                ShuttleTutorialPlacement.Right,
                3f,
                ShuttleTutorialTargetIds.SettingsNavigationPanel,
                ShuttleTutorialPreAction.None,
                1.8f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.SettingsTutorialsEnabledToggle,
                "CT_Shuttle_Tutorial_Settings_TutorialToggle_Title",
                "CT_Shuttle_Tutorial_Settings_TutorialToggle_Body",
                ShuttleTutorialPlacement.Right,
                3f,
                ShuttleTutorialTargetIds.SettingsNavigationPanel,
                ShuttleTutorialPreAction.None,
                2.0f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.SettingsTutorialsAutoStartToggle,
                "CT_Shuttle_Tutorial_Settings_AutoStart_Title",
                "CT_Shuttle_Tutorial_Settings_AutoStart_Body",
                ShuttleTutorialPlacement.Right,
                3f,
                ShuttleTutorialTargetIds.SettingsNavigationPanel,
                ShuttleTutorialPreAction.None,
                2.0f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.SettingsResetTutorialsButton,
                "CT_Shuttle_Tutorial_Settings_Reset_Title",
                "CT_Shuttle_Tutorial_Settings_Reset_Body",
                ShuttleTutorialPlacement.Right,
                3f,
                ShuttleTutorialTargetIds.SettingsNavigationPanel,
                ShuttleTutorialPreAction.None,
                2.0f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.SettingsRightInfoPanel,
                "CT_Shuttle_Tutorial_Settings_InfoPanel_Title",
                "CT_Shuttle_Tutorial_Settings_InfoPanel_Body",
                ShuttleTutorialPlacement.Left,
                6f,
                null,
                ShuttleTutorialPreAction.None,
                1.8f);
            return new ShuttleTutorialDefinition(ShuttleControlPageId.Settings, steps);
        }
    }
}

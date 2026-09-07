using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttleDefenseTutorialDefinitionBuilder
    {
        internal ShuttleTutorialDefinition Build()
        {
            List<ShuttleTutorialStep> steps = new List<ShuttleTutorialStep>();
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.DefenseOverviewPanel,
                "CT_Shuttle_Tutorial_Defense_Overview_Title",
                "CT_Shuttle_Tutorial_Defense_Overview_Body",
                ShuttleTutorialPlacement.Auto,
                6f,
                null,
                ShuttleTutorialPreAction.None,
                1.8f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.DefenseWeaponListPanel,
                "CT_Shuttle_Tutorial_Defense_WeaponList_Title",
                "CT_Shuttle_Tutorial_Defense_WeaponList_Body",
                ShuttleTutorialPlacement.Right,
                6f,
                ShuttleTutorialTargetIds.DefenseOverviewPanel,
                ShuttleTutorialPreAction.None,
                2.0f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.DefenseWeaponListPanel,
                "CT_Shuttle_Tutorial_Defense_Ammo_Title",
                "CT_Shuttle_Tutorial_Defense_Ammo_Body",
                ShuttleTutorialPlacement.Right,
                6f,
                ShuttleTutorialTargetIds.DefenseOverviewPanel,
                ShuttleTutorialPreAction.None,
                2.2f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.DefenseWeaponListPanel,
                "CT_Shuttle_Tutorial_Defense_PointDefense_Title",
                "CT_Shuttle_Tutorial_Defense_PointDefense_Body",
                ShuttleTutorialPlacement.Right,
                6f,
                ShuttleTutorialTargetIds.DefenseOverviewPanel,
                ShuttleTutorialPreAction.None,
                2.4f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.DefenseFireControlPanel,
                "CT_Shuttle_Tutorial_Defense_FireControl_Title",
                "CT_Shuttle_Tutorial_Defense_FireControl_Body",
                ShuttleTutorialPlacement.Left,
                6f,
                ShuttleTutorialTargetIds.DefenseWeaponListPanel,
                ShuttleTutorialPreAction.None,
                2.0f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.DefenseShieldPanel,
                "CT_Shuttle_Tutorial_Defense_Shields_Title",
                "CT_Shuttle_Tutorial_Defense_Shields_Body",
                ShuttleTutorialPlacement.Left,
                6f,
                ShuttleTutorialTargetIds.DefenseFireControlPanel,
                ShuttleTutorialPreAction.None,
                2.0f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.DefenseSelectedWeaponPanel,
                "CT_Shuttle_Tutorial_Defense_SelectedWeapon_Title",
                "CT_Shuttle_Tutorial_Defense_SelectedWeapon_Body",
                ShuttleTutorialPlacement.Left,
                6f,
                ShuttleTutorialTargetIds.DefenseShieldPanel,
                ShuttleTutorialPreAction.None,
                2.0f);
            return new ShuttleTutorialDefinition(ShuttleControlPageId.Defense, steps);
        }
    }
}

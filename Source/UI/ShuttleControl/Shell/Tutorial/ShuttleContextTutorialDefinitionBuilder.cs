using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttleContextTutorialDefinitionBuilder
    {
        internal ShuttleTutorialDefinition Build(
            ShuttleControlPageId page,
            ShuttleTutorialEvent tutorialEvent)
        {
            if (page != ShuttleControlPageId.Main ||
                tutorialEvent == null ||
                tutorialEvent.Kind != ShuttleTutorialEventKind.SegmentInstalled)
            {
                return null;
            }

            List<ShuttleTutorialStep> steps = this.BuildSegmentInstalledSteps(tutorialEvent);
            return steps != null && steps.Count > 0
                ? new ShuttleTutorialDefinition(page, steps)
                : null;
        }

        private List<ShuttleTutorialStep> BuildSegmentInstalledSteps(
            ShuttleTutorialEvent tutorialEvent)
        {
            string kind =
                ShuttleTutorialContextKeyUtility.GetSegmentInstalledContextKind(
                    tutorialEvent);
            List<ShuttleTutorialStep> steps = new List<ShuttleTutorialStep>();
            ShuttleTutorialStepFactory.Add(
                steps,
                null,
                this.GetInstalledTitleKey(kind),
                this.GetInstalledBodyKey(kind),
                ShuttleTutorialPlacement.Center,
                0f,
                null,
                ShuttleTutorialPreAction.None,
                1.5f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.MainSelectedSegmentModulesPanel,
                "CT_Shuttle_Tutorial_Context_SegmentModules_Title",
                "CT_Shuttle_Tutorial_Context_SegmentModules_Body",
                ShuttleTutorialPlacement.Left,
                6f,
                null,
                ShuttleTutorialPreAction.MainSelectContextSegment,
                2.2f,
                true);
            this.AddKindSpecificStep(steps, kind);
            return steps;
        }

        private void AddKindSpecificStep(List<ShuttleTutorialStep> steps, string kind)
        {
            if (kind == ShuttleTutorialContextKeyUtility.Cockpit)
            {
                this.AddModuleSlotStep(
                    steps,
                    ShuttleTutorialTargetIds.MainContextModuleSlotCockpit,
                    "CT_Shuttle_Tutorial_Context_CockpitModuleSlot_Title",
                    "CT_Shuttle_Tutorial_Context_CockpitModuleSlot_Body",
                    ShuttleTutorialTargetIds.MainContextModuleSlotControl);
                return;
            }

            if (kind == ShuttleTutorialContextKeyUtility.Power)
            {
                this.AddModuleSlotStep(
                    steps,
                    ShuttleTutorialTargetIds.MainContextModuleSlotReactor,
                    "CT_Shuttle_Tutorial_Context_ReactorModuleSlot_Title",
                    "CT_Shuttle_Tutorial_Context_ReactorModuleSlot_Body",
                    null);
                this.AddModuleSlotStep(
                    steps,
                    ShuttleTutorialTargetIds.MainContextModuleSlotBattery,
                    "CT_Shuttle_Tutorial_Context_BatteryModuleSlot_Title",
                    "CT_Shuttle_Tutorial_Context_BatteryModuleSlot_Body",
                    null);
                return;
            }

            if (kind == ShuttleTutorialContextKeyUtility.Cargo)
            {
                this.AddModuleSlotStep(
                    steps,
                    ShuttleTutorialTargetIds.MainContextModuleSlotCargo,
                    "CT_Shuttle_Tutorial_Context_CargoModuleSlot_Title",
                    "CT_Shuttle_Tutorial_Context_CargoModuleSlot_Body",
                    null);
                this.AddModuleSlotStep(
                    steps,
                    ShuttleTutorialTargetIds.MainContextModuleSlotColdStorage,
                    "CT_Shuttle_Tutorial_Context_ColdStorageModuleSlot_Title",
                    "CT_Shuttle_Tutorial_Context_ColdStorageModuleSlot_Body",
                    null);
                ShuttleTutorialStepFactory.Add(
                    steps,
                    ShuttleTutorialTargetIds.HeaderPageSwitchButton,
                    "CT_Shuttle_Tutorial_Context_CargoPagesUnlocked_Title",
                    "CT_Shuttle_Tutorial_Context_CargoPagesUnlocked_Body",
                    ShuttleTutorialPlacement.Below,
                    2f,
                    null,
                    ShuttleTutorialPreAction.None,
                    2.2f,
                    true);
                return;
            }

            if (kind == ShuttleTutorialContextKeyUtility.Weapon)
            {
                this.AddModuleSlotStep(
                    steps,
                    ShuttleTutorialTargetIds.MainContextModuleSlotWeapon,
                    "CT_Shuttle_Tutorial_Context_WeaponModuleSlot_Title",
                    "CT_Shuttle_Tutorial_Context_WeaponModuleSlot_Body",
                    null);
                this.AddModuleSlotStep(
                    steps,
                    ShuttleTutorialTargetIds.MainContextModuleSlotFireControl,
                    "CT_Shuttle_Tutorial_Context_FireControlModuleSlot_Title",
                    "CT_Shuttle_Tutorial_Context_FireControlModuleSlot_Body",
                    null);
                this.AddSelectedModuleStep(
                    steps,
                    "CT_Shuttle_Tutorial_Context_WeaponModules_Title",
                    "CT_Shuttle_Tutorial_Context_WeaponModules_Body");
                return;
            }

            if (kind == ShuttleTutorialContextKeyUtility.Living)
            {
                this.AddModuleSlotStep(
                    steps,
                    ShuttleTutorialTargetIds.MainContextModuleSlotHabitat,
                    "CT_Shuttle_Tutorial_Context_HabitatModuleSlot_Title",
                    "CT_Shuttle_Tutorial_Context_HabitatModuleSlot_Body",
                    null);
                this.AddSelectedModuleStep(
                    steps,
                    "CT_Shuttle_Tutorial_Context_LivingModules_Title",
                    "CT_Shuttle_Tutorial_Context_LivingModules_Body");
            }
        }

        private void AddModuleSlotStep(
            List<ShuttleTutorialStep> steps,
            string targetId,
            string titleKey,
            string bodyKey,
            string alternateTargetId)
        {
            ShuttleTutorialStepFactory.Add(
                steps,
                targetId,
                titleKey,
                bodyKey,
                ShuttleTutorialPlacement.Left,
                3f,
                alternateTargetId,
                ShuttleTutorialPreAction.MainSelectContextSegment,
                2.4f,
                true);
        }

        private void AddSelectedModuleStep(
            List<ShuttleTutorialStep> steps,
            string titleKey,
            string bodyKey)
        {
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.MainSelectedSegmentModulesPanel,
                titleKey,
                bodyKey,
                ShuttleTutorialPlacement.Left,
                6f,
                null,
                ShuttleTutorialPreAction.MainSelectContextSegment,
                2.2f,
                true);
        }

        private string GetInstalledTitleKey(string kind)
        {
            if (kind == ShuttleTutorialContextKeyUtility.Cockpit)
            {
                return "CT_Shuttle_Tutorial_Context_CockpitInstalled_Title";
            }

            if (kind == ShuttleTutorialContextKeyUtility.Power)
            {
                return "CT_Shuttle_Tutorial_Context_PowerInstalled_Title";
            }

            if (kind == ShuttleTutorialContextKeyUtility.Cargo)
            {
                return "CT_Shuttle_Tutorial_Context_CargoInstalled_Title";
            }

            if (kind == ShuttleTutorialContextKeyUtility.Weapon)
            {
                return "CT_Shuttle_Tutorial_Context_WeaponInstalled_Title";
            }

            if (kind == ShuttleTutorialContextKeyUtility.Living)
            {
                return "CT_Shuttle_Tutorial_Context_LivingInstalled_Title";
            }

            if (kind == ShuttleTutorialContextKeyUtility.Support)
            {
                return "CT_Shuttle_Tutorial_Context_SupportInstalled_Title";
            }

            return "CT_Shuttle_Tutorial_Context_SegmentInstalled_Title";
        }

        private string GetInstalledBodyKey(string kind)
        {
            if (kind == ShuttleTutorialContextKeyUtility.Cockpit)
            {
                return "CT_Shuttle_Tutorial_Context_CockpitInstalled_Body";
            }

            if (kind == ShuttleTutorialContextKeyUtility.Power)
            {
                return "CT_Shuttle_Tutorial_Context_PowerInstalled_Body";
            }

            if (kind == ShuttleTutorialContextKeyUtility.Cargo)
            {
                return "CT_Shuttle_Tutorial_Context_CargoInstalled_Body";
            }

            if (kind == ShuttleTutorialContextKeyUtility.Weapon)
            {
                return "CT_Shuttle_Tutorial_Context_WeaponInstalled_Body";
            }

            if (kind == ShuttleTutorialContextKeyUtility.Living)
            {
                return "CT_Shuttle_Tutorial_Context_LivingInstalled_Body";
            }

            if (kind == ShuttleTutorialContextKeyUtility.Support)
            {
                return "CT_Shuttle_Tutorial_Context_SupportInstalled_Body";
            }

            return "CT_Shuttle_Tutorial_Context_SegmentInstalled_Body";
        }
    }
}

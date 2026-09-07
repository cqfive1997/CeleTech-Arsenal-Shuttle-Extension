using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal sealed class ShuttleCurrentDefenseIssuesBuilder
    {
        internal void AddIssues(
            ShuttleControlReadModel model,
            ShuttleWeaponBayReadModel weaponBayModel,
            List<ShuttleIssueReadModel> issues)
        {
            if (issues == null || weaponBayModel == null)
            {
                return;
            }

            bool hasDefenseHardware = weaponBayModel.HasWeaponBay ||
                (weaponBayModel.Shields != null && weaponBayModel.Shields.Count > 0) ||
                (weaponBayModel.WeaponControls != null &&
                    weaponBayModel.WeaponControls.Count > 0);
            if (hasDefenseHardware &&
                model != null &&
                !model.InternalBusPowered)
            {
                this.AddSimpleIssue(
                    issues,
                    "defense:power-low",
                    ShuttleIssueSeverity.Warning,
                    ShuttleIssueCategory.Defense,
                    "CT_Shuttle_Issue_DefensePowerLow_Title",
                    "CT_Shuttle_Issue_DefensePowerLow_Detail",
                    ShuttleIssueActionKind.OpenDefense,
                    70);
            }

            this.AddShieldIssues(weaponBayModel, issues);
            this.AddWeaponControlIssues(weaponBayModel, issues);
        }

        private void AddShieldIssues(
            ShuttleWeaponBayReadModel weaponBayModel,
            List<ShuttleIssueReadModel> issues)
        {
            if (weaponBayModel == null || weaponBayModel.Shields == null)
            {
                return;
            }

            for (int i = 0; i < weaponBayModel.Shields.Count; i++)
            {
                ShuttleShieldStatusReadModel shield = weaponBayModel.Shields[i];
                if (shield == null || !shield.HasShield)
                {
                    continue;
                }

                string key = !string.IsNullOrEmpty(shield.ModuleInstanceID)
                    ? shield.ModuleInstanceID
                    : !string.IsNullOrEmpty(shield.SlotID) ? shield.SlotID : i.ToString();
                if (shield.IsEnabled &&
                    shield.MaxHitPoints > 0 &&
                    shield.CurrentHitPoints <= 0)
                {
                    ShuttleIssueReadModel issue = ShuttleIssueReadModelFactory.CreateSimpleIssue(
                        "defense:shield-down:" + key,
                        ShuttleIssueSeverity.Warning,
                        ShuttleIssueCategory.Defense,
                        "CT_Shuttle_Issue_ShieldDown_Title",
                        "CT_Shuttle_Issue_ShieldDown_Detail",
                        ShuttleIssueActionKind.OpenDefense,
                        80);
                    issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                        "CT_Shuttle_Issue_Evidence_Module",
                        this.ResolveShieldModuleLabel(shield)));
                    ShuttleIssueReadModelSet.AddIssueIfUnique(issues, issue);
                }

                if (shield.IsEnabled && shield.RechargeStalledForNoEnergy)
                {
                    ShuttleIssueReadModel issue = ShuttleIssueReadModelFactory.CreateSimpleIssue(
                        "defense:shield-no-energy:" + key,
                        ShuttleIssueSeverity.Warning,
                        ShuttleIssueCategory.Defense,
                        "CT_Shuttle_Issue_ShieldRechargeNoEnergy_Title",
                        "CT_Shuttle_Issue_ShieldRechargeNoEnergy_Detail",
                        ShuttleIssueActionKind.OpenDefense,
                        90);
                    issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                        "CT_Shuttle_Issue_Evidence_Module",
                        this.ResolveShieldModuleLabel(shield)));
                    ShuttleIssueReadModelSet.AddIssueIfUnique(issues, issue);
                }
            }
        }

        private void AddWeaponControlIssues(
            ShuttleWeaponBayReadModel weaponBayModel,
            List<ShuttleIssueReadModel> issues)
        {
            if (weaponBayModel == null || weaponBayModel.WeaponControls == null)
            {
                return;
            }

            for (int i = 0; i < weaponBayModel.WeaponControls.Count; i++)
            {
                ShuttleWeaponControlReadModel weapon = weaponBayModel.WeaponControls[i];
                if (weapon == null)
                {
                    continue;
                }

                string key = !string.IsNullOrEmpty(weapon.ModuleInstanceID)
                    ? weapon.ModuleInstanceID
                    : i.ToString();
                if (!weapon.IsEnabled)
                {
                    ShuttleIssueReadModel issue = ShuttleIssueReadModelFactory.CreateSimpleIssue(
                        "defense:weapon-offline:" + key,
                        ShuttleIssueSeverity.Warning,
                        ShuttleIssueCategory.Defense,
                        "CT_Shuttle_Issue_DefenseWeaponOffline_Title",
                        "CT_Shuttle_Issue_DefenseWeaponOffline_Detail",
                        ShuttleIssueActionKind.OpenDefense,
                        100);
                    issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                        "CT_Shuttle_Issue_Evidence_Module",
                        this.ResolveWeaponModuleLabel(weapon)));
                    ShuttleIssueReadModelSet.AddIssueIfUnique(issues, issue);
                }
                else if (weapon.FireControlLinked && !weapon.EffectiveFireControlLinked)
                {
                    ShuttleIssueReadModel issue = ShuttleIssueReadModelFactory.CreateSimpleIssue(
                        "defense:fire-control-unavailable:" + key,
                        ShuttleIssueSeverity.Warning,
                        ShuttleIssueCategory.Defense,
                        "CT_Shuttle_Issue_DefenseFireControlUnavailable_Title",
                        "CT_Shuttle_Issue_DefenseFireControlUnavailable_Detail",
                        ShuttleIssueActionKind.OpenDefense,
                        110);
                    issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                        "CT_Shuttle_Issue_Evidence_Module",
                        this.ResolveWeaponModuleLabel(weapon)));
                    ShuttleIssueReadModelSet.AddIssueIfUnique(issues, issue);
                }
            }
        }

        private string ResolveShieldModuleLabel(ShuttleShieldStatusReadModel shield)
        {
            if (shield == null)
            {
                return "CT_Shuttle_Issue_Evidence_Shield".Translate().ToString();
            }

            return ShuttleAssemblyDisplayTextResolver.ResolveModuleDisplayName(
                shield.ModuleDefName,
                shield.ModuleLabel,
                "CT_Shuttle_Issue_Evidence_Shield".Translate().ToString());
        }

        private string ResolveWeaponModuleLabel(ShuttleWeaponControlReadModel weapon)
        {
            if (weapon == null)
            {
                return "-";
            }

            return ShuttleAssemblyDisplayTextResolver.ResolveModuleDisplayName(
                weapon.ModuleDefName,
                weapon.ModuleLabel,
                weapon.WeaponLabel);
        }

        private void AddSimpleIssue(
            List<ShuttleIssueReadModel> issues,
            string stableId,
            ShuttleIssueSeverity severity,
            ShuttleIssueCategory category,
            string titleKey,
            string detailKey,
            ShuttleIssueActionKind actionKind,
            int sortPriority)
        {
            ShuttleIssueReadModelSet.AddIssueIfUnique(
                issues,
                ShuttleIssueReadModelFactory.CreateSimpleIssue(
                    stableId,
                    severity,
                    category,
                    titleKey,
                    detailKey,
                    actionKind,
                    sortPriority));
        }
    }
}

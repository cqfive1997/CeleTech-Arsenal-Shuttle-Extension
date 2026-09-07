using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Defense
{
    /// <summary>
    /// Dispatches existing single-weapon commands and reports one aggregate result.
    /// </summary>
    internal sealed class ShuttleDefenseWeaponGroupUIActions :
        IShuttleDefenseWeaponGroupUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleDefenseWeaponGroupUIActions(
            IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool SetAllHoldFire(
            IList<ShuttleDefenseWeaponActionTarget> weapons,
            bool holdFire)
        {
            return this.ExecuteBatch(
                weapons,
                holdFire
                    ? GroupOperation.HoldFire
                    : GroupOperation.OpenFire);
        }

        public bool ReloadAll(IList<ShuttleDefenseWeaponActionTarget> weapons)
        {
            return this.ExecuteBatch(weapons, GroupOperation.Reload);
        }

        public bool CancelReloadAll(IList<ShuttleDefenseWeaponActionTarget> weapons)
        {
            return this.ExecuteBatch(weapons, GroupOperation.CancelReload);
        }

        public bool SetAllFireControlLinked(
            IList<ShuttleDefenseWeaponActionTarget> weapons,
            bool linked)
        {
            return this.ExecuteBatch(
                weapons,
                linked
                    ? GroupOperation.LinkFireControl
                    : GroupOperation.UnlinkFireControl);
        }

        public bool ClearAllForcedTargets(
            IList<ShuttleDefenseWeaponActionTarget> weapons)
        {
            return this.ExecuteBatch(weapons, GroupOperation.ClearForcedTarget);
        }

        public bool SetAllForcedTarget(
            IList<ShuttleDefenseWeaponActionTarget> weapons,
            LocalTargetInfo target)
        {
            return this.ExecuteBatch(
                weapons,
                GroupOperation.SetForcedTarget,
                target,
                ShuttleWeaponFireControlMode.ManualOnly);
        }

        public bool SetAllFireControlMode(
            IList<ShuttleDefenseWeaponActionTarget> weapons,
            ShuttleWeaponFireControlMode mode)
        {
            return this.ExecuteBatch(
                weapons,
                GroupOperation.SetFireControlMode,
                LocalTargetInfo.Invalid,
                mode);
        }

        private bool ExecuteBatch(
            IList<ShuttleDefenseWeaponActionTarget> weapons,
            GroupOperation operation)
        {
            return this.ExecuteBatch(
                weapons,
                operation,
                LocalTargetInfo.Invalid,
                ShuttleWeaponFireControlMode.ManualOnly);
        }

        private bool ExecuteBatch(
            IList<ShuttleDefenseWeaponActionTarget> weapons,
            GroupOperation operation,
            LocalTargetInfo forcedTarget,
            ShuttleWeaponFireControlMode fireControlMode)
        {
            if (this.commandExecutor == null)
            {
                return ShuttleUICommandFeedback.ShowReject(
                    ShuttleUIText.Tr("CT_Shuttle_Command_ExecutorUnavailable"));
            }

            if (weapons == null || weapons.Count == 0)
            {
                return ShuttleUICommandFeedback.ShowReject(
                    ShuttleUIText.Tr("CT_Shuttle_Defense_GroupNoApplicable"));
            }

            int applied = 0;
            int failed = 0;
            int skipped = 0;
            int ammoWeapons = 0;
            int fullMagazines = 0;
            List<string> requestedAmmoLabels = new List<string>();
            List<int> requestedAmmoCounts = new List<int>();
            string firstFailure = null;
            for (int i = 0; i < weapons.Count; i++)
            {
                ShuttleDefenseWeaponActionTarget weapon = weapons[i];
                if (operation == GroupOperation.Reload &&
                    weapon != null &&
                    weapon.HasAmmoSystem)
                {
                    ammoWeapons++;
                    if (weapon.MagazineCapacity > 0 &&
                        weapon.LoadedAmmoCount >= weapon.MagazineCapacity)
                    {
                        fullMagazines++;
                    }
                }

                IShuttleCommand command;
                if (!this.TryBuildCommand(
                    weapon,
                    operation,
                    forcedTarget,
                    fireControlMode,
                    out command))
                {
                    skipped++;
                    continue;
                }

                ShuttleCommandResult result = this.commandExecutor.Execute(command);
                if (result != null && result.Success)
                {
                    applied++;
                    if (operation == GroupOperation.Reload && weapon != null)
                    {
                        int deficit = weapon.MagazineCapacity - weapon.LoadedAmmoCount;
                        if (deficit > 0)
                        {
                            AddRequestedAmmo(
                                requestedAmmoLabels,
                                requestedAmmoCounts,
                                weapon.SelectedAmmoLabel,
                                deficit);
                        }
                    }
                    continue;
                }

                failed++;
                if (string.IsNullOrEmpty(firstFailure))
                {
                    firstFailure = result != null && !string.IsNullOrEmpty(result.Message)
                        ? result.Message
                        : ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable");
                }
            }

            if (applied == 0 && failed == 0)
            {
                if (operation == GroupOperation.Reload &&
                    ammoWeapons > 0 &&
                    fullMagazines == ammoWeapons)
                {
                    ShuttleUICommandFeedback.ShowNeutral(
                        ShuttleUIText.Tr("CT_Shuttle_WeaponAmmo_AllFull"));
                    return true;
                }

                return ShuttleUICommandFeedback.ShowReject(
                    ShuttleUIText.Tr("CT_Shuttle_Defense_GroupNoApplicable"));
            }

            if (operation == GroupOperation.Reload)
            {
                string reloadSummary = ShuttleUIText.Tr(
                    "CT_Shuttle_Defense_GroupReloadResultFormat",
                    applied,
                    BuildRequestedAmmoSummary(
                        requestedAmmoLabels,
                        requestedAmmoCounts));
                if (fullMagazines > 0)
                {
                    reloadSummary = ShuttleUIText.Tr(
                        "CT_Shuttle_Defense_GroupReloadSummaryWithFullFormat",
                        reloadSummary,
                        ShuttleUIText.Tr(
                            "CT_Shuttle_Defense_GroupReloadFullSuffix",
                            fullMagazines));
                }

                if (failed > 0)
                {
                    ShuttleUICommandFeedback.ShowReject(
                        ShuttleUIText.Tr(
                            "CT_Shuttle_Defense_GroupReloadFailureFormat",
                            reloadSummary,
                            firstFailure),
                        false);
                    return applied > 0;
                }

                ShuttleUICommandFeedback.ShowNeutral(reloadSummary);
                return applied > 0;
            }

            if (operation == GroupOperation.CancelReload)
            {
                string cancelSummary = ShuttleUIText.Tr(
                    "CT_Shuttle_Defense_GroupCancelReloadResultFormat",
                    applied);
                if (failed > 0)
                {
                    ShuttleUICommandFeedback.ShowReject(
                        ShuttleUIText.Tr(
                            "CT_Shuttle_Defense_GroupCancelReloadFailureFormat",
                            cancelSummary,
                            firstFailure),
                        false);
                    return applied > 0;
                }

                ShuttleUICommandFeedback.ShowNeutral(cancelSummary);
                return applied > 0;
            }

            string summary = ShuttleUIText.Tr(
                "CT_Shuttle_Defense_GroupResultFormat",
                applied,
                failed,
                skipped);
            if (failed > 0)
            {
                ShuttleUICommandFeedback.ShowReject(
                    ShuttleUIText.Tr(
                        "CT_Shuttle_Defense_GroupResultFailureFormat",
                        summary,
                        firstFailure),
                    false);
                return applied > 0;
            }

            ShuttleUICommandFeedback.ShowNeutral(summary);
            return applied > 0;
        }

        private static void AddRequestedAmmo(
            List<string> labels,
            List<int> counts,
            string ammoLabel,
            int count)
        {
            if (labels == null || counts == null || count <= 0)
            {
                return;
            }

            string resolvedLabel = !string.IsNullOrEmpty(ammoLabel)
                ? ammoLabel
                : ShuttleUIText.Tr("CT_Shuttle_WeaponAmmo_GenericAmmo");
            int index = labels.IndexOf(resolvedLabel);
            if (index >= 0)
            {
                counts[index] += count;
                return;
            }

            labels.Add(resolvedLabel);
            counts.Add(count);
        }

        private static string BuildRequestedAmmoSummary(
            List<string> labels,
            List<int> counts)
        {
            List<string> parts = new List<string>();
            for (int i = 0;
                labels != null && counts != null &&
                i < labels.Count && i < counts.Count;
                i++)
            {
                parts.Add(ShuttleUIText.Tr(
                    "CT_Shuttle_Defense_GroupReloadAmmoNeedFormat",
                    counts[i],
                    labels[i]));
            }

            if (parts.Count == 0)
            {
                return ShuttleUIText.Tr(
                    "CT_Shuttle_Defense_GroupReloadAmmoNeedFormat",
                    0,
                    ShuttleUIText.Tr("CT_Shuttle_WeaponAmmo_GenericAmmo"));
            }

            return string.Join(
                ShuttleUIText.Tr(
                    "CT_Shuttle_Defense_GroupReloadAmmoSeparator"),
                parts.ToArray());
        }

        private bool TryBuildCommand(
            ShuttleDefenseWeaponActionTarget weapon,
            GroupOperation operation,
            LocalTargetInfo forcedTarget,
            ShuttleWeaponFireControlMode fireControlMode,
            out IShuttleCommand command)
        {
            command = null;
            if (weapon == null || string.IsNullOrEmpty(weapon.ModuleInstanceID))
            {
                return false;
            }

            if (operation == GroupOperation.OpenFire)
            {
                if (!weapon.CanToggleHoldFire || !weapon.HoldFire)
                {
                    return false;
                }

                command = new SetWeaponHoldFireCommand(
                    weapon.ModuleInstanceID,
                    false);
                return true;
            }

            if (operation == GroupOperation.HoldFire)
            {
                if (!weapon.CanToggleHoldFire || weapon.HoldFire)
                {
                    return false;
                }

                command = new SetWeaponHoldFireCommand(
                    weapon.ModuleInstanceID,
                    true);
                return true;
            }

            if (operation == GroupOperation.Reload)
            {
                if (!weapon.HasAmmoSystem || !weapon.CanReload)
                {
                    return false;
                }

                command = new ReloadShuttleWeaponCommand(weapon.ModuleInstanceID);
                return true;
            }

            if (operation == GroupOperation.CancelReload)
            {
                if (!weapon.CanCancelReload)
                {
                    return false;
                }

                command = new CancelShuttleWeaponReloadCommand(
                    weapon.ModuleInstanceID);
                return true;
            }

            if (operation == GroupOperation.LinkFireControl)
            {
                if (!weapon.CanToggleFireControlLink || weapon.FireControlLinked)
                {
                    return false;
                }

                command = new SetWeaponFireControlLinkedCommand(
                    weapon.ModuleInstanceID,
                    true);
                return true;
            }

            if (operation == GroupOperation.UnlinkFireControl)
            {
                if (!weapon.CanToggleFireControlLink || !weapon.FireControlLinked)
                {
                    return false;
                }

                command = new SetWeaponFireControlLinkedCommand(
                    weapon.ModuleInstanceID,
                    false);
                return true;
            }

            if (operation == GroupOperation.SetForcedTarget)
            {
                if (!weapon.CanSetForcedTarget || !forcedTarget.IsValid)
                {
                    return false;
                }

                if (!forcedTarget.HasThing && !weapon.CanTargetLocations)
                {
                    return false;
                }

                command = new SetWeaponForcedTargetCommand(
                    weapon.ModuleInstanceID,
                    forcedTarget);
                return true;
            }

            if (operation == GroupOperation.SetFireControlMode)
            {
                if (!weapon.CanSetFireControlMode ||
                    string.Equals(
                        weapon.FireControlMode,
                        fireControlMode.ToString(),
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (fireControlMode == ShuttleWeaponFireControlMode.AutoDefense &&
                    !weapon.AutoFireAvailable)
                {
                    return false;
                }

                if (fireControlMode == ShuttleWeaponFireControlMode.PointDefense &&
                    !weapon.PointDefenseAvailable)
                {
                    return false;
                }

                command = new SetWeaponFireControlModeCommand(
                    weapon.ModuleInstanceID,
                    fireControlMode);
                return true;
            }

            if (!weapon.HasForcedTarget || !weapon.CanClearForcedTarget)
            {
                return false;
            }

            command = new ClearWeaponForcedTargetCommand(weapon.ModuleInstanceID);
            return true;
        }

        private enum GroupOperation
        {
            OpenFire,
            HoldFire,
            Reload,
            CancelReload,
            LinkFireControl,
            UnlinkFireControl,
            ClearForcedTarget,
            SetForcedTarget,
            SetFireControlMode
        }
    }
}

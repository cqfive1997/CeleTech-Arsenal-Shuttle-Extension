using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Defense;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings.Dialogs
{
    /// <summary>
    /// Owns the explicit commit boundary for current-shuttle shield settings.
    /// </summary>
    internal sealed class ShuttleDefenseSettingsSection : IShuttleSettingsHubSection
    {
        private readonly ShuttleDefenseSettingsModel model;
        private readonly ShuttleDefenseSettingsDrawer drawer;
        private readonly IShuttleDefenseShieldUIActions actions;
        private readonly Action onApplied;

        internal ShuttleDefenseSettingsSection(
            ShuttleControlReadModel controlModel,
            ShuttleWeaponBayReadModel weaponBayModel,
            IShuttleCommandExecutor commandExecutor,
            Action onApplied)
        {
            this.model = new ShuttleDefenseSettingsModel(controlModel, weaponBayModel);
            this.drawer = new ShuttleDefenseSettingsDrawer();
            this.actions = new ShuttleDefenseShieldUIActions(commandExecutor);
            this.onApplied = onApplied;
        }

        public ShuttleSettingsHubCommitMode CommitMode
        {
            get { return ShuttleSettingsHubCommitMode.Explicit; }
        }

        public bool CanApply
        {
            get
            {
                this.model.SanitizeWorkingValues();
                if (!this.model.HasPendingChanges)
                {
                    return false;
                }

                if (this.model.HasPendingRange &&
                    !this.actions.CanApplyShieldRange(
                        this.model.ActionTarget,
                        this.model.WorkingRange))
                {
                    return false;
                }

                return !this.model.HasPendingRechargeSpeed ||
                    this.actions.CanApplySurfaceShieldRechargeSpeed(
                        this.model.ActionTarget,
                        this.model.WorkingRechargeSpeed);
            }
        }

        public bool HasPendingChanges
        {
            get { return this.model.HasPendingChanges; }
        }

        public string ApplyDisabledReason
        {
            get
            {
                if (!this.model.HasShield)
                {
                    return ShuttleUIText.Tr("CT_Shuttle_Defense_NoShieldModule");
                }

                if (!this.model.SupportsRange && !this.model.SupportsRechargeSpeed)
                {
                    return ShuttleUIText.Tr(
                        "CT_Shuttle_ShuttleDefenseSettings_NoAdjustableSettings");
                }

                if (!this.model.HasPendingChanges)
                {
                    return ShuttleUIText.Tr(
                        "CT_Shuttle_ShuttleDefenseSettings_NoChanges");
                }

                if (this.model.HasPendingRange)
                {
                    return this.actions.GetShieldRangeTooltip(this.model.ActionTarget);
                }

                return this.actions.GetSurfaceShieldRechargeSpeedTooltip(
                    this.model.ActionTarget);
            }
        }

        public void Draw(Rect rect)
        {
            this.drawer.Draw(rect, this.model);
        }

        public void Reset()
        {
            this.model.ResetToDefaults();
        }

        public bool Apply()
        {
            this.model.SanitizeWorkingValues();
            if (!this.CanApply)
            {
                return false;
            }

            bool changed = false;
            if (this.model.HasPendingRange)
            {
                if (!this.actions.ApplyShieldRange(
                    this.model.ActionTarget,
                    this.model.WorkingRange))
                {
                    return false;
                }

                this.model.CommitRange();
                changed = true;
            }

            if (this.model.HasPendingRechargeSpeed)
            {
                if (!this.actions.ApplySurfaceShieldRechargeSpeed(
                    this.model.ActionTarget,
                    this.model.WorkingRechargeSpeed))
                {
                    this.NotifyApplied(changed);
                    return false;
                }

                this.model.CommitRechargeSpeed();
                changed = true;
            }

            this.NotifyApplied(changed);
            return changed;
        }

        public void OnHubClosed()
        {
        }

        private void NotifyApplied(bool changed)
        {
            if (changed && this.onApplied != null)
            {
                this.onApplied();
            }
        }
    }
}

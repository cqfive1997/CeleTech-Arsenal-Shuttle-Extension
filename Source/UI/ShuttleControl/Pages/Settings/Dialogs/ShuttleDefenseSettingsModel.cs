using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings.Dialogs
{
    /// <summary>
    /// Detached working values for current-shuttle shield configuration.
    /// </summary>
    internal sealed class ShuttleDefenseSettingsModel
    {
        internal readonly V3DefenseShieldPanelModel Shield;
        internal readonly ShuttleDefenseShieldActionTarget ActionTarget;

        internal float AppliedRange;
        internal float WorkingRange;
        internal float AppliedRechargeSpeed;
        internal float WorkingRechargeSpeed;

        internal ShuttleDefenseSettingsModel(
            ShuttleControlReadModel controlModel,
            ShuttleWeaponBayReadModel weaponBayModel)
        {
            V3DefenseShieldModelBuilder builder =
                new V3DefenseShieldModelBuilder(new V3DefenseTooltipBuilder());
            this.Shield = builder.BuildShield(
                weaponBayModel ?? ShuttleWeaponBayReadModel.Empty,
                controlModel ?? new ShuttleControlReadModel());
            this.ActionTarget = V3DefenseActionTargetFactory.CreateShield(this.Shield);
            this.AppliedRange = this.GetCurrentRange();
            this.WorkingRange = this.AppliedRange;
            this.AppliedRechargeSpeed = this.GetCurrentRechargeSpeed();
            this.WorkingRechargeSpeed = this.AppliedRechargeSpeed;
        }

        internal bool HasShield
        {
            get { return this.Shield != null && this.Shield.HasShield; }
        }

        internal bool SupportsRange
        {
            get
            {
                return this.HasShield &&
                    this.Shield.CanSetRange &&
                    this.Shield.MaxRange > this.Shield.MinRange;
            }
        }

        internal bool SupportsRechargeSpeed
        {
            get
            {
                return this.HasShield &&
                    this.Shield.IsSurfaceShield &&
                    this.Shield.SupportsRechargeSpeedControl &&
                    this.Shield.MaxRechargeSpeedMultiplier >
                        this.Shield.MinRechargeSpeedMultiplier;
            }
        }

        internal bool HasPendingRange
        {
            get
            {
                return this.SupportsRange &&
                    !Mathf.Approximately(this.WorkingRange, this.AppliedRange);
            }
        }

        internal bool HasPendingRechargeSpeed
        {
            get
            {
                return this.SupportsRechargeSpeed &&
                    !Mathf.Approximately(
                        this.WorkingRechargeSpeed,
                        this.AppliedRechargeSpeed);
            }
        }

        internal bool HasPendingChanges
        {
            get { return this.HasPendingRange || this.HasPendingRechargeSpeed; }
        }

        internal void SanitizeWorkingValues()
        {
            if (this.SupportsRange)
            {
                this.WorkingRange = Mathf.Clamp(
                    this.WorkingRange,
                    this.Shield.MinRange,
                    this.Shield.MaxRange);
            }

            if (this.SupportsRechargeSpeed)
            {
                this.WorkingRechargeSpeed = Mathf.Clamp(
                    this.WorkingRechargeSpeed,
                    this.Shield.MinRechargeSpeedMultiplier,
                    this.Shield.MaxRechargeSpeedMultiplier);
            }
        }

        internal void ResetToDefaults()
        {
            if (this.SupportsRange)
            {
                float fallback = this.Shield.DefaultRange >= 0f
                    ? this.Shield.DefaultRange
                    : this.AppliedRange;
                this.WorkingRange = Mathf.Clamp(
                    fallback,
                    this.Shield.MinRange,
                    this.Shield.MaxRange);
            }

            if (this.SupportsRechargeSpeed)
            {
                this.WorkingRechargeSpeed = Mathf.Clamp(
                    1f,
                    this.Shield.MinRechargeSpeedMultiplier,
                    this.Shield.MaxRechargeSpeedMultiplier);
            }
        }

        internal void CommitRange()
        {
            this.AppliedRange = this.WorkingRange;
            if (this.ActionTarget != null)
            {
                this.ActionTarget.RangeValue = this.AppliedRange;
            }

            if (this.Shield != null)
            {
                this.Shield.RangeValue = this.AppliedRange;
            }
        }

        internal void CommitRechargeSpeed()
        {
            this.AppliedRechargeSpeed = this.WorkingRechargeSpeed;
            if (this.ActionTarget != null)
            {
                this.ActionTarget.RechargeSpeedMultiplier =
                    this.AppliedRechargeSpeed;
            }

            if (this.Shield != null)
            {
                this.Shield.RechargeSpeedMultiplier =
                    this.AppliedRechargeSpeed;
            }
        }

        private float GetCurrentRange()
        {
            if (!this.SupportsRange)
            {
                return 0f;
            }

            float value = this.Shield.RangeValue >= 0f
                ? this.Shield.RangeValue
                : this.Shield.DefaultRange;
            return Mathf.Clamp(value, this.Shield.MinRange, this.Shield.MaxRange);
        }

        private float GetCurrentRechargeSpeed()
        {
            if (!this.SupportsRechargeSpeed)
            {
                return 1f;
            }

            return Mathf.Clamp(
                this.Shield.RechargeSpeedMultiplier,
                this.Shield.MinRechargeSpeedMultiplier,
                this.Shield.MaxRechargeSpeedMultiplier);
        }
    }
}

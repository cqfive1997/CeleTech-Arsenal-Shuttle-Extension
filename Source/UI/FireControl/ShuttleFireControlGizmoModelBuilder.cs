using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.FireControl
{
    /// <summary>
    /// Aggregates a detached weapon projection at a bounded selected-UI cadence.
    /// </summary>
    internal sealed class ShuttleFireControlGizmoModelBuilder
    {
        private const int RefreshIntervalTicks = 60;

        private readonly IShuttleWeaponBayReadPort readPort;
        private ShuttleWeaponBayReadModel weaponBay = ShuttleWeaponBayReadModel.Empty;
        private ShuttleFireControlGizmoModel model = new ShuttleFireControlGizmoModel();
        private int nextRefreshTick = -1;

        internal ShuttleFireControlGizmoModelBuilder(IShuttleWeaponBayReadPort readPort)
        {
            this.readPort = readPort;
        }

        internal ShuttleWeaponBayReadModel WeaponBay
        {
            get { return this.weaponBay; }
        }

        internal ShuttleFireControlGizmoModel GetModel()
        {
            return this.GetModel(false);
        }

        internal ShuttleFireControlGizmoModel RefreshForInteraction()
        {
            return this.GetModel(true);
        }

        internal void Invalidate()
        {
            this.nextRefreshTick = -1;
        }

        private ShuttleFireControlGizmoModel GetModel(bool force)
        {
            int currentTick = Find.TickManager != null
                ? Find.TickManager.TicksGame
                : 0;
            if (!force && this.nextRefreshTick >= 0 && currentTick < this.nextRefreshTick)
            {
                return this.model;
            }

            this.weaponBay = this.readPort != null
                ? this.readPort.BuildWeaponBayReadModel()
                : ShuttleWeaponBayReadModel.Empty;
            this.model = this.Build(this.weaponBay);
            this.nextRefreshTick = currentTick + RefreshIntervalTicks;
            return this.model;
        }

        private ShuttleFireControlGizmoModel Build(ShuttleWeaponBayReadModel source)
        {
            ShuttleFireControlGizmoModel result = new ShuttleFireControlGizmoModel();
            if (source == null || source.WeaponControls == null)
            {
                return result;
            }

            bool hasHoldToggle = false;
            bool hasFiringWeapon = false;
            bool hasLinkToggle = false;
            bool hasUnlinkedWeapon = false;
            bool hasMode = false;
            ShuttleWeaponFireControlMode commonMode = ShuttleWeaponFireControlMode.ManualOnly;

            for (int i = 0; i < source.WeaponControls.Count; i++)
            {
                ShuttleWeaponControlReadModel weapon = source.WeaponControls[i];
                if (weapon == null || string.IsNullOrEmpty(weapon.ModuleInstanceID))
                {
                    continue;
                }

                result.WeaponCount++;
                if (weapon.CanToggleHoldFire)
                {
                    hasHoldToggle = true;
                    result.CanOpenFire |= weapon.HoldFire;
                    result.CanHoldFire |= !weapon.HoldFire;
                    hasFiringWeapon |= !weapon.HoldFire;
                }

                result.CanSetForcedTarget |= weapon.CanSetForcedTarget;
                result.CanTargetLocations |= weapon.CanSetForcedTarget && weapon.CanTargetLocations;
                result.HasForcedTarget |= weapon.HasForcedTarget;
                result.CanClearForcedTarget |=
                    weapon.HasForcedTarget && weapon.CanClearForcedTarget;
                // Keep the map-panel action available for a useful all-full response.
                result.CanReload |= weapon.HasAmmoSystem;
                result.CanCancelReload |= weapon.CanCancelReload;
                result.ReloadActive |= weapon.ReloadInProgress || weapon.ReloadRequested;

                if (weapon.CanToggleFireControlLink)
                {
                    hasLinkToggle = true;
                    hasUnlinkedWeapon |= !weapon.FireControlLinked;
                }

                if (!weapon.CanSetFireControlMode)
                {
                    continue;
                }

                result.CanSetFireControlMode = true;
                result.AutoDefenseAvailable |= weapon.AutoFireAvailable;
                result.PointDefenseAvailable |= weapon.PointDefenseAvailable;

                ShuttleWeaponFireControlMode mode;
                if (!Enum.TryParse(weapon.FireControlMode, true, out mode))
                {
                    result.MixedFireControlModes = true;
                    continue;
                }

                if (!hasMode)
                {
                    commonMode = mode;
                    hasMode = true;
                }
                else if (commonMode != mode)
                {
                    result.MixedFireControlModes = true;
                }
            }

            result.AllControllableWeaponsHoldFire = hasHoldToggle && !hasFiringWeapon;
            result.CanToggleFireControlLink = hasLinkToggle;
            result.LinkOnNextClick = hasUnlinkedWeapon;
            result.AllLinkableWeaponsLinked = hasLinkToggle && !hasUnlinkedWeapon;
            result.HasCommonFireControlMode = hasMode && !result.MixedFireControlModes;
            result.CommonFireControlMode = commonMode;
            return result;
        }
    }
}

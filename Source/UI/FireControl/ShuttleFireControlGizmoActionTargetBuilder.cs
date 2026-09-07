using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.FireControl
{
    /// <summary>
    /// Creates command targets only after a Gizmo interaction begins.
    /// </summary>
    internal sealed class ShuttleFireControlGizmoActionTargetBuilder
    {
        internal IList<ShuttleDefenseWeaponActionTarget> Build(
            ShuttleWeaponBayReadModel weaponBay)
        {
            List<ShuttleDefenseWeaponActionTarget> targets =
                new List<ShuttleDefenseWeaponActionTarget>();
            if (weaponBay == null || weaponBay.WeaponControls == null)
            {
                return targets;
            }

            for (int i = 0; i < weaponBay.WeaponControls.Count; i++)
            {
                ShuttleWeaponControlReadModel weapon = weaponBay.WeaponControls[i];
                if (weapon == null || string.IsNullOrEmpty(weapon.ModuleInstanceID))
                {
                    continue;
                }

                targets.Add(new ShuttleDefenseWeaponActionTarget
                {
                    ModuleInstanceID = weapon.ModuleInstanceID,
                    Label = weapon.WeaponLabel,
                    ModuleLabel = weapon.ModuleLabel,
                    HoldFire = weapon.HoldFire,
                    CanToggleHoldFire = weapon.CanToggleHoldFire,
                    CanSetForcedTarget = weapon.CanSetForcedTarget,
                    CanClearForcedTarget = weapon.CanClearForcedTarget,
                    CanTargetLocations = weapon.CanTargetLocations,
                    HasForcedTarget = weapon.HasForcedTarget,
                    FireControlLinked = weapon.FireControlLinked,
                    FireControlMode = weapon.FireControlMode,
                    CanToggleFireControlLink = weapon.CanToggleFireControlLink,
                    CanSetFireControlMode = weapon.CanSetFireControlMode,
                    AutoFireAvailable = weapon.AutoFireAvailable,
                    PointDefenseAvailable = weapon.PointDefenseAvailable,
                    HasAmmoSystem = weapon.HasAmmoSystem,
                    SelectedAmmoLabel = weapon.SelectedAmmoLabel,
                    LoadedAmmoCount = weapon.LoadedAmmoCount,
                    MagazineCapacity = weapon.MagazineCapacity,
                    ReloadInProgress = weapon.ReloadInProgress,
                    ReloadRequested = weapon.ReloadRequested,
                    CanReload = weapon.CanReload,
                    CanCancelReload = weapon.CanCancelReload
                });
            }

            return targets;
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.FireControl
{
    internal sealed class ShuttleFireControlGizmoModel
    {
        internal int WeaponCount;
        internal bool CanOpenFire;
        internal bool CanHoldFire;
        internal bool AllControllableWeaponsHoldFire;
        internal bool CanSetForcedTarget;
        internal bool CanTargetLocations;
        internal bool HasForcedTarget;
        internal bool CanClearForcedTarget;
        internal bool CanReload;
        internal bool CanCancelReload;
        internal bool ReloadActive;
        internal bool CanToggleFireControlLink;
        internal bool LinkOnNextClick;
        internal bool AllLinkableWeaponsLinked;
        internal bool CanSetFireControlMode;
        internal bool AutoDefenseAvailable;
        internal bool PointDefenseAvailable;
        internal bool HasCommonFireControlMode;
        internal bool MixedFireControlModes;
        internal ShuttleWeaponFireControlMode CommonFireControlMode;

        internal bool HasWeapons
        {
            get { return this.WeaponCount > 0; }
        }
    }
}

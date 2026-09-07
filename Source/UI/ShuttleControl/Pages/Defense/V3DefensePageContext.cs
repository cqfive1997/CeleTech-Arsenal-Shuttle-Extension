using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    internal sealed class V3DefensePageContext
    {
        internal readonly IShuttleDefenseShieldUIActions ShieldActions;
        internal readonly IShuttleDefenseWeaponTargetingUIActions WeaponTargetingActions;
        internal readonly IShuttleDefenseWeaponFireControlUIActions WeaponFireControlActions;
        internal readonly IShuttleDefenseWeaponAmmoUIActions WeaponAmmoActions;
        internal readonly IShuttleDefenseWeaponLogisticsUIActions WeaponLogisticsActions;
        internal readonly IShuttleDefenseWeaponGroupUIActions WeaponGroupActions;
        internal readonly IShuttleIconService Icons;
        internal readonly IShuttlePaintPreviewService PaintPreviewProvider;
        internal readonly IShuttleTutorialTargetService TutorialTargets;

        internal V3DefensePageContext(
            IShuttleDefenseShieldUIActions shieldActions,
            IShuttleDefenseWeaponTargetingUIActions weaponTargetingActions,
            IShuttleDefenseWeaponFireControlUIActions weaponFireControlActions,
            IShuttleDefenseWeaponAmmoUIActions weaponAmmoActions,
            IShuttleDefenseWeaponLogisticsUIActions weaponLogisticsActions,
            IShuttleDefenseWeaponGroupUIActions weaponGroupActions,
            IShuttleIconService icons,
            IShuttlePaintPreviewService paintPreviewProvider,
            IShuttleTutorialTargetService tutorialTargets)
        {
            this.ShieldActions = shieldActions;
            this.WeaponTargetingActions = weaponTargetingActions;
            this.WeaponFireControlActions = weaponFireControlActions;
            this.WeaponAmmoActions = weaponAmmoActions;
            this.WeaponLogisticsActions = weaponLogisticsActions;
            this.WeaponGroupActions = weaponGroupActions;
            this.Icons = icons;
            this.PaintPreviewProvider = paintPreviewProvider;
            this.TutorialTargets = tutorialTargets;
        }
    }
}

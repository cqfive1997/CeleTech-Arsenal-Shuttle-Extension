using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings
{
    internal sealed class V3SettingsPageInputs
    {
        internal readonly ShuttleControlReadModel ControlModel;
        internal readonly ShuttleWeaponBayReadModel WeaponBayModel;

        internal V3SettingsPageInputs(
            ShuttleControlReadModel controlModel,
            ShuttleWeaponBayReadModel weaponBayModel)
        {
            this.ControlModel = controlModel ?? new ShuttleControlReadModel();
            this.WeaponBayModel = weaponBayModel ?? ShuttleWeaponBayReadModel.Empty;
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    internal sealed class V3DefensePageInputs
    {
        internal readonly ShuttleControlReadModel ControlModel;
        internal readonly ShuttleWeaponBayReadModel WeaponBayModel;

        internal V3DefensePageInputs(
            ShuttleControlReadModel controlModel,
            ShuttleWeaponBayReadModel weaponBayModel)
        {
            this.ControlModel = controlModel ?? new ShuttleControlReadModel();
            this.WeaponBayModel = weaponBayModel ?? ShuttleWeaponBayReadModel.Empty;
        }
    }
}

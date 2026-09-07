using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    internal sealed class V3DefensePageModelBuilder
    {
        private readonly V3DefenseReadModelBuilder defenseBuilder =
            new V3DefenseReadModelBuilder();

        internal void Fill(
            V3DefensePageModel model,
            V3DefensePageInputs inputs,
            string selectedWeaponId)
        {
            if (model == null)
            {
                return;
            }

            model.ControlModel = inputs != null && inputs.ControlModel != null
                ? inputs.ControlModel
                : new ShuttleControlReadModel();
            ShuttleWeaponBayReadModel weaponBayModel =
                inputs != null && inputs.WeaponBayModel != null
                ? inputs.WeaponBayModel
                : ShuttleWeaponBayReadModel.Empty;
            model.DefenseModel = this.defenseBuilder.Build(
                model.ControlModel,
                weaponBayModel,
                selectedWeaponId);
        }
    }
}

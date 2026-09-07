using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    /// <summary>
    /// Thin page projection aggregate. Weapon, shield, loader, status and metric rules remain in
    /// focused collaborators.
    /// </summary>
    internal sealed class V3DefenseReadModelBuilder
    {
        private readonly V3DefenseWeaponModelBuilder weaponBuilder;
        private readonly V3DefenseShieldModelBuilder shieldBuilder;
        private readonly V3DefenseAutoLoaderProjection autoLoaderProjection =
            new V3DefenseAutoLoaderProjection();
        private readonly V3DefenseStatusBuilder statusBuilder =
            new V3DefenseStatusBuilder();
        private readonly V3DefenseMetricModelBuilder metricBuilder =
            new V3DefenseMetricModelBuilder();

        internal V3DefenseReadModelBuilder()
        {
            V3DefenseTooltipBuilder tooltipBuilder = new V3DefenseTooltipBuilder();
            this.weaponBuilder = new V3DefenseWeaponModelBuilder(tooltipBuilder);
            this.shieldBuilder = new V3DefenseShieldModelBuilder(tooltipBuilder);
        }

        internal V3DefensePageReadModel Build(
            ShuttleControlReadModel controlModel,
            ShuttleWeaponBayReadModel weaponBayModel,
            string selectedWeaponId)
        {
            controlModel = controlModel ?? new ShuttleControlReadModel();
            weaponBayModel = weaponBayModel ?? ShuttleWeaponBayReadModel.Empty;

            V3DefensePageReadModel model = new V3DefensePageReadModel();
            model.WeaponPowerWatts =
                this.weaponBuilder.GetWeaponPowerWatts(weaponBayModel);
            model.AutoLoaderInstalled =
                this.autoLoaderProjection.HasAnyInstalled(controlModel);
            this.weaponBuilder.BuildWeapons(model, weaponBayModel, controlModel);
            this.autoLoaderProjection.ApplyWeaponAvailability(model);
            model.Hull = controlModel.Hull ?? new ShuttleHullReadModel();
            model.Shield = this.shieldBuilder.BuildShield(
                weaponBayModel,
                controlModel);
            this.statusBuilder.Apply(model, controlModel, weaponBayModel);
            this.weaponBuilder.SelectWeapon(model, selectedWeaponId);
            this.metricBuilder.BuildMetrics(model);
            return model;
        }
    }
}

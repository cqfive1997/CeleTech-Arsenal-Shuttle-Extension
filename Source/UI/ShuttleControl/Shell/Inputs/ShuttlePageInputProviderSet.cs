using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Inputs
{
    internal sealed class ShuttlePageInputProviderSet
    {
        private readonly IShuttlePageInputProvider settingsProvider =
            new V3SettingsPageInputProvider();
        private readonly IShuttlePageInputProvider processingProvider =
            new V3ProcessingPageInputProvider();
        private readonly IShuttlePageInputProvider prisonCellProvider =
            new V3PrisonCellPageInputProvider();
        private readonly IShuttlePageInputProvider crewProvider =
            new V3CrewPageInputProvider();
        private readonly IShuttlePageInputProvider defenseProvider =
            new V3DefensePageInputProvider();
        private readonly IShuttlePageInputProvider medicalProvider =
            new V3MedicalPageInputProvider();
        private readonly IShuttlePageInputProvider cargoProvider =
            new V3CargoPageInputProvider();
        private readonly IShuttlePageInputProvider externalModulesProvider =
            new V3ExternalModulesPageInputProvider();
        private readonly IShuttlePageInputProvider mainProvider =
            new V3MainPageInputProvider();

        internal void FillInputs(
            ShuttlePageDrawContext context,
            ShuttlePageInputBuildContext inputs)
        {
            if (context == null || inputs == null)
            {
                return;
            }

            this.settingsProvider.FillInputs(context, inputs);
            this.processingProvider.FillInputs(context, inputs);
            this.prisonCellProvider.FillInputs(context, inputs);
            this.crewProvider.FillInputs(context, inputs);
            this.defenseProvider.FillInputs(context, inputs);
            this.medicalProvider.FillInputs(context, inputs);
            this.cargoProvider.FillInputs(context, inputs);
            this.externalModulesProvider.FillInputs(context, inputs);
            this.mainProvider.FillInputs(context, inputs);
        }
    }
}

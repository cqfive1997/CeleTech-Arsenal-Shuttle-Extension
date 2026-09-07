using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.State;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell
{
    /// <summary>
    /// Shared draw payload for V3 pages. Ordinary pages use the typed V3
    /// read-model, action, service, and state surfaces.
    /// </summary>
    internal sealed class ShuttlePageDrawContext
    {
        internal readonly ShuttlePageReadModels ReadModels = new ShuttlePageReadModels();
        internal readonly ShuttlePageActions Actions = new ShuttlePageActions();
        internal readonly ShuttlePageServices Services = new ShuttlePageServices();
        internal readonly ShuttleGlobalChromeActions GlobalChromeActions =
            new ShuttleGlobalChromeActions();

        internal ShuttleControlState State;
        internal V3SettingsPageContext SettingsPageContext;
        internal V3ProcessingPageContext ProcessingPageContext;
        internal V3PrisonCellPageContext PrisonCellPageContext;
        internal V3CrewPageContext CrewPageContext;
        internal V3DefensePageContext DefensePageContext;
        internal V3MedicalPageContext MedicalPageContext;
        internal V3CargoPageContext CargoPageContext;
        internal V3ExternalModulesPageContext ExternalModulesPageContext;
        internal V3MainPageContext MainPageContext;

        internal V3SettingsPageInputs SettingsInputs;
        internal V3ProcessingPageInputs ProcessingInputs;
        internal V3PrisonCellPageInputs PrisonCellInputs;
        internal V3CrewPageInputs CrewInputs;
        internal V3DefensePageInputs DefenseInputs;
        internal V3MedicalPageInputs MedicalInputs;
        internal V3CargoPageInputs CargoInputs;
        internal V3ExternalModulesPageInputs ExternalModulesInputs;
        internal V3MainPageInputs MainInputs;

        internal Rot4 PreviewRotation = Rot4.East;
    }
}

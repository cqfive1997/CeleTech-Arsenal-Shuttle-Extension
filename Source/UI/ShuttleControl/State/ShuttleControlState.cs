using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Performance;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.State
{
    /// <summary>
    /// Lightweight V3 state owner for the shell page identity and page-local
    /// native state.
    /// </summary>
    internal sealed class ShuttleControlState
    {
        internal readonly V3MainPageState Main = new V3MainPageState();
        internal readonly V3CargoPageState Cargo = new V3CargoPageState();
        internal readonly V3CrewPageState Crew = new V3CrewPageState();
        internal readonly V3DefensePageState Defense = new V3DefensePageState();
        internal readonly V3MedicalPageState Medical = new V3MedicalPageState();
        internal readonly V3PrisonCellPageState PrisonCell = new V3PrisonCellPageState();
        internal readonly V3ProcessingPageState Processing = new V3ProcessingPageState();
        internal readonly V3SettingsPageState Settings = new V3SettingsPageState();
        internal readonly V3PerformancePageState Performance = new V3PerformancePageState();
        internal readonly V3ExternalModulesPageState ExternalModules = new V3ExternalModulesPageState();
        internal ShuttleControlPageId CurrentPage;
        internal ShuttleControlPageId LastPage;
        internal bool IsDirty;

        internal ShuttleControlState()
            : this(ShuttleControlPageId.Main)
        {
        }

        internal ShuttleControlState(ShuttleControlPageId initialPage)
        {
            this.SetCurrentPage(initialPage);
            this.LastPage = this.CurrentPage;
        }

        internal void SetCurrentPage(ShuttleControlPageId page)
        {
            if (!Enum.IsDefined(typeof(ShuttleControlPageId), page))
            {
                page = ShuttleControlPageId.Main;
            }

            if (this.CurrentPage != page)
            {
                this.IsDirty = true;
            }

            this.CurrentPage = page;
        }

        internal void ClearDirty()
        {
            this.IsDirty = false;
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages;
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

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell
{
    /// <summary>
    /// Cached page lookup for the V3 shell. Pages are constructed once so page
    /// caches and scroll state are not churned during IMGUI redraws.
    /// </summary>
    internal sealed class ShuttlePageRegistry
    {
        private readonly IShuttleControlPageV3 mainPage = new V3MainPage();
        private readonly IShuttleControlPageV3 cargoPage = new V3CargoPage();
        private readonly IShuttleControlPageV3 crewPage = new V3CrewPage();
        private readonly IShuttleControlPageV3 defensePage = new V3DefensePage();
        private readonly IShuttleControlPageV3 medicalPage = new V3MedicalPage();
        private readonly IShuttleControlPageV3 prisonCellPage = new V3PrisonCellPage();
        private readonly IShuttleControlPageV3 processingPage = new V3ProcessingPage();
        private readonly IShuttleControlPageV3 externalModulesPage = new V3ExternalModulesPage();
        private readonly IShuttleControlPageV3 performancePage = new V3PerformancePage();
        private readonly IShuttleControlPageV3 settingsPage = new V3SettingsPage();
        private readonly IShuttleControlPageV3[] pages;
        private readonly ShuttleControlPageId[] pageIds;

        internal ShuttlePageRegistry()
        {
            this.pageIds = new ShuttleControlPageId[]
            {
                ShuttleControlPageId.Main,
                ShuttleControlPageId.Cargo,
                ShuttleControlPageId.Crew,
                ShuttleControlPageId.Defense,
                ShuttleControlPageId.Medical,
                ShuttleControlPageId.PrisonCell,
                ShuttleControlPageId.Processing,
                ShuttleControlPageId.ExternalModules,
                ShuttleControlPageId.Performance,
                ShuttleControlPageId.Settings
            };
            this.pages = new IShuttleControlPageV3[]
            {
                this.mainPage,
                this.cargoPage,
                this.crewPage,
                this.defensePage,
                this.medicalPage,
                this.prisonCellPage,
                this.processingPage,
                this.externalModulesPage,
                this.performancePage,
                this.settingsPage
            };
        }

        internal int PageCount
        {
            get { return this.pageIds.Length; }
        }

        internal ShuttleControlPageId GetPageIdAt(int index)
        {
            if (index < 0 || index >= this.pageIds.Length)
            {
                return ShuttleControlPageId.Main;
            }

            return this.pageIds[index];
        }

        internal bool IsRegistered(ShuttleControlPageId page)
        {
            for (int i = 0; i < this.pageIds.Length; i++)
            {
                if (this.pageIds[i] == page)
                {
                    return true;
                }
            }

            return false;
        }

        internal IShuttleControlPageV3 GetPage(ShuttleControlPageId page)
        {
            switch (page)
            {
                case ShuttleControlPageId.Crew:
                    return this.crewPage;
                case ShuttleControlPageId.Defense:
                    return this.defensePage;
                case ShuttleControlPageId.Medical:
                    return this.medicalPage;
                case ShuttleControlPageId.PrisonCell:
                    return this.prisonCellPage;
                case ShuttleControlPageId.Cargo:
                    return this.cargoPage;
                case ShuttleControlPageId.Processing:
                    return this.processingPage;
                case ShuttleControlPageId.ExternalModules:
                    return this.externalModulesPage;
                case ShuttleControlPageId.Settings:
                    return this.settingsPage;
                case ShuttleControlPageId.Performance:
                    return this.performancePage;
                case ShuttleControlPageId.Main:
                    return this.mainPage;
                default:
                    return this.mainPage;
            }
        }

        internal void ReleasePageCache(ShuttlePageDrawContext context)
        {
            for (int i = 0; i < this.pages.Length; i++)
            {
                IShuttleControlPageV3 page = this.pages[i];
                if (page != null)
                {
                    page.OnExit(context);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.State;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Navigation
{
    internal sealed class ShuttleControlPageNavigation
    {
        private readonly ShuttleControlState state;
        private readonly ShuttlePageRegistry pageRegistry;
        private readonly ShuttleControlReadModelCache readModelCache;
        private readonly Action markDirty;
        private readonly Dictionary<ShuttleControlPageId, ShuttleControlPageDef> defsByPage =
            new Dictionary<ShuttleControlPageId, ShuttleControlPageDef>();

        private bool defsLoaded;
        private ShuttleControlPageId lastUnavailablePageNotified = ShuttleControlPageId.Main;

        internal ShuttleControlPageNavigation(
            ShuttleControlState state,
            ShuttlePageRegistry pageRegistry,
            ShuttleControlReadModelCache readModelCache,
            ShuttleUIModalService modalService,
            Action markDirty)
        {
            this.state = state ?? new ShuttleControlState();
            this.pageRegistry = pageRegistry ?? new ShuttlePageRegistry();
            this.readModelCache = readModelCache;
            this.markDirty = markDirty;

            if (modalService != null)
            {
                modalService.OpenPageMenuAction = this.OpenPageMenu;
            }
        }

        internal bool TrySwitchToPage(ShuttleControlPageId page)
        {
            if (!Enum.IsDefined(typeof(ShuttleControlPageId), page) ||
                !this.pageRegistry.IsRegistered(page))
            {
                return false;
            }

            if (this.state.CurrentPage == page)
            {
                return true;
            }

            this.state.SetCurrentPage(page);
            this.MarkDirty();
            return true;
        }

        internal ShuttleControlPageId EnsureCurrentPageAvailable(
            ShuttleControlReadModel controlModel)
        {
            ShuttleControlPageId currentPage = this.state.CurrentPage;
            if (this.IsPageAvailable(currentPage, controlModel))
            {
                if (currentPage == this.lastUnavailablePageNotified)
                {
                    this.lastUnavailablePageNotified = ShuttleControlPageId.Main;
                }

                return currentPage;
            }

            this.state.SetCurrentPage(ShuttleControlPageId.Main);
            this.MarkDirty();
            if (currentPage == ShuttleControlPageId.Main ||
                currentPage == this.lastUnavailablePageNotified)
            {
                return this.state.CurrentPage;
            }

            this.lastUnavailablePageNotified = currentPage;
            Messages.Message(
                "CT_Shuttle_UI_PageUnavailableModuleMissing".Translate().ToString(),
                MessageTypeDefOf.NeutralEvent,
                false);
            return this.state.CurrentPage;
        }

        internal void OpenPageMenu()
        {
            ShuttleControlReadModel controlModel = this.RefreshControlModelForPageMenu();
            List<PageMenuEntry> entries = this.BuildPageMenuEntries(controlModel);
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            for (int i = 0; i < entries.Count; i++)
            {
                this.AddPageMenuOption(options, entries[i]);
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private List<PageMenuEntry> BuildPageMenuEntries(ShuttleControlReadModel controlModel)
        {
            List<PageMenuEntry> entries = new List<PageMenuEntry>();
            for (int i = 0; i < this.pageRegistry.PageCount; i++)
            {
                this.AddPageMenuEntry(
                    entries,
                    this.pageRegistry.GetPageIdAt(i),
                    i * 10,
                    controlModel);
            }

            entries.Sort(delegate(PageMenuEntry left, PageMenuEntry right)
            {
                int orderCompare = left.Order.CompareTo(right.Order);
                if (orderCompare != 0)
                {
                    return orderCompare;
                }

                return left.Page.CompareTo(right.Page);
            });
            return entries;
        }

        private void AddPageMenuEntry(
            List<PageMenuEntry> entries,
            ShuttleControlPageId page,
            int fallbackOrder,
            ShuttleControlReadModel controlModel)
        {
            if (entries == null ||
                !this.pageRegistry.IsRegistered(page) ||
                !this.IsPageAvailable(page, controlModel))
            {
                return;
            }

            ShuttleControlPageDef def = this.GetPageDef(page);
            if (def != null && !def.enabled)
            {
                return;
            }

            string label = this.GetDefaultPageLabel(page);
            int order = def != null ? def.order : fallbackOrder;
            entries.Add(new PageMenuEntry(page, label, order));
        }

        private void AddPageMenuOption(List<FloatMenuOption> options, PageMenuEntry entry)
        {
            if (options == null)
            {
                return;
            }

            string label = entry.Page == this.state.CurrentPage
                ? "* " + entry.Label
                : entry.Label;
            options.Add(new FloatMenuOption(label, delegate
            {
                this.TrySwitchToPage(entry.Page);
            }));
        }

        private bool IsPageAvailable(
            ShuttleControlPageId page,
            ShuttleControlReadModel controlModel)
        {
            if (page == ShuttleControlPageId.Main ||
                page == ShuttleControlPageId.Settings)
            {
                return true;
            }

            if (page == ShuttleControlPageId.Performance)
            {
                CeleTechShuttleModSettings settings = CeleTechShuttleMod.Settings;
                return settings != null && settings.ShowPerformanceDashboard;
            }

            ShuttleControlPageAvailabilityReadModel availability =
                controlModel != null ? controlModel.PageAvailability : null;
            if (availability == null)
            {
                return false;
            }

            if (page == ShuttleControlPageId.Crew)
            {
                return availability.Crew;
            }

            if (page == ShuttleControlPageId.Cargo)
            {
                return availability.Loading;
            }

            if (page == ShuttleControlPageId.Defense)
            {
                return availability.Defense;
            }

            if (page == ShuttleControlPageId.Medical)
            {
                return availability.Medical;
            }

            if (page == ShuttleControlPageId.PrisonCell)
            {
                return availability.PrisonCell;
            }

            if (page == ShuttleControlPageId.Processing)
            {
                return availability.Processing;
            }

            if (page == ShuttleControlPageId.ExternalModules)
            {
                return availability.ExternalModules;
            }

            return false;
        }

        private ShuttleControlReadModel RefreshControlModelForPageMenu()
        {
            return this.readModelCache != null
                ? this.readModelCache.RefreshControlModelForPageMenu()
                : new ShuttleControlReadModel();
        }

        private ShuttleControlPageDef GetPageDef(ShuttleControlPageId page)
        {
            this.EnsureDefsLoaded();
            ShuttleControlPageDef def;
            return this.defsByPage.TryGetValue(page, out def)
                ? def
                : null;
        }

        private void EnsureDefsLoaded()
        {
            if (this.defsLoaded)
            {
                return;
            }

            this.defsLoaded = true;
            List<ShuttleControlPageDef> defs = GetDefsSafe();
            for (int i = 0; i < defs.Count; i++)
            {
                ShuttleControlPageDef def = defs[i];
                if (def == null)
                {
                    continue;
                }

                ShuttleControlPageId page = ToPageId(def.pageKind);
                this.defsByPage[page] = def;
            }
        }

        private static List<ShuttleControlPageDef> GetDefsSafe()
        {
            try
            {
                List<ShuttleControlPageDef> defs =
                    DefDatabase<ShuttleControlPageDef>.AllDefsListForReading;
                return defs ?? new List<ShuttleControlPageDef>();
            }
            catch
            {
                return new List<ShuttleControlPageDef>();
            }
        }

        private static ShuttleControlPageId ToPageId(ShuttleControlPageKind kind)
        {
            if (kind == ShuttleControlPageKind.Crew)
            {
                return ShuttleControlPageId.Crew;
            }

            if (kind == ShuttleControlPageKind.Defense)
            {
                return ShuttleControlPageId.Defense;
            }

            if (kind == ShuttleControlPageKind.Medical)
            {
                return ShuttleControlPageId.Medical;
            }

            if (kind == ShuttleControlPageKind.PrisonCell)
            {
                return ShuttleControlPageId.PrisonCell;
            }

            if (kind == ShuttleControlPageKind.Loading)
            {
                return ShuttleControlPageId.Cargo;
            }

            if (kind == ShuttleControlPageKind.Processing)
            {
                return ShuttleControlPageId.Processing;
            }

            if (kind == ShuttleControlPageKind.ExternalModules)
            {
                return ShuttleControlPageId.ExternalModules;
            }

            if (kind == ShuttleControlPageKind.Settings)
            {
                return ShuttleControlPageId.Settings;
            }

            if (kind == ShuttleControlPageKind.Performance)
            {
                return ShuttleControlPageId.Performance;
            }

            return ShuttleControlPageId.Main;
        }

        private string GetDefaultPageLabel(ShuttleControlPageId page)
        {
            if (page == ShuttleControlPageId.Crew)
            {
                return "CT_Shuttle_Page_Crew".Translate().ToString();
            }

            if (page == ShuttleControlPageId.Defense)
            {
                return "CT_Shuttle_Page_Defense".Translate().ToString();
            }

            if (page == ShuttleControlPageId.Medical)
            {
                return "CT_Shuttle_Page_Medical".Translate().ToString();
            }

            if (page == ShuttleControlPageId.PrisonCell)
            {
                return "CT_Shuttle_PrisonCell_PageTitle".Translate().ToString();
            }

            if (page == ShuttleControlPageId.Cargo)
            {
                return "CT_Shuttle_Page_Loading".Translate().ToString();
            }

            if (page == ShuttleControlPageId.Processing)
            {
                return "CT_Shuttle_Page_Processing".Translate().ToString();
            }

            if (page == ShuttleControlPageId.ExternalModules)
            {
                return "CT_Shuttle_Page_ExternalModules".Translate().ToString();
            }

            if (page == ShuttleControlPageId.Settings)
            {
                return "CT_Shuttle_Settings_PageTitle".Translate().ToString();
            }

            if (page == ShuttleControlPageId.Performance)
            {
                return "CT_Shuttle_Performance_Title".Translate().ToString();
            }

            return "CT_Shuttle_Page_Main".Translate().ToString();
        }

        private void MarkDirty()
        {
            if (this.markDirty != null)
            {
                this.markDirty();
            }
        }

        private struct PageMenuEntry
        {
            internal readonly ShuttleControlPageId Page;
            internal readonly string Label;
            internal readonly int Order;

            internal PageMenuEntry(ShuttleControlPageId page, string label, int order)
            {
                this.Page = page;
                this.Label = label;
                this.Order = order;
            }
        }
    }
}

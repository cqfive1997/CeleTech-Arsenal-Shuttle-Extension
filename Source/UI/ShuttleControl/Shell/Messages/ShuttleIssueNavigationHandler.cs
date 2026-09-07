using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.State;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell
{
    internal sealed class ShuttleIssueNavigationHandler
    {
        internal bool Execute(
            V3SharedMessageRow row,
            ShuttlePageDrawContext context)
        {
            if (row == null ||
                context == null ||
                context.State == null ||
                row.ActionKind == ShuttleIssueActionKind.None)
            {
                return false;
            }

            switch (row.ActionKind)
            {
                case ShuttleIssueActionKind.OpenCargo:
                case ShuttleIssueActionKind.OpenLoading:
                    this.OpenCargo(context, row.NavigationTarget);
                    return true;
                case ShuttleIssueActionKind.OpenAssembly:
                    this.OpenAssembly(context, row.NavigationTarget);
                    return true;
                case ShuttleIssueActionKind.OpenLaunch:
                    this.OpenLaunchDiagnostics(context);
                    return true;
                case ShuttleIssueActionKind.OpenDefense:
                    this.SwitchToPage(context, ShuttleControlPageId.Defense);
                    return true;
                case ShuttleIssueActionKind.OpenMedical:
                    this.SwitchToPage(context, ShuttleControlPageId.Medical);
                    return true;
                case ShuttleIssueActionKind.OpenPrisonCell:
                    this.SwitchToPage(context, ShuttleControlPageId.PrisonCell);
                    return true;
                case ShuttleIssueActionKind.OpenProcessing:
                    this.SwitchToPage(context, ShuttleControlPageId.Processing);
                    return true;
                case ShuttleIssueActionKind.OpenExternalModule:
                    this.OpenExternalModule(context, row.NavigationTarget);
                    return true;
            }

            return false;
        }

        private void OpenCargo(
            ShuttlePageDrawContext context,
            ShuttleIssueNavigationTarget target)
        {
            if (context.State.Cargo != null &&
                target != null &&
                target.Kind == ShuttleIssueNavigationTargetKind.CargoBay &&
                !string.IsNullOrEmpty(target.CargoBayKey))
            {
                context.State.Cargo.SelectedBayKey = target.CargoBayKey;
            }

            this.SwitchToPage(context, ShuttleControlPageId.Cargo);
        }

        private void OpenAssembly(
            ShuttlePageDrawContext context,
            ShuttleIssueNavigationTarget target)
        {
            target = this.ResolveAssemblyTarget(context, target);
            if (context.State.Main != null && target != null)
            {
                if (!string.IsNullOrEmpty(target.SegmentSlotID))
                {
                    context.State.Main.SelectedSegmentSlotID = target.SegmentSlotID;
                    context.State.Main.FocusedSegmentSlotID = target.SegmentSlotID;
                }

                if (target.Kind == ShuttleIssueNavigationTargetKind.ModuleSlot)
                {
                    context.State.Main.SelectedModuleSlotID = target.ModuleSlotID;
                    context.State.Main.FocusedModuleSlotID = target.ModuleSlotID;
                }

                this.StartAssemblyFocus(context.State);
            }

            this.SwitchToPage(context, ShuttleControlPageId.Main);
        }

        private ShuttleIssueNavigationTarget ResolveAssemblyTarget(
            ShuttlePageDrawContext context,
            ShuttleIssueNavigationTarget target)
        {
            ShuttleControlReadModel model =
                context != null && context.ReadModels != null
                    ? context.ReadModels.ControlModel
                    : null;
            if (target == null || model == null)
            {
                return target;
            }

            if (target.Kind == ShuttleIssueNavigationTargetKind.ModuleSlot)
            {
                ShuttleControlSegmentSlotModel segment;
                ShuttleControlModuleSlotModel moduleSlot;
                if (ShuttleIssueSlotReferenceResolver.TryResolveModuleSlot(
                    model,
                    target.SegmentSlotID,
                    target.ModuleSlotID,
                    out segment,
                    out moduleSlot) &&
                    segment != null &&
                    moduleSlot != null)
                {
                    return new ShuttleIssueNavigationTarget
                    {
                        Kind = ShuttleIssueNavigationTargetKind.ModuleSlot,
                        SegmentSlotID = segment.SlotID,
                        ModuleSlotID = moduleSlot.SlotID
                    };
                }
            }

            if (!string.IsNullOrEmpty(target.SegmentSlotID))
            {
                ShuttleControlSegmentSlotModel segment;
                if (ShuttleIssueSlotReferenceResolver.TryResolveSegmentSlot(
                    model,
                    target.SegmentSlotID,
                    out segment) &&
                    segment != null)
                {
                    return new ShuttleIssueNavigationTarget
                    {
                        Kind = target.Kind,
                        SegmentSlotID = segment.SlotID,
                        ModuleSlotID = target.ModuleSlotID,
                        CargoBayKey = target.CargoBayKey,
                        ModuleInstanceID = target.ModuleInstanceID,
                        RuntimeSystemKey = target.RuntimeSystemKey
                    };
                }
            }

            return target;
        }

        private void OpenLaunchDiagnostics(ShuttlePageDrawContext context)
        {
            V3SharedMessagePanelState state = this.GetCurrentMessagePanelState(context.State);
            if (state != null)
            {
                state.CurrentTab = V3SharedMessagePanelTab.LaunchDiagnostics;
                state.Expanded = false;
            }

            this.MarkDirty(context);
        }

        private void OpenExternalModule(
            ShuttlePageDrawContext context,
            ShuttleIssueNavigationTarget target)
        {
            if (context.State.ExternalModules != null &&
                target != null &&
                !string.IsNullOrEmpty(target.ModuleInstanceID) &&
                !string.IsNullOrEmpty(target.RuntimeSystemKey))
            {
                context.State.ExternalModules.SelectedModuleInstanceID =
                    target.ModuleInstanceID;
                context.State.ExternalModules.SelectedRuntimeSystemKey =
                    target.RuntimeSystemKey;
            }

            this.SwitchToPage(context, ShuttleControlPageId.ExternalModules);
        }

        private void SwitchToPage(
            ShuttlePageDrawContext context,
            ShuttleControlPageId page)
        {
            context.State.SetCurrentPage(page);
            this.CollapseMessagePanels(context.State);
            this.MarkDirty(context);
        }

        private void StartAssemblyFocus(ShuttleControlState state)
        {
            int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            state.Main.AssemblyFocusStartTick = tick;
            state.Main.AssemblyFocusUntilTick = tick + 180;
        }

        private void CollapseMessagePanels(ShuttleControlState state)
        {
            this.CollapseMessagePanel(state.Main.MessagePanelState);
            this.CollapseMessagePanel(state.Cargo.MessagePanelState);
            this.CollapseMessagePanel(state.Crew.MessagePanelState);
            this.CollapseMessagePanel(state.Defense.MessagePanelState);
            this.CollapseMessagePanel(state.Medical.MessagePanelState);
            this.CollapseMessagePanel(state.PrisonCell.MessagePanelState);
            this.CollapseMessagePanel(state.Processing.MessagePanelState);
            this.CollapseMessagePanel(state.Settings.MessagePanelState);
            this.CollapseMessagePanel(state.ExternalModules.MessagePanelState);
        }

        private void CollapseMessagePanel(V3SharedMessagePanelState state)
        {
            if (state != null)
            {
                state.Expanded = false;
            }
        }

        private V3SharedMessagePanelState GetCurrentMessagePanelState(
            ShuttleControlState state)
        {
            if (state.CurrentPage == ShuttleControlPageId.Cargo)
            {
                return state.Cargo.MessagePanelState;
            }

            if (state.CurrentPage == ShuttleControlPageId.Crew)
            {
                return state.Crew.MessagePanelState;
            }

            if (state.CurrentPage == ShuttleControlPageId.Defense)
            {
                return state.Defense.MessagePanelState;
            }

            if (state.CurrentPage == ShuttleControlPageId.Medical)
            {
                return state.Medical.MessagePanelState;
            }

            if (state.CurrentPage == ShuttleControlPageId.PrisonCell)
            {
                return state.PrisonCell.MessagePanelState;
            }

            if (state.CurrentPage == ShuttleControlPageId.Processing)
            {
                return state.Processing.MessagePanelState;
            }

            if (state.CurrentPage == ShuttleControlPageId.ExternalModules)
            {
                return state.ExternalModules.MessagePanelState;
            }

            if (state.CurrentPage == ShuttleControlPageId.Settings)
            {
                return state.Settings.MessagePanelState;
            }

            return state.Main.MessagePanelState;
        }

        private void MarkDirty(ShuttlePageDrawContext context)
        {
            if (context.Actions != null && context.Actions.MarkDirty != null)
            {
                context.Actions.MarkDirty();
            }
            else if (context.State != null)
            {
                context.State.IsDirty = true;
            }
        }
    }
}

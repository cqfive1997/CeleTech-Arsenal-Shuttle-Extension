using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Main
{
    internal sealed class ShuttleMainModuleInstallSelectionHandler
    {
        private readonly IShuttleCommandExecutor commandExecutor;
        private readonly IShuttleModuleInstallEligibilityReadPort moduleInstallEligibilityPort;
        private readonly ShuttleMainHullPlatingMaterialMenu hullPlatingMaterialMenu;

        internal ShuttleMainModuleInstallSelectionHandler(
            IShuttleCommandExecutor commandExecutor,
            IShuttleModuleInstallEligibilityReadPort moduleInstallEligibilityPort)
        {
            this.commandExecutor = commandExecutor;
            this.moduleInstallEligibilityPort = moduleInstallEligibilityPort;
            this.hullPlatingMaterialMenu =
                new ShuttleMainHullPlatingMaterialMenu(commandExecutor);
        }

        internal void InstallSelectedCandidate(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            V3MainInstallCandidateModel candidate,
            bool replacing)
        {
            if (candidate == null || string.IsNullOrEmpty(candidate.Id))
            {
                return;
            }

            ShuttleModuleBaseDef moduleDef =
                DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(candidate.Id);
            string failureReason;
            if (!ShuttleMainInstallPolicy.CanInstallModuleDefIntoSlot(
                this.moduleInstallEligibilityPort,
                segment,
                moduleSlot,
                moduleDef,
                replacing,
                out failureReason))
            {
                this.ShowReject(!string.IsNullOrEmpty(failureReason)
                    ? failureReason
                    : replacing
                        ? ShuttleUIText.Tr("CT_Shuttle_UI_CannotReplaceModule")
                        : ShuttleUIText.Tr("CT_Shuttle_UI_NoCompatibleModuleDefs"));
                return;
            }

            ShuttleHullPlatingModuleDef hullDef =
                moduleDef as ShuttleHullPlatingModuleDef;
            if (hullDef != null)
            {
                this.hullPlatingMaterialMenu.Open(
                    segment,
                    moduleSlot,
                    hullDef,
                    replacing);
                return;
            }

            this.BeginModuleConstruction(segment, moduleSlot, moduleDef, replacing);
        }

        private void BeginModuleConstruction(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            ShuttleModuleBaseDef moduleDef,
            bool replacing)
        {
            if (this.commandExecutor == null ||
                segment == null ||
                moduleSlot == null ||
                moduleDef == null ||
                string.IsNullOrEmpty(segment.InstalledSegmentInstanceID) ||
                string.IsNullOrEmpty(moduleSlot.SlotID))
            {
                this.ShowReject(ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable"));
                return;
            }

            ShuttleCommandResult result = replacing
                ? this.commandExecutor.Execute(
                    new BeginModuleReplacementConstructionCommand(
                        segment.InstalledSegmentInstanceID,
                        moduleSlot.SlotID,
                        moduleDef.defName))
                : this.commandExecutor.Execute(
                    new BeginModuleConstructionCommand(
                        segment.InstalledSegmentInstanceID,
                        moduleSlot.SlotID,
                        moduleDef.defName));
            this.ShowResult(result);
        }

        private bool ShowResult(ShuttleCommandResult result)
        {
            return ShuttleUICommandFeedback.ShowResult(result, MessageTypeDefOf.PositiveEvent);
        }

        private void ShowReject(string message)
        {
            ShuttleUICommandFeedback.ShowReject(message, false);
        }
    }
}

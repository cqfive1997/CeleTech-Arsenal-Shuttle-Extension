using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Cargo
{
    internal sealed class ShuttleCargoStackUnloadUIActions :
        IShuttleCargoStackUnloadUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;
        private readonly Action markDirty;

        internal ShuttleCargoStackUnloadUIActions(
            IShuttleCommandExecutor commandExecutor,
            Action markDirty)
        {
            this.commandExecutor = commandExecutor;
            this.markDirty = markDirty;
        }

        public bool CanUnloadStack(ShuttleCargoStackActionTarget target)
        {
            return this.CanUnloadStack(target, target != null ? target.StackCount : 0);
        }

        public bool CanUnloadStack(ShuttleCargoStackActionTarget target, int count)
        {
            return this.commandExecutor != null &&
                this.HasValidSourceMembers(target) &&
                count > 0 &&
                target.StackCount > 0 &&
                count <= target.StackCount;
        }

        public bool UnloadStack(ShuttleCargoStackActionTarget target)
        {
            return this.UnloadStack(target, target != null ? target.StackCount : 0);
        }

        public bool UnloadStack(ShuttleCargoStackActionTarget target, int count)
        {
            if (!this.CanUnloadStack(target, count))
            {
                return this.ShowReject(this.GetUnloadTooltip(target));
            }

            IShuttleCommand command = this.BuildUnloadCommand(target, count);
            if (command == null)
            {
                return this.ShowReject(this.GetUnloadTooltip(target));
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(command);
            return this.ShowResult(result);
        }

        public string GetUnloadTooltip(ShuttleCargoStackActionTarget target)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (!this.HasValidSourceMembers(target))
            {
                return this.Tr("CT_Shuttle_Cargo_Action_UnloadQuantityUnavailable");
            }

            return this.Tr("CT_Shuttle_Cargo_Action_UnloadAllTooltip");
        }

        public string GetQuantityEjectTooltip()
        {
            return this.Tr("CT_Shuttle_Cargo_Action_EjectByCountTooltip");
        }

        public void OpenQuantityUnload(ShuttleCargoStackActionTarget target)
        {
            Find.WindowStack.Add(new Dialog_ShuttleCargoQuantityUnloadV3(
                target,
                this));
        }

        private bool HasValidSourceMembers(ShuttleCargoStackActionTarget target)
        {
            return target != null &&
                (ShuttleCargoStackMemberActionUtility.HasValidMembers(
                    target,
                    ShuttleCargoStackActionSourceKind.LoadedCargo) ||
                 ShuttleCargoStackMemberActionUtility.HasValidMembers(
                    target,
                    ShuttleCargoStackActionSourceKind.RefrigeratedCargo));
        }

        private IShuttleCommand BuildUnloadCommand(
            ShuttleCargoStackActionTarget target,
            int count)
        {
            List<ShuttleCargoStackMemberActionTarget> slices;
            if (!ShuttleCargoStackMemberActionUtility.TryBuildSlices(
                target,
                count,
                out slices))
            {
                return null;
            }

            if (target.SourceKind == ShuttleCargoStackActionSourceKind.LoadedCargo)
            {
                List<UnloadLoadedCargoBayEntry> entries =
                    new List<UnloadLoadedCargoBayEntry>();
                for (int i = 0; i < slices.Count; i++)
                {
                    ShuttleCargoStackMemberActionTarget member = slices[i];
                    entries.Add(new UnloadLoadedCargoBayEntry(
                        member.TransporterIndex,
                        member.LoadedIndex,
                        member.ThingIDNumber,
                        member.DefName,
                        member.StackCount));
                }

                return new UnloadLoadedCargoBayCommand(entries);
            }

            if (target.SourceKind == ShuttleCargoStackActionSourceKind.RefrigeratedCargo)
            {
                string moduleInstanceID =
                    ShuttleCargoStackMemberActionUtility.ResolveColdModuleInstanceID(
                        target,
                        slices);
                if (string.IsNullOrEmpty(moduleInstanceID))
                {
                    return null;
                }

                List<UnloadRefrigeratedCargoBayEntry> entries =
                    new List<UnloadRefrigeratedCargoBayEntry>();
                for (int i = 0; i < slices.Count; i++)
                {
                    ShuttleCargoStackMemberActionTarget member = slices[i];
                    entries.Add(new UnloadRefrigeratedCargoBayEntry(
                        member.ColdIndex,
                        member.ThingIDNumber,
                        member.DefName,
                        member.StackCount));
                }

                return new UnloadRefrigeratedCargoBayCommand(
                    moduleInstanceID,
                    entries);
            }

            return null;
        }

        private bool ShowResult(ShuttleCommandResult result)
        {
            bool success = ShuttleUICommandFeedback.ShowResult(result);
            if (success && this.markDirty != null)
            {
                this.markDirty();
            }

            return success;
        }

        private bool ShowReject(string message)
        {
            return ShuttleUICommandFeedback.ShowReject(message, false);
        }

        private string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }
    }
}

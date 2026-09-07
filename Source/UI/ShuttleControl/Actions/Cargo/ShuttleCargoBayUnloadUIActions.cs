using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Cargo
{
    internal sealed class ShuttleCargoBayUnloadUIActions :
        IShuttleCargoBayUnloadUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;
        private readonly Action markDirty;

        internal ShuttleCargoBayUnloadUIActions(
            IShuttleCommandExecutor commandExecutor,
            Action markDirty)
        {
            this.commandExecutor = commandExecutor;
            this.markDirty = markDirty;
        }

        public bool CanUnloadBay(ShuttleCargoBayActionTarget bay)
        {
            return this.commandExecutor != null &&
                ShuttleCargoBayUnloadPolicy.CanUnloadBay(bay);
        }

        public string GetUnloadBayTooltip(ShuttleCargoBayActionTarget bay)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            return ShuttleCargoBayUnloadPolicy.GetUnloadBayTooltip(bay);
        }

        public void ConfirmUnloadBay(
            ShuttleCargoBayActionTarget bay,
            Action onSuccess)
        {
            if (!this.CanUnloadBay(bay))
            {
                this.ShowReject(this.GetUnloadBayTooltip(bay));
                return;
            }

            int stackCount = ShuttleCargoBayUnloadPolicy.CountUnloadableStacks(bay);
            int thingCount = ShuttleCargoBayUnloadPolicy.CountUnloadableThings(bay);
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                this.Tr(
                    "CT_Shuttle_Cargo_Action_UnloadBayConfirm",
                    this.GetBayLabel(bay),
                    stackCount,
                    thingCount),
                delegate
                {
                    this.ExecuteUnloadBay(bay, onSuccess);
                },
                false,
                null));
        }

        private void ExecuteUnloadBay(
            ShuttleCargoBayActionTarget bay,
            Action onSuccess)
        {
            IShuttleCommand command = this.BuildCommand(bay);
            if (command == null)
            {
                this.ShowReject(this.GetUnloadBayTooltip(bay));
                return;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(command);
            bool success = ShuttleUICommandFeedback.ShowResult(result);
            if (!success)
            {
                return;
            }

            if (this.markDirty != null)
            {
                this.markDirty();
            }

            if (onSuccess != null)
            {
                onSuccess();
            }
        }

        private IShuttleCommand BuildCommand(ShuttleCargoBayActionTarget bay)
        {
            if (bay == null)
            {
                return null;
            }

            return bay.IsRefrigerated
                ? (IShuttleCommand)this.BuildRefrigeratedCommand(bay)
                : this.BuildLoadedCargoCommand(bay);
        }

        private UnloadLoadedCargoBayCommand BuildLoadedCargoCommand(
            ShuttleCargoBayActionTarget bay)
        {
            List<UnloadLoadedCargoBayEntry> entries =
                new List<UnloadLoadedCargoBayEntry>();
            for (int i = 0; bay != null && bay.Items != null && i < bay.Items.Count; i++)
            {
                ShuttleCargoStackActionTarget stack = bay.Items[i];
                if (!ShuttleCargoBayUnloadPolicy.IsUnloadableStack(bay, stack))
                {
                    continue;
                }

                List<ShuttleCargoStackMemberActionTarget> slices;
                if (!ShuttleCargoStackMemberActionUtility.TryBuildSlices(
                    stack,
                    stack.StackCount,
                    out slices))
                {
                    continue;
                }

                for (int memberIndex = 0; memberIndex < slices.Count; memberIndex++)
                {
                    ShuttleCargoStackMemberActionTarget member = slices[memberIndex];
                    entries.Add(new UnloadLoadedCargoBayEntry(
                        member.TransporterIndex,
                        member.LoadedIndex,
                        member.ThingIDNumber,
                        member.DefName,
                        member.StackCount));
                }
            }

            return entries.Count > 0
                ? new UnloadLoadedCargoBayCommand(entries)
                : null;
        }

        private UnloadRefrigeratedCargoBayCommand BuildRefrigeratedCommand(
            ShuttleCargoBayActionTarget bay)
        {
            string moduleInstanceID = this.GetRefrigeratedModuleInstanceID(bay);
            if (string.IsNullOrEmpty(moduleInstanceID))
            {
                return null;
            }

            List<UnloadRefrigeratedCargoBayEntry> entries =
                new List<UnloadRefrigeratedCargoBayEntry>();
            for (int i = 0; bay != null && bay.Items != null && i < bay.Items.Count; i++)
            {
                ShuttleCargoStackActionTarget stack = bay.Items[i];
                if (!ShuttleCargoBayUnloadPolicy.IsUnloadableStack(bay, stack))
                {
                    continue;
                }

                List<ShuttleCargoStackMemberActionTarget> slices;
                if (!ShuttleCargoStackMemberActionUtility.TryBuildSlices(
                    stack,
                    stack.StackCount,
                    out slices))
                {
                    continue;
                }

                for (int memberIndex = 0; memberIndex < slices.Count; memberIndex++)
                {
                    ShuttleCargoStackMemberActionTarget member = slices[memberIndex];
                    entries.Add(new UnloadRefrigeratedCargoBayEntry(
                        member.ColdIndex,
                        member.ThingIDNumber,
                        member.DefName,
                        member.StackCount));
                }
            }

            return entries.Count > 0
                ? new UnloadRefrigeratedCargoBayCommand(moduleInstanceID, entries)
                : null;
        }

        private string GetRefrigeratedModuleInstanceID(
            ShuttleCargoBayActionTarget bay)
        {
            if (bay != null && !string.IsNullOrEmpty(bay.ModuleInstanceID))
            {
                return bay.ModuleInstanceID;
            }

            for (int i = 0; bay != null && bay.Items != null && i < bay.Items.Count; i++)
            {
                string moduleInstanceID =
                    ShuttleCargoBayUnloadPolicy.GetModuleInstanceID(bay, bay.Items[i]);
                if (!string.IsNullOrEmpty(moduleInstanceID))
                {
                    return moduleInstanceID;
                }
            }

            return null;
        }

        private string GetBayLabel(ShuttleCargoBayActionTarget bay)
        {
            return bay != null && !string.IsNullOrEmpty(bay.Label)
                ? bay.Label
                : this.Tr("CT_Shuttle_Cargo_Bay");
        }

        private bool ShowReject(string message)
        {
            return ShuttleUICommandFeedback.ShowReject(message, false);
        }

        private string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }

        private string Tr(string key, object arg0, object arg1, object arg2)
        {
            return ShuttleUIText.Tr(key, arg0, arg1, arg2);
        }
    }
}

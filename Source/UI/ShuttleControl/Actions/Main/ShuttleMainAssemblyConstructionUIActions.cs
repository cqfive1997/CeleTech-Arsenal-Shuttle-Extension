using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Main
{
    internal sealed class ShuttleMainAssemblyConstructionUIActions :
        IShuttleMainAssemblyConstructionUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleMainAssemblyConstructionUIActions(
            IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool CanCancelAssemblyConstruction(
            ShuttleAssemblyConstructionReadModel construction)
        {
            return this.HasCommandContext(construction);
        }

        public bool CancelAssemblyConstruction(
            ShuttleAssemblyConstructionReadModel construction)
        {
            if (!this.CanCancelAssemblyConstruction(construction))
            {
                this.ShowReject(this.GetUnavailableMessage());
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new CancelAssemblyConstructionCommand(construction.OrderID));
            return this.ShowResult(result);
        }

        public bool CanCancelQueuedAssemblyConstruction(
            ShuttleAssemblyConstructionQueueItemReadModel queuedOrder)
        {
            return this.commandExecutor != null &&
                queuedOrder != null &&
                !string.IsNullOrEmpty(queuedOrder.OrderID);
        }

        public bool CancelQueuedAssemblyConstruction(
            ShuttleAssemblyConstructionQueueItemReadModel queuedOrder)
        {
            if (!this.CanCancelQueuedAssemblyConstruction(queuedOrder))
            {
                this.ShowReject(this.GetUnavailableMessage());
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new CancelAssemblyConstructionCommand(queuedOrder.OrderID));
            return this.ShowResult(result);
        }

        public bool CanDebugCompleteAssemblyConstruction(
            ShuttleAssemblyConstructionReadModel construction)
        {
            return Prefs.DevMode &&
                DebugSettings.godMode &&
                this.HasCommandContext(construction) &&
                construction.MaterialsReady;
        }

        public bool DebugCompleteAssemblyConstruction(
            ShuttleAssemblyConstructionReadModel construction)
        {
            if (!this.CanDebugCompleteAssemblyConstruction(construction))
            {
                this.ShowReject(this.GetUnavailableMessage());
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new DebugCompleteAssemblyConstructionCommand(construction.OrderID));
            if (result != null && result.Success)
            {
                // CompletionService already emits the targeted completion message and
                // Building_Complete sound. Do not display the same command result twice.
                return true;
            }

            return this.ShowResult(result);
        }

        public bool CanDebugFillAssemblyConstructionMaterials(
            ShuttleAssemblyConstructionReadModel construction)
        {
            return Prefs.DevMode &&
                DebugSettings.godMode &&
                this.HasCommandContext(construction) &&
                !construction.MaterialsReady;
        }

        public bool DebugFillAssemblyConstructionMaterials(
            ShuttleAssemblyConstructionReadModel construction)
        {
            if (!this.CanDebugFillAssemblyConstructionMaterials(construction))
            {
                this.ShowReject(this.GetUnavailableMessage());
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new DebugFillAssemblyConstructionMaterialsCommand(construction.OrderID));
            return this.ShowResult(result);
        }

        private bool HasCommandContext(ShuttleAssemblyConstructionReadModel construction)
        {
            return this.commandExecutor != null &&
                construction != null &&
                construction.HasActiveOrder &&
                !string.IsNullOrEmpty(construction.OrderID);
        }

        private string GetUnavailableMessage()
        {
            return this.commandExecutor != null
                ? ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable")
                : ShuttleUIText.Tr("CT_Shuttle_Command_ExecutorUnavailable");
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

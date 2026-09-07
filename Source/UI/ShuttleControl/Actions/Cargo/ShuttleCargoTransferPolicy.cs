using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Cargo
{
    internal static class ShuttleCargoTransferPolicy
    {
        internal static bool CanStartTransfer(
            ShuttleCargoStackActionTarget target,
            ShuttleCargoPageActionContext pageContext)
        {
            if (target == null || target.StackCount <= 0)
            {
                return false;
            }

            if (target.SourceKind == ShuttleCargoStackActionSourceKind.LoadedCargo)
            {
                return ShuttleCargoTransferIdentityPolicy.HasLoadedSourceIdentity(
                        target) &&
                    ShuttleCargoTransferBayPolicy.HasOpenableRefrigeratedBay(
                        pageContext);
            }

            if (target.SourceKind == ShuttleCargoStackActionSourceKind.RefrigeratedCargo)
            {
                return ShuttleCargoTransferIdentityPolicy.HasColdSourceIdentity(
                    target);
            }

            return false;
        }

        internal static bool CanConfirmTransfer(
            ShuttleCargoTransferActionTarget target)
        {
            ShuttleCargoTransferCommandTarget command = GetCommand(target);
            if (command == null)
            {
                return false;
            }

            if (command.Direction ==
                ShuttleCargoTransferDirection.LoadedToRefrigerated)
            {
                return CanConfirmLoadedToRefrigerated(target, command);
            }

            if (command.Direction ==
                ShuttleCargoTransferDirection.RefrigeratedToLoaded)
            {
                return CanConfirmRefrigeratedToLoaded(target, command);
            }

            return false;
        }

        internal static string GetStartTooltip(
            ShuttleCargoStackActionTarget target,
            ShuttleCargoPageActionContext pageContext)
        {
            if (target == null)
            {
                return Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            if (target.StackCount <= 0)
            {
                return Tr("CT_Shuttle_Cargo_LoadedEntryNoStack");
            }

            if (target.SourceKind == ShuttleCargoStackActionSourceKind.LoadedCargo)
            {
                return GetLoadedToRefrigeratedStartTooltip(target, pageContext);
            }

            if (target.SourceKind ==
                ShuttleCargoStackActionSourceKind.RefrigeratedCargo)
            {
                return ShuttleCargoTransferIdentityPolicy.HasColdSourceIdentity(target)
                    ? Tr("CT_Shuttle_Cargo_Action_TransferToNormal")
                    : Tr("CT_Shuttle_Command_RefrigeratedCargoModuleMissing");
            }

            return Tr("CT_Shuttle_Command_ContextUnavailable");
        }

        internal static string GetConfirmTooltip(
            ShuttleCargoTransferActionTarget target)
        {
            ShuttleCargoTransferCommandTarget command = GetCommand(target);
            if (command == null)
            {
                return Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            if (!ShuttleCargoTransferIdentityPolicy.HasValidCount(command))
            {
                return Tr("CT_Shuttle_Cargo_LoadedEntryNoStack");
            }

            if (command.Direction ==
                ShuttleCargoTransferDirection.LoadedToRefrigerated)
            {
                return GetLoadedToRefrigeratedConfirmTooltip(target, command);
            }

            if (command.Direction ==
                ShuttleCargoTransferDirection.RefrigeratedToLoaded)
            {
                return GetRefrigeratedToLoadedConfirmTooltip(target, command);
            }

            return Tr("CT_Shuttle_Command_ContextUnavailable");
        }

        private static bool CanConfirmLoadedToRefrigerated(
            ShuttleCargoTransferActionTarget target,
            ShuttleCargoTransferCommandTarget command)
        {
            if (!ShuttleCargoTransferIdentityPolicy.HasLoadedCommandIdentity(
                    command) ||
                string.IsNullOrEmpty(command.DestinationModuleInstanceID))
            {
                return false;
            }

            ShuttleCargoTransferValidationContext validation = GetValidation(target);
            if (validation == null ||
                !ShuttleCargoTransferIdentityPolicy.HasLoadedSourceIdentity(
                    validation.Stack) ||
                !ShuttleCargoTransferBayPolicy.IsSelectedRefrigeratedBayUsable(
                    validation.DestinationBay,
                    command.DestinationModuleInstanceID))
            {
                return false;
            }

            return ShuttleCargoTransferMassPolicy.FitsDestinationMass(
                GetDisplay(target));
        }

        private static bool CanConfirmRefrigeratedToLoaded(
            ShuttleCargoTransferActionTarget target,
            ShuttleCargoTransferCommandTarget command)
        {
            ShuttleCargoTransferValidationContext validation = GetValidation(target);
            return validation != null &&
                ShuttleCargoTransferIdentityPolicy.HasColdSourceIdentity(
                    validation.Stack) &&
                ShuttleCargoTransferIdentityPolicy.HasColdCommandIdentity(command) &&
                ShuttleCargoTransferMassPolicy.FitsDestinationMass(
                    GetDisplay(target));
        }

        private static string GetLoadedToRefrigeratedStartTooltip(
            ShuttleCargoStackActionTarget target,
            ShuttleCargoPageActionContext pageContext)
        {
            if (!ShuttleCargoTransferIdentityPolicy.HasLoadedSourceIdentity(target))
            {
                return Tr("CT_Shuttle_Cargo_LoadedEntryUnavailable");
            }

            if (!ShuttleCargoTransferBayPolicy.HasAnyRefrigeratedBay(pageContext))
            {
                return Tr("CT_Shuttle_Command_RefrigeratedCargoModuleMissing");
            }

            if (!ShuttleCargoTransferBayPolicy.HasOpenableRefrigeratedBay(
                    pageContext))
            {
                return ShuttleCargoTransferBayPolicy.GetFirstRefrigeratedBayBlocker(
                    pageContext);
            }

            return Tr("CT_Shuttle_Cargo_Action_TransferToCold");
        }

        private static string GetLoadedToRefrigeratedConfirmTooltip(
            ShuttleCargoTransferActionTarget target,
            ShuttleCargoTransferCommandTarget command)
        {
            if (!ShuttleCargoTransferIdentityPolicy.HasLoadedCommandIdentity(
                    command) ||
                GetValidation(target) == null ||
                !ShuttleCargoTransferIdentityPolicy.HasLoadedSourceIdentity(
                    GetValidation(target).Stack))
            {
                return Tr("CT_Shuttle_Cargo_LoadedEntryUnavailable");
            }

            if (string.IsNullOrEmpty(command.DestinationModuleInstanceID))
            {
                return Tr("CT_Shuttle_Command_RefrigeratedCargoModuleMissing");
            }

            ShuttleCargoTransferValidationContext validation = GetValidation(target);
            if (validation != null)
            {
                string blocker =
                    ShuttleCargoTransferBayPolicy.GetSelectedRefrigeratedBayBlocker(
                        validation.DestinationBay,
                        command.DestinationModuleInstanceID);
                if (!string.IsNullOrEmpty(blocker))
                {
                    return blocker;
                }
            }

            return ShuttleCargoTransferMassPolicy.FitsDestinationMass(
                    GetDisplay(target))
                ? Tr("CT_Shuttle_Cargo_Action_TransferToCold")
                : ShuttleCargoTransferMassPolicy.GetDestinationMassBlocker(
                    GetDisplay(target));
        }

        private static string GetRefrigeratedToLoadedConfirmTooltip(
            ShuttleCargoTransferActionTarget target,
            ShuttleCargoTransferCommandTarget command)
        {
            if (!ShuttleCargoTransferIdentityPolicy.HasColdCommandIdentity(command))
            {
                return Tr("CT_Shuttle_Command_RefrigeratedCargoModuleMissing");
            }

            ShuttleCargoTransferValidationContext validation = GetValidation(target);
            if (validation == null ||
                !ShuttleCargoTransferIdentityPolicy.HasColdSourceIdentity(
                    validation.Stack))
            {
                return Tr("CT_Shuttle_Cargo_LoadedEntryUnavailable");
            }

            return ShuttleCargoTransferMassPolicy.FitsDestinationMass(
                    GetDisplay(target))
                ? Tr("CT_Shuttle_Cargo_Action_TransferToNormal")
                : ShuttleCargoTransferMassPolicy.GetDestinationMassBlocker(
                    GetDisplay(target));
        }

        private static ShuttleCargoTransferCommandTarget GetCommand(
            ShuttleCargoTransferActionTarget target)
        {
            return target != null ? target.Command : null;
        }

        private static ShuttleCargoTransferDisplayTarget GetDisplay(
            ShuttleCargoTransferActionTarget target)
        {
            return target != null ? target.Display : null;
        }

        private static ShuttleCargoTransferValidationContext GetValidation(
            ShuttleCargoTransferActionTarget target)
        {
            return target != null ? target.Validation : null;
        }

        private static string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }
    }
}

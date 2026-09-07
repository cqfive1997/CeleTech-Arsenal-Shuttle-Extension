using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Cargo
{
    internal static class ShuttleCargoTransferIdentityPolicy
    {
        internal static bool HasLoadedSourceIdentity(
            ShuttleCargoStackActionTarget target)
        {
            return ShuttleCargoStackMemberActionUtility.HasValidMembers(
                target,
                ShuttleCargoStackActionSourceKind.LoadedCargo);
        }

        internal static bool HasColdSourceIdentity(
            ShuttleCargoStackActionTarget target)
        {
            return ShuttleCargoStackMemberActionUtility.HasValidMembers(
                target,
                ShuttleCargoStackActionSourceKind.RefrigeratedCargo);
        }

        internal static bool HasLoadedCommandIdentity(
            ShuttleCargoTransferCommandTarget command)
        {
            return command != null &&
                command.Direction ==
                    ShuttleCargoTransferDirection.LoadedToRefrigerated &&
                command.SourceTransporterIndex >= 0 &&
                command.SourceLoadedIndex >= 0 &&
                command.SourceThingIDNumber > 0 &&
                !string.IsNullOrEmpty(command.SourceDefName) &&
                HasValidCount(command);
        }

        internal static bool HasColdCommandIdentity(
            ShuttleCargoTransferCommandTarget command)
        {
            return command != null &&
                command.Direction ==
                    ShuttleCargoTransferDirection.RefrigeratedToLoaded &&
                !string.IsNullOrEmpty(command.SourceModuleInstanceID) &&
                command.SourceColdIndex >= 0 &&
                command.SourceThingIDNumber > 0 &&
                !string.IsNullOrEmpty(command.SourceDefName) &&
                HasValidCount(command);
        }

        internal static bool HasValidCount(
            ShuttleCargoTransferCommandTarget command)
        {
            return command != null &&
                command.Count > 0 &&
                command.SourceAvailableCount > 0 &&
                command.Count <= command.SourceAvailableCount;
        }
    }
}

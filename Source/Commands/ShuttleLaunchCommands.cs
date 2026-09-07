using RimWorld.Planet;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Opens launch targeting through the modular shuttle command boundary.
    /// </summary>
    public sealed class BeginLaunchTargetingCommand : IShuttleCommand
    {
        public const string ID = "begin-launch-targeting";

        public string CommandID
        {
            get
            {
                return ID;
            }
        }
    }

    /// <summary>
    /// Future command for canceling modular launch warmup once LaunchSystem owns warmup state.
    /// </summary>
    public sealed class CancelLaunchWarmupCommand : IShuttleCommand
    {
        public const string ID = "cancel-launch-warmup";

        public string CommandID
        {
            get
            {
                return ID;
            }
        }
    }

    /// <summary>
    /// Confirms one shuttle-owned electric launch target and arrival action.
    /// </summary>
    public sealed class ConfirmLaunchCommand : IShuttleCommand
    {
        public const string ID = "confirm-launch";

        public ConfirmLaunchCommand(PlanetTile destinationTile, TransportersArrivalAction arrivalAction)
        {
            this.DestinationTile = destinationTile;
            this.ArrivalAction = arrivalAction;
        }

        public PlanetTile DestinationTile { get; private set; }
        public TransportersArrivalAction ArrivalAction { get; private set; }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }
    }
}

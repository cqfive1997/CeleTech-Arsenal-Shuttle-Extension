using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Requests a safe manual eject/wake of all pawns currently held in shuttle Habitat occupancy.
    /// </summary>
    public sealed class EjectHabitatOccupantsCommand : IShuttleCommand
    {
        public const string ID = "eject-habitat-occupants";

        public string CommandID
        {
            get
            {
                return ID;
            }
        }
    }

    /// <summary>
    /// Requests safe manual eject/wake of one pawn currently held in shuttle Habitat occupancy.
    /// </summary>
    public sealed class EjectHabitatOccupantCommand : IShuttleCommand
    {
        public const string ID = "eject-habitat-occupant";

        public EjectHabitatOccupantCommand(int pawnThingID)
        {
            this.PawnThingID = pawnThingID;
        }

        public int PawnThingID { get; private set; }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }
    }

    /// <summary>
    /// Updates the enabled JoyKindDef selection for one recreation-capable Habitat module.
    /// </summary>
    public sealed class SetHabitatJoyKindsCommand : IShuttleCommand
    {
        public const string ID = "set-habitat-joy-kinds";

        private readonly IReadOnlyList<string> joyKindDefNames;

        public SetHabitatJoyKindsCommand(
            string moduleInstanceID,
            IReadOnlyList<string> joyKindDefNames)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.joyKindDefNames = joyKindDefNames != null
                ? new List<string>(joyKindDefNames)
                : new List<string>();
        }

        public string ModuleInstanceID { get; private set; }

        public IReadOnlyList<string> JoyKindDefNames
        {
            get
            {
                return this.joyKindDefNames;
            }
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }
    }
}

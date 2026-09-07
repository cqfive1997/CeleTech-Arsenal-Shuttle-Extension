namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Requests safe manual eject of one mech currently held in the shuttle mech charging bay.
    /// </summary>
    public sealed class EjectMechChargerOccupantCommand : IShuttleCommand
    {
        public const string ID = "eject-mech-charger-occupant";

        public EjectMechChargerOccupantCommand(int mechThingID)
        {
            this.MechThingID = mechThingID;
        }

        public int MechThingID { get; private set; }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }
    }
}

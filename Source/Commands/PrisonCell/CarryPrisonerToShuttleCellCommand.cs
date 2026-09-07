namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    public sealed class CarryPrisonerToShuttleCellCommand : IShuttleCommand
    {
        public const string ID = "carry-prisoner-to-shuttle-cell";

        public CarryPrisonerToShuttleCellCommand(int prisonerThingID, int carrierThingID)
        {
            this.PrisonerThingID = prisonerThingID;
            this.CarrierThingID = carrierThingID;
        }

        public int PrisonerThingID { get; private set; }

        public int CarrierThingID { get; private set; }

        public string CommandID
        {
            get { return ID; }
        }
    }
}

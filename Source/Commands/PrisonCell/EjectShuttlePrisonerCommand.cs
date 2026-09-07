namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    public sealed class EjectShuttlePrisonerCommand : IShuttleCommand
    {
        public const string ID = "eject-shuttle-prisoner";

        public EjectShuttlePrisonerCommand()
            : this(-1)
        {
        }

        public EjectShuttlePrisonerCommand(int prisonerThingID)
        {
            this.PrisonerThingID = prisonerThingID;
        }

        public int PrisonerThingID { get; private set; }

        public string CommandID
        {
            get { return ID; }
        }
    }
}

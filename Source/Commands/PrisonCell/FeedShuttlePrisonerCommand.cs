namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    public sealed class FeedShuttlePrisonerCommand : IShuttleCommand
    {
        public const string ID = "feed-shuttle-prisoner";

        public FeedShuttlePrisonerCommand(int prisonerThingID, int feederThingID)
        {
            this.PrisonerThingID = prisonerThingID;
            this.FeederThingID = feederThingID;
        }

        public int PrisonerThingID { get; private set; }

        public int FeederThingID { get; private set; }

        public string CommandID
        {
            get { return ID; }
        }
    }
}

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    public sealed class TendShuttlePrisonerCommand : IShuttleCommand
    {
        public const string ID = "tend-shuttle-prisoner";

        public TendShuttlePrisonerCommand(int prisonerThingID, int doctorThingID)
        {
            this.PrisonerThingID = prisonerThingID;
            this.DoctorThingID = doctorThingID;
        }

        public int PrisonerThingID { get; private set; }

        public int DoctorThingID { get; private set; }

        public string CommandID
        {
            get { return ID; }
        }
    }
}

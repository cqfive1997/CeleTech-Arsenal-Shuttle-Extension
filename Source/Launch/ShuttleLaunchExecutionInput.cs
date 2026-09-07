using CeleTech.ShuttleExtension.ModularShuttle.Flight;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    /// <summary>
    /// Confirmation-time launch input assembled by the controller seam.
    /// The launch service consumes this detached request and quote to run the transaction.
    /// </summary>
    public sealed class ShuttleLaunchExecutionInput
    {
        public ShuttleLaunchExecutionInput(
            ShuttleLaunchRequest request,
            ShuttleFlightEnergyQuote quote,
            int profileRevision)
        {
            this.Request = request;
            this.Quote = quote;
            this.ProfileRevision = profileRevision;
        }

        public ShuttleLaunchRequest Request { get; private set; }
        public ShuttleFlightEnergyQuote Quote { get; private set; }
        public int ProfileRevision { get; private set; }
    }
}

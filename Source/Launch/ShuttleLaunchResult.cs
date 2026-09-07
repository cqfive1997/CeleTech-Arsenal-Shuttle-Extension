using CeleTech.ShuttleExtension.ModularShuttle.Flight;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    public sealed class ShuttleLaunchResult
    {
        private ShuttleLaunchResult(bool success, string message, ShuttleFlightEnergyQuote quote)
        {
            this.Success = success;
            this.Message = message;
            this.Quote = quote;
        }

        public bool Success { get; private set; }
        public string Message { get; private set; }
        public ShuttleFlightEnergyQuote Quote { get; private set; }

        public static ShuttleLaunchResult Succeeded(string message, ShuttleFlightEnergyQuote quote)
        {
            return new ShuttleLaunchResult(true, message, quote);
        }

        public static ShuttleLaunchResult Failed(string message)
        {
            return new ShuttleLaunchResult(false, message, null);
        }

        public static ShuttleLaunchResult Failed(string message, ShuttleFlightEnergyQuote quote)
        {
            return new ShuttleLaunchResult(false, message, quote);
        }
    }
}

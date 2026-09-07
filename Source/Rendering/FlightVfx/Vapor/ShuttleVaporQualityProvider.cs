namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx.Vapor
{
    internal static class ShuttleVaporQualityProvider
    {
        internal static ShuttleVaporQuality CurrentQuality
        {
            get
            {
                // TODO: Replace with ModSettings once the vapor visual effect is stabilized.
                return ShuttleVaporQuality.High;
            }
        }

        internal static ShuttleVaporQualityProfile CurrentProfile
        {
            get
            {
                return ShuttleVaporQualityProfile.ForQuality(CurrentQuality);
            }
        }
    }
}

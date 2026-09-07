namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal static class ShuttleFlightVfxShaderAvailability
    {
        internal static bool IsAvailable
        {
            get
            {
                return ShuttleFlightVfxShaderMaterialCache.Shared.IsReady;
            }
        }
    }
}

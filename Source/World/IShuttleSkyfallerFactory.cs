using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.World
{
    /// <summary>
    /// Creates shuttle-owned skyfallers after launch validation and energy spending are complete.
    /// </summary>
    public interface IShuttleSkyfallerFactory
    {
        FlyShipLeaving CreateLeavingSkyfaller(ShuttleLaunchPayload payload, ActiveTransporter activeTransporter);
    }
}

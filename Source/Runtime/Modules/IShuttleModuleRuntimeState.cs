using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    /// <summary>
    /// Durable state payload owned by one built-in module runtime system.
    /// This is an internal project seam, not a documented third-party API.
    /// </summary>
    internal interface IShuttleModuleRuntimeState : IExposable
    {
        void EnsureInitialized();
    }
}

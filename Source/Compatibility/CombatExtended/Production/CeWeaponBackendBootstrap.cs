using CombatExtended;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Registers the conditional CE factory into the main assembly's shared internal registry.
    /// It owns no runtime weapon state and performs no compatibility probing during startup.
    /// </summary>
    [StaticConstructorOnStartup]
    internal static class CeWeaponBackendBootstrap
    {
        static CeWeaponBackendBootstrap()
        {
            Factory = new CeWeaponBackendFactory();
            RegistrationSucceeded = ShuttleWeaponBackendRegistry.Shared.Register(Factory);
            ThreatAdapterRegistrationSucceeded =
                ShuttleProjectileThreatAdapterRegistry.Shared.Register(
                    new CeProjectileThreatAdapter());

            Log.Message(
                "[CeleTech Shuttle][CE Backend] production shell loaded" +
                " registered=" + RegistrationSucceeded +
                " threatAdapter=" + ThreatAdapterRegistrationSucceeded +
                " backend=" + Factory.BackendId +
                " ceVersion=" + typeof(AmmoDef).Assembly.GetName().Version);
        }

        internal static IShuttleWeaponBackendFactory Factory { get; private set; }

        internal static bool RegistrationSucceeded { get; private set; }

        internal static bool ThreatAdapterRegistrationSucceeded { get; private set; }
    }
}

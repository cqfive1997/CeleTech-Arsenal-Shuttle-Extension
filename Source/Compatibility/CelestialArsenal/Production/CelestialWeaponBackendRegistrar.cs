using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    /// <summary>
    /// Registers the standalone shuttle-owned sustained particle-lance backend.
    /// </summary>
    [StaticConstructorOnStartup]
    internal static class CelestialWeaponBackendRegistrar
    {
        static CelestialWeaponBackendRegistrar()
        {
            Factory = new CelestialSustainLaserBackendFactory();
            RegistrationSucceeded = ShuttleWeaponBackendRegistry.Shared.Register(Factory);

            Log.Message(
                "[CeleTech Shuttle][Particle Lance] standalone backend loaded" +
                " registered=" + RegistrationSucceeded +
                " backend=" + Factory.BackendId);
        }

        internal static IShuttleWeaponBackendFactory Factory { get; private set; }

        internal static bool RegistrationSucceeded { get; private set; }
    }
}

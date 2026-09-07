using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Conditional production composition root for the authored 6mm/40mm CE backend.
    /// Selection remains pure; the lifecycle facet performs the idle-boundary transfer.
    /// </summary>
    internal sealed class CeWeaponBackendFactory : IShuttleWeaponBackendFactory
    {
        internal const string Id = "combat-extended";

        private readonly CeWeaponCompatibilityEvaluator compatibilityEvaluator =
            new CeWeaponCompatibilityEvaluator();
        private readonly ShuttleWeaponBackendBinding binding;

        internal CeWeaponBackendFactory()
        {
            CeWeaponFiringComposition firing = new CeWeaponFiringComposition();
            ShuttleWeaponReloadPowerCoordinator reloadPowerCoordinator =
                new ShuttleWeaponReloadPowerCoordinator();
            CeWeaponReloadDriver reloadDriver =
                new CeWeaponReloadDriver(reloadPowerCoordinator);
            CeWeaponBackendLifecycleDriver lifecycle =
                new CeWeaponBackendLifecycleDriver(firing.CycleDriver);
            this.binding = new ShuttleWeaponBackendBinding(
                Id,
                lifecycle,
                new CeWeaponBackendTickDriver(
                    lifecycle,
                    firing.CycleDriver,
                    reloadDriver),
                new CeWeaponBackendPowerDemandDriver(
                    firing.PowerProjection,
                    reloadDriver),
                new CeWeaponBackendForcedTargetEvaluator(firing.CycleDriver),
                new CeWeaponAmmoReadDriver(),
                new CeWeaponAmmoCommandDriver(reloadDriver),
                new CeWeaponManualReloadDriver(reloadDriver),
                new CeWeaponRemovalRefundGuard());
        }

        public string BackendId
        {
            get { return Id; }
        }

        public int Priority
        {
            get { return 100; }
        }

        public ShuttleWeaponCompatibilityReport Probe(ShuttleWeaponBackendProbeContext context)
        {
            return this.compatibilityEvaluator.Evaluate(context, Id);
        }

        public ShuttleWeaponBackendBinding CreateBinding(ShuttleWeaponBackendProbeContext context)
        {
            return this.binding;
        }
    }
}

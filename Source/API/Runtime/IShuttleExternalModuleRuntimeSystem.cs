namespace CeleTech.ShuttleExtension.ModularShuttle.API.Runtime
{
    /// <summary>
    /// Public contract for third-party module runtime systems.
    /// Prefer inheriting from ShuttleExternalModuleRuntimeSystemBase instead of implementing
    /// this interface directly.
    /// </summary>
    public interface IShuttleExternalModuleRuntimeSystem
    {
        int TickInterval { get; }

        int StateSchemaVersion { get; }

        bool AppliesTo(ShuttleExternalModuleInfo module);

        bool AppliesTo(ShuttleExternalLaunchModuleInfo module);

        void Initialize(ShuttleExternalRuntimeContext context);

        void Migrate(ShuttleExternalRuntimeContext context, int loadedSchemaVersion);

        void Reconcile(ShuttleExternalRuntimeContext context);

        void CollectPowerDemand(ShuttleExternalPowerDemandContext context);

        void CollectMassContribution(ShuttleExternalMassContributionContext context);

        void Tick(ShuttleExternalRuntimeContext context);

        bool CanRemove(ShuttleExternalRuntimeContext context, out string reason);

        bool PreLaunchValidate(ShuttleExternalLaunchValidationContext context, out string reason);

        void OnInstalled(ShuttleExternalRuntimeContext context);

        void OnRemoved(ShuttleExternalRuntimeContext context);

        void OnLaunchSucceeded(ShuttleExternalLaunchContext context);

        void OnArrived(ShuttleExternalRuntimeContext context);
    }
}

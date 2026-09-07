using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    /// <summary>
    /// Internal contract for built-in module runtime systems. This is not a third-party API:
    /// systems are registered explicitly by code and receive only narrow runtime contexts.
    /// Runtime systems must not mutate AssemblyState topology, UI, cargo backend, or host comps
    /// directly. Runtime tick/reconcile hooks may inspect their own installed module context,
    /// while launch hooks receive detached launch records. Systems may mutate only their own
    /// IShuttleModuleRuntimeState payload. One-shot stored-energy spending must go through
    /// IShuttleStoredEnergySink, and transient active draw must go through
    /// IShuttlePowerDemandSink via the runtime context. Assembly install/remove must still go
    /// through commands and the mutation controller. PreLaunchValidate must be read-only.
    /// </summary>
    internal interface IShuttleModuleRuntimeSystem
    {
        /// <summary>
        /// Stable key used with the module instance ID to locate this system's durable state.
        /// Treat changes to this value as save-affecting.
        /// </summary>
        string RuntimeSystemKey { get; }

        /// <summary>
        /// Tick cadence in game ticks. Values less than one disable ticking for that system.
        /// </summary>
        int TickInterval { get; }

        /// <summary>
        /// Returns whether this system has a real transient power-demand hook.
        /// Dispatch-plan construction uses this static declaration to exclude inherited no-op
        /// hooks without guessing from a previous zero-watt result.
        /// </summary>
        bool ParticipatesInPowerDemand { get; }

        /// <summary>
        /// Returns whether this built-in system owns runtime behavior for the installed module.
        /// Must be side-effect free because validators and tick scheduling call it before dispatch.
        /// </summary>
        bool AppliesTo(ShuttleModule module);

        /// <summary>
        /// Launch-time applicability uses detached confirmation snapshots, not live assembly records.
        /// </summary>
        bool AppliesTo(ShuttleLaunchModuleRecord moduleRecord);

        /// <summary>
        /// Creates the durable payload stored under ShuttleRuntimeState.Modules.
        /// Concrete state type names are save-affecting because they are persisted by Scribe_Deep.
        /// Implementations must return a non-null state.
        /// </summary>
        IShuttleModuleRuntimeState CreateState();

        /// <summary>
        /// Reconciles installed-module state after assembly/profile changes.
        /// This must stay lightweight and idempotent. It may update only the owned runtime
        /// payload, not AssemblyState.
        /// </summary>
        void Reconcile(ShuttleModuleRuntimeContext context);

        /// <summary>
        /// Reports transient internal power demand before PowerSystem.Tick consumes it.
        /// This must inspect existing runtime state only and must not create state or run
        /// gameplay actions such as firing, target acquisition, or assembly mutation.
        /// </summary>
        void CollectPowerDemand(ShuttleModuleRuntimeContext context);

        /// <summary>
        /// Reports dynamic runtime-owned payload mass. It must be read-only with respect to
        /// assembly/cargo/holder state and should not create cargo objects.
        /// </summary>
        void CollectMassContribution(
            ShuttleModuleRuntimeContext context,
            ShuttleRuntimeMassContributionCollector collector);

        /// <summary>
        /// Advances runtime behavior at the system cadence. Called only after state already exists.
        /// </summary>
        void Tick(ShuttleModuleRuntimeContext context);

        /// <summary>
        /// Validates removal. Failure is player-facing and must return a useful reason.
        /// </summary>
        bool CanRemove(ShuttleModuleRuntimeContext context, out string reason);

        /// <summary>
        /// Read-only launch validation hook. It must not mutate runtime, assembly, cargo, or host comps.
        /// </summary>
        bool PreLaunchValidate(ShuttleModulePreLaunchValidationContext context, out string reason);

        /// <summary>
        /// Lifecycle hook after a module has been installed and the owned runtime state is available.
        /// </summary>
        void OnInstalled(ShuttleModuleRuntimeContext context);

        /// <summary>
        /// Lifecycle hook before the coordinator removes the module's runtime state after successful removal.
        /// </summary>
        void OnRemoved(ShuttleModuleRuntimeContext context);

        /// <summary>
        /// Lifecycle hook after launch transaction success. It receives launch context only.
        /// </summary>
        void OnLaunchSucceeded(ShuttleModuleLaunchContext context);

        /// <summary>
        /// Lifecycle hook after the shuttle arrives and map-side runtime context is available again.
        /// </summary>
        void OnArrived(ShuttleModuleRuntimeContext context);
    }
}

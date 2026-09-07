using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    /// <summary>
    /// Convenience base for built-in runtime systems. New systems only implement identity,
    /// applicability, and state creation unless they need a specific lifecycle hook.
    /// </summary>
    internal abstract class ShuttleModuleRuntimeSystemBase : IShuttleModuleRuntimeSystem
    {
        public abstract string RuntimeSystemKey { get; }

        /// <summary>
        /// Conservative default cadence for systems that do not need per-tick behavior.
        /// </summary>
        public virtual int TickInterval
        {
            get
            {
                return 60;
            }
        }

        public virtual bool ParticipatesInPowerDemand
        {
            get
            {
                return false;
            }
        }

        public abstract bool AppliesTo(ShuttleModule module);

        public virtual bool AppliesTo(ShuttleLaunchModuleRecord moduleRecord)
        {
            return false;
        }

        public abstract IShuttleModuleRuntimeState CreateState();

        /// <summary>
        /// Default hooks intentionally do nothing. Subclasses opt in to behavior explicitly,
        /// which keeps profile-only modules from accidentally gaining runtime effects.
        /// </summary>
        public virtual void Reconcile(ShuttleModuleRuntimeContext context)
        {
        }

        public virtual void CollectPowerDemand(ShuttleModuleRuntimeContext context)
        {
        }

        public virtual void CollectMassContribution(
            ShuttleModuleRuntimeContext context,
            ShuttleRuntimeMassContributionCollector collector)
        {
        }

        public virtual void Tick(ShuttleModuleRuntimeContext context)
        {
        }

        public virtual bool CanRemove(ShuttleModuleRuntimeContext context, out string reason)
        {
            reason = null;
            return true;
        }

        public virtual bool PreLaunchValidate(ShuttleModulePreLaunchValidationContext context, out string reason)
        {
            reason = null;
            return true;
        }

        public virtual void OnInstalled(ShuttleModuleRuntimeContext context)
        {
        }

        public virtual void OnRemoved(ShuttleModuleRuntimeContext context)
        {
        }

        public virtual void OnLaunchSucceeded(ShuttleModuleLaunchContext context)
        {
        }

        public virtual void OnArrived(ShuttleModuleRuntimeContext context)
        {
        }
    }
}

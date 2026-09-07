namespace CeleTech.ShuttleExtension.ModularShuttle.API.Runtime
{
    /// <summary>
    /// Recommended base class for third-party runtime systems. Inheriting from this class
    /// gives future SDK versions room to add optional hooks without forcing every mod to
    /// implement them immediately.
    /// </summary>
    public abstract class ShuttleExternalModuleRuntimeSystemBase : IShuttleExternalModuleRuntimeSystem
    {
        public virtual int TickInterval
        {
            get
            {
                return 60;
            }
        }

        public virtual int StateSchemaVersion
        {
            get
            {
                return 1;
            }
        }

        public virtual bool AppliesTo(ShuttleExternalModuleInfo module)
        {
            return true;
        }

        public virtual bool AppliesTo(ShuttleExternalLaunchModuleInfo module)
        {
            return true;
        }

        public virtual void Initialize(ShuttleExternalRuntimeContext context)
        {
        }

        public virtual void Migrate(ShuttleExternalRuntimeContext context, int loadedSchemaVersion)
        {
        }

        public virtual void Reconcile(ShuttleExternalRuntimeContext context)
        {
        }

        public virtual void CollectPowerDemand(ShuttleExternalPowerDemandContext context)
        {
        }

        public virtual void CollectMassContribution(ShuttleExternalMassContributionContext context)
        {
        }

        public virtual void Tick(ShuttleExternalRuntimeContext context)
        {
        }

        public virtual bool CanRemove(ShuttleExternalRuntimeContext context, out string reason)
        {
            reason = null;
            return true;
        }

        public virtual bool PreLaunchValidate(ShuttleExternalLaunchValidationContext context, out string reason)
        {
            reason = null;
            return true;
        }

        public virtual void OnInstalled(ShuttleExternalRuntimeContext context)
        {
        }

        public virtual void OnRemoved(ShuttleExternalRuntimeContext context)
        {
        }

        public virtual void OnLaunchSucceeded(ShuttleExternalLaunchContext context)
        {
        }

        public virtual void OnArrived(ShuttleExternalRuntimeContext context)
        {
        }
    }
}

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal sealed class NativeVerbWeaponLifecycleDriver :
        IShuttleWeaponLifecycleDriver,
        IShuttleWeaponCycleHost
    {
        private readonly NativeVerbWeaponHost host;
        private readonly NativeVerbWeaponFireDriver fireDriver;

        internal NativeVerbWeaponLifecycleDriver(
            NativeVerbWeaponHost host,
            NativeVerbWeaponFireDriver fireDriver)
        {
            this.host = host;
            this.fireDriver = fireDriver;
        }

        public bool EnsureReady(ShuttleModuleRuntimeContext context)
        {
            return this.host != null &&
                this.host.EnsureReady(context, this.fireDriver);
        }

        public Verse.Verb GetAttackVerb(ShuttleModuleRuntimeContext context)
        {
            return this.host != null ? this.host.GetPrimaryVerb(context) : null;
        }

        public void TickVerbs(ShuttleModuleRuntimeContext context)
        {
            if (this.host != null)
            {
                this.host.TickVerbTracker(context);
            }
        }
    }
}

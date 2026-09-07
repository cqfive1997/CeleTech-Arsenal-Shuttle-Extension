namespace CeleTech.ShuttleExtension.ModularShuttle.API.Runtime
{
    /// <summary>
    /// Read-only launch validation context. It can block launch but cannot write runtime state.
    /// </summary>
    public sealed class ShuttleExternalLaunchValidationContext
    {
        public ShuttleExternalLaunchValidationContext(ShuttleExternalLaunchModuleInfo module)
            : this(module, null)
        {
        }

        public ShuttleExternalLaunchValidationContext(
            ShuttleExternalLaunchModuleInfo module,
            IShuttleExternalRuntimeStateReader state)
        {
            this.Module = module;
            this.State = state ?? NullShuttleExternalRuntimeStateStore.Instance;
        }

        public ShuttleExternalLaunchModuleInfo Module { get; private set; }

        public IShuttleExternalRuntimeStateReader State { get; private set; }
    }
}

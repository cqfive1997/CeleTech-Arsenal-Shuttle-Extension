namespace CeleTech.ShuttleExtension.ModularShuttle.API.Runtime
{
    /// <summary>
    /// Read-only launch success notification context. Launch has already succeeded when this
    /// context is delivered.
    /// </summary>
    public sealed class ShuttleExternalLaunchContext
    {
        public ShuttleExternalLaunchContext(ShuttleExternalLaunchModuleInfo module)
            : this(module, null)
        {
        }

        public ShuttleExternalLaunchContext(
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

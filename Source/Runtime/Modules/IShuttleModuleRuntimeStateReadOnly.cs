namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    /// <summary>
    /// Read-only launch-validation view over a module runtime payload. It exposes identity/type
    /// facts plus optional read-only views implemented by the payload.
    /// </summary>
    internal interface IShuttleModuleRuntimeStateReadOnly
    {
        bool HasState { get; }

        string RuntimeStateTypeName { get; }

        bool IsStateType<TState>()
            where TState : class, IShuttleModuleRuntimeState;

        bool TryGetStateView<TView>(out TView view)
            where TView : class;
    }

    internal sealed class ShuttleModuleRuntimeStateReadOnlyAdapter : IShuttleModuleRuntimeStateReadOnly
    {
        private readonly IShuttleModuleRuntimeState state;

        internal ShuttleModuleRuntimeStateReadOnlyAdapter(IShuttleModuleRuntimeState state)
        {
            this.state = state;
        }

        public bool HasState
        {
            get
            {
                return this.state != null;
            }
        }

        public string RuntimeStateTypeName
        {
            get
            {
                return this.state != null ? this.state.GetType().FullName : null;
            }
        }

        public bool IsStateType<TState>()
            where TState : class, IShuttleModuleRuntimeState
        {
            return this.state is TState;
        }

        public bool TryGetStateView<TView>(out TView view)
            where TView : class
        {
            view = this.state as TView;
            return view != null;
        }
    }
}

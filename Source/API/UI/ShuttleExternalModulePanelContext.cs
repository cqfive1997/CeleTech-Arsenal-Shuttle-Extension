using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.UI
{
    /// <summary>
    /// Context passed to an external module panel provider.
    /// It exposes detached module facts, a read-only state reader, and an optional
    /// controlled command executor. It does not expose writable runtime state.
    /// </summary>
    public sealed class ShuttleExternalModulePanelContext
    {
        public ShuttleExternalModulePanelContext(
            string panelKey,
            string runtimeSystemKey,
            ShuttleExternalModuleInfo module,
            IShuttleExternalRuntimeStateReader state,
            bool runtimeEnabled,
            int ticksGame)
            : this(
                panelKey,
                runtimeSystemKey,
                module,
                state,
                runtimeEnabled,
                ticksGame,
                null)
        {
        }

        public ShuttleExternalModulePanelContext(
            string panelKey,
            string runtimeSystemKey,
            ShuttleExternalModuleInfo module,
            IShuttleExternalRuntimeStateReader state,
            bool runtimeEnabled,
            int ticksGame,
            IShuttleExternalPanelCommandExecutor commandExecutor)
        {
            this.PanelKey = panelKey;
            this.RuntimeSystemKey = runtimeSystemKey;
            this.Module = module;
            this.State = state ?? NullShuttleExternalRuntimeStateStore.Instance;
            this.RuntimeEnabled = runtimeEnabled;
            this.TicksGame = ticksGame;
            this.CommandExecutor = commandExecutor;
        }

        public string PanelKey { get; private set; }

        public string RuntimeSystemKey { get; private set; }

        public ShuttleExternalModuleInfo Module { get; private set; }

        public IShuttleExternalRuntimeStateReader State { get; private set; }

        public bool RuntimeEnabled { get; private set; }

        public int TicksGame { get; private set; }

        public IShuttleExternalPanelCommandExecutor CommandExecutor { get; private set; }

        public ShuttleExternalModulePanelContext WithCommandExecutor(
            IShuttleExternalPanelCommandExecutor commandExecutor)
        {
            return new ShuttleExternalModulePanelContext(
                this.PanelKey,
                this.RuntimeSystemKey,
                this.Module,
                this.State,
                this.RuntimeEnabled,
                this.TicksGame,
                commandExecutor);
        }
    }
}

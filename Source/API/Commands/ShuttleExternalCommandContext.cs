using System;
using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.Commands
{
    /// <summary>
    /// Writable command context for a single external runtime state envelope.
    /// It does not expose controller, assembly state, runtime state, or live module objects.
    /// </summary>
    public sealed class ShuttleExternalCommandContext
    {
        private readonly Func<float, bool> storedEnergyConsumer;

        public ShuttleExternalCommandContext(
            ShuttleExternalCommand command,
            ShuttleExternalModuleInfo module,
            IShuttleExternalRuntimeStateStore state,
            int ticksGame,
            bool internalBusPowered,
            Func<float, bool> storedEnergyConsumer)
        {
            this.Command = command;
            this.Module = module;
            this.State = state ?? NullShuttleExternalRuntimeStateStore.Instance;
            this.TicksGame = ticksGame;
            this.InternalBusPowered = internalBusPowered;
            this.storedEnergyConsumer = storedEnergyConsumer;
        }

        public ShuttleExternalCommand Command { get; private set; }

        public ShuttleExternalModuleInfo Module { get; private set; }

        public IShuttleExternalRuntimeStateStore State { get; private set; }

        public int TicksGame { get; private set; }

        public bool InternalBusPowered { get; private set; }

        public bool TryConsumeStoredEnergyWd(float amountWd)
        {
            if (float.IsNaN(amountWd) || float.IsInfinity(amountWd))
            {
                return false;
            }

            if (amountWd <= 0f)
            {
                return true;
            }

            try
            {
                return this.storedEnergyConsumer != null &&
                    this.storedEnergyConsumer(amountWd);
            }
            catch
            {
                return false;
            }
        }
    }
}

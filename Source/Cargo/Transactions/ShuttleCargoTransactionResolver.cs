using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions
{
    /// <summary>
    /// Resolves the controller-composed internal Cargo service without exposing controller or
    /// backend instances to Medical, SDK, or other domain transaction adapters.
    /// </summary>
    internal static class ShuttleCargoTransactionResolver
    {
        internal static bool TryResolveBroker(
            ThingWithComps shuttleHost,
            out IShuttleCargoResourceBroker cargoBroker)
        {
            cargoBroker = null;
            CompModularShuttleCore core = shuttleHost != null
                ? shuttleHost.TryGetComp<CompModularShuttleCore>()
                : null;
            ShuttleController controller = core != null ? core.Controller : null;
            if (controller == null)
            {
                return false;
            }

            cargoBroker = controller.GetCargoResourceBrokerForInternalTransactions();
            return cargoBroker != null;
        }
    }
}

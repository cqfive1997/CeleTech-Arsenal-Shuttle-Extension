using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    internal static class ShuttleAssemblyConstructionUtility
    {
        internal static bool TryGetState(
            ShuttleCommandContext context,
            out ShuttleAssemblyConstructionState state,
            out string failureReason)
        {
            state = null;
            failureReason = null;
            ShuttleRuntimeState runtimeState = context != null ? context.GetRuntimeState() : null;
            if (runtimeState == null)
            {
                failureReason = "CT_Shuttle_Command_ShuttleRuntimeStateUnavailable".Translate().ToString();
                return false;
            }

            runtimeState.EnsureInitialized();
            state = runtimeState.AssemblyConstruction;
            if (state == null)
            {
                failureReason = "CT_Shuttle_AssemblyConstruction_StateUnavailable".Translate().ToString();
                return false;
            }

            state.EnsureInitialized();
            return true;
        }

        internal static bool MatchesActiveOrder(
            ShuttleAssemblyConstructionState state,
            string orderID)
        {
            return state != null &&
                state.HasActiveOrder &&
                state.ActiveOrder != null &&
                (string.IsNullOrEmpty(orderID) || state.ActiveOrder.OrderID == orderID);
        }

        internal static bool HasActiveRemovalOrder(
            ShuttleCommandContext context,
            out string failureReason)
        {
            failureReason = null;
            ShuttleRuntimeState runtimeState = context != null ? context.GetRuntimeState() : null;
            if (runtimeState == null)
            {
                return false;
            }

            runtimeState.EnsureInitialized();
            if (runtimeState.ModuleRemoval == null || !runtimeState.ModuleRemoval.HasActiveRemoval)
            {
                return false;
            }

            failureReason = "CT_Shuttle_Command_AssemblyRemovalAlreadyActive".Translate().ToString();
            return true;
        }

        internal static string GetDefLabel(Def def)
        {
            if (def == null)
            {
                return "-";
            }

            return !string.IsNullOrEmpty(def.label) ? def.LabelCap.ToString() : def.defName;
        }

        internal static string ActiveOrderFailureMessage()
        {
            return "CT_Shuttle_Command_AssemblyConstructionAlreadyActive".Translate().ToString();
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    internal sealed class AutoWorkTableProductDepositor
    {
        internal void TryDepositPendingProducts(
            ShuttleModuleRuntimeContext context,
            ShuttleAutoWorkTableModuleDef moduleDef,
            AutoWorkTableRuntimeState state,
            out bool shouldSetPostOutputStatus)
        {
            shouldSetPostOutputStatus = false;
            if (state.CompletionCommitBlocked)
            {
                state.SetStatus(
                    AutoWorkTableStatus.CompletionCommitBlocked,
                    state.LastFailureReason ??
                        "AutoWorkTable completion commit is blocked; pending products cannot be deposited until staged ingredients are resolved.");
                return;
            }

            if (state.HasActiveProduction || state.HasStagedIngredients)
            {
                state.SetStatus(
                    AutoWorkTableStatus.RecoveryBlocked,
                    "AutoWorkTable pending products cannot be deposited while active production or staged ingredients remain.");
                return;
            }

            if (!state.HasPendingProducts)
            {
                shouldSetPostOutputStatus = true;
                return;
            }

            if (state.NextDepositRetryTick > context.TicksGame)
            {
                return;
            }

            string failureReason;
            if (!this.TryValidateCargoAccess(context, moduleDef, out failureReason))
            {
                state.SetStatus(AutoWorkTableStatus.OutputBlocked, failureReason);
                state.ScheduleDepositRetry(context.TicksGame, moduleDef.depositRetryIntervalTicks);
                return;
            }

            int depositedCount;
            ShuttleCargoDepositReceipt depositReceipt;
            if (!context.CargoResourceBroker.TryDepositFrom(
                state.PendingProducts,
                "AutoWorkTable products",
                out depositedCount,
                out depositReceipt,
                out failureReason))
            {
                state.SetStatus(AutoWorkTableStatus.OutputBlocked, failureReason);
                state.ScheduleDepositRetry(context.TicksGame, moduleDef.depositRetryIntervalTicks);
                return;
            }

            if (state.HasPendingProducts)
            {
                state.SetStatus(AutoWorkTableStatus.OutputBlocked, "Cargo deposit did not clear pending products.");
                state.ScheduleDepositRetry(context.TicksGame, moduleDef.depositRetryIntervalTicks);
                return;
            }

            this.TryRouteDepositedProductsToCold(context, depositReceipt);
            shouldSetPostOutputStatus = true;
        }

        private void TryRouteDepositedProductsToCold(
            ShuttleModuleRuntimeContext context,
            ShuttleCargoDepositReceipt depositReceipt)
        {
            if (context == null ||
                context.PostDepositCargoRouter == null ||
                depositReceipt == null ||
                !depositReceipt.HasStacks)
            {
                return;
            }

            int movedStackCount;
            int movedThingCount;
            string failureReason;
            if (!context.PostDepositCargoRouter.TryRoute(
                    depositReceipt,
                    "AutoWorkTable post-deposit cold routing",
                    out movedStackCount,
                    out movedThingCount,
                    out failureReason))
            {
                // The production output transaction is already committed. Cold routing failure
                // must not reopen or duplicate it; the cold service owns rollback/recovery.
                ShuttleLog.Warn(
                    "AutoWorkTable",
                    "Production remains committed after post-deposit cold routing reported a " +
                    "failure. reason=" + (failureReason ?? "null"));
            }
        }

        private bool TryValidateCargoAccess(
            ShuttleModuleRuntimeContext context,
            ShuttleAutoWorkTableModuleDef moduleDef,
            out string failureReason)
        {
            failureReason = null;
            if (context == null)
            {
                failureReason = "AutoWorkTable runtime context is unavailable.";
                return false;
            }

            if (moduleDef.requirePoweredInternalBus && !context.InternalBusPowered)
            {
                failureReason = "Internal power bus is offline.";
                return false;
            }

            if (context.CargoResourceBroker == null || !context.CargoResourceBroker.IsAvailable)
            {
                failureReason = "Cargo broker is not available.";
                return false;
            }

            if (!moduleDef.requiresCargoLogistics)
            {
                return true;
            }

            if (context.Profile == null || context.Profile.CargoLogistics == null)
            {
                failureReason = "Cargo logistics profile is unavailable.";
                return false;
            }

            if (!context.Profile.CargoLogistics.HasCargoLogistics)
            {
                failureReason = "Cargo logistics module is missing.";
                return false;
            }

            if (!context.Profile.CargoLogistics.SupportsItemConsumption)
            {
                failureReason = "Cargo logistics does not support item consumption.";
                return false;
            }

            if (!context.Profile.CargoLogistics.SupportsItemDeposit)
            {
                failureReason = "Cargo logistics does not support item deposit.";
                return false;
            }

            return true;
        }
    }
}

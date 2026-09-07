using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Narrow runtime service for module-owned cargo requests.
    /// It exposes item operations by ThingDef only and never exposes cargo containers.
    /// </summary>
    internal interface IShuttleCargoResourceBroker
    {
        bool IsAvailable { get; }

        // Cheap mutation stamp for idle runtime systems. Reading it must not scan cargo.
        int InventoryRevision { get; }

        int CountAvailable(ThingDef thingDef);

        // Counts shuttle-owned stored cargo regardless of whether logistics/power currently
        // allow ingredient consumption. Used by production policies that need inventory state.
        int CountStored(ThingDef thingDef);

        ShuttleCargoInventorySnapshot GetInventorySnapshot();

        void InvalidateInventorySnapshot();

        IReadOnlyList<ShuttleCargoOrdinaryDepositTargetSnapshot>
            GetOrdinaryDepositTargets();

        bool TryBeginExactWithdrawal(
            CargoStackRef stackRef,
            int count,
            ShuttleCargoAccessRequirement accessRequirement,
            string reason,
            out ShuttleCargoWithdrawal withdrawal,
            out string failureReason);

        bool TryDepositLooseThings(
            IReadOnlyList<ShuttleCargoLooseDepositEntry> entries,
            ShuttleCargoAccessRequirement accessRequirement,
            string reason,
            out int depositedCount,
            out string failureReason);

        IReadOnlyList<CargoThingDefCount> GetAvailableThingDefs(
            ThingFilter ingredientFilter,
            ThingFilter fixedIngredientFilter);

        CargoAvailabilitySnapshot GetAvailabilitySnapshot();

        // Irreversible immediate consumption. This destroys cargo as soon as it
        // is taken from cargo storage, so callers must not use it for production
        // or long-running module transactions that need rollback/save recovery.
        bool TryConsumeImmediatelyAndDestroy(
            ThingDef thingDef,
            int count,
            string reason,
            out int consumedCount);

        [Obsolete("Use TryConsumeImmediatelyAndDestroy only for explicit irreversible consumption. This method destroys cargo immediately, is not rollback-safe, and must not be used for production or long-running module transactions. Use TryTakeBatchToOwner for staged module transactions.")]
        bool TryConsume(
            ThingDef thingDef,
            int count,
            string reason,
            out int consumedCount);

        // Irreversible immediate batch consumption. Prefer TryTakeBatchToOwner
        // for staged ingredients and only destroy after durable module state is
        // committed and no rollback is required.
        bool TryConsumeBatchImmediatelyAndDestroy(
            IReadOnlyList<CargoIngredientRequirement> requirements,
            string reason,
            out int consumedCount,
            out string failureReason);

        [Obsolete("Use TryTakeBatchToOwner for staged, rollback-safe module ingredient transactions. This method destroys cargo immediately, is not rollback-safe, and must not be used for production or long-running module transactions.")]
        bool TryConsumeBatch(
            IReadOnlyList<CargoIngredientRequirement> requirements,
            string reason,
            out int consumedCount,
            out string failureReason);

        bool TryTakeBatchToOwner(
            IReadOnlyList<CargoIngredientRequirement> requirements,
            ThingOwner<Thing> destination,
            string reason,
            out int movedCount,
            out string failureReason);

        bool TryTakeFirstMatchingToOwner(
            Predicate<Thing> matcher,
            ThingOwner<Thing> destination,
            string reason,
            out Thing taken,
            out string failureReason);

        bool TryTransferTo(
            ThingDef thingDef,
            int count,
            ThingOwner<Thing> destination,
            string reason,
            out int movedCount);

        // Atomic ownership transfer into cargo. On failure, source is left unchanged
        // where rollback is possible and depositedCount must be zero.
        bool TryDepositFrom(
            ThingOwner<Thing> source,
            string reason,
            out int depositedCount,
            out string failureReason);

        bool TryDepositFrom(
            ThingOwner<Thing> source,
            string reason,
            out int depositedCount,
            out ShuttleCargoDepositReceipt receipt,
            out string failureReason);
    }
}

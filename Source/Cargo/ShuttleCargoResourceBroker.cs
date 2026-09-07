using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Runtime cargo broker backed by the current cargo backend. Modules receive this narrow
    /// service instead of direct access to CompTransporter or cargo containers.
    /// </summary>
    internal sealed class ShuttleCargoResourceBroker :
        IShuttleCargoResourceBroker,
        IShuttleCargoSupplyReadPort
    {
        private readonly ThingWithComps host;
        private readonly ShuttleProfile profile;
        private readonly ShuttleRuntimeState runtimeState;
        private readonly IShuttleCargoBackend cargoBackend;
        private readonly ShuttleCargoResourceQueryService queryService;
        private readonly ShuttleCargoResourceRequirementPlanner requirementPlanner;
        private readonly ShuttleCargoResourcePreflightService preflightService;
        private readonly ShuttleCargoResourceTransferExecutor transferExecutor;
        private readonly ShuttleCargoExactWithdrawalService exactWithdrawalService;
        private readonly ShuttleCargoLooseDepositService looseDepositService;
        private readonly ShuttleCargoSupplySnapshotBuilder supplySnapshotBuilder;
        private int inventoryRevision;
        private bool inventorySnapshotDirty = true;
        private int cachedRefrigeratedProjectionRevision = int.MinValue;
        private ShuttleCargoInventorySnapshot cachedInventorySnapshot;
        private ShuttleCargoInventorySnapshot cachedSupplySourceSnapshot;
        private ShuttleCargoSupplySnapshot cachedSupplySnapshot;
        private CargoAvailabilitySnapshot cachedAvailabilitySnapshot;

        public ShuttleCargoResourceBroker(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttleCargoBackend cargoBackend)
        {
            this.host = host;
            this.profile = profile;
            this.runtimeState = runtimeState;
            this.cargoBackend = cargoBackend;
            this.queryService = new ShuttleCargoResourceQueryService(host, cargoBackend);
            this.requirementPlanner = new ShuttleCargoResourceRequirementPlanner();
            this.preflightService = new ShuttleCargoResourcePreflightService();
            this.transferExecutor = new ShuttleCargoResourceTransferExecutor(
                host,
                this.queryService,
                this.InvalidateInventorySnapshot);
            ShuttleCargoAccessPolicy accessPolicy = new ShuttleCargoAccessPolicy(
                profile,
                runtimeState);
            this.exactWithdrawalService = new ShuttleCargoExactWithdrawalService(
                host,
                this.transferExecutor,
                accessPolicy,
                this.InvalidateInventorySnapshot);
            this.looseDepositService = new ShuttleCargoLooseDepositService(
                this.queryService,
                accessPolicy,
                this.InvalidateInventorySnapshot);
            this.supplySnapshotBuilder = new ShuttleCargoSupplySnapshotBuilder();
        }

        public bool IsAvailable
        {
            get
            {
                return this.HasCargoLogistics() &&
                    this.HasPoweredInternalBus() &&
                    this.ResolveTransporters().Count > 0;
            }
        }

        public int InventoryRevision
        {
            get
            {
                // Refrigerated cargo has its own projection revision. Synchronizing the stamp is
                // cheap and keeps consumers event-driven without rebuilding the inventory view.
                this.SynchronizeRefrigeratedProjectionRevision();
                return this.inventoryRevision;
            }
        }

        public int CountAvailable(ThingDef thingDef)
        {
            return this.GetAvailabilitySnapshot().CountAvailable(thingDef);
        }

        public int CountStored(ThingDef thingDef)
        {
            return this.GetInventorySnapshot().Count(thingDef);
        }

        public ShuttleCargoInventorySnapshot GetInventorySnapshot()
        {
            this.SynchronizeRefrigeratedProjectionRevision();
            if (!this.ShouldRebuildInventorySnapshot())
            {
                return this.cachedInventorySnapshot;
            }

            this.cachedInventorySnapshot = this.queryService.BuildInventorySnapshot(this.inventoryRevision);
            this.cachedAvailabilitySnapshot = null;
            this.inventorySnapshotDirty = false;
            return this.cachedInventorySnapshot;
        }

        public ShuttleCargoSupplySnapshot GetSupplySnapshot()
        {
            ShuttleCargoInventorySnapshot inventorySnapshot = this.GetInventorySnapshot();
            if (this.cachedSupplySnapshot != null &&
                object.ReferenceEquals(this.cachedSupplySourceSnapshot, inventorySnapshot))
            {
                return this.cachedSupplySnapshot;
            }

            this.cachedSupplySnapshot = this.supplySnapshotBuilder.Build(inventorySnapshot);
            this.cachedSupplySourceSnapshot = inventorySnapshot;
            return this.cachedSupplySnapshot;
        }

        public void InvalidateInventorySnapshot()
        {
            this.MarkInventorySnapshotDirty();
        }

        public IReadOnlyList<ShuttleCargoOrdinaryDepositTargetSnapshot>
            GetOrdinaryDepositTargets()
        {
            return this.queryService.BuildOrdinaryDepositTargets();
        }

        public bool TryBeginExactWithdrawal(
            CargoStackRef stackRef,
            int count,
            ShuttleCargoAccessRequirement accessRequirement,
            string reason,
            out ShuttleCargoWithdrawal withdrawal,
            out string failureReason)
        {
            return this.exactWithdrawalService.TryBegin(
                stackRef,
                count,
                accessRequirement,
                reason,
                out withdrawal,
                out failureReason);
        }

        public bool TryDepositLooseThings(
            IReadOnlyList<ShuttleCargoLooseDepositEntry> entries,
            ShuttleCargoAccessRequirement accessRequirement,
            string reason,
            out int depositedCount,
            out string failureReason)
        {
            return this.looseDepositService.TryDeposit(
                entries,
                accessRequirement,
                reason,
                out depositedCount,
                out failureReason);
        }

        private bool ShouldRebuildInventorySnapshot()
        {
            return this.inventorySnapshotDirty ||
                this.cachedInventorySnapshot == null ||
                this.cachedInventorySnapshot.Revision != this.inventoryRevision;
        }

        private bool ShouldRebuildAvailabilitySnapshot()
        {
            if (this.cachedAvailabilitySnapshot == null || this.inventorySnapshotDirty)
            {
                return true;
            }

            if (this.cachedAvailabilitySnapshot.IsAvailable != this.ComputeAvailabilityGuard())
            {
                return true;
            }

            return false;
        }

        private void SynchronizeRefrigeratedProjectionRevision()
        {
            int currentRevision = this.GetRefrigeratedProjectionRevision();
            if (this.cachedRefrigeratedProjectionRevision == int.MinValue)
            {
                this.cachedRefrigeratedProjectionRevision = currentRevision;
                return;
            }

            if (this.cachedRefrigeratedProjectionRevision == currentRevision)
            {
                return;
            }

            this.cachedRefrigeratedProjectionRevision = currentRevision;
            this.MarkInventorySnapshotDirty();
        }

        private int GetRefrigeratedProjectionRevision()
        {
            CompShuttleRefrigeratedCargoRegistry registry = this.host != null
                ? this.host.TryGetComp<CompShuttleRefrigeratedCargoRegistry>()
                : null;
            return registry != null ? registry.InventoryProjectionRevision : 0;
        }

        private void MarkInventorySnapshotDirty()
        {
            this.inventorySnapshotDirty = true;
            this.inventoryRevision++;
            this.cachedInventorySnapshot = null;
            this.cachedSupplySourceSnapshot = null;
            this.cachedSupplySnapshot = null;
            this.cachedAvailabilitySnapshot = null;
        }

        private bool ComputeAvailabilityGuard()
        {
            return this.HasCargoLogistics() &&
                this.HasPoweredInternalBus() &&
                this.ResolveTransporters().Count > 0;
        }

        private int GetTicksGameSafe()
        {
            return Find.TickManager != null ? Find.TickManager.TicksGame : -1;
        }

        public IReadOnlyList<CargoThingDefCount> GetAvailableThingDefs(
            ThingFilter ingredientFilter,
            ThingFilter fixedIngredientFilter)
        {
            CargoAvailabilitySnapshot snapshot = this.GetAvailabilitySnapshot();
            if (!this.preflightService.CanQueryAvailableThingDefs(
                ingredientFilter,
                snapshot != null ? snapshot.IsAvailable : this.IsAvailable,
                this.profile))
            {
                return new List<CargoThingDefCount>();
            }

            return snapshot.GetAvailableThingDefs(
                ingredientFilter,
                fixedIngredientFilter);
        }

        public CargoAvailabilitySnapshot GetAvailabilitySnapshot()
        {
            if (!this.ShouldRebuildAvailabilitySnapshot())
            {
                return this.cachedAvailabilitySnapshot;
            }

            int createdTick = this.GetTicksGameSafe();
            Dictionary<ThingDef, int> countsByThingDef = new Dictionary<ThingDef, int>();
            if (!this.HasCargoLogistics() || !this.HasPoweredInternalBus())
            {
                this.cachedAvailabilitySnapshot = new CargoAvailabilitySnapshot(
                    createdTick,
                    false,
                    countsByThingDef,
                    0);
                return this.cachedAvailabilitySnapshot;
            }

            if (this.ResolveTransporters().Count == 0)
            {
                this.cachedAvailabilitySnapshot = new CargoAvailabilitySnapshot(
                    createdTick,
                    false,
                    countsByThingDef,
                    0);
                return this.cachedAvailabilitySnapshot;
            }

            ShuttleCargoInventorySnapshot inventorySnapshot = this.GetInventorySnapshot();
            IReadOnlyList<CargoThingDefCount> thingDefCounts =
                inventorySnapshot != null ? inventorySnapshot.ThingDefCounts : null;
            if (thingDefCounts != null)
            {
                for (int i = 0; i < thingDefCounts.Count; i++)
                {
                    CargoThingDefCount count = thingDefCounts[i];
                    if (count == null || count.ThingDef == null || count.Count <= 0)
                    {
                        continue;
                    }

                    countsByThingDef[count.ThingDef] = count.Count;
                }
            }

            this.cachedAvailabilitySnapshot = new CargoAvailabilitySnapshot(
                createdTick,
                true,
                countsByThingDef,
                inventorySnapshot != null ? inventorySnapshot.ScannedStackCount : 0);
            return this.cachedAvailabilitySnapshot;
        }

        public bool TryConsumeImmediatelyAndDestroy(
            ThingDef thingDef,
            int count,
            string reason,
            out int consumedCount)
        {
            consumedCount = 0;
            if (!this.preflightService.CanConsume(
                thingDef,
                count,
                thingDef != null && count > 0 && this.IsAvailable,
                this.profile))
            {
                return false;
            }

            if (this.CountAvailable(thingDef) < count)
            {
                return false;
            }

            List<CargoTakeRecord> takenThings = new List<CargoTakeRecord>();
            int remaining = count;
            if (!this.transferExecutor.TryTakeMatchingThings(thingDef, count, takenThings, out remaining) || remaining > 0)
            {
                string rollbackNotice;
                if (!this.RollBackTakenThings(takenThings, out rollbackNotice))
                {
                    Log.Error("[CeleTech Shuttle] TryConsumeImmediatelyAndDestroy take failed and rollback left unresolved cargo. hostThingID=" +
                        (this.host != null ? this.host.thingIDNumber : -1) +
                        " thingDef=" +
                        this.GetThingDefName(thingDef) +
                        " requestedCount=" +
                        count +
                        " reason=" +
                        (reason ?? "null") +
                        " rollback=" +
                        (rollbackNotice ?? "null"));
                }

                return false;
            }

            for (int i = 0; i < takenThings.Count; i++)
            {
                Thing thing = takenThings[i] != null ? takenThings[i].Thing : null;
                if (thing == null)
                {
                    continue;
                }

                consumedCount += thing.stackCount;
                thing.Destroy(DestroyMode.Vanish);
            }

            bool success = consumedCount == count;
            if (success)
            {
                this.InvalidateInventorySnapshot();
            }

            return success;
        }

        [System.Obsolete("Use TryTakeBatchToOwner for staged, rollback-safe module ingredient transactions. This method destroys cargo immediately, is not rollback-safe, and must not be used for production or long-running module transactions.")]
        public bool TryConsumeBatch(
            IReadOnlyList<CargoIngredientRequirement> requirements,
            string reason,
            out int consumedCount,
            out string failureReason)
        {
            return this.TryConsumeBatchImmediatelyAndDestroy(
                requirements,
                reason,
                out consumedCount,
                out failureReason);
        }

        [System.Obsolete("Use TryConsumeImmediatelyAndDestroy only for explicit irreversible consumption. This method destroys cargo immediately, is not rollback-safe, and must not be used for production or long-running module transactions. Use TryTakeBatchToOwner for staged module transactions.")]
        public bool TryConsume(
            ThingDef thingDef,
            int count,
            string reason,
            out int consumedCount)
        {
            return this.TryConsumeImmediatelyAndDestroy(
                thingDef,
                count,
                reason,
                out consumedCount);
        }

        public bool TryConsumeBatchImmediatelyAndDestroy(
            IReadOnlyList<CargoIngredientRequirement> requirements,
            string reason,
            out int consumedCount,
            out string failureReason)
        {
            consumedCount = 0;
            failureReason = null;

            int requiredTotal;
            List<CargoIngredientRequirement> normalizedRequirements =
                this.requirementPlanner.NormalizeRequirements(requirements, out requiredTotal, out failureReason);
            if (normalizedRequirements == null)
            {
                return false;
            }

            if (!this.preflightService.CanConsumeBatch(
                normalizedRequirements,
                this.IsAvailable,
                this.profile,
                out failureReason))
            {
                return false;
            }

            for (int i = 0; i < normalizedRequirements.Count; i++)
            {
                CargoIngredientRequirement requirement = normalizedRequirements[i];
                if (this.CountAvailable(requirement.ThingDef) < requirement.Count)
                {
                    failureReason = "Cargo lacks required " + this.GetThingDefName(requirement.ThingDef) +
                        " x" + requirement.Count + ".";
                    return false;
                }
            }

            List<CargoTakeRecord> takenThings = new List<CargoTakeRecord>();
            for (int i = 0; i < normalizedRequirements.Count; i++)
            {
                CargoIngredientRequirement requirement = normalizedRequirements[i];
                int remaining;
                if (!this.transferExecutor.TryTakeMatchingThings(
                    requirement.ThingDef,
                    requirement.Count,
                    takenThings,
                    out remaining) ||
                    remaining > 0)
                {
                    string rollbackNotice;
                    if (!this.RollBackTakenThings(takenThings, out rollbackNotice))
                    {
                        failureReason = "Failed to atomically take cargo ingredient " +
                            this.GetThingDefName(requirement.ThingDef) +
                            ", and rollback left unresolved cargo: " +
                            (rollbackNotice ?? "null");
                        Log.Error("[CeleTech Shuttle] TryConsumeBatchImmediatelyAndDestroy take failed and rollback left unresolved cargo. hostThingID=" +
                            (this.host != null ? this.host.thingIDNumber : -1) +
                            " thingDef=" +
                            this.GetThingDefName(requirement.ThingDef) +
                            " requestedCount=" +
                            requirement.Count +
                            " reason=" +
                            (reason ?? "null") +
                            " rollback=" +
                            (rollbackNotice ?? "null"));
                        return false;
                    }

                    failureReason = "Failed to atomically take cargo ingredient " +
                        this.GetThingDefName(requirement.ThingDef) + ".";
                    return false;
                }
            }

            int takenTotal = 0;
            for (int i = 0; i < takenThings.Count; i++)
            {
                Thing thing = takenThings[i] != null ? takenThings[i].Thing : null;
                if (thing != null)
                {
                    takenTotal += thing.stackCount;
                }
            }

            if (takenTotal != requiredTotal)
            {
                string rollbackNotice;
                if (!this.RollBackTakenThings(takenThings, out rollbackNotice))
                {
                    failureReason = "Atomic cargo ingredient take count mismatch, and rollback left unresolved cargo: " +
                        (rollbackNotice ?? "null");
                    Log.Error("[CeleTech Shuttle] TryConsumeBatchImmediatelyAndDestroy count mismatch rollback left unresolved cargo. hostThingID=" +
                        (this.host != null ? this.host.thingIDNumber : -1) +
                        " requiredTotal=" +
                        requiredTotal +
                        " takenTotal=" +
                        takenTotal +
                        " reason=" +
                        (reason ?? "null") +
                        " rollback=" +
                        (rollbackNotice ?? "null"));
                    return false;
                }

                failureReason = "Atomic cargo ingredient take count mismatch.";
                return false;
            }

            for (int i = 0; i < takenThings.Count; i++)
            {
                Thing thing = takenThings[i] != null ? takenThings[i].Thing : null;
                if (thing == null)
                {
                    continue;
                }

                consumedCount += thing.stackCount;
                thing.Destroy(DestroyMode.Vanish);
            }

            if (consumedCount != requiredTotal)
            {
                failureReason = "Atomic cargo ingredient consume count mismatch.";
                return false;
            }

            this.InvalidateInventorySnapshot();
            return true;
        }

        public bool TryTakeBatchToOwner(
            IReadOnlyList<CargoIngredientRequirement> requirements,
            ThingOwner<Thing> destination,
            string reason,
            out int movedCount,
            out string failureReason)
        {
            movedCount = 0;
            failureReason = null;

            if (destination == null)
            {
                failureReason = "Destination owner for cargo ingredient staging is missing.";
                return false;
            }

            int requiredTotal;
            List<CargoIngredientRequirement> normalizedRequirements =
                this.requirementPlanner.NormalizeRequirements(requirements, out requiredTotal, out failureReason);
            if (normalizedRequirements == null)
            {
                return false;
            }

            if (!this.preflightService.CanConsumeBatch(
                normalizedRequirements,
                this.IsAvailable,
                this.profile,
                out failureReason))
            {
                return false;
            }

            for (int i = 0; i < normalizedRequirements.Count; i++)
            {
                CargoIngredientRequirement requirement = normalizedRequirements[i];
                if (this.CountAvailable(requirement.ThingDef) < requirement.Count)
                {
                    failureReason = "Cargo lacks required " + this.GetThingDefName(requirement.ThingDef) +
                        " x" + requirement.Count + ".";
                    return false;
                }
            }

            List<CargoTakeRecord> takenThings = new List<CargoTakeRecord>();
            for (int i = 0; i < normalizedRequirements.Count; i++)
            {
                CargoIngredientRequirement requirement = normalizedRequirements[i];
                int remaining;
                if (!this.transferExecutor.TryTakeMatchingThings(
                    requirement.ThingDef,
                    requirement.Count,
                    takenThings,
                    out remaining) ||
                    remaining > 0)
                {
                    string rollbackNotice;
                    if (!this.RollBackTakenThings(takenThings, out rollbackNotice))
                    {
                        failureReason = "Failed to atomically stage cargo ingredient " +
                            this.GetThingDefName(requirement.ThingDef) +
                            ", and rollback left unresolved cargo: " +
                            (rollbackNotice ?? "null");
                        return false;
                    }

                    failureReason = "Failed to atomically stage cargo ingredient " +
                        this.GetThingDefName(requirement.ThingDef) + ".";
                    return false;
                }
            }

            int takenTotal = this.CountTakenThings(takenThings);
            if (takenTotal != requiredTotal)
            {
                string rollbackNotice;
                if (!this.RollBackTakenThings(takenThings, out rollbackNotice))
                {
                    failureReason = "Atomic cargo ingredient staging count mismatch, and rollback left unresolved cargo: " +
                        (rollbackNotice ?? "null");
                    return false;
                }

                failureReason = "Atomic cargo ingredient staging count mismatch.";
                return false;
            }

            List<CargoTransferRecord> transferredThings = new List<CargoTransferRecord>();
            for (int i = 0; i < takenThings.Count; i++)
            {
                CargoTakeRecord takeRecord = takenThings[i];
                Thing thing = takeRecord != null ? takeRecord.Thing : null;
                if (thing == null)
                {
                    continue;
                }

                int stackCount = thing.stackCount;
                if (!destination.TryAdd(thing, false))
                {
                    string transferRollbackNotice;
                    string takeRollbackNotice;
                    bool transferredRollbackSafe = this.RollBackTransferredThings(transferredThings, out transferRollbackNotice);
                    bool takenRollbackSafe = this.RollBackTakenThingsFromIndex(takenThings, i, out takeRollbackNotice);
                    if (!transferredRollbackSafe || !takenRollbackSafe)
                    {
                        Log.Error("[CeleTech Shuttle] Cargo ingredient staging destination rejected thing and rollback left unresolved cargo. hostThingID=" +
                            (this.host != null ? this.host.thingIDNumber : -1) +
                            " thing=" +
                            this.DescribeThing(thing) +
                            " reason=" +
                            (reason ?? "null") +
                            " transferredRollback=" +
                            (transferRollbackNotice ?? "null") +
                            " takenRollback=" +
                            (takeRollbackNotice ?? "null"));
                    }

                    failureReason = "Failed to stage cargo ingredient into destination owner. transferredRollback=" +
                        (transferRollbackNotice ?? "null") +
                        " takenRollback=" +
                        (takeRollbackNotice ?? "null");
                    return false;
                }

                movedCount += stackCount;
                transferredThings.Add(new CargoTransferRecord(
                    takeRecord.SourceTransporter,
                    takeRecord.SourceContents,
                    destination,
                    thing));
            }

            if (movedCount != requiredTotal)
            {
                string rollbackNotice;
                if (!this.RollBackTransferredThings(transferredThings, out rollbackNotice))
                {
                    failureReason = "Cargo ingredient staging count mismatch, and rollback left unresolved cargo: " +
                        (rollbackNotice ?? "null");
                    return false;
                }

                failureReason = "Cargo ingredient staging count mismatch.";
                return false;
            }

            this.InvalidateInventorySnapshot();
            return true;
        }

        public bool TryTakeFirstMatchingToOwner(
            System.Predicate<Thing> matcher,
            ThingOwner<Thing> destination,
            string reason,
            out Thing taken,
            out string failureReason)
        {
            taken = null;
            failureReason = null;

            if (matcher == null)
            {
                failureReason = "Cargo matcher is missing.";
                return false;
            }

            if (destination == null)
            {
                failureReason = "Destination owner for cargo staging is missing.";
                return false;
            }

            if (!this.IsAvailable ||
                this.profile == null ||
                this.profile.CargoLogistics == null ||
                !this.profile.CargoLogistics.SupportsItemConsumption)
            {
                failureReason = "Cargo logistics does not support item consumption.";
                return false;
            }

            CargoTakeRecord takeRecord;
            if (!this.transferExecutor.TryTakeFirstMatchingThing(matcher, out takeRecord) ||
                takeRecord == null ||
                takeRecord.Thing == null)
            {
                failureReason = "No matching loaded cargo item is available.";
                return false;
            }

            int stackCount = takeRecord.Thing.stackCount;
            if (!destination.TryAdd(takeRecord.Thing, false))
            {
                string takeRollbackNotice;
                bool rollbackSafe = this.RollBackTakenThings(
                    new List<CargoTakeRecord> { takeRecord },
                    out takeRollbackNotice);
                if (!rollbackSafe)
                {
                    Log.Error("[CeleTech Shuttle] Predicate cargo staging destination rejected thing and rollback left unresolved cargo. hostThingID=" +
                        (this.host != null ? this.host.thingIDNumber : -1) +
                        " thing=" +
                        this.DescribeThing(takeRecord.Thing) +
                        " reason=" +
                        (reason ?? "null") +
                        " rollback=" +
                        (takeRollbackNotice ?? "null"));
                }

                failureReason = "Failed to stage matching cargo item into destination owner. rollback=" +
                    (takeRollbackNotice ?? "null");
                return false;
            }

            taken = takeRecord.Thing;
            if (taken == null || taken.stackCount != stackCount)
            {
                // Stack count should be stable for whole-thing predicate transfers, but the
                // staged owner is already durable if this ever changes.
                taken = takeRecord.Thing;
            }

            this.InvalidateInventorySnapshot();
            return true;
        }

        public bool TryTransferTo(
            ThingDef thingDef,
            int count,
            ThingOwner<Thing> destination,
            string reason,
            out int movedCount)
        {
            movedCount = 0;
            if (!this.preflightService.CanTransfer(
                thingDef,
                count,
                destination,
                thingDef != null && count > 0 && destination != null && this.IsAvailable,
                this.profile))
            {
                return false;
            }

            if (this.CountAvailable(thingDef) < count)
            {
                return false;
            }

            List<CargoTakeRecord> takenThings = new List<CargoTakeRecord>();
            int remaining = count;
            if (!this.transferExecutor.TryTakeMatchingThings(thingDef, count, takenThings, out remaining) || remaining > 0)
            {
                string rollbackNotice;
                if (!this.RollBackTakenThings(takenThings, out rollbackNotice))
                {
                    Log.Error("[CeleTech Shuttle] Cargo transfer take failed and rollback left unresolved cargo. rollback=" +
                        (rollbackNotice ?? "null"));
                }

                return false;
            }

            List<CargoTransferRecord> transferredThings = new List<CargoTransferRecord>();
            for (int i = 0; i < takenThings.Count; i++)
            {
                CargoTakeRecord takeRecord = takenThings[i];
                Thing thing = takeRecord != null ? takeRecord.Thing : null;
                if (thing == null)
                {
                    continue;
                }

                int stackCount = thing.stackCount;
                if (!destination.TryAdd(thing, false))
                {
                    string transferRollbackNotice;
                    string takeRollbackNotice;
                    bool transferredRollbackSafe = this.RollBackTransferredThings(transferredThings, out transferRollbackNotice);
                    bool takenRollbackSafe = this.RollBackTakenThingsFromIndex(takenThings, i, out takeRollbackNotice);
                    if (!transferredRollbackSafe || !takenRollbackSafe)
                    {
                        Log.Error("[CeleTech Shuttle] Cargo transfer failed and rollback left unresolved cargo. transferredRollback=" +
                            (transferRollbackNotice ?? "null") +
                            " takenRollback=" +
                            (takeRollbackNotice ?? "null"));
                    }

                    return false;
                }

                movedCount += stackCount;
                transferredThings.Add(new CargoTransferRecord(
                    takeRecord.SourceTransporter,
                    takeRecord.SourceContents,
                    destination,
                    thing));
            }

            if (movedCount != count)
            {
                string transferRollbackNotice;
                if (!this.RollBackTransferredThings(transferredThings, out transferRollbackNotice))
                {
                    Log.Error("[CeleTech Shuttle] Cargo transfer count mismatch and rollback left unresolved cargo. rollback=" +
                        (transferRollbackNotice ?? "null"));
                }

                return false;
            }

            this.InvalidateInventorySnapshot();
            return true;
        }

        public bool TryDepositFrom(
            ThingOwner<Thing> source,
            string reason,
            out int depositedCount,
            out string failureReason)
        {
            ShuttleCargoDepositReceipt receipt;
            return this.TryDepositFrom(
                source,
                reason,
                out depositedCount,
                out receipt,
                out failureReason);
        }

        public bool TryDepositFrom(
            ThingOwner<Thing> source,
            string reason,
            out int depositedCount,
            out ShuttleCargoDepositReceipt receipt,
            out string failureReason)
        {
            depositedCount = 0;
            receipt = ShuttleCargoDepositReceipt.Empty();
            failureReason = null;

            string validationFailureReason;
            if (!this.preflightService.CanDeposit(
                source,
                source != null && this.IsAvailable,
                this.profile,
                out validationFailureReason))
            {
                return this.FailDeposit(validationFailureReason, out failureReason, ref depositedCount);
            }

            if (source.Count == 0)
            {
                return true;
            }

            if (!this.preflightService.ValidateDepositSource(source, out validationFailureReason))
            {
                return this.FailDeposit(validationFailureReason, out failureReason, ref depositedCount);
            }

            if (!this.preflightService.HasMassCapacityForDeposit(
                source,
                this.host,
                this.cargoBackend,
                out validationFailureReason))
            {
                return this.FailDeposit(validationFailureReason, out failureReason, ref depositedCount);
            }

            CompTransporter destinationTransporter;
            ThingOwner destinationContents;
            int destinationTransporterIndex;
            if (!this.TryGetDepositDestination(
                    out destinationTransporter,
                    out destinationContents,
                    out destinationTransporterIndex))
            {
                return this.FailDeposit(
                    "No cargo deposit destination is available.",
                    out failureReason,
                    ref depositedCount);
            }

            List<CargoDepositRecord> depositedThings = new List<CargoDepositRecord>();
            List<CargoStackRef> depositedStackRefs = new List<CargoStackRef>();
            for (int i = source.Count - 1; i >= 0; i--)
            {
                Thing thing = source[i];
                if (!ShuttleCargoResourceMatcher.MatchesAvailableThing(thing))
                {
                    continue;
                }

                int stackCount = thing.stackCount;
                Thing taken = source.Take(thing, stackCount);
                if (taken == null)
                {
                    string rollbackNotice;
                    this.RollBackDepositedThings(depositedThings, source, out rollbackNotice);
                    return this.FailDeposit(
                        "Failed to take pending product for cargo deposit. rollback=" +
                            (rollbackNotice ?? "null"),
                        out failureReason,
                        ref depositedCount);
                }

                if (!destinationContents.TryAdd(taken, false))
                {
                    ShuttleTransferRecoveryStatus recoveryStatus;
                    string recoveryFailureReason;
                    if (!this.TryRecoverCargoTransferThing(
                        taken,
                        "Cargo deposit destination rejected pending product.",
                        source,
                        null,
                        out recoveryStatus,
                        out recoveryFailureReason))
                    {
                        string depositRollbackNotice;
                        this.RollBackDepositedThings(depositedThings, source, out depositRollbackNotice);
                        return this.FailDeposit(
                            "Cargo deposit destination rejected pending product, and emergency recovery failed: " +
                                (recoveryFailureReason ?? "null"),
                            out failureReason,
                            ref depositedCount);
                    }

                    string rollbackNotice;
                    this.RollBackDepositedThings(depositedThings, source, out rollbackNotice);
                    return this.FailDeposit(
                        "Cargo deposit destination rejected pending product. recoveryStatus=" +
                            recoveryStatus +
                            " recovery=" +
                            (recoveryFailureReason ?? "null") +
                            " rollback=" +
                            (rollbackNotice ?? "null"),
                        out failureReason,
                        ref depositedCount);
                }

                depositedCount += stackCount;
                this.InvalidateInventorySnapshot();
                destinationTransporter.Notify_ThingAdded(taken);
                depositedThings.Add(new CargoDepositRecord(
                    destinationTransporter,
                    destinationContents,
                    taken));
                depositedStackRefs.Add(new CargoStackRef(
                    taken.thingIDNumber,
                    ShuttleCargoResourceMatcher.GetThingDefName(taken.def),
                    taken.stackCount,
                    ShuttleCargoInventorySourceKind.RegularCargo,
                    destinationTransporterIndex,
                    null));
            }

            if (source.Count != 0)
            {
                string rollbackNotice;
                this.RollBackDepositedThings(depositedThings, source, out rollbackNotice);
                return this.FailDeposit(
                    "Cargo deposit left pending products behind. rollback=" +
                        (rollbackNotice ?? "null"),
                    out failureReason,
                    ref depositedCount);
            }

            this.InvalidateInventorySnapshot();
            receipt = new ShuttleCargoDepositReceipt(
                depositedStackRefs,
                depositedCount);
            return true;
        }

        private bool TryGetDepositDestination(
            out CompTransporter destinationTransporter,
            out ThingOwner destinationContents,
            out int destinationTransporterIndex)
        {
            destinationTransporter = null;
            destinationContents = null;
            destinationTransporterIndex = -1;

            List<CompTransporter> transporters = this.ResolveTransporters();
            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                ThingOwner contents = this.GetContents(transporter);
                if (contents == null)
                {
                    continue;
                }

                destinationTransporter = transporter;
                destinationContents = contents;
                destinationTransporterIndex = i;
                return true;
            }

            return false;
        }

        private void AddAvailableThingDefCount(
            List<CargoThingDefCount> thingDefCounts,
            ThingDef thingDef,
            int count)
        {
            if (thingDefCounts == null || thingDef == null || count <= 0)
            {
                return;
            }

            for (int i = 0; i < thingDefCounts.Count; i++)
            {
                CargoThingDefCount existing = thingDefCounts[i];
                if (existing != null && existing.ThingDef == thingDef)
                {
                    thingDefCounts[i] = new CargoThingDefCount(thingDef, existing.Count + count);
                    return;
                }
            }

            thingDefCounts.Add(new CargoThingDefCount(thingDef, count));
        }

        private void AddAvailableThingDefCount(
            Dictionary<ThingDef, int> thingDefCounts,
            ThingDef thingDef,
            int count)
        {
            if (thingDefCounts == null || thingDef == null || count <= 0)
            {
                return;
            }

            int existingCount;
            thingDefCounts.TryGetValue(thingDef, out existingCount);
            thingDefCounts[thingDef] = existingCount + count;
        }

        private bool FailDeposit(
            string reason,
            out string failureReason,
            ref int depositedCount)
        {
            depositedCount = 0;
            failureReason = reason;
            return false;
        }

        private bool RollBackTakenThings(
            List<CargoTakeRecord> takenThings,
            out string recoveryNotice)
        {
            return this.RollBackTakenThingsFromIndex(takenThings, 0, out recoveryNotice);
        }

        private bool RollBackTakenThingsFromIndex(
            List<CargoTakeRecord> takenThings,
            int startIndex,
            out string recoveryNotice)
        {
            recoveryNotice = null;
            if (takenThings == null)
            {
                return true;
            }

            bool success = true;
            for (int i = takenThings.Count - 1; i >= startIndex; i--)
            {
                CargoTakeRecord record = takenThings[i];
                if (record == null || record.Thing == null || record.SourceContents == null)
                {
                    continue;
                }

                if (record.SourceContents.TryAddOrTransfer(record.Thing, false))
                {
                    this.InvalidateInventorySnapshot();
                    if (record.SourceTransporter != null)
                    {
                        record.SourceTransporter.Notify_ThingAdded(record.Thing);
                    }

                    recoveryNotice = AppendNote(recoveryNotice, "returned taken cargo " + this.DescribeThing(record.Thing));
                    continue;
                }

                ShuttleTransferRecoveryStatus recoveryStatus;
                string recoveryFailureReason;
                if (this.TryRecoverCargoTransferThing(
                    record.Thing,
                    "Cargo take rollback to source failed.",
                    record.SourceContents,
                    null,
                    out recoveryStatus,
                    out recoveryFailureReason))
                {
                    if (recoveryStatus == ShuttleTransferRecoveryStatus.RecoveredToPreferredOwner &&
                        record.SourceTransporter != null)
                    {
                        record.SourceTransporter.Notify_ThingAdded(record.Thing);
                    }

                    recoveryNotice = AppendNote(
                        recoveryNotice,
                        "recovered taken cargo " +
                            this.DescribeThing(record.Thing) +
                            " status=" +
                            recoveryStatus +
                            " note=" +
                            (recoveryFailureReason ?? "null"));
                    continue;
                }

                success = false;
                recoveryNotice = AppendNote(
                    recoveryNotice,
                    "fatal unresolved taken cargo " +
                        this.DescribeThing(record.Thing) +
                        " recovery=" +
                        (recoveryFailureReason ?? "null"));
            }

            return success;
        }

        private bool RollBackTransferredThings(
            List<CargoTransferRecord> transferredThings,
            out string recoveryNotice)
        {
            recoveryNotice = null;
            if (transferredThings == null)
            {
                return true;
            }

            bool success = true;
            for (int i = transferredThings.Count - 1; i >= 0; i--)
            {
                CargoTransferRecord record = transferredThings[i];
                if (record == null ||
                    record.Thing == null ||
                    record.SourceContents == null ||
                    record.Destination == null)
                {
                    continue;
                }

                Thing returnedThing;
                int moved = record.Destination.TryTransferToContainer(
                    record.Thing,
                    record.SourceContents,
                    record.Thing.stackCount,
                    out returnedThing,
                    false);
                if (moved > 0 && record.SourceTransporter != null)
                {
                    this.InvalidateInventorySnapshot();
                    record.SourceTransporter.Notify_ThingAdded(returnedThing ?? record.Thing);
                    recoveryNotice = AppendNote(recoveryNotice, "rolled back transferred cargo " + this.DescribeThing(returnedThing ?? record.Thing));
                    continue;
                }

                if (moved > 0)
                {
                    this.InvalidateInventorySnapshot();
                    recoveryNotice = AppendNote(recoveryNotice, "rolled back transferred cargo " + this.DescribeThing(returnedThing ?? record.Thing));
                    continue;
                }

                ShuttleTransferRecoveryStatus recoveryStatus;
                string recoveryFailureReason;
                if (this.TryRecoverCargoTransferThing(
                    record.Thing,
                    "Cargo transferred rollback to source failed.",
                    record.SourceContents,
                    record.Destination,
                    out recoveryStatus,
                    out recoveryFailureReason))
                {
                    if (recoveryStatus == ShuttleTransferRecoveryStatus.RecoveredToPreferredOwner &&
                        record.SourceTransporter != null)
                    {
                        record.SourceTransporter.Notify_ThingAdded(record.Thing);
                    }

                    recoveryNotice = AppendNote(
                        recoveryNotice,
                        "recovered transferred cargo " +
                            this.DescribeThing(record.Thing) +
                            " status=" +
                            recoveryStatus +
                            " note=" +
                            (recoveryFailureReason ?? "null"));
                    continue;
                }

                success = false;
                recoveryNotice = AppendNote(
                    recoveryNotice,
                    "fatal unresolved transferred cargo " +
                        this.DescribeThing(record.Thing) +
                        " recovery=" +
                        (recoveryFailureReason ?? "null"));
            }

            return success;
        }

        private bool RollBackDepositedThings(
            List<CargoDepositRecord> depositedThings,
            ThingOwner<Thing> source,
            out string recoveryNotice)
        {
            recoveryNotice = null;
            if (depositedThings == null || source == null)
            {
                return true;
            }

            bool success = true;
            for (int i = depositedThings.Count - 1; i >= 0; i--)
            {
                CargoDepositRecord record = depositedThings[i];
                if (record == null ||
                    record.Thing == null ||
                    record.DestinationContents == null)
                {
                    continue;
                }

                Thing returnedThing;
                int moved = record.DestinationContents.TryTransferToContainer(
                    record.Thing,
                    source,
                    record.Thing.stackCount,
                    out returnedThing,
                    false);
                if (moved > 0 && record.DestinationTransporter != null)
                {
                    this.InvalidateInventorySnapshot();
                    record.DestinationTransporter.Notify_ThingRemoved(returnedThing ?? record.Thing);
                    recoveryNotice = AppendNote(recoveryNotice, "rolled back deposited cargo " + this.DescribeThing(returnedThing ?? record.Thing));
                    continue;
                }

                if (moved > 0)
                {
                    this.InvalidateInventorySnapshot();
                    recoveryNotice = AppendNote(recoveryNotice, "rolled back deposited cargo " + this.DescribeThing(returnedThing ?? record.Thing));
                    continue;
                }

                ShuttleTransferRecoveryStatus recoveryStatus;
                string recoveryFailureReason;
                if (this.TryRecoverCargoTransferThing(
                    record.Thing,
                    "Cargo deposit rollback to product source failed.",
                    source,
                    record.DestinationContents,
                    out recoveryStatus,
                    out recoveryFailureReason))
                {
                    if (recoveryStatus == ShuttleTransferRecoveryStatus.RecoveredToFallbackOwner &&
                        record.DestinationTransporter != null)
                    {
                        record.DestinationTransporter.Notify_ThingAdded(record.Thing);
                    }

                    recoveryNotice = AppendNote(
                        recoveryNotice,
                        "recovered deposited cargo " +
                            this.DescribeThing(record.Thing) +
                            " status=" +
                            recoveryStatus +
                            " note=" +
                            (recoveryFailureReason ?? "null"));
                    continue;
                }

                success = false;
                recoveryNotice = AppendNote(
                    recoveryNotice,
                    "fatal unresolved deposited cargo " +
                        this.DescribeThing(record.Thing) +
                        " recovery=" +
                        (recoveryFailureReason ?? "null"));
            }

            return success;
        }

        private bool TryRecoverCargoTransferThing(
            Thing thing,
            string reason,
            ThingOwner preferredOwner,
            ThingOwner fallbackOwner,
            out ShuttleTransferRecoveryStatus recoveryStatus,
            out string recoveryNotice)
        {
            recoveryStatus = ShuttleTransferRecoveryStatus.None;
            recoveryNotice = null;
            CompShuttleHolderLaunchTransferState transferState = this.GetTransferState();
            if (transferState == null)
            {
                recoveryStatus = ShuttleTransferRecoveryStatus.FatalUnresolved;
                recoveryNotice = "holder transfer state is unavailable";
                Log.Error("[CeleTech Shuttle] Cargo transfer emergency recovery failed for " +
                    this.DescribeThing(thing) +
                    ": " +
                    recoveryNotice +
                    " reason=" +
                    (reason ?? "null"));
                return false;
            }

            if (transferState.TryRecoverTransferThing(
                thing,
                reason,
                preferredOwner,
                fallbackOwner,
                this.host != null ? this.host.Map : null,
                this.host != null ? this.host.Position : IntVec3.Invalid,
                out recoveryStatus,
                out recoveryNotice))
            {
                this.InvalidateInventorySnapshot();
                return true;
            }

            Log.Error("[CeleTech Shuttle] Cargo transfer emergency recovery failed for " +
                this.DescribeThing(thing) +
                ": " +
                (recoveryNotice ?? "null") +
                " reason=" +
                (reason ?? "null"));
            return false;
        }

        private List<CompTransporter> ResolveTransporters()
        {
            return this.queryService.ResolveTransporters();
        }

        private ThingOwner GetContents(CompTransporter transporter)
        {
            return ShuttleCargoResourceQueryService.GetContents(transporter);
        }

        private CompShuttleHolderLaunchTransferState GetTransferState()
        {
            return this.host != null
                ? this.host.TryGetComp<CompShuttleHolderLaunchTransferState>()
                : null;
        }

        private bool HasCargoLogistics()
        {
            return this.profile != null &&
                this.profile.CargoLogistics != null &&
                this.profile.CargoLogistics.HasCargoLogistics;
        }

        private bool HasPoweredInternalBus()
        {
            return this.runtimeState != null &&
                this.runtimeState.Power != null &&
                this.runtimeState.Power.InternalBusPowered;
        }

        private string GetThingDefName(ThingDef thingDef)
        {
            return thingDef != null && !string.IsNullOrEmpty(thingDef.defName)
                ? thingDef.defName
                : string.Empty;
        }

        private string DescribeThing(Thing thing)
        {
            if (thing == null)
            {
                return "null thing";
            }

            string defName = thing.def != null ? thing.def.defName : "null-def";
            return defName + "#" + thing.thingIDNumber + " x" + thing.stackCount;
        }

        private int CountTakenThings(List<CargoTakeRecord> takenThings)
        {
            int count = 0;
            if (takenThings == null)
            {
                return count;
            }

            for (int i = 0; i < takenThings.Count; i++)
            {
                Thing thing = takenThings[i] != null ? takenThings[i].Thing : null;
                if (thing != null)
                {
                    count += thing.stackCount;
                }
            }

            return count;
        }

        private static string AppendNote(string current, string note)
        {
            if (string.IsNullOrEmpty(note))
            {
                return current;
            }

            return string.IsNullOrEmpty(current) ? note : current + " | " + note;
        }

        private sealed class CargoTransferRecord
        {
            public CargoTransferRecord(
                CompTransporter sourceTransporter,
                ThingOwner sourceContents,
                ThingOwner<Thing> destination,
                Thing thing)
            {
                this.SourceTransporter = sourceTransporter;
                this.SourceContents = sourceContents;
                this.Destination = destination;
                this.Thing = thing;
            }

            public CompTransporter SourceTransporter { get; private set; }
            public ThingOwner SourceContents { get; private set; }
            public ThingOwner<Thing> Destination { get; private set; }
            public Thing Thing { get; private set; }
        }

        private sealed class CargoDepositRecord
        {
            public CargoDepositRecord(
                CompTransporter destinationTransporter,
                ThingOwner destinationContents,
                Thing thing)
            {
                this.DestinationTransporter = destinationTransporter;
                this.DestinationContents = destinationContents;
                this.Thing = thing;
            }

            public CompTransporter DestinationTransporter { get; private set; }
            public ThingOwner DestinationContents { get; private set; }
            public Thing Thing { get; private set; }
        }
    }
}

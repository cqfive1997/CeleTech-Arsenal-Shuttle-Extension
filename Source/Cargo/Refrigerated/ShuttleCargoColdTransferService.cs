using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated
{
    internal sealed class ShuttleCargoColdTransferService : IShuttleCargoColdTransferService
    {
        private const float MassEpsilon = 0.0001f;

        private readonly ThingWithComps host;
        private readonly ShuttleAssemblyState assemblyState;
        private readonly ShuttleRuntimeState runtimeState;
        private readonly ShuttleProfile profile;
        private readonly IShuttleCargoBackend cargoBackend;
        private readonly ShuttleColdTransferQueryService queryService;
        private readonly ShuttleColdTransferPreflightService preflightService;
        private readonly ShuttleColdAutoTransferPlanner autoTransferPlanner;
        private ColdAutoTransferCandidateSnapshot cachedAutoTransferCandidateSnapshot;

        internal ShuttleCargoColdTransferService(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            ShuttleProfile profile,
            IShuttleCargoBackend cargoBackend)
        {
            this.host = host;
            this.assemblyState = assemblyState;
            this.runtimeState = runtimeState;
            this.profile = profile;
            this.cargoBackend = cargoBackend;
            this.queryService = new ShuttleColdTransferQueryService(host, cargoBackend);
            this.preflightService = new ShuttleColdTransferPreflightService();
            this.autoTransferPlanner = new ShuttleColdAutoTransferPlanner(
                this.queryService,
                this.preflightService,
                host,
                profile);
        }

        public bool TryRouteLoadedCargoToCold(
            string moduleInstanceID,
            CompTransporter sourceTransporter,
            Thing deliveredThing,
            int count,
            string reason,
            out int movedCount,
            out string failureReason)
        {
            movedCount = 0;
            failureReason = null;
            if (this.HasActiveGlobalUnload())
            {
                failureReason = "CT_Shuttle_Cargo_ManualMoveBlockedByUnloading".Translate().ToString();
                return false;
            }

            this.ReconcileRegistry();

            try
            {
                RefrigeratedCargoRecord record;
                ShuttleModule module;
                ShuttleRefrigeratedCargoModuleDef moduleDef;
                if (!this.TryResolveColdRecord(
                    moduleInstanceID,
                    out record,
                    out module,
                    out moduleDef,
                    out failureReason))
                {
                    return false;
                }

                if (!this.preflightService.TryValidateLoadRoutingIntoCold(
                    module,
                    moduleDef,
                    record,
                    this.host,
                    this.cargoBackend,
                    out failureReason))
                {
                    return false;
                }

                ThingOwner sourceContents;
                Thing sourceThing;
                if (!this.queryService.TryResolveDeliveredLoadedCargo(
                    sourceTransporter,
                    deliveredThing,
                    count,
                    out sourceContents,
                    out sourceThing,
                    out failureReason))
                {
                    return false;
                }

                int moveCount;
                ShuttleRefrigeratedCargoAutoTransferConfig autoTransferConfig =
                    this.ResolveAutoTransferConfig(moduleInstanceID, moduleDef);
                if (!this.preflightService.TryValidateLoadRoutedMoveIntoCold(
                    sourceThing,
                    moduleDef,
                    autoTransferConfig != null ? autoTransferConfig.AutoTransferFilter : null,
                    autoTransferConfig != null && autoTransferConfig.HasCustomAutoTransferFilter,
                    count,
                    out moveCount,
                    out failureReason))
                {
                    return false;
                }

                float moveMassKg = CargoDisplayUtility.GetThingMass(sourceThing, moveCount);
                if (!this.preflightService.HasColdCapacity(
                    record,
                    this.profile,
                    this.GetRegistry(),
                    moveMassKg,
                    out failureReason))
                {
                    return false;
                }

                Thing taken = sourceContents.Take(sourceThing, moveCount);
                if (taken == null)
                {
                    failureReason = "Failed to take newly loaded cargo for refrigerated routing.";
                    return false;
                }

                int takenCount = taken.stackCount;
                if (sourceTransporter != null)
                {
                    sourceTransporter.Notify_ThingRemoved(taken);
                }

                if (!record.Contents.TryAdd(taken, false))
                {
                    string rollbackFailureReason;
                    ShuttleTransferRecoveryStatus rollbackStatus;
                    if (!this.TryRollbackToLoadedCargo(
                        sourceContents,
                        sourceTransporter,
                        taken,
                        out rollbackStatus,
                        out rollbackFailureReason))
                    {
                        failureReason = "Refrigerated load routing rollback failed: " +
                            rollbackFailureReason;
                        return false;
                    }

                    failureReason = "Refrigerated cargo holder rejected the newly loaded item. rollbackStatus=" +
                        rollbackStatus;
                    return false;
                }

                movedCount = takenCount;
                return true;
            }
            finally
            {
                this.ReconcileRegistry();
            }
        }

        public bool TryTransferLoadedCargoToCold(
            string moduleInstanceID,
            int transporterIndex,
            int loadedIndex,
            int thingIDNumber,
            string expectedDefName,
            int count,
            string reason,
            out int movedCount,
            out string failureReason)
        {
            movedCount = 0;
            failureReason = null;
            if (this.HasActiveGlobalUnload())
            {
                failureReason = "CT_Shuttle_Cargo_ManualMoveBlockedByUnloading".Translate().ToString();
                return false;
            }

            this.ReconcileRegistry();

            try
            {
                RefrigeratedCargoRecord record;
                ShuttleModule module;
                ShuttleRefrigeratedCargoModuleDef moduleDef;
                if (!this.TryResolveColdRecord(
                    moduleInstanceID,
                    out record,
                    out module,
                    out moduleDef,
                    out failureReason))
                {
                    return false;
                }

                if (!this.preflightService.TryValidateTransferIntoCold(
                    module,
                    moduleDef,
                    record,
                    this.host,
                    this.cargoBackend,
                    this.profile,
                    out failureReason))
                {
                    return false;
                }

                CompTransporter sourceTransporter;
                ThingOwner sourceContents;
                Thing sourceThing;
                if (!this.queryService.TryResolveLoadedCargo(
                    transporterIndex,
                    loadedIndex,
                    thingIDNumber,
                    expectedDefName,
                    out sourceTransporter,
                    out sourceContents,
                    out sourceThing,
                    out failureReason))
                {
                    return false;
                }

                int moveCount;
                ShuttleRefrigeratedCargoAutoTransferConfig autoTransferConfig =
                    this.ResolveAutoTransferConfig(moduleInstanceID, moduleDef);
                if (!this.preflightService.TryValidateMoveIntoCold(
                    sourceThing,
                    moduleDef,
                    autoTransferConfig != null ? autoTransferConfig.AutoTransferFilter : null,
                    autoTransferConfig != null && autoTransferConfig.HasCustomAutoTransferFilter,
                    count,
                    out moveCount,
                    out failureReason))
                {
                    return false;
                }

                float moveMassKg = CargoDisplayUtility.GetThingMass(sourceThing, moveCount);
                if (!this.preflightService.HasColdCapacity(
                    record,
                    this.profile,
                    this.GetRegistry(),
                    moveMassKg,
                    out failureReason))
                {
                    return false;
                }

                Thing taken = sourceContents.Take(sourceThing, moveCount);
                if (taken == null)
                {
                    failureReason = "Failed to take loaded cargo for refrigerated transfer.";
                    return false;
                }

                int takenCount = taken.stackCount;
                if (sourceTransporter != null)
                {
                    sourceTransporter.Notify_ThingRemoved(taken);
                }

                if (!record.Contents.TryAdd(taken, false))
                {
                    string rollbackFailureReason;
                    ShuttleTransferRecoveryStatus rollbackStatus;
                    if (!this.TryRollbackToLoadedCargo(
                        sourceContents,
                        sourceTransporter,
                        taken,
                        out rollbackStatus,
                        out rollbackFailureReason))
                    {
                        movedCount = 0;
                        failureReason = "Refrigerated cargo holder rejected the transferred item, and rollback to normal cargo failed: " +
                            rollbackFailureReason;
                        return false;
                    }

                    movedCount = 0;
                    failureReason = "Refrigerated cargo holder rejected the transferred item. rollbackStatus=" +
                        rollbackStatus;
                    return false;
                }

                movedCount = takenCount;
                return true;
            }
            finally
            {
                this.ReconcileRegistry();
            }
        }

        public bool TryTransferColdCargoToLoadedCargo(
            string moduleInstanceID,
            int coldIndex,
            int thingIDNumber,
            string expectedDefName,
            int count,
            string reason,
            out int movedCount,
            out string failureReason)
        {
            movedCount = 0;
            failureReason = null;
            if (this.HasActiveGlobalUnload())
            {
                failureReason = "CT_Shuttle_Cargo_ManualMoveBlockedByUnloading".Translate().ToString();
                return false;
            }

            this.ReconcileRegistry();

            try
            {
                RefrigeratedCargoRecord record;
                ShuttleModule module;
                ShuttleRefrigeratedCargoModuleDef moduleDef;
                if (!this.TryResolveColdRecord(
                    moduleInstanceID,
                    out record,
                    out module,
                    out moduleDef,
                    out failureReason))
                {
                    return false;
                }

                if (!this.preflightService.TryValidateTransferOutOfCold(
                    module,
                    moduleDef,
                    this.host,
                    this.cargoBackend,
                    this.profile,
                    out failureReason))
                {
                    return false;
                }

                Thing coldThing;
                if (!this.queryService.TryResolveColdThing(
                    record,
                    coldIndex,
                    thingIDNumber,
                    expectedDefName,
                    out coldThing,
                    out failureReason))
                {
                    return false;
                }

                int moveCount;
                if (!this.preflightService.TryValidateMoveOutOfCold(coldThing, moduleDef, count, out moveCount, out failureReason))
                {
                    return false;
                }

                float moveMassKg = CargoDisplayUtility.GetThingMass(coldThing, moveCount);
                CompTransporter destinationTransporter;
                ThingOwner destinationContents;
                if (!this.TryResolveDepositDestination(moveMassKg, out destinationTransporter, out destinationContents, out failureReason))
                {
                    return false;
                }

                Thing taken = record.Contents.Take(coldThing, moveCount);
                if (taken == null)
                {
                    failureReason = "Failed to take refrigerated cargo for normal cargo transfer.";
                    return false;
                }

                int takenCount = taken.stackCount;
                if (!destinationContents.TryAdd(taken, false))
                {
                    string rollbackFailureReason;
                    ShuttleTransferRecoveryStatus rollbackStatus;
                    if (!this.TryRollbackToColdCargo(record.Contents, taken, out rollbackStatus, out rollbackFailureReason))
                    {
                        movedCount = 0;
                        failureReason = "Normal cargo holder rejected the transferred item, and rollback to refrigerated cargo failed: " +
                            rollbackFailureReason;
                        return false;
                    }

                    movedCount = 0;
                    failureReason = "Normal cargo holder rejected the transferred item. rollbackStatus=" +
                        rollbackStatus;
                    return false;
                }

                if (destinationTransporter != null)
                {
                    destinationTransporter.Notify_ThingAdded(taken);
                }

                movedCount = takenCount;
                return true;
            }
            finally
            {
                this.ReconcileRegistry();
            }
        }

        public bool TryUnloadColdCargoBay(
            string moduleInstanceID,
            IReadOnlyList<ShuttleColdCargoUnloadTarget> targets,
            string reason,
            out int unloadedStackCount,
            out int unloadedThingCount,
            out string failureReason)
        {
            unloadedStackCount = 0;
            unloadedThingCount = 0;
            failureReason = null;
            if (this.HasActiveGlobalUnload())
            {
                failureReason = "CT_Shuttle_Cargo_ManualMoveBlockedByUnloading".Translate().ToString();
                return false;
            }

            this.ReconcileRegistry();

            try
            {
                if (targets == null || targets.Count == 0)
                {
                    failureReason = "CT_Shuttle_Cargo_BayUnloadNoContents".Translate().ToString();
                    return false;
                }

                if (this.HasActiveRefrigeratedLaunchTransfer())
                {
                    failureReason = "Refrigerated cargo launch transfer is active.";
                    return false;
                }

                RefrigeratedCargoRecord record;
                ShuttleModule module;
                ShuttleRefrigeratedCargoModuleDef moduleDef;
                if (!this.TryResolveColdRecord(
                    moduleInstanceID,
                    out record,
                    out module,
                    out moduleDef,
                    out failureReason))
                {
                    return false;
                }

                if (this.host == null || this.host.Map == null)
                {
                    failureReason = "CT_Shuttle_Command_ShuttleMapUnavailable".Translate().ToString();
                    return false;
                }

                List<ResolvedColdCargoUnloadTarget> resolvedTargets =
                    new List<ResolvedColdCargoUnloadTarget>();
                HashSet<int> seenThingIDs = new HashSet<int>();
                for (int i = 0; i < targets.Count; i++)
                {
                    ShuttleColdCargoUnloadTarget target = targets[i];
                    if (target == null ||
                        target.ThingIDNumber <= 0 ||
                        string.IsNullOrEmpty(target.ExpectedDefName))
                    {
                        failureReason = "Refrigerated cargo entry is no longer available.";
                        return false;
                    }

                    if (seenThingIDs.Contains(target.ThingIDNumber))
                    {
                        continue;
                    }

                    seenThingIDs.Add(target.ThingIDNumber);

                    Thing coldThing;
                    if (!this.queryService.TryResolveColdThing(
                        record,
                        target.ColdIndex,
                        target.ThingIDNumber,
                        target.ExpectedDefName,
                        out coldThing,
                        out failureReason))
                    {
                        return false;
                    }

                    int moveCount;
                    if (!this.preflightService.TryValidateMoveOutOfCold(
                        coldThing,
                        moduleDef,
                        target.Count,
                        out moveCount,
                        out failureReason))
                    {
                        return false;
                    }

                    resolvedTargets.Add(new ResolvedColdCargoUnloadTarget(
                        coldThing,
                        moveCount));
                }

                if (resolvedTargets.Count == 0)
                {
                    failureReason = "CT_Shuttle_Cargo_BayUnloadNoContents".Translate().ToString();
                    return false;
                }

                for (int i = 0; i < resolvedTargets.Count; i++)
                {
                    ResolvedColdCargoUnloadTarget target = resolvedTargets[i];
                    Thing resultingThing;
                    if (!record.Contents.TryDrop(
                        target.Thing,
                        this.host.Position,
                        this.host.Map,
                        ThingPlaceMode.Near,
                        target.Count,
                        out resultingThing,
                        null,
                        null))
                    {
                        failureReason = "CT_Shuttle_Cargo_UnloadFailed".Translate().ToString();
                        return false;
                    }

                    unloadedStackCount++;
                    unloadedThingCount += target.Count;
                }

                return true;
            }
            finally
            {
                this.ReconcileRegistry();
            }
        }

        public bool TryUnloadColdCargoEntry(
            string moduleInstanceID,
            int coldIndex,
            int thingIDNumber,
            string expectedDefName,
            int count,
            string reason,
            out int unloadedCount,
            out string failureReason)
        {
            unloadedCount = 0;
            failureReason = null;
            this.ReconcileRegistry();

            try
            {
                if (this.HasActiveRefrigeratedLaunchTransfer())
                {
                    failureReason = "Refrigerated cargo launch transfer is active.";
                    return false;
                }

                RefrigeratedCargoRecord record;
                ShuttleModule module;
                ShuttleRefrigeratedCargoModuleDef moduleDef;
                if (!this.TryResolveColdRecord(
                    moduleInstanceID,
                    out record,
                    out module,
                    out moduleDef,
                    out failureReason))
                {
                    return false;
                }

                if (this.host == null || this.host.Map == null)
                {
                    failureReason = "CT_Shuttle_Command_ShuttleMapUnavailable".Translate().ToString();
                    return false;
                }

                Thing coldThing;
                if (!this.queryService.TryResolveColdThing(
                    record,
                    coldIndex,
                    thingIDNumber,
                    expectedDefName,
                    out coldThing,
                    out failureReason))
                {
                    return false;
                }

                int moveCount;
                if (!this.preflightService.TryValidateMoveOutOfCold(
                    coldThing,
                    moduleDef,
                    count,
                    out moveCount,
                    out failureReason))
                {
                    return false;
                }

                Thing resultingThing;
                if (!record.Contents.TryDrop(
                    coldThing,
                    this.host.Position,
                    this.host.Map,
                    ThingPlaceMode.Near,
                    moveCount,
                    out resultingThing,
                    null,
                    null))
                {
                    failureReason = "CT_Shuttle_Cargo_UnloadFailed".Translate().ToString();
                    return false;
                }

                unloadedCount = moveCount;
                return true;
            }
            finally
            {
                this.ReconcileRegistry();
            }
        }

        public bool TryAutoTransferLoadedCargoToCold(
            string moduleInstanceID,
            int maxStacks,
            ThingFilter effectiveAutoTransferFilter,
            bool hasCustomAutoTransferFilter,
            string reason,
            out int movedStackCount,
            out int movedThingCount,
            out string failureReason)
        {
            movedStackCount = 0;
            movedThingCount = 0;
            failureReason = null;

            if (this.runtimeState != null &&
                this.runtimeState.CargoUnload != null &&
                this.runtimeState.CargoUnload.IsActive)
            {
                failureReason = "CT_Shuttle_Cargo_AutoTransferBlockedByUnloading".Translate().ToString();
                return false;
            }

            if (maxStacks <= 0)
            {
                return true;
            }

            this.ReconcileRegistry();

            try
            {
                if (this.HasActiveRefrigeratedLaunchTransfer())
                {
                    failureReason = "Refrigerated cargo launch transfer is active.";
                    return false;
                }

                RefrigeratedCargoRecord record;
                ShuttleModule module;
                ShuttleRefrigeratedCargoModuleDef moduleDef;
                if (!this.TryResolveColdRecord(
                    moduleInstanceID,
                    out record,
                    out module,
                    out moduleDef,
                    out failureReason))
                {
                    return false;
                }

                if (moduleDef.autoTransferOnlyWhenCoolingActive &&
                    !record.CoolingActive)
                {
                    // Event-driven reconciliation and the legacy runtime caller share this
                    // service. Treat inactive cooling as a normal no-op, not a transfer error.
                    failureReason = null;
                    return true;
                }

                if (!this.preflightService.TryValidateTransferIntoCold(
                    module,
                    moduleDef,
                    record,
                    this.host,
                    this.cargoBackend,
                    this.profile,
                    out failureReason))
                {
                    return false;
                }

                ColdAutoTransferCandidateSnapshot snapshot = this.GetAutoTransferCandidateSnapshot();
                for (int i = 0; i < maxStacks; i++)
                {
                    ColdAutoTransferCandidate candidate;
                    int moveCount;
                    if (!this.autoTransferPlanner.TryFindAutoTransferCandidate(
                        snapshot,
                        moduleDef,
                        record,
                        effectiveAutoTransferFilter,
                        hasCustomAutoTransferFilter,
                        out candidate,
                        out moveCount))
                    {
                        failureReason = null;
                        return true;
                    }

                    int movedCount;
                    string moveFailureReason;
                    ColdAutoTransferMoveStatus moveStatus = this.TryMoveAutoTransferCandidateToCold(
                        candidate,
                        record,
                        moduleDef,
                        effectiveAutoTransferFilter,
                        hasCustomAutoTransferFilter,
                        moveCount,
                        string.IsNullOrEmpty(reason) ? "auto-transfer" : reason,
                        out movedCount,
                        out moveFailureReason);
                    if (moveStatus == ColdAutoTransferMoveStatus.Moved)
                    {
                        candidate.Consumed = true;
                        movedStackCount++;
                        movedThingCount += movedCount;
                        continue;
                    }

                    if (moveStatus == ColdAutoTransferMoveStatus.CandidateInvalid)
                    {
                        candidate.Consumed = true;
                        failureReason = null;
                        return true;
                    }

                    if (moveStatus == ColdAutoTransferMoveStatus.CapacityUnavailable)
                    {
                        failureReason = null;
                        return true;
                    }

                    this.LogAutoTransferMoveFailure(
                        moduleInstanceID,
                        moveStatus,
                        moveFailureReason);
                    candidate.Consumed = true;
                    failureReason = moveFailureReason;
                    if (string.IsNullOrEmpty(failureReason))
                    {
                        failureReason = "Refrigerated cargo auto-transfer failed with status=" +
                            moveStatus +
                            ".";
                    }

                    return false;
                }

                return true;
            }
            finally
            {
                this.ReconcileRegistry();
            }
        }

        private bool HasActiveGlobalUnload()
        {
            return this.runtimeState != null &&
                this.runtimeState.CargoUnload != null &&
                this.runtimeState.CargoUnload.IsActive;
        }

        private bool TryResolveColdRecord(
            string moduleInstanceID,
            out RefrigeratedCargoRecord record,
            out ShuttleModule module,
            out ShuttleRefrigeratedCargoModuleDef moduleDef,
            out string failureReason)
        {
            record = null;
            module = null;
            moduleDef = null;
            failureReason = null;

            if (string.IsNullOrEmpty(moduleInstanceID))
            {
                failureReason = "No refrigerated module instance was selected.";
                return false;
            }

            if (this.host == null)
            {
                failureReason = "Shuttle host is not available.";
                return false;
            }

            module = this.assemblyState != null
                ? this.assemblyState.GetModule(moduleInstanceID)
                : null;
            moduleDef = module != null ? module.ModuleDef as ShuttleRefrigeratedCargoModuleDef : null;
            if (module == null || moduleDef == null)
            {
                failureReason = "Refrigerated cargo module is no longer installed.";
                return false;
            }

            if (!module.IsEnabled)
            {
                failureReason = "Refrigerated cargo module is disabled.";
                return false;
            }

            CompShuttleRefrigeratedCargoRegistry registry = this.GetRegistry();
            if (registry == null)
            {
                failureReason = "Refrigerated cargo registry is not available.";
                return false;
            }

            registry.Reconcile(this.assemblyState, this.runtimeState);
            if (!registry.TryGetRecord(moduleInstanceID, out record) || record == null)
            {
                failureReason = "Refrigerated cargo holder is not available.";
                return false;
            }

            return true;
        }

        private ColdAutoTransferCandidateSnapshot GetAutoTransferCandidateSnapshot()
        {
            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : -1;
            if (this.cachedAutoTransferCandidateSnapshot != null &&
                this.cachedAutoTransferCandidateSnapshot.CreatedTick == ticksGame)
            {
                return this.cachedAutoTransferCandidateSnapshot;
            }

            this.cachedAutoTransferCandidateSnapshot =
                this.autoTransferPlanner.BuildAutoTransferCandidateSnapshot(ticksGame);
            return this.cachedAutoTransferCandidateSnapshot;
        }

        private ShuttleRefrigeratedCargoAutoTransferConfig ResolveAutoTransferConfig(
            string moduleInstanceID,
            ShuttleRefrigeratedCargoModuleDef moduleDef)
        {
            if (this.assemblyState == null ||
                this.assemblyState.RefrigeratedCargoConfig == null)
            {
                return ShuttleRefrigeratedCargoAutoTransferConfig.FromModuleDef(
                    moduleInstanceID,
                    moduleDef);
            }

            return this.assemblyState.RefrigeratedCargoConfig.BuildEffectiveAutoTransferConfig(
                moduleInstanceID,
                moduleDef);
        }

        private ColdAutoTransferMoveStatus TryMoveAutoTransferCandidateToCold(
            ColdAutoTransferCandidate candidate,
            RefrigeratedCargoRecord record,
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            ThingFilter effectiveAutoTransferFilter,
            bool hasCustomAutoTransferFilter,
            int count,
            string reason,
            out int movedCount,
            out string failureReason)
        {
            movedCount = 0;
            failureReason = null;
            if (!this.autoTransferPlanner.IsAutoTransferCandidateStillAvailable(candidate))
            {
                failureReason = "Auto-transfer candidate is no longer in ordinary loaded cargo.";
                return ColdAutoTransferMoveStatus.CandidateInvalid;
            }

            Thing sourceThing = candidate.Thing;
            int moveCount;
            if (!this.preflightService.TryValidateMoveIntoCold(
                sourceThing,
                moduleDef,
                effectiveAutoTransferFilter,
                hasCustomAutoTransferFilter,
                count,
                out moveCount,
                out failureReason))
            {
                return ColdAutoTransferMoveStatus.CandidateInvalid;
            }

            float moveMassKg = CargoDisplayUtility.GetThingMass(sourceThing, moveCount);
            if (!this.preflightService.HasColdCapacity(
                record,
                this.profile,
                this.GetRegistry(),
                moveMassKg,
                out failureReason))
            {
                return ColdAutoTransferMoveStatus.CapacityUnavailable;
            }

            Thing taken = candidate.SourceOwner.Take(sourceThing, moveCount);
            if (taken == null)
            {
                failureReason = "Failed to take loaded cargo for refrigerated auto-transfer.";
                return ColdAutoTransferMoveStatus.CandidateInvalid;
            }

            int takenCount = taken.stackCount;
            if (candidate.SourceTransporter != null)
            {
                candidate.SourceTransporter.Notify_ThingRemoved(taken);
            }

            if (!record.Contents.TryAdd(taken, false))
            {
                string rollbackFailureReason;
                ShuttleTransferRecoveryStatus rollbackStatus;
                if (!this.TryRollbackToLoadedCargo(
                    candidate.SourceOwner,
                    candidate.SourceTransporter,
                    taken,
                    out rollbackStatus,
                    out rollbackFailureReason))
                {
                    movedCount = 0;
                    failureReason = "Refrigerated cargo holder rejected the auto-transferred item, and rollback to normal cargo failed: " +
                        rollbackFailureReason;
                    return ColdAutoTransferMoveStatus.FatalUnresolved;
                }

                movedCount = 0;
                failureReason = "Refrigerated cargo holder rejected the auto-transferred item. rollbackStatus=" +
                    rollbackStatus;
                return rollbackStatus == ShuttleTransferRecoveryStatus.RecoveredToPreferredOwner
                    ? ColdAutoTransferMoveStatus.DestinationRejectedRolledBack
                    : ColdAutoTransferMoveStatus.DestinationRejectedRecovered;
            }

            movedCount = takenCount;
            return ColdAutoTransferMoveStatus.Moved;
        }

        private void LogAutoTransferMoveFailure(
            string moduleInstanceID,
            ColdAutoTransferMoveStatus moveStatus,
            string failureReason)
        {
            if (moveStatus == ColdAutoTransferMoveStatus.FatalUnresolved)
            {
                return;
            }

            if (moveStatus != ColdAutoTransferMoveStatus.DestinationRejectedRolledBack &&
                moveStatus != ColdAutoTransferMoveStatus.DestinationRejectedRecovered)
            {
                return;
            }

            string key = "ColdAutoTransfer:" +
                (this.host != null ? this.host.thingIDNumber : -1) +
                ":" +
                (moduleInstanceID ?? "null") +
                ":" +
                moveStatus;
            if (!ShuttleLogThrottle.Global.ShouldLog(key))
            {
                return;
            }

            Log.Warning("[CeleTech Shuttle] Refrigerated cargo auto-transfer destination rejected candidate. status=" +
                moveStatus +
                " reason=" +
                (failureReason ?? "null"));
        }

        private bool HasWholeCargoCapacityAfterMove(float totalMassDeltaKg, out string failureReason)
        {
            failureReason = null;
            float capacityKg = this.profile != null && this.profile.Cargo != null
                ? this.profile.Cargo.CargoMassCapacityKg
                : 0f;
            if (capacityKg < 0f)
            {
                capacityKg = 0f;
            }

            float totalMassKg = this.queryService.GetNormalCargoMassKg() +
                this.queryService.GetTotalColdCargoMassKg(this.GetRegistry()) +
                totalMassDeltaKg;
            if (totalMassKg <= capacityKg + MassEpsilon)
            {
                return true;
            }

            failureReason = "Total shuttle cargo capacity is insufficient (" +
                totalMassKg.ToString("0.#") + "/" +
                capacityKg.ToString("0.#") + " kg).";
            return false;
        }

        private bool TryResolveDepositDestination(
            float moveMassKg,
            out CompTransporter destinationTransporter,
            out ThingOwner destinationContents,
            out string failureReason)
        {
            destinationTransporter = null;
            destinationContents = null;
            failureReason = null;

            List<CompTransporter> transporters = this.queryService.ResolveTransporters();
            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter == null)
                {
                    continue;
                }

                float availableMassKg = transporter.MassCapacity - transporter.MassUsage;
                if (availableMassKg + MassEpsilon < moveMassKg)
                {
                    continue;
                }

                ThingOwner contents = ShuttleColdTransferQueryService.GetContents(transporter);
                if (contents == null)
                {
                    continue;
                }

                destinationTransporter = transporter;
                destinationContents = contents;
                return true;
            }

            failureReason = "No normal cargo transporter has enough free mass capacity.";
            return false;
        }

        private bool TryRollbackToLoadedCargo(
            ThingOwner sourceContents,
            CompTransporter sourceTransporter,
            Thing thing,
            out ShuttleTransferRecoveryStatus recoveryStatus,
            out string failureReason)
        {
            failureReason = null;
            recoveryStatus = ShuttleTransferRecoveryStatus.None;
            if (sourceContents != null && thing != null && sourceContents.TryAddOrTransfer(thing, false))
            {
                recoveryStatus = ShuttleTransferRecoveryStatus.RecoveredToPreferredOwner;
                if (sourceTransporter != null)
                {
                    sourceTransporter.Notify_ThingAdded(thing);
                }

                return true;
            }

            CompShuttleHolderLaunchTransferState transferState = this.GetTransferState();
            if (transferState != null &&
                transferState.TryRecoverTransferThing(
                    thing,
                    "Refrigerated normal-to-cold rollback to loaded cargo failed.",
                    sourceContents,
                    null,
                    this.host != null ? this.host.Map : null,
                    this.host != null ? this.host.Position : IntVec3.Invalid,
                    out recoveryStatus,
                    out failureReason))
            {
                if (recoveryStatus == ShuttleTransferRecoveryStatus.RecoveredToPreferredOwner &&
                    sourceTransporter != null)
                {
                    sourceTransporter.Notify_ThingAdded(thing);
                }

                failureReason = "normal cargo source rejected rollback for " +
                    this.DescribeThing(thing) +
                    ", but emergency recovery secured it with status=" +
                    recoveryStatus +
                    ".";
                return true;
            }

            failureReason = "normal cargo source rejected rollback for " +
                this.DescribeThing(thing) +
                ". emergencyRecovery=" +
                (failureReason ?? "unavailable");
            Log.Error("[CeleTech Shuttle] Refrigerated cargo rollback failed: " + failureReason);
            recoveryStatus = ShuttleTransferRecoveryStatus.FatalUnresolved;
            return false;
        }

        private bool TryRollbackToColdCargo(
            ThingOwner<Thing> coldContents,
            Thing thing,
            out ShuttleTransferRecoveryStatus recoveryStatus,
            out string failureReason)
        {
            failureReason = null;
            recoveryStatus = ShuttleTransferRecoveryStatus.None;
            if (coldContents != null && thing != null && coldContents.TryAddOrTransfer(thing, false))
            {
                recoveryStatus = ShuttleTransferRecoveryStatus.RecoveredToPreferredOwner;
                return true;
            }

            CompShuttleHolderLaunchTransferState transferState = this.GetTransferState();
            if (transferState != null &&
                transferState.TryRecoverTransferThing(
                    thing,
                    "Refrigerated cold-to-normal rollback to cold cargo failed.",
                    coldContents,
                    null,
                    this.host != null ? this.host.Map : null,
                    this.host != null ? this.host.Position : IntVec3.Invalid,
                    out recoveryStatus,
                    out failureReason))
            {
                failureReason = "refrigerated cargo holder rejected rollback for " +
                    this.DescribeThing(thing) +
                    ", but emergency recovery secured it with status=" +
                    recoveryStatus +
                    ".";
                return true;
            }

            failureReason = "refrigerated cargo holder rejected rollback for " +
                this.DescribeThing(thing) +
                ". emergencyRecovery=" +
                (failureReason ?? "unavailable");
            Log.Error("[CeleTech Shuttle] Refrigerated cargo rollback failed: " + failureReason);
            recoveryStatus = ShuttleTransferRecoveryStatus.FatalUnresolved;
            return false;
        }

        private CompShuttleHolderLaunchTransferState GetTransferState()
        {
            return this.host != null
                ? this.host.TryGetComp<CompShuttleHolderLaunchTransferState>()
                : null;
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

        private float GetNormalCargoCapacityKg()
        {
            float capacityKg = 0f;
            List<CompTransporter> transporters = this.queryService.ResolveTransporters();
            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter != null)
                {
                    capacityKg += transporter.MassCapacity;
                }
            }

            return capacityKg;
        }

        private CompShuttleRefrigeratedCargoRegistry GetRegistry()
        {
            return this.host != null
                ? this.host.TryGetComp<CompShuttleRefrigeratedCargoRegistry>()
                : null;
        }

        private bool HasActiveRefrigeratedLaunchTransfer()
        {
            CompShuttleHolderLaunchTransferState state = this.host != null
                ? this.host.TryGetComp<CompShuttleHolderLaunchTransferState>()
                : null;

            return state != null && state.HasRefrigeratedCargoLaunchTransfer;
        }

        private void ReconcileRegistry()
        {
            CompShuttleRefrigeratedCargoRegistry registry = this.GetRegistry();
            if (registry != null)
            {
                registry.Reconcile(this.assemblyState, this.runtimeState);
            }
        }

        private enum ColdAutoTransferMoveStatus
        {
            Moved,
            CandidateInvalid,
            CapacityUnavailable,
            DestinationRejectedRolledBack,
            DestinationRejectedRecovered,
            FatalUnresolved
        }

        private sealed class ResolvedColdCargoUnloadTarget
        {
            internal ResolvedColdCargoUnloadTarget(Thing thing, int count)
            {
                this.Thing = thing;
                this.Count = count;
            }

            internal readonly Thing Thing;
            internal readonly int Count;
        }
    }
}

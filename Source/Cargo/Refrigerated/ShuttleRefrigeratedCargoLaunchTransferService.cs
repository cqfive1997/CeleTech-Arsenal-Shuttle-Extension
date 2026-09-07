using System;
using System.Collections.Generic;
using System.Text;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated
{
    internal static class ShuttleRefrigeratedCargoLaunchTransferService
    {
        private enum RestoreEntryDisposition
        {
            Resolved,
            RecoverableFailure,
            FatalUnresolved
        }

        internal static bool HasColdCargoForLaunch(
            ThingWithComps shuttleHost,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState)
        {
            CompShuttleRefrigeratedCargoRegistry registry = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleRefrigeratedCargoRegistry>()
                : null;
            if (registry == null)
            {
                return false;
            }

            registry.Reconcile(assemblyState, runtimeState);
            RefrigeratedCargoCoolingStatus status;
            return registry.TryGetAnyColdCargo(out status);
        }

        internal static bool TryGetLaunchReadinessBlocker(
            ThingWithComps shuttleHost,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            out RefrigeratedCargoCoolingStatus status,
            out string failureReason)
        {
            status = null;
            failureReason = null;

            if (shuttleHost == null)
            {
                return false;
            }

            CompShuttleHolderLaunchTransferState transferState = GetTransferState(shuttleHost);
            if (transferState != null && transferState.HasRefrigeratedCargoLaunchTransfer)
            {
                failureReason = "CT_Shuttle_Launch_Failed_RefrigeratedCargoTransferActive"
                    .Translate(
                        transferState.RefrigeratedCargoLaunchTransferStackCount)
                    .ToString();
                return true;
            }

            CompShuttleRefrigeratedCargoRegistry registry =
                shuttleHost.TryGetComp<CompShuttleRefrigeratedCargoRegistry>();
            if (registry == null)
            {
                return false;
            }

            registry.Reconcile(assemblyState, runtimeState);
            IReadOnlyList<RefrigeratedCargoRecord> records = registry.Records;
            for (int i = 0; i < records.Count; i++)
            {
                RefrigeratedCargoRecord record = records[i];
                if (record == null || !record.HasContents)
                {
                    continue;
                }

                ShuttleRefrigeratedCargoModuleDef moduleDef = ResolveModuleDef(record.ModuleDefName);
                registry.TryGetCoolingStatus(record.ModuleInstanceID, out status);
                if (moduleDef == null)
                {
                    failureReason = BuildLaunchBlockFailure(
                        moduleDef,
                        record,
                        "refrigerated module definition is unavailable");
                    return true;
                }

                if (!record.CoolingActive)
                {
                    failureReason = BuildLaunchBlockFailure(
                        moduleDef,
                        record,
                        "cooling is inactive: " + (record.InactiveReason ?? "unknown"));
                    return true;
                }

                if (!IsFinite(record.TargetTemperatureC))
                {
                    failureReason = BuildLaunchBlockFailure(
                        moduleDef,
                        record,
                        "target temperature is invalid");
                    return true;
                }

                if (transferState == null)
                {
                    if (!moduleDef.blockLaunchIfColdCargoCannotBeTransferred)
                    {
                        continue;
                    }

                    failureReason = BuildLaunchBlockFailure(
                        moduleDef,
                        record,
                        "refrigerated launch transfer state is unavailable");
                    return true;
                }
            }

            return false;
        }

        internal static bool TryExportColdCargoForLaunch(
            ThingWithComps shuttleHost,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            out int exportedStackCount,
            out string failureReason)
        {
            exportedStackCount = 0;
            failureReason = null;

            CompShuttleRefrigeratedCargoRegistry registry;
            CompShuttleHolderLaunchTransferState state;
            if (!TryResolveTransferComponents(
                shuttleHost,
                assemblyState,
                runtimeState,
                out registry,
                out state,
                out failureReason))
            {
                return false;
            }

            if (state.HasRefrigeratedCargoLaunchTransfer)
            {
                failureReason = "[CeleTech Shuttle] Refrigerated cargo export refused because a refrigerated launch transfer is already active.";
                Log.Error(failureReason);
                return false;
            }

            IReadOnlyList<RefrigeratedCargoRecord> records = registry.Records;
            int exportTick = Find.TickManager != null ? Find.TickManager.TicksGame : -1;
            for (int recordIndex = 0; recordIndex < records.Count; recordIndex++)
            {
                RefrigeratedCargoRecord record = records[recordIndex];
                if (record == null || !record.HasContents)
                {
                    continue;
                }

                if (!record.CoolingActive)
                {
                    failureReason = "[CeleTech Shuttle] Refrigerated cargo export refused because cold holder " +
                        record.ModuleInstanceID +
                        " contains cargo but is not actively cooling.";
                    exportedStackCount = 0;
                    TryRollbackExport(shuttleHost, assemblyState, runtimeState, out string rollbackNotice);
                    Log.Error(failureReason + " rollback=" + rollbackNotice);
                    return false;
                }

                RefrigeratedLaunchStagingHolder stagingHolder =
                    state.GetOrCreateRefrigeratedLaunchStagingHolder(
                        record.ModuleInstanceID,
                        record.ModuleDefName,
                        record.TargetTemperatureC,
                        record.TargetTemperatureC,
                        record.CoolingActive);

                while (record.Contents.Count > 0)
                {
                    Thing sourceThing = record.Contents[record.Contents.Count - 1];
                    if (sourceThing == null)
                    {
                        record.Contents.RemoveAt(record.Contents.Count - 1);
                        continue;
                    }

                    int originalStackCount = sourceThing.stackCount;
                    float massKg = CargoDisplayUtility.GetThingMass(sourceThing, originalStackCount);
                    RefrigeratedCargoLaunchManifestEntry entry =
                        state.RefrigeratedCargoLaunchManifest.AddExportEntry(
                            record.ModuleInstanceID,
                            record.ModuleDefName,
                            sourceThing,
                            originalStackCount,
                            massKg,
                            record.TargetTemperatureC,
                            stagingHolder.TargetTemperatureC,
                            exportTick,
                            RefrigeratedCargoLaunchManifestConstants.PhaseExported,
                            "Exported from refrigerated cold holder to refrigerated launch staging.");

                    Thing taken = record.Contents.Take(sourceThing, originalStackCount);
                    if (taken == null)
                    {
                        entry.TransferPhase = RefrigeratedCargoLaunchManifestConstants.PhaseFailed;
                        entry.DebugNotes = "Failed to take thing from refrigerated cargo record.";
                        failureReason = "[CeleTech Shuttle] Refrigerated cargo export failed while taking source thing. " +
                            entry.DumpForDebug();
                        exportedStackCount = 0;
                        TryRollbackExport(shuttleHost, assemblyState, runtimeState, out string rollbackNotice);
                        Log.Error(failureReason + " rollback=" + rollbackNotice);
                        return false;
                    }

                    if (!stagingHolder.Contents.TryAdd(taken, false))
                    {
                        entry.TransferPhase = RefrigeratedCargoLaunchManifestConstants.PhaseFailed;
                        entry.DebugNotes = "Refrigerated launch staging holder rejected exported thing.";
                        if (!record.Contents.TryAdd(taken, false))
                        {
                            ShuttleTransferRecoveryStatus recoveryStatus;
                            string recoveryFailureReason;
                            if (state.TryRecoverTransferThing(
                                taken,
                                "Refrigerated export staging rejection rollback-to-source failed. " +
                                    entry.DumpForDebug(),
                                record.Contents,
                                stagingHolder.Contents,
                                shuttleHost != null ? shuttleHost.Map : null,
                                shuttleHost != null ? shuttleHost.Position : IntVec3.Invalid,
                                out recoveryStatus,
                                out recoveryFailureReason))
                            {
                                entry.DebugNotes = "Refrigerated launch staging holder rejected exported thing; source rollback failed but emergency recovery succeeded. status=" +
                                    recoveryStatus;
                            }
                            else
                            {
                                entry.DebugNotes = "Refrigerated launch staging holder rejected exported thing; source rollback and emergency recovery failed: " +
                                    (recoveryFailureReason ?? "null");
                                failureReason = "[CeleTech Shuttle] Refrigerated cargo export failed and emergency recovery could not secure the taken thing. " +
                                    entry.DumpForDebug() +
                                    " recovery=" +
                                    (recoveryFailureReason ?? "null");
                                exportedStackCount = 0;
                                Log.Error(failureReason);
                                return false;
                            }
                        }

                        failureReason = "[CeleTech Shuttle] Refrigerated cargo export failed while adding to launch staging. " +
                            entry.DumpForDebug();
                        exportedStackCount = 0;
                        TryRollbackExport(shuttleHost, assemblyState, runtimeState, out string rollbackNotice);
                        Log.Error(failureReason + " rollback=" + rollbackNotice);
                        return false;
                    }

                    entry.TransferPhase = RefrigeratedCargoLaunchManifestConstants.PhaseInFlight;
                    exportedStackCount++;
                }
            }

            return true;
        }

        internal static bool TryRollbackExport(
            ThingWithComps shuttleHost,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            out string failureReason)
        {
            failureReason = null;

            CompShuttleHolderLaunchTransferState existingState = GetTransferState(shuttleHost);
            if (existingState == null || !existingState.HasRefrigeratedCargoLaunchTransfer)
            {
                failureReason = "No refrigerated cargo launch transfer is active.";
                return true;
            }

            CompShuttleRefrigeratedCargoRegistry registry;
            CompShuttleHolderLaunchTransferState state;
            if (!TryResolveTransferComponents(
                shuttleHost,
                assemblyState,
                runtimeState,
                out registry,
                out state,
                out failureReason))
            {
                return false;
            }

            bool allRolledBack = true;
            List<RefrigeratedCargoLaunchManifestEntry> entries =
                state.RefrigeratedCargoLaunchManifest.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                RefrigeratedCargoLaunchManifestEntry entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                if (entry.TransferPhase == RefrigeratedCargoLaunchManifestConstants.PhaseRolledBack ||
                    entry.TransferPhase == RefrigeratedCargoLaunchManifestConstants.PhaseRestored)
                {
                    continue;
                }

                string restoreFailure;
                if (TryRestoreEntryToColdHolder(
                    registry,
                    state,
                    entry,
                    RefrigeratedCargoLaunchManifestConstants.PhaseRolledBack,
                    "Rolled back refrigerated launch export.",
                    out restoreFailure) != RestoreEntryDisposition.Resolved)
                {
                    allRolledBack = false;
                }
            }

            if (!TryLogUnmatchedStagingContents(state, "rollback"))
            {
                allRolledBack = false;
            }

            if (allRolledBack)
            {
                state.ClearRefrigeratedCargoLaunchTransferIfEmpty();
                return true;
            }

            failureReason = "[CeleTech Shuttle] Refrigerated cargo rollback did not restore every exported thing. " +
                state.RefrigeratedCargoLaunchManifest.DumpForDebug();
            Log.Error(failureReason);
            return false;
        }

        internal static RefrigeratedCargoRestoreResult TryRestoreBeforeImpact(
            ThingWithComps shuttleHost,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            ThingOwner normalCargoFallbackOwner,
            ThingOwner ordinaryImpactFallbackOwner)
        {
            try
            {
                RefrigeratedCargoRestoreResult result = TryRestoreBeforeImpactCore(
                    shuttleHost,
                    assemblyState,
                    runtimeState,
                    normalCargoFallbackOwner,
                    ordinaryImpactFallbackOwner);
                if (result != null)
                {
                    return result;
                }

                CompShuttleHolderLaunchTransferState state = TryGetTransferStateForRestoreDiagnostics(shuttleHost);
                return BuildFatalRestoreResult(
                    shuttleHost,
                    state,
                    "ShuttleRefrigeratedCargoLaunchTransferService.TryRestoreBeforeImpactCore returned null");
            }
            catch (Exception exception)
            {
                CompShuttleHolderLaunchTransferState state = TryGetTransferStateForRestoreDiagnostics(shuttleHost);
                return BuildFatalRestoreResult(
                    shuttleHost,
                    state,
                    "ShuttleRefrigeratedCargoLaunchTransferService.TryRestoreBeforeImpact threw: " + exception);
            }
        }

        private static RefrigeratedCargoRestoreResult TryRestoreBeforeImpactCore(
            ThingWithComps shuttleHost,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            ThingOwner normalCargoFallbackOwner,
            ThingOwner ordinaryImpactFallbackOwner)
        {
            // This service must be called before the incoming skyfaller's base.Impact
            // handles ordinary cargo. It does not use ordinary vanilla transporter
            // storage as refrigerated flight-time ownership.
            CompShuttleHolderLaunchTransferState existingState = GetTransferState(shuttleHost);
            if (existingState == null || !existingState.HasRefrigeratedCargoLaunchTransfer)
            {
                return new RefrigeratedCargoRestoreResult(
                    RefrigeratedCargoRestoreStatus.AllResolved,
                    null,
                    null,
                    false,
                    false);
            }

            CompShuttleHolderLaunchTransferState state = existingState;
            CompShuttleRefrigeratedCargoRegistry registry = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleRefrigeratedCargoRegistry>()
                : null;
            if (registry != null)
            {
                registry.Reconcile(assemblyState, runtimeState);
            }

            bool anyFallback = false;
            bool anyQuarantined = false;
            bool fatalUnresolved = false;
            string failureNotes = null;
            List<RefrigeratedCargoLaunchManifestEntry> entries =
                state.RefrigeratedCargoLaunchManifest.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                RefrigeratedCargoLaunchManifestEntry entry = entries[i];
                if (entry == null)
                {
                    fatalUnresolved = true;
                    failureNotes = AppendNote(
                        failureNotes,
                        BuildRestoreDiagnostic(
                            shuttleHost,
                            state,
                            null,
                            "manifest entry at index " + i + " is null"));
                    continue;
                }

                if (entry.TransferPhase == RefrigeratedCargoLaunchManifestConstants.PhaseRestored ||
                    entry.TransferPhase == RefrigeratedCargoLaunchManifestConstants.PhaseRolledBack)
                {
                    continue;
                }

                string coldRestoreFailure = null;
                RestoreEntryDisposition coldRestoreDisposition = registry != null
                    ? TryRestoreEntryToColdHolder(
                        registry,
                        state,
                        entry,
                        RefrigeratedCargoLaunchManifestConstants.PhaseRestored,
                        "Restored to the matching refrigerated holder before ordinary impact cargo handling.",
                        out coldRestoreFailure)
                    : RestoreEntryDisposition.RecoverableFailure;
                if (registry == null)
                {
                    coldRestoreFailure = "refrigerated cargo registry is unavailable";
                }

                if (coldRestoreDisposition == RestoreEntryDisposition.Resolved)
                {
                    if (entry.TransferPhase == RefrigeratedCargoLaunchManifestConstants.PhaseQuarantined)
                    {
                        anyQuarantined = true;
                        failureNotes = AppendNote(
                            failureNotes,
                            BuildRestoreDiagnostic(
                                shuttleHost,
                                state,
                                entry,
                                coldRestoreFailure ?? "entry retained in refrigerated recovery quarantine"));
                    }

                    continue;
                }

                if (coldRestoreDisposition == RestoreEntryDisposition.FatalUnresolved)
                {
                    fatalUnresolved = true;
                    failureNotes = AppendNote(
                        failureNotes,
                        BuildRestoreDiagnostic(shuttleHost, state, entry, coldRestoreFailure));
                    continue;
                }

                string normalFallbackFailure;
                RestoreEntryDisposition normalFallbackDisposition = TryMoveStagedThingToFallbackOwner(
                    state,
                    entry,
                    normalCargoFallbackOwner,
                    RefrigeratedCargoLaunchManifestConstants.PhaseNormalCargoFallback,
                    "Fallback restored refrigerated cargo into normal cargo owner.",
                    out normalFallbackFailure);
                if (normalFallbackDisposition == RestoreEntryDisposition.Resolved)
                {
                    anyFallback = true;
                    continue;
                }

                if (normalFallbackDisposition == RestoreEntryDisposition.FatalUnresolved)
                {
                    fatalUnresolved = true;
                    failureNotes = AppendNote(
                        failureNotes,
                        BuildRestoreDiagnostic(shuttleHost, state, entry, normalFallbackFailure));
                    continue;
                }

                string impactFallbackFailure;
                RestoreEntryDisposition impactFallbackDisposition = TryMoveStagedThingToFallbackOwner(
                    state,
                    entry,
                    ordinaryImpactFallbackOwner,
                    RefrigeratedCargoLaunchManifestConstants.PhaseImpactFallback,
                    "Fallback left refrigerated cargo for ordinary impact handling.",
                    out impactFallbackFailure);
                if (impactFallbackDisposition == RestoreEntryDisposition.Resolved)
                {
                    anyFallback = true;
                    continue;
                }

                if (impactFallbackDisposition == RestoreEntryDisposition.FatalUnresolved)
                {
                    fatalUnresolved = true;
                    failureNotes = AppendNote(
                        failureNotes,
                        BuildRestoreDiagnostic(shuttleHost, state, entry, impactFallbackFailure));
                    continue;
                }

                string quarantineFailure;
                string priorRestoreFailure = AppendNote(
                    AppendNote(coldRestoreFailure, normalFallbackFailure),
                    impactFallbackFailure);
                RestoreEntryDisposition quarantineDisposition = TryQuarantineEntryInStagingForRecovery(
                    state,
                    entry,
                    priorRestoreFailure,
                    out quarantineFailure);
                if (quarantineDisposition == RestoreEntryDisposition.Resolved)
                {
                    anyQuarantined = true;
                    failureNotes = AppendNote(
                        failureNotes,
                        BuildRestoreDiagnostic(
                            shuttleHost,
                            state,
                            entry,
                            "entry quarantined in refrigerated staging after restore failures: " +
                                (priorRestoreFailure ?? "none")));
                    continue;
                }

                fatalUnresolved = true;
                failureNotes = AppendNote(
                    failureNotes,
                    BuildRestoreDiagnostic(shuttleHost, state, entry, quarantineFailure));
            }

            string unmatchedFailure;
            if (!TryBuildUnmatchedStagingContentsFailure(
                shuttleHost,
                state,
                "arrival-restore",
                out unmatchedFailure))
            {
                fatalUnresolved = true;
                failureNotes = AppendNote(failureNotes, unmatchedFailure);
            }

            string debugDump = state.DumpRefrigeratedCargoLaunchTransferForDebug();
            if (fatalUnresolved)
            {
                string failureReason = "[CeleTech Shuttle] Refrigerated cargo arrival restore has fatal unresolved cargo. " +
                    (failureNotes ?? "No detailed failure note.");
                return new RefrigeratedCargoRestoreResult(
                    RefrigeratedCargoRestoreStatus.FatalUnresolved,
                    failureReason,
                    debugDump,
                    anyFallback,
                    anyQuarantined);
            }

            if (anyQuarantined)
            {
                string quarantineReason = "[CeleTech Shuttle] Refrigerated cargo arrival restore quarantined one or more entries in refrigerated launch staging for recovery. " +
                    (failureNotes ?? "No unresolved ownerless cargo remains.");
                return new RefrigeratedCargoRestoreResult(
                    RefrigeratedCargoRestoreStatus.Quarantined,
                    quarantineReason,
                    debugDump,
                    anyFallback,
                    true);
            }

            if (anyFallback)
            {
                failureNotes = "[CeleTech Shuttle] Refrigerated cargo arrival restore used explicit ordinary cargo/impact fallback for one or more entries. " +
                    (failureNotes ?? "All fallback entries were moved to a saveable owner before base impact.");
            }

            state.ClearRefrigeratedCargoLaunchTransferIfEmpty();
            return new RefrigeratedCargoRestoreResult(
                RefrigeratedCargoRestoreStatus.AllResolved,
                failureNotes,
                debugDump,
                anyFallback,
                false);
        }

        private static bool TryResolveTransferComponents(
            ThingWithComps shuttleHost,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            out CompShuttleRefrigeratedCargoRegistry registry,
            out CompShuttleHolderLaunchTransferState state,
            out string failureReason)
        {
            registry = null;
            state = null;
            failureReason = null;

            if (shuttleHost == null)
            {
                failureReason = "[CeleTech Shuttle] Refrigerated cargo launch transfer requires a shuttle host.";
                return false;
            }

            registry = shuttleHost.TryGetComp<CompShuttleRefrigeratedCargoRegistry>();
            if (registry == null)
            {
                failureReason = "[CeleTech Shuttle] Refrigerated cargo registry is not available for launch transfer.";
                return false;
            }

            state = shuttleHost.TryGetComp<CompShuttleHolderLaunchTransferState>();
            if (state == null)
            {
                failureReason = "[CeleTech Shuttle] Holder launch transfer state is not available for refrigerated cargo.";
                return false;
            }

            registry.Reconcile(assemblyState, runtimeState);
            return true;
        }

        private static RestoreEntryDisposition TryRestoreEntryToColdHolder(
            CompShuttleRefrigeratedCargoRegistry registry,
            CompShuttleHolderLaunchTransferState state,
            RefrigeratedCargoLaunchManifestEntry entry,
            string successPhase,
            string successNotes,
            out string failureReason)
        {
            failureReason = null;
            if (registry == null || state == null || entry == null)
            {
                failureReason = "registry, transfer state, or manifest entry is unavailable";
                return RestoreEntryDisposition.RecoverableFailure;
            }

            RefrigeratedCargoRecord record;
            if (!registry.TryGetRecord(entry.ModuleInstanceID, out record) || record == null)
            {
                entry.TransferPhase = RefrigeratedCargoLaunchManifestConstants.PhaseFailed;
                entry.DebugNotes = "Matching refrigerated cargo record is unavailable.";
                failureReason = "matching refrigerated cargo record is unavailable";
                return RestoreEntryDisposition.RecoverableFailure;
            }

            Thing alreadyInColdHolder = FindThingInOwner(record.Contents, entry.ThingIDNumber);
            if (alreadyInColdHolder != null)
            {
                entry.TransferPhase = successPhase;
                entry.DebugNotes = "Thing was already present in matching cold holder.";
                return RestoreEntryDisposition.Resolved;
            }

            ThingOwner<Thing> sourceOwner;
            Thing thing;
            if (!TryResolveRefrigeratedRecoverySource(
                state,
                entry,
                out sourceOwner,
                out thing))
            {
                entry.TransferPhase = RefrigeratedCargoLaunchManifestConstants.PhaseFailed;
                entry.DebugNotes = "Recovery thing was not found.";
                failureReason = "manifest thing was not found in refrigerated staging, emergency recovery, or the matching cold holder";
                return RestoreEntryDisposition.RecoverableFailure;
            }

            string mismatchReason;
            if (!TryValidateStagedThingMatchesManifest(thing, entry, out mismatchReason))
            {
                entry.TransferPhase = RefrigeratedCargoLaunchManifestConstants.PhaseFailed;
                entry.DebugNotes = "Staged thing did not match refrigerated launch manifest: " + mismatchReason;
                failureReason = "staged thing did not match refrigerated launch manifest: " + mismatchReason;
                return RestoreEntryDisposition.RecoverableFailure;
            }

            Thing taken = sourceOwner.Take(thing, thing.stackCount);
            if (taken == null)
            {
                entry.TransferPhase = RefrigeratedCargoLaunchManifestConstants.PhaseFailed;
                entry.DebugNotes = "Failed to take thing from refrigerated recovery owner.";
                failureReason = "failed to take thing from refrigerated recovery owner";
                return RestoreEntryDisposition.RecoverableFailure;
            }

            if (record.Contents.TryAdd(taken, false))
            {
                entry.TransferPhase = successPhase;
                entry.DebugNotes = successNotes;
                state.RemoveEmptyRefrigeratedLaunchStagingHolders();
                return RestoreEntryDisposition.Resolved;
            }

            if (sourceOwner.TryAdd(taken, false))
            {
                entry.TransferPhase = RefrigeratedCargoLaunchManifestConstants.PhaseFailed;
                entry.DebugNotes = "Cold holder rejected the recovery thing; it was returned to its saved recovery owner.";
                failureReason = "cold holder rejected the recovery thing; it remains in its saved recovery owner";
                return RestoreEntryDisposition.RecoverableFailure;
            }

            ShuttleTransferRecoveryStatus recoveryStatus;
            string recoveryFailureReason;
            if (state.TryRecoverTransferThing(
                taken,
                "Cold holder rejected staged refrigerated cargo and rollback-to-staging failed. " +
                    entry.DumpForDebug(),
                record.Contents,
                sourceOwner,
                state.parent != null ? state.parent.Map : null,
                state.parent != null ? state.parent.Position : IntVec3.Invalid,
                out recoveryStatus,
                out recoveryFailureReason))
            {
                if (recoveryStatus == ShuttleTransferRecoveryStatus.RecoveredToPreferredOwner)
                {
                    entry.TransferPhase = successPhase;
                    entry.DebugNotes = successNotes + " Emergency recovery retried the cold holder successfully.";
                    return RestoreEntryDisposition.Resolved;
                }

                entry.TransferPhase = RefrigeratedCargoLaunchManifestConstants.PhaseQuarantined;
                entry.DebugNotes = "Cold holder rejected staged thing; rollback-to-staging failed, but emergency recovery secured it. status=" +
                    recoveryStatus;
                failureReason = "cold holder rejected staged thing; emergency recovery secured it with status=" +
                    recoveryStatus;
                return RestoreEntryDisposition.RecoverableFailure;
            }

            entry.TransferPhase = RefrigeratedCargoLaunchManifestConstants.PhaseFailed;
            entry.DebugNotes = "Cold holder rejected staged thing; rollback-to-staging and emergency recovery failed.";
            failureReason = "cold holder rejected staged thing; rollback-to-staging and emergency recovery failed. thingOwner=" +
                DescribeThingOwner(taken) +
                " recovery=" +
                (recoveryFailureReason ?? "null");
            return RestoreEntryDisposition.FatalUnresolved;
        }

        private static RestoreEntryDisposition TryMoveStagedThingToFallbackOwner(
            CompShuttleHolderLaunchTransferState state,
            RefrigeratedCargoLaunchManifestEntry entry,
            ThingOwner fallbackOwner,
            string successPhase,
            string successNotes,
            out string failureReason)
        {
            failureReason = null;
            if (state == null || entry == null || fallbackOwner == null)
            {
                failureReason = "fallback owner, transfer state, or manifest entry is unavailable";
                return RestoreEntryDisposition.RecoverableFailure;
            }

            Thing alreadyInFallback = FindThingInOwner(fallbackOwner, entry.ThingIDNumber);
            if (alreadyInFallback != null)
            {
                string previousNotes = entry.DebugNotes;
                entry.TransferPhase = successPhase;
                entry.DebugNotes = string.IsNullOrEmpty(previousNotes)
                    ? successNotes + " Thing was already present in fallback owner."
                    : successNotes + " Thing was already present in fallback owner. Previous restore note: " + previousNotes;
                return RestoreEntryDisposition.Resolved;
            }

            ThingOwner<Thing> sourceOwner;
            Thing thing;
            if (!TryResolveRefrigeratedRecoverySource(
                state,
                entry,
                out sourceOwner,
                out thing))
            {
                failureReason = "manifest thing was not found in refrigerated staging or emergency recovery for fallback";
                return RestoreEntryDisposition.RecoverableFailure;
            }

            Thing taken = sourceOwner.Take(thing, thing.stackCount);
            if (taken == null)
            {
                failureReason = "failed to take thing from refrigerated recovery owner for fallback";
                return RestoreEntryDisposition.RecoverableFailure;
            }

            if (fallbackOwner.TryAdd(taken, false))
            {
                string previousNotes = entry.DebugNotes;
                entry.TransferPhase = successPhase;
                entry.DebugNotes = string.IsNullOrEmpty(previousNotes)
                    ? successNotes
                    : successNotes + " Previous restore note: " + previousNotes;
                state.RemoveEmptyRefrigeratedLaunchStagingHolders();
                return RestoreEntryDisposition.Resolved;
            }

            if (sourceOwner.TryAdd(taken, false))
            {
                entry.DebugNotes = "Fallback owner rejected the recovery thing; it was returned to its saved recovery owner.";
                failureReason = "fallback owner rejected the recovery thing; it remains in its saved recovery owner";
                return RestoreEntryDisposition.RecoverableFailure;
            }

            ShuttleTransferRecoveryStatus recoveryStatus;
            string recoveryFailureReason;
            if (state.TryRecoverTransferThing(
                taken,
                "Fallback owner rejected staged refrigerated cargo and rollback-to-staging failed. " +
                    entry.DumpForDebug(),
                fallbackOwner,
                sourceOwner,
                state.parent != null ? state.parent.Map : null,
                state.parent != null ? state.parent.Position : IntVec3.Invalid,
                out recoveryStatus,
                out recoveryFailureReason))
            {
                if (recoveryStatus == ShuttleTransferRecoveryStatus.RecoveredToPreferredOwner)
                {
                    string previousNotes = entry.DebugNotes;
                    entry.TransferPhase = successPhase;
                    entry.DebugNotes = string.IsNullOrEmpty(previousNotes)
                        ? successNotes + " Emergency recovery retried fallback owner successfully."
                        : successNotes + " Emergency recovery retried fallback owner successfully. Previous restore note: " + previousNotes;
                    return RestoreEntryDisposition.Resolved;
                }

                entry.TransferPhase = RefrigeratedCargoLaunchManifestConstants.PhaseQuarantined;
                entry.DebugNotes = "Fallback owner rejected staged thing; rollback-to-staging failed, but emergency recovery secured it. status=" +
                    recoveryStatus;
                failureReason = "fallback owner rejected staged thing; emergency recovery secured it with status=" +
                    recoveryStatus;
                return RestoreEntryDisposition.RecoverableFailure;
            }

            entry.TransferPhase = RefrigeratedCargoLaunchManifestConstants.PhaseFailed;
            entry.DebugNotes = "Fallback owner rejected staged thing; rollback-to-staging and emergency recovery failed.";
            failureReason = "fallback owner rejected staged thing; rollback-to-staging and emergency recovery failed. thingOwner=" +
                DescribeThingOwner(taken) +
                " recovery=" +
                (recoveryFailureReason ?? "null");
            return RestoreEntryDisposition.FatalUnresolved;
        }

        private static RestoreEntryDisposition TryQuarantineEntryInStagingForRecovery(
            CompShuttleHolderLaunchTransferState state,
            RefrigeratedCargoLaunchManifestEntry entry,
            string priorFailureReason,
            out string failureReason)
        {
            failureReason = null;
            if (state == null || entry == null)
            {
                failureReason = "transfer state or manifest entry is unavailable for refrigerated recovery quarantine";
                return RestoreEntryDisposition.FatalUnresolved;
            }

            if (state.ContainsEmergencyRecoveryThing(entry.ThingIDNumber))
            {
                entry.TransferPhase = RefrigeratedCargoLaunchManifestConstants.PhaseQuarantined;
                entry.DebugNotes = "Arrival restore could not resolve this refrigerated cargo entry; the thing remains in the saved emergency recovery holder. priorFailure=" +
                    (priorFailureReason ?? "none");
                return RestoreEntryDisposition.Resolved;
            }

            RefrigeratedLaunchStagingHolder stagingHolder;
            if (!state.TryGetRefrigeratedLaunchStagingHolder(entry.ModuleInstanceID, out stagingHolder) ||
                stagingHolder == null ||
                stagingHolder.Contents == null)
            {
                entry.TransferPhase = RefrigeratedCargoLaunchManifestConstants.PhaseFailed;
                entry.DebugNotes = "Restore failed and no refrigerated staging holder was available for recovery quarantine.";
                failureReason = "no refrigerated staging holder was available for recovery quarantine. priorFailure=" +
                    (priorFailureReason ?? "null");
                return RestoreEntryDisposition.FatalUnresolved;
            }

            Thing thing = FindThingInOwner(stagingHolder.Contents, entry.ThingIDNumber);
            if (thing == null)
            {
                entry.TransferPhase = RefrigeratedCargoLaunchManifestConstants.PhaseFailed;
                entry.DebugNotes = "Restore failed and the manifest thing was not found in refrigerated staging for recovery quarantine.";
                failureReason = "manifest thing was not found in refrigerated staging for recovery quarantine. priorFailure=" +
                    (priorFailureReason ?? "null");
                return RestoreEntryDisposition.FatalUnresolved;
            }

            entry.TransferPhase = RefrigeratedCargoLaunchManifestConstants.PhaseQuarantined;
            entry.DebugNotes = "Arrival restore could not safely resolve this refrigerated cargo entry; thing remains in refrigerated launch staging as a saved recovery quarantine. priorFailure=" +
                (priorFailureReason ?? "none");
            return RestoreEntryDisposition.Resolved;
        }

        private static bool TryResolveRefrigeratedRecoverySource(
            CompShuttleHolderLaunchTransferState state,
            RefrigeratedCargoLaunchManifestEntry entry,
            out ThingOwner<Thing> sourceOwner,
            out Thing thing)
        {
            sourceOwner = null;
            thing = null;
            if (state == null || entry == null || entry.ThingIDNumber <= 0)
            {
                return false;
            }

            RefrigeratedLaunchStagingHolder stagingHolder;
            state.TryGetRefrigeratedLaunchStagingHolder(
                entry.ModuleInstanceID,
                out stagingHolder);
            sourceOwner = stagingHolder != null
                ? stagingHolder.Contents
                : null;
            thing = FindThingInOwner(sourceOwner, entry.ThingIDNumber);
            if (thing != null)
            {
                return true;
            }

            sourceOwner = state.EmergencyRecoveryThings;
            thing = FindThingInOwner(sourceOwner, entry.ThingIDNumber);
            return thing != null;
        }

        private static bool TryBuildUnmatchedStagingContentsFailure(
            ThingWithComps shuttleHost,
            CompShuttleHolderLaunchTransferState state,
            string phase,
            out string failureReason)
        {
            failureReason = null;
            if (state == null || state.RefrigeratedLaunchStagingHolders == null)
            {
                return true;
            }

            bool allMatched = true;
            StringBuilder builder = null;
            IReadOnlyList<RefrigeratedLaunchStagingHolder> holders =
                state.RefrigeratedLaunchStagingHolders;
            for (int holderIndex = 0; holderIndex < holders.Count; holderIndex++)
            {
                RefrigeratedLaunchStagingHolder holder = holders[holderIndex];
                if (holder == null || holder.Contents == null || holder.Contents.Count == 0)
                {
                    continue;
                }

                for (int thingIndex = 0; thingIndex < holder.Contents.Count; thingIndex++)
                {
                    Thing thing = holder.Contents[thingIndex];
                    if (thing == null)
                    {
                        continue;
                    }

                    RefrigeratedCargoLaunchManifestEntry entry =
                        state.RefrigeratedCargoLaunchManifest.FindEntry(
                            holder.ModuleInstanceID,
                            thing.thingIDNumber);
                    if (entry != null)
                    {
                        continue;
                    }

                    allMatched = false;
                    if (builder == null)
                    {
                        builder = new StringBuilder();
                    }

                    builder.AppendLine(BuildRestoreDiagnostic(
                        shuttleHost,
                        state,
                        null,
                        "unmatched refrigerated staging content during " +
                            (phase ?? "unknown") +
                            ": moduleInstanceID=" +
                            (holder.ModuleInstanceID ?? "null") +
                            " sourceModuleDefName=" +
                            (holder.SourceModuleDefName ?? "null") +
                            " holderIndex=" +
                            holderIndex +
                            " thingIndex=" +
                            thingIndex +
                            " thingID=" +
                            thing.thingIDNumber +
                            " defName=" +
                            (thing.def != null ? thing.def.defName : "null") +
                            " stackCount=" +
                            thing.stackCount +
                            " phase=UnmatchedStaging"));
                }
            }

            if (!allMatched)
            {
                failureReason = builder != null ? builder.ToString() : "unmatched refrigerated staging contents";
            }

            return allMatched;
        }

        private static bool TryLogUnmatchedStagingContents(
            CompShuttleHolderLaunchTransferState state,
            string phase)
        {
            if (state == null || state.RefrigeratedLaunchStagingHolders == null)
            {
                return true;
            }

            bool allMatched = true;
            IReadOnlyList<RefrigeratedLaunchStagingHolder> holders =
                state.RefrigeratedLaunchStagingHolders;
            for (int holderIndex = 0; holderIndex < holders.Count; holderIndex++)
            {
                RefrigeratedLaunchStagingHolder holder = holders[holderIndex];
                if (holder == null || holder.Contents == null || holder.Contents.Count == 0)
                {
                    continue;
                }

                for (int thingIndex = 0; thingIndex < holder.Contents.Count; thingIndex++)
                {
                    Thing thing = holder.Contents[thingIndex];
                    if (thing == null)
                    {
                        continue;
                    }

                    RefrigeratedCargoLaunchManifestEntry entry =
                        state.RefrigeratedCargoLaunchManifest.FindEntry(
                            holder.ModuleInstanceID,
                            thing.thingIDNumber);
                    if (entry != null)
                    {
                        continue;
                    }

                    allMatched = false;
                    Log.Error("[CeleTech Shuttle] Refrigerated launch staging holder has contents without a matching manifest entry during " +
                        (phase ?? "unknown") +
                        ". moduleInstanceID=" +
                        (holder.ModuleInstanceID ?? "null") +
                        " sourceModuleDefName=" +
                        (holder.SourceModuleDefName ?? "null") +
                        " holderIndex=" +
                        holderIndex +
                        " thingIndex=" +
                        thingIndex +
                        " thingID=" +
                        thing.thingIDNumber +
                        " defName=" +
                        (thing.def != null ? thing.def.defName : "null") +
                        " stackCount=" +
                        thing.stackCount +
                        " manifest=" +
                        state.RefrigeratedCargoLaunchManifest.DumpForDebug());
                }
            }

            return allMatched;
        }

        private static Thing FindThingInOwner(ThingOwner owner, int thingIDNumber)
        {
            if (owner == null || thingIDNumber <= 0)
            {
                return null;
            }

            for (int i = 0; i < owner.Count; i++)
            {
                Thing thing = owner[i];
                if (thing != null && !thing.Destroyed && thing.thingIDNumber == thingIDNumber)
                {
                    return thing;
                }
            }

            return null;
        }

        private static string AppendNote(string current, string note)
        {
            if (string.IsNullOrEmpty(note))
            {
                return current;
            }

            return string.IsNullOrEmpty(current) ? note : current + " | " + note;
        }

        private static string BuildRestoreDiagnostic(
            ThingWithComps shuttleHost,
            CompShuttleHolderLaunchTransferState state,
            RefrigeratedCargoLaunchManifestEntry entry,
            string reason)
        {
            return "shuttleThingID=" +
                (shuttleHost != null ? shuttleHost.thingIDNumber : -1) +
                " manifestEntryCount=" +
                GetManifestEntryCount(state) +
                " failedThingID=" +
                (entry != null ? entry.ThingIDNumber : -1) +
                " moduleInstanceID=" +
                (entry != null ? entry.ModuleInstanceID ?? "null" : "null") +
                " phase=" +
                (entry != null ? entry.TransferPhase ?? "null" : "null") +
                " reason=" +
                (reason ?? "unknown") +
                " entry=" +
                (entry != null ? entry.DumpForDebug() : "null");
        }

        private static RefrigeratedCargoRestoreResult BuildFatalRestoreResult(
            ThingWithComps shuttleHost,
            CompShuttleHolderLaunchTransferState state,
            string reason)
        {
            string debugDump = BuildRestoreFailClosedDebugDump(shuttleHost, state, reason);
            return new RefrigeratedCargoRestoreResult(
                RefrigeratedCargoRestoreStatus.FatalUnresolved,
                "[CeleTech Shuttle] Refrigerated cargo arrival restore failed closed. " + debugDump,
                debugDump,
                false,
                false);
        }

        private static string BuildRestoreFailClosedDebugDump(
            ThingWithComps shuttleHost,
            CompShuttleHolderLaunchTransferState state,
            string reason)
        {
            return "shuttleThingID=" +
                (shuttleHost != null ? shuttleHost.thingIDNumber : -1) +
                " hasRefrigeratedCargoLaunchTransfer=" +
                HasRefrigeratedCargoLaunchTransferForDebug(state) +
                " refrigeratedManifestEntryCount=" +
                GetManifestEntryCountForDebug(state) +
                " refrigeratedStagingHolderCount=" +
                GetRefrigeratedStagingHolderCountForDebug(state) +
                " sourceMethod=ShuttleRefrigeratedCargoLaunchTransferService.TryRestoreBeforeImpact" +
                " reason=" +
                (reason ?? "unknown") +
                " transferDump=" +
                DumpRefrigeratedTransferForDebugSafe(state);
        }

        private static int GetManifestEntryCount(CompShuttleHolderLaunchTransferState state)
        {
            if (state == null ||
                state.RefrigeratedCargoLaunchManifest == null ||
                state.RefrigeratedCargoLaunchManifest.Entries == null)
            {
                return 0;
            }

            return state.RefrigeratedCargoLaunchManifest.Entries.Count;
        }

        private static int GetRefrigeratedStagingHolderCount(CompShuttleHolderLaunchTransferState state)
        {
            return state != null ? state.RefrigeratedLaunchStagingHolderCount : 0;
        }

        private static CompShuttleHolderLaunchTransferState TryGetTransferStateForRestoreDiagnostics(
            ThingWithComps shuttleHost)
        {
            try
            {
                return GetTransferState(shuttleHost);
            }
            catch
            {
                return null;
            }
        }

        private static bool HasRefrigeratedCargoLaunchTransferForDebug(
            CompShuttleHolderLaunchTransferState state)
        {
            try
            {
                return state != null && state.HasRefrigeratedCargoLaunchTransfer;
            }
            catch
            {
                return false;
            }
        }

        private static int GetManifestEntryCountForDebug(CompShuttleHolderLaunchTransferState state)
        {
            try
            {
                return GetManifestEntryCount(state);
            }
            catch
            {
                return -1;
            }
        }

        private static int GetRefrigeratedStagingHolderCountForDebug(
            CompShuttleHolderLaunchTransferState state)
        {
            try
            {
                return GetRefrigeratedStagingHolderCount(state);
            }
            catch
            {
                return -1;
            }
        }

        private static string DumpRefrigeratedTransferForDebugSafe(
            CompShuttleHolderLaunchTransferState state)
        {
            if (state == null)
            {
                return "state unavailable";
            }

            try
            {
                return state.DumpRefrigeratedCargoLaunchTransferForDebug();
            }
            catch (Exception exception)
            {
                return "refrigerated transfer dump failed: " + exception;
            }
        }

        private static string DescribeThingOwner(Thing thing)
        {
            if (thing == null)
            {
                return "thing-null";
            }

            return thing.holdingOwner != null
                ? thing.holdingOwner.GetType().Name
                : "ownerless";
        }

        private static bool TryValidateStagedThingMatchesManifest(
            Thing thing,
            RefrigeratedCargoLaunchManifestEntry entry,
            out string mismatchReason)
        {
            mismatchReason = null;
            if (thing == null || entry == null)
            {
                mismatchReason = "thing or manifest entry is unavailable";
                return false;
            }

            if (thing.thingIDNumber != entry.ThingIDNumber)
            {
                mismatchReason = "thingIDNumber mismatch: staged=" +
                    thing.thingIDNumber +
                    " manifest=" +
                    entry.ThingIDNumber;
                return false;
            }

            string stagedDefName = thing.def != null ? thing.def.defName : null;
            if (string.IsNullOrEmpty(entry.ThingDefName) || stagedDefName != entry.ThingDefName)
            {
                mismatchReason = "defName mismatch: staged=" +
                    (stagedDefName ?? "null") +
                    " manifest=" +
                    (entry.ThingDefName ?? "null");
                return false;
            }

            if (thing.stackCount != entry.OriginalStackCount)
            {
                mismatchReason = "stackCount mismatch: staged=" +
                    thing.stackCount +
                    " manifest=" +
                    entry.OriginalStackCount;
                return false;
            }

            return true;
        }

        private static CompShuttleHolderLaunchTransferState GetTransferState(ThingWithComps shuttleHost)
        {
            return shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHolderLaunchTransferState>()
                : null;
        }

        private static ShuttleRefrigeratedCargoModuleDef ResolveModuleDef(string moduleDefName)
        {
            return !string.IsNullOrEmpty(moduleDefName)
                ? DefDatabase<ShuttleRefrigeratedCargoModuleDef>.GetNamedSilentFail(moduleDefName)
                : null;
        }

        private static string BuildLaunchBlockFailure(
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            RefrigeratedCargoRecord record,
            string reason)
        {
            string label = moduleDef != null
                ? moduleDef.LabelCap.ToString()
                : (record != null && !string.IsNullOrEmpty(record.ModuleDefName)
                    ? record.ModuleDefName
                    : "refrigerated cargo");
            string moduleInstanceID = record != null ? record.ModuleInstanceID : null;
            int stackCount = record != null ? record.StackCount : 0;
            return "CT_Shuttle_Launch_Failed_RefrigeratedCargoTransferInvalid"
                .Translate(
                    label,
                    string.IsNullOrEmpty(moduleInstanceID) ? "unknown" : moduleInstanceID,
                    stackCount,
                    reason ?? "unknown")
                .ToString();
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

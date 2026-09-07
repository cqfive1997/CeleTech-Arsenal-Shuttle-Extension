using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    /// <summary>
    /// Dispatches launch-time runtime hooks against detached launch snapshots.
    /// Live AssemblyState may already be moving through a launch transaction, so these hooks
    /// must not assume map-side module records are mutable or available.
    /// </summary>
    internal sealed class ShuttleRuntimeLaunchCallbackDispatcher
    {
        private readonly BuiltInShuttleModuleRuntimeRegistry registry;
        private readonly ShuttleRuntimeDispatchSupport support;

        internal ShuttleRuntimeLaunchCallbackDispatcher(
            BuiltInShuttleModuleRuntimeRegistry registry,
            ShuttleRuntimeDispatchSupport support)
        {
            this.registry = registry;
            this.support = support;
        }

        internal bool PreLaunchValidate(
            ShuttleLaunchAssemblySnapshot assemblySnapshot,
            ShuttleModuleRuntimeStateBucket moduleStates,
            out string reason)
        {
            return this.PreLaunchValidate(
                assemblySnapshot,
                moduleStates,
                true,
                out reason);
        }

        internal bool PreLaunchValidate(
            ShuttleLaunchAssemblySnapshot assemblySnapshot,
            ShuttleModuleRuntimeStateBucket moduleStates,
            bool includeExternalRuntimeSystems,
            out string reason)
        {
            // Launch validation consumes the detached confirmation-time snapshot. Disabled
            // installed modules still run validation so durable runtime blockers, such as
            // pending products, cannot be bypassed by toggling the module off.
            reason = null;

            if (assemblySnapshot == null || moduleStates == null)
            {
                return true;
            }

            IReadOnlyList<ShuttleLaunchModuleRecord> moduleRecords = assemblySnapshot.Modules;
            if (moduleRecords == null)
            {
                return true;
            }

            for (int i = 0; i < moduleRecords.Count; i++)
            {
                ShuttleLaunchModuleRecord moduleRecord = moduleRecords[i];
                if (moduleRecord == null || !moduleRecord.IsInstalled)
                {
                    continue;
                }

                foreach (IShuttleModuleRuntimeSystem system in this.registry.SystemsForRead)
                {
                    if (!includeExternalRuntimeSystems &&
                        system is ExternalShuttleModuleRuntimeSystemAdapter)
                    {
                        continue;
                    }

                    string appliesFailureReason;
                    if (!this.support.AppliesToSystem(system, moduleRecord, "launch validation", out appliesFailureReason))
                    {
                        if (!string.IsNullOrEmpty(appliesFailureReason))
                        {
                            reason = appliesFailureReason;
                            return false;
                        }

                        continue;
                    }

                    IShuttleModuleRuntimeState state;
                    if (!moduleStates.TryGetState(
                        moduleRecord.ModuleInstanceID,
                        system.RuntimeSystemKey,
                        out state))
                    {
                        if (system is ExternalShuttleModuleRuntimeSystemAdapter)
                        {
                            ShuttleModulePreLaunchValidationContext missingExternalStateContext =
                                this.support.CreatePreLaunchValidationContext(
                                    assemblySnapshot,
                                    moduleRecord,
                                    null);

                            try
                            {
                                if (!system.PreLaunchValidate(missingExternalStateContext, out reason))
                                {
                                    return false;
                                }
                            }
                            catch (Exception exception)
                            {
                                reason = "Module runtime launch validation failed for " +
                                    this.support.GetModuleDebugName(moduleRecord) + " / " +
                                    system.RuntimeSystemKey + ".";
                                Log.ErrorOnce(
                                    "[CeleTech Shuttle] " + reason + " Exception: " + exception,
                                    this.support.MakeRuntimeLogHash(
                                        "launch-validation-missing-external-state-exception",
                                        moduleRecord != null ? moduleRecord.ModuleInstanceID : null,
                                        system != null ? system.RuntimeSystemKey : null,
                                        exception.GetType().FullName + exception.Message));
                                return false;
                            }

                            continue;
                        }

                        if (!moduleRecord.IsEnabled)
                        {
                            continue;
                        }

                        reason = "Missing module runtime state for " + this.support.GetModuleDebugName(moduleRecord) +
                            " / " + system.RuntimeSystemKey + ".";
                        Log.WarningOnce(
                            "[CeleTech Shuttle] " + reason,
                            this.support.MakeRuntimeLogHash(
                                "launch-validation-missing-state",
                                moduleRecord != null ? moduleRecord.ModuleInstanceID : null,
                                system != null ? system.RuntimeSystemKey : null,
                                reason));
                        return false;
                    }

                    ShuttleModulePreLaunchValidationContext context = this.support.CreatePreLaunchValidationContext(
                        assemblySnapshot,
                        moduleRecord,
                        state);

                    try
                    {
                        if (!system.PreLaunchValidate(context, out reason))
                        {
                            return false;
                        }
                    }
                    catch (Exception exception)
                    {
                        reason = "Module runtime launch validation failed for " +
                            this.support.GetModuleDebugName(moduleRecord) + " / " + system.RuntimeSystemKey + ".";
                        Log.ErrorOnce(
                            "[CeleTech Shuttle] " + reason + " Exception: " + exception,
                            this.support.MakeRuntimeLogHash(
                                "launch-validation-exception",
                                moduleRecord != null ? moduleRecord.ModuleInstanceID : null,
                                system != null ? system.RuntimeSystemKey : null,
                                exception.GetType().FullName + exception.Message));
                        return false;
                    }
                }
            }

            return true;
        }

        internal void NotifyLaunchSucceeded(
            ShuttleLaunchAssemblySnapshot assemblySnapshot,
            ShuttleModuleRuntimeStateBucket moduleStates)
        {
            // Launch success notification runs after the launch transaction has already succeeded.
            if (assemblySnapshot == null || moduleStates == null)
            {
                return;
            }

            IReadOnlyList<ShuttleLaunchModuleRecord> moduleRecords = assemblySnapshot.Modules;
            if (moduleRecords == null)
            {
                return;
            }

            for (int i = 0; i < moduleRecords.Count; i++)
            {
                ShuttleLaunchModuleRecord moduleRecord = moduleRecords[i];
                if (moduleRecord == null || !moduleRecord.IsInstalled)
                {
                    continue;
                }

                if (!moduleRecord.IsEnabled)
                {
                    continue;
                }

                foreach (IShuttleModuleRuntimeSystem system in this.registry.SystemsForRead)
                {
                    string appliesFailureReason;
                    if (!this.support.AppliesToSystem(system, moduleRecord, "launch success notification", out appliesFailureReason))
                    {
                        continue;
                    }

                    IShuttleModuleRuntimeState state;
                    if (!moduleStates.TryGetState(
                        moduleRecord.ModuleInstanceID,
                        system.RuntimeSystemKey,
                        out state))
                    {
                        if (system is ExternalShuttleModuleRuntimeSystemAdapter)
                        {
                            ShuttleModuleLaunchContext missingExternalStateContext =
                                this.support.CreateLaunchContext(
                                    assemblySnapshot,
                                    moduleRecord,
                                    null);

                            this.support.TryRunRuntimeAction(
                                system,
                                moduleRecord,
                                "launch success notification",
                                () => system.OnLaunchSucceeded(missingExternalStateContext));
                        }

                        continue;
                    }

                    ShuttleModuleLaunchContext context = this.support.CreateLaunchContext(
                        assemblySnapshot,
                        moduleRecord,
                        state);

                    this.support.TryRunRuntimeAction(system, moduleRecord, "launch success notification", () => system.OnLaunchSucceeded(context));
                }
            }
        }
    }
}

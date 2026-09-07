using System;
using System.Collections.Generic;
using System.Text;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal static class ExternalRuntimeDiagnostics
    {
        private static readonly HashSet<string> missingRegisteredKeyWarnings =
            new HashSet<string>(StringComparer.Ordinal);

        internal static string DumpExternalRuntimeRegistrations()
        {
            List<ExternalRuntimeRegistration> registrations =
                ExternalShuttleRuntimeRegistry.GetRegistrationsSnapshot();
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("registered external runtime count=" +
                (registrations != null ? registrations.Count : 0) +
                " revision=" + ExternalShuttleRuntimeRegistry.Revision);

            for (int i = 0; registrations != null && i < registrations.Count; i++)
            {
                ExternalRuntimeRegistration registration = registrations[i];
                if (registration == null)
                {
                    builder.AppendLine("  [" + i + "] null registration");
                    continue;
                }

                builder.AppendLine("  [" + i + "] key=" + registration.FullRuntimeKey +
                    " owner=" + registration.OwnerPackageId +
                    " localKey=" + registration.LocalRuntimeKey +
                    " labelKey=" + (registration.RuntimeLabelKey ?? "(none)") +
                    " descriptionKey=" + (registration.RuntimeDescriptionKey ?? "(none)") +
                    " systemType=" + GetSystemTypeName(registration) +
                    " schemaVersion=" + GetSchemaVersionSafe(registration));
            }

            return builder.ToString();
        }

        internal static string DumpExternalRuntimeBindings(ShuttleAssemblyState assemblyState)
        {
            StringBuilder builder = new StringBuilder();
            if (assemblyState == null)
            {
                builder.AppendLine("external runtime bindings: assemblyState=null");
                return builder.ToString();
            }

            assemblyState.EnsureInitialized();
            IReadOnlyList<ShuttleModule> modules = assemblyState.Modules;
            builder.AppendLine("external runtime bindings: moduleCount=" +
                (modules != null ? modules.Count : 0));

            for (int i = 0; modules != null && i < modules.Count; i++)
            {
                ShuttleModule module = modules[i];
                if (module == null)
                {
                    continue;
                }

                List<string> runtimeKeys =
                    ExternalRuntimeBindingUtility.GetRuntimeSystemKeys(module.ModuleDef);
                if (runtimeKeys == null || runtimeKeys.Count == 0)
                {
                    continue;
                }

                builder.AppendLine("  module=" + GetModuleDebugName(module) +
                    " runtimeKeys=" + JoinKeys(runtimeKeys));

                for (int keyIndex = 0; keyIndex < runtimeKeys.Count; keyIndex++)
                {
                    string key = runtimeKeys[keyIndex];
                    ExternalRuntimeRegistration registration;
                    bool registered =
                        ExternalShuttleRuntimeRegistry.TryResolve(key, out registration);
                    builder.AppendLine("    key=" + key +
                        " registered=" + registered);
                }
            }

            return builder.ToString();
        }

        internal static string DumpExternalRuntimeStates(ShuttleRuntimeState runtimeState)
        {
            StringBuilder builder = new StringBuilder();
            if (runtimeState == null)
            {
                builder.AppendLine("external runtime states: runtimeState=null");
                return builder.ToString();
            }

            runtimeState.EnsureInitialized();
            ShuttleModuleRuntimeStateBucket moduleStates = runtimeState.Modules;
            IReadOnlyList<ShuttleModuleRuntimeStateRecord> records =
                moduleStates != null ? moduleStates.RecordsForRead : null;
            builder.AppendLine("external runtime states: recordCount=" +
                (records != null ? records.Count : 0));

            for (int i = 0; records != null && i < records.Count; i++)
            {
                ShuttleModuleRuntimeStateRecord record = records[i];
                if (record == null)
                {
                    continue;
                }

                ExternalRuntimeRegistration registration;
                bool registered = ExternalShuttleRuntimeRegistry.TryResolve(
                    record.RuntimeSystemKey,
                    out registration);
                ExternalModuleRuntimeState externalState =
                    record.State as ExternalModuleRuntimeState;
                if (!registered && externalState == null)
                {
                    continue;
                }

                builder.AppendLine("  module=" + record.ModuleInstanceID +
                    " runtimeKey=" + record.RuntimeSystemKey +
                    " registered=" + registered +
                    " stateType=" + (record.State != null ? record.State.GetType().FullName : "null") +
                    " schemaVersion=" + (externalState != null ? externalState.SchemaVersion : 0) +
                    " initialized=" + (externalState != null && externalState.Initialized) +
                    " initializationFailed=" + (externalState != null && externalState.InitializationFailed) +
                    " migrationFailed=" + (externalState != null && externalState.MigrationFailed) +
                    " runtimeExecutionFailed=" + (externalState != null && externalState.RuntimeExecutionFailed) +
                    " lastFailure=" + (externalState != null && !string.IsNullOrEmpty(externalState.LastFailureMessage)
                        ? externalState.LastFailureMessage
                        : "(none)") +
                    " valueCount=" + GetExternalValueCount(externalState));
            }

            return builder.ToString();
        }

        internal static void LogCompositeSummaryIfDevMode(
            int compositeRuntimeCount,
            int dispatchFingerprint)
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            Log.Message(ExternalShuttleRuntimeRegistry.LogPrefix +
                "diagnostics summary: registeredExternalCount=" +
                ExternalShuttleRuntimeRegistry.GetRegistrationsSnapshot().Count +
                " compositeRuntimeCount=" + compositeRuntimeCount +
                " dispatchFingerprint=" + dispatchFingerprint);
            Log.Message(ExternalShuttleRuntimeRegistry.LogPrefix +
                "diagnostics registrations:\n" + DumpExternalRuntimeRegistrations());
        }

        internal static void LogBindingsAndStatesIfDevMode(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            int dispatchFingerprint)
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            Log.Message(ExternalShuttleRuntimeRegistry.LogPrefix +
                "diagnostics runtime sync dispatchFingerprint=" + dispatchFingerprint +
                "\n" + DumpExternalRuntimeBindings(assemblyState) +
                DumpExternalRuntimeStates(runtimeState));
            WarnMissingRegisteredBindingsOnce(assemblyState);
        }

        internal static void WarnMissingRegisteredBindingsIfDevMode(
            ShuttleAssemblyState assemblyState)
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            WarnMissingRegisteredBindingsOnce(assemblyState);
        }

        private static string GetSystemTypeName(ExternalRuntimeRegistration registration)
        {
            return registration != null && registration.System != null
                ? registration.System.GetType().FullName
                : "null";
        }

        private static int GetSchemaVersionSafe(ExternalRuntimeRegistration registration)
        {
            if (registration == null || registration.System == null)
            {
                return 0;
            }

            try
            {
                return registration.System.StateSchemaVersion;
            }
            catch (Exception exception)
            {
                Log.Warning(ExternalShuttleRuntimeRegistry.LogPrefix +
                    "diagnostics could not read StateSchemaVersion for key '" +
                    registration.FullRuntimeKey + "'. Exception: " + exception);
                return 0;
            }
        }

        private static string JoinKeys(List<string> keys)
        {
            if (keys == null || keys.Count == 0)
            {
                return "(none)";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < keys.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(keys[i]);
            }

            return builder.ToString();
        }

        private static string GetModuleDebugName(ShuttleModule module)
        {
            if (module == null)
            {
                return "null module";
            }

            return module.ModuleInstanceID + " (" +
                (!string.IsNullOrEmpty(module.moduleDefName) ? module.moduleDefName : "missing def") +
                ")";
        }

        private static int GetExternalValueCount(ExternalModuleRuntimeState externalState)
        {
            return externalState != null && externalState.ValuesForDebug != null
                ? externalState.ValuesForDebug.Count
                : 0;
        }

        private static void WarnMissingRegisteredBindingsOnce(ShuttleAssemblyState assemblyState)
        {
            if (assemblyState == null)
            {
                return;
            }

            assemblyState.EnsureInitialized();
            IReadOnlyList<ShuttleModule> modules = assemblyState.Modules;
            for (int i = 0; modules != null && i < modules.Count; i++)
            {
                ShuttleModule module = modules[i];
                if (module == null)
                {
                    continue;
                }

                List<string> runtimeKeys =
                    ExternalRuntimeBindingUtility.GetRuntimeSystemKeys(module.ModuleDef);
                for (int keyIndex = 0; runtimeKeys != null && keyIndex < runtimeKeys.Count; keyIndex++)
                {
                    string key = runtimeKeys[keyIndex];
                    ExternalRuntimeRegistration registration;
                    if (ExternalShuttleRuntimeRegistry.TryResolve(key, out registration))
                    {
                        continue;
                    }

                    string warningKey = module.ModuleInstanceID + "/" + key;
                    if (!missingRegisteredKeyWarnings.Add(warningKey))
                    {
                        continue;
                    }

                    Log.Warning(ExternalShuttleRuntimeRegistry.LogPrefix +
                        "diagnostics found module XML runtime key '" + key +
                        "' but no external runtime is registered for " +
                        GetModuleDebugName(module) + ".");
                }
            }
        }
    }
}

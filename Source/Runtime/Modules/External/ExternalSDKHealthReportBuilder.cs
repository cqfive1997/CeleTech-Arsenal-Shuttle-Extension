using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Commands.External;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Extensions;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.UI.External;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal static class ExternalSDKHealthReportBuilder
    {
        internal static ShuttleExternalIntegrationHealthReport Build(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState)
        {
            List<ShuttleExternalRuntimeHealthSnapshot> runtimeRows =
                BuildRuntimeRows(assemblyState, runtimeState);
            List<ShuttleExternalCommandRegistrationSnapshot> commandRows =
                BuildCommandRows();
            List<ShuttleExternalPanelRegistrationSnapshot> panelRows =
                BuildPanelRows();
            List<ShuttleExternalProfileContributorSnapshot> profileContributorRows =
                BuildProfileContributorRows();

            return new ShuttleExternalIntegrationHealthReport(
                true,
                null,
                BuildRevision(runtimeState),
                ExternalShuttleRuntimeRegistry.GetRegistrationsSnapshot().Count,
                CountRuntimeStateRows(runtimeRows),
                commandRows.Count,
                panelRows.Count,
                profileContributorRows.Count,
                runtimeRows,
                commandRows,
                panelRows,
                profileContributorRows);
        }

        private static List<ShuttleExternalRuntimeHealthSnapshot> BuildRuntimeRows(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState)
        {
            List<ShuttleExternalRuntimeHealthSnapshot> rows =
                new List<ShuttleExternalRuntimeHealthSnapshot>();
            List<ExternalRuntimeRegistration> registrations =
                ExternalShuttleRuntimeRegistry.GetRegistrationsSnapshot();

            for (int i = 0; i < registrations.Count; i++)
            {
                rows.Add(BuildRegisteredRuntimeRow(registrations[i]));
            }

            if (runtimeState == null)
            {
                return rows;
            }

            runtimeState.EnsureInitialized();
            IReadOnlyList<ShuttleModuleRuntimeStateRecord> records =
                runtimeState.Modules != null ? runtimeState.Modules.RecordsForRead : null;
            for (int i = 0; records != null && i < records.Count; i++)
            {
                ShuttleModuleRuntimeStateRecord record = records[i];
                if (record == null)
                {
                    continue;
                }

                ExternalModuleRuntimeState externalState =
                    record.State as ExternalModuleRuntimeState;
                if (externalState == null)
                {
                    continue;
                }

                ExternalRuntimeRegistration registration;
                ExternalShuttleRuntimeRegistry.TryResolve(
                    record.RuntimeSystemKey,
                    out registration);
                rows.Add(BuildRuntimeStateRow(assemblyState, record, externalState, registration));
            }

            return rows;
        }

        private static ShuttleExternalRuntimeHealthSnapshot BuildRegisteredRuntimeRow(
            ExternalRuntimeRegistration registration)
        {
            string owner;
            string local;
            SplitKey(
                registration != null ? registration.FullRuntimeKey : null,
                out owner,
                out local);

            return new ShuttleExternalRuntimeHealthSnapshot(
                registration != null ? registration.OwnerPackageId : owner,
                registration != null ? registration.LocalRuntimeKey : local,
                registration != null ? registration.FullRuntimeKey : null,
                registration != null ? registration.RuntimeLabelKey : null,
                registration != null ? registration.RuntimeDescriptionKey : null,
                GetRegisteredSchemaVersionSafe(registration),
                null,
                null,
                registration != null,
                false,
                0,
                false,
                false,
                false,
                false,
                false,
                false,
                null,
                -1,
                false,
                0);
        }

        private static ShuttleExternalRuntimeHealthSnapshot BuildRuntimeStateRow(
            ShuttleAssemblyState assemblyState,
            ShuttleModuleRuntimeStateRecord record,
            ExternalModuleRuntimeState state,
            ExternalRuntimeRegistration registration)
        {
            ShuttleModule module = GetModuleSafe(assemblyState, record.ModuleInstanceID);
            string owner;
            string local;
            SplitKey(record.RuntimeSystemKey, out owner, out local);

            return new ShuttleExternalRuntimeHealthSnapshot(
                registration != null ? registration.OwnerPackageId : owner,
                registration != null ? registration.LocalRuntimeKey : local,
                record.RuntimeSystemKey,
                registration != null ? registration.RuntimeLabelKey : null,
                registration != null ? registration.RuntimeDescriptionKey : null,
                GetRegisteredSchemaVersionSafe(registration),
                record.ModuleInstanceID,
                module != null ? module.moduleDefName : null,
                registration != null,
                true,
                state != null ? state.SchemaVersion : 0,
                true,
                ExternalRuntimeStateUtility.IsRuntimeEnabled(state),
                state != null && state.Initialized,
                state != null && state.InitializationFailed,
                state != null && state.MigrationFailed,
                state != null && state.RuntimeExecutionFailed,
                state != null ? state.LastFailureMessage : null,
                state != null ? state.LastFailureTick : -1,
                false,
                0);
        }

        private static List<ShuttleExternalCommandRegistrationSnapshot> BuildCommandRows()
        {
            List<ExternalCommandRegistration> registrations =
                ExternalShuttleCommandRegistry.GetRegistrationsSnapshot();
            List<ShuttleExternalCommandRegistrationSnapshot> rows =
                new List<ShuttleExternalCommandRegistrationSnapshot>(registrations.Count);
            for (int i = 0; i < registrations.Count; i++)
            {
                ExternalCommandRegistration registration = registrations[i];
                if (registration == null)
                {
                    continue;
                }

                rows.Add(new ShuttleExternalCommandRegistrationSnapshot(
                    registration.OwnerPackageId,
                    registration.LocalCommandKey,
                    registration.FullCommandKey));
            }

            return rows;
        }

        private static List<ShuttleExternalPanelRegistrationSnapshot> BuildPanelRows()
        {
            List<ExternalModulePanelRegistration> registrations =
                ExternalModulePanelRegistry.GetRegistrationsSnapshot();
            List<ShuttleExternalPanelRegistrationSnapshot> rows =
                new List<ShuttleExternalPanelRegistrationSnapshot>(registrations.Count);
            for (int i = 0; i < registrations.Count; i++)
            {
                ExternalModulePanelRegistration registration = registrations[i];
                if (registration == null)
                {
                    continue;
                }

                rows.Add(new ShuttleExternalPanelRegistrationSnapshot(
                    registration.OwnerPackageId,
                    registration.LocalPanelKey,
                    registration.FullPanelKey,
                    registration.RuntimeSystemKey,
                    false,
                    false));
            }

            return rows;
        }

        private static List<ShuttleExternalProfileContributorSnapshot> BuildProfileContributorRows()
        {
            List<string> keys =
                ShuttleProfileContributorResolver.GetRegisteredContributorKeysSnapshot();
            List<ShuttleExternalProfileContributorSnapshot> rows =
                new List<ShuttleExternalProfileContributorSnapshot>(keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                string owner;
                string local;
                SplitKey(keys[i], out owner, out local);
                rows.Add(new ShuttleExternalProfileContributorSnapshot(owner, local, keys[i]));
            }

            return rows;
        }

        private static int CountRuntimeStateRows(
            List<ShuttleExternalRuntimeHealthSnapshot> rows)
        {
            int count = 0;
            for (int i = 0; rows != null && i < rows.Count; i++)
            {
                if (rows[i] != null && rows[i].StateAvailable)
                {
                    count++;
                }
            }

            return count;
        }

        private static int BuildRevision(ShuttleRuntimeState runtimeState)
        {
            unchecked
            {
                int revision = 17;
                revision = (revision * 31) + ExternalShuttleRuntimeRegistry.Revision;
                revision = (revision * 31) + ExternalShuttleCommandRegistry.Revision;
                revision = (revision * 31) + ExternalModulePanelRegistry.Revision;
                revision = (revision * 31) + (runtimeState != null && runtimeState.Modules != null
                    ? runtimeState.Modules.DispatchRevision
                    : 0);
                return revision;
            }
        }

        private static int GetRegisteredSchemaVersionSafe(
            ExternalRuntimeRegistration registration)
        {
            if (registration == null || registration.System == null)
            {
                return 0;
            }

            try
            {
                int schema = registration.System.StateSchemaVersion;
                return schema > 0 ? schema : 1;
            }
            catch
            {
                return 0;
            }
        }

        private static ShuttleModule GetModuleSafe(
            ShuttleAssemblyState assemblyState,
            string moduleInstanceID)
        {
            if (assemblyState == null || string.IsNullOrEmpty(moduleInstanceID))
            {
                return null;
            }

            try
            {
                assemblyState.EnsureInitialized();
                return assemblyState.GetModule(moduleInstanceID);
            }
            catch
            {
                return null;
            }
        }

        private static void SplitKey(
            string fullKey,
            out string ownerPackageId,
            out string localKey)
        {
            ownerPackageId = null;
            localKey = null;
            if (string.IsNullOrEmpty(fullKey))
            {
                return;
            }

            int slash = fullKey.IndexOf('/');
            if (slash < 0)
            {
                localKey = fullKey;
                return;
            }

            ownerPackageId = slash > 0 ? fullKey.Substring(0, slash) : null;
            localKey = slash + 1 < fullKey.Length ? fullKey.Substring(slash + 1) : null;
        }
    }
}

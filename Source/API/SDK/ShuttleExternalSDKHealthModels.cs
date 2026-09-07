using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.SDK
{
    public sealed class ShuttleExternalIntegrationHealthReport
    {
        public ShuttleExternalIntegrationHealthReport(
            bool available,
            string unavailableReason,
            int revision,
            int runtimeRegistrationCount,
            int runtimeStateCount,
            int commandRegistrationCount,
            int panelRegistrationCount,
            int profileContributorRegistrationCount,
            IEnumerable<ShuttleExternalRuntimeHealthSnapshot> runtimeRows,
            IEnumerable<ShuttleExternalCommandRegistrationSnapshot> commandRows,
            IEnumerable<ShuttleExternalPanelRegistrationSnapshot> panelRows,
            IEnumerable<ShuttleExternalProfileContributorSnapshot> profileContributorRows)
        {
            this.Available = available;
            this.UnavailableReason = unavailableReason;
            this.Revision = revision;
            this.RuntimeRegistrationCount = runtimeRegistrationCount;
            this.RuntimeStateCount = runtimeStateCount;
            this.CommandRegistrationCount = commandRegistrationCount;
            this.PanelRegistrationCount = panelRegistrationCount;
            this.ProfileContributorRegistrationCount = profileContributorRegistrationCount;
            this.RuntimeRows = ShuttleExternalSDKCollections.Copy(runtimeRows);
            this.CommandRows = ShuttleExternalSDKCollections.Copy(commandRows);
            this.PanelRows = ShuttleExternalSDKCollections.Copy(panelRows);
            this.ProfileContributorRows = ShuttleExternalSDKCollections.Copy(profileContributorRows);
        }

        public bool Available { get; private set; }
        public string UnavailableReason { get; private set; }
        public int Revision { get; private set; }
        public int RuntimeRegistrationCount { get; private set; }
        public int RuntimeStateCount { get; private set; }
        public int CommandRegistrationCount { get; private set; }
        public int PanelRegistrationCount { get; private set; }
        public int ProfileContributorRegistrationCount { get; private set; }
        public IReadOnlyList<ShuttleExternalRuntimeHealthSnapshot> RuntimeRows { get; private set; }
        public IReadOnlyList<ShuttleExternalCommandRegistrationSnapshot> CommandRows { get; private set; }
        public IReadOnlyList<ShuttleExternalPanelRegistrationSnapshot> PanelRows { get; private set; }
        public IReadOnlyList<ShuttleExternalProfileContributorSnapshot> ProfileContributorRows { get; private set; }
    }

    public sealed class ShuttleExternalRuntimeHealthSnapshot
    {
        public ShuttleExternalRuntimeHealthSnapshot(
            string ownerPackageId,
            string localRuntimeKey,
            string runtimeKey,
            string runtimeLabelKey,
            string runtimeDescriptionKey,
            int registeredSchemaVersion,
            string moduleInstanceId,
            string moduleDefName,
            bool registered,
            bool stateAvailable,
            int stateSchemaVersion,
            bool runtimeEnabledKnown,
            bool runtimeEnabled,
            bool initialized,
            bool initializationFailed,
            bool migrationFailed,
            bool runtimeExecutionFailed,
            string lastFailureMessage,
            int lastFailureTick,
            bool exceptionCountKnown,
            int exceptionCount)
        {
            this.OwnerPackageId = ownerPackageId;
            this.LocalRuntimeKey = localRuntimeKey;
            this.RuntimeKey = runtimeKey;
            this.RuntimeLabelKey = runtimeLabelKey;
            this.RuntimeDescriptionKey = runtimeDescriptionKey;
            this.RegisteredSchemaVersion = registeredSchemaVersion;
            this.ModuleInstanceId = moduleInstanceId;
            this.ModuleDefName = moduleDefName;
            this.Registered = registered;
            this.StateAvailable = stateAvailable;
            this.StateSchemaVersion = stateSchemaVersion;
            this.RuntimeEnabledKnown = runtimeEnabledKnown;
            this.RuntimeEnabled = runtimeEnabled;
            this.Initialized = initialized;
            this.InitializationFailed = initializationFailed;
            this.MigrationFailed = migrationFailed;
            this.RuntimeExecutionFailed = runtimeExecutionFailed;
            this.LastFailureMessage = lastFailureMessage;
            this.LastFailureTick = lastFailureTick;
            this.ExceptionCountKnown = exceptionCountKnown;
            this.ExceptionCount = exceptionCount;
        }

        public string OwnerPackageId { get; private set; }
        public string LocalRuntimeKey { get; private set; }
        public string RuntimeKey { get; private set; }
        public string RuntimeLabelKey { get; private set; }
        public string RuntimeDescriptionKey { get; private set; }
        public int RegisteredSchemaVersion { get; private set; }
        public string ModuleInstanceId { get; private set; }
        public string ModuleDefName { get; private set; }
        public bool Registered { get; private set; }
        public bool StateAvailable { get; private set; }
        public int StateSchemaVersion { get; private set; }
        public bool RuntimeEnabledKnown { get; private set; }
        public bool RuntimeEnabled { get; private set; }
        public bool Initialized { get; private set; }
        public bool InitializationFailed { get; private set; }
        public bool MigrationFailed { get; private set; }
        public bool RuntimeExecutionFailed { get; private set; }
        public string LastFailureMessage { get; private set; }
        public int LastFailureTick { get; private set; }
        public bool ExceptionCountKnown { get; private set; }
        public int ExceptionCount { get; private set; }
    }

    public sealed class ShuttleExternalCommandRegistrationSnapshot
    {
        public ShuttleExternalCommandRegistrationSnapshot(
            string ownerPackageId,
            string localCommandKey,
            string commandKey)
        {
            this.OwnerPackageId = ownerPackageId;
            this.LocalCommandKey = localCommandKey;
            this.CommandKey = commandKey;
        }

        public string OwnerPackageId { get; private set; }
        public string LocalCommandKey { get; private set; }
        public string CommandKey { get; private set; }
    }

    public sealed class ShuttleExternalPanelRegistrationSnapshot
    {
        public ShuttleExternalPanelRegistrationSnapshot(
            string ownerPackageId,
            string localPanelKey,
            string panelKey,
            string runtimeKey,
            bool disabledStatusKnown,
            bool disabled)
        {
            this.OwnerPackageId = ownerPackageId;
            this.LocalPanelKey = localPanelKey;
            this.PanelKey = panelKey;
            this.RuntimeKey = runtimeKey;
            this.DisabledStatusKnown = disabledStatusKnown;
            this.Disabled = disabled;
        }

        public string OwnerPackageId { get; private set; }
        public string LocalPanelKey { get; private set; }
        public string PanelKey { get; private set; }
        public string RuntimeKey { get; private set; }
        public bool DisabledStatusKnown { get; private set; }
        public bool Disabled { get; private set; }
    }

    public sealed class ShuttleExternalProfileContributorSnapshot
    {
        public ShuttleExternalProfileContributorSnapshot(
            string ownerPackageId,
            string localContributorKey,
            string contributorKey)
        {
            this.OwnerPackageId = ownerPackageId;
            this.LocalContributorKey = localContributorKey;
            this.ContributorKey = contributorKey;
        }

        public string OwnerPackageId { get; private set; }
        public string LocalContributorKey { get; private set; }
        public string ContributorKey { get; private set; }
    }
}

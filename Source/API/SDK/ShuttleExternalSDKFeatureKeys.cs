namespace CeleTech.ShuttleExtension.ModularShuttle.API.SDK
{
    /// <summary>
    /// Stable feature keys for external SDK discovery. Keys describe available SDK surfaces,
    /// not concrete implementation class names.
    /// </summary>
    public static class ShuttleExternalSDKFeatureKeys
    {
        public const string SDKDiscovery = "external.sdk.discovery";
        public const string IntegrationHealth = "external.integration.health";
        public const string ExternalRuntime = "external.runtime";
        public const string ExternalRuntimeState = "external.runtime.state";
        public const string ExternalRuntimeStateTryStore = "external.runtime.state.try_store";
        public const string ExternalRuntimeStateTryStoreReasons = "external.runtime.state.try_store.reasons";
        public const string ExternalRuntimeMigration = "external.runtime.migration";
        public const string ExternalRuntimeDiagnostics = "external.runtime.diagnostics";
        public const string ExternalCommands = "external.commands";
        public const string ExternalPanels = "external.panels";
        public const string ExternalProfileContributors = "external.profile_contributors";
        public const string ExternalProfileCapabilities = "external.profile.capabilities";
        public const string ExternalProfileMetrics = "external.profile.metrics";
        public const string ExternalProfileRequirements = "external.profile.requirements";
        public const string ExternalProfileContributionSources = "external.profile.contribution_sources";
        public const string ExternalCargoRead = "external.cargo.read";
        public const string ExternalCargoQuery = "external.cargo.query";
        public const string ExternalCargoTransaction = "external.cargo.transaction";
        public const string ExternalCargoConsume = "external.cargo.consume";
        public const string ExternalCargoConsumeQuote = "external.cargo.consume.quote";
        public const string ExternalCargoDeposit = "external.cargo.deposit";
        public const string ExternalCargoDepositQuote = "external.cargo.deposit.quote";
        public const string ExternalCargoTransactionDiagnostics = "external.cargo.transaction.diagnostics";
        public const string ExternalLaunchRead = "external.launch.read";
        public const string ExternalLaunchQuote = "external.launch.quote";
        public const string ExternalLaunchReadiness = "external.launch.readiness";
        public const string ExternalLaunchIssues = "external.launch.issues";
        public const string ExternalLaunchRules = "external.launch.rules";
        public const string ExternalLaunchRuleProvider = "external.launch.rule_provider";
        public const string ExternalLaunchOccupantHandoff = "external.launch.occupant_handoff";
        public const string ExternalLaunchOccupantHandoffQuote = "external.launch.occupant_handoff.quote";
        public const string ExternalReadSnapshots = "external.read_snapshots";
        public const string UnsafeLiveReferenceCompatibility = "external.unsafe_live_references_compat";
    }
}

using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKInfoProvider : IShuttleExternalSDKInfoProvider
    {
        private static readonly ExternalSDKInfoProvider instance = new ExternalSDKInfoProvider();

        private ExternalSDKInfoProvider()
        {
        }

        internal static ExternalSDKInfoProvider Instance
        {
            get
            {
                return instance;
            }
        }

        public ShuttleExternalSDKInfo GetSDKInfo()
        {
            List<string> features = new List<string>
            {
                ShuttleExternalSDKFeatureKeys.SDKDiscovery,
                ShuttleExternalSDKFeatureKeys.IntegrationHealth,
                ShuttleExternalSDKFeatureKeys.ExternalRuntime,
                ShuttleExternalSDKFeatureKeys.ExternalRuntimeState,
                ShuttleExternalSDKFeatureKeys.ExternalRuntimeStateTryStore,
                ShuttleExternalSDKFeatureKeys.ExternalRuntimeStateTryStoreReasons,
                ShuttleExternalSDKFeatureKeys.ExternalRuntimeMigration,
                ShuttleExternalSDKFeatureKeys.ExternalRuntimeDiagnostics,
                ShuttleExternalSDKFeatureKeys.ExternalCommands,
                ShuttleExternalSDKFeatureKeys.ExternalPanels,
                ShuttleExternalSDKFeatureKeys.ExternalProfileContributors,
                ShuttleExternalSDKFeatureKeys.ExternalProfileCapabilities,
                ShuttleExternalSDKFeatureKeys.ExternalProfileMetrics,
                ShuttleExternalSDKFeatureKeys.ExternalProfileRequirements,
                ShuttleExternalSDKFeatureKeys.ExternalProfileContributionSources,
                ShuttleExternalSDKFeatureKeys.ExternalCargoRead,
                ShuttleExternalSDKFeatureKeys.ExternalCargoQuery,
                ShuttleExternalSDKFeatureKeys.ExternalCargoTransaction,
                ShuttleExternalSDKFeatureKeys.ExternalCargoConsume,
                ShuttleExternalSDKFeatureKeys.ExternalCargoConsumeQuote,
                ShuttleExternalSDKFeatureKeys.ExternalCargoDeposit,
                ShuttleExternalSDKFeatureKeys.ExternalCargoDepositQuote,
                ShuttleExternalSDKFeatureKeys.ExternalCargoTransactionDiagnostics,
                ShuttleExternalSDKFeatureKeys.ExternalLaunchRead,
                ShuttleExternalSDKFeatureKeys.ExternalLaunchQuote,
                ShuttleExternalSDKFeatureKeys.ExternalLaunchReadiness,
                ShuttleExternalSDKFeatureKeys.ExternalLaunchIssues,
                ShuttleExternalSDKFeatureKeys.ExternalLaunchRules,
                ShuttleExternalSDKFeatureKeys.ExternalLaunchRuleProvider,
                ShuttleExternalSDKFeatureKeys.ExternalLaunchOccupantHandoff,
                ShuttleExternalSDKFeatureKeys.ExternalLaunchOccupantHandoffQuote,
                ShuttleExternalSDKFeatureKeys.ExternalReadSnapshots,
                ShuttleExternalSDKFeatureKeys.UnsafeLiveReferenceCompatibility
            };

            return new ShuttleExternalSDKInfo(
                ShuttleExternalSDK.APIVersion.ToString(),
                ShuttleModVersionUtility.ResolveCurrentModVersionOrUnknown(),
                "RimWorld 1.6 / Odyssey",
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                features);
        }

        public bool IsFeatureSupported(string featureKey)
        {
            return this.GetSDKInfo().IsFeatureSupported(featureKey);
        }
    }
}

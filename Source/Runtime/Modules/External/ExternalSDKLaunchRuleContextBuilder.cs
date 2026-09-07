using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchRuleContextBuilder
    {
        internal ShuttleExternalLaunchRuleContext Build(
            ShuttleExternalLaunchReadSnapshot baseSnapshot)
        {
            if (baseSnapshot == null)
            {
                return null;
            }

            return new ShuttleExternalLaunchRuleContext(
                ShuttleExternalSDK.APIVersion.ToString(),
                baseSnapshot.HostStableId,
                baseSnapshot.ProfileRevision,
                baseSnapshot.TicksGame,
                baseSnapshot.HostStateKind,
                baseSnapshot.Readiness,
                baseSnapshot.Payload,
                baseSnapshot.Cost,
                baseSnapshot.Cooldown,
                baseSnapshot.Issues,
                baseSnapshot.DestinationQuoteSupported,
                baseSnapshot.DestinationQuotePolicy);
        }
    }
}

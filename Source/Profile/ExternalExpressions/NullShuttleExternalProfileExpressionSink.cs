using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.ExternalExpressions
{
    internal sealed class NullShuttleExternalProfileExpressionSink :
        IShuttleExternalProfileExpressionSink
    {
        private static readonly NullShuttleExternalProfileExpressionSink instance =
            new NullShuttleExternalProfileExpressionSink();

        private NullShuttleExternalProfileExpressionSink()
        {
        }

        internal static NullShuttleExternalProfileExpressionSink Instance
        {
            get
            {
                return instance;
            }
        }

        public bool TryAddCapability(
            string ownerPackageId,
            string localCapabilityKey,
            string labelKey,
            string tooltipKey,
            ShuttleExternalProfileValueKind valueKind,
            float numericValue,
            bool booleanValue,
            string textValue,
            string unitKey,
            ShuttleExternalProfileAggregationMode aggregationMode,
            out string rejectionReason)
        {
            rejectionReason = "external profile expression sink is unavailable";
            return false;
        }

        public bool TryAddBooleanCapability(
            string ownerPackageId,
            string localCapabilityKey,
            string labelKey,
            string tooltipKey,
            bool value,
            ShuttleExternalProfileAggregationMode aggregationMode,
            out string rejectionReason)
        {
            rejectionReason = "external profile expression sink is unavailable";
            return false;
        }

        public bool TryAddTextCapability(
            string ownerPackageId,
            string localCapabilityKey,
            string labelKey,
            string tooltipKey,
            string value,
            string unitKey,
            out string rejectionReason)
        {
            rejectionReason = "external profile expression sink is unavailable";
            return false;
        }

        public bool TryAddMetric(
            string ownerPackageId,
            string localMetricKey,
            string labelKey,
            string tooltipKey,
            float numericValue,
            string unitKey,
            out string rejectionReason)
        {
            rejectionReason = "external profile expression sink is unavailable";
            return false;
        }

        public bool TryAddTextMetric(
            string ownerPackageId,
            string localMetricKey,
            string labelKey,
            string tooltipKey,
            string textValue,
            string unitKey,
            out string rejectionReason)
        {
            rejectionReason = "external profile expression sink is unavailable";
            return false;
        }

        public bool TryAddRequirement(
            string ownerPackageId,
            string localRequirementKey,
            ShuttleExternalProfileRequirementSeverity severity,
            string messageKey,
            string message,
            string tooltipKey,
            bool blocksLaunch,
            bool blocksInstallation,
            out string rejectionReason)
        {
            rejectionReason = "external profile expression sink is unavailable";
            return false;
        }
    }
}

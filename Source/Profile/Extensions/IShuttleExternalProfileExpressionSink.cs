using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions
{
    /// <summary>
    /// Optional profile-expression surface for third-party contributors.
    /// These declarations are data-only profile facts and do not grant mutation access or
    /// install, launch, cargo, UI, combat, or runtime service privileges.
    /// </summary>
    public interface IShuttleExternalProfileExpressionSink
    {
        bool TryAddCapability(
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
            out string rejectionReason);

        bool TryAddBooleanCapability(
            string ownerPackageId,
            string localCapabilityKey,
            string labelKey,
            string tooltipKey,
            bool value,
            ShuttleExternalProfileAggregationMode aggregationMode,
            out string rejectionReason);

        bool TryAddTextCapability(
            string ownerPackageId,
            string localCapabilityKey,
            string labelKey,
            string tooltipKey,
            string value,
            string unitKey,
            out string rejectionReason);

        bool TryAddMetric(
            string ownerPackageId,
            string localMetricKey,
            string labelKey,
            string tooltipKey,
            float numericValue,
            string unitKey,
            out string rejectionReason);

        bool TryAddTextMetric(
            string ownerPackageId,
            string localMetricKey,
            string labelKey,
            string tooltipKey,
            string textValue,
            string unitKey,
            out string rejectionReason);

        bool TryAddRequirement(
            string ownerPackageId,
            string localRequirementKey,
            ShuttleExternalProfileRequirementSeverity severity,
            string messageKey,
            string message,
            string tooltipKey,
            bool blocksLaunch,
            bool blocksInstallation,
            out string rejectionReason);
    }
}

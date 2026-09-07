using System;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.ExternalExpressions
{
    internal sealed class ShuttleExternalProfileExpressionSink :
        IShuttleExternalProfileExpressionSink
    {
        private readonly ShuttleExternalProfileExpressionCollector collector;
        private readonly IShuttleProfileIssueSink issueSink;
        private ShuttleExternalProfileExpressionSource currentSource;

        internal ShuttleExternalProfileExpressionSink(
            ShuttleExternalProfileExpressionCollector collector,
            IShuttleProfileIssueSink issueSink)
        {
            this.collector = collector;
            this.issueSink = issueSink ?? NullShuttleProfileIssueSink.Instance;
        }

        internal void SetCurrentSource(
            string contributorKey,
            bool builtIn,
            ShuttleModule module)
        {
            this.currentSource = ShuttleExternalProfileExpressionSource.From(
                contributorKey,
                builtIn,
                module);
        }

        internal void ClearCurrentSource()
        {
            this.currentSource = null;
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
            try
            {
                if (this.collector == null)
                {
                    rejectionReason = "external profile expression collector is unavailable";
                    return false;
                }

                return this.collector.TryAddCapability(
                    this.currentSource,
                    this.issueSink,
                    ownerPackageId,
                    localCapabilityKey,
                    labelKey,
                    tooltipKey,
                    valueKind,
                    numericValue,
                    booleanValue,
                    textValue,
                    unitKey,
                    aggregationMode,
                    out rejectionReason);
            }
            catch (Exception exception)
            {
                rejectionReason = "external profile capability failed: " + exception.GetType().Name;
                this.ReportSinkFailure(rejectionReason);
                return false;
            }
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
            return this.TryAddCapability(
                ownerPackageId,
                localCapabilityKey,
                labelKey,
                tooltipKey,
                ShuttleExternalProfileValueKind.Boolean,
                0f,
                value,
                null,
                null,
                aggregationMode,
                out rejectionReason);
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
            return this.TryAddCapability(
                ownerPackageId,
                localCapabilityKey,
                labelKey,
                tooltipKey,
                ShuttleExternalProfileValueKind.Text,
                0f,
                false,
                value,
                unitKey,
                ShuttleExternalProfileAggregationMode.None,
                out rejectionReason);
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
            try
            {
                if (this.collector == null)
                {
                    rejectionReason = "external profile expression collector is unavailable";
                    return false;
                }

                return this.collector.TryAddMetric(
                    this.currentSource,
                    this.issueSink,
                    ownerPackageId,
                    localMetricKey,
                    labelKey,
                    tooltipKey,
                    ShuttleExternalProfileValueKind.Numeric,
                    numericValue,
                    null,
                    unitKey,
                    out rejectionReason);
            }
            catch (Exception exception)
            {
                rejectionReason = "external profile metric failed: " + exception.GetType().Name;
                this.ReportSinkFailure(rejectionReason);
                return false;
            }
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
            try
            {
                if (this.collector == null)
                {
                    rejectionReason = "external profile expression collector is unavailable";
                    return false;
                }

                return this.collector.TryAddMetric(
                    this.currentSource,
                    this.issueSink,
                    ownerPackageId,
                    localMetricKey,
                    labelKey,
                    tooltipKey,
                    ShuttleExternalProfileValueKind.Text,
                    0f,
                    textValue,
                    unitKey,
                    out rejectionReason);
            }
            catch (Exception exception)
            {
                rejectionReason = "external profile text metric failed: " + exception.GetType().Name;
                this.ReportSinkFailure(rejectionReason);
                return false;
            }
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
            try
            {
                if (this.collector == null)
                {
                    rejectionReason = "external profile expression collector is unavailable";
                    return false;
                }

                return this.collector.TryAddRequirement(
                    this.currentSource,
                    this.issueSink,
                    ownerPackageId,
                    localRequirementKey,
                    severity,
                    messageKey,
                    message,
                    tooltipKey,
                    blocksLaunch,
                    blocksInstallation,
                    out rejectionReason);
            }
            catch (Exception exception)
            {
                rejectionReason = "external profile requirement failed: " + exception.GetType().Name;
                this.ReportSinkFailure(rejectionReason);
                return false;
            }
        }

        private void ReportSinkFailure(string message)
        {
            if (this.issueSink == null)
            {
                return;
            }

            this.issueSink.AddIssue(
                "external-profile-expression-sink-failed",
                message,
                ProfileBuildIssueSeverity.Warning,
                ProfileBuildIssueScope.Module,
                this.currentSource != null ? this.currentSource.ReferenceId : null);
        }
    }
}

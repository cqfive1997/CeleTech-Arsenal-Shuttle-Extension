using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.SDK
{
    public enum ShuttleExternalProfileValueKind
    {
        Numeric,
        Boolean,
        Text
    }

    public enum ShuttleExternalProfileRequirementSeverity
    {
        Info,
        Warning,
        Error,
        Blocker
    }

    public enum ShuttleExternalProfileAggregationMode
    {
        None,
        Sum,
        Max,
        Min,
        Any,
        All
    }

    public sealed class ShuttleExternalProfileContributionSourceSnapshot
    {
        public ShuttleExternalProfileContributionSourceSnapshot(
            string contributorKey,
            string moduleDefName,
            string moduleLabel,
            string moduleInstanceId,
            string referenceId,
            bool builtIn)
        {
            this.ContributorKey = contributorKey;
            this.ModuleDefName = moduleDefName;
            this.ModuleLabel = moduleLabel;
            this.ModuleInstanceId = moduleInstanceId;
            this.ReferenceId = referenceId;
            this.BuiltIn = builtIn;
        }

        public string ContributorKey { get; private set; }
        public string ModuleDefName { get; private set; }
        public string ModuleLabel { get; private set; }
        public string ModuleInstanceId { get; private set; }
        public string ReferenceId { get; private set; }
        public bool BuiltIn { get; private set; }
    }

    public sealed class ShuttleExternalProfileCapabilitySnapshot
    {
        public ShuttleExternalProfileCapabilitySnapshot(
            string capabilityKey,
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
            ShuttleExternalProfileContributionSourceSnapshot source)
        {
            this.CapabilityKey = capabilityKey;
            this.OwnerPackageId = ownerPackageId;
            this.LocalCapabilityKey = localCapabilityKey;
            this.LabelKey = labelKey;
            this.TooltipKey = tooltipKey;
            this.ValueKind = valueKind;
            this.NumericValue = numericValue;
            this.BooleanValue = booleanValue;
            this.TextValue = textValue;
            this.UnitKey = unitKey;
            this.AggregationMode = aggregationMode;
            this.Source = source;
        }

        public string CapabilityKey { get; private set; }
        public string OwnerPackageId { get; private set; }
        public string LocalCapabilityKey { get; private set; }
        public string LabelKey { get; private set; }
        public string TooltipKey { get; private set; }
        public ShuttleExternalProfileValueKind ValueKind { get; private set; }
        public float NumericValue { get; private set; }
        public bool BooleanValue { get; private set; }
        public string TextValue { get; private set; }
        public string UnitKey { get; private set; }
        public ShuttleExternalProfileAggregationMode AggregationMode { get; private set; }
        public ShuttleExternalProfileContributionSourceSnapshot Source { get; private set; }
    }

    public sealed class ShuttleExternalProfileMetricSnapshot
    {
        public ShuttleExternalProfileMetricSnapshot(
            string metricKey,
            string ownerPackageId,
            string localMetricKey,
            string labelKey,
            string tooltipKey,
            ShuttleExternalProfileValueKind valueKind,
            float numericValue,
            string textValue,
            string unitKey,
            ShuttleExternalProfileContributionSourceSnapshot source)
        {
            this.MetricKey = metricKey;
            this.OwnerPackageId = ownerPackageId;
            this.LocalMetricKey = localMetricKey;
            this.LabelKey = labelKey;
            this.TooltipKey = tooltipKey;
            this.ValueKind = valueKind;
            this.NumericValue = numericValue;
            this.TextValue = textValue;
            this.UnitKey = unitKey;
            this.Source = source;
        }

        public string MetricKey { get; private set; }
        public string OwnerPackageId { get; private set; }
        public string LocalMetricKey { get; private set; }
        public string LabelKey { get; private set; }
        public string TooltipKey { get; private set; }
        public ShuttleExternalProfileValueKind ValueKind { get; private set; }
        public float NumericValue { get; private set; }
        public string TextValue { get; private set; }
        public string UnitKey { get; private set; }
        public ShuttleExternalProfileContributionSourceSnapshot Source { get; private set; }
    }

    public sealed class ShuttleExternalProfileRequirementSnapshot
    {
        public ShuttleExternalProfileRequirementSnapshot(
            string requirementKey,
            string ownerPackageId,
            string localRequirementKey,
            ShuttleExternalProfileRequirementSeverity severity,
            string messageKey,
            string message,
            string tooltipKey,
            bool blocksLaunch,
            bool blocksInstallation,
            ShuttleExternalProfileContributionSourceSnapshot source)
        {
            this.RequirementKey = requirementKey;
            this.OwnerPackageId = ownerPackageId;
            this.LocalRequirementKey = localRequirementKey;
            this.Severity = severity;
            this.MessageKey = messageKey;
            this.Message = message;
            this.TooltipKey = tooltipKey;
            this.BlocksLaunch = blocksLaunch;
            this.BlocksInstallation = blocksInstallation;
            this.Source = source;
        }

        public string RequirementKey { get; private set; }
        public string OwnerPackageId { get; private set; }
        public string LocalRequirementKey { get; private set; }
        public ShuttleExternalProfileRequirementSeverity Severity { get; private set; }
        public string MessageKey { get; private set; }
        public string Message { get; private set; }
        public string TooltipKey { get; private set; }
        public bool BlocksLaunch { get; private set; }
        public bool BlocksInstallation { get; private set; }
        public ShuttleExternalProfileContributionSourceSnapshot Source { get; private set; }
    }

    public sealed class ShuttleExternalProfileExtensionSnapshot
    {
        public ShuttleExternalProfileExtensionSnapshot(
            IEnumerable<ShuttleExternalProfileCapabilitySnapshot> capabilities,
            IEnumerable<ShuttleExternalProfileMetricSnapshot> metrics,
            IEnumerable<ShuttleExternalProfileRequirementSnapshot> requirements)
        {
            this.Capabilities = ShuttleExternalSDKCollections.Copy(capabilities);
            this.Metrics = ShuttleExternalSDKCollections.Copy(metrics);
            this.Requirements = ShuttleExternalSDKCollections.Copy(requirements);
            this.ExternalCapabilityCount = this.Capabilities.Count;
            this.ExternalMetricCount = this.Metrics.Count;
            this.ExternalRequirementCount = this.Requirements.Count;
            this.ExternalRequirementWarningCount = CountRequirements(
                this.Requirements,
                ShuttleExternalProfileRequirementSeverity.Warning);
            this.ExternalRequirementErrorCount = CountRequirements(
                this.Requirements,
                ShuttleExternalProfileRequirementSeverity.Error);
            this.ExternalRequirementBlockerCount = CountRequirements(
                this.Requirements,
                ShuttleExternalProfileRequirementSeverity.Blocker);
        }

        public IReadOnlyList<ShuttleExternalProfileCapabilitySnapshot> Capabilities { get; private set; }
        public IReadOnlyList<ShuttleExternalProfileMetricSnapshot> Metrics { get; private set; }
        public IReadOnlyList<ShuttleExternalProfileRequirementSnapshot> Requirements { get; private set; }
        public int ExternalCapabilityCount { get; private set; }
        public int ExternalMetricCount { get; private set; }
        public int ExternalRequirementCount { get; private set; }
        public int ExternalRequirementWarningCount { get; private set; }
        public int ExternalRequirementErrorCount { get; private set; }
        public int ExternalRequirementBlockerCount { get; private set; }

        public static ShuttleExternalProfileExtensionSnapshot Empty()
        {
            return new ShuttleExternalProfileExtensionSnapshot(null, null, null);
        }

        private static int CountRequirements(
            IReadOnlyList<ShuttleExternalProfileRequirementSnapshot> requirements,
            ShuttleExternalProfileRequirementSeverity severity)
        {
            int count = 0;
            for (int i = 0; requirements != null && i < requirements.Count; i++)
            {
                ShuttleExternalProfileRequirementSnapshot requirement = requirements[i];
                if (requirement != null && requirement.Severity == severity)
                {
                    count++;
                }
            }

            return count;
        }
    }
}

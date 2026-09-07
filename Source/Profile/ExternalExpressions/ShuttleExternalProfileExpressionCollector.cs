using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.ExternalExpressions
{
    internal sealed class ShuttleExternalProfileExpressionCollector
    {
        private readonly List<ShuttleExternalProfileCapabilitySnapshot> capabilities =
            new List<ShuttleExternalProfileCapabilitySnapshot>();
        private readonly List<ShuttleExternalProfileMetricSnapshot> metrics =
            new List<ShuttleExternalProfileMetricSnapshot>();
        private readonly List<ShuttleExternalProfileRequirementSnapshot> requirements =
            new List<ShuttleExternalProfileRequirementSnapshot>();
        private readonly Dictionary<string, int> rowCountsBySource =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> nonNamespacedOwnerWarnings =
            new HashSet<string>(StringComparer.Ordinal);

        internal bool TryAddCapability(
            ShuttleExternalProfileExpressionSource source,
            IShuttleProfileIssueSink issueSink,
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
            rejectionReason = null;
            if (!this.CanAddRow(source, issueSink, "capability", out rejectionReason))
            {
                return false;
            }

            string owner = this.NormalizeRequired(
                ownerPackageId,
                ShuttleExternalProfileExpressionRules.MaxOwnerLength,
                "ownerPackageId",
                source,
                issueSink,
                out rejectionReason);
            if (owner == null)
            {
                return false;
            }

            string local = this.NormalizeRequired(
                localCapabilityKey,
                ShuttleExternalProfileExpressionRules.MaxLocalKeyLength,
                "localCapabilityKey",
                source,
                issueSink,
                out rejectionReason);
            if (local == null)
            {
                return false;
            }

            if (!this.ValidateValue(valueKind, numericValue, textValue, source, issueSink, out rejectionReason))
            {
                return false;
            }

            this.WarnIfOwnerNotNamespaced(owner, source, issueSink);
            this.capabilities.Add(new ShuttleExternalProfileCapabilitySnapshot(
                owner + "/" + local,
                owner,
                local,
                this.NormalizeOptional(labelKey, ShuttleExternalProfileExpressionRules.MaxLabelKeyLength, "labelKey", source, issueSink),
                this.NormalizeOptional(tooltipKey, ShuttleExternalProfileExpressionRules.MaxTooltipKeyLength, "tooltipKey", source, issueSink),
                valueKind,
                valueKind == ShuttleExternalProfileValueKind.Numeric ? numericValue : 0f,
                valueKind == ShuttleExternalProfileValueKind.Boolean && booleanValue,
                valueKind == ShuttleExternalProfileValueKind.Text
                    ? this.NormalizeOptional(textValue, ShuttleExternalProfileExpressionRules.MaxTextLength, "textValue", source, issueSink)
                    : null,
                this.NormalizeOptional(unitKey, ShuttleExternalProfileExpressionRules.MaxUnitKeyLength, "unitKey", source, issueSink),
                aggregationMode,
                this.ToSnapshot(source)));
            this.IncrementSourceCount(source);
            return true;
        }

        internal bool TryAddMetric(
            ShuttleExternalProfileExpressionSource source,
            IShuttleProfileIssueSink issueSink,
            string ownerPackageId,
            string localMetricKey,
            string labelKey,
            string tooltipKey,
            ShuttleExternalProfileValueKind valueKind,
            float numericValue,
            string textValue,
            string unitKey,
            out string rejectionReason)
        {
            rejectionReason = null;
            if (!this.CanAddRow(source, issueSink, "metric", out rejectionReason))
            {
                return false;
            }

            if (valueKind == ShuttleExternalProfileValueKind.Boolean)
            {
                rejectionReason = "metric value kind must be numeric or text";
                this.ReportRejected(source, issueSink, rejectionReason);
                return false;
            }

            string owner = this.NormalizeRequired(
                ownerPackageId,
                ShuttleExternalProfileExpressionRules.MaxOwnerLength,
                "ownerPackageId",
                source,
                issueSink,
                out rejectionReason);
            if (owner == null)
            {
                return false;
            }

            string local = this.NormalizeRequired(
                localMetricKey,
                ShuttleExternalProfileExpressionRules.MaxLocalKeyLength,
                "localMetricKey",
                source,
                issueSink,
                out rejectionReason);
            if (local == null)
            {
                return false;
            }

            if (!this.ValidateValue(valueKind, numericValue, textValue, source, issueSink, out rejectionReason))
            {
                return false;
            }

            this.WarnIfOwnerNotNamespaced(owner, source, issueSink);
            this.metrics.Add(new ShuttleExternalProfileMetricSnapshot(
                owner + "/" + local,
                owner,
                local,
                this.NormalizeOptional(labelKey, ShuttleExternalProfileExpressionRules.MaxLabelKeyLength, "labelKey", source, issueSink),
                this.NormalizeOptional(tooltipKey, ShuttleExternalProfileExpressionRules.MaxTooltipKeyLength, "tooltipKey", source, issueSink),
                valueKind,
                valueKind == ShuttleExternalProfileValueKind.Numeric ? numericValue : 0f,
                valueKind == ShuttleExternalProfileValueKind.Text
                    ? this.NormalizeOptional(textValue, ShuttleExternalProfileExpressionRules.MaxTextLength, "textValue", source, issueSink)
                    : null,
                this.NormalizeOptional(unitKey, ShuttleExternalProfileExpressionRules.MaxUnitKeyLength, "unitKey", source, issueSink),
                this.ToSnapshot(source)));
            this.IncrementSourceCount(source);
            return true;
        }

        internal bool TryAddRequirement(
            ShuttleExternalProfileExpressionSource source,
            IShuttleProfileIssueSink issueSink,
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
            rejectionReason = null;
            if (!this.CanAddRow(source, issueSink, "requirement", out rejectionReason))
            {
                return false;
            }

            string owner = this.NormalizeRequired(
                ownerPackageId,
                ShuttleExternalProfileExpressionRules.MaxOwnerLength,
                "ownerPackageId",
                source,
                issueSink,
                out rejectionReason);
            if (owner == null)
            {
                return false;
            }

            string local = this.NormalizeRequired(
                localRequirementKey,
                ShuttleExternalProfileExpressionRules.MaxLocalKeyLength,
                "localRequirementKey",
                source,
                issueSink,
                out rejectionReason);
            if (local == null)
            {
                return false;
            }

            this.WarnIfOwnerNotNamespaced(owner, source, issueSink);
            this.requirements.Add(new ShuttleExternalProfileRequirementSnapshot(
                owner + "/" + local,
                owner,
                local,
                severity,
                this.NormalizeOptional(messageKey, ShuttleExternalProfileExpressionRules.MaxLabelKeyLength, "messageKey", source, issueSink),
                this.NormalizeOptional(message, ShuttleExternalProfileExpressionRules.MaxMessageLength, "message", source, issueSink),
                this.NormalizeOptional(tooltipKey, ShuttleExternalProfileExpressionRules.MaxTooltipKeyLength, "tooltipKey", source, issueSink),
                blocksLaunch,
                blocksInstallation,
                this.ToSnapshot(source)));
            this.IncrementSourceCount(source);
            return true;
        }

        internal ShuttleExternalProfileExtensionSnapshot ToSnapshot()
        {
            return new ShuttleExternalProfileExtensionSnapshot(
                this.capabilities,
                this.metrics,
                this.requirements);
        }

        private bool CanAddRow(
            ShuttleExternalProfileExpressionSource source,
            IShuttleProfileIssueSink issueSink,
            string rowKind,
            out string rejectionReason)
        {
            rejectionReason = null;
            if (source == null)
            {
                rejectionReason = "profile expression source is unavailable";
                this.ReportRejected(null, issueSink, rejectionReason);
                return false;
            }

            int totalRows = this.capabilities.Count + this.metrics.Count + this.requirements.Count;
            if (totalRows >= ShuttleExternalProfileExpressionRules.MaxRowsPerProfile)
            {
                rejectionReason = "external profile expression profile row limit reached";
                this.ReportRejected(source, issueSink, rejectionReason);
                return false;
            }

            if (this.GetSourceCount(source) >= ShuttleExternalProfileExpressionRules.MaxRowsPerSource)
            {
                rejectionReason = "external profile expression source row limit reached for " + rowKind;
                this.ReportRejected(source, issueSink, rejectionReason);
                return false;
            }

            return true;
        }

        private string NormalizeRequired(
            string value,
            int maxLength,
            string fieldName,
            ShuttleExternalProfileExpressionSource source,
            IShuttleProfileIssueSink issueSink,
            out string rejectionReason)
        {
            string normalized = ShuttleExternalProfileExpressionRules.NormalizeRequiredKeyPart(
                value,
                maxLength,
                fieldName,
                out rejectionReason);
            if (normalized == null)
            {
                this.ReportRejected(source, issueSink, rejectionReason);
            }

            return normalized;
        }

        private string NormalizeOptional(
            string value,
            int maxLength,
            string fieldName,
            ShuttleExternalProfileExpressionSource source,
            IShuttleProfileIssueSink issueSink)
        {
            bool clamped;
            int actualLength;
            string normalized = ShuttleExternalProfileExpressionRules.NormalizeOptional(
                value,
                maxLength,
                out clamped,
                out actualLength);
            if (clamped)
            {
                this.ReportClamped(source, issueSink, fieldName, actualLength, maxLength);
            }

            return normalized;
        }

        private bool ValidateValue(
            ShuttleExternalProfileValueKind valueKind,
            float numericValue,
            string textValue,
            ShuttleExternalProfileExpressionSource source,
            IShuttleProfileIssueSink issueSink,
            out string rejectionReason)
        {
            if (ShuttleExternalProfileExpressionRules.TryValidateValue(
                valueKind,
                numericValue,
                textValue,
                out rejectionReason))
            {
                return true;
            }

            this.ReportRejected(source, issueSink, rejectionReason);
            return false;
        }

        private void WarnIfOwnerNotNamespaced(
            string ownerPackageId,
            ShuttleExternalProfileExpressionSource source,
            IShuttleProfileIssueSink issueSink)
        {
            if (ShuttleExternalProfileExpressionRules.IsOwnerNamespaced(ownerPackageId) ||
                !this.nonNamespacedOwnerWarnings.Add(ownerPackageId))
            {
                return;
            }

            this.ReportIssue(
                source,
                issueSink,
                "external-profile-expression-non-namespaced-owner",
                "External profile expression owner '" + ownerPackageId +
                    "' is not namespaced. Use package.id/local-key for third-party profile facts.");
        }

        private void IncrementSourceCount(ShuttleExternalProfileExpressionSource source)
        {
            string key = source != null ? source.CountKey : string.Empty;
            int count;
            if (!this.rowCountsBySource.TryGetValue(key, out count))
            {
                count = 0;
            }

            this.rowCountsBySource[key] = count + 1;
        }

        private int GetSourceCount(ShuttleExternalProfileExpressionSource source)
        {
            int count;
            return source != null && this.rowCountsBySource.TryGetValue(source.CountKey, out count)
                ? count
                : 0;
        }

        private ShuttleExternalProfileContributionSourceSnapshot ToSnapshot(
            ShuttleExternalProfileExpressionSource source)
        {
            return source != null ? source.ToSnapshot() : null;
        }

        private void ReportRejected(
            ShuttleExternalProfileExpressionSource source,
            IShuttleProfileIssueSink issueSink,
            string reason)
        {
            this.ReportIssue(
                source,
                issueSink,
                "external-profile-expression-rejected",
                "External profile expression rejected: " + reason + ".");
        }

        private void ReportClamped(
            ShuttleExternalProfileExpressionSource source,
            IShuttleProfileIssueSink issueSink,
            string fieldName,
            int actualLength,
            int maxLength)
        {
            this.ReportIssue(
                source,
                issueSink,
                "external-profile-expression-clamped",
                "External profile expression field '" + fieldName + "' length " +
                    actualLength + " was clamped to " + maxLength + ".");
        }

        private void ReportIssue(
            ShuttleExternalProfileExpressionSource source,
            IShuttleProfileIssueSink issueSink,
            string code,
            string message)
        {
            if (issueSink == null)
            {
                return;
            }

            issueSink.AddIssue(
                code,
                message,
                ProfileBuildIssueSeverity.Warning,
                ProfileBuildIssueScope.Module,
                source != null ? source.ReferenceId : null);
        }
    }
}

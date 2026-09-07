using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.ExternalExpressions
{
    internal static class ShuttleExternalProfileExpressionRules
    {
        internal const int MaxOwnerLength = 128;
        internal const int MaxLocalKeyLength = 96;
        internal const int MaxLabelKeyLength = 192;
        internal const int MaxTooltipKeyLength = 192;
        internal const int MaxUnitKeyLength = 64;
        internal const int MaxTextLength = 512;
        internal const int MaxMessageLength = 1024;
        internal const int MaxRowsPerSource = 64;
        internal const int MaxRowsPerProfile = 512;

        internal static string NormalizeRequiredKeyPart(
            string value,
            int maxLength,
            string fieldName,
            out string rejectionReason)
        {
            rejectionReason = null;
            string normalized = value != null ? value.Trim() : null;
            if (string.IsNullOrEmpty(normalized))
            {
                rejectionReason = fieldName + " is empty";
                return null;
            }

            if (normalized.IndexOf('/') >= 0)
            {
                rejectionReason = fieldName + " must not contain '/'";
                return null;
            }

            if (normalized.Length > maxLength)
            {
                rejectionReason = fieldName + " exceeds " + maxLength + " characters";
                return null;
            }

            return normalized;
        }

        internal static string NormalizeOptional(
            string value,
            int maxLength,
            out bool clamped,
            out int actualLength)
        {
            clamped = false;
            actualLength = 0;
            string normalized = value != null ? value.Trim() : null;
            if (string.IsNullOrEmpty(normalized))
            {
                return null;
            }

            actualLength = normalized.Length;
            if (normalized.Length <= maxLength)
            {
                return normalized;
            }

            clamped = true;
            return normalized.Substring(0, maxLength);
        }

        internal static bool TryValidateValue(
            ShuttleExternalProfileValueKind valueKind,
            float numericValue,
            string textValue,
            out string rejectionReason)
        {
            rejectionReason = null;
            if (valueKind == ShuttleExternalProfileValueKind.Numeric)
            {
                if (float.IsNaN(numericValue) || float.IsInfinity(numericValue))
                {
                    rejectionReason = "numeric value must be finite";
                    return false;
                }

                return true;
            }

            if (valueKind == ShuttleExternalProfileValueKind.Boolean)
            {
                return true;
            }

            if (valueKind == ShuttleExternalProfileValueKind.Text)
            {
                if (textValue != null && textValue.Length > MaxTextLength * 4)
                {
                    rejectionReason = "text value is too large";
                    return false;
                }

                return true;
            }

            rejectionReason = "unknown value kind";
            return false;
        }

        internal static bool IsOwnerNamespaced(string ownerPackageId)
        {
            return !string.IsNullOrEmpty(ownerPackageId) &&
                ownerPackageId.IndexOf('.') >= 0;
        }
    }
}

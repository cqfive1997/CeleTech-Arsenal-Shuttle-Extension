using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels
{
    internal sealed class ShuttleReadModelIssueMessageFormatter
    {
        private readonly ShuttleReadModelIssueDisplayNameResolver displayNameResolver =
            new ShuttleReadModelIssueDisplayNameResolver();
        private readonly ShuttleReadModelInternalTokenSanitizer tokenSanitizer =
            new ShuttleReadModelInternalTokenSanitizer();

        internal string FormatIssueMessage(
            ShuttleControlReadModel model,
            ShuttleControlIssueModel issue,
            string rawMessage)
        {
            string message = string.IsNullOrEmpty(rawMessage) ? "-" : rawMessage;
            message = this.ApplyDisplayNameReplacements(
                message,
                this.displayNameResolver.BuildReplacements(model));

            if (issue != null &&
                !string.IsNullOrEmpty(issue.ReferenceID) &&
                string.Equals(message, issue.ReferenceID, StringComparison.Ordinal))
            {
                string referenceDisplayName =
                    this.displayNameResolver.ResolveReferenceDisplayName(model, issue.ReferenceID);
                if (!string.IsNullOrEmpty(referenceDisplayName))
                {
                    message = referenceDisplayName;
                }
            }

            return this.tokenSanitizer.Sanitize(message);
        }

        internal string BuildIssueTooltip(
            ShuttleControlIssueModel issue,
            string rawMessage,
            string displayMessage)
        {
            if (!Prefs.DevMode || issue == null)
            {
                return null;
            }

            string tooltip = displayMessage;
            if (!string.IsNullOrEmpty(rawMessage) &&
                rawMessage != displayMessage)
            {
                tooltip += "\nRaw: " + rawMessage;
            }

            if (!string.IsNullOrEmpty(issue.Code))
            {
                tooltip += "\nCode: " + issue.Code;
            }

            if (!string.IsNullOrEmpty(issue.Scope))
            {
                tooltip += "\nScope: " + issue.Scope;
            }

            if (!string.IsNullOrEmpty(issue.ReferenceID))
            {
                tooltip += "\nReference: " + issue.ReferenceID;
            }

            return tooltip;
        }

        private string ApplyDisplayNameReplacements(
            string message,
            List<ShuttleReadModelDisplayNameReplacement> replacements)
        {
            if (replacements == null)
            {
                return message;
            }

            replacements.Sort(delegate(
                ShuttleReadModelDisplayNameReplacement left,
                ShuttleReadModelDisplayNameReplacement right)
            {
                int leftLength = left != null && left.RawID != null ? left.RawID.Length : 0;
                int rightLength = right != null && right.RawID != null ? right.RawID.Length : 0;
                return rightLength.CompareTo(leftLength);
            });

            for (int i = 0; i < replacements.Count; i++)
            {
                ShuttleReadModelDisplayNameReplacement replacement = replacements[i];
                if (replacement == null ||
                    string.IsNullOrEmpty(replacement.RawID) ||
                    string.IsNullOrEmpty(replacement.DisplayName))
                {
                    continue;
                }

                message = message.Replace(replacement.RawID, replacement.DisplayName);
                message = message.Replace(
                    replacement.RawID.ToLowerInvariant(),
                    replacement.DisplayName);
            }

            return message;
        }
    }
}

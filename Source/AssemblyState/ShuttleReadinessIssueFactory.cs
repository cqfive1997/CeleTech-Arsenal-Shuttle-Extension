using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    internal sealed class ShuttleReadinessIssueFactory
    {
        internal void AddIssue(
            List<ProfileBuildIssue> issues,
            string code,
            string message,
            ProfileBuildIssueSeverity severity,
            ProfileBuildIssueScope scope,
            string referenceID)
        {
            if (issues == null)
            {
                return;
            }

            issues.Add(new ProfileBuildIssue(code, message, severity, scope, referenceID));
        }

        internal void ValidateDuplicateIDReferences(
            Dictionary<string, List<string>> references,
            string code,
            string messageKey,
            string contextReference,
            ProfileBuildIssueScope scope,
            List<ProfileBuildIssue> issues)
        {
            if (references == null)
            {
                return;
            }

            foreach (KeyValuePair<string, List<string>> entry in references)
            {
                if (entry.Value == null || entry.Value.Count <= 1)
                {
                    continue;
                }

                string formattedReferences = this.FormatReferences(entry.Value);
                string message = !string.IsNullOrEmpty(contextReference)
                    ? messageKey.Translate(contextReference, entry.Key, formattedReferences).ToString()
                    : messageKey.Translate(entry.Key, formattedReferences).ToString();

                this.AddIssue(
                    issues,
                    code,
                    message,
                    ProfileBuildIssueSeverity.Error,
                    scope,
                    entry.Key);
            }
        }

        private string FormatReferences(List<string> references)
        {
            if (references == null || references.Count == 0)
            {
                return "no references";
            }

            string result = references[0];
            for (int i = 1; i < references.Count; i++)
            {
                result += ", " + references[i];
            }

            return result;
        }
    }
}

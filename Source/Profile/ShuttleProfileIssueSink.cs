using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile
{
    /// <summary>
    /// Adapts profile contributor diagnostics into the builder's issue list.
    /// </summary>
    internal sealed class ShuttleProfileIssueSink : IShuttleProfileIssueSink
    {
        private readonly List<ProfileBuildIssue> issues;

        internal ShuttleProfileIssueSink(List<ProfileBuildIssue> issues)
        {
            this.issues = issues;
        }

        public void AddIssue(
            string code,
            string message,
            ProfileBuildIssueSeverity severity,
            ProfileBuildIssueScope scope,
            string referenceID)
        {
            if (this.issues == null)
            {
                return;
            }

            this.issues.Add(new ProfileBuildIssue(code, message, severity, scope, referenceID));
        }
    }
}

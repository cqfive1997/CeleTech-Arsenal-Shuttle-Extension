namespace CeleTech.ShuttleExtension.ModularShuttle.Profile
{
    /// <summary>
    /// Structured diagnostic emitted while building a shuttle profile.
    /// It is intended for logging, read-model projection, and future UI/debug consumption.
    /// </summary>
    public sealed class ProfileBuildIssue
    {
        public ProfileBuildIssue(
            string code,
            string message,
            ProfileBuildIssueSeverity severity,
            ProfileBuildIssueScope scope,
            string referenceID)
        {
            this.Code = code;
            this.Message = message;
            this.Severity = severity;
            this.Scope = scope;
            this.ReferenceID = referenceID;
        }

        public string Code { get; private set; }

        public string Message { get; private set; }

        public ProfileBuildIssueSeverity Severity { get; private set; }

        public ProfileBuildIssueScope Scope { get; private set; }

        public string ReferenceID { get; private set; }
    }
}

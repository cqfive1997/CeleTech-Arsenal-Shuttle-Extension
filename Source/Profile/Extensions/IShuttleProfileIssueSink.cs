namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions
{
    /// <summary>
    /// Narrow diagnostic surface for contributors. It mirrors ProfileBuildIssue shape
    /// without allowing contributors to mutate profile-build internals directly.
    /// </summary>
    public interface IShuttleProfileIssueSink
    {
        void AddIssue(
            string code,
            string message,
            ProfileBuildIssueSeverity severity,
            ProfileBuildIssueScope scope,
            string referenceID);
    }

    /// <summary>
    /// No-op issue sink for contexts that are not wired to a profile builder adapter.
    /// </summary>
    public sealed class NullShuttleProfileIssueSink : IShuttleProfileIssueSink
    {
        private static readonly NullShuttleProfileIssueSink instance = new NullShuttleProfileIssueSink();

        private NullShuttleProfileIssueSink()
        {
        }

        public static NullShuttleProfileIssueSink Instance
        {
            get
            {
                return instance;
            }
        }

        public void AddIssue(
            string code,
            string message,
            ProfileBuildIssueSeverity severity,
            ProfileBuildIssueScope scope,
            string referenceID)
        {
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Contributors
{
    /// <summary>
    /// No-op built-in contributor for supported module categories that currently have no typed
    /// profile contribution.
    /// </summary>
    public sealed class NoProfileContributor : IShuttleProfileContributor
    {
        private static readonly NoProfileContributor instance = new NoProfileContributor();

        private NoProfileContributor()
        {
        }

        public static NoProfileContributor Instance
        {
            get
            {
                return instance;
            }
        }

        public void Contribute(ShuttleProfileContributionContext context)
        {
        }
    }
}

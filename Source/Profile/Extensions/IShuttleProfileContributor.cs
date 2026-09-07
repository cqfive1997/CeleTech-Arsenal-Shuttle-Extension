namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions
{
    /// <summary>
    /// Side-effect-free extension facet for declaring shuttle profile contributions.
    /// Contributors must not mutate AssemblyState or RuntimeState, install/remove modules,
    /// materialize slots, mutate cargo backends, or read/write Scribe data.
    /// Implementations should report only derived static capability values.
    /// </summary>
    public interface IShuttleProfileContributor
    {
        void Contribute(ShuttleProfileContributionContext context);
    }
}

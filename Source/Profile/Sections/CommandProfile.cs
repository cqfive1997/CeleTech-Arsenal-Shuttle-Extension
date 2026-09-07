namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections
{
    /// <summary>
    /// Static command/control capabilities derived from installed modules.
    /// Live launch timing remains in ShuttleRuntimeState.Launch.
    /// </summary>
    public sealed class CommandProfile
    {
        public CommandProfile(
            bool hasCockpit,
            bool stabilizesAdverseWeather,
            bool providesAutonomousLaunchControl = false,
            bool providesSecureSignalLink = false)
        {
            this.HasCockpit = hasCockpit;
            this.StabilizesAdverseWeather = stabilizesAdverseWeather;
            this.ProvidesAutonomousLaunchControl = providesAutonomousLaunchControl;
            this.ProvidesSecureSignalLink = providesSecureSignalLink;
        }

        public bool HasCockpit { get; private set; }

        public bool StabilizesAdverseWeather { get; private set; }

        public bool ProvidesAutonomousLaunchControl { get; private set; }

        public bool ProvidesSecureSignalLink { get; private set; }
    }
}

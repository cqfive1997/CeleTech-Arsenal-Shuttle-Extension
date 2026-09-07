namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections
{
    /// <summary>
    /// Minimal UI-facing layout summary derived from current assembly topology.
    /// This remains a profile section, not a UI state container.
    /// </summary>
    public sealed class UILayoutProfile
    {
        public UILayoutProfile(int segmentSlotCount, int moduleSlotCount)
        {
            this.SegmentSlotCount = segmentSlotCount;
            this.ModuleSlotCount = moduleSlotCount;
        }

        public int SegmentSlotCount { get; private set; }

        public int ModuleSlotCount { get; private set; }
    }
}

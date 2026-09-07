namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Canonical segment-side type values used by segment defs and shuttle-level segment slots.
    /// Optional is a slot-side wildcard. Unknown is reserved for bad or missing segment-def input.
    /// </summary>
    public enum ShuttleSegmentType
    {
        Unknown = 0,
        Optional = 1,
        Power = 2,
        Living = 3,
        Cargo = 4,
        Support = 5,
        Cockpit = 6,
        Weapon = 7
    }
}

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Canonical module-side type values used by module defs and segment-owned module slots.
    /// Optional is a slot-side wildcard. Unknown is reserved for bad or missing module-def input.
    /// </summary>
    public enum ShuttleModuleType
    {
        Unknown = 0,
        Optional = 1,
        Reactor = 2,
        Battery = 3,
        Cargo = 4,
        Cockpit = 5,
        Navigation = 6,
        Support = 7,
        Scanner = 8,
        Habitat = 9,
        PowerRegulator = 10,
        Shield = 11,
        Weapon = 12,
        MedicalBay = 13,
        Production = 14,
        MechCharger = 15,
        Armor = 16,
        FireControl = 17,
        PrisonCell = 18,
        AmmoLoader = 19
    }
}

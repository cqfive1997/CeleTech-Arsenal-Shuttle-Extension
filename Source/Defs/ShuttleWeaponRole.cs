namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Static authoring category for shuttle weapon modules. Runtime targeting and firing
    /// policy may use this later, but the enum itself does not imply any behavior.
    /// </summary>
    public enum ShuttleWeaponRole
    {
        Unknown = 0,
        // Close-range defensive weapons intended for projectile or small-target coverage.
        PointDefense = 1,
        // Forward/direct-fire weapons. This is a capability label, not a firing implementation.
        DirectFire = 2,
        // Launcher-style weapons with limited ready ammunition or reload slots.
        Rocket = 3,
        Missile = 4,
        Artillery = 5,
        // Medium close-in weapon systems such as 40mm CIWS cannons.
        CloseInWeapon = 6
    }
}

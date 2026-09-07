namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal enum ShuttleWeaponFireControlMode
    {
        // Completely offline. Later fire-control runtime work should block all firing.
        Offline = 0,

        // Manual mode. Later fire-control runtime work should allow forced targets only.
        ManualOnly = 1,

        // Automatic defense mode.
        AutoDefense = 2,

        // Point-defense mode for threats near the shuttle.
        PointDefense = 3
    }
}

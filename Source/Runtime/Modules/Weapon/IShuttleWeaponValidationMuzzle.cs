namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Internal geometry seam for a backend Verb that needs the shuttle-authored muzzle before
    /// its own CanHitTargetFrom evaluation. It does not launch, advance a barrel or mutate ammo.
    /// </summary>
    internal interface IShuttleWeaponValidationMuzzle
    {
        bool TrySetValidationMuzzle(ShuttleWeaponMuzzleSource source);
    }
}

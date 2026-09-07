namespace CeleTech.ShuttleExtension.ModularShuttle.Weapons
{
    /// <summary>
    /// Compatibility verb for existing rocket definitions. Rocket launch now uses the same
    /// runtime-injected shuttle muzzle source as other shuttle projectile weapons.
    /// </summary>
    public sealed class Verb_ShuttleRocketShoot : Verb_ShuttleMuzzleShoot
    {
    }
}

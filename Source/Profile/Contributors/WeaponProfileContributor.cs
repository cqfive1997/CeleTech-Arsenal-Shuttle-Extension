using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Contributors
{
    /// <summary>
    /// Built-in weapon profile contribution. This reports static installed capability only:
    /// live weapon cycle details, ammo remaining, and transient power demand stay runtime-owned.
    /// </summary>
    public sealed class WeaponProfileContributor : IShuttleProfileContributor
    {
        private static readonly WeaponProfileContributor instance = new WeaponProfileContributor();

        private WeaponProfileContributor()
        {
        }

        public static WeaponProfileContributor Instance
        {
            get
            {
                return instance;
            }
        }

        public void Contribute(ShuttleProfileContributionContext context)
        {
            if (context == null || context.Contributions == null)
            {
                return;
            }

            ShuttleWeaponModuleDef weaponDef = context.ModuleDef as ShuttleWeaponModuleDef;
            if (weaponDef == null)
            {
                return;
            }

            IShuttleWeaponProfileContributionSink weaponSink =
                context.Contributions as IShuttleWeaponProfileContributionSink;
            if (weaponSink == null)
            {
                return;
            }

            // Feed only Def-authored capability into the derived profile. Runtime state stays out.
            weaponSink.AddWeaponModule(
                weaponDef.weaponRole,
                weaponDef.mountCount,
                weaponDef.idlePowerDrawWatts,
                weaponDef.firingPowerDrawWatts,
                weaponDef.canAutoFire,
                weaponDef.canSetForcedTarget,
                weaponDef.requiresScanner,
                weaponDef.requiresNavigationComputer,
                weaponDef.maxAmmo);
        }
    }
}

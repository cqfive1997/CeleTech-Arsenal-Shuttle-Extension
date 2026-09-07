namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections
{
    /// <summary>
    /// Derived static weapon capability snapshot. It is rebuilt from AssemblyState and Def data,
    /// not saved, and does not contain weapon cycle or ammo-remaining runtime details.
    /// </summary>
    public sealed class WeaponProfile
    {
        public WeaponProfile(
            int totalWeaponModules,
            int totalWeaponMounts,
            int pointDefenseModules,
            int pointDefenseMounts,
            int closeInWeaponModules,
            int closeInWeaponMounts,
            int rocketLauncherModules,
            int rocketLauncherSlots,
            int autoFireCapableModules,
            int forcedTargetCapableModules,
            int scannerRequiredModules,
            int navigationRequiredModules,
            int totalAmmoCapacity,
            float totalStandbyPowerDrawWatts,
            float maxActiveFiringPowerDrawWatts)
        {
            this.TotalWeaponModules = Max(0, totalWeaponModules);
            this.TotalWeaponMounts = Max(0, totalWeaponMounts);
            this.PointDefenseModules = Max(0, pointDefenseModules);
            this.PointDefenseMounts = Max(0, pointDefenseMounts);
            this.CloseInWeaponModules = Max(0, closeInWeaponModules);
            this.CloseInWeaponMounts = Max(0, closeInWeaponMounts);
            this.RocketLauncherModules = Max(0, rocketLauncherModules);
            this.RocketLauncherSlots = Max(0, rocketLauncherSlots);
            this.AutoFireCapableModules = Max(0, autoFireCapableModules);
            this.ForcedTargetCapableModules = Max(0, forcedTargetCapableModules);
            this.ScannerRequiredModules = Max(0, scannerRequiredModules);
            this.NavigationRequiredModules = Max(0, navigationRequiredModules);
            this.TotalAmmoCapacity = Max(0, totalAmmoCapacity);
            this.TotalStandbyPowerDrawWatts = Max(0f, totalStandbyPowerDrawWatts);
            this.MaxActiveFiringPowerDrawWatts = Max(0f, maxActiveFiringPowerDrawWatts);
        }

        public int TotalWeaponModules { get; private set; }
        public int TotalWeaponMounts { get; private set; }
        public int PointDefenseModules { get; private set; }
        public int PointDefenseMounts { get; private set; }
        public int CloseInWeaponModules { get; private set; }
        public int CloseInWeaponMounts { get; private set; }
        public int RocketLauncherModules { get; private set; }
        public int RocketLauncherSlots { get; private set; }
        public int AutoFireCapableModules { get; private set; }
        public int ForcedTargetCapableModules { get; private set; }
        public int ScannerRequiredModules { get; private set; }
        public int NavigationRequiredModules { get; private set; }
        public int TotalAmmoCapacity { get; private set; }
        // Reporting-only standby draw for weapon modules. PowerProfile owns actual idle demand.
        public float TotalStandbyPowerDrawWatts { get; private set; }
        // Reporting-only maximum active draw. Runtime systems add transient demand when active.
        public float MaxActiveFiringPowerDrawWatts { get; private set; }

        public bool HasWeapons
        {
            get
            {
                return this.TotalWeaponModules > 0;
            }
        }

        public bool HasPointDefense
        {
            get
            {
                return this.PointDefenseModules > 0;
            }
        }

        public bool HasRocketLaunchers
        {
            get
            {
                return this.RocketLauncherModules > 0;
            }
        }

        public bool HasCloseInWeapons
        {
            get
            {
                return this.CloseInWeaponModules > 0;
            }
        }

        private static int Max(int a, int b)
        {
            return a > b ? a : b;
        }

        private static float Max(float a, float b)
        {
            return a > b ? a : b;
        }
    }
}

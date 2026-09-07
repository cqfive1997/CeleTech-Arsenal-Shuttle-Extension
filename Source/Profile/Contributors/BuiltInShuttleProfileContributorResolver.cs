using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Contributors
{
    /// <summary>
    /// Maps current core module types to built-in profile contributors. This is only the
    /// built-in fallback path; explicit contributor keys are resolved before this type is used.
    /// </summary>
    public static class BuiltInShuttleProfileContributorResolver
    {
        public static IShuttleProfileContributor ResolveOrNull(ShuttleModuleBaseDef moduleDef)
        {
            if (moduleDef == null)
            {
                return null;
            }

            if (moduleDef is ShuttleCargoLogisticsModuleDef)
            {
                return CargoLogisticsProfileContributor.Instance;
            }

            if (moduleDef is ShuttleHabitatModuleDef)
            {
                return HabitatProfileContributor.Instance;
            }

            if (moduleDef is ShuttleMedicalBayModuleDef)
            {
                return MedicalBayProfileContributor.Instance;
            }

            if (moduleDef is ShuttlePrisonCellModuleDef)
            {
                return PrisonCellProfileContributor.Instance;
            }

            if (moduleDef is ShuttleMechChargerModuleDef)
            {
                return MechChargerProfileContributor.Instance;
            }

            if (moduleDef is ShuttleHullPlatingModuleDef)
            {
                return HullProfileContributor.Instance;
            }

            if (moduleDef is ShuttleFireControlRadarModuleDef)
            {
                return FireControlProfileContributor.Instance;
            }

            if (moduleDef is ShuttleShieldModuleDef)
            {
                return ShieldProfileContributor.Instance;
            }

            return ResolveOrNull(moduleDef.ModuleType);
        }

        public static IShuttleProfileContributor ResolveOrNull(ShuttleModuleType moduleType)
        {
            switch (moduleType)
            {
                case ShuttleModuleType.Reactor:
                    return ReactorProfileContributor.Instance;
                case ShuttleModuleType.Battery:
                    return BatteryProfileContributor.Instance;
                case ShuttleModuleType.Cargo:
                    return CargoProfileContributor.Instance;
                case ShuttleModuleType.Cockpit:
                    return CockpitProfileContributor.Instance;
                case ShuttleModuleType.Navigation:
                    return NavigationComputerProfileContributor.Instance;
                case ShuttleModuleType.Support:
                    return DriveControlProfileContributor.Instance;
                case ShuttleModuleType.PowerRegulator:
                    return PowerRegulatorProfileContributor.Instance;
                case ShuttleModuleType.Scanner:
                case ShuttleModuleType.Production:
                case ShuttleModuleType.AmmoLoader:
                    return NoProfileContributor.Instance;
                case ShuttleModuleType.Shield:
                    return ShieldProfileContributor.Instance;
                case ShuttleModuleType.Habitat:
                    return HabitatProfileContributor.Instance;
                case ShuttleModuleType.Weapon:
                    return WeaponProfileContributor.Instance;
                case ShuttleModuleType.MedicalBay:
                    return MedicalBayProfileContributor.Instance;
                case ShuttleModuleType.PrisonCell:
                    return PrisonCellProfileContributor.Instance;
                case ShuttleModuleType.MechCharger:
                    return MechChargerProfileContributor.Instance;
                case ShuttleModuleType.Armor:
                    return HullProfileContributor.Instance;
                case ShuttleModuleType.FireControl:
                    return FireControlProfileContributor.Instance;
                default:
                    return null;
            }
        }
    }
}

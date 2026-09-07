using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Canonical module-side type IDs shared by module defs and module slots.
    /// Empty slot-side input falls back to Optional. Non-empty invalid input is exposed as
    /// Unknown so def validation can warn explicitly and install rules fail closed.
    /// </summary>
    public static class ShuttleModuleTypeCatalog
    {
        public const string Unknown = "unknown";
        public const string Optional = "optional";
        public const string Reactor = "reactor";
        public const string Battery = "battery";
        public const string Cargo = "cargo";
        public const string Cockpit = "cockpit";
        public const string Navigation = "navigation";
        public const string Support = "support";
        public const string Scanner = "scanner";
        public const string Habitat = "habitat";
        public const string PowerRegulator = "power-regulator";
        public const string Shield = "shield";
        public const string Weapon = "weapon";
        public const string MedicalBay = "medical-bay";
        public const string Production = "production";
        public const string MechCharger = "mech-charger";
        public const string Armor = "armor";
        public const string FireControl = "fire-control";
        public const string PrisonCell = "prison-cell";
        public const string AmmoLoader = "ammo-loader";

        public static string NormalizeModuleTypeID(string rawTypeID)
        {
            return ToTypeID(ParseModuleType(rawTypeID));
        }

        public static string NormalizeModuleSlotTypeID(string rawTypeID)
        {
            return ToTypeID(ParseSlotType(rawTypeID));
        }

        public static void NormalizeTypeList(List<string> typeIDs)
        {
            if (typeIDs == null)
            {
                return;
            }

            for (int i = 0; i < typeIDs.Count; i++)
            {
                typeIDs[i] = ToTypeID(ParseSlotType(typeIDs[i]));
            }
        }

        public static ShuttleModuleType ParseModuleType(string rawTypeID)
        {
            if (string.IsNullOrWhiteSpace(rawTypeID))
            {
                return ShuttleModuleType.Unknown;
            }

            string normalized = rawTypeID.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case Reactor:
                    return ShuttleModuleType.Reactor;
                case Battery:
                    return ShuttleModuleType.Battery;
                case Cargo:
                    return ShuttleModuleType.Cargo;
                case Cockpit:
                    return ShuttleModuleType.Cockpit;
                case Navigation:
                    return ShuttleModuleType.Navigation;
                case Support:
                    return ShuttleModuleType.Support;
                case Scanner:
                    return ShuttleModuleType.Scanner;
                case Habitat:
                    return ShuttleModuleType.Habitat;
                case PowerRegulator:
                    return ShuttleModuleType.PowerRegulator;
                case Shield:
                    return ShuttleModuleType.Shield;
                case Weapon:
                    return ShuttleModuleType.Weapon;
                case MedicalBay:
                    return ShuttleModuleType.MedicalBay;
                case Production:
                    return ShuttleModuleType.Production;
                case MechCharger:
                    return ShuttleModuleType.MechCharger;
                case Armor:
                    return ShuttleModuleType.Armor;
                case FireControl:
                    return ShuttleModuleType.FireControl;
                case PrisonCell:
                    return ShuttleModuleType.PrisonCell;
                case AmmoLoader:
                    return ShuttleModuleType.AmmoLoader;
                case Optional:
                    return ShuttleModuleType.Optional;
                case Unknown:
                    return ShuttleModuleType.Unknown;
                default:
                    return ShuttleModuleType.Unknown;
            }
        }

        public static ShuttleModuleType ParseSlotType(string rawTypeID)
        {
            if (string.IsNullOrWhiteSpace(rawTypeID))
            {
                return ShuttleModuleType.Optional;
            }

            string normalized = rawTypeID.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case Reactor:
                    return ShuttleModuleType.Reactor;
                case Battery:
                    return ShuttleModuleType.Battery;
                case Cargo:
                    return ShuttleModuleType.Cargo;
                case Cockpit:
                    return ShuttleModuleType.Cockpit;
                case Navigation:
                    return ShuttleModuleType.Navigation;
                case Support:
                    return ShuttleModuleType.Support;
                case Scanner:
                    return ShuttleModuleType.Scanner;
                case Habitat:
                    return ShuttleModuleType.Habitat;
                case PowerRegulator:
                    return ShuttleModuleType.PowerRegulator;
                case Shield:
                    return ShuttleModuleType.Shield;
                case Weapon:
                    return ShuttleModuleType.Weapon;
                case MedicalBay:
                    return ShuttleModuleType.MedicalBay;
                case Production:
                    return ShuttleModuleType.Production;
                case MechCharger:
                    return ShuttleModuleType.MechCharger;
                case Armor:
                    return ShuttleModuleType.Armor;
                case FireControl:
                    return ShuttleModuleType.FireControl;
                case PrisonCell:
                    return ShuttleModuleType.PrisonCell;
                case AmmoLoader:
                    return ShuttleModuleType.AmmoLoader;
                case Optional:
                    return ShuttleModuleType.Optional;
                default:
                    return ShuttleModuleType.Unknown;
            }
        }

        public static string ToTypeID(ShuttleModuleType type)
        {
            switch (type)
            {
                case ShuttleModuleType.Reactor:
                    return Reactor;
                case ShuttleModuleType.Battery:
                    return Battery;
                case ShuttleModuleType.Cargo:
                    return Cargo;
                case ShuttleModuleType.Cockpit:
                    return Cockpit;
                case ShuttleModuleType.Navigation:
                    return Navigation;
                case ShuttleModuleType.Support:
                    return Support;
                case ShuttleModuleType.Scanner:
                    return Scanner;
                case ShuttleModuleType.Habitat:
                    return Habitat;
                case ShuttleModuleType.PowerRegulator:
                    return PowerRegulator;
                case ShuttleModuleType.Shield:
                    return Shield;
                case ShuttleModuleType.Weapon:
                    return Weapon;
                case ShuttleModuleType.MedicalBay:
                    return MedicalBay;
                case ShuttleModuleType.Production:
                    return Production;
                case ShuttleModuleType.MechCharger:
                    return MechCharger;
                case ShuttleModuleType.Armor:
                    return Armor;
                case ShuttleModuleType.FireControl:
                    return FireControl;
                case ShuttleModuleType.PrisonCell:
                    return PrisonCell;
                case ShuttleModuleType.AmmoLoader:
                    return AmmoLoader;
                case ShuttleModuleType.Unknown:
                    return Unknown;
                default:
                    return Optional;
            }
        }

        public static string ToDisplayString(ShuttleModuleType type)
        {
            switch (type)
            {
                case ShuttleModuleType.Reactor:
                    return "CT_Shuttle_Type_Module_Reactor".Translate().ToString();
                case ShuttleModuleType.Battery:
                    return "CT_Shuttle_Type_Module_Battery".Translate().ToString();
                case ShuttleModuleType.Cargo:
                    return "CT_Shuttle_Type_Module_Cargo".Translate().ToString();
                case ShuttleModuleType.Cockpit:
                    return "CT_Shuttle_Type_Module_Cockpit".Translate().ToString();
                case ShuttleModuleType.Navigation:
                    return "CT_Shuttle_Type_Module_Navigation".Translate().ToString();
                case ShuttleModuleType.Support:
                    return "CT_Shuttle_Type_Module_Support".Translate().ToString();
                case ShuttleModuleType.Scanner:
                    return "CT_Shuttle_Type_Module_Scanner".Translate().ToString();
                case ShuttleModuleType.Habitat:
                    return "CT_Shuttle_Type_Module_Habitat".Translate().ToString();
                case ShuttleModuleType.PowerRegulator:
                    return "CT_Shuttle_Type_Module_PowerRegulator".Translate().ToString();
                case ShuttleModuleType.Shield:
                    return "CT_Shuttle_Type_Module_Shield".Translate().ToString();
                case ShuttleModuleType.Weapon:
                    return "CT_Shuttle_Type_Module_Weapon".Translate().ToString();
                case ShuttleModuleType.MedicalBay:
                    return "CT_Shuttle_Type_Module_MedicalBay".Translate().ToString();
                case ShuttleModuleType.Production:
                    return "CT_Shuttle_Type_Module_Production".Translate().ToString();
                case ShuttleModuleType.MechCharger:
                    return "CT_Shuttle_Type_Module_MechCharger".Translate().ToString();
                case ShuttleModuleType.Armor:
                    return "CT_Shuttle_Type_Module_Armor".Translate().ToString();
                case ShuttleModuleType.FireControl:
                    return "CT_Shuttle_Type_Module_FireControl".Translate().ToString();
                case ShuttleModuleType.PrisonCell:
                    return "CT_Shuttle_Type_Module_PrisonCell".Translate().ToString();
                case ShuttleModuleType.AmmoLoader:
                    return "CT_Shuttle_Type_Module_AmmoLoader".Translate().ToString();
                case ShuttleModuleType.Unknown:
                    return "CT_Shuttle_Type_Module_Unknown".Translate().ToString();
                default:
                    return "CT_Shuttle_Type_Module_Optional".Translate().ToString();
            }
        }

        public static bool AreCompatible(string leftTypeID, string rightTypeID)
        {
            return AreCompatible(ParseSlotType(leftTypeID), ParseSlotType(rightTypeID));
        }

        public static bool AreCompatible(ShuttleModuleType leftType, ShuttleModuleType rightType)
        {
            if (leftType == ShuttleModuleType.Unknown || rightType == ShuttleModuleType.Unknown)
            {
                return false;
            }

            if (leftType == ShuttleModuleType.Optional || rightType == ShuttleModuleType.Optional)
            {
                return true;
            }

            return leftType == rightType;
        }

        public static bool MatchesAnyInstallableType(ShuttleModuleType actualType, IReadOnlyList<ShuttleModuleType> installableTypes)
        {
            if (installableTypes == null || installableTypes.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < installableTypes.Count; i++)
            {
                if (AreCompatible(actualType, installableTypes[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Canonical segment-side type IDs shared by segment defs and shuttle-level segment slots.
    /// Empty slot-side input falls back to Optional. Non-empty invalid input is exposed as
    /// Unknown so def validation can warn explicitly and install rules fail closed.
    /// </summary>
    public static class ShuttleSegmentTypeCatalog
    {
        public const string Unknown = "unknown";
        public const string Power = "power";
        public const string Living = "living";
        public const string Cargo = "cargo";
        public const string Support = "support";
        public const string Cockpit = "cockpit";
        public const string Weapon = "weapon";
        public const string Optional = "optional";

        public static string NormalizeSegmentTypeID(string rawTypeID)
        {
            return ToTypeID(ParseSegmentType(rawTypeID));
        }

        public static string NormalizeSegmentSlotTypeID(string rawTypeID)
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

        public static ShuttleSegmentType ParseSegmentType(string rawTypeID)
        {
            if (string.IsNullOrWhiteSpace(rawTypeID))
            {
                return ShuttleSegmentType.Unknown;
            }

            string normalized = rawTypeID.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case Power:
                    return ShuttleSegmentType.Power;
                case Living:
                    return ShuttleSegmentType.Living;
                case Cargo:
                    return ShuttleSegmentType.Cargo;
                case Support:
                    return ShuttleSegmentType.Support;
                case Cockpit:
                    return ShuttleSegmentType.Cockpit;
                case Weapon:
                    return ShuttleSegmentType.Weapon;
                case Optional:
                    return ShuttleSegmentType.Optional;
                case Unknown:
                    return ShuttleSegmentType.Unknown;
                default:
                    return ShuttleSegmentType.Unknown;
            }
        }

        public static ShuttleSegmentType ParseSlotType(string rawTypeID)
        {
            if (string.IsNullOrWhiteSpace(rawTypeID))
            {
                return ShuttleSegmentType.Optional;
            }

            string normalized = rawTypeID.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case Power:
                    return ShuttleSegmentType.Power;
                case Living:
                    return ShuttleSegmentType.Living;
                case Cargo:
                    return ShuttleSegmentType.Cargo;
                case Support:
                    return ShuttleSegmentType.Support;
                case Cockpit:
                    return ShuttleSegmentType.Cockpit;
                case Weapon:
                    return ShuttleSegmentType.Weapon;
                case Optional:
                    return ShuttleSegmentType.Optional;
                default:
                    return ShuttleSegmentType.Unknown;
            }
        }

        public static string ToTypeID(ShuttleSegmentType type)
        {
            switch (type)
            {
                case ShuttleSegmentType.Power:
                    return Power;
                case ShuttleSegmentType.Living:
                    return Living;
                case ShuttleSegmentType.Cargo:
                    return Cargo;
                case ShuttleSegmentType.Support:
                    return Support;
                case ShuttleSegmentType.Cockpit:
                    return Cockpit;
                case ShuttleSegmentType.Weapon:
                    return Weapon;
                case ShuttleSegmentType.Unknown:
                    return Unknown;
                default:
                    return Optional;
            }
        }

        public static string ToDisplayString(ShuttleSegmentType type)
        {
            switch (type)
            {
                case ShuttleSegmentType.Power:
                    return "CT_Shuttle_Type_Segment_Power".Translate().ToString();
                case ShuttleSegmentType.Living:
                    return "CT_Shuttle_Type_Segment_Living".Translate().ToString();
                case ShuttleSegmentType.Cargo:
                    return "CT_Shuttle_Type_Segment_Cargo".Translate().ToString();
                case ShuttleSegmentType.Support:
                    return "CT_Shuttle_Type_Segment_Support".Translate().ToString();
                case ShuttleSegmentType.Cockpit:
                    return "CT_Shuttle_Type_Segment_Cockpit".Translate().ToString();
                case ShuttleSegmentType.Weapon:
                    return "CT_Shuttle_Type_Segment_Weapon".Translate().ToString();
                case ShuttleSegmentType.Unknown:
                    return "CT_Shuttle_Type_Segment_Unknown".Translate().ToString();
                default:
                    return "CT_Shuttle_Type_Segment_Optional".Translate().ToString();
            }
        }

        public static bool AreCompatible(string leftTypeID, string rightTypeID)
        {
            return AreCompatible(ParseSlotType(leftTypeID), ParseSlotType(rightTypeID));
        }

        public static bool AreCompatible(ShuttleSegmentType leftType, ShuttleSegmentType rightType)
        {
            if (leftType == ShuttleSegmentType.Unknown || rightType == ShuttleSegmentType.Unknown)
            {
                return false;
            }

            if (leftType == ShuttleSegmentType.Optional || rightType == ShuttleSegmentType.Optional)
            {
                return true;
            }

            return leftType == rightType;
        }

        public static bool MatchesAnyInstallableType(string actualTypeID, List<string> installableTypeIDs)
        {
            if (installableTypeIDs == null || installableTypeIDs.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < installableTypeIDs.Count; i++)
            {
                if (AreCompatible(actualTypeID, installableTypeIDs[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool MatchesAnyInstallableType(ShuttleSegmentType actualType, IReadOnlyList<ShuttleSegmentType> installableTypes)
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

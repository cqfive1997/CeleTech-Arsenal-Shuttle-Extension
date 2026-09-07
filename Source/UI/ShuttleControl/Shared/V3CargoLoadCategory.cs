using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal static class V3CargoLoadFilterIds
    {
        private static readonly string[] PassengerStandardOrder =
        {
            PassengerHumanlike,
            PassengerAnimals,
            PassengerMechanoids,
            PassengerOther
        };

        private static readonly string[] CargoStandardOrder =
        {
            CargoFood,
            CargoMedicineAndDrugs,
            CargoRawMaterials,
            CargoWeapons,
            CargoApparel,
            CargoBuildings,
            CargoManufactured,
            CargoCorpses,
            CargoOther
        };

        internal const string All = "all";

        internal const string PassengerHumanlike = "passenger:humanlike";
        internal const string PassengerAnimals = "passenger:animals";
        internal const string PassengerMechanoids = "passenger:mechanoids";
        internal const string PassengerOther = "passenger:other";

        internal const string CargoFood = "cargo:food";
        internal const string CargoMedicineAndDrugs = "cargo:medicine_drugs";
        internal const string CargoRawMaterials = "cargo:raw_materials";
        internal const string CargoWeapons = "cargo:weapons";
        internal const string CargoApparel = "cargo:apparel";
        internal const string CargoBuildings = "cargo:buildings";
        internal const string CargoManufactured = "cargo:manufactured";
        internal const string CargoCorpses = "cargo:corpses";
        internal const string CargoOther = "cargo:other";

        internal const string CustomPrefix = "cargo:custom:";

        internal static bool IsAll(string filterId)
        {
            return string.IsNullOrEmpty(filterId) || filterId == All;
        }

        internal static string[] GetStandardOrder(bool passengers)
        {
            return passengers ? PassengerStandardOrder : CargoStandardOrder;
        }

        internal static string ForCustomCategory(ThingCategoryDef category)
        {
            return category == null || string.IsNullOrEmpty(category.defName)
                ? null
                : CustomPrefix + category.defName;
        }

        internal static string GetCustomCategoryDefName(string filterId)
        {
            return !string.IsNullOrEmpty(filterId) &&
                filterId.StartsWith(CustomPrefix, System.StringComparison.Ordinal)
                    ? filterId.Substring(CustomPrefix.Length)
                    : null;
        }
    }

    internal sealed class V3CargoLoadCategoryMatch
    {
        internal V3CargoLoadCategoryMatch(
            string standardFilterId,
            ThingCategoryDef customCategory)
        {
            this.StandardFilterId = standardFilterId;
            this.CustomCategory = customCategory;
        }

        internal string StandardFilterId { get; private set; }

        internal ThingCategoryDef CustomCategory { get; private set; }
    }

    internal sealed class V3CargoLoadFilterOption
    {
        internal V3CargoLoadFilterOption(string id, string label, int count)
        {
            this.Id = id;
            this.Label = label;
            this.Count = count;
        }

        internal string Id { get; private set; }

        internal string Label { get; private set; }

        internal int Count { get; private set; }
    }

    internal sealed class V3CargoLoadBatchCategory
    {
        internal V3CargoLoadBatchCategory(
            string filterId,
            string label,
            System.Collections.Generic.List<TransferableOneWay> targets,
            long availableCount)
        {
            this.FilterId = filterId;
            this.Label = label;
            this.Targets = targets ??
                new System.Collections.Generic.List<TransferableOneWay>();
            this.AvailableCount = availableCount > 0L ? availableCount : 0L;
        }

        internal string FilterId { get; private set; }

        internal string Label { get; private set; }

        internal System.Collections.Generic.List<TransferableOneWay> Targets { get; private set; }

        internal long AvailableCount { get; private set; }
    }
}

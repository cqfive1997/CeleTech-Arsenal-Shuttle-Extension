using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoLoadCategoryResolver
    {
        private static readonly string[] MedicineCategoryDefNames =
        {
            "Medicine",
            "Drugs"
        };

        private static readonly string[] RawMaterialCategoryDefNames =
        {
            "ResourcesRaw",
            "PlantMatter",
            "StoneBlocks",
            "Textiles",
            "Leathers",
            "Wools",
            "Chunks",
            "StoneChunks",
            "Plants"
        };

        private static readonly string[] ManufacturedCategoryDefNames =
        {
            "Manufactured",
            "ResourcesManufactured",
            "BodyParts",
            "Artifacts"
        };

        private readonly Dictionary<TransferableOneWay, V3CargoLoadCategoryMatch>
            passengerCache =
                new Dictionary<TransferableOneWay, V3CargoLoadCategoryMatch>();
        private readonly Dictionary<TransferableOneWay, V3CargoLoadCategoryMatch>
            cargoCache =
                new Dictionary<TransferableOneWay, V3CargoLoadCategoryMatch>();
        private readonly Dictionary<Thing, V3CargoLoadCategoryMatch> thingCache =
            new Dictionary<Thing, V3CargoLoadCategoryMatch>();
        private readonly Dictionary<string, string> customFilterLabels =
            new Dictionary<string, string>(StringComparer.Ordinal);

        internal V3CargoLoadCategoryMatch Resolve(
            TransferableOneWay transferable,
            bool passengers)
        {
            Dictionary<TransferableOneWay, V3CargoLoadCategoryMatch> cache =
                passengers ? this.passengerCache : this.cargoCache;
            V3CargoLoadCategoryMatch match;
            if (transferable != null && cache.TryGetValue(transferable, out match))
            {
                return match;
            }

            match = passengers
                ? this.ResolvePassenger(transferable)
                : this.ResolveCargo(transferable);
            if (transferable != null)
            {
                cache[transferable] = match;
            }

            return match;
        }

        internal bool Matches(
            TransferableOneWay transferable,
            bool passengers,
            string filterId)
        {
            if (V3CargoLoadFilterIds.IsAll(filterId))
            {
                return true;
            }

            V3CargoLoadCategoryMatch match = this.Resolve(transferable, passengers);
            if (match == null)
            {
                return false;
            }

            if (string.Equals(
                    match.StandardFilterId,
                    filterId,
                    StringComparison.Ordinal))
            {
                return true;
            }

            string customDefName = V3CargoLoadFilterIds.GetCustomCategoryDefName(filterId);
            return !string.IsNullOrEmpty(customDefName) &&
                match.CustomCategory != null &&
                string.Equals(
                    match.CustomCategory.defName,
                    customDefName,
                    StringComparison.Ordinal);
        }

        internal V3CargoLoadCategoryMatch ResolveCargoThing(Thing thing)
        {
            V3CargoLoadCategoryMatch match;
            if (thing != null && this.thingCache.TryGetValue(thing, out match))
            {
                return match;
            }

            match = this.ResolveCargoThingCore(thing);
            if (thing != null)
            {
                this.thingCache[thing] = match;
            }

            return match;
        }

        internal bool MatchesCargoThing(Thing thing, string filterId)
        {
            if (V3CargoLoadFilterIds.IsAll(filterId))
            {
                return true;
            }

            V3CargoLoadCategoryMatch match = this.ResolveCargoThing(thing);
            if (match == null)
            {
                return false;
            }

            if (string.Equals(match.StandardFilterId, filterId, StringComparison.Ordinal))
            {
                return true;
            }

            string customDefName = V3CargoLoadFilterIds.GetCustomCategoryDefName(filterId);
            return !string.IsNullOrEmpty(customDefName) &&
                match.CustomCategory != null &&
                string.Equals(match.CustomCategory.defName, customDefName, StringComparison.Ordinal);
        }

        internal string GetFilterLabel(string filterId)
        {
            string key = GetStandardLabelKey(filterId);
            if (!string.IsNullOrEmpty(key))
            {
                return V3CargoLoadText.Tr(key);
            }

            string cachedLabel;
            if (!string.IsNullOrEmpty(filterId) &&
                this.customFilterLabels.TryGetValue(filterId, out cachedLabel))
            {
                return cachedLabel;
            }

            string customDefName = V3CargoLoadFilterIds.GetCustomCategoryDefName(filterId);
            ThingCategoryDef category = !string.IsNullOrEmpty(customDefName)
                ? DefDatabase<ThingCategoryDef>.GetNamedSilentFail(customDefName)
                : null;
            return category != null
                ? category.LabelCap.ToString()
                : V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_FilterOther");
        }

        internal void Clear()
        {
            this.passengerCache.Clear();
            this.cargoCache.Clear();
            this.thingCache.Clear();
            this.customFilterLabels.Clear();
        }

        internal static string GetStandardLabelKey(string filterId)
        {
            switch (filterId)
            {
                case V3CargoLoadFilterIds.PassengerHumanlike:
                    return "CT_Shuttle_LoadCargo_FilterHumanlike";
                case V3CargoLoadFilterIds.PassengerAnimals:
                    return "CT_Shuttle_LoadCargo_FilterAnimals";
                case V3CargoLoadFilterIds.PassengerMechanoids:
                    return "CT_Shuttle_LoadCargo_FilterMechanoids";
                case V3CargoLoadFilterIds.PassengerOther:
                case V3CargoLoadFilterIds.CargoOther:
                    return "CT_Shuttle_LoadCargo_FilterOther";
                case V3CargoLoadFilterIds.CargoFood:
                    return "CT_Shuttle_LoadCargo_FilterFood";
                case V3CargoLoadFilterIds.CargoMedicineAndDrugs:
                    return "CT_Shuttle_LoadCargo_FilterMedicineDrugs";
                case V3CargoLoadFilterIds.CargoRawMaterials:
                    return "CT_Shuttle_LoadCargo_FilterRawMaterials";
                case V3CargoLoadFilterIds.CargoWeapons:
                    return "CT_Shuttle_LoadCargo_FilterWeapons";
                case V3CargoLoadFilterIds.CargoApparel:
                    return "CT_Shuttle_LoadCargo_FilterApparel";
                case V3CargoLoadFilterIds.CargoBuildings:
                    return "CT_Shuttle_LoadCargo_FilterBuildings";
                case V3CargoLoadFilterIds.CargoManufactured:
                    return "CT_Shuttle_LoadCargo_FilterManufactured";
                case V3CargoLoadFilterIds.CargoCorpses:
                    return "CT_Shuttle_LoadCargo_FilterCorpses";
                case V3CargoLoadFilterIds.All:
                    return "CT_Shuttle_LoadCargo_FilterAll";
                default:
                    return V3CargoLoadFilterIds.IsAll(filterId)
                        ? "CT_Shuttle_LoadCargo_FilterAll"
                        : null;
            }
        }

        private V3CargoLoadCategoryMatch ResolvePassenger(TransferableOneWay transferable)
        {
            Pawn pawn = FirstThing(transferable) as Pawn;
            string filterId = V3CargoLoadFilterIds.PassengerOther;
            if (pawn != null && pawn.RaceProps != null)
            {
                if (pawn.RaceProps.IsMechanoid)
                {
                    filterId = V3CargoLoadFilterIds.PassengerMechanoids;
                }
                else if (pawn.RaceProps.Humanlike)
                {
                    filterId = V3CargoLoadFilterIds.PassengerHumanlike;
                }
                else if (pawn.RaceProps.Animal)
                {
                    filterId = V3CargoLoadFilterIds.PassengerAnimals;
                }
            }

            return new V3CargoLoadCategoryMatch(filterId, null);
        }

        private V3CargoLoadCategoryMatch ResolveCargo(TransferableOneWay transferable)
        {
            return this.ResolveCargoThingCore(FirstThing(transferable));
        }

        private V3CargoLoadCategoryMatch ResolveCargoThingCore(Thing thing)
        {
            ThingDef def = thing != null ? thing.def : null;
            string filterId;
            if (thing is Corpse || HasThingCategoryDefOrParent(def, "Corpses"))
            {
                filterId = V3CargoLoadFilterIds.CargoCorpses;
            }
            else if (IsMedicineOrDrug(def))
            {
                filterId = V3CargoLoadFilterIds.CargoMedicineAndDrugs;
            }
            else if (IsWeapon(def))
            {
                filterId = V3CargoLoadFilterIds.CargoWeapons;
            }
            else if (IsApparel(def))
            {
                filterId = V3CargoLoadFilterIds.CargoApparel;
            }
            else if (IsFood(def))
            {
                filterId = V3CargoLoadFilterIds.CargoFood;
            }
            else if (IsRawMaterial(def))
            {
                filterId = V3CargoLoadFilterIds.CargoRawMaterials;
            }
            else if (IsBuilding(def, thing))
            {
                filterId = V3CargoLoadFilterIds.CargoBuildings;
            }
            else if (HasThingCategoryDefOrParent(def, ManufacturedCategoryDefNames))
            {
                filterId = V3CargoLoadFilterIds.CargoManufactured;
            }
            else
            {
                filterId = V3CargoLoadFilterIds.CargoOther;
            }

            ThingCategoryDef customCategory =
                filterId == V3CargoLoadFilterIds.CargoOther
                    ? ResolveResidualCategory(def)
                    : null;
            string customFilterId = V3CargoLoadFilterIds.ForCustomCategory(customCategory);
            if (!string.IsNullOrEmpty(customFilterId) &&
                !this.customFilterLabels.ContainsKey(customFilterId))
            {
                this.customFilterLabels.Add(
                    customFilterId,
                    customCategory.LabelCap.ToString());
            }

            return new V3CargoLoadCategoryMatch(filterId, customCategory);
        }

        private static Thing FirstThing(TransferableOneWay transferable)
        {
            if (transferable == null || transferable.things == null)
            {
                return null;
            }

            for (int i = 0; i < transferable.things.Count; i++)
            {
                if (transferable.things[i] != null)
                {
                    return transferable.things[i];
                }
            }

            return null;
        }

        private static bool IsFood(ThingDef def)
        {
            return HasThingCategoryDefOrParent(def, "Foods") ||
                (def != null &&
                    def.ingestible != null &&
                    def.ingestible.CachedNutrition > 0f);
        }

        private static bool IsMedicineOrDrug(ThingDef def)
        {
            return def != null &&
                (def.IsMedicine ||
                    def.IsDrug ||
                    HasThingCategoryDefOrParent(def, MedicineCategoryDefNames));
        }

        private static bool IsRawMaterial(ThingDef def)
        {
            return def != null &&
                (def.stuffProps != null ||
                    def.category == ThingCategory.Plant ||
                    HasThingCategoryDefOrParent(def, RawMaterialCategoryDefNames));
        }

        private static bool IsWeapon(ThingDef def)
        {
            return HasThingCategoryDefOrParent(def, "Weapons") ||
                (def != null &&
                    (def.equipmentType != EquipmentType.None ||
                        def.IsRangedWeapon ||
                        def.IsMeleeWeapon ||
                        (def.weaponTags != null && def.weaponTags.Count > 0)));
        }

        private static bool IsApparel(ThingDef def)
        {
            return def != null &&
                (def.apparel != null || HasThingCategoryDefOrParent(def, "Apparel"));
        }

        private static bool IsBuilding(ThingDef def, Thing thing)
        {
            return thing is MinifiedThing ||
                (def != null &&
                    (def.category == ThingCategory.Building ||
                        HasThingCategoryDefOrParent(def, "Buildings")));
        }

        private static bool HasThingCategoryDefOrParent(
            ThingDef def,
            string[] categoryDefNames)
        {
            if (categoryDefNames == null)
            {
                return false;
            }

            for (int i = 0; i < categoryDefNames.Length; i++)
            {
                if (HasThingCategoryDefOrParent(def, categoryDefNames[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasThingCategoryDefOrParent(
            ThingDef def,
            string categoryDefName)
        {
            if (def == null ||
                def.thingCategories == null ||
                string.IsNullOrEmpty(categoryDefName))
            {
                return false;
            }

            for (int i = 0; i < def.thingCategories.Count; i++)
            {
                ThingCategoryDef category = def.thingCategories[i];
                int depthGuard = 0;
                while (category != null && depthGuard < 32)
                {
                    if (string.Equals(
                            category.defName,
                            categoryDefName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    category = category.parent;
                    depthGuard++;
                }
            }

            return false;
        }

        private static ThingCategoryDef ResolveResidualCategory(ThingDef def)
        {
            if (def == null || def.thingCategories == null)
            {
                return null;
            }

            for (int i = 0; i < def.thingCategories.Count; i++)
            {
                ThingCategoryDef category = def.thingCategories[i];
                ThingCategoryDef highestSpecificCategory = null;
                int depthGuard = 0;
                while (category != null && depthGuard < 32)
                {
                    if (string.Equals(category.defName, "Root", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(category.defName, "Items", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    highestSpecificCategory = category;
                    category = category.parent;
                    depthGuard++;
                }

                if (highestSpecificCategory != null)
                {
                    return highestSpecificCategory;
                }
            }

            return null;
        }
    }
}

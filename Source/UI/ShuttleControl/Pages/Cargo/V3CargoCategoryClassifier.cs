using System;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoCategoryClassifier
    {
        private static readonly string[] ForceRawResourceDefNames =
        {
            "WoodLog",
            "Steel",
            "Plasteel",
            "Uranium",
            "Jade",
            "Gold",
            "Silver",
            "Chemfuel",
            "Cloth",
            "Synthread",
            "Hyperweave",
            "DevilstrandCloth"
        };

        private static readonly string[] ForceManufacturedDefNames =
        {
            "ComponentIndustrial",
            "ComponentSpacer",
            "MedicineHerbal",
            "MedicineIndustrial",
            "MedicineUltratech",
            "PowerCore"
        };

        private static readonly string[] RawResourceCategoryDefNames =
        {
            "ResourcesRaw",
            "PlantMatter",
            "StoneBlocks",
            "Leathers",
            "Wools",
            "Textiles"
        };

        private static readonly string[] ManufacturedCategoryDefNames =
        {
            "ResourcesManufactured",
            "Manufactured",
            "Medicine",
            "Drugs",
            "Artifacts"
        };

        internal V3CargoCategory Classify(Thing thing, string defName, bool isPawn)
        {
            ThingDef def = this.ResolveThingDef(thing, defName);

            if (thing is Corpse || this.IsCorpseCargo(def, defName))
            {
                return V3CargoCategory.Corpses;
            }

            if (isPawn || thing is Pawn)
            {
                return V3CargoCategory.Items;
            }

            if (this.IsFoodDef(def))
            {
                return V3CargoCategory.Food;
            }

            if (this.IsApparelDef(def))
            {
                return V3CargoCategory.Apparel;
            }

            if (this.IsForceRawResourceDefName(def != null ? def.defName : null) ||
                this.IsForceRawResourceDefName(defName))
            {
                return V3CargoCategory.RawResources;
            }

            if (def != null && def.equipmentType != EquipmentType.None)
            {
                return V3CargoCategory.Weapons;
            }

            if (this.IsBuildingDef(def, thing))
            {
                return V3CargoCategory.Buildings;
            }

            if (this.IsChunkDef(def, defName))
            {
                return V3CargoCategory.Chunks;
            }

            if (this.IsRawResource(def, defName))
            {
                return V3CargoCategory.RawResources;
            }

            if (this.IsTrueWeapon(def))
            {
                return V3CargoCategory.Weapons;
            }

            if (this.IsManufactured(def, defName))
            {
                return V3CargoCategory.Manufactured;
            }

            if (this.IsPlantDef(def))
            {
                return V3CargoCategory.Plants;
            }

            return V3CargoCategory.Items;
        }

        private ThingDef ResolveThingDef(Thing thing, string defName)
        {
            if (thing != null && thing.def != null)
            {
                return thing.def;
            }

            return !string.IsNullOrEmpty(defName)
                ? DefDatabase<ThingDef>.GetNamedSilentFail(defName)
                : null;
        }

        private bool IsCorpseCargo(ThingDef def, string defName)
        {
            return this.HasThingCategoryDefOrParent(def, "Corpses") ||
                this.DefNameContains(defName, "corpse") ||
                this.DefNameContains(def != null ? def.defName : null, "corpse");
        }

        private bool IsFoodDef(ThingDef def)
        {
            return def != null &&
                def.ingestible != null &&
                def.ingestible.CachedNutrition > 0f;
        }

        private bool IsApparelDef(ThingDef def)
        {
            return def != null &&
                (def.apparel != null || this.HasThingCategoryDefOrParent(def, "Apparel"));
        }

        private bool IsBuildingDef(ThingDef def, Thing thing)
        {
            return thing is MinifiedThing ||
                (def != null && def.category == ThingCategory.Building);
        }

        private bool IsChunkDef(ThingDef def, string defName)
        {
            return this.HasThingCategoryDefOrParent(def, "Chunks") ||
                this.HasThingCategoryDefOrParent(def, "StoneChunks") ||
                this.DefNameContains(defName, "chunk") ||
                this.DefNameContains(def != null ? def.defName : null, "chunk");
        }

        private bool IsRawResource(ThingDef def, string defName)
        {
            return this.HasThingCategoryDefOrParent(def, RawResourceCategoryDefNames) ||
                this.IsForceRawResourceDefName(def != null ? def.defName : null) ||
                this.IsForceRawResourceDefName(defName) ||
                this.DefNameContains(defName, "stoneblock") ||
                this.DefNameContains(defName, "leather") ||
                this.DefNameContains(defName, "wool") ||
                this.IsOreLikeRawResourceDefName(def != null ? def.defName : null) ||
                this.IsOreLikeRawResourceDefName(defName);
        }

        private bool IsTrueWeapon(ThingDef def)
        {
            return def != null &&
                (def.IsRangedWeapon ||
                    def.IsMeleeWeapon ||
                    (def.weaponTags != null && def.weaponTags.Count > 0));
        }

        private bool IsManufactured(ThingDef def, string defName)
        {
            return this.HasThingCategoryDefOrParent(def, ManufacturedCategoryDefNames) ||
                this.IsKnownManufacturedDefName(def != null ? def.defName : null) ||
                this.IsKnownManufacturedDefName(defName);
        }

        private bool IsPlantDef(ThingDef def)
        {
            return def != null &&
                (def.category == ThingCategory.Plant ||
                    def.plant != null ||
                    this.HasThingCategoryDefOrParent(def, "Plants"));
        }

        private bool IsForceRawResourceDefName(string defName)
        {
            return this.DefNameEqualsAny(defName, ForceRawResourceDefNames);
        }

        private bool IsOreLikeRawResourceDefName(string defName)
        {
            if (string.IsNullOrEmpty(defName))
            {
                return false;
            }

            return string.Equals(defName, "Ore", StringComparison.OrdinalIgnoreCase) ||
                defName.StartsWith("Ore_", StringComparison.OrdinalIgnoreCase) ||
                defName.StartsWith("Ore-", StringComparison.OrdinalIgnoreCase) ||
                defName.StartsWith("Ore.", StringComparison.OrdinalIgnoreCase) ||
                defName.StartsWith("RawOre", StringComparison.OrdinalIgnoreCase) ||
                defName.IndexOf("_Ore_", StringComparison.OrdinalIgnoreCase) >= 0 ||
                defName.IndexOf("-Ore-", StringComparison.OrdinalIgnoreCase) >= 0 ||
                defName.EndsWith("_Ore", StringComparison.OrdinalIgnoreCase) ||
                defName.EndsWith("-Ore", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsKnownManufacturedDefName(string defName)
        {
            return this.DefNameEqualsAny(defName, ForceManufacturedDefNames) ||
                this.DefNameContains(defName, "component") ||
                this.DefNameContains(defName, "medicine") ||
                this.DefNameContains(defName, "drug") ||
                this.DefNameContains(defName, "artifact") ||
                this.DefNameContains(defName, "advanced");
        }

        private bool HasThingCategoryDefOrParent(ThingDef def, string categoryDefName)
        {
            if (def == null || def.thingCategories == null)
            {
                return false;
            }

            for (int i = 0; i < def.thingCategories.Count; i++)
            {
                if (this.CategoryOrParentsMatch(def.thingCategories[i], categoryDefName))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasThingCategoryDefOrParent(ThingDef def, string[] categoryDefNames)
        {
            if (categoryDefNames == null)
            {
                return false;
            }

            for (int i = 0; i < categoryDefNames.Length; i++)
            {
                if (this.HasThingCategoryDefOrParent(def, categoryDefNames[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CategoryOrParentsMatch(ThingCategoryDef category, string categoryDefName)
        {
            int depthGuard = 0;
            while (category != null && depthGuard < 32)
            {
                if (string.Equals(category.defName, categoryDefName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                category = category.parent;
                depthGuard++;
            }

            return false;
        }

        private bool DefNameEqualsAny(string defName, string[] candidates)
        {
            if (string.IsNullOrEmpty(defName) || candidates == null)
            {
                return false;
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                if (string.Equals(defName, candidates[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool DefNameContains(string value, string fragment)
        {
            return !string.IsNullOrEmpty(value) &&
                !string.IsNullOrEmpty(fragment) &&
                value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

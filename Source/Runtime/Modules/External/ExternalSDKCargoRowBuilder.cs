using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKCargoRowBuilder
    {
        internal ShuttleExternalCargoItemRowSnapshot BuildRegularRow(
            ShuttleCargoItemSnapshot item)
        {
            if (item == null)
            {
                return null;
            }

            Thing display = item.DisplayThing;
            bool blocked = item.CargoRegionIndex < 0;
            ShuttleExternalCargoLocationKind location = blocked
                ? ShuttleExternalCargoLocationKind.Blocked
                : item.IsLoaded
                    ? ShuttleExternalCargoLocationKind.Loaded
                    : item.IsAssignedToLoad
                        ? ShuttleExternalCargoLocationKind.AssignedToLoad
                        : ShuttleExternalCargoLocationKind.Unknown;
            int stackCount = item.StackCount > 0 ? item.StackCount : 1;
            return this.BuildRow(
                this.BuildRegularRowId(item, location),
                location,
                item.DefName,
                item.Label,
                item.Category,
                display,
                stackCount,
                item.Mass,
                false,
                item.IsAssignedToLoad,
                blocked,
                blocked ? "no active cargo region accepts this row" : null,
                item.IsLoaded ? "loaded cargo" : "assigned cargo");
        }

        internal ShuttleExternalCargoItemRowSnapshot BuildRefrigeratedRow(
            ShuttleRefrigeratedCargoModuleSnapshot module,
            ShuttleRefrigeratedCargoItemSnapshot item)
        {
            if (item == null)
            {
                return null;
            }

            Thing display = item.DisplayThing;
            int stackCount = item.StackCount > 0 ? item.StackCount : 1;
            return this.BuildRow(
                "refrigerated:" + (item.ModuleInstanceID ?? string.Empty) + ":" +
                    item.ColdIndex + ":" + item.ThingIDNumber,
                ShuttleExternalCargoLocationKind.Refrigerated,
                item.DefName,
                item.Label,
                null,
                display,
                stackCount,
                item.Mass,
                true,
                false,
                false,
                null,
                module != null && !string.IsNullOrEmpty(module.Label)
                    ? module.Label
                    : "refrigerated cargo");
        }

        private ShuttleExternalCargoItemRowSnapshot BuildRow(
            string rowId,
            ShuttleExternalCargoLocationKind location,
            string itemDefName,
            string itemLabel,
            string fallbackCategory,
            Thing display,
            int stackCount,
            float massKg,
            bool refrigerated,
            bool assigned,
            bool blocked,
            string blockedReason,
            string sourceLabel)
        {
            string resolvedDefName = !string.IsNullOrEmpty(itemDefName)
                ? itemDefName
                : display != null && display.def != null
                    ? display.def.defName
                    : null;
            string resolvedLabel = !string.IsNullOrEmpty(itemLabel)
                ? itemLabel
                : display != null
                    ? CargoDisplayUtility.GetDisplayLabel(display)
                    : resolvedDefName;
            float unitMass = stackCount > 0 ? massKg / stackCount : 0f;
            if (unitMass < 0f)
            {
                unitMass = 0f;
            }

            return new ShuttleExternalCargoItemRowSnapshot(
                rowId,
                location,
                resolvedDefName,
                resolvedLabel,
                this.GetStuffDefName(display),
                this.GetStuffLabel(display),
                this.GetCategoryDefName(display, fallbackCategory),
                this.GetQuality(display),
                stackCount,
                massKg > 0f ? massKg : 0f,
                unitMass,
                this.GetNutrition(display),
                this.GetMarketValue(display),
                display is Corpse,
                this.IsCreatureCargo(display),
                this.IsPerishable(display),
                refrigerated,
                assigned,
                blocked,
                blockedReason,
                sourceLabel);
        }

        private string BuildRegularRowId(
            ShuttleCargoItemSnapshot item,
            ShuttleExternalCargoLocationKind location)
        {
            if (item == null)
            {
                return "cargo:unknown";
            }

            return "cargo:" + location.ToString() + ":" +
                item.TransporterIndex + ":" +
                item.LoadedIndex + ":" +
                item.QueueIndex + ":" +
                item.ThingIDNumber;
        }

        private string GetStuffDefName(Thing display)
        {
            return display != null && display.Stuff != null
                ? display.Stuff.defName
                : null;
        }

        private string GetStuffLabel(Thing display)
        {
            return display != null && display.Stuff != null
                ? display.Stuff.LabelCap.ToString()
                : null;
        }

        private string GetCategoryDefName(Thing display, string fallbackCategory)
        {
            if (display != null &&
                display.def != null &&
                display.def.thingCategories != null &&
                display.def.thingCategories.Count > 0 &&
                display.def.thingCategories[0] != null)
            {
                return display.def.thingCategories[0].defName;
            }

            if (display != null && display.def != null)
            {
                return display.def.category.ToString();
            }

            return fallbackCategory;
        }

        private string GetQuality(Thing display)
        {
            ThingWithComps thingWithComps = display as ThingWithComps;
            if (thingWithComps == null)
            {
                return null;
            }

            CompQuality quality = thingWithComps.GetComp<CompQuality>();
            return quality != null ? quality.Quality.ToString() : null;
        }

        private float GetNutrition(Thing display)
        {
            if (display == null)
            {
                return 0f;
            }

            try
            {
                return display.GetStatValue(StatDefOf.Nutrition);
            }
            catch
            {
                return 0f;
            }
        }

        private float GetMarketValue(Thing display)
        {
            if (display == null)
            {
                return 0f;
            }

            try
            {
                return display.MarketValue;
            }
            catch
            {
                return 0f;
            }
        }

        private bool IsCreatureCargo(Thing display)
        {
            if (display is Pawn)
            {
                return true;
            }

            Corpse corpse = display as Corpse;
            return corpse != null && corpse.InnerPawn != null;
        }

        private bool IsPerishable(Thing display)
        {
            return display != null && display.TryGetComp<CompRottable>() != null;
        }
    }
}

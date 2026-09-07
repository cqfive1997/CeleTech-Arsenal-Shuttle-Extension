using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoStackModelBuilder
    {
        private readonly V3CargoCategoryClassifier categoryClassifier =
            new V3CargoCategoryClassifier();

        internal V3CargoStackCardModel BuildLoadedStack(ShuttleCargoItemSnapshot item)
        {
            if (item == null)
            {
                return null;
            }

            V3CargoStackCardModel stack = this.BuildStack(
                item.Label,
                item.DefName,
                item.StackCount,
                item.Mass,
                item.DisplayThing,
                item.IsPawn);
            stack.SourceKind = V3CargoStackSourceKind.LoadedCargo;
            stack.TransporterIndex = item.TransporterIndex;
            stack.LoadedIndex = item.LoadedIndex;
            stack.ThingIDNumber = item.ThingIDNumber;
            stack.Members.Add(new V3CargoStackMemberModel
            {
                SourceKind = V3CargoStackSourceKind.LoadedCargo,
                DefName = item.DefName,
                StackCount = Mathf.Max(1, item.StackCount),
                TransporterIndex = item.TransporterIndex,
                LoadedIndex = item.LoadedIndex,
                ThingIDNumber = item.ThingIDNumber
            });
            return stack;
        }

        internal V3CargoStackCardModel BuildRefrigeratedStack(
            ShuttleRefrigeratedCargoItemSnapshot item)
        {
            if (item == null)
            {
                return null;
            }

            V3CargoStackCardModel stack = this.BuildStack(
                item.Label,
                item.DefName,
                item.StackCount,
                item.Mass,
                item.DisplayThing,
                false);
            stack.SourceKind = V3CargoStackSourceKind.RefrigeratedCargo;
            stack.ModuleInstanceID = item.ModuleInstanceID;
            stack.ColdIndex = item.ColdIndex;
            stack.ThingIDNumber = item.ThingIDNumber;
            stack.Members.Add(new V3CargoStackMemberModel
            {
                SourceKind = V3CargoStackSourceKind.RefrigeratedCargo,
                DefName = item.DefName,
                StackCount = Mathf.Max(1, item.StackCount),
                ModuleInstanceID = item.ModuleInstanceID,
                ColdIndex = item.ColdIndex,
                ThingIDNumber = item.ThingIDNumber
            });
            return stack;
        }

        private V3CargoStackCardModel BuildStack(
            string label,
            string defName,
            int stackCount,
            float massKg,
            Thing displayThing,
            bool isPawn)
        {
            V3CargoStackCardModel stack = new V3CargoStackCardModel();
            stack.Label = !string.IsNullOrEmpty(label)
                ? label
                : this.GetFallbackThingLabel(defName);
            stack.DefName = defName;
            stack.StackCount = Mathf.Max(1, stackCount);
            stack.MassKg = Mathf.Max(0f, massKg);
            stack.DisplayThing = displayThing;
            stack.Category = this.categoryClassifier.Classify(displayThing, defName, isPawn);
            this.ApplyVanillaDefaultSortKeys(stack, displayThing, defName);
            return stack;
        }

        private void ApplyVanillaDefaultSortKeys(
            V3CargoStackCardModel stack,
            Thing displayThing,
            string defName)
        {
            ThingDef thingDef = displayThing != null
                ? displayThing.def
                : DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (stack == null || thingDef == null)
            {
                return;
            }

            stack.VanillaSortThingCategory = (int)thingDef.category;
            stack.VanillaSortListPriority =
                TransferableUIUtility.DefaultListOrderPriority(thingDef);
            stack.VanillaSortCategoryIndex = thingDef.thingCategories != null &&
                thingDef.thingCategories.Count > 0 &&
                thingDef.thingCategories[0] != null
                    ? thingDef.thingCategories[0].index
                    : 0;
            stack.VanillaSortMarketValue = displayThing != null
                ? displayThing.MarketValue
                : thingDef.BaseMarketValue;
        }

        private string GetFallbackThingLabel(string defName)
        {
            return !string.IsNullOrEmpty(defName)
                ? defName
                : ShuttleUIText.Tr("CT_Shuttle_Cargo_Unknown");
        }
    }
}

using System;
using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    /// <summary>
    /// Sorts detached cargo cards with keys equivalent to RimWorld's default transporter list.
    /// All potentially expensive keys are computed once by the stack model builder.
    /// </summary>
    internal sealed class V3CargoVanillaDefaultSorter
    {
        internal void Sort(List<V3CargoStackCardModel> items)
        {
            if (items != null && items.Count > 1)
            {
                items.Sort(Compare);
            }
        }

        private static int Compare(
            V3CargoStackCardModel left,
            V3CargoStackCardModel right)
        {
            if (object.ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            int result = left.VanillaSortThingCategory.CompareTo(
                right.VanillaSortThingCategory);
            if (result != 0)
            {
                return result;
            }

            result = left.VanillaSortListPriority.CompareTo(
                right.VanillaSortListPriority);
            if (result != 0)
            {
                return result;
            }

            result = left.VanillaSortCategoryIndex.CompareTo(
                right.VanillaSortCategoryIndex);
            if (result != 0)
            {
                return result;
            }

            result = left.VanillaSortMarketValue.CompareTo(
                right.VanillaSortMarketValue);
            if (result != 0)
            {
                return result;
            }

            result = string.Compare(
                left.Label,
                right.Label,
                StringComparison.OrdinalIgnoreCase);
            if (result != 0)
            {
                return result;
            }

            result = left.SourceKind.CompareTo(right.SourceKind);
            if (result != 0)
            {
                return result;
            }

            result = left.TransporterIndex.CompareTo(right.TransporterIndex);
            if (result != 0)
            {
                return result;
            }

            result = left.LoadedIndex.CompareTo(right.LoadedIndex);
            return result != 0
                ? result
                : left.ThingIDNumber.CompareTo(right.ThingIDNumber);
        }
    }
}

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Food
{
    internal enum ShuttleCargoFoodSelectionMode
    {
        LowestWithinRange,
        PreferredDefsThenHighest
    }

    /// <summary>
    /// Data-only food selection policy. Habitat and Prison Cell keep their policy ownership;
    /// Cargo uses this copy only while selecting an already-loaded real food Thing.
    /// </summary>
    internal sealed class ShuttleCargoFoodSelectionPolicy
    {
        internal ShuttleCargoFoodSelectionPolicy(
            FoodPreferability minimumPreferability,
            FoodPreferability maximumPreferability,
            bool allowRefrigerated,
            ShuttleCargoFoodSelectionMode mode,
            IReadOnlyList<ThingDef> preferredFoodDefs)
        {
            this.MinimumPreferability = minimumPreferability;
            this.MaximumPreferability = maximumPreferability;
            this.AllowRefrigerated = allowRefrigerated;
            this.Mode = mode;
            this.PreferredFoodDefs = preferredFoodDefs;
        }

        internal FoodPreferability MinimumPreferability { get; private set; }

        internal FoodPreferability MaximumPreferability { get; private set; }

        internal bool AllowRefrigerated { get; private set; }

        internal ShuttleCargoFoodSelectionMode Mode { get; private set; }

        internal IReadOnlyList<ThingDef> PreferredFoodDefs { get; private set; }

        internal static ShuttleCargoFoodSelectionPolicy LowestAtOrBelowMaximum(
            FoodPreferability maximumPreferability,
            bool allowRefrigerated)
        {
            return new ShuttleCargoFoodSelectionPolicy(
                FoodPreferability.MealAwful,
                maximumPreferability,
                allowRefrigerated,
                ShuttleCargoFoodSelectionMode.LowestWithinRange,
                null);
        }

        internal static ShuttleCargoFoodSelectionPolicy Habitat(
            IReadOnlyList<ThingDef> preferredFoodDefs)
        {
            return new ShuttleCargoFoodSelectionPolicy(
                FoodPreferability.Undefined,
                FoodPreferability.MealLavish,
                true,
                ShuttleCargoFoodSelectionMode.PreferredDefsThenHighest,
                preferredFoodDefs);
        }
    }
}

using System;
using CeleTech.ShuttleExtension.ModularShuttle.Core;

namespace CeleTech.ShuttleExtension.ModularShuttle.Flight
{
    /// <summary>
    /// Converts the player-facing payload-capacity multiplier into the effective
    /// mass-related launch energy costs used by profile and launch calculations.
    /// </summary>
    internal static class ShuttlePayloadCapacityPolicy
    {
        internal static float ResolveEffectiveMassDistanceEnergyPerKgTileWd(
            ShuttleProfile profile)
        {
            float authoredCost = profile != null && profile.Flight != null
                ? profile.Flight.EnergyPerKgTileWd
                : 0f;
            return ApplyToAuthoredEnergyCost(authoredCost);
        }

        internal static float ResolveEffectivePayloadLiftEnergyPerKgWd(
            ShuttleProfile profile)
        {
            float authoredCost = profile != null && profile.Flight != null
                ? profile.Flight.PayloadLiftEnergyPerKgWd
                : 0f;
            return ApplyToAuthoredEnergyCost(authoredCost);
        }

        internal static float ApplyToAuthoredEnergyCost(float authoredCost)
        {
            if (float.IsNaN(authoredCost) || authoredCost <= 0f)
            {
                return 0f;
            }

            if (float.IsInfinity(authoredCost))
            {
                return float.MaxValue;
            }

            double effectiveCost = authoredCost / ResolveCapacityMultiplier();
            if (double.IsNaN(effectiveCost) || effectiveCost <= 0d)
            {
                return 0f;
            }

            return double.IsInfinity(effectiveCost) || effectiveCost > float.MaxValue
                ? float.MaxValue
                : (float)effectiveCost;
        }

        internal static float ResolveCapacityMultiplier()
        {
            ShuttleOtherSettings other = CeleTechShuttleMod.Settings != null
                ? CeleTechShuttleMod.Settings.Other
                : null;
            float multiplier = other != null
                ? other.PayloadCapacityMultiplier
                : ShuttleOtherSettings.DefaultPayloadCapacityMultiplier;
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier))
            {
                multiplier = ShuttleOtherSettings.DefaultPayloadCapacityMultiplier;
            }

            return Math.Max(
                ShuttleOtherSettings.MinimumPayloadCapacityMultiplier,
                Math.Min(
                    ShuttleOtherSettings.MaximumPayloadCapacityMultiplier,
                    multiplier));
        }
    }
}

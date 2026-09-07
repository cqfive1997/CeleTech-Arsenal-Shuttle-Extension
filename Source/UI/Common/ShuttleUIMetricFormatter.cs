using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common
{
    internal static class ShuttleUIMetricFormatter
    {
        internal static string FormatPercent(float value01)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(value01) * 100f).ToString() + "%";
        }

        internal static string FormatKg(float kg)
        {
            return kg.ToString("0.#") + " kg";
        }

        internal static string FormatKgCompact(float kg)
        {
            return kg.ToString("0.#") + "kg";
        }

        internal static string FormatKgPair(float currentKg, float maxKg)
        {
            if (maxKg > 0f)
            {
                return FormatKgCompact(currentKg) + " / " + FormatKgCompact(maxKg);
            }

            return FormatKgCompact(currentKg);
        }

        internal static string FormatWattsPair(float currentWatts, float maxWatts)
        {
            if (maxWatts > 0f)
            {
                return FormatWatts(currentWatts) + " / " + FormatWatts(maxWatts);
            }

            return FormatWatts(currentWatts);
        }

        internal static string FormatEnergyPair(float currentWd, float maxWd)
        {
            if (maxWd > 0f)
            {
                return FormatEnergy(currentWd) + " / " + FormatEnergy(maxWd);
            }

            return FormatEnergy(currentWd);
        }

        internal static string FormatWatts(float watts)
        {
            if (Mathf.Abs(watts) >= 1000000f)
            {
                return (watts / 1000000f).ToString("0.#") + " MW";
            }

            if (Mathf.Abs(watts) >= 1000f)
            {
                return (watts / 1000f).ToString("0.#") + " kW";
            }

            return watts.ToString("0") + " W";
        }

        internal static string FormatWattsOrUnavailable(float watts, string unavailableText)
        {
            if (watts < 0f)
            {
                return string.IsNullOrEmpty(unavailableText) ? "-" : unavailableText;
            }

            return FormatWatts(watts);
        }

        internal static string FormatEnergy(float wattDays)
        {
            if (Mathf.Abs(wattDays) >= 1000000f)
            {
                return (wattDays / 1000000f).ToString("0.#") + " MWd";
            }

            if (Mathf.Abs(wattDays) >= 1000f)
            {
                return (wattDays / 1000f).ToString("0.#") + " kWd";
            }

            if (Mathf.Abs(wattDays) >= 10f)
            {
                return wattDays.ToString("0") + " Wd";
            }

            return Mathf.Abs(wattDays) >= 1f
                ? wattDays.ToString("0.#") + " Wd"
                : wattDays.ToString("0.##") + " Wd";
        }

        internal static float SafePositive(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                return 0f;
            }

            return value;
        }

        internal static float GetDisplayPowerWatts(ShuttleControlReadModel model)
        {
            if (model == null)
            {
                return 0f;
            }

            if (model.HasPowerRuntimeSnapshot)
            {
                return SafePositive(model.CurrentReactorGenerationWatts);
            }

            float powerGeneration = SafePositive(model.PowerGeneration);
            return powerGeneration > 0f ? powerGeneration : SafePositive(model.ReactorGenerationWatts);
        }

        internal static float GetDisplayPowerMaxWatts(ShuttleControlReadModel model, float currentWatts)
        {
            float reactorWatts = model != null ? SafePositive(model.ReactorGenerationWatts) : 0f;
            if (reactorWatts > 0f)
            {
                return Mathf.Max(currentWatts, reactorWatts);
            }

            return currentWatts > 0f ? currentWatts : 0f;
        }

        internal static float GetCargoCurrentMassKg(ShuttleCargoSnapshot cargoSnapshot)
        {
            return GetCargoLoadedMassKg(cargoSnapshot);
        }

        internal static float GetRemainingCapacityRatio01(float current, float capacity)
        {
            if (capacity <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01((capacity - Mathf.Max(0f, current)) / capacity);
        }

        internal static float GetCargoLoadedMassKg(ShuttleCargoSnapshot cargoSnapshot)
        {
            if (cargoSnapshot == null)
            {
                return 0f;
            }

            return Mathf.Max(
                0f,
                cargoSnapshot.LoadedMassKg +
                cargoSnapshot.RefrigeratedMassKg +
                cargoSnapshot.MedicalBayPatientMassKg +
                cargoSnapshot.ExternalRuntimeMassKg);
        }

        internal static float GetCargoCapacity(
            ShuttleControlReadModel model,
            ShuttleCargoSnapshot cargoSnapshot,
            bool clampModelCapacity)
        {
            float capacity = cargoSnapshot != null ? cargoSnapshot.MassCapacity : 0f;
            if (capacity > 0f)
            {
                return clampModelCapacity ? Mathf.Max(0f, capacity) : capacity;
            }

            capacity = model != null ? model.CargoMassCapacityKg : 0f;
            if (capacity > 0f)
            {
                return clampModelCapacity ? Mathf.Max(0f, capacity) : capacity;
            }

            return clampModelCapacity ? Mathf.Max(0f, capacity) : capacity;
        }
    }
}

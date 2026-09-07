using System;
using CeleTech.ShuttleExtension.ModularShuttle.Core;

namespace CeleTech.ShuttleExtension.ModularShuttle.Flight
{
    internal static class ShuttlePayloadLiftEnergyPolicy
    {
        internal static float ResolveNominalEnergyPerKgWd(ShuttleProfile profile)
        {
            return ShuttlePayloadCapacityPolicy
                .ResolveEffectivePayloadLiftEnergyPerKgWd(profile);
        }

        internal static float CalculatePayloadLiftEnergyWd(
            ShuttleProfile profile,
            float payloadMassKg)
        {
            if (payloadMassKg <= 0f)
            {
                return 0f;
            }

            float nominalPerKgWd = ResolveNominalEnergyPerKgWd(profile);
            if (nominalPerKgWd <= 0f)
            {
                return 0f;
            }

            float nominalTotalWd = ClampFinite(payloadMassKg * nominalPerKgWd);
            return QuantizeForReadableEnergy(nominalTotalWd);
        }

        private static float QuantizeForReadableEnergy(float wattDays)
        {
            if (wattDays <= 0f)
            {
                return 0f;
            }

            // Use progressively coarser, display-aligned steps. Small payloads retain
            // enough precision to stay close to the target multiplier, while large
            // charges settle on whole Wd, tenths of kWd, or tenths of MWd.
            float step = wattDays >= 1000000f
                ? 100000f
                : (wattDays >= 1000f
                    ? 100f
                    : (wattDays >= 10f ? 1f : (wattDays >= 1f ? 0.1f : 0.01f)));
            double units = Math.Floor((wattDays / step) + 0.5d);
            if (units < 1d)
            {
                units = 1d;
            }

            return ClampFinite((float)(units * step));
        }

        private static float ClampFinite(float value)
        {
            if (float.IsNaN(value) || value <= 0f)
            {
                return 0f;
            }

            return float.IsInfinity(value) ? float.MaxValue : value;
        }
    }
}

using System;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    internal static class ShuttleConstructionWorkTuning
    {
        internal const float BaselineScale = 0.1f;

        internal static int ApplyToAuthoredWork(int authoredWork)
        {
            if (authoredWork <= 0)
            {
                return 0;
            }

            double effective = authoredWork *
                (double)BaselineScale *
                ResolvePlayerMultiplier();
            if (effective >= int.MaxValue)
            {
                return int.MaxValue;
            }

            return Math.Max(1, (int)Math.Round(effective, MidpointRounding.AwayFromZero));
        }

        internal static float ApplyToAuthoredWork(float authoredWork)
        {
            if (authoredWork <= 0f)
            {
                return 0f;
            }

            double effective = authoredWork *
                (double)BaselineScale *
                ResolvePlayerMultiplier();
            if (effective >= float.MaxValue)
            {
                return float.MaxValue;
            }

            return Math.Max(
                1f,
                (float)Math.Round(effective, MidpointRounding.AwayFromZero));
        }

        private static float ResolvePlayerMultiplier()
        {
            ShuttleOtherSettings other = CeleTechShuttleMod.Settings != null
                ? CeleTechShuttleMod.Settings.Other
                : null;
            float multiplier = other != null
                ? other.ConstructionWorkMultiplier
                : ShuttleOtherSettings.DefaultConstructionWorkMultiplier;
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier))
            {
                multiplier = ShuttleOtherSettings.DefaultConstructionWorkMultiplier;
            }

            return Math.Max(
                ShuttleOtherSettings.MinimumConstructionWorkMultiplier,
                Math.Min(
                    ShuttleOtherSettings.MaximumConstructionWorkMultiplier,
                    multiplier));
        }
    }
}

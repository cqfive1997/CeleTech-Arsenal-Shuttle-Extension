using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class SurfaceShieldVisualConfigResolver
    {
        internal static bool TryGetVisualConfig(
            CompModularShuttleSurfaceShieldVisual comp,
            out ShuttleSurfaceShieldVisualConfigSnapshot config)
        {
            config = null;
            if (comp.parent == null)
            {
                return false;
            }

            IShuttleSurfaceShieldPort port =
                comp.parent.GetComp<CompModularShuttleCore>() as IShuttleSurfaceShieldPort;
            return port != null && port.TryGetActiveSurfaceShieldVisualConfig(out config);
        }

        internal static float GetCurrentShieldStrengthForVisual(CompModularShuttleSurfaceShieldVisual comp)
        {
            ShuttleSurfaceShieldVisualConfigSnapshot config;
            if (TryGetVisualConfig(comp, out config) && config != null)
            {
                return GetCurrentShieldStrengthForVisual(config);
            }

            return 1f;
        }

        internal static float GetCurrentShieldStrengthForVisual(ShuttleSurfaceShieldVisualConfigSnapshot config)
        {
            if (config == null)
            {
                return 1f;
            }

            if (!config.Online || IsBrokenSurfaceShieldStatus(config))
            {
                return 0f;
            }

            return Mathf.Clamp01(config.HitPointsPercent);
        }

        internal static float GetEffectiveShieldStrengthForVisual(ShuttleSurfaceShieldVisualConfigSnapshot config)
        {
            return GetCurrentShieldStrengthForVisual(config);
        }

        internal static bool IsBrokenSurfaceShieldStatus(ShuttleSurfaceShieldVisualConfigSnapshot config)
        {
            return config != null && config.StatusKey == "Broken";
        }
    }
}

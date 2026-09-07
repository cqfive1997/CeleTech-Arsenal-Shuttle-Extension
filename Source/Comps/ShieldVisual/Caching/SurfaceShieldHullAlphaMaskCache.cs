using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class SurfaceShieldHullAlphaMaskCache
    {
        internal static bool TryIsSolidCurrentHullUv(
            CompModularShuttleSurfaceShieldVisual comp,
            Vector2 hullUv)
        {
            HullAlphaMaskCache mask;
            return TryGetHullAlphaMask(comp, out mask) && IsSolidUv(hullUv, mask);
        }

        internal static float GetHullOutlineAlphaThreshold(CompModularShuttleSurfaceShieldVisual comp)
        {
            CompProperties_ModularShuttleSurfaceShieldVisual props = Props(comp);
            return props.enableGpuHullEdgeGlow
                ? Mathf.Clamp01(props.hullOutlineAlphaThreshold)
                : Mathf.Clamp01(props.hullAlphaThreshold);
        }

        internal static float GetHullOutlineGlowAlpha(CompModularShuttleSurfaceShieldVisual comp)
        {
            CompProperties_ModularShuttleSurfaceShieldVisual props = Props(comp);
            return props.enableGpuHullEdgeGlow
                ? Mathf.Clamp01(props.hullOutlineGlowAlpha)
                : Mathf.Clamp01(props.idleSurfaceEdgeGlowAlpha);
        }

        internal static float GetHullOutlineLowShieldFlicker(CompModularShuttleSurfaceShieldVisual comp)
        {
            CompProperties_ModularShuttleSurfaceShieldVisual props = Props(comp);
            return props.enableGpuHullEdgeGlow
                ? Mathf.Clamp01(props.hullOutlineLowShieldFlicker)
                : Mathf.Clamp01(props.idleSurfaceLowShieldFlickerAlpha);
        }

        internal static float GetEffectiveHullOutlineSampleOffset(CompModularShuttleSurfaceShieldVisual comp)
        {
            return Mathf.Clamp(Props(comp).hullOutlineSampleOffsetScale, 0.25f, 8f);
        }

            internal static bool TryGetHullAlphaMask(
                CompModularShuttleSurfaceShieldVisual comp,
                out HullAlphaMaskCache mask)
            {
                mask = null;
                Texture2D texture = SurfaceShieldMaterialCache.TryGetCurrentHullTexture(comp);
                if (texture == null)
                {
                    return false;
                }

                CompProperties_ModularShuttleSurfaceShieldVisual props = Props(comp);
                int resolution = Mathf.Clamp(props.hullAlphaMaskResolution, 8, 256);
                float threshold = Mathf.Clamp01(props.hullAlphaThreshold);
                if (comp.CachedHullAlphaMask != null &&
                    comp.CachedHullAlphaMask.Texture == texture &&
                    comp.CachedHullAlphaMask.Resolution == resolution &&
                    Mathf.Approximately(comp.CachedHullAlphaMask.Threshold, threshold))
                {
                    mask = comp.CachedHullAlphaMask;
                    return mask.IsValid;
                }

                comp.CachedHullAlphaMask = new HullAlphaMaskCache
                {
                    Texture = texture,
                    Resolution = resolution,
                    Threshold = threshold
                };

                bool[] solid;
                if (!TryBuildHullAlphaMask(comp, texture, resolution, threshold, out solid))
                {
                    comp.CachedHullAlphaMask.IsValid = false;
                    mask = comp.CachedHullAlphaMask;
                    return false;
                }

                comp.CachedHullAlphaMask.Solid = solid;
                comp.CachedHullAlphaMask.IsValid = true;
                mask = comp.CachedHullAlphaMask;
                return true;
            }

            internal static bool TryBuildHullAlphaMask(
                CompModularShuttleSurfaceShieldVisual comp,
                Texture2D texture,
                int resolution,
                float threshold,
                out bool[] solid)
            {
                solid = null;
                if (texture == null || resolution <= 0)
                {
                    return false;
                }

                bool[] mask = new bool[resolution * resolution];
                bool anySolid = false;
                try
                {
                    for (int y = 0; y < resolution; y++)
                    {
                        float v = (y + 0.5f) / resolution;
                        for (int x = 0; x < resolution; x++)
                        {
                            float u = (x + 0.5f) / resolution;
                            bool isSolid = texture.GetPixelBilinear(u, v).a > threshold;
                            mask[y * resolution + x] = isSolid;
                            anySolid |= isSolid;
                        }
                    }
                }
                catch (UnityException exception)
                {
                    comp.LogHullAlphaMaskFailureOnce(texture, exception.Message);
                    return false;
                }

                if (!anySolid)
                {
                    comp.LogHullAlphaMaskFailureOnce(texture, "alpha mask contained no solid cells");
                    return false;
                }

                solid = mask;
                return true;
            }

            internal static bool IsSolidUv(Vector2 uv, HullAlphaMaskCache mask)
            {
                int resolution = mask != null ? mask.Resolution : 0;
                if (resolution <= 0)
                {
                    return false;
                }

                int x = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(uv.x) * resolution), 0, resolution - 1);
                int y = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(uv.y) * resolution), 0, resolution - 1);
                return IsSolidCell(mask, x, y);
            }

            internal static bool TryGetSolidCellUv(Vector2 uv, HullAlphaMaskCache mask, out Vector2 solidUv)
            {
                solidUv = Vector2.zero;
                int resolution = mask != null ? mask.Resolution : 0;
                if (resolution <= 0)
                {
                    return false;
                }

                int x = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(uv.x) * resolution), 0, resolution - 1);
                int y = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(uv.y) * resolution), 0, resolution - 1);
                if (!IsSolidCell(mask, x, y))
                {
                    return false;
                }

                solidUv = SurfaceShieldHullGeometry.CellToUv(x, y, resolution);
                return true;
            }

            internal static bool IsSolidCell(HullAlphaMaskCache mask, int x, int y)
            {
                return mask != null &&
                    mask.IsValid &&
                    mask.Solid != null &&
                    x >= 0 &&
                    y >= 0 &&
                    x < mask.Resolution &&
                    y < mask.Resolution &&
                    mask.Solid[y * mask.Resolution + x];
            }

            internal static bool TryFindNearestSolidUv(
                float u,
                float v,
                HullAlphaMaskCache mask,
                int localRadius,
                int maxRadius,
                out Vector2 solidUv)
            {
                solidUv = Vector2.zero;
                if (mask == null || !mask.IsValid || mask.Solid == null || mask.Resolution <= 0)
                {
                    return false;
                }

                int resolution = mask.Resolution;
                int centerX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(u) * resolution), 0, resolution - 1);
                int centerY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(v) * resolution), 0, resolution - 1);

                int bestX;
                int bestY;
                int clampedLocalRadius = Mathf.Clamp(localRadius, 0, resolution);
                int clampedMaxRadius = Mathf.Clamp(Mathf.Max(clampedLocalRadius, maxRadius), 0, resolution);
                if (TryFindNearestSolidCellInRange(centerX, centerY, mask, 0, clampedLocalRadius, out bestX, out bestY) ||
                    TryFindNearestSolidCellInRange(centerX, centerY, mask, clampedLocalRadius + 1, clampedMaxRadius, out bestX, out bestY))
                {
                    solidUv = SurfaceShieldHullGeometry.CellToUv(
                        bestX,
                        bestY,
                        resolution);
                    return true;
                }

                return false;
            }

            internal static bool TryFindNearestSolidCellInRange(
                int centerX,
                int centerY,
                HullAlphaMaskCache mask,
                int minRadius,
                int maxRadius,
                out int bestX,
                out int bestY)
            {
                bestX = centerX;
                bestY = centerY;
                if (mask == null || maxRadius < minRadius)
                {
                    return false;
                }

                bool found = false;
                int bestDistanceSquared = int.MaxValue;
                int resolution = mask.Resolution;
                for (int dy = -maxRadius; dy <= maxRadius; dy++)
                {
                    int y = centerY + dy;
                    if (y < 0 || y >= resolution)
                    {
                        continue;
                    }

                    for (int dx = -maxRadius; dx <= maxRadius; dx++)
                    {
                        int ringRadius = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                        if (ringRadius < minRadius || ringRadius > maxRadius)
                        {
                            continue;
                        }

                        int x = centerX + dx;
                        if (x < 0 || x >= resolution || !IsSolidCell(mask, x, y))
                        {
                            continue;
                        }

                        int distanceSquared = dx * dx + dy * dy;
                        if (!found || distanceSquared < bestDistanceSquared)
                        {
                            found = true;
                            bestDistanceSquared = distanceSquared;
                            bestX = x;
                            bestY = y;
                        }
                    }
                }

                return found;
            }

        private static CompProperties_ModularShuttleSurfaceShieldVisual Props(
            CompModularShuttleSurfaceShieldVisual comp)
        {
            return (CompProperties_ModularShuttleSurfaceShieldVisual)comp.props;
        }
    }
}

using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class SurfaceShieldHullGeometry
    {
            internal static Vector2 WorldPositionToHullUv(
                CompModularShuttleSurfaceShieldVisual comp,
                Vector3 worldPosition,
                Vector2 drawSize)
            {
                Vector2 uv = WorldPositionToHullUvUnclamped(comp, worldPosition);
                return new Vector2(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y));
            }

            internal static Vector2 WorldPositionToHullUvUnclamped(
                CompModularShuttleSurfaceShieldVisual comp,
                Vector3 worldPosition)
            {
                Vector3 drawLoc = GetCurrentHullDrawLocWithoutOverlayOffset(comp);
                Quaternion inverseRotation = Quaternion.Inverse(GetCurrentHullRotation(comp));
                Vector3 local = inverseRotation * (worldPosition - drawLoc);
                Vector2 hullSize = GetCurrentHullMeshWorldSize(comp);
                float width = Mathf.Max(0.001f, hullSize.x);
                float depth = Mathf.Max(0.001f, hullSize.y);
                float u = 0.5f + (local.x / width);
                float v = 0.5f + (local.z / depth);
                return new Vector2(u, v);
            }

            internal static Vector3 HullUvToWorldPosition(
                CompModularShuttleSurfaceShieldVisual comp,
                Vector2 uv,
                Vector2 drawSize,
                float y)
            {
                Vector3 drawLoc = GetCurrentHullDrawLocWithoutOverlayOffset(comp);
                Vector2 hullSize = GetCurrentHullMeshWorldSize(comp);
                Vector3 local = new Vector3(
                    (uv.x - 0.5f) * Mathf.Max(0.001f, hullSize.x),
                    0f,
                    (uv.y - 0.5f) * Mathf.Max(0.001f, hullSize.y));
                Vector3 world = drawLoc + GetCurrentHullRotation(comp) * local;
                return new Vector3(world.x, y, world.z);
            }

            internal static Vector3 ResolveVisualHitPosition(
                CompModularShuttleSurfaceShieldVisual comp,
                Vector3 rawWorldPosition,
                Vector2 impactDirectionXZ,
                out Vector2 hullUv,
                out bool snappedToHull,
                out bool hullMaskAvailable,
                out bool directionalSurfaceHit,
                out bool nearestSolidFallback,
                out bool impactClampedToHull)
            {
                Vector2 rawUvUnclamped = WorldPositionToHullUvUnclamped(comp, rawWorldPosition);
                hullUv = new Vector2(Mathf.Clamp01(rawUvUnclamped.x), Mathf.Clamp01(rawUvUnclamped.y));
                snappedToHull = false;
                hullMaskAvailable = false;
                directionalSurfaceHit = false;
                nearestSolidFallback = false;
                impactClampedToHull = false;
                if (!Props(comp).snapHitToHullTextureAlpha || comp.parent == null)
                {
                    return rawWorldPosition;
                }

                HullAlphaMaskCache mask;
                if (!SurfaceShieldHullAlphaMaskCache.TryGetHullAlphaMask(comp, out mask))
                {
                    Vector3 rectClampedWorld = HullUvToWorldPosition(comp, hullUv, GetCurrentHullDrawSize(comp), rawWorldPosition.y);
                    rectClampedWorld = OffsetHullSurfaceWorldPosition(comp, rectClampedWorld, impactDirectionXZ, rawWorldPosition.y);
                    snappedToHull =
                        Mathf.Abs(rectClampedWorld.x - rawWorldPosition.x) > 0.001f ||
                        Mathf.Abs(rectClampedWorld.z - rawWorldPosition.z) > 0.001f;
                    return rectClampedWorld;
                }

                hullMaskAvailable = true;
                Vector2 drawSize = GetCurrentHullDrawSize(comp);
                Vector2 rawUv = new Vector2(Mathf.Clamp01(rawUvUnclamped.x), Mathf.Clamp01(rawUvUnclamped.y));
                if (IsUvInUnitRect(rawUvUnclamped) && SurfaceShieldHullAlphaMaskCache.IsSolidUv(rawUv, mask))
                {
                    hullUv = rawUv;
                    Vector3 rawSurfaceWorld = HullUvToWorldPosition(comp, rawUv, drawSize, rawWorldPosition.y);
                    rawSurfaceWorld = OffsetHullSurfaceWorldPosition(comp, rawSurfaceWorld, impactDirectionXZ, rawWorldPosition.y);
                    impactClampedToHull = true;
                    snappedToHull =
                        Mathf.Abs(rawSurfaceWorld.x - rawWorldPosition.x) > 0.001f ||
                        Mathf.Abs(rawSurfaceWorld.z - rawWorldPosition.z) > 0.001f;
                    return rawSurfaceWorld;
                }

                Vector2 snappedUv;
                if (SurfaceShieldHullAlphaMaskCache.TryFindNearestSolidUv(
                    rawUv.x,
                    rawUv.y,
                    mask,
                    Props(comp).hullAlphaLocalSearchRadius,
                    Props(comp).hullAlphaMaxSearchRadius,
                    out snappedUv))
                {
                    Vector3 snappedWorld = HullUvToWorldPosition(comp, snappedUv, drawSize, rawWorldPosition.y);
                    snappedWorld = OffsetHullSurfaceWorldPosition(comp, snappedWorld, impactDirectionXZ, rawWorldPosition.y);
                    hullUv = snappedUv;
                    nearestSolidFallback = true;
                    impactClampedToHull = true;
                    snappedToHull =
                        Mathf.Abs(snappedWorld.x - rawWorldPosition.x) > 0.001f ||
                        Mathf.Abs(snappedWorld.z - rawWorldPosition.z) > 0.001f;
                    return snappedWorld;
                }

                Vector2 surfaceUv;
                if (TryIntersectHullSurfaceByDirection(comp, mask, impactDirectionXZ, drawSize, out surfaceUv))
                {
                    hullUv = surfaceUv;
                    Vector3 surfaceWorld = HullUvToWorldPosition(comp, surfaceUv, drawSize, rawWorldPosition.y);
                    Vector3 shieldWorld = OffsetHullSurfaceWorldPosition(comp, surfaceWorld, impactDirectionXZ, rawWorldPosition.y);
                    directionalSurfaceHit = true;
                    impactClampedToHull = true;
                    snappedToHull =
                        Mathf.Abs(shieldWorld.x - rawWorldPosition.x) > 0.001f ||
                        Mathf.Abs(shieldWorld.z - rawWorldPosition.z) > 0.001f;
                    return shieldWorld;
                }

                Vector3 rectFallbackWorld = HullUvToWorldPosition(comp, rawUv, drawSize, rawWorldPosition.y);
                rectFallbackWorld = OffsetHullSurfaceWorldPosition(comp, rectFallbackWorld, impactDirectionXZ, rawWorldPosition.y);
                snappedToHull =
                    Mathf.Abs(rectFallbackWorld.x - rawWorldPosition.x) > 0.001f ||
                    Mathf.Abs(rectFallbackWorld.z - rawWorldPosition.z) > 0.001f;
                return rectFallbackWorld;
            }

            internal static bool IsUvInUnitRect(Vector2 uv)
            {
                return uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
            }

            internal static Vector3 ApplyVerticalImpactInwardBias(
                CompModularShuttleSurfaceShieldVisual comp,
                Vector3 correctedWorldPosition,
                Vector2 impactDirectionXZ,
                float y,
                ref Vector2 hullUv,
                out bool applied,
                out bool finalHullUvSolid)
            {
                applied = false;
                finalHullUvSolid = false;
                float biasWorld = Mathf.Max(0f, Props(comp).verticalImpactInwardBiasWorld);
                float extraBiasWorld = Mathf.Max(0f, Props(comp).verticalExtraInwardBiasWorld);
                float totalCandidateBiasWorld = biasWorld + extraBiasWorld;
                if (totalCandidateBiasWorld <= 0f || comp.parent == null)
                {
                    finalHullUvSolid = SurfaceShieldHullAlphaMaskCache.TryIsSolidCurrentHullUv(comp, hullUv);
                    return correctedWorldPosition;
                }

                Vector3 hullCenter = GetCurrentHullDrawLocWithoutOverlayOffset(comp);
                Vector2 fromCenter = new Vector2(
                    correctedWorldPosition.x - hullCenter.x,
                    correctedWorldPosition.z - hullCenter.z);
                if (fromCenter.sqrMagnitude <= 0.0001f)
                {
                    finalHullUvSolid = SurfaceShieldHullAlphaMaskCache.TryIsSolidCurrentHullUv(comp, hullUv);
                    return correctedWorldPosition;
                }

                Vector2 outward = fromCenter.normalized;
                float threshold = Mathf.Clamp01(Props(comp).verticalImpactInwardBiasThreshold);
                if (Mathf.Abs(outward.y) < threshold)
                {
                    finalHullUvSolid = SurfaceShieldHullAlphaMaskCache.TryIsSolidCurrentHullUv(comp, hullUv);
                    return correctedWorldPosition;
                }

                float distanceFromCenter = fromCenter.magnitude;
                biasWorld = totalCandidateBiasWorld;
                float safeBias = Mathf.Min(biasWorld, Mathf.Max(0f, distanceFromCenter - 0.05f));
                if (safeBias <= 0f)
                {
                    finalHullUvSolid = SurfaceShieldHullAlphaMaskCache.TryIsSolidCurrentHullUv(comp, hullUv);
                    return correctedWorldPosition;
                }

                Vector3 biasedWorld = new Vector3(
                    correctedWorldPosition.x - outward.x * safeBias,
                    y,
                    correctedWorldPosition.z - outward.y * safeBias);
                Vector2 biasedUv = WorldPositionToHullUv(comp, biasedWorld, GetCurrentHullDrawSize(comp));
                HullAlphaMaskCache mask;
                if (SurfaceShieldHullAlphaMaskCache.TryGetHullAlphaMask(comp, out mask))
                {
                    if (!SurfaceShieldHullAlphaMaskCache.IsSolidUv(biasedUv, mask))
                    {
                        Vector2 snappedUv;
                        if (SurfaceShieldHullAlphaMaskCache.TryFindNearestSolidUv(
                            biasedUv.x,
                            biasedUv.y,
                            mask,
                            Props(comp).hullAlphaLocalSearchRadius,
                            Props(comp).hullAlphaMaxSearchRadius,
                            out snappedUv))
                        {
                            biasedUv = snappedUv;
                            biasedWorld = HullUvToWorldPosition(comp, biasedUv, GetCurrentHullDrawSize(comp), y);
                        }
                    }

                    finalHullUvSolid = SurfaceShieldHullAlphaMaskCache.IsSolidUv(biasedUv, mask);
                }

                hullUv = biasedUv;
                applied = true;
                return biasedWorld;
            }

            internal static bool TryIntersectHullSurfaceByDirection(
                CompModularShuttleSurfaceShieldVisual comp,
                HullAlphaMaskCache mask,
                Vector2 impactDirectionXZ,
                Vector2 drawSize,
                out Vector2 surfaceUv)
            {
                surfaceUv = Vector2.zero;
                if (mask == null || !mask.IsValid || mask.Solid == null || mask.Resolution <= 0)
                {
                    return false;
                }

                Vector3 localDirection = Quaternion.Inverse(GetCurrentHullRotation(comp)) *
                    new Vector3(impactDirectionXZ.x, 0f, impactDirectionXZ.y);
                Vector2 hullSize = GetCurrentHullMeshWorldSize(comp);
                float width = Mathf.Max(0.001f, hullSize.x);
                float depth = Mathf.Max(0.001f, hullSize.y);
                Vector2 directionUv = new Vector2(localDirection.x / width, localDirection.z / depth);
                if (directionUv.sqrMagnitude <= 0.000001f)
                {
                    return false;
                }

                directionUv = directionUv.normalized;
                Vector2 centerUv = new Vector2(0.5f, 0.5f);
                float distanceToEdge;
                if (!TryGetUvEdgeDistance(centerUv, directionUv, out distanceToEdge))
                {
                    return false;
                }

                Vector2 startUv = centerUv + directionUv * distanceToEdge;
                startUv.x = Mathf.Clamp01(startUv.x);
                startUv.y = Mathf.Clamp01(startUv.y);

                int steps = Mathf.Clamp(mask.Resolution, 24, 64);
                for (int i = 0; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    Vector2 sampleUv = Vector2.Lerp(startUv, centerUv, t);
                    if (SurfaceShieldHullAlphaMaskCache.TryGetSolidCellUv(sampleUv, mask, out surfaceUv))
                    {
                        return true;
                    }
                }

                return false;
            }

            internal static bool TryGetUvEdgeDistance(
                Vector2 centerUv,
                Vector2 directionUv,
                out float distanceToEdge)
            {
                distanceToEdge = float.MaxValue;
                bool found = false;
                const float Epsilon = 0.0001f;
                if (directionUv.x > Epsilon)
                {
                    distanceToEdge = Mathf.Min(distanceToEdge, (1f - centerUv.x) / directionUv.x);
                    found = true;
                }
                else if (directionUv.x < -Epsilon)
                {
                    distanceToEdge = Mathf.Min(distanceToEdge, (0f - centerUv.x) / directionUv.x);
                    found = true;
                }

                if (directionUv.y > Epsilon)
                {
                    distanceToEdge = Mathf.Min(distanceToEdge, (1f - centerUv.y) / directionUv.y);
                    found = true;
                }
                else if (directionUv.y < -Epsilon)
                {
                    distanceToEdge = Mathf.Min(distanceToEdge, (0f - centerUv.y) / directionUv.y);
                    found = true;
                }

                return found && distanceToEdge > 0f && distanceToEdge < float.MaxValue;
            }

            internal static Vector3 OffsetHullSurfaceWorldPosition(
                CompModularShuttleSurfaceShieldVisual comp,
                Vector3 hullSurfaceWorld,
                Vector2 impactDirectionXZ,
                float y)
            {
                float offset = GetImpactSurfaceVisualOffset(comp);
                if (offset <= 0f)
                {
                    return hullSurfaceWorld;
                }

                Vector2 outward = ResolveHullSurfaceOutwardDirection(comp, hullSurfaceWorld, impactDirectionXZ);
                if (outward.sqrMagnitude <= 0.0001f)
                {
                    return hullSurfaceWorld;
                }

                float scaledOffset = offset * GetDirectionalImpactSurfaceOffsetScale(comp, outward);
                return new Vector3(
                    hullSurfaceWorld.x + outward.x * scaledOffset,
                    y,
                    hullSurfaceWorld.z + outward.y * scaledOffset);
            }

            internal static Vector2 ResolveHullSurfaceOutwardDirection(
                CompModularShuttleSurfaceShieldVisual comp,
                Vector3 hullSurfaceWorld,
                Vector2 impactDirectionXZ)
            {
                Vector3 parentDrawPos = GetCurrentHullDrawLocWithoutOverlayOffset(comp);
                Vector2 outward = new Vector2(
                    hullSurfaceWorld.x - parentDrawPos.x,
                    hullSurfaceWorld.z - parentDrawPos.z);
                if (outward.sqrMagnitude <= 0.0001f)
                {
                    outward = impactDirectionXZ;
                }

                if (outward.sqrMagnitude <= 0.0001f)
                {
                    return Vector2.zero;
                }

                return outward.normalized;
            }

            internal static float GetDirectionalImpactSurfaceOffsetScale(
                CompModularShuttleSurfaceShieldVisual comp,
                Vector2 outward)
            {
                if (outward.sqrMagnitude <= 0.0001f)
                {
                    return Mathf.Max(0f, Props(comp).horizontalImpactSurfaceOffsetScale);
                }

                Vector2 normalized = outward.normalized;
                float verticalWeight = Mathf.Clamp01(Mathf.Abs(normalized.y));
                float horizontalScale = Mathf.Max(0f, Props(comp).horizontalImpactSurfaceOffsetScale);
                float verticalScale = Mathf.Max(0f, Props(comp).verticalImpactSurfaceOffsetScale);
                return Mathf.Lerp(horizontalScale, verticalScale, verticalWeight);
            }

            internal static bool IsVerticalImpactTighteningApplied(
                CompModularShuttleSurfaceShieldVisual comp,
                Vector2 outward)
            {
                if (outward.sqrMagnitude <= 0.0001f)
                {
                    return false;
                }

                float horizontalScale = Mathf.Max(0f, Props(comp).horizontalImpactSurfaceOffsetScale);
                float verticalScale = Mathf.Max(0f, Props(comp).verticalImpactSurfaceOffsetScale);
                return verticalScale < horizontalScale && Mathf.Abs(outward.normalized.y) > Mathf.Abs(outward.normalized.x);
            }

            internal static float GetImpactSurfaceVisualOffset(CompModularShuttleSurfaceShieldVisual comp)
            {
                return Mathf.Clamp(
                    Props(comp).hullSurfaceShieldOffset,
                    0f,
                    Mathf.Max(0f, Props(comp).maxImpactSurfaceVisualOffset));
            }

            internal static Vector2 CellToUv(int x, int y, int resolution)
            {
                float safeResolution = Mathf.Max(1, resolution);
                return new Vector2((x + 0.5f) / safeResolution, (y + 0.5f) / safeResolution);
            }

            internal static Quaternion GetCurrentHullRotation(CompModularShuttleSurfaceShieldVisual comp)
            {
                Graphic graphic = SurfaceShieldMaterialCache.TryGetCurrentHullGraphic(comp);
                if (graphic == null || comp.parent == null)
                {
                    return Quaternion.identity;
                }

                Quaternion rotation = graphic.QuatFromRot(comp.parent.Rotation);
                if (graphic.data != null && graphic.data.addTopAltitudeBias)
                {
                    rotation *= Quaternion.Euler(Vector3.left * 2f);
                }

                return rotation;
            }

            internal static Vector3 GetCurrentHullDrawScale()
            {
                return Vector3.one;
            }

            internal static Vector3 GetCurrentHullDrawLoc(CompModularShuttleSurfaceShieldVisual comp)
            {
                Vector3 drawLoc = GetCurrentHullDrawLocWithoutOverlayOffset(comp);
                drawLoc.y += Mathf.Max(0.01f, Props(comp).shieldOverlayHeightOffset);
                return drawLoc;
            }

            internal static Vector3 GetCurrentHullDrawLocWithoutOverlayOffset(
                CompModularShuttleSurfaceShieldVisual comp)
            {
                if (comp.parent == null)
                {
                    return Vector3.zero;
                }

                Vector3 drawLoc = comp.parent.DrawPos;
                Graphic graphic = SurfaceShieldMaterialCache.TryGetCurrentHullGraphic(comp);
                if (graphic != null)
                {
                    drawLoc += graphic.DrawOffset(comp.parent.Rotation);
                }

                return drawLoc;
            }

            internal static bool GetCurrentHullShouldDrawRotated(CompModularShuttleSurfaceShieldVisual comp)
            {
                Graphic graphic = SurfaceShieldMaterialCache.TryGetCurrentHullGraphic(comp);
                return graphic != null && graphic.ShouldDrawRotated;
            }

            internal static Vector2 GetCurrentHullMeshWorldSize(CompModularShuttleSurfaceShieldVisual comp)
            {
                Vector2 drawSize = GetCurrentHullDrawSize(comp);
                if (comp.parent != null &&
                    comp.parent.Rotation.IsHorizontal &&
                    !GetCurrentHullShouldDrawRotated(comp))
                {
                    return drawSize.Rotated();
                }

                return drawSize;
            }

            internal static Vector2 GetCurrentHullDrawSize(CompModularShuttleSurfaceShieldVisual comp)
            {
                if (comp.parent != null && comp.parent.Graphic != null && comp.parent.Graphic.drawSize != Vector2.zero)
                {
                    return comp.parent.Graphic.drawSize;
                }

                if (comp.parent != null && comp.parent.def != null && comp.parent.def.graphicData != null)
                {
                    Vector2 drawSize = comp.parent.def.graphicData.drawSize;
                    if (drawSize != Vector2.zero)
                    {
                        return drawSize;
                    }
                }

                if (comp.parent != null && comp.parent.def != null)
                {
                    return new Vector2(
                        Mathf.Max(1f, comp.parent.def.size.x),
                        Mathf.Max(1f, comp.parent.def.size.z));
                }

                return new Vector2(1f, 1f);
            }

            internal static Vector2 ResolveImpactDirectionXZ(
                CompModularShuttleSurfaceShieldVisual comp,
                Vector3 worldPosition,
                int seed)
            {
                Vector3 parentDrawPos = comp.parent != null ? comp.parent.DrawPos : Vector3.zero;
                Vector2 direction = new Vector2(worldPosition.x - parentDrawPos.x, worldPosition.z - parentDrawPos.z);
                if (direction.sqrMagnitude > 0.0001f)
                {
                    return direction.normalized;
                }

                float angle = SurfaceShieldFallbackMath.PseudoRandom01(seed + 271) * Mathf.PI * 2f;
                return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            internal static Vector3 DirectionToWorld(Vector2 direction)
            {
                return new Vector3(direction.x, 0f, direction.y);
            }

            internal static float DirectionAngleDeg(Vector2 direction)
            {
                return Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            }

            internal static Vector2 RotateDirection(Vector2 direction, float degrees)
            {
                float radians = degrees * Mathf.Deg2Rad;
                float cos = Mathf.Cos(radians);
                float sin = Mathf.Sin(radians);
                return new Vector2(
                    direction.x * cos - direction.y * sin,
                    direction.x * sin + direction.y * cos).normalized;
            }

            internal static Vector2 GetSplashDirection(
                CompModularShuttleSurfaceShieldVisual comp,
                int seed,
                int index,
                int count,
                float rotation)
            {
                int safeCount = Mathf.Max(1, count);
                float baseAngle = Mathf.PI * 2f * index / safeCount;
                float jitter = (SurfaceShieldFallbackMath.PseudoRandom01(seed + index * 97 + 23) - 0.5f) * (Mathf.PI * 2f / safeCount) * 0.55f;
                float angle = baseAngle + rotation + jitter;
                return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            private static CompProperties_ModularShuttleSurfaceShieldVisual Props(
                CompModularShuttleSurfaceShieldVisual comp)
            {
                return (CompProperties_ModularShuttleSurfaceShieldVisual)comp.props;
            }
    }
}

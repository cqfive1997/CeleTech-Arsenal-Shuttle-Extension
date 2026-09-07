using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class SurfaceShieldVisualDiagnostics
    {
        internal static string BuildActivePulseHullUvDebugString(
            ShieldHitPulse[] pulses,
            int activePulseCount)
        {
            if (pulses == null || pulses.Length == 0 || activePulseCount <= 0)
            {
                return "<none>";
            }

            string result = null;
            int count = 0;
            for (int i = 0; i < pulses.Length && count < 4; i++)
            {
                ShieldHitPulse pulse = pulses[i];
                if (!pulse.Valid)
                {
                    continue;
                }

                string item =
                    "#" + count +
                    "(uv=" + pulse.HullUv +
                    ", clamped=" + pulse.ImpactClampedToHull +
                    ", method=" + (pulse.ClampMethod ?? "<none>") +
                    ", mask=" + pulse.HullMaskAvailable +
                    ", offset=" + pulse.SurfaceOffsetDistance.ToString("0.###") +
                    ", verticalTightening=" + pulse.VerticalTighteningApplied +
                    ", verticalInwardBias=" + pulse.VerticalInwardBiasApplied +
                    ", broke=" + pulse.BrokeShield + ")";
                result = result == null ? item : result + "; " + item;
                count++;
            }

            return result ?? "<none>";
        }

        internal static string ResolveImpactClampMethod(
            bool hullMaskAvailable,
            bool directionalSurfaceHit,
            bool nearestSolidFallback,
            bool impactClampedToHull,
            bool snappedToHull)
        {
            if (directionalSurfaceHit)
            {
                return "directional";
            }

            if (nearestSolidFallback)
            {
                return "nearest";
            }

            if (impactClampedToHull)
            {
                return "solid";
            }

            if (!hullMaskAvailable && snappedToHull)
            {
                return "rectFallback";
            }

            if (!hullMaskAvailable)
            {
                return "rawNoMask";
            }

            return snappedToHull ? "rectClamp" : "rawFallback";
        }

        internal static void LogVisualResourcesNotConfiguredOnce(
            ref string lastLoggedUnconfiguredModuleID,
            ShuttleSurfaceShieldVisualConfigSnapshot config)
        {
            if (!Prefs.DevMode || config == null)
            {
                return;
            }

            string moduleID = config.ModuleInstanceID ?? "<none>";
            if (lastLoggedUnconfiguredModuleID == moduleID)
            {
                return;
            }

            lastLoggedUnconfiguredModuleID = moduleID;
            ShuttleLog.Debug(
                "SurfaceShieldVisual",
                "Surface shield visual config missing bundle/material; fallback active. module=" + moduleID);
        }

        internal static void LogMissingMaterialOnce(
            ref string lastLoggedMissingMaterialKey,
            ShuttleSurfaceShieldVisualConfigSnapshot config,
            string failureReason)
        {
            if (!Prefs.DevMode || config == null)
            {
                return;
            }

            string key =
                (config.ModuleInstanceID ?? "<none>") + "|" +
                (config.SurfaceShaderBundlePath ?? "<null>") + "|" +
                (config.SurfaceMaterialAssetName ?? "<null>");
            if (lastLoggedMissingMaterialKey == key)
            {
                return;
            }

            lastLoggedMissingMaterialKey = key;
            ShuttleLog.Debug(
                "SurfaceShieldVisual",
                "Surface shield hull overlay material unavailable; fallback active. module=" +
                (config.ModuleInstanceID ?? "<none>") +
                ", bundle=" + (config.SurfaceShaderBundlePath ?? "<null>") +
                ", material=" + (config.SurfaceMaterialAssetName ?? "<null>") +
                ", reason=" +
                (failureReason ?? "unknown"));
        }

        internal static void LogHullOverlayMaterialActiveOnce(
            ref string lastLoggedActiveMaterialKey,
            ShuttleSurfaceShieldVisualConfigSnapshot config)
        {
            if (!Prefs.DevMode || config == null)
            {
                return;
            }

            string key =
                (config.ModuleInstanceID ?? "<none>") + "|" +
                (config.SurfaceShaderBundlePath ?? "<null>") + "|" +
                (config.SurfaceMaterialAssetName ?? "<null>");
            if (lastLoggedActiveMaterialKey == key)
            {
                return;
            }

            lastLoggedActiveMaterialKey = key;
            ShuttleLog.Debug(
                "SurfaceShieldVisual",
                "Surface shield hull overlay material active. module=" +
                (config.ModuleInstanceID ?? "<none>") +
                ", bundle=" + (config.SurfaceShaderBundlePath ?? "<null>") +
                ", material=" + (config.SurfaceMaterialAssetName ?? "<null>"));
        }

        internal static void LogHullOverlayDrawFailedOnce(
            ref string lastLoggedDrawFailedMaterialKey,
            ShuttleSurfaceShieldVisualConfigSnapshot config)
        {
            if (!Prefs.DevMode || config == null)
            {
                return;
            }

            string key =
                (config.ModuleInstanceID ?? "<none>") + "|" +
                (config.SurfaceShaderBundlePath ?? "<null>") + "|" +
                (config.SurfaceMaterialAssetName ?? "<null>");
            if (lastLoggedDrawFailedMaterialKey == key)
            {
                return;
            }

            lastLoggedDrawFailedMaterialKey = key;
            ShuttleLog.Debug(
                "SurfaceShieldVisual",
                "Surface shield hull overlay material loaded, but masked draw failed; using existing shield fallback. module=" +
                (config.ModuleInstanceID ?? "<none>") +
                ", bundle=" + (config.SurfaceShaderBundlePath ?? "<null>") +
                ", material=" + (config.SurfaceMaterialAssetName ?? "<null>"));
        }

        internal static void LogHullOverlayGeometryModeOnce(
            ref bool loggedHullOverlayGeometryMode,
            Mesh mesh,
            Vector2 drawSize,
            Vector3 matrixScale)
        {
            if (!Prefs.DevMode || loggedHullOverlayGeometryMode)
            {
                return;
            }

            loggedHullOverlayGeometryMode = true;
            ShuttleLog.Debug(
                "SurfaceShieldVisual",
                "hull overlay using Graphic.MeshAt with identity matrix scale. mesh=" +
                (mesh != null ? mesh.name : "<null>") +
                ", drawSize=" + drawSize +
                ", matrixScale=" + matrixScale);
        }

        internal static void LogMissingHullTextureOnce(ref bool loggedMissingHullTexture)
        {
            if (!Prefs.DevMode || loggedMissingHullTexture)
            {
                return;
            }

            loggedMissingHullTexture = true;
            ShuttleLog.Debug(
                "SurfaceShieldVisual",
                "hull texture unavailable for masked overlay; using world fallback ripple when active.");
        }

        internal static void LogHullOverlayUnavailableOnce(ref bool loggedHullOverlayUnavailable)
        {
            if (!Prefs.DevMode || loggedHullOverlayUnavailable)
            {
                return;
            }

            loggedHullOverlayUnavailable = true;
            ShuttleLog.Debug(
                "SurfaceShieldVisual",
                "GPU hull edge glow unavailable; using existing shield fallback.");
        }

        internal static void LogHullAlphaMaskFailureOnce(
            bool devLogPulses,
            ref Texture2D lastLoggedUnreadableHullAlphaTexture,
            Texture2D texture,
            string reason)
        {
            if ((!Prefs.DevMode && !devLogPulses) || texture == null)
            {
                return;
            }

            if (lastLoggedUnreadableHullAlphaTexture == texture)
            {
                return;
            }

            lastLoggedUnreadableHullAlphaTexture = texture;
            ShuttleLog.Debug(
                "SurfaceShieldVisual",
                "hull alpha snap disabled for texture=" + texture.name +
                ", reason=" + (reason ?? "unknown") +
                "; using raw visual hit position.");
        }
    }
}

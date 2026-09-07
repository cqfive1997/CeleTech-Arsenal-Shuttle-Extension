using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    [StaticConstructorOnStartup]
    internal static class SurfaceShieldMaterialCache
    {
        private static Material fallbackPulseMaterial;
        private static Material fallbackHullOverlayMaterial;
        private static bool loggedMissingFallbackMaterial;

        static SurfaceShieldMaterialCache()
        {
        }

        internal static Texture2D TryGetCurrentHullTexture(CompModularShuttleSurfaceShieldVisual comp)
        {
            Material material = TryGetCurrentHullMaterial(comp);
            return material != null ? material.mainTexture as Texture2D : null;
        }

        internal static Graphic TryGetCurrentHullGraphic(CompModularShuttleSurfaceShieldVisual comp)
        {
            return comp.parent != null ? comp.parent.Graphic : null;
        }

        internal static Material TryGetCurrentHullMaterial(CompModularShuttleSurfaceShieldVisual comp)
        {
            Graphic graphic = TryGetCurrentHullGraphic(comp);
            if (comp.parent == null || graphic == null)
            {
                return null;
            }

            return graphic.MatAt(comp.parent.Rotation, comp.parent);
        }

        internal static Material GetFallbackPulseMaterial()
        {
            if (fallbackPulseMaterial != null)
            {
                return fallbackPulseMaterial;
            }

            if (ShaderDatabase.Transparent == null || !ShaderDatabase.Transparent.isSupported)
            {
                if (!loggedMissingFallbackMaterial && Prefs.DevMode)
                {
                    loggedMissingFallbackMaterial = true;
                    ShuttleLog.Debug(
                        "SurfaceShieldVisual",
                        "fallback pulse material unavailable because ShaderDatabase.Transparent is missing.");
                }

                return null;
            }

            fallbackPulseMaterial = new Material(ShaderDatabase.Transparent);
            fallbackPulseMaterial.name = "CeleTech_SurfaceShieldFallbackPulse";
            fallbackPulseMaterial.mainTexture = BaseContent.WhiteTex;
            fallbackPulseMaterial.color = Color.white;
            fallbackPulseMaterial.renderQueue = 3600;
            return fallbackPulseMaterial;
        }

        internal static Material GetFallbackHullOverlayMaterial()
        {
            if (fallbackHullOverlayMaterial != null)
            {
                return fallbackHullOverlayMaterial;
            }

            if (ShaderDatabase.Transparent == null || !ShaderDatabase.Transparent.isSupported)
            {
                return null;
            }

            fallbackHullOverlayMaterial = new Material(ShaderDatabase.Transparent);
            fallbackHullOverlayMaterial.name = "CeleTech_SurfaceShieldFallbackHullOverlay";
            fallbackHullOverlayMaterial.mainTexture = BaseContent.WhiteTex;
            fallbackHullOverlayMaterial.color = Color.white;
            fallbackHullOverlayMaterial.renderQueue = 3605;
            return fallbackHullOverlayMaterial;
        }
    }
}

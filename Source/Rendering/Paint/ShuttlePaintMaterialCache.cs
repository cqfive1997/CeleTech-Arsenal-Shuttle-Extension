using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.CustomShaders;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.Paint
{
    internal sealed class ShuttlePaintMaterialCache
    {
        private readonly ShuttlePaintMaterialBuilder materialBuilder;
        private readonly Dictionary<string, Material> cachedMaterials =
            new Dictionary<string, Material>();

        internal ShuttlePaintMaterialCache(ShuttlePaintMaterialBuilder materialBuilder)
        {
            this.materialBuilder = materialBuilder;
        }

        internal bool TryGetMaterial(ShuttlePaintVisualAssetSet assets, out Material material)
        {
            material = null;
            if (assets == null || !assets.IsComplete || this.materialBuilder == null)
            {
                return false;
            }

            string cacheKey = BuildCacheKey(assets);
            if (string.IsNullOrEmpty(cacheKey))
            {
                return false;
            }

            Material cached;
            if (this.cachedMaterials.TryGetValue(cacheKey, out cached))
            {
                if (cached != null)
                {
                    material = cached;
                    return true;
                }

                this.cachedMaterials.Remove(cacheKey);
            }

            Material created = new Material(assets.PaintTemplateMaterial);
            CustomShaderMaterialValidationResult validation =
                this.materialBuilder.ValidatePaintMaterial(created);
            if (!validation.IsValid)
            {
                ShuttleCustomShaderDiagnostics.LogMaterialValidation(
                    "paint",
                    ShuttlePaintShaderLoader.SelectedBundleRelativePath,
                    assets.PaintTemplateMaterial.name,
                    created,
                    validation,
                    "fallback");
                this.WarnOnce(
                    "unsupported-material|" + assets.PaintTemplateMaterial.name,
                    "Map paint overlay skipped because the paint template material is not using supported shader " +
                    ShuttlePaintMaterialBuilder.ExpectedPaintShaderName +
                    " or does not expose required properties " +
                    "(_MainTex, _PaintMaskTex, _PrimaryColor, _SecondaryColor, _AccentColor, _OverlayAlpha).");
                UnityEngine.Object.Destroy(created);
                return false;
            }

            ShuttleCustomShaderDiagnostics.LogMaterialValidation(
                "paint",
                ShuttlePaintShaderLoader.SelectedBundleRelativePath,
                assets.PaintTemplateMaterial.name,
                created,
                validation,
                "custom");
            this.materialBuilder.ConfigureMaterial(created, assets);
            this.cachedMaterials[cacheKey] = created;
            material = created;
            return true;
        }

        private static string BuildCacheKey(ShuttlePaintVisualAssetSet assets)
        {
            if (assets == null ||
                assets.PaintTemplateMaterial == null ||
                assets.PaintSourceTexture == null ||
                assets.PaintMaskTexture == null)
            {
                return null;
            }

            return assets.PaintTemplateMaterial.GetInstanceID().ToString() +
                "|" +
                assets.PaintSourceTexture.GetInstanceID().ToString() +
                "|" +
                assets.PaintMaskTexture.GetInstanceID().ToString();
        }

        private void WarnOnce(string key, string message)
        {
            ShuttleLog.WarnOnce("ShuttlePaintOverlay", key, message);
        }
    }
}

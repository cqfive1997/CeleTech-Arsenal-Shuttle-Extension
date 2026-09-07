using System;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.CustomShaders;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.Paint
{
    internal sealed class ShuttlePaintMaterialBuilder
    {
        internal const string ExpectedPaintShaderName = "Map/CMC_ShuttlePaint_RGBMask";

        private static readonly int MainTexProperty = Shader.PropertyToID("_MainTex");
        private static readonly int PaintMaskTexProperty = Shader.PropertyToID("_PaintMaskTex");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int ColorOneProperty = Shader.PropertyToID("_ColorOne");
        private static readonly int ColorTwoProperty = Shader.PropertyToID("_ColorTwo");
        private static readonly int ColorThreeProperty = Shader.PropertyToID("_ColorThree");
        private static readonly int PrimaryColorProperty = Shader.PropertyToID("_PrimaryColor");
        private static readonly int SecondaryColorProperty = Shader.PropertyToID("_SecondaryColor");
        private static readonly int AccentColorProperty = Shader.PropertyToID("_AccentColor");
        private static readonly int OverlayAlphaProperty = Shader.PropertyToID("_OverlayAlpha");
        private static readonly int PreviewOpaqueProperty = Shader.PropertyToID("_PreviewOpaque");
        private static readonly int PreviewBackgroundColorProperty = Shader.PropertyToID("_PreviewBackgroundColor");

        private static readonly Color PreviewBackgroundColor = new Color(0.035f, 0.04f, 0.045f, 1f);

        internal void ConfigureMaterial(Material material, ShuttlePaintVisualAssetSet assets)
        {
            if (material == null || assets == null)
            {
                return;
            }

            material.name = "CeleTech_ShuttlePaintMapOverlay";
            material.renderQueue = ShuttlePaintVisualAssetSet.OverlayRenderQueue;
            this.SetMaterialTextureIfSupported(material, MainTexProperty, assets.PaintSourceTexture);
            this.SetMaterialTextureIfSupported(material, PaintMaskTexProperty, assets.PaintMaskTexture);
            this.SetMaterialColorIfSupported(material, ColorProperty, Color.white);
            this.SetMaterialFloatIfSupported(material, PreviewOpaqueProperty, 0f);
            this.SetMaterialColorIfSupported(material, PreviewBackgroundColorProperty, PreviewBackgroundColor);
        }

        internal void ConfigurePropertyBlock(
            MaterialPropertyBlock block,
            ShuttlePaintVisualAssetSet assets,
            ShuttlePaintSchemeSnapshot scheme)
        {
            if (block == null || assets == null || scheme == null)
            {
                return;
            }

            block.Clear();
            block.SetTexture(MainTexProperty, assets.PaintSourceTexture);
            block.SetTexture(PaintMaskTexProperty, assets.PaintMaskTexture);
            block.SetColor(ColorProperty, Color.white);
            block.SetColor(ColorOneProperty, this.SanitizeColor(scheme.PrimaryColor));
            block.SetColor(ColorTwoProperty, this.SanitizeColor(scheme.SecondaryColor));
            block.SetColor(ColorThreeProperty, this.SanitizeColor(scheme.AccentColor));
            block.SetColor(PrimaryColorProperty, this.SanitizeColor(scheme.PrimaryColor));
            block.SetColor(SecondaryColorProperty, this.SanitizeColor(scheme.SecondaryColor));
            block.SetColor(AccentColorProperty, this.SanitizeColor(scheme.AccentColor));
            block.SetFloat(OverlayAlphaProperty, 1f);
            block.SetFloat(PreviewOpaqueProperty, 0f);
            block.SetColor(PreviewBackgroundColorProperty, PreviewBackgroundColor);
        }

        internal bool SupportsRequiredPaintProperties(Material material)
        {
            return this.ValidatePaintMaterial(material).IsValid;
        }

        internal bool UsesSupportedPaintShader(Material material)
        {
            return this.ValidatePaintMaterial(material).IsValid;
        }

        internal CustomShaderMaterialValidationResult ValidatePaintMaterial(
            Material material)
        {
            return UnityCustomShaderMaterialValidator.Validate(
                material,
                ShuttleCustomShaderMaterialContracts.Paint);
        }

        private void SetMaterialTextureIfSupported(Material material, int propertyId, Texture texture)
        {
            if (material.HasProperty(propertyId))
            {
                material.SetTexture(propertyId, texture);
            }
        }

        private void SetMaterialColorIfSupported(Material material, int propertyId, Color color)
        {
            if (material.HasProperty(propertyId))
            {
                material.SetColor(propertyId, color);
            }
        }

        private void SetMaterialFloatIfSupported(Material material, int propertyId, float value)
        {
            if (material.HasProperty(propertyId))
            {
                material.SetFloat(propertyId, value);
            }
        }

        private Color SanitizeColor(Color color)
        {
            return new Color(
                Mathf.Clamp01(color.r),
                Mathf.Clamp01(color.g),
                Mathf.Clamp01(color.b),
                1f);
        }
    }
}

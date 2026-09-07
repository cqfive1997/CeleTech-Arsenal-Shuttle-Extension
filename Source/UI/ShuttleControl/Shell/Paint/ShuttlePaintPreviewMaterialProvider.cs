using CeleTech.ShuttleExtension.ModularShuttle.Rendering.Paint;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Paint
{
    internal sealed class ShuttlePaintPreviewMaterialProvider
    {
        private static readonly int PrimaryColorProperty = Shader.PropertyToID("_PrimaryColor");
        private static readonly int SecondaryColorProperty = Shader.PropertyToID("_SecondaryColor");
        private static readonly int AccentColorProperty = Shader.PropertyToID("_AccentColor");
        private static readonly int OverlayAlphaProperty = Shader.PropertyToID("_OverlayAlpha");
        private static readonly int PreviewOpaqueProperty = Shader.PropertyToID("_PreviewOpaque");
        private static readonly int PreviewBackgroundColorProperty = Shader.PropertyToID("_PreviewBackgroundColor");

        private static readonly Color PreviewBackgroundColor = new Color(0.04f, 0.06f, 0.08f, 1f);

        private readonly ShuttlePaintMaterialBuilder materialBuilder = new ShuttlePaintMaterialBuilder();
        private Material runtimeMaterial;
        private Material templateMaterial;
        private Texture2D paintSourceTexture;
        private Texture2D paintMaskTexture;

        internal bool TryGetMaterial(
            Texture2D paintSource,
            Texture2D paintMask,
            ShuttlePaintSchemeSnapshot paintScheme,
            out Material material)
        {
            return this.TryGetMaterial(
                paintSource,
                paintMask,
                paintScheme,
                true,
                out material);
        }

        internal bool TryGetMaterial(
            Texture2D paintSource,
            Texture2D paintMask,
            ShuttlePaintSchemeSnapshot paintScheme,
            bool previewOpaque,
            out Material material)
        {
            material = null;
            if (paintSource == null || paintMask == null || paintScheme == null)
            {
                return false;
            }

            Material currentTemplate;
            if (!ShuttlePaintShaderLoader.TryLoadTemplateMaterial(out currentTemplate) || currentTemplate == null)
            {
                return false;
            }

            if (this.runtimeMaterial == null ||
                this.templateMaterial != currentTemplate ||
                this.paintSourceTexture != paintSource ||
                this.paintMaskTexture != paintMask)
            {
                this.ReleaseRuntimeMaterial();
                this.runtimeMaterial = new Material(currentTemplate);
                this.templateMaterial = currentTemplate;
                this.paintSourceTexture = paintSource;
                this.paintMaskTexture = paintMask;

                ShuttlePaintVisualAssetSet assets = new ShuttlePaintVisualAssetSet(
                    paintSource,
                    paintMask,
                    currentTemplate);
                if (!this.materialBuilder.SupportsRequiredPaintProperties(this.runtimeMaterial))
                {
                    this.ReleaseRuntimeMaterial();
                    return false;
                }

                this.materialBuilder.ConfigureMaterial(this.runtimeMaterial, assets);
            }

            this.SetPaintColors(this.runtimeMaterial, paintScheme, previewOpaque);
            material = this.runtimeMaterial;
            return material != null;
        }

        internal void Release()
        {
            this.ReleaseRuntimeMaterial();
        }

        private void SetPaintColors(
            Material material,
            ShuttlePaintSchemeSnapshot paintScheme,
            bool previewOpaque)
        {
            this.SetColorIfSupported(material, PrimaryColorProperty, this.SanitizeColor(paintScheme.PrimaryColor));
            this.SetColorIfSupported(material, SecondaryColorProperty, this.SanitizeColor(paintScheme.SecondaryColor));
            this.SetColorIfSupported(material, AccentColorProperty, this.SanitizeColor(paintScheme.AccentColor));
            this.SetFloatIfSupported(material, OverlayAlphaProperty, paintScheme.Enabled ? 1f : 0f);
            this.SetFloatIfSupported(material, PreviewOpaqueProperty, previewOpaque ? 1f : 0f);
            this.SetColorIfSupported(material, PreviewBackgroundColorProperty, PreviewBackgroundColor);
        }

        private void SetColorIfSupported(Material material, int propertyId, Color color)
        {
            if (material != null && material.HasProperty(propertyId))
            {
                material.SetColor(propertyId, color);
            }
        }

        private void SetFloatIfSupported(Material material, int propertyId, float value)
        {
            if (material != null && material.HasProperty(propertyId))
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

        private void ReleaseRuntimeMaterial()
        {
            if (this.runtimeMaterial != null)
            {
                Object.Destroy(this.runtimeMaterial);
            }

            this.runtimeMaterial = null;
            this.templateMaterial = null;
            this.paintSourceTexture = null;
            this.paintMaskTexture = null;
        }
    }
}

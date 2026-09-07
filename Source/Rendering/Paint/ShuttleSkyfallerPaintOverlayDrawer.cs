using CeleTech.ShuttleExtension.ModularShuttle.Rendering.SkyfallerDeploy;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.Paint
{
    [StaticConstructorOnStartup]
    internal static class ShuttleSkyfallerPaintOverlayDrawer
    {
        private static readonly ShuttlePaintTextureResolver TextureResolver =
            new ShuttlePaintTextureResolver();

        private static readonly ShuttlePaintMaterialBuilder MaterialBuilder =
            new ShuttlePaintMaterialBuilder();

        private static readonly ShuttlePaintMaterialCache MaterialCache =
            new ShuttlePaintMaterialCache(MaterialBuilder);

        private static MaterialPropertyBlock propertyBlock;
        private static ShuttlePaintVisualAssetSet cachedAssetSet;

        static ShuttleSkyfallerPaintOverlayDrawer()
        {
        }

        internal static bool Draw(
            Skyfaller skyfaller,
            Vector3 drawLoc,
            Rot4 drawRotation,
            float extraRotation,
            ShuttlePaintSchemeSnapshot paintScheme)
        {
            if (skyfaller == null || paintScheme == null || !paintScheme.Enabled)
            {
                return false;
            }

            ShuttlePaintVisualAssetSet assets;
            if (!TryResolveAssets(drawRotation, out assets))
            {
                return false;
            }

            Material material;
            if (!MaterialCache.TryGetMaterial(assets, out material) || material == null)
            {
                return false;
            }

            ShuttleSkyfallerDeployDrawParms drawParms;
            if (!ShuttleSkyfallerDeployDrawUtility.TryBuildDrawParms(
                    skyfaller,
                    drawLoc,
                    drawRotation,
                    extraRotation,
                    Vector3.zero,
                    out drawParms))
            {
                return false;
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            MaterialBuilder.ConfigurePropertyBlock(propertyBlock, assets, paintScheme);
            Matrix4x4 matrix = drawParms.Matrix;
            matrix.m13 += ShuttlePaintVisualAssetSet.OverlayHeightOffset;

            Graphics.DrawMesh(
                drawParms.Mesh,
                matrix,
                material,
                0,
                null,
                0,
                propertyBlock);
            return true;
        }

        private static bool TryResolveAssets(Rot4 drawRotation, out ShuttlePaintVisualAssetSet assets)
        {
            assets = null;

            Texture2D paintSourceTexture;
            Texture2D paintMaskTexture;
            if (!TextureResolver.TryResolve(drawRotation, out paintSourceTexture, out paintMaskTexture))
            {
                return false;
            }

            Material paintTemplateMaterial;
            if (!ShuttlePaintShaderLoader.TryLoadTemplateMaterial(out paintTemplateMaterial))
            {
                return false;
            }

            if (cachedAssetSet == null ||
                cachedAssetSet.PaintSourceTexture != paintSourceTexture ||
                cachedAssetSet.PaintMaskTexture != paintMaskTexture ||
                cachedAssetSet.PaintTemplateMaterial != paintTemplateMaterial)
            {
                cachedAssetSet = new ShuttlePaintVisualAssetSet(
                    paintSourceTexture,
                    paintMaskTexture,
                    paintTemplateMaterial);
            }

            assets = cachedAssetSet;
            return assets.IsComplete;
        }
    }
}

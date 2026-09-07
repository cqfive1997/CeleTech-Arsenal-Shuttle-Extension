using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.Paint
{
    [StaticConstructorOnStartup]
    internal static class ShuttlePaintMapOverlayDrawer
    {
        private static readonly ShuttlePaintTextureResolver TextureResolver =
            new ShuttlePaintTextureResolver();

        private static readonly ShuttlePaintMaterialBuilder MaterialBuilder =
            new ShuttlePaintMaterialBuilder();

        private static readonly ShuttlePaintMaterialCache MaterialCache =
            new ShuttlePaintMaterialCache(MaterialBuilder);

        private static MaterialPropertyBlock propertyBlock;
        private static ShuttlePaintVisualAssetSet cachedAssetSet;

        static ShuttlePaintMapOverlayDrawer()
        {
        }

        internal static bool Draw(ThingWithComps parent, ShuttlePaintSchemeSnapshot paintScheme)
        {
            if (parent == null || !parent.Spawned || paintScheme == null || !paintScheme.Enabled)
            {
                return false;
            }

            Graphic graphic = parent.Graphic;
            if (graphic == null)
            {
                WarnOnce(
                    "missing-graphic",
                    "Map paint overlay skipped because parent.Graphic is null.");
                return false;
            }

            Mesh mesh = graphic.MeshAt(parent.Rotation);
            if (mesh == null)
            {
                WarnOnce(
                    "missing-mesh",
                    "Map paint overlay skipped because Graphic.MeshAt returned null.");
                return false;
            }

            ShuttlePaintVisualAssetSet assets;
            if (!TryResolveAssets(parent.Rotation, out assets))
            {
                return false;
            }

            Material material;
            if (!MaterialCache.TryGetMaterial(assets, out material) || material == null)
            {
                return false;
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            MaterialBuilder.ConfigurePropertyBlock(propertyBlock, assets, paintScheme);
            Matrix4x4 matrix = Matrix4x4.TRS(
                GetDrawLoc(parent, graphic),
                GetDrawRotation(parent, graphic),
                Vector3.one);

            Graphics.DrawMesh(
                mesh,
                matrix,
                material,
                0,
                null,
                0,
                propertyBlock);
            return true;
        }

        private static bool TryResolveAssets(Rot4 rotation, out ShuttlePaintVisualAssetSet assets)
        {
            assets = null;

            Texture2D paintSourceTexture;
            Texture2D paintMaskTexture;
            if (!TextureResolver.TryResolve(rotation, out paintSourceTexture, out paintMaskTexture))
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

        private static Vector3 GetDrawLoc(ThingWithComps parent, Graphic graphic)
        {
            Vector3 drawLoc = parent.DrawPos;
            drawLoc += graphic.DrawOffset(parent.Rotation);
            drawLoc.y += ShuttlePaintVisualAssetSet.OverlayHeightOffset;
            return drawLoc;
        }

        private static Quaternion GetDrawRotation(ThingWithComps parent, Graphic graphic)
        {
            Quaternion rotation = graphic.QuatFromRot(parent.Rotation);
            if (graphic.data != null && graphic.data.addTopAltitudeBias)
            {
                rotation *= Quaternion.Euler(Vector3.left * 2f);
            }

            return rotation;
        }

        private static void WarnOnce(string key, string message)
        {
            if (Prefs.DevMode)
            {
                ShuttleLog.WarnOnce("ShuttlePaintOverlay", key, message);
            }
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.Rendering.Paint;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Paint
{
    internal sealed class ShuttlePaintPreviewTextureResolver
    {
        private readonly ShuttlePaintPreviewSourceKind sourceKind;
        private bool resolved;
        private int resolvedRotationIndex = -1;
        private Texture2D paintSourceTexture;
        private Texture2D paintMaskTexture;

        internal ShuttlePaintPreviewTextureResolver()
            : this(ShuttlePaintPreviewSourceKind.Body)
        {
        }

        internal ShuttlePaintPreviewTextureResolver(ShuttlePaintPreviewSourceKind sourceKind)
        {
            this.sourceKind = sourceKind;
        }

        internal bool TryResolve(out Texture2D paintSource, out Texture2D paintMask)
        {
            return this.TryResolve(Rot4.East, out paintSource, out paintMask);
        }

        internal bool TryResolve(Rot4 rotation, out Texture2D paintSource, out Texture2D paintMask)
        {
            if (!this.resolved ||
                this.resolvedRotationIndex != rotation.AsInt ||
                this.paintSourceTexture == null ||
                this.paintMaskTexture == null)
            {
                this.paintSourceTexture = ContentFinder<Texture2D>.Get(
                    this.GetPaintSourceTexturePath(rotation),
                    false);
                this.paintMaskTexture = ContentFinder<Texture2D>.Get(
                    ShuttlePaintVisualAssetSet.GetPaintMaskTexturePath(rotation),
                    false);
                this.resolved = this.paintSourceTexture != null && this.paintMaskTexture != null;
                this.resolvedRotationIndex = rotation.AsInt;
            }

            paintSource = this.paintSourceTexture;
            paintMask = this.paintMaskTexture;
            return paintSource != null && paintMask != null;
        }

        private string GetPaintSourceTexturePath(Rot4 rotation)
        {
            if (this.sourceKind == ShuttlePaintPreviewSourceKind.LandedBrowser)
            {
                return ShuttlePaintVisualAssetSet.GetLandedBrowserPaintSourceTexturePath(rotation);
            }

            return ShuttlePaintVisualAssetSet.GetPaintSourceTexturePath(rotation);
        }
    }
}

using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Paint
{
    internal sealed class ShuttlePaintPreviewRenderTextureCache
    {
        internal const int PreviewWidth = 768;
        internal const int PreviewHeight = 540;

        private readonly int previewWidth;
        private readonly int previewHeight;
        private readonly string textureName;
        private RenderTexture previewTexture;

        internal ShuttlePaintPreviewRenderTextureCache()
            : this(PreviewWidth, PreviewHeight, "CeleTech_ShuttlePaintPreview")
        {
        }

        internal ShuttlePaintPreviewRenderTextureCache(int previewWidth, int previewHeight, string textureName)
        {
            this.previewWidth = Mathf.Max(1, previewWidth);
            this.previewHeight = Mathf.Max(1, previewHeight);
            this.textureName = string.IsNullOrEmpty(textureName)
                ? "CeleTech_ShuttlePaintPreview"
                : textureName;
        }

        internal RenderTexture GetOrCreate()
        {
            if (this.previewTexture != null &&
                this.previewTexture.width == this.previewWidth &&
                this.previewTexture.height == this.previewHeight)
            {
                return this.previewTexture;
            }

            this.Release();
            this.previewTexture = new RenderTexture(
                this.previewWidth,
                this.previewHeight,
                0,
                RenderTextureFormat.ARGB32);
            this.previewTexture.name = this.textureName;
            this.previewTexture.Create();
            return this.previewTexture;
        }

        internal void Release()
        {
            if (this.previewTexture == null)
            {
                return;
            }

            this.previewTexture.Release();
            Object.Destroy(this.previewTexture);
            this.previewTexture = null;
        }
    }
}

using System;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Paint
{
    internal sealed class ShuttlePaintPreviewBaker : IDisposable
    {
        private const float ThrottledRebuildDelaySeconds = 0.125f;

        private readonly ShuttlePaintPreviewTextureResolver textureResolver;
        private readonly ShuttlePaintPreviewMaterialProvider materialProvider;
        private readonly ShuttlePaintPreviewRenderTextureCache renderTextureCache;
        private readonly ShuttlePaintPreviewLayerDrawer layerDrawer =
            new ShuttlePaintPreviewLayerDrawer();
        private readonly ShuttlePaintPreviewSourceKind sourceKind;

        private ShuttlePaintSchemeSnapshot pendingScheme;
        private ShuttlePaintSchemeSnapshot lastBuiltScheme;
        private int pendingWeaponStationMask;
        private int lastBuiltWeaponStationMask;
        private Rot4 pendingRotation = Rot4.East;
        private Rot4 lastBuiltRotation = Rot4.East;
        private bool rebuildRequested;
        private float rebuildAfterRealtime;
        private bool hasPreview;

        internal ShuttlePaintPreviewBaker()
            : this(
                ShuttlePaintPreviewSourceKind.Body,
                ShuttlePaintPreviewRenderTextureCache.PreviewWidth,
                ShuttlePaintPreviewRenderTextureCache.PreviewHeight,
                "CeleTech_ShuttlePaintPreview")
        {
        }

        internal ShuttlePaintPreviewBaker(
            ShuttlePaintPreviewSourceKind sourceKind,
            int previewWidth,
            int previewHeight,
            string textureName)
        {
            this.sourceKind = sourceKind;
            this.textureResolver = new ShuttlePaintPreviewTextureResolver(sourceKind);
            this.materialProvider = new ShuttlePaintPreviewMaterialProvider();
            this.renderTextureCache = new ShuttlePaintPreviewRenderTextureCache(
                previewWidth,
                previewHeight,
                textureName);
        }

        internal Texture PreviewTexture
        {
            get
            {
                return this.renderTextureCache.GetOrCreate();
            }
        }

        internal bool HasPreview
        {
            get
            {
                return this.hasPreview;
            }
        }

        internal void RequestRebuild(ShuttlePaintSchemeSnapshot scheme, bool throttled)
        {
            this.RequestRebuild(scheme, throttled, 0);
        }

        internal void RequestRebuild(
            ShuttlePaintSchemeSnapshot scheme,
            bool throttled,
            int weaponStationMask)
        {
            this.RequestRebuild(scheme, throttled, weaponStationMask, Rot4.East);
        }

        internal void RequestRebuild(
            ShuttlePaintSchemeSnapshot scheme,
            bool throttled,
            int weaponStationMask,
            Rot4 rotation)
        {
            this.pendingScheme = scheme != null ? scheme.Clone() : ShuttlePaintSchemeSnapshot.Default;
            this.pendingWeaponStationMask = weaponStationMask;
            this.pendingRotation = rotation;
            this.rebuildRequested = true;
            this.rebuildAfterRealtime = Time.realtimeSinceStartup +
                (throttled ? ThrottledRebuildDelaySeconds : 0f);
        }

        internal bool TryRebuildIfDue()
        {
            if (!this.rebuildRequested ||
                Time.realtimeSinceStartup < this.rebuildAfterRealtime ||
                Event.current == null ||
                Event.current.type != EventType.Repaint)
            {
                return false;
            }

            this.RebuildNow();
            return true;
        }

        internal void RebuildNow()
        {
            RenderTexture preview = this.renderTextureCache.GetOrCreate();
            RenderTexture oldActive = RenderTexture.active;

            try
            {
                RenderTexture.active = preview;
                GL.Clear(true, true, Color.clear);

                ShuttlePaintSchemeSnapshot scheme = this.pendingScheme != null
                    ? this.pendingScheme
                    : this.lastBuiltScheme ?? ShuttlePaintSchemeSnapshot.Default;
                int weaponStationMask = this.pendingScheme != null
                    ? this.pendingWeaponStationMask
                    : this.lastBuiltWeaponStationMask;
                Rot4 rotation = this.pendingScheme != null
                    ? this.pendingRotation
                    : this.lastBuiltRotation;

                Texture2D paintSource;
                Texture2D paintMask;
                if (!this.textureResolver.TryResolve(rotation, out paintSource, out paintMask) ||
                    paintSource == null)
                {
                    this.hasPreview = false;
                    this.rebuildRequested = false;
                    return;
                }

                if (this.sourceKind == ShuttlePaintPreviewSourceKind.LandedBrowser)
                {
                    this.layerDrawer.DrawLandedUnderlay(
                        preview,
                        paintSource,
                        weaponStationMask,
                        rotation);
                    this.layerDrawer.DrawBody(preview, paintSource);
                }

                Material material;
                if (paintMask != null &&
                    this.materialProvider.TryGetMaterial(
                        paintSource,
                        paintMask,
                        scheme,
                        this.sourceKind != ShuttlePaintPreviewSourceKind.LandedBrowser,
                        out material))
                {
                    if (this.sourceKind == ShuttlePaintPreviewSourceKind.LandedBrowser)
                    {
                        this.layerDrawer.DrawPaintOverlay(preview, paintSource, material);
                    }
                    else
                    {
                        Graphics.Blit(paintSource, preview, material);
                    }
                }
                else if (this.sourceKind != ShuttlePaintPreviewSourceKind.LandedBrowser)
                {
                    Graphics.Blit(paintSource, preview);
                }

                this.lastBuiltScheme = scheme.Clone();
                this.lastBuiltWeaponStationMask = weaponStationMask;
                this.lastBuiltRotation = rotation;
                this.pendingScheme = null;
                this.pendingWeaponStationMask = 0;
                this.pendingRotation = Rot4.East;
                this.rebuildRequested = false;
                this.hasPreview = true;
            }
            finally
            {
                RenderTexture.active = oldActive;
            }
        }

        public void Dispose()
        {
            this.materialProvider.Release();
            this.renderTextureCache.Release();
            this.pendingScheme = null;
            this.lastBuiltScheme = null;
            this.pendingWeaponStationMask = 0;
            this.lastBuiltWeaponStationMask = 0;
            this.pendingRotation = Rot4.East;
            this.lastBuiltRotation = Rot4.East;
            this.rebuildRequested = false;
            this.hasPreview = false;
        }
    }
}

using System;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Paint
{
    internal sealed class ShuttlePaintBrowserPreviewProvider : IDisposable
    {
        private const int BrowserPreviewWidth = 512;
        private const int BrowserPreviewHeight = 360;

        private readonly ShuttlePaintPreviewBaker baker =
            new ShuttlePaintPreviewBaker(
                ShuttlePaintPreviewSourceKind.LandedBrowser,
                BrowserPreviewWidth,
                BrowserPreviewHeight,
                "CeleTech_ShuttlePaintBrowserPreview");

        private PaintPreviewKey lastKey;
        private bool hasLastKey;

        internal bool TryGetPreview(ShuttlePaintSchemeSnapshot paintScheme, out Texture texture)
        {
            return this.TryGetPreview(paintScheme, 0, out texture);
        }

        internal bool TryGetPreview(
            ShuttlePaintSchemeSnapshot paintScheme,
            int weaponStationMask,
            out Texture texture)
        {
            return this.TryGetPreview(
                paintScheme,
                weaponStationMask,
                Rot4.East,
                out texture);
        }

        internal bool TryGetPreview(
            ShuttlePaintSchemeSnapshot paintScheme,
            int weaponStationMask,
            Rot4 rotation,
            out Texture texture)
        {
            texture = null;

            ShuttlePaintSchemeSnapshot safeScheme =
                paintScheme != null ? paintScheme : ShuttlePaintSchemeSnapshot.Default;
            PaintPreviewKey key = PaintPreviewKey.FromScheme(
                safeScheme,
                ShuttlePaintPreviewSourceKind.LandedBrowser,
                weaponStationMask,
                rotation);

            if (!this.hasLastKey || !this.lastKey.EqualsTo(key) || !this.baker.HasPreview)
            {
                this.lastKey = key;
                this.hasLastKey = true;
                this.baker.RequestRebuild(safeScheme, false, weaponStationMask, rotation);
            }

            this.baker.TryRebuildIfDue();
            if (!this.baker.HasPreview)
            {
                return false;
            }

            texture = this.baker.PreviewTexture;
            return texture != null;
        }

        public void Dispose()
        {
            this.baker.Dispose();
            this.hasLastKey = false;
        }

        private struct PaintPreviewKey
        {
            private bool enabled;
            private Color primaryColor;
            private Color secondaryColor;
            private Color accentColor;
            private ShuttlePaintPreviewSourceKind sourceKind;
            private int weaponStationMask;
            private int rotationIndex;

            internal static PaintPreviewKey FromScheme(
                ShuttlePaintSchemeSnapshot paintScheme,
                ShuttlePaintPreviewSourceKind sourceKind,
                int weaponStationMask,
                Rot4 rotation)
            {
                ShuttlePaintSchemeSnapshot safeScheme =
                    paintScheme != null ? paintScheme : ShuttlePaintSchemeSnapshot.Default;
                PaintPreviewKey key = new PaintPreviewKey();
                key.enabled = safeScheme.Enabled;
                key.primaryColor = safeScheme.PrimaryColor;
                key.secondaryColor = safeScheme.SecondaryColor;
                key.accentColor = safeScheme.AccentColor;
                key.sourceKind = sourceKind;
                key.weaponStationMask = weaponStationMask;
                key.rotationIndex = rotation.AsInt;
                return key;
            }

            internal bool EqualsTo(PaintPreviewKey other)
            {
                return this.enabled == other.enabled &&
                    this.sourceKind == other.sourceKind &&
                    this.weaponStationMask == other.weaponStationMask &&
                    this.rotationIndex == other.rotationIndex &&
                    SameColor(this.primaryColor, other.primaryColor) &&
                    SameColor(this.secondaryColor, other.secondaryColor) &&
                    SameColor(this.accentColor, other.accentColor);
            }

            private static bool SameColor(Color left, Color right)
            {
                const float Epsilon = 0.0001f;
                return Mathf.Abs(left.r - right.r) <= Epsilon &&
                    Mathf.Abs(left.g - right.g) <= Epsilon &&
                    Mathf.Abs(left.b - right.b) <= Epsilon &&
                    Mathf.Abs(left.a - right.a) <= Epsilon;
            }
        }
    }
}

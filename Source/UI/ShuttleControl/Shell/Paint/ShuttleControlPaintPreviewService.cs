using System;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Paint
{
    internal sealed class ShuttleControlPaintPreviewService :
        IShuttlePaintPreviewService,
        IDisposable
    {
        private readonly ShuttlePaintBrowserPreviewProvider paintPreviewProvider;

        internal ShuttleControlPaintPreviewService()
            : this(new ShuttlePaintBrowserPreviewProvider())
        {
        }

        internal ShuttleControlPaintPreviewService(
            ShuttlePaintBrowserPreviewProvider paintPreviewProvider)
        {
            this.paintPreviewProvider = paintPreviewProvider;
        }

        public bool TryGetPreview(ShuttlePaintSchemeSnapshot paintScheme, out Texture texture)
        {
            return this.TryGetPreview(paintScheme, 0, out texture);
        }

        public bool TryGetPreview(
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

        public bool TryGetPreview(
            ShuttlePaintSchemeSnapshot paintScheme,
            int weaponStationMask,
            Rot4 rotation,
            out Texture texture)
        {
            if (this.paintPreviewProvider != null)
            {
                return this.paintPreviewProvider.TryGetPreview(
                    paintScheme,
                    weaponStationMask,
                    rotation,
                    out texture);
            }

            texture = null;
            return false;
        }

        public void Dispose()
        {
            if (this.paintPreviewProvider != null)
            {
                this.paintPreviewProvider.Dispose();
            }
        }
    }
}

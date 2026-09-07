using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.Paint
{
    internal sealed class ShuttlePaintVisualAssetSet
    {
        internal const string PaintSourceTexturePath =
            "Things/Building/ModularShuttle/KunPeng/CAT_pelican";

        internal const string LandedBrowserPaintSourceTexturePath =
            "Things/Building/ModularShuttle/KunPeng/KunPengShuttleSkyfaller_east";

        internal const string PaintMaskTexturePath =
            "Things/Building/ModularShuttle/KunPeng/CAT_pelican_paintmask";

        internal const string NorthPaintSourceTexturePath =
            "Things/Building/ModularShuttle/KunPeng/KunPengShuttleSkyfaller_north";

        internal const string NorthPaintMaskTexturePath =
            "Things/Building/ModularShuttle/KunPeng/CAT_north_paintmask";

        internal const string WestPaintSourceTexturePath =
            "Things/Building/ModularShuttle/KunPeng/KunPengShuttleSkyfaller_west";

        internal const string WestPaintMaskTexturePath =
            "Things/Building/ModularShuttle/KunPeng/CAT_west_paintmask";

        internal const string SouthPaintSourceTexturePath =
            "Things/Building/ModularShuttle/KunPeng/KunPengShuttleSkyfaller_south";

        internal const string SouthPaintMaskTexturePath =
            "Things/Building/ModularShuttle/KunPeng/CAT_south_paintmask";

        internal const float OverlayHeightOffset = 0.025f;
        internal const int OverlayRenderQueue = 3550;

        internal ShuttlePaintVisualAssetSet(
            Texture2D paintSourceTexture,
            Texture2D paintMaskTexture,
            Material paintTemplateMaterial)
        {
            this.PaintSourceTexture = paintSourceTexture;
            this.PaintMaskTexture = paintMaskTexture;
            this.PaintTemplateMaterial = paintTemplateMaterial;
        }

        internal Texture2D PaintSourceTexture { get; private set; }

        internal Texture2D PaintMaskTexture { get; private set; }

        internal Material PaintTemplateMaterial { get; private set; }

        internal bool IsComplete
        {
            get
            {
                return this.PaintSourceTexture != null &&
                    this.PaintMaskTexture != null &&
                    this.PaintTemplateMaterial != null;
            }
        }

        internal static string GetPaintSourceTexturePath(Rot4 rotation)
        {
            if (rotation == Rot4.North)
            {
                return NorthPaintSourceTexturePath;
            }

            if (rotation == Rot4.West)
            {
                return WestPaintSourceTexturePath;
            }

            return rotation == Rot4.South
                ? SouthPaintSourceTexturePath
                : PaintSourceTexturePath;
        }

        internal static string GetLandedBrowserPaintSourceTexturePath(Rot4 rotation)
        {
            if (rotation == Rot4.North)
            {
                return NorthPaintSourceTexturePath;
            }

            if (rotation == Rot4.West)
            {
                return WestPaintSourceTexturePath;
            }

            return rotation == Rot4.South
                ? SouthPaintSourceTexturePath
                : LandedBrowserPaintSourceTexturePath;
        }

        internal static string GetPaintMaskTexturePath(Rot4 rotation)
        {
            if (rotation == Rot4.North)
            {
                return NorthPaintMaskTexturePath;
            }

            if (rotation == Rot4.West)
            {
                return WestPaintMaskTexturePath;
            }

            return rotation == Rot4.South
                ? SouthPaintMaskTexturePath
                : PaintMaskTexturePath;
        }
    }
}

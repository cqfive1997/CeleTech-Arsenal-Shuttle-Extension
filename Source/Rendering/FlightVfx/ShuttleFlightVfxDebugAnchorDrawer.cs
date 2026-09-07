using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxDebugAnchorDrawer
    {
        internal static readonly ShuttleFlightVfxDebugAnchorDrawer Shared =
            new ShuttleFlightVfxDebugAnchorDrawer(
                ShuttleFlightVfxDebugMaterialCache.Shared,
                ShuttleFlightVfxDebugDirectionDrawer.Shared);

        private const float MarkerYOffset = 2f;

        private readonly ShuttleFlightVfxDebugMaterialCache materialCache;
        private readonly ShuttleFlightVfxDebugDirectionDrawer directionDrawer;

        private ShuttleFlightVfxDebugAnchorDrawer(
            ShuttleFlightVfxDebugMaterialCache materialCache,
            ShuttleFlightVfxDebugDirectionDrawer directionDrawer)
        {
            this.materialCache = materialCache;
            this.directionDrawer = directionDrawer;
        }

        internal void Draw(Skyfaller skyfaller, Vector3 drawLoc, Rot4 drawRotation, float extraRotation)
        {
            if (!ShuttleFlightVfxDebugSettings.ShouldDrawThrusterAnchors ||
                skyfaller == null ||
                skyfaller.Graphic == null ||
                this.materialCache == null ||
                this.directionDrawer == null)
            {
                return;
            }

            ShuttleThrusterLayout layout = ShuttleKunPengThrusterLayoutProvider.Layout;
            if (layout == null || layout.Count == 0)
            {
                return;
            }

            Vector2 drawSize = ShuttleFlightVfxDrawSizeResolver.ResolveDrawSize(skyfaller);
            Vector3 origin = drawLoc + skyfaller.Graphic.DrawOffset(drawRotation);

            for (int i = 0; i < layout.Count; i++)
            {
                this.DrawAnchor(
                    layout.GetAnchor(i),
                    drawSize,
                    origin,
                    drawRotation,
                    extraRotation);
            }
        }

        private void DrawAnchor(
            ShuttleThrusterAnchor anchor,
            Vector2 drawSize,
            Vector3 origin,
            Rot4 drawRotation,
            float extraRotation)
        {
            Material material = this.materialCache.GetMaterial(anchor.Kind);
            if (material == null || material == BaseContent.BadMat)
            {
                return;
            }

            Vector3 position = origin +
                ShuttleFlightVfxAnchorMapper.ResolveAnchorWorldOffset(
                    anchor,
                    drawSize,
                    drawRotation,
                    extraRotation);
            position.y += MarkerYOffset;

            float markerSize = Mathf.Max(0.02f, anchor.MarkerSize);
            Quaternion markerRotation =
                ShuttleFlightVfxAnchorMapper.EastAuthoredRotation(
                    drawRotation,
                    extraRotation);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(position, markerRotation, new Vector3(markerSize, 1f, markerSize));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);

            Vector3 worldDirection =
                ShuttleFlightVfxAnchorMapper.ResolveAnchorWorldDirection(
                    anchor,
                    drawRotation,
                    extraRotation);
            this.directionDrawer.Draw(
                position,
                Quaternion.identity,
                new Vector2(worldDirection.x, worldDirection.z),
                markerSize * 1.8f,
                markerSize,
                material);
        }
    }
}

using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.SkyfallerDeploy
{
    internal sealed class ShuttleSkyfallerDeployVisualDrawer
    {
        internal static readonly ShuttleSkyfallerDeployVisualDrawer Shared =
            new ShuttleSkyfallerDeployVisualDrawer(ShuttleSkyfallerDeployLayerDrawer.Shared);

        private const float GearRetractedDistance = 0.32f;
        private const float LandingGearYOffset = -0.03f;

        private readonly ShuttleSkyfallerDeployLayerDrawer layerDrawer;

        private ShuttleSkyfallerDeployVisualDrawer(ShuttleSkyfallerDeployLayerDrawer layerDrawer)
        {
            this.layerDrawer = layerDrawer;
        }

        internal void DrawLandingGear(
            Skyfaller skyfaller,
            Vector3 drawLoc,
            Rot4 drawRotation,
            float extraRotation,
            float progress,
            ShuttleSkyfallerDeployVisualAssets assets)
        {
            this.DrawLayer(
                skyfaller,
                drawLoc,
                drawRotation,
                extraRotation,
                progress,
                assets != null ? assets.LandingGearMaterialFor(drawRotation) : null,
                ResolveGearRetractedOffset(drawRotation),
                LandingGearYOffset);
        }

        private static Vector3 ResolveGearRetractedOffset(Rot4 drawRotation)
        {
            if (drawRotation == Rot4.South)
            {
                return new Vector3(0f, 0f, -GearRetractedDistance);
            }

            return new Vector3(0f, 0f, GearRetractedDistance);
        }

        private void DrawLayer(
            Skyfaller skyfaller,
            Vector3 drawLoc,
            Rot4 drawRotation,
            float extraRotation,
            float progress,
            Material material,
            Vector3 retractedOffset,
            float yOffset)
        {
            if (this.layerDrawer == null ||
                !ShuttleSkyfallerDeployProgress.ShouldDrawDeployLayers(progress))
            {
                return;
            }

            Vector3 localOffset = Vector3.Lerp(
                retractedOffset,
                Vector3.zero,
                Mathf.Clamp01(progress));

            ShuttleSkyfallerDeployDrawParms drawParms;
            if (!ShuttleSkyfallerDeployDrawUtility.TryBuildDrawParms(
                    skyfaller,
                    drawLoc,
                    drawRotation,
                    extraRotation,
                    localOffset,
                    out drawParms))
            {
                return;
            }

            this.layerDrawer.Draw(material, drawParms, yOffset);
        }
    }
}

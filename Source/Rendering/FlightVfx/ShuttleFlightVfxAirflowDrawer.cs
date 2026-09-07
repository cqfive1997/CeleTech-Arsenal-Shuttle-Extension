using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxAirflowDrawer
    {
        internal static readonly ShuttleFlightVfxAirflowDrawer Shared =
            new ShuttleFlightVfxAirflowDrawer(ShuttleFlightVfxAirflowMaterialCache.Shared);

        private const float MinBandIntensity = 0.03f;
        private const float MinSonicIntensity = 0.04f;
        private const float AirflowYOffset = 0.08f;

        private readonly ShuttleFlightVfxAirflowMaterialCache materialCache;
        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        private ShuttleFlightVfxAirflowDrawer(
            ShuttleFlightVfxAirflowMaterialCache materialCache)
        {
            this.materialCache = materialCache;
        }

        internal void Draw(
            Skyfaller skyfaller,
            Vector3 drawLoc,
            Rot4 drawRotation,
            float extraRotation,
            ShuttleFlightVfxMode mode)
        {
            if (!this.CanDraw(skyfaller))
            {
                return;
            }

            ShuttleFlightVfxAirflowState state = ShuttleFlightVfxAirflowProgress.Evaluate(skyfaller, mode);
            if (state.StreakIntensity < MinBandIntensity &&
                state.WakeIntensity < MinBandIntensity &&
                state.SonicCloudIntensity < MinSonicIntensity)
            {
                return;
            }

            Vector2 drawSize = ShuttleFlightVfxDrawSizeResolver.ResolveDrawSize(skyfaller);
            Quaternion rotation = this.BuildRotation(skyfaller.Graphic, drawRotation, extraRotation);
            Vector3 origin = drawLoc + skyfaller.Graphic.DrawOffset(drawRotation);

            this.DrawBands(
                ShuttleFlightVfxAirflowBandProvider.WakeBands,
                state.WakeIntensity,
                drawSize,
                origin,
                rotation,
                this.materialCache.WakeMaterial,
                new Color(0.30f, 0.90f, 1f, 0.26f));

            this.DrawBands(
                ShuttleFlightVfxAirflowBandProvider.StreakBands,
                state.StreakIntensity,
                drawSize,
                origin,
                rotation,
                this.materialCache.StreakMaterial,
                new Color(0.72f, 1f, 1f, 0.48f));

            this.DrawSonicCloud(state.SonicCloudIntensity, drawSize, origin, rotation);
        }

        private bool CanDraw(Skyfaller skyfaller)
        {
            return Prefs.DevMode &&
                skyfaller != null &&
                skyfaller.Graphic != null &&
                this.materialCache != null;
        }

        private void DrawBands(
            ShuttleFlightVfxAirflowBand[] bands,
            float baseIntensity,
            Vector2 drawSize,
            Vector3 origin,
            Quaternion rotation,
            Material material,
            Color baseColor)
        {
            if (bands == null || baseIntensity < MinBandIntensity)
            {
                return;
            }

            for (int i = 0; i < bands.Length; i++)
            {
                ShuttleFlightVfxAirflowBand band = bands[i];
                float intensity = ShuttleFlightVfxProgress.Clamp01(baseIntensity * band.IntensityScale);
                if (intensity >= MinBandIntensity)
                {
                    this.DrawBand(band, intensity, drawSize, origin, rotation, material, baseColor);
                }
            }
        }

        private void DrawBand(
            ShuttleFlightVfxAirflowBand band,
            float intensity,
            Vector2 drawSize,
            Vector3 origin,
            Quaternion rotation,
            Material material,
            Color baseColor)
        {
            Vector3 worldDirection = rotation * new Vector3(band.LocalDirection.x, 0f, band.LocalDirection.y);
            if (worldDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            worldDirection.Normalize();
            Vector3 localOffset = ShuttleFlightVfxAnchorMapper.TextureUvToLocalOffset(band.TextureUv, drawSize);
            Vector3 anchorWorld = origin + (rotation * localOffset);
            float length = band.BaseLength * Mathf.Lerp(0.70f, 1.12f, intensity);
            float width = band.BaseWidth * Mathf.Lerp(0.75f, 1.22f, intensity);
            Vector3 center = anchorWorld + (worldDirection * length * 0.5f);
            center.y += AirflowYOffset;

            Quaternion meshRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
            Color color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * intensity);
            this.DrawPlane(center, meshRotation, width, length, material, color);
        }

        private void DrawSonicCloud(
            float intensity,
            Vector2 drawSize,
            Vector3 origin,
            Quaternion rotation)
        {
            intensity = ShuttleFlightVfxProgress.Clamp01(intensity);
            if (intensity < MinSonicIntensity)
            {
                return;
            }

            Vector3 localOffset;
            if (!this.TryResolveAirflowReferenceLocalOffset(drawSize, out localOffset))
            {
                return;
            }

            localOffset.x += 0.45f;
            Vector3 center = origin + (rotation * localOffset);
            center.y += AirflowYOffset + 0.02f;

            float length = 3.8f * Mathf.Lerp(0.75f, 1.15f, intensity);
            float width = 1.55f * Mathf.Lerp(0.75f, 1.20f, intensity);
            Color color = new Color(0.95f, 1f, 1f, 0.34f * intensity);
            this.DrawPlane(center, rotation, length, width, this.materialCache.SonicCloudMaterial, color);
        }

        private bool TryResolveAirflowReferenceLocalOffset(Vector2 drawSize, out Vector3 localOffset)
        {
            localOffset = Vector3.zero;
            ShuttleThrusterLayout layout = ShuttleKunPengThrusterLayoutProvider.Layout;
            if (layout == null)
            {
                return false;
            }

            for (int i = 0; i < layout.Count; i++)
            {
                ShuttleThrusterAnchor anchor = layout.GetAnchor(i);
                if (anchor.Kind == ShuttleThrusterKind.AirflowReference)
                {
                    localOffset = ShuttleFlightVfxAnchorMapper.TextureUvToLocalOffset(anchor.TextureUv, drawSize);
                    return true;
                }
            }

            return false;
        }

        private void DrawPlane(
            Vector3 center,
            Quaternion rotation,
            float width,
            float length,
            Material material,
            Color color)
        {
            if (material == null || material == BaseContent.BadMat || width <= 0f || length <= 0f)
            {
                return;
            }

            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(center, rotation, new Vector3(width, 1f, length));
            this.propertyBlock.Clear();
            this.propertyBlock.SetColor("_Color", color);
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0, null, 0, this.propertyBlock);
        }

        private Quaternion BuildRotation(Graphic graphic, Rot4 drawRotation, float extraRotation)
        {
            Quaternion rotation = ShuttleFlightVfxAnchorMapper.EastAuthoredRotation(
                drawRotation,
                extraRotation);

            if (graphic.data != null && graphic.data.addTopAltitudeBias)
            {
                rotation *= Quaternion.Euler(Vector3.left * 2f);
            }

            return rotation;
        }
    }
}

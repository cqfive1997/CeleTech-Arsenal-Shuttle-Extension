using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxLaunchVaporDrawer
    {
        internal static readonly ShuttleFlightVfxLaunchVaporDrawer Shared =
            new ShuttleFlightVfxLaunchVaporDrawer(ShuttleFlightVfxLaunchVaporMaterialCache.Shared);

        private const float MinIntensity = 0.04f;
        private const float VaporYOffset = 0.11f;

        private readonly ShuttleFlightVfxLaunchVaporMaterialCache materialCache;
        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        private ShuttleFlightVfxLaunchVaporDrawer(
            ShuttleFlightVfxLaunchVaporMaterialCache materialCache)
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

            ShuttleFlightVfxLaunchVaporState state =
                ShuttleFlightVfxLaunchVaporProgress.Evaluate(skyfaller, mode);
            if (state.RearEdgeVaporIntensity < MinIntensity &&
                state.BellyVaporIntensity < MinIntensity &&
                state.BroadHazeIntensity < MinIntensity)
            {
                return;
            }

            Vector2 drawSize = ShuttleFlightVfxDrawSizeResolver.ResolveDrawSize(skyfaller);
            Quaternion rotation = this.BuildRotation(skyfaller.Graphic, drawRotation, extraRotation);
            Vector3 origin = drawLoc + skyfaller.Graphic.DrawOffset(drawRotation);
            ShuttleFlightVfxLaunchVaporEmitter[] emitters =
                ShuttleFlightVfxLaunchVaporEmitterProvider.Emitters;

            for (int i = 0; i < emitters.Length; i++)
            {
                this.DrawEmitter(emitters[i], state, drawSize, origin, rotation);
            }
        }

        private bool CanDraw(Skyfaller skyfaller)
        {
            return Prefs.DevMode &&
                skyfaller != null &&
                skyfaller.Graphic != null &&
                this.materialCache != null;
        }

        private void DrawEmitter(
            ShuttleFlightVfxLaunchVaporEmitter emitter,
            ShuttleFlightVfxLaunchVaporState state,
            Vector2 drawSize,
            Vector3 origin,
            Quaternion rotation)
        {
            float intensity = ShuttleFlightVfxProgress.Clamp01(
                this.ResolveKindIntensity(emitter.Kind, state) * emitter.IntensityScale);
            if (intensity < MinIntensity)
            {
                return;
            }

            Vector3 worldDirection = rotation * new Vector3(emitter.LocalDirection.x, 0f, emitter.LocalDirection.y);
            if (worldDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            worldDirection.Normalize();
            Vector3 localOffset = ShuttleFlightVfxAnchorMapper.TextureUvToLocalOffset(emitter.TextureUv, drawSize);
            Vector3 anchorWorld = origin + (rotation * localOffset);
            Vector3 center = anchorWorld + (worldDirection * emitter.BaseLength * 0.35f);
            center.y += VaporYOffset;

            float lengthScale;
            float widthScale;
            Material material;
            Color color;
            this.ResolveDrawStyle(emitter.Kind, intensity, out lengthScale, out widthScale, out material, out color);

            Quaternion meshRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
            this.DrawPlane(
                center,
                meshRotation,
                emitter.BaseWidth * widthScale,
                emitter.BaseLength * lengthScale,
                material,
                color);
        }

        private float ResolveKindIntensity(
            ShuttleFlightVfxLaunchVaporEmitterKind kind,
            ShuttleFlightVfxLaunchVaporState state)
        {
            if (kind == ShuttleFlightVfxLaunchVaporEmitterKind.BellyHaze)
            {
                return Mathf.Max(state.BellyVaporIntensity, state.BroadHazeIntensity);
            }

            return state.RearEdgeVaporIntensity;
        }

        private void ResolveDrawStyle(
            ShuttleFlightVfxLaunchVaporEmitterKind kind,
            float intensity,
            out float lengthScale,
            out float widthScale,
            out Material material,
            out Color color)
        {
            if (kind == ShuttleFlightVfxLaunchVaporEmitterKind.Curl)
            {
                lengthScale = Mathf.Lerp(0.85f, 1.18f, intensity);
                widthScale = Mathf.Lerp(0.85f, 1.25f, intensity);
                material = this.materialCache.CurlMaterial;
                color = new Color(0.86f, 1f, 1f, 0.38f * intensity);
                return;
            }

            if (kind == ShuttleFlightVfxLaunchVaporEmitterKind.RearEdgePlume)
            {
                lengthScale = Mathf.Lerp(0.75f, 1.10f, intensity);
                widthScale = Mathf.Lerp(0.75f, 1.10f, intensity);
                material = this.materialCache.PlumeMaterial;
                color = new Color(0.72f, 0.98f, 1f, 0.34f * intensity);
                return;
            }

            lengthScale = Mathf.Lerp(0.70f, 1.05f, intensity);
            widthScale = Mathf.Lerp(0.85f, 1.25f, intensity);
            material = this.materialCache.HazeMaterial;
            color = new Color(0.45f, 0.85f, 1f, 0.20f * intensity);
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

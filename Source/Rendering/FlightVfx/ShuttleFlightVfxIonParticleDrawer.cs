using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxIonParticleDrawer
    {
        internal static readonly ShuttleFlightVfxIonParticleDrawer Shared =
            new ShuttleFlightVfxIonParticleDrawer(
                ShuttleFlightVfxIonParticleMaterialCache.Shared,
                ShuttleFlightVfxIonParticleMeshCache.Shared);

        private const float MinIntensity = 0.035f;
        private const float UnderHullYOffset = -0.012f;
        private const float OverHullYOffset = 0.058f;

        private readonly ShuttleFlightVfxIonParticleMaterialCache materialCache;
        private readonly ShuttleFlightVfxIonParticleMeshCache meshCache;
        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        private ShuttleFlightVfxIonParticleDrawer(
            ShuttleFlightVfxIonParticleMaterialCache materialCache,
            ShuttleFlightVfxIonParticleMeshCache meshCache)
        {
            this.materialCache = materialCache;
            this.meshCache = meshCache;
        }

        internal void DrawOuterParticles(
            ShuttleThrusterAnchor anchor,
            Vector3 anchorWorld,
            Vector3 worldDirection,
            Quaternion meshRotation,
            bool underHull,
            float intensity,
            float length,
            float width)
        {
            if (!this.CanDraw(anchor, intensity, length, width))
            {
                return;
            }

            ShuttleFlightVfxIonParticleParams haze = ResolveHazeParams(anchor.Kind);
            this.DrawParticleSet(anchor, anchorWorld, worldDirection, meshRotation, underHull, intensity, length, width, haze, 101);

            ShuttleFlightVfxIonParticleParams wisp = ResolveWispParams(anchor.Kind);
            this.DrawParticleSet(anchor, anchorWorld, worldDirection, meshRotation, underHull, intensity, length, width, wisp, 307);
        }

        internal void DrawFilaments(
            ShuttleThrusterAnchor anchor,
            Vector3 anchorWorld,
            Vector3 worldDirection,
            Quaternion meshRotation,
            bool underHull,
            float intensity,
            float length,
            float width)
        {
            if (!this.CanDraw(anchor, intensity, length, width))
            {
                return;
            }

            ShuttleFlightVfxIonParticleParams filament = ResolveFilamentParams(anchor.Kind);
            this.DrawParticleSet(anchor, anchorWorld, worldDirection, meshRotation, underHull, intensity, length, width, filament, 509);
        }

        private bool CanDraw(
            ShuttleThrusterAnchor anchor,
            float intensity,
            float length,
            float width)
        {
            return anchor.Kind != ShuttleThrusterKind.AirflowReference &&
                intensity > MinIntensity &&
                length > 0f &&
                width > 0f &&
                this.materialCache != null &&
                this.materialCache.IsReady &&
                this.meshCache != null &&
                this.meshCache.QuadMesh != null;
        }

        private void DrawParticleSet(
            ShuttleThrusterAnchor anchor,
            Vector3 anchorWorld,
            Vector3 worldDirection,
            Quaternion meshRotation,
            bool underHull,
            float intensity,
            float length,
            float width,
            ShuttleFlightVfxIonParticleParams parameters,
            int salt)
        {
            if (parameters.Count <= 0)
            {
                return;
            }

            Material material = this.materialCache.GetParticleMaterial(anchor.Kind);
            Mesh mesh = this.meshCache.QuadMesh;
            if (material == null || mesh == null)
            {
                return;
            }

            Vector3 along = worldDirection;
            if (along.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            along.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, along);
            if (side.sqrMagnitude <= 0.0001f)
            {
                side = Vector3.right;
            }
            else
            {
                side.Normalize();
            }

            float tickTime = Find.TickManager != null ? Find.TickManager.TicksGame * 0.02f : 0f;
            for (int i = 0; i < parameters.Count; i++)
            {
                this.DrawParticle(
                    mesh,
                    material,
                    anchor,
                    anchorWorld,
                    along,
                    side,
                    meshRotation,
                    underHull,
                    intensity,
                    length,
                    width,
                    tickTime,
                    parameters,
                    i,
                    salt);
            }
        }

        private void DrawParticle(
            Mesh mesh,
            Material material,
            ShuttleThrusterAnchor anchor,
            Vector3 anchorWorld,
            Vector3 along,
            Vector3 side,
            Quaternion meshRotation,
            bool underHull,
            float intensity,
            float length,
            float width,
            float tickTime,
            ShuttleFlightVfxIonParticleParams parameters,
            int index,
            int salt)
        {
            float seed = Hash01(anchor, index, salt);
            float seedB = Hash01(anchor, index, salt + 41);
            float seedC = Hash01(anchor, index, salt + 83);
            float seedD = Hash01(anchor, index, salt + 127);

            float cycle = Frac((tickTime * parameters.Speed) + seed);
            float v = Mathf.Lerp(parameters.MinV, parameters.MaxV, cycle);
            float radiusProfile = ResolveParticleRadiusProfile(v, anchor.Kind);
            float driftWave = Mathf.Sin((seedB * 17.31f) + (tickTime * parameters.DriftSpeed));
            float lateralOffset = driftWave * radiusProfile * width * parameters.Spread;

            Vector3 position = anchorWorld +
                (along * (length * v)) +
                (side * lateralOffset);
            position.y += underHull ? UnderHullYOffset : OverHullYOffset;

            float fade = SmoothStep01(0f, 0.18f, cycle) *
                (1f - SmoothStep01(0.82f, 1f, cycle));
            float alpha = Mathf.Clamp01(
                intensity *
                Mathf.Lerp(parameters.MinAlpha, parameters.MaxAlpha, seedC) *
                fade);
            if (alpha <= 0.01f)
            {
                return;
            }

            float particleWidth = width * Mathf.Lerp(parameters.MinWidth, parameters.MaxWidth, seedB);
            float particleLength = length * Mathf.Lerp(parameters.MinLength, parameters.MaxLength, seedD);
            float yawOffset = Mathf.Lerp(-18f, 18f, seedC);
            Quaternion rotation = meshRotation * Quaternion.Euler(0f, yawOffset, 0f);

            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(position, rotation, new Vector3(particleWidth, 1f, particleLength));

            this.ApplyParticleParams(anchor, parameters, alpha, tickTime, seed);
            Graphics.DrawMesh(mesh, matrix, material, 0, null, 0, this.propertyBlock);
        }

        private void ApplyParticleParams(
            ShuttleThrusterAnchor anchor,
            ShuttleFlightVfxIonParticleParams parameters,
            float alpha,
            float tickTime,
            float seed)
        {
            this.propertyBlock.Clear();
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.Intensity, alpha);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.LengthScale, 1f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.WidthScale, 1f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.TimePhase, tickTime * 1.15f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.NoiseScroll, 1.2f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.EdgeSoftness, 0.88f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.TipFade, 0.72f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.RingStrength, 0.04f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.RingFrequency, 2.5f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.FlickerStrength, 0.26f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.FlickerSpeed, 2.4f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.PhaseOffset, (seed * 19.37f) + Hash01(anchor, 0, 911));
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.CylinderEnd, parameters.CylinderEnd);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.TipRadius, parameters.TipRadius);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.EdgeNoiseStrength, 0.10f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.FlowSpeed, parameters.FlowSpeed);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.FlowScale, parameters.FlowScale);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.FilamentStrength, parameters.FilamentStrength);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.PulseStrength, parameters.PulseStrength);
            this.propertyBlock.SetColor(ShuttleFlightVfxFlameShaderPropertyIds.CoreColor, parameters.CoreColor);
            this.propertyBlock.SetColor(ShuttleFlightVfxFlameShaderPropertyIds.OuterColor, parameters.OuterColor);
            this.propertyBlock.SetColor(ShuttleFlightVfxFlameShaderPropertyIds.HaloColor, parameters.HaloColor);
        }

        private static ShuttleFlightVfxIonParticleParams ResolveHazeParams(ShuttleThrusterKind kind)
        {
            if (kind == ShuttleThrusterKind.TailMain)
            {
                return new ShuttleFlightVfxIonParticleParams(
                    10,
                    0.18f,
                    0.95f,
                    0.10f,
                    0.24f,
                    0.18f,
                    0.42f,
                    0.08f,
                    0.18f,
                    1.10f,
                    0.78f,
                    1.10f,
                    0.28f,
                    0.18f,
                    7.5f,
                    1.55f,
                    0.12f,
                    0.06f,
                    new Color(0.72f, 1f, 1f, 1f),
                    new Color(0.12f, 0.78f, 1f, 1f),
                    new Color(0.18f, 0.20f, 1f, 1f));
            }

            if (kind == ShuttleThrusterKind.BellyVtol)
            {
                return new ShuttleFlightVfxIonParticleParams(
                    3,
                    0.08f,
                    0.62f,
                    0.06f,
                    0.14f,
                    0.08f,
                    0.20f,
                    0.035f,
                    0.085f,
                    0.46f,
                    0.92f,
                    1.05f,
                    0.22f,
                    0.12f,
                    5.8f,
                    1.65f,
                    0.06f,
                    0.045f,
                    new Color(0.74f, 1f, 1f, 1f),
                    new Color(0.16f, 0.74f, 1f, 1f),
                    new Color(0.12f, 0.28f, 0.95f, 1f));
            }

            return new ShuttleFlightVfxIonParticleParams(
                4,
                0.12f,
                0.88f,
                0.08f,
                0.18f,
                0.12f,
                0.30f,
                0.06f,
                0.14f,
                0.82f,
                0.92f,
                1.25f,
                0.26f,
                0.18f,
                6.2f,
                1.85f,
                0.08f,
                0.05f,
                new Color(0.74f, 1f, 1f, 1f),
                new Color(0.18f, 0.78f, 1f, 1f),
                new Color(0.13f, 0.32f, 1f, 1f));
        }

        private static ShuttleFlightVfxIonParticleParams ResolveFilamentParams(ShuttleThrusterKind kind)
        {
            if (kind == ShuttleThrusterKind.TailMain)
            {
                return new ShuttleFlightVfxIonParticleParams(
                    5,
                    0.05f,
                    0.80f,
                    0.025f,
                    0.055f,
                    0.35f,
                    0.75f,
                    0.35f,
                    0.65f,
                    0.45f,
                    1.05f,
                    0.75f,
                    0.42f,
                    0.05f,
                    13.0f,
                    2.15f,
                    0.42f,
                    0.11f,
                    new Color(0.90f, 1f, 1f, 1f),
                    new Color(0.32f, 0.95f, 1f, 1f),
                    new Color(0.20f, 0.28f, 1f, 1f));
            }

            if (kind == ShuttleThrusterKind.BellyVtol)
            {
                return new ShuttleFlightVfxIonParticleParams(
                    1,
                    0.04f,
                    0.50f,
                    0.018f,
                    0.034f,
                    0.12f,
                    0.26f,
                    0.12f,
                    0.26f,
                    0.20f,
                    1.10f,
                    0.75f,
                    0.24f,
                    0.05f,
                    8.5f,
                    2.05f,
                    0.16f,
                    0.06f,
                    new Color(0.88f, 1f, 1f, 1f),
                    new Color(0.26f, 0.88f, 1f, 1f),
                    new Color(0.14f, 0.34f, 0.95f, 1f));
            }

            return new ShuttleFlightVfxIonParticleParams(
                2,
                0.05f,
                0.72f,
                0.020f,
                0.040f,
                0.22f,
                0.48f,
                0.22f,
                0.44f,
                0.34f,
                1.15f,
                0.90f,
                0.36f,
                0.07f,
                10.0f,
                2.35f,
                0.26f,
                0.08f,
                new Color(0.88f, 1f, 1f, 1f),
                new Color(0.30f, 0.92f, 1f, 1f),
                new Color(0.16f, 0.38f, 1f, 1f));
        }

        private static ShuttleFlightVfxIonParticleParams ResolveWispParams(ShuttleThrusterKind kind)
        {
            if (kind == ShuttleThrusterKind.TailMain)
            {
                return new ShuttleFlightVfxIonParticleParams(
                    6,
                    0.65f,
                    1.05f,
                    0.05f,
                    0.14f,
                    0.16f,
                    0.36f,
                    0.12f,
                    0.28f,
                    1.40f,
                    0.66f,
                    1.38f,
                    0.20f,
                    0.28f,
                    6.5f,
                    1.45f,
                    0.10f,
                    0.08f,
                    new Color(0.62f, 1f, 1f, 1f),
                    new Color(0.10f, 0.70f, 1f, 1f),
                    new Color(0.26f, 0.16f, 1f, 1f));
            }

            if (kind == ShuttleThrusterKind.BellyVtol)
            {
                return new ShuttleFlightVfxIonParticleParams(
                    1,
                    0.42f,
                    0.70f,
                    0.025f,
                    0.075f,
                    0.06f,
                    0.15f,
                    0.040f,
                    0.10f,
                    0.38f,
                    0.68f,
                    1.00f,
                    0.16f,
                    0.16f,
                    4.8f,
                    1.35f,
                    0.05f,
                    0.045f,
                    new Color(0.62f, 1f, 1f, 1f),
                    new Color(0.14f, 0.70f, 1f, 1f),
                    new Color(0.16f, 0.25f, 0.95f, 1f));
            }

            return new ShuttleFlightVfxIonParticleParams(
                2,
                0.55f,
                0.92f,
                0.035f,
                0.10f,
                0.10f,
                0.26f,
                0.08f,
                0.18f,
                0.72f,
                0.72f,
                1.25f,
                0.18f,
                0.22f,
                5.6f,
                1.55f,
                0.08f,
                0.06f,
                new Color(0.64f, 1f, 1f, 1f),
                new Color(0.15f, 0.74f, 1f, 1f),
                new Color(0.18f, 0.28f, 1f, 1f));
        }

        private static float ResolveParticleRadiusProfile(float v, ShuttleThrusterKind kind)
        {
            float cylinderEnd = kind == ShuttleThrusterKind.TailMain ? 0.36f : 0.30f;
            float tipRadius = kind == ShuttleThrusterKind.TailMain ? 0.22f : 0.18f;
            if (v <= cylinderEnd)
            {
                return 1f;
            }

            return Mathf.Lerp(1f, tipRadius, SmoothStep01(cylinderEnd, 1f, v));
        }

        private static float Hash01(ShuttleThrusterAnchor anchor, int index, int salt)
        {
            unchecked
            {
                int hash = 23;
                string id = anchor.Id;
                if (!string.IsNullOrEmpty(id))
                {
                    for (int i = 0; i < id.Length; i++)
                    {
                        hash = (hash * 31) + id[i];
                    }
                }

                hash = (hash * 31) + index;
                hash = (hash * 31) + salt;
                hash = (hash * 31) + Mathf.RoundToInt(anchor.TextureUv.x * 10000f);
                hash = (hash * 31) + Mathf.RoundToInt(anchor.TextureUv.y * 10000f);
                return (hash & 0x7fffffff) / 2147483647f;
            }
        }

        private static float SmoothStep01(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
            return t * t * (3f - (2f * t));
        }

        private static float Frac(float value)
        {
            return value - Mathf.Floor(value);
        }
    }
}

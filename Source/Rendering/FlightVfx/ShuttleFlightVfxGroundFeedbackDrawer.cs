using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxGroundFeedbackDrawer
    {
        internal static readonly ShuttleFlightVfxGroundFeedbackDrawer Shared =
            new ShuttleFlightVfxGroundFeedbackDrawer(ShuttleFlightVfxGroundFeedbackMaterialCache.Shared);

        private const float MinIntensity = 0.04f;
        private const float GroundFeedbackYOffset = -0.055f;

        private readonly ShuttleFlightVfxGroundFeedbackMaterialCache materialCache;
        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        private ShuttleFlightVfxGroundFeedbackDrawer(
            ShuttleFlightVfxGroundFeedbackMaterialCache materialCache)
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

            ShuttleFlightVfxState state = ShuttleFlightVfxProgress.Evaluate(skyfaller, mode);
            ShuttleFlightVfxGroundFeedbackFactors factors =
                ShuttleFlightVfxGroundFeedbackProgress.Evaluate(skyfaller, mode);

            if (state.RearVtolIntensity < MinIntensity && state.BellyVtolIntensity < MinIntensity)
            {
                return;
            }

            ShuttleFlightVfxGroundImpactGroup rearGroup;
            ShuttleFlightVfxGroundImpactGroup bellyGroup;
            ShuttleFlightVfxGroundImpactResolver.ResolveGroups(
                skyfaller,
                drawLoc,
                drawRotation,
                extraRotation,
                state,
                out rearGroup,
                out bellyGroup);

            this.DrawGroup(rearGroup, factors);
            this.DrawGroup(bellyGroup, factors);
        }

        private bool CanDraw(Skyfaller skyfaller)
        {
            return Prefs.DevMode &&
                skyfaller != null &&
                skyfaller.Graphic != null &&
                this.materialCache != null;
        }

        private void DrawGroup(
            ShuttleFlightVfxGroundImpactGroup group,
            ShuttleFlightVfxGroundFeedbackFactors factors)
        {
            if (!group.IsValid)
            {
                return;
            }

            float baseIntensity = ShuttleFlightVfxProgress.Clamp01(group.Intensity);
            float hazeIntensity = baseIntensity * ShuttleFlightVfxProgress.Clamp01(factors.HazeCoupling);
            float glowIntensity = baseIntensity * ShuttleFlightVfxProgress.Clamp01(factors.GlowCoupling);
            float ringIntensity = baseIntensity * ShuttleFlightVfxProgress.Clamp01(factors.RingCoupling);

            if (hazeIntensity >= 0.04f)
            {
                float hazeRadius = group.BaseRadius * Mathf.Lerp(0.80f, 1.25f, hazeIntensity);
                this.DrawDisc(
                    group.Center,
                    hazeRadius,
                    hazeRadius * 0.82f,
                    this.materialCache.BlueHazeMaterial,
                    new Color(0.55f, 0.85f, 1f, 0.12f * hazeIntensity));
            }

            if (glowIntensity >= 0.05f)
            {
                float glowRadius = group.BaseRadius * Mathf.Lerp(0.30f, 0.75f, glowIntensity);
                this.DrawDisc(
                    group.Center,
                    glowRadius,
                    glowRadius * 0.72f,
                    this.materialCache.BlueGlowMaterial,
                    new Color(0.25f, 0.85f, 1f, 0.32f * glowIntensity));

                if (glowIntensity > 0.65f)
                {
                    this.DrawDisc(
                        group.Center,
                        glowRadius * 0.62f,
                        glowRadius * 0.44f,
                        this.materialCache.BlueGlowMaterial,
                        new Color(0.25f, 0.85f, 1f, 0.22f * glowIntensity));
                }
            }

            if (ringIntensity >= 0.12f)
            {
                float pulse = this.ResolveRingPulse();
                float ringRadius = group.BaseRadius *
                    Mathf.Lerp(0.70f, 1.10f, ringIntensity) *
                    Mathf.Lerp(0.92f, 1.10f, pulse);
                this.DrawDisc(
                    group.Center,
                    ringRadius,
                    ringRadius * 0.76f,
                    this.materialCache.ShockRingMaterial,
                    new Color(0.65f, 0.95f, 1f, 0.22f * ringIntensity));
            }
        }

        private float ResolveRingPulse()
        {
            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float phase = (ticks % 60) / 60f;
            return 0.5f + (0.5f * Mathf.Sin(phase * Mathf.PI * 2f));
        }

        private void DrawDisc(
            Vector3 center,
            float radiusX,
            float radiusZ,
            Material material,
            Color color)
        {
            if (material == null || material == BaseContent.BadMat || radiusX <= 0f || radiusZ <= 0f)
            {
                return;
            }

            Vector3 drawCenter = center;
            drawCenter.y += GroundFeedbackYOffset;

            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(drawCenter, Quaternion.identity, new Vector3(radiusX, 1f, radiusZ));
            this.propertyBlock.Clear();
            this.propertyBlock.SetColor("_Color", color);
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0, null, 0, this.propertyBlock);
        }
    }
}

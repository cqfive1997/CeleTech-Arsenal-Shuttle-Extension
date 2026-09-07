using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal static class ShuttleFlightVfxShaderParamResolver
    {
        internal static ShuttleFlightVfxFlameShaderParams Resolve(
            ShuttleThrusterKind kind,
            float intensity,
            float lengthScale,
            float widthScale)
        {
            return Resolve(default(ShuttleThrusterAnchor), kind, intensity, lengthScale, widthScale);
        }

        internal static ShuttleFlightVfxFlameShaderParams Resolve(
            ShuttleThrusterAnchor anchor,
            ShuttleThrusterKind kind,
            float intensity,
            float lengthScale,
            float widthScale)
        {
            float clampedIntensity = Mathf.Clamp01(intensity);
            bool tailMain = kind == ShuttleThrusterKind.TailMain;
            ShuttleFlightVfxThrusterVisualTuning tuning =
                ShuttleFlightVfxThrusterVisualTuningProvider.ForAnchor(anchor);
            float phaseOffset = DeterministicHash01(anchor) * 17.31f;

            ShuttleFlightVfxFlameShaderParams parameters = new ShuttleFlightVfxFlameShaderParams();
            parameters.Intensity = clampedIntensity;
            parameters.LengthScale = Mathf.Max(0.01f, lengthScale);
            parameters.WidthScale = Mathf.Max(0.01f, widthScale);
            parameters.TimePhase = ResolveTimePhase();
            parameters.PhaseOffset = phaseOffset;
            parameters.NoiseScroll = tailMain ? 1.05f : 1.22f;
            parameters.CoreColor = tailMain ?
                new Color(0.90f, 1.00f, 1.00f, 1f) :
                new Color(0.82f, 1.00f, 1.00f, 1f);
            parameters.OuterColor = tailMain ?
                new Color(0.08f, 0.65f, 1.00f, 1f) :
                new Color(0.12f, 0.62f, 1.00f, 1f);

            if (tailMain)
            {
                ApplyTailMainParameters(ref parameters, clampedIntensity);
            }
            else if (IsBottomParallelVisualMode(tuning.VisualMode))
            {
                ApplyBellyVtolParameters(ref parameters, clampedIntensity);
            }
            else
            {
                ApplyRearVtolParameters(ref parameters, clampedIntensity);
            }

            return parameters;
        }

        private static void ApplyTailMainParameters(
            ref ShuttleFlightVfxFlameShaderParams parameters,
            float intensity)
        {
            parameters.EdgeSoftness = 0.82f;
            parameters.TipFade = 0.72f;
            parameters.RingStrength = Mathf.Lerp(0.22f, 0.36f, intensity);
            parameters.RingFrequency = 6.2f;
            parameters.FlickerStrength = Mathf.Lerp(0.24f, 0.38f, intensity);
            parameters.FlickerSpeed = 3.15f;
            parameters.CylinderEnd = 0.38f;
            parameters.TipRadius = 0.09f;
            parameters.EdgeNoiseStrength = Mathf.Lerp(0.02f, 0.08f, intensity);
            parameters.FlowSpeed = 2.2f;
            parameters.FlowScale = 16f;
            parameters.FilamentStrength = 0.35f;
            parameters.PulseStrength = 0.10f;
            parameters.HaloColor = new Color(0.18f, 0.22f, 1.00f, 1f);
        }

        private static void ApplyRearVtolParameters(
            ref ShuttleFlightVfxFlameShaderParams parameters,
            float intensity)
        {
            parameters.EdgeSoftness = 0.86f;
            parameters.TipFade = 0.72f;
            parameters.RingStrength = Mathf.Lerp(0.00f, 0.04f, intensity);
            parameters.RingFrequency = 2.2f;
            parameters.FlickerStrength = Mathf.Lerp(0.26f, 0.42f, intensity);
            parameters.FlickerSpeed = 3.55f;
            parameters.CylinderEnd = 0.30f;
            parameters.TipRadius = 0.10f;
            parameters.EdgeNoiseStrength = Mathf.Lerp(0.01f, 0.05f, intensity);
            parameters.FlowSpeed = 2.8f;
            parameters.FlowScale = 12f;
            parameters.FilamentStrength = 0.18f;
            parameters.PulseStrength = 0.07f;
            parameters.HaloColor = new Color(0.15f, 0.35f, 1.00f, 1f);
        }

        private static void ApplyBellyVtolParameters(
            ref ShuttleFlightVfxFlameShaderParams parameters,
            float intensity)
        {
            parameters.EdgeSoftness = 0.96f;
            parameters.TipFade = 0.48f;
            parameters.RingStrength = Mathf.Lerp(0.00f, 0.025f, intensity);
            parameters.RingFrequency = 1.8f;
            parameters.FlickerStrength = Mathf.Lerp(0.22f, 0.36f, intensity);
            parameters.FlickerSpeed = 3.2f;
            parameters.CylinderEnd = 0.18f;
            parameters.TipRadius = 0.16f;
            parameters.EdgeNoiseStrength = Mathf.Lerp(0.01f, 0.04f, intensity);
            parameters.FlowSpeed = 2.4f;
            parameters.FlowScale = 10f;
            parameters.FilamentStrength = 0.05f;
            parameters.PulseStrength = 0.035f;
            parameters.HaloColor = new Color(0.12f, 0.30f, 0.95f, 1f);
        }

        private static bool IsBottomParallelVisualMode(
            ShuttleFlightVfxThrusterVisualMode visualMode)
        {
            return visualMode == ShuttleFlightVfxThrusterVisualMode.BellyParallel ||
                visualMode == ShuttleFlightVfxThrusterVisualMode.BellyVtol;
        }

        private static float ResolveTimePhase()
        {
            return Find.TickManager != null ? Find.TickManager.TicksGame * 0.055f : 0f;
        }

        private static float DeterministicHash01(ShuttleThrusterAnchor anchor)
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

                hash = (hash * 31) + Mathf.RoundToInt(anchor.TextureUv.x * 10000f);
                hash = (hash * 31) + Mathf.RoundToInt(anchor.TextureUv.y * 10000f);
                return (hash & 0x7fffffff) / 2147483647f;
            }
        }
    }
}

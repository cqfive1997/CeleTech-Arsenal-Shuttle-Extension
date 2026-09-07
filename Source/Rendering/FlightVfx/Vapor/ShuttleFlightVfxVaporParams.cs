using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx.Vapor
{
    internal struct ShuttleFlightVfxVaporParams
    {
        internal Color Color;
        internal float Alpha;
        internal float WidthStart;
        internal float WidthPeak;
        internal float WidthEnd;
        internal float PeakPos;
        internal float EdgeFeather;
        internal float RootFade;
        internal float TailFadeStart;
        internal float CenterPower;
        internal float NoiseScaleX;
        internal float NoiseScaleY;
        internal float NoiseStrengthFront;
        internal float NoiseStrengthTail;
        internal float TailNoiseStart;
        internal float Dissolve;
        internal float TailBreakup;
        internal float FlowSpeed;
        internal float CurlAmp;
        internal float CurlFreq;

        internal static ShuttleFlightVfxVaporParams ForKind(ShuttleFlightVfxVaporKind kind)
        {
            if (kind == ShuttleFlightVfxVaporKind.EdgeRibbon)
            {
                return Create(
                    new Color(0.90f, 0.97f, 1.00f, 1f),
                    0.28f,
                    0.10f,
                    0.20f,
                    0.07f,
                    0.30f,
                    0.10f,
                    0.05f,
                    0.88f,
                    0.70f,
                    4.8f,
                    1.4f,
                    0.01f,
                    0.10f,
                    0.74f,
                    0.03f,
                    0.10f,
                    0.12f,
                    0.003f,
                    1.4f);
            }

            if (kind == ShuttleFlightVfxVaporKind.ShoulderPuff)
            {
                return Create(
                    new Color(0.90f, 0.97f, 1.00f, 1f),
                    0.34f,
                    0.14f,
                    0.32f,
                    0.12f,
                    0.34f,
                    0.14f,
                    0.05f,
                    0.86f,
                    0.80f,
                    3.6f,
                    1.8f,
                    0.04f,
                    0.16f,
                    0.66f,
                    0.05f,
                    0.14f,
                    0.14f,
                    0.006f,
                    2.0f);
            }

            if (kind == ShuttleFlightVfxVaporKind.CurlPuff)
            {
                return Create(
                    new Color(0.88f, 0.96f, 1.00f, 1f),
                    0.24f,
                    0.10f,
                    0.24f,
                    0.09f,
                    0.34f,
                    0.12f,
                    0.06f,
                    0.82f,
                    0.72f,
                    4.2f,
                    2.0f,
                    0.04f,
                    0.16f,
                    0.64f,
                    0.05f,
                    0.16f,
                    0.16f,
                    0.014f,
                    3.0f);
            }

            return Create(
                new Color(0.82f, 0.91f, 0.96f, 1f),
                0.08f,
                0.06f,
                0.12f,
                0.04f,
                0.34f,
                0.08f,
                0.08f,
                0.70f,
                1.25f,
                4.0f,
                2.4f,
                0.10f,
                0.36f,
                0.55f,
                0.22f,
                0.38f,
                0.18f,
                0.025f,
                3.0f);
        }

        private static ShuttleFlightVfxVaporParams Create(
            Color color,
            float alpha,
            float widthStart,
            float widthPeak,
            float widthEnd,
            float peakPos,
            float edgeFeather,
            float rootFade,
            float tailFadeStart,
            float centerPower,
            float noiseScaleX,
            float noiseScaleY,
            float noiseStrengthFront,
            float noiseStrengthTail,
            float tailNoiseStart,
            float dissolve,
            float tailBreakup,
            float flowSpeed,
            float curlAmp,
            float curlFreq)
        {
            ShuttleFlightVfxVaporParams value = new ShuttleFlightVfxVaporParams();
            value.Color = color;
            value.Alpha = alpha;
            value.WidthStart = widthStart;
            value.WidthPeak = widthPeak;
            value.WidthEnd = widthEnd;
            value.PeakPos = peakPos;
            value.EdgeFeather = edgeFeather;
            value.RootFade = rootFade;
            value.TailFadeStart = tailFadeStart;
            value.CenterPower = centerPower;
            value.NoiseScaleX = noiseScaleX;
            value.NoiseScaleY = noiseScaleY;
            value.NoiseStrengthFront = noiseStrengthFront;
            value.NoiseStrengthTail = noiseStrengthTail;
            value.TailNoiseStart = tailNoiseStart;
            value.Dissolve = dissolve;
            value.TailBreakup = tailBreakup;
            value.FlowSpeed = flowSpeed;
            value.CurlAmp = curlAmp;
            value.CurlFreq = curlFreq;
            return value;
        }
    }
}

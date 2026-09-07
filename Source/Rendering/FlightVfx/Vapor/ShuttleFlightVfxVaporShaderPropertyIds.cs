using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx.Vapor
{
    internal static class ShuttleFlightVfxVaporShaderPropertyIds
    {
        internal static readonly int Color = Shader.PropertyToID("_Color");
        internal static readonly int Alpha = Shader.PropertyToID("_Alpha");
        internal static readonly int TimePhase = Shader.PropertyToID("_TimePhase");
        internal static readonly int WidthStart = Shader.PropertyToID("_WidthStart");
        internal static readonly int WidthPeak = Shader.PropertyToID("_WidthPeak");
        internal static readonly int WidthEnd = Shader.PropertyToID("_WidthEnd");
        internal static readonly int PeakPos = Shader.PropertyToID("_PeakPos");
        internal static readonly int RootFade = Shader.PropertyToID("_RootFade");
        internal static readonly int TailFadeStart = Shader.PropertyToID("_TailFadeStart");
        internal static readonly int CenterPower = Shader.PropertyToID("_CenterPower");
        internal static readonly int NoiseScaleX = Shader.PropertyToID("_NoiseScaleX");
        internal static readonly int NoiseScaleY = Shader.PropertyToID("_NoiseScaleY");
        internal static readonly int NoiseStrengthFront = Shader.PropertyToID("_NoiseStrengthFront");
        internal static readonly int NoiseStrengthTail = Shader.PropertyToID("_NoiseStrengthTail");
        internal static readonly int TailNoiseStart = Shader.PropertyToID("_TailNoiseStart");
        internal static readonly int FlowSpeed = Shader.PropertyToID("_FlowSpeed");
        internal static readonly int Dissolve = Shader.PropertyToID("_Dissolve");
        internal static readonly int EdgeFeather = Shader.PropertyToID("_EdgeFeather");
        internal static readonly int TailBreakup = Shader.PropertyToID("_TailBreakup");
        internal static readonly int PhaseOffset = Shader.PropertyToID("_PhaseOffset");
        internal static readonly int CurlAmp = Shader.PropertyToID("_CurlAmp");
        internal static readonly int CurlFreq = Shader.PropertyToID("_CurlFreq");
        internal static readonly int MotionMultiplier = Shader.PropertyToID("_MotionMultiplier");
        internal static readonly int DetailLayer = Shader.PropertyToID("_DetailLayer");
    }
}

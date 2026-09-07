using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal static class ShuttleFlightVfxFlameShaderPropertyIds
    {
        internal static readonly int Intensity = Shader.PropertyToID("_Intensity");
        internal static readonly int LengthScale = Shader.PropertyToID("_LengthScale");
        internal static readonly int WidthScale = Shader.PropertyToID("_WidthScale");
        internal static readonly int TimePhase = Shader.PropertyToID("_TimePhase");
        internal static readonly int CoreColor = Shader.PropertyToID("_CoreColor");
        internal static readonly int OuterColor = Shader.PropertyToID("_OuterColor");
        internal static readonly int NoiseScroll = Shader.PropertyToID("_NoiseScroll");
        internal static readonly int EdgeSoftness = Shader.PropertyToID("_EdgeSoftness");
        internal static readonly int TipFade = Shader.PropertyToID("_TipFade");
        internal static readonly int RingStrength = Shader.PropertyToID("_RingStrength");
        internal static readonly int RingFrequency = Shader.PropertyToID("_RingFrequency");
        internal static readonly int FlickerStrength = Shader.PropertyToID("_FlickerStrength");
        internal static readonly int FlickerSpeed = Shader.PropertyToID("_FlickerSpeed");
        internal static readonly int PhaseOffset = Shader.PropertyToID("_PhaseOffset");
        internal static readonly int CylinderEnd = Shader.PropertyToID("_CylinderEnd");
        internal static readonly int TipRadius = Shader.PropertyToID("_TipRadius");
        internal static readonly int EdgeNoiseStrength = Shader.PropertyToID("_EdgeNoiseStrength");
        internal static readonly int FlowSpeed = Shader.PropertyToID("_FlowSpeed");
        internal static readonly int FlowScale = Shader.PropertyToID("_FlowScale");
        internal static readonly int FilamentStrength = Shader.PropertyToID("_FilamentStrength");
        internal static readonly int HaloColor = Shader.PropertyToID("_HaloColor");
        internal static readonly int PulseStrength = Shader.PropertyToID("_PulseStrength");
    }
}

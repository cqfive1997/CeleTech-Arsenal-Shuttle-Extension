namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.CustomShaders
{
    internal static class ShuttleCustomShaderMaterialContracts
    {
        internal static readonly CustomShaderMaterialContract SurfaceShield =
            new CustomShaderMaterialContract(
                "CeleTech/SurfaceShieldHullOverlay",
                new[]
                {
                    "_HullTex",
                    "_SurfaceMaskTex",
                    "_NoiseTex",
                    "_ShieldStrength",
                    "_GameTick",
                    "_HitCount",
                    "_BaseAlpha",
                    "_EdgeGlowAlpha",
                    "_EdgeDetectScale",
                    "_EdgeSampleOffset"
                },
                true);

        internal static readonly CustomShaderMaterialContract Paint =
            new CustomShaderMaterialContract(
                "Map/CMC_ShuttlePaint_RGBMask",
                new[]
                {
                    "_MainTex",
                    "_PaintMaskTex",
                    "_PrimaryColor",
                    "_SecondaryColor",
                    "_AccentColor",
                    "_OverlayAlpha",
                    "_PreviewOpaque",
                    "_PreviewBackgroundColor"
                },
                true);

        internal static readonly CustomShaderMaterialContract Flame =
            new CustomShaderMaterialContract(
                "Map/CMC_ShuttleFlame",
                new[]
                {
                    "_Intensity",
                    "_LengthScale",
                    "_WidthScale",
                    "_TimePhase",
                    "_CoreColor",
                    "_OuterColor",
                    "_NoiseScroll",
                    "_EdgeSoftness",
                    "_TipFade",
                    "_RingStrength",
                    "_RingFrequency",
                    "_FlickerStrength",
                    "_FlickerSpeed",
                    "_PhaseOffset",
                    "_CylinderEnd",
                    "_TipRadius",
                    "_EdgeNoiseStrength",
                    "_FlowSpeed",
                    "_FlowScale",
                    "_FilamentStrength",
                    "_HaloColor",
                    "_PulseStrength"
                },
                true);

        internal static readonly CustomShaderMaterialContract Vapor =
            new CustomShaderMaterialContract(
                "Map/CMC_ShuttleVapor",
                new[]
                {
                    "_Color",
                    "_Alpha",
                    "_TimePhase",
                    "_WidthStart",
                    "_WidthPeak",
                    "_WidthEnd",
                    "_PeakPos",
                    "_RootFade",
                    "_TailFadeStart",
                    "_CenterPower",
                    "_NoiseScaleX",
                    "_NoiseScaleY",
                    "_NoiseStrengthFront",
                    "_NoiseStrengthTail",
                    "_TailNoiseStart",
                    "_FlowSpeed",
                    "_Dissolve",
                    "_EdgeFeather",
                    "_TailBreakup",
                    "_PhaseOffset",
                    "_CurlAmp",
                    "_CurlFreq",
                    "_MotionMultiplier",
                    "_DetailLayer"
                },
                true);
    }
}

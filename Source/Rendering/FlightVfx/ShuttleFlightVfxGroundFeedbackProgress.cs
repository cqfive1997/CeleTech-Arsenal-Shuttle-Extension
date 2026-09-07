using RimWorld;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal struct ShuttleFlightVfxGroundFeedbackFactors
    {
        internal float GlowCoupling;
        internal float HazeCoupling;
        internal float RingCoupling;
    }

    internal static class ShuttleFlightVfxGroundFeedbackProgress
    {
        internal static ShuttleFlightVfxGroundFeedbackFactors Evaluate(
            Skyfaller skyfaller,
            ShuttleFlightVfxMode mode)
        {
            float t = ResolveAnimationTime(skyfaller, mode);
            if (mode == ShuttleFlightVfxMode.Leaving)
            {
                return EvaluateLeaving(t);
            }

            return EvaluateIncoming(t);
        }

        private static ShuttleFlightVfxGroundFeedbackFactors EvaluateLeaving(float t)
        {
            ShuttleFlightVfxGroundFeedbackFactors factors = new ShuttleFlightVfxGroundFeedbackFactors();
            float nearGround = FadeOut(0.14f, 0.34f, t);
            float airborneAssistHaze = 0.18f * SmoothPulse(0.40f, 0.50f, 0.62f, 0.78f, t);

            factors.GlowCoupling = nearGround;
            factors.RingCoupling = nearGround;
            factors.HazeCoupling = Mathf.Max(nearGround, airborneAssistHaze);
            return factors;
        }

        private static ShuttleFlightVfxGroundFeedbackFactors EvaluateIncoming(float t)
        {
            ShuttleFlightVfxGroundFeedbackFactors factors = new ShuttleFlightVfxGroundFeedbackFactors();
            float nearGround = SmoothStep01(0.68f, 0.94f, t);
            float touchdownFlare = SmoothPulse(0.90f, 0.96f, 0.985f, 1.00f, t);
            float coupling = Clamp01(nearGround + (0.30f * touchdownFlare));

            factors.GlowCoupling = coupling;
            factors.RingCoupling = coupling;
            factors.HazeCoupling = Clamp01(SmoothStep01(0.55f, 0.88f, t) + (0.20f * touchdownFlare));
            return factors;
        }

        private static float ResolveAnimationTime(Skyfaller skyfaller, ShuttleFlightVfxMode mode)
        {
            if (skyfaller == null)
            {
                return 0f;
            }

            if (mode == ShuttleFlightVfxMode.Leaving)
            {
                return Clamp01(1f - ((float)skyfaller.ticksToImpact / Mathf.Max(1f, skyfaller.LeaveMapAfterTicks)));
            }

            int maxTicks = 220;
            if (skyfaller.def != null && skyfaller.def.skyfaller != null)
            {
                maxTicks = Mathf.Max(1, skyfaller.def.skyfaller.ticksToImpactRange.max);
            }

            return Clamp01(1f - ((float)skyfaller.ticksToImpact / Mathf.Max(1f, maxTicks)));
        }

        private static float SmoothPulse(float inStart, float inEnd, float outStart, float outEnd, float x)
        {
            return SmoothStep01(inStart, inEnd, x) * FadeOut(outStart, outEnd, x);
        }

        private static float FadeOut(float start, float end, float x)
        {
            return 1f - SmoothStep01(start, end, x);
        }

        private static float SmoothStep01(float edge0, float edge1, float x)
        {
            float t = Clamp01((x - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
            return t * t * (3f - (2f * t));
        }

        private static float Clamp01(float value)
        {
            return Mathf.Clamp01(value);
        }
    }
}

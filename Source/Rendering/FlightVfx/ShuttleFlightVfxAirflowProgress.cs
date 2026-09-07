using RimWorld;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal static class ShuttleFlightVfxAirflowProgress
    {
        internal static ShuttleFlightVfxAirflowState Evaluate(
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

        private static ShuttleFlightVfxAirflowState EvaluateLeaving(float t)
        {
            ShuttleFlightVfxAirflowState state = new ShuttleFlightVfxAirflowState();
            state.StreakIntensity = SmoothStep01(0.28f, 0.46f, t);
            state.WakeIntensity = SmoothStep01(0.34f, 0.54f, t);
            state.SonicCloudIntensity = SmoothPulse(0.48f, 0.56f, 0.70f, 0.84f, t);
            return state;
        }

        private static ShuttleFlightVfxAirflowState EvaluateIncoming(float t)
        {
            ShuttleFlightVfxAirflowState state = new ShuttleFlightVfxAirflowState();
            state.StreakIntensity = FadeOut(0.50f, 0.78f, t);
            state.WakeIntensity = FadeOut(0.42f, 0.68f, t);
            state.SonicCloudIntensity = 0.40f * SmoothPulse(0.06f, 0.14f, 0.22f, 0.34f, t);
            return state;
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

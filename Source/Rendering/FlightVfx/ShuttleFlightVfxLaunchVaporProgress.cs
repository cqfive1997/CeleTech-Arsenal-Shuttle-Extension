using RimWorld;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal static class ShuttleFlightVfxLaunchVaporProgress
    {
        internal static ShuttleFlightVfxLaunchVaporState Evaluate(
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

        private static ShuttleFlightVfxLaunchVaporState EvaluateLeaving(float t)
        {
            ShuttleFlightVfxLaunchVaporState state = new ShuttleFlightVfxLaunchVaporState();
            float firstLift = FadeOut(0.12f, 0.34f, t);
            float secondLift = SmoothPulse(0.40f, 0.50f, 0.66f, 0.82f, t);
            float launchPulse = Clamp01(Mathf.Max(firstLift, 0.85f * secondLift));

            state.RearEdgeVaporIntensity = launchPulse;
            state.BellyVaporIntensity = 0.75f * launchPulse;
            state.BroadHazeIntensity = 0.45f * launchPulse;
            return state;
        }

        private static ShuttleFlightVfxLaunchVaporState EvaluateIncoming(float t)
        {
            ShuttleFlightVfxLaunchVaporState state = new ShuttleFlightVfxLaunchVaporState();
            float nearGround = SmoothStep01(0.55f, 0.88f, t);

            state.RearEdgeVaporIntensity = 0.65f * nearGround;
            state.BellyVaporIntensity = nearGround;
            state.BroadHazeIntensity = 0.45f * nearGround;
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

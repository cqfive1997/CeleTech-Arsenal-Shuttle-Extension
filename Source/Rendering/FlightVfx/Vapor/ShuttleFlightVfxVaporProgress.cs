using CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx.Vapor
{
    internal static class ShuttleFlightVfxVaporProgress
    {
        private const float LeavingStartDelaySeconds = 1.00f;
        private const float TicksPerSecond = 60f;
        private const float LeavingFadeInSeconds = 0.28f;

        internal static float Evaluate(Skyfaller skyfaller, ShuttleFlightVfxMode mode)
        {
            float t = ResolveAnimationTime(skyfaller, mode);
            return EvaluateNormalized(skyfaller, mode, t);
        }

        internal static float Evaluate(
            Skyfaller skyfaller,
            ShuttleFlightVfxMode mode,
            float timeInAnimation)
        {
            float t = Mathf.Clamp01(timeInAnimation);
            return EvaluateNormalized(skyfaller, mode, t);
        }

        private static float EvaluateNormalized(Skyfaller skyfaller, ShuttleFlightVfxMode mode, float t)
        {
            if (mode == ShuttleFlightVfxMode.Leaving)
            {
                return EvaluateLeaving(skyfaller, t);
            }

            return EvaluateIncoming(t);
        }

        private static float EvaluateLeaving(Skyfaller skyfaller, float t)
        {
            float delay = ResolveLeavingDelayNormalized(skyfaller);
            float fadeEnd = Mathf.Min(0.88f, delay + ResolveLeavingFadeInNormalized(skyfaller));
            if (fadeEnd <= delay)
            {
                fadeEnd = delay + 0.0001f;
            }

            float appear = SmoothStep01(delay, fadeEnd, t);
            float fade = 1f - SmoothStep01(0.90f, 0.995f, t);
            float active = appear * fade;
            return active * Mathf.Lerp(0.75f, 1.0f, active);
        }

        private static float EvaluateIncoming(float t)
        {
            float appear = SmoothStep01(0.08f, 0.20f, t);
            float fade = 1f - SmoothStep01(0.58f, 0.78f, t);
            float active = appear * fade;
            return active * Mathf.Lerp(0.32f, 0.85f, active);
        }

        private static float ResolveLeavingDelayNormalized(Skyfaller skyfaller)
        {
            float totalTicks = ResolveLeavingTotalTicks(skyfaller);
            return Mathf.Clamp01((LeavingStartDelaySeconds * TicksPerSecond) / totalTicks);
        }

        private static float ResolveLeavingFadeInNormalized(Skyfaller skyfaller)
        {
            float totalTicks = ResolveLeavingTotalTicks(skyfaller);
            return Mathf.Clamp01((LeavingFadeInSeconds * TicksPerSecond) / totalTicks);
        }

        private static float ResolveLeavingTotalTicks(Skyfaller skyfaller)
        {
            if (skyfaller == null)
            {
                return TicksPerSecond;
            }

            return Mathf.Max(1f, skyfaller.LeaveMapAfterTicks);
        }

        internal static float ResolveAnimationTime(Skyfaller skyfaller, ShuttleFlightVfxMode mode)
        {
            if (skyfaller == null)
            {
                return 0f;
            }

            if (mode == ShuttleFlightVfxMode.Leaving)
            {
                return Mathf.Clamp01(1f - ((float)skyfaller.ticksToImpact / Mathf.Max(1f, skyfaller.LeaveMapAfterTicks)));
            }

            int maxTicks = 220;
            if (skyfaller.def != null && skyfaller.def.skyfaller != null)
            {
                maxTicks = Mathf.Max(1, skyfaller.def.skyfaller.ticksToImpactRange.max);
            }

            return Mathf.Clamp01(1f - ((float)skyfaller.ticksToImpact / Mathf.Max(1f, maxTicks)));
        }

        private static float SmoothStep01(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
            return t * t * (3f - (2f * t));
        }
    }
}

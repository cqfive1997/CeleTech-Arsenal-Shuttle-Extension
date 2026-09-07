using RimWorld;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal static class ShuttleFlightVfxProgress
    {
        internal static ShuttleFlightVfxState Evaluate(Skyfaller skyfaller, ShuttleFlightVfxMode mode)
        {
            float t = ResolveAnimationTime(skyfaller, mode);
            if (mode == ShuttleFlightVfxMode.Leaving)
            {
                return EvaluateLeaving(t);
            }

            return EvaluateIncoming(t);
        }

        internal static float SmoothStep01(float edge0, float edge1, float x)
        {
            float t = Clamp01((x - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
            return t * t * (3f - (2f * t));
        }

        internal static float FadeOut(float start, float end, float x)
        {
            return 1f - SmoothStep01(start, end, x);
        }

        internal static float Clamp01(float value)
        {
            return Mathf.Clamp01(value);
        }

        private static float SmoothPulse(float inStart, float inEnd, float outStart, float outEnd, float x)
        {
            return SmoothStep01(inStart, inEnd, x) * FadeOut(outStart, outEnd, x);
        }

        private static ShuttleFlightVfxState EvaluateLeaving(float t)
        {
            ShuttleFlightVfxState state = new ShuttleFlightVfxState();

            state.TailMainIntensity = Clamp01(
                0.45f + 0.55f * SmoothStep01(0.08f, 0.22f, t));
            state.TailMainLengthScale =
                0.45f + 0.55f * SmoothStep01(0.02f, 0.18f, t);

            float vtolBase = 0.28f;
            float firstLiftPulse = 0.72f * FadeOut(0.18f, 0.34f, t);
            float secondLiftPulse = 0.82f * SmoothPulse(0.40f, 0.50f, 0.66f, 0.82f, t);
            float vtol = Clamp01(vtolBase + Mathf.Max(firstLiftPulse, secondLiftPulse));
            state.RearVtolIntensity = vtol;
            state.BellyVtolIntensity = vtol;
            state.RearVtolLengthScale = 0.55f + 0.45f * state.RearVtolIntensity;
            state.BellyVtolLengthScale = 0.55f + 0.45f * state.BellyVtolIntensity;
            state.TailMainWidthScale = 1f;
            state.RearVtolWidthScale = 1f;
            state.BellyVtolWidthScale = 1f;
            state.FlickerBoost = 0.15f;

            return state;
        }

        private static ShuttleFlightVfxState EvaluateIncoming(float t)
        {
            ShuttleFlightVfxState state = new ShuttleFlightVfxState();

            float rearStartup = SmoothStep01(0.28f, 0.42f, t);
            float bellyStartup = SmoothStep01(0.38f, 0.52f, t);
            float bellyMain = SmoothStep01(0.50f, 0.76f, t);
            float nearGround = SmoothStep01(0.68f, 0.94f, t);
            float touchdownFlare = SmoothPulse(0.90f, 0.96f, 0.985f, 1.00f, t);

            state.TailMainIntensity = EvaluateTailMainIncomingIntensity(t);
            state.TailMainLengthScale = EvaluateTailMainIncomingLength(t);
            state.TailMainWidthScale = EvaluateTailMainIncomingWidth(t);

            state.RearVtolIntensity = Clamp01(EvaluateRearVtolIncomingIntensity(t, rearStartup, touchdownFlare));
            state.BellyVtolIntensity = Clamp01(EvaluateBellyVtolIncomingIntensity(t, bellyStartup, bellyMain, touchdownFlare));

            state.RearVtolLengthScale = Mathf.Lerp(1.02f, 0.70f, nearGround);
            state.BellyVtolLengthScale = EvaluateBellyVtolIncomingLength(t, nearGround);
            state.RearVtolWidthScale = Mathf.Lerp(1.12f, 1.45f, nearGround);
            state.BellyVtolWidthScale = EvaluateBellyVtolIncomingWidth(t, nearGround);

            state.GroundCoupling = Clamp01(nearGround + (0.30f * touchdownFlare));
            state.FlickerBoost = 0.15f + (0.35f * touchdownFlare);
            state.StabilizerPulse = SmoothPulse(0.35f, 0.48f, 0.62f, 0.75f, t);
            state.TouchdownFlare = touchdownFlare;
            return state;
        }

        private static float EvaluateTailMainIncomingIntensity(float t)
        {
            float nearGround = SmoothStep01(0.20f, 0.98f, t);
            float touchdownCut = 1f - SmoothStep01(0.985f, 1.00f, t);
            float intensity = Mathf.Lerp(0.70f, 0.22f, nearGround) * touchdownCut;
            if (t < 0.98f)
            {
                intensity = Mathf.Max(intensity, 0.20f);
            }

            return Clamp01(intensity);
        }

        private static float EvaluateTailMainIncomingLength(float t)
        {
            float nearGround = SmoothStep01(0.20f, 0.98f, t);
            float touchdownCut = 1f - SmoothStep01(0.985f, 1.00f, t);
            float length = Mathf.Lerp(1.25f, 0.25f, nearGround) * touchdownCut;
            if (t < 0.98f)
            {
                length = Mathf.Max(length, 0.22f);
            }

            return Mathf.Max(0f, length);
        }

        private static float EvaluateTailMainIncomingWidth(float t)
        {
            float nearGround = SmoothStep01(0.20f, 0.98f, t);
            return Mathf.Lerp(0.90f, 1.18f, nearGround);
        }

        private static float EvaluateRearVtolIncomingIntensity(
            float t,
            float rearStartup,
            float touchdownFlare)
        {
            float hover = SmoothStep01(0.42f, 0.70f, t);
            float intensity = (0.12f * rearStartup) + (0.76f * hover) + (0.24f * touchdownFlare);
            if (t > 0.55f)
            {
                intensity = Mathf.Max(intensity, 0.30f);
            }

            if (t > 0.70f)
            {
                intensity = Mathf.Max(intensity, 0.45f);
            }

            if (t > 0.90f)
            {
                intensity = Mathf.Max(intensity, 0.65f);
            }

            return intensity;
        }

        private static float EvaluateBellyVtolIncomingIntensity(
            float t,
            float bellyStartup,
            float bellyMain,
            float touchdownFlare)
        {
            float intensity =
                (0.08f * bellyStartup) +
                (0.92f * bellyMain) +
                (0.34f * touchdownFlare);

            if (t > 0.55f)
            {
                intensity = Mathf.Max(intensity, 0.30f);
            }

            if (t > 0.70f)
            {
                intensity = Mathf.Max(intensity, 0.45f);
            }

            if (t > 0.90f)
            {
                intensity = Mathf.Max(intensity, 0.65f);
            }

            if (touchdownFlare > 0f)
            {
                intensity = Mathf.Max(intensity, Mathf.Lerp(0.80f, 1.05f, touchdownFlare));
            }

            return intensity;
        }

        private static float EvaluateBellyVtolIncomingLength(float t, float nearGround)
        {
            float length = Mathf.Lerp(0.92f, 0.58f, nearGround);
            if (t > 0.92f)
            {
                length = Mathf.Lerp(length, 0.52f, SmoothStep01(0.92f, 1.00f, t));
            }

            return length;
        }

        private static float EvaluateBellyVtolIncomingWidth(float t, float nearGround)
        {
            float width = Mathf.Lerp(1.25f, 1.62f, nearGround);
            if (t > 0.92f)
            {
                width = Mathf.Lerp(width, 1.72f, SmoothStep01(0.92f, 1.00f, t));
            }

            return width;
        }

        private static float SmoothKeyframe(
            float start,
            float end,
            float startValue,
            float valueDelta,
            float x)
        {
            return startValue + (valueDelta * SmoothStep01(start, end, x));
        }

        private static void ApplyLengthScales(ref ShuttleFlightVfxState state)
        {
            state.TailMainLengthScale = state.TailMainIntensity;
            state.RearVtolLengthScale = state.RearVtolIntensity;
            state.BellyVtolLengthScale = state.BellyVtolIntensity;
            state.TailMainWidthScale = 1f;
            state.RearVtolWidthScale = 1f;
            state.BellyVtolWidthScale = 1f;
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
    }
}

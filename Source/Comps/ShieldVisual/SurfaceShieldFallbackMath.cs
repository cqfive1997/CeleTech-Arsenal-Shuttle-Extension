using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class SurfaceShieldFallbackMath
    {
        internal static float EaseOutSqrt(float value)
        {
            return Mathf.Sqrt(Mathf.Clamp01(value));
        }

        internal static float FadeInFast(float value)
        {
            float t = Mathf.Clamp01(value / 0.16f);
            return t * t * (3f - 2f * t);
        }

        internal static float FadeOutQuadratic(float value)
        {
            float t = 1f - Mathf.Clamp01(value);
            return t * t;
        }

        internal static float FadeOutSmooth(float value)
        {
            float t = Mathf.Clamp01(value);
            float fade = 1f - (t * t * (3f - 2f * t));
            return fade * fade;
        }

        internal static float PseudoRandom01(int seed)
        {
            unchecked
            {
                uint value = (uint)seed;
                value ^= value >> 16;
                value *= 2246822519u;
                value ^= value >> 13;
                value *= 3266489917u;
                value ^= value >> 16;
                return (value & 0x00FFFFFF) / 16777215f;
            }
        }

        internal static float PseudoRandomSigned(int seed)
        {
            return PseudoRandom01(seed) * 2f - 1f;
        }

        internal static float GetLowShieldFlicker(int pulseSeed, int ticksGame)
        {
            // Deterministic per-pulse flicker: low shield visuals look unstable
            // without using per-frame random state or affecting gameplay.
            int flickerStep = ticksGame / 3;
            float coarse = PseudoRandom01(pulseSeed + flickerStep * 613 + 331);
            float fine = PseudoRandom01(pulseSeed + ticksGame * 197 + 349);
            return Mathf.Clamp(0.62f + coarse * 0.28f + fine * 0.14f, 0.58f, 1.04f);
        }
    }
}

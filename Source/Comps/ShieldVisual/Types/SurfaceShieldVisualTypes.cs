using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    // Internal only so the split hull-mask/geometry helpers can share the
    // same transient alpha cache without making it part of the public API.
    internal sealed class HullAlphaMaskCache
    {
        internal Texture2D Texture;
        internal int Resolution;
        internal float Threshold;
        internal bool[] Solid;
        internal bool IsValid;
    }

    internal struct ShieldHitPulse
    {
        internal bool Valid;
        internal Vector3 WorldPosition;
        internal int StartTick;
        internal float Strength;
        internal float Radius;
        internal bool BrokeShield;
        internal string DamageCategory;
        internal int Seed;
        internal Vector2 ImpactDirectionXZ;
        internal Vector2 HullUv;
        internal bool HasHullUv;
        internal bool ImpactClampedToHull;
        internal bool DirectionalSurfaceHit;
        internal bool NearestSolidFallback;
        internal bool HullMaskAvailable;
        internal string ClampMethod;
        internal float SurfaceOffsetDistance;
        internal bool VerticalTighteningApplied;
        internal bool VerticalInwardBiasApplied;
        internal Vector3 CorrectedBeforeVerticalInwardBias;
        internal float ShieldStrengthAtHit;
        internal bool WasLowShieldAtHit;
        internal float SplashRotation;
        internal int SplashCount;
    }
}

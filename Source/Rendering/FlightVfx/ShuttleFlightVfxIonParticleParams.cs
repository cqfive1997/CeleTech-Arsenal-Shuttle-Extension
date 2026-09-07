using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal struct ShuttleFlightVfxIonParticleParams
    {
        internal int Count;
        internal float MinV;
        internal float MaxV;
        internal float MinWidth;
        internal float MaxWidth;
        internal float MinLength;
        internal float MaxLength;
        internal float MinAlpha;
        internal float MaxAlpha;
        internal float Spread;
        internal float Speed;
        internal float DriftSpeed;
        internal float CylinderEnd;
        internal float TipRadius;
        internal float FlowScale;
        internal float FlowSpeed;
        internal float FilamentStrength;
        internal float PulseStrength;
        internal Color CoreColor;
        internal Color OuterColor;
        internal Color HaloColor;

        internal ShuttleFlightVfxIonParticleParams(
            int count,
            float minV,
            float maxV,
            float minWidth,
            float maxWidth,
            float minLength,
            float maxLength,
            float minAlpha,
            float maxAlpha,
            float spread,
            float speed,
            float driftSpeed,
            float cylinderEnd,
            float tipRadius,
            float flowScale,
            float flowSpeed,
            float filamentStrength,
            float pulseStrength,
            Color coreColor,
            Color outerColor,
            Color haloColor)
        {
            this.Count = count;
            this.MinV = minV;
            this.MaxV = maxV;
            this.MinWidth = minWidth;
            this.MaxWidth = maxWidth;
            this.MinLength = minLength;
            this.MaxLength = maxLength;
            this.MinAlpha = minAlpha;
            this.MaxAlpha = maxAlpha;
            this.Spread = spread;
            this.Speed = speed;
            this.DriftSpeed = driftSpeed;
            this.CylinderEnd = cylinderEnd;
            this.TipRadius = tipRadius;
            this.FlowScale = flowScale;
            this.FlowSpeed = flowSpeed;
            this.FilamentStrength = filamentStrength;
            this.PulseStrength = pulseStrength;
            this.CoreColor = coreColor;
            this.OuterColor = outerColor;
            this.HaloColor = haloColor;
        }
    }
}

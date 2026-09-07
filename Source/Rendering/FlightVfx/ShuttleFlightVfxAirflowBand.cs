using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal struct ShuttleFlightVfxAirflowBand
    {
        internal Vector2 TextureUv;
        internal Vector2 LocalDirection;
        internal float BaseLength;
        internal float BaseWidth;
        internal float IntensityScale;

        internal ShuttleFlightVfxAirflowBand(
            Vector2 textureUv,
            Vector2 localDirection,
            float baseLength,
            float baseWidth,
            float intensityScale)
        {
            this.TextureUv = textureUv;
            this.LocalDirection = localDirection;
            this.BaseLength = baseLength;
            this.BaseWidth = baseWidth;
            this.IntensityScale = intensityScale;
        }
    }
}

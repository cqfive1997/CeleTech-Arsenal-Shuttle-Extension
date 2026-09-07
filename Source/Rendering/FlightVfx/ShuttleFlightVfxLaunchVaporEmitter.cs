using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal struct ShuttleFlightVfxLaunchVaporEmitter
    {
        internal string Id;
        internal Vector2 TextureUv;
        internal Vector2 LocalDirection;
        internal float BaseLength;
        internal float BaseWidth;
        internal float IntensityScale;
        internal ShuttleFlightVfxLaunchVaporEmitterKind Kind;

        internal ShuttleFlightVfxLaunchVaporEmitter(
            string id,
            Vector2 textureUv,
            Vector2 localDirection,
            float baseLength,
            float baseWidth,
            float intensityScale,
            ShuttleFlightVfxLaunchVaporEmitterKind kind)
        {
            this.Id = id;
            this.TextureUv = textureUv;
            this.LocalDirection = localDirection;
            this.BaseLength = baseLength;
            this.BaseWidth = baseWidth;
            this.IntensityScale = intensityScale;
            this.Kind = kind;
        }
    }
}

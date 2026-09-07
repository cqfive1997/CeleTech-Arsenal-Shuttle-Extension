using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal struct ShuttleThrusterAnchor
    {
        internal string Id;
        internal ShuttleThrusterKind Kind;
        internal Vector2 TextureUv;
        internal Vector2 LocalDirection;
        internal float MarkerSize;

        internal ShuttleThrusterAnchor(
            string id,
            ShuttleThrusterKind kind,
            Vector2 textureUv,
            Vector2 localDirection,
            float markerSize)
        {
            this.Id = id;
            this.Kind = kind;
            this.TextureUv = textureUv;
            this.LocalDirection = localDirection;
            this.MarkerSize = markerSize;
        }
    }
}

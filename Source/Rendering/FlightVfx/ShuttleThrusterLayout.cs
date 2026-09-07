using System;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleThrusterLayout
    {
        private readonly ShuttleThrusterAnchor[] anchors;

        internal ShuttleThrusterLayout(ShuttleThrusterAnchor[] anchors)
        {
            this.anchors = anchors ?? new ShuttleThrusterAnchor[0];
        }

        internal int Count
        {
            get
            {
                return this.anchors.Length;
            }
        }

        internal ShuttleThrusterAnchor GetAnchor(int index)
        {
            if (index < 0 || index >= this.anchors.Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return this.anchors[index];
        }
    }
}

using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal struct ShuttleFlightVfxGroundImpactGroup
    {
        internal static readonly ShuttleFlightVfxGroundImpactGroup Empty =
            new ShuttleFlightVfxGroundImpactGroup(Vector3.zero, 0f, 0f, false);

        internal Vector3 Center;
        internal float Intensity;
        internal float BaseRadius;
        internal bool IsValid;

        internal ShuttleFlightVfxGroundImpactGroup(
            Vector3 center,
            float intensity,
            float baseRadius,
            bool isValid)
        {
            this.Center = center;
            this.Intensity = intensity;
            this.BaseRadius = baseRadius;
            this.IsValid = isValid;
        }
    }
}

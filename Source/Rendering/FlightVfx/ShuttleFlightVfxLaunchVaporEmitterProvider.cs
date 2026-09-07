using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal static class ShuttleFlightVfxLaunchVaporEmitterProvider
    {
        private static readonly ShuttleFlightVfxLaunchVaporEmitter[] emitters =
        {
            new ShuttleFlightVfxLaunchVaporEmitter(
                "UpperRearCurl",
                new Vector2(0.15f, 0.84f),
                new Vector2(-0.85f, 0.25f),
                1.55f,
                0.75f,
                1.00f,
                ShuttleFlightVfxLaunchVaporEmitterKind.Curl),
            new ShuttleFlightVfxLaunchVaporEmitter(
                "LowerRearCurl",
                new Vector2(0.16f, 0.22f),
                new Vector2(-0.85f, -0.25f),
                1.45f,
                0.70f,
                0.85f,
                ShuttleFlightVfxLaunchVaporEmitterKind.Curl),
            new ShuttleFlightVfxLaunchVaporEmitter(
                "UpperRearPlume",
                new Vector2(0.10f, 0.76f),
                new Vector2(-1f, 0.10f),
                1.85f,
                0.45f,
                0.70f,
                ShuttleFlightVfxLaunchVaporEmitterKind.RearEdgePlume),
            new ShuttleFlightVfxLaunchVaporEmitter(
                "LowerRearPlume",
                new Vector2(0.10f, 0.32f),
                new Vector2(-1f, -0.10f),
                1.85f,
                0.45f,
                0.70f,
                ShuttleFlightVfxLaunchVaporEmitterKind.RearEdgePlume),
            new ShuttleFlightVfxLaunchVaporEmitter(
                "BellyHazeRear",
                new Vector2(0.36f, 0.29f),
                new Vector2(-0.25f, -1f),
                1.00f,
                0.80f,
                0.55f,
                ShuttleFlightVfxLaunchVaporEmitterKind.BellyHaze),
            new ShuttleFlightVfxLaunchVaporEmitter(
                "BellyHazeMid",
                new Vector2(0.54f, 0.29f),
                new Vector2(0.05f, -1f),
                1.05f,
                0.85f,
                0.50f,
                ShuttleFlightVfxLaunchVaporEmitterKind.BellyHaze)
        };

        internal static ShuttleFlightVfxLaunchVaporEmitter[] Emitters
        {
            get
            {
                return emitters;
            }
        }
    }
}

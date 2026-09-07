using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal static class ShuttleFlightVfxAirflowBandProvider
    {
        private static readonly ShuttleFlightVfxAirflowBand[] streakBands =
        {
            new ShuttleFlightVfxAirflowBand(
                new Vector2(0.34f, 0.84f),
                new Vector2(-1f, 0f),
                3.0f,
                0.20f,
                1.00f),
            new ShuttleFlightVfxAirflowBand(
                new Vector2(0.36f, 0.24f),
                new Vector2(-1f, 0f),
                3.0f,
                0.20f,
                1.00f)
        };

        private static readonly ShuttleFlightVfxAirflowBand[] wakeBands =
        {
            new ShuttleFlightVfxAirflowBand(
                new Vector2(0.20f, 0.78f),
                new Vector2(-1f, 0f),
                2.8f,
                0.42f,
                0.72f),
            new ShuttleFlightVfxAirflowBand(
                new Vector2(0.20f, 0.30f),
                new Vector2(-1f, 0f),
                2.8f,
                0.42f,
                0.72f)
        };

        internal static ShuttleFlightVfxAirflowBand[] StreakBands
        {
            get
            {
                return streakBands;
            }
        }

        internal static ShuttleFlightVfxAirflowBand[] WakeBands
        {
            get
            {
                return wakeBands;
            }
        }
    }
}

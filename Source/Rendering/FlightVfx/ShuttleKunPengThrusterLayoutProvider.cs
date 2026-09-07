using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal static class ShuttleKunPengThrusterLayoutProvider
    {
        private static readonly ShuttleThrusterLayout layout =
            new ShuttleThrusterLayout(new ShuttleThrusterAnchor[]
            {
                new ShuttleThrusterAnchor(
                    "TailMainUpper",
                    ShuttleThrusterKind.TailMain,
                    new Vector2(-0.035f, 0.705f),
                    new Vector2(-1f, 0f),
                    0.18f),
                new ShuttleThrusterAnchor(
                    "TailMainLower",
                    ShuttleThrusterKind.TailMain,
                    new Vector2(-0.035f, 0.405f),
                    new Vector2(-1f, 0f),
                    0.18f),
                new ShuttleThrusterAnchor(
                    "RearVtolLeft",
                    ShuttleThrusterKind.RearVtol,
                    new Vector2(0.305f, 0.355f),
                    new Vector2(0f, -1f),
                    0.14f),
                new ShuttleThrusterAnchor(
                    "RearVtolRight",
                    ShuttleThrusterKind.RearVtol,
                    new Vector2(0.355f, 0.355f),
                    new Vector2(0f, -1f),
                    0.14f),
                new ShuttleThrusterAnchor(
                    "BellyVtolLeft",
                    ShuttleThrusterKind.BellyVtol,
                    new Vector2(0.515f, 0.335f),
                    new Vector2(0f, -1f),
                    0.14f),
                new ShuttleThrusterAnchor(
                    "BellyVtolRight",
                    ShuttleThrusterKind.BellyVtol,
                    new Vector2(0.575f, 0.335f),
                    new Vector2(0f, -1f),
                    0.14f),
                new ShuttleThrusterAnchor(
                    "AirflowCenter",
                    ShuttleThrusterKind.AirflowReference,
                    new Vector2(0.55f, 0.52f),
                    new Vector2(1f, 0f),
                    0.22f)
            });

        internal static ShuttleThrusterLayout Layout
        {
            get
            {
                return layout;
            }
        }
    }
}

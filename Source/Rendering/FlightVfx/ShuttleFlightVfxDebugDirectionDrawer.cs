using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxDebugDirectionDrawer
    {
        internal static readonly ShuttleFlightVfxDebugDirectionDrawer Shared =
            new ShuttleFlightVfxDebugDirectionDrawer();

        private const float DirectionWidthFactor = 0.28f;

        internal void Draw(
            Vector3 markerCenter,
            Quaternion bodyRotation,
            Vector2 localDirection,
            float length,
            float markerSize,
            Material material)
        {
            if (material == null ||
                material == BaseContent.BadMat ||
                length <= 0f ||
                localDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 localDirection3 = new Vector3(localDirection.x, 0f, localDirection.y).normalized;
            Vector3 worldDirection = bodyRotation * localDirection3;
            if (worldDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            worldDirection.Normalize();
            Vector3 center = markerCenter + (worldDirection * length * 0.5f);
            Quaternion lineRotation = Quaternion.LookRotation(worldDirection, bodyRotation * Vector3.up);
            float width = Mathf.Max(0.015f, markerSize * DirectionWidthFactor);

            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(center, lineRotation, new Vector3(width, 1f, length));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }
    }
}

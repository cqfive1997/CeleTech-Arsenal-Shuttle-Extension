using CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx.Vapor
{
    internal struct ShuttleFlightVfxVaporAnchorFrame
    {
        internal Vector3 RootLocal;
        internal Vector3 RootWorld;
        internal Vector2 TangentLocal;
        internal Vector2 NormalLocal;
        internal Vector3 TangentWorld;
        internal Vector3 NormalWorld;
        internal Quaternion MeshRotation;
    }

    internal static class ShuttleFlightVfxVaporAnchorFrameResolver
    {
        internal static ShuttleFlightVfxVaporAnchorFrame ResolveCalibratedFrame(
            Vector2 drawSize,
            Vector3 origin,
            Quaternion rotation,
            ShuttleFlightVfxVaporAnchor anchor,
            float normalOffsetLocal,
            float streamwiseOffsetLocal)
        {
            Vector3 rootLocalOffset = ShuttleFlightVfxAnchorMapper.TextureUvToLocalOffset(anchor.RootTextureUv, drawSize);
            Vector2 localNormal;
            Vector2 localTangent;
            if (anchor.HasCalibratedDirections)
            {
                Vector3 tailLocal = ShuttleFlightVfxAnchorMapper.TextureUvToLocalOffset(anchor.TailTextureUv, drawSize);
                Vector3 outwardLocal = ShuttleFlightVfxAnchorMapper.TextureUvToLocalOffset(anchor.OutwardTextureUv, drawSize);
                localTangent = NormalizeLocalDelta(tailLocal - rootLocalOffset, ResolveLocalDirection(anchor.LengthAxisDeg));
                localNormal = NormalizeLocalDelta(outwardLocal - rootLocalOffset, ResolveLocalDirection(anchor.OutwardNormalDeg));
            }
            else
            {
                localTangent = ResolveLocalDirection(anchor.LengthAxisDeg);
                localNormal = ResolveLocalDirection(anchor.OutwardNormalDeg);
            }

            Vector3 rootLocal = rootLocalOffset +
                new Vector3(localNormal.x * normalOffsetLocal, 0f, localNormal.y * normalOffsetLocal) +
                new Vector3(localTangent.x * streamwiseOffsetLocal, 0f, localTangent.y * streamwiseOffsetLocal);
            Vector3 tangentWorld = ResolveWorldDirection(rotation, localTangent);
            Vector3 normalWorld = ResolveWorldDirection(rotation, localNormal);

            ShuttleFlightVfxVaporAnchorFrame frame = new ShuttleFlightVfxVaporAnchorFrame();
            frame.RootLocal = rootLocal;
            frame.RootWorld = origin + (rotation * rootLocal);
            frame.TangentLocal = localTangent;
            frame.NormalLocal = localNormal;
            frame.TangentWorld = tangentWorld;
            frame.NormalWorld = normalWorld;
            frame.MeshRotation = Quaternion.LookRotation(tangentWorld, Vector3.up);
            return frame;
        }

        internal static Vector3 ResolveOrigin(Skyfaller skyfaller, Vector3 drawLoc, Rot4 drawRotation)
        {
            return drawLoc + skyfaller.Graphic.DrawOffset(drawRotation);
        }

        internal static Quaternion BuildRotation(Graphic graphic, Rot4 drawRotation, float extraRotation)
        {
            Quaternion rotation = ShuttleFlightVfxAnchorMapper.EastAuthoredRotation(
                drawRotation,
                extraRotation);

            if (graphic.data != null && graphic.data.addTopAltitudeBias)
            {
                rotation *= Quaternion.Euler(Vector3.left * 2f);
            }

            return rotation;
        }

        internal static Vector2 ResolveLocalDirection(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return new Vector2(0f, 1f);
            }

            return direction.normalized;
        }

        private static Vector2 NormalizeLocalDelta(Vector3 delta, Vector2 fallback)
        {
            Vector2 direction = new Vector2(delta.x, delta.z);
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return fallback;
            }

            return direction.normalized;
        }

        private static Vector3 ResolveWorldDirection(Quaternion rotation, Vector2 localDirection)
        {
            Vector3 world = rotation * new Vector3(localDirection.x, 0f, localDirection.y);
            world.y = 0f;
            if (world.sqrMagnitude <= 0.0001f)
            {
                return new Vector3(0f, 0f, 1f);
            }

            return world.normalized;
        }
    }
}

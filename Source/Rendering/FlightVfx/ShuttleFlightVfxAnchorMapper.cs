using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal static class ShuttleFlightVfxAnchorMapper
    {
        internal static Vector3 TextureUvToLocalOffset(Vector2 textureUv, Vector2 drawSize)
        {
            float localX = (textureUv.x - 0.5f) * drawSize.x;
            float localZ = (textureUv.y - 0.5f) * drawSize.y;
            return new Vector3(localX, 0f, localZ);
        }

        internal static Vector3 ResolveAnchorWorldOffset(
            ShuttleThrusterAnchor anchor,
            Vector2 drawSize,
            Rot4 rotation,
            float extraRotation)
        {
            Vector2 textureUv;
            if (TryResolveDirectionalTextureUv(anchor, rotation, out textureUv))
            {
                return ApplyExtraRotation(
                    TextureUvToLocalOffset(textureUv, drawSize),
                    extraRotation);
            }

            return EastAuthoredRotation(rotation, extraRotation) *
                TextureUvToLocalOffset(anchor.TextureUv, drawSize);
        }

        internal static Vector3 ResolveAuthoredWorldOffset(
            ShuttleThrusterAnchor anchor,
            Vector3 eastAuthoredOffset,
            Rot4 rotation,
            float extraRotation)
        {
            if (UsesDirectionalTextureUv(anchor, rotation))
            {
                return ApplyExtraRotation(eastAuthoredOffset, extraRotation);
            }

            return EastAuthoredRotation(rotation, extraRotation) * eastAuthoredOffset;
        }

        internal static Vector3 ResolveAnchorWorldDirection(
            ShuttleThrusterAnchor anchor,
            Rot4 rotation,
            float extraRotation)
        {
            Vector2 direction = ResolveDirectionalLocalDirection(anchor, rotation);
            Vector3 worldDirection = new Vector3(direction.x, 0f, direction.y);
            worldDirection = ApplyExtraRotation(worldDirection, extraRotation);
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            return worldDirection.normalized;
        }

        internal static Vector3 ApplyExtraRotation(Vector3 offset, float extraRotation)
        {
            if (Mathf.Abs(extraRotation) <= 0.0001f)
            {
                return offset;
            }

            return Quaternion.AngleAxis(extraRotation, Vector3.up) * offset;
        }

        internal static Quaternion EastAuthoredRotation(Rot4 rotation, float extraRotation)
        {
            float angle = EastAuthoredAngle(rotation) + extraRotation;
            return Mathf.Abs(angle) <= 0.0001f
                ? Quaternion.identity
                : Quaternion.AngleAxis(angle, Vector3.up);
        }

        private static float EastAuthoredAngle(Rot4 rotation)
        {
            if (rotation == Rot4.South)
            {
                return 90f;
            }

            if (rotation == Rot4.West)
            {
                return 180f;
            }

            if (rotation == Rot4.North)
            {
                return -90f;
            }

            return 0f;
        }

        internal static bool UsesDirectionalTextureUv(
            ShuttleThrusterAnchor anchor,
            Rot4 rotation)
        {
            Vector2 textureUv;
            return TryResolveDirectionalTextureUv(anchor, rotation, out textureUv);
        }

        private static bool TryResolveDirectionalTextureUv(
            ShuttleThrusterAnchor anchor,
            Rot4 rotation,
            out Vector2 textureUv)
        {
            textureUv = Vector2.zero;
            if (rotation == Rot4.North)
            {
                if (anchor.Id == "TailMainUpper")
                {
                    textureUv = new Vector2(0.19f, 0.135f);
                    return true;
                }

                if (anchor.Id == "TailMainLower")
                {
                    textureUv = new Vector2(0.81f, 0.135f);
                    return true;
                }

                return false;
            }

            if (rotation == Rot4.West)
            {
                if (anchor.Id == "TailMainUpper")
                {
                    textureUv = new Vector2(0.955f, 0.775f);
                    return true;
                }

                if (anchor.Id == "TailMainLower")
                {
                    textureUv = new Vector2(0.955f, 0.355f);
                    return true;
                }

                return false;
            }

            if (rotation != Rot4.South)
            {
                return false;
            }

            if (anchor.Id == "TailMainUpper")
            {
                textureUv = new Vector2(0.19f, 0.865f);
                return true;
            }

            if (anchor.Id == "TailMainLower")
            {
                textureUv = new Vector2(0.81f, 0.865f);
                return true;
            }

            if (anchor.Id == "RearVtolLeft")
            {
                textureUv = new Vector2(0.41f, 0.61f);
                return true;
            }

            if (anchor.Id == "RearVtolRight")
            {
                textureUv = new Vector2(0.59f, 0.61f);
                return true;
            }

            if (anchor.Id == "BellyVtolLeft")
            {
                textureUv = new Vector2(0.43f, 0.43f);
                return true;
            }

            if (anchor.Id == "BellyVtolRight")
            {
                textureUv = new Vector2(0.57f, 0.43f);
                return true;
            }

            return false;
        }

        private static Vector2 ResolveDirectionalLocalDirection(
            ShuttleThrusterAnchor anchor,
            Rot4 rotation)
        {
            if (anchor.Kind == ShuttleThrusterKind.TailMain)
            {
                return ResolveTailMainDirection(rotation);
            }

            if (anchor.Kind == ShuttleThrusterKind.RearVtol ||
                anchor.Kind == ShuttleThrusterKind.BellyVtol)
            {
                return new Vector2(0f, -1f);
            }

            Vector3 fallback = EastAuthoredRotation(rotation, 0f) *
                new Vector3(anchor.LocalDirection.x, 0f, anchor.LocalDirection.y);
            return new Vector2(fallback.x, fallback.z);
        }

        private static Vector2 ResolveTailMainDirection(Rot4 rotation)
        {
            if (rotation == Rot4.South)
            {
                return new Vector2(0f, 1f);
            }

            if (rotation == Rot4.West)
            {
                return new Vector2(1f, 0f);
            }

            if (rotation == Rot4.North)
            {
                return new Vector2(0f, -1f);
            }

            return new Vector2(-1f, 0f);
        }
    }
}

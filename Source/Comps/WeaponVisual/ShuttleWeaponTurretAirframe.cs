using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class ShuttleWeaponTurretAirframe
    {
        private const float MeshPoolArtworkRotationCorrectionDegrees = 180f;

        internal static Vector3 ResolveHostMountWorldPosition(
            ThingWithComps host,
            ShuttleWeaponTurretVisualMount mount)
        {
            if (host == null)
            {
                return Vector3.zero;
            }

            Vector3 drawLoc = host.Spawned ? host.DrawPos : host.Position.ToVector3Shifted();
            if (host.Graphic != null)
            {
                drawLoc += host.Graphic.DrawOffset(host.Rotation);
            }

            return drawLoc + ResolveRotatedMountOffset(mount, host.Rotation);
        }

        internal static Vector3 ResolveRotatedMountOffset(
            ShuttleWeaponTurretVisualMount mount,
            Rot4 rotation)
        {
            return RotateEastAuthoredOffset(
                ResolveEastAuthoredLocalOffset(mount, rotation),
                rotation);
        }

        internal static Vector3 ResolveEastAuthoredLocalOffset(
            ShuttleWeaponTurretVisualMount mount,
            Rot4 rotation)
        {
            if (mount == null)
            {
                return Vector3.zero;
            }

            Vector3 local = mount.localOffset;
            if (rotation == Rot4.West)
            {
                local += mount.westLocalOffsetAdjustment;
            }

            return local;
        }

        internal static Vector3 RotateEastAuthoredOffset(Vector3 offset, Rot4 rotation)
        {
            if (rotation == Rot4.South)
            {
                return new Vector3(offset.z, offset.y, -offset.x);
            }

            if (rotation == Rot4.West)
            {
                return new Vector3(-offset.x, offset.y, -offset.z);
            }

            if (rotation == Rot4.North)
            {
                return new Vector3(-offset.z, offset.y, offset.x);
            }

            return offset;
        }

        internal static float RotateEastAuthoredAngle(float angle, Rot4 rotation)
        {
            if (rotation == Rot4.South)
            {
                return NormalizeAngle(angle + 90f);
            }

            if (rotation == Rot4.West)
            {
                return NormalizeAngle(angle + 180f);
            }

            if (rotation == Rot4.North)
            {
                return NormalizeAngle(angle - 90f);
            }

            return NormalizeAngle(angle);
        }

        internal static Vector3 DirectionFromAngle(float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
        }

        internal static float ResolveMeshDrawAngle(
            float artworkRotationOffset,
            float airframeAngle,
            float extraRotation)
        {
            return NormalizeAngle(
                artworkRotationOffset +
                airframeAngle +
                extraRotation +
                MeshPoolArtworkRotationCorrectionDegrees);
        }

        internal static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle < 0f)
            {
                angle += 360f;
            }

            return angle;
        }
    }
}

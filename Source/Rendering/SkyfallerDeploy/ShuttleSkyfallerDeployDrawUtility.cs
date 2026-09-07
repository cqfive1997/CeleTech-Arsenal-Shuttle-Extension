using RimWorld;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.SkyfallerDeploy
{
    internal struct ShuttleSkyfallerDeployDrawParms
    {
        internal Mesh Mesh;
        internal Matrix4x4 Matrix;
    }

    internal static class ShuttleSkyfallerDeployDrawUtility
    {
        internal static Thing ResolveThingForGraphic(Skyfaller skyfaller)
        {
            if (skyfaller == null)
            {
                return null;
            }

            if (skyfaller.def != null && skyfaller.def.graphicData != null)
            {
                return skyfaller;
            }

            if (skyfaller.innerContainer == null || skyfaller.innerContainer.Count == 0)
            {
                return skyfaller;
            }

            return skyfaller.innerContainer[0];
        }

        internal static Rot4 ResolveDrawRotation(Thing thingForGraphic, bool flip)
        {
            if (thingForGraphic == null)
            {
                return Rot4.North;
            }

            return flip ? thingForGraphic.Rotation.Opposite : thingForGraphic.Rotation;
        }

        internal static bool TryBuildDrawParms(
            Skyfaller skyfaller,
            Vector3 drawLoc,
            Rot4 rot,
            float extraRotation,
            Vector3 localOffset,
            out ShuttleSkyfallerDeployDrawParms drawParms)
        {
            drawParms = default(ShuttleSkyfallerDeployDrawParms);
            if (skyfaller == null || skyfaller.Graphic == null)
            {
                return false;
            }

            Graphic graphic = skyfaller.Graphic;
            Mesh mesh = graphic.MeshAt(rot);
            if (mesh == null)
            {
                return false;
            }

            Quaternion quaternion = graphic.QuatFromRot(rot);
            if (extraRotation != 0f)
            {
                quaternion *= Quaternion.Euler(Vector3.up * extraRotation);
            }

            if (graphic.data != null && graphic.data.addTopAltitudeBias)
            {
                quaternion *= Quaternion.Euler(Vector3.left * 2f);
            }

            Vector3 finalDrawLoc = drawLoc + graphic.DrawOffset(rot);
            if (localOffset != Vector3.zero)
            {
                finalDrawLoc += ShuttleFlightVfxAnchorMapper.ApplyExtraRotation(
                    localOffset,
                    extraRotation);
            }

            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(finalDrawLoc, quaternion, Vector3.one);

            drawParms.Mesh = mesh;
            drawParms.Matrix = matrix;
            return true;
        }
    }
}

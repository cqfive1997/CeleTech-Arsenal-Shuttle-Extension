using CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx.Vapor
{
    internal sealed class ShuttleFlightVfxVaporCalibrationDrawer
    {
        internal static readonly ShuttleFlightVfxVaporCalibrationDrawer Shared =
            new ShuttleFlightVfxVaporCalibrationDrawer(
                ShuttleFlightVfxVaporCalibrationMaterialCache.Shared);

        private const float OverlayYOffset = 2.15f;
        private const float CenterMarkerSize = 0.055f;
        private const float RootMarkerSize = 0.070f;
        private const float CenterCrossLength = 0.22f;
        private const float RootCrossLength = 0.26f;
        private const float LineWidth = 0.026f;
        private const int OutlineSegments = 28;

        private readonly ShuttleFlightVfxVaporCalibrationMaterialCache materialCache;

        private ShuttleFlightVfxVaporCalibrationDrawer(
            ShuttleFlightVfxVaporCalibrationMaterialCache materialCache)
        {
            this.materialCache = materialCache;
        }

        internal void Draw(
            Skyfaller skyfaller,
            Vector3 drawLoc,
            Rot4 drawRotation,
            float extraRotation)
        {
            if (skyfaller == null ||
                skyfaller.Graphic == null ||
                this.materialCache == null)
            {
                return;
            }

            ShuttleFlightVfxVaporAnchor[] anchors = ShuttleFlightVfxVaporLayoutProvider.Anchors;
            if (anchors == null || anchors.Length == 0)
            {
                return;
            }

            Vector2 drawSize = ShuttleFlightVfxDrawSizeResolver.ResolveDrawSize(skyfaller);
            Quaternion rotation = ShuttleFlightVfxVaporAnchorFrameResolver.BuildRotation(
                skyfaller.Graphic,
                drawRotation,
                extraRotation);
            Vector3 origin = ShuttleFlightVfxVaporAnchorFrameResolver.ResolveOrigin(skyfaller, drawLoc, drawRotation);

            for (int i = 0; i < anchors.Length; i++)
            {
                ShuttleFlightVfxVaporAnchor anchor = anchors[i];
                if (!ShuttleFlightVfxVaporLayoutProvider.IsCalibrationAnchor(anchor))
                {
                    continue;
                }

                if (!ShuttleFlightVfxVaporDebugSettings.ShouldDrawAnchorId(anchor.Id))
                {
                    continue;
                }

                this.DrawAnchor(anchor, drawSize, origin, rotation);
            }
        }

        private void DrawAnchor(
            ShuttleFlightVfxVaporAnchor anchor,
            Vector2 drawSize,
            Vector3 origin,
            Quaternion bodyRotation)
        {
            Vector2 effectiveSize = new Vector2(
                anchor.BaseSize.x * anchor.SizeMultiplier * anchor.LengthMultiplier,
                anchor.BaseSize.y * anchor.SizeMultiplier * anchor.WidthMultiplier);

            ShuttleFlightVfxVaporAnchorFrame frame =
                ShuttleFlightVfxVaporAnchorFrameResolver.ResolveCalibratedFrame(
                    drawSize,
                    origin,
                    bodyRotation,
                    anchor,
                    anchor.CenterOffsetLocal,
                    anchor.StreamwiseOffsetLocal);
            Vector3 centerLocal = frame.RootLocal;
            if (anchor.Kind == ShuttleFlightVfxVaporKind.EdgeRibbon)
            {
                centerLocal += new Vector3(
                    frame.TangentLocal.x * effectiveSize.x * 0.5f,
                    0f,
                    frame.TangentLocal.y * effectiveSize.x * 0.5f);
            }

            Vector3 root = frame.RootWorld;
            Vector3 center = origin + (bodyRotation * centerLocal);
            root.y += OverlayYOffset;
            center.y += OverlayYOffset;

            Vector3 worldTangent = frame.TangentWorld;
            Vector3 worldNormal = frame.NormalWorld;

            float tangentLength = Mathf.Max(0.42f, effectiveSize.x * 0.72f);
            float normalLength = Mathf.Max(0.36f, effectiveSize.y * 1.20f);
            float rootLength = Mathf.Max(0.36f, effectiveSize.y * 0.95f);

            this.DrawOutline(center, bodyRotation, anchor, frame.TangentLocal, frame.NormalLocal, effectiveSize);
            this.DrawRootEdge(root, worldNormal, rootLength);
            this.DrawRootPoint(root, bodyRotation);
            this.DrawCenter(center, bodyRotation);
            this.DrawArrow(root, worldTangent, tangentLength, this.materialCache.Tangent);
            this.DrawArrow(root, worldNormal, anchor.CenterOffsetLocal + normalLength, this.materialCache.Normal);
        }

        private void DrawRootPoint(Vector3 root, Quaternion bodyRotation)
        {
            this.DrawLine(
                root - (bodyRotation * Vector3.right * RootCrossLength * 0.5f),
                root + (bodyRotation * Vector3.right * RootCrossLength * 0.5f),
                RootMarkerSize,
                this.materialCache.RootEdge);
            this.DrawLine(
                root - (bodyRotation * Vector3.forward * RootCrossLength * 0.5f),
                root + (bodyRotation * Vector3.forward * RootCrossLength * 0.5f),
                RootMarkerSize,
                this.materialCache.RootEdge);
        }

        private void DrawCenter(Vector3 center, Quaternion bodyRotation)
        {
            this.DrawLine(
                center - (bodyRotation * Vector3.right * CenterCrossLength * 0.5f),
                center + (bodyRotation * Vector3.right * CenterCrossLength * 0.5f),
                CenterMarkerSize,
                this.materialCache.Center);
            this.DrawLine(
                center - (bodyRotation * Vector3.forward * CenterCrossLength * 0.5f),
                center + (bodyRotation * Vector3.forward * CenterCrossLength * 0.5f),
                CenterMarkerSize,
                this.materialCache.Center);
        }

        private void DrawOutline(
            Vector3 center,
            Quaternion bodyRotation,
            ShuttleFlightVfxVaporAnchor anchor,
            Vector2 localTangent,
            Vector2 localNormal,
            Vector2 effectiveSize)
        {
            Vector3 previous = this.ResolveOutlinePoint(
                center,
                bodyRotation,
                localTangent,
                localNormal,
                effectiveSize,
                0f);

            for (int i = 1; i <= OutlineSegments; i++)
            {
                float angle = (6.283185f * i) / OutlineSegments;
                Vector3 current = this.ResolveOutlinePoint(
                    center,
                    bodyRotation,
                    localTangent,
                    localNormal,
                    effectiveSize,
                    angle);
                this.DrawLine(previous, current, LineWidth, this.materialCache.Outline);
                previous = current;
            }
        }

        private Vector3 ResolveOutlinePoint(
            Vector3 center,
            Quaternion bodyRotation,
            Vector2 localTangent,
            Vector2 localNormal,
            Vector2 size,
            float angle)
        {
            float tangentOffset = Mathf.Cos(angle) * size.x * 0.5f;
            float normalOffset = Mathf.Sin(angle) * size.y * 0.5f;
            Vector2 local =
                (localTangent * tangentOffset) +
                (localNormal * normalOffset);
            return center + (bodyRotation * new Vector3(local.x, 0f, local.y));
        }

        private void DrawRootEdge(
            Vector3 rootCenter,
            Vector3 worldTangent,
            float rootLength)
        {
            this.DrawLine(
                rootCenter - (worldTangent * rootLength * 0.5f),
                rootCenter + (worldTangent * rootLength * 0.5f),
                LineWidth * 1.45f,
                this.materialCache.RootEdge);
        }

        private void DrawArrow(
            Vector3 start,
            Vector3 direction,
            float length,
            Material material)
        {
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            direction.Normalize();
            Vector3 end = start + (direction * length);
            this.DrawLine(start, end, LineWidth * 1.15f, material);

            Vector3 up = Vector3.up;
            float headLength = Mathf.Max(0.12f, length * 0.18f);
            Vector3 headLeft = Quaternion.AngleAxis(150f, up) * direction;
            Vector3 headRight = Quaternion.AngleAxis(-150f, up) * direction;
            this.DrawLine(end, end + (headLeft * headLength), LineWidth * 1.15f, material);
            this.DrawLine(end, end + (headRight * headLength), LineWidth * 1.15f, material);
        }

        private void DrawLine(Vector3 start, Vector3 end, float width, Material material)
        {
            if (material == null || material == BaseContent.BadMat)
            {
                return;
            }

            Vector3 direction = end - start;
            float length = direction.magnitude;
            if (length <= 0.001f)
            {
                return;
            }

            direction /= length;
            Vector3 center = start + (direction * length * 0.5f);
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(center, rotation, new Vector3(width, 1f, length));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

    }
}

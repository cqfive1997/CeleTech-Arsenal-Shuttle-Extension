using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal static class ShuttleFlightVfxGroundImpactResolver
    {
        private const float ImpactOffset = 0.95f;
        private const float RearBaseRadius = 0.75f;
        private const float BellyBaseRadius = 0.82f;

        internal static void ResolveGroups(
            Skyfaller skyfaller,
            Vector3 drawLoc,
            Rot4 drawRotation,
            float extraRotation,
            ShuttleFlightVfxState state,
            out ShuttleFlightVfxGroundImpactGroup rearGroup,
            out ShuttleFlightVfxGroundImpactGroup bellyGroup)
        {
            rearGroup = ShuttleFlightVfxGroundImpactGroup.Empty;
            bellyGroup = ShuttleFlightVfxGroundImpactGroup.Empty;

            if (skyfaller == null || skyfaller.Graphic == null || ShuttleKunPengThrusterLayoutProvider.Layout == null)
            {
                return;
            }

            ShuttleThrusterLayout layout = ShuttleKunPengThrusterLayoutProvider.Layout;
            Vector2 drawSize = ShuttleFlightVfxDrawSizeResolver.ResolveDrawSize(skyfaller);
            Vector3 origin = drawLoc + skyfaller.Graphic.DrawOffset(drawRotation);

            Vector3 rearSum = Vector3.zero;
            Vector3 bellySum = Vector3.zero;
            int rearCount = 0;
            int bellyCount = 0;

            for (int i = 0; i < layout.Count; i++)
            {
                ShuttleThrusterAnchor anchor = layout.GetAnchor(i);
                if (anchor.Kind == ShuttleThrusterKind.RearVtol)
                {
                    rearSum += ResolveImpactPoint(
                        anchor,
                        drawSize,
                        origin,
                        drawRotation,
                        extraRotation);
                    rearCount++;
                }
                else if (anchor.Kind == ShuttleThrusterKind.BellyVtol)
                {
                    bellySum += ResolveImpactPoint(
                        anchor,
                        drawSize,
                        origin,
                        drawRotation,
                        extraRotation);
                    bellyCount++;
                }
            }

            if (rearCount > 0)
            {
                rearGroup = new ShuttleFlightVfxGroundImpactGroup(
                    rearSum / rearCount,
                    state.RearVtolIntensity,
                    RearBaseRadius,
                    true);
            }

            if (bellyCount > 0)
            {
                bellyGroup = new ShuttleFlightVfxGroundImpactGroup(
                    bellySum / bellyCount,
                    state.BellyVtolIntensity,
                    BellyBaseRadius,
                    true);
            }
        }

        private static Vector3 ResolveImpactPoint(
            ShuttleThrusterAnchor anchor,
            Vector2 drawSize,
            Vector3 origin,
            Rot4 drawRotation,
            float extraRotation)
        {
            Vector3 anchorWorld = origin +
                ShuttleFlightVfxAnchorMapper.ResolveAnchorWorldOffset(
                    anchor,
                    drawSize,
                    drawRotation,
                    extraRotation);
            Vector3 worldDirection =
                ShuttleFlightVfxAnchorMapper.ResolveAnchorWorldDirection(
                    anchor,
                    drawRotation,
                    extraRotation);
            if (worldDirection.sqrMagnitude <= 0.0001f)
            {
                return anchorWorld;
            }

            worldDirection.Normalize();
            return anchorWorld + (worldDirection * ImpactOffset);
        }
    }
}

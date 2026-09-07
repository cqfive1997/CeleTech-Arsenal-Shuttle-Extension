using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal static class ShuttleFlightVfxDrawSizeResolver
    {
        internal static Vector2 ResolveDrawSize(Skyfaller skyfaller)
        {
            if (skyfaller != null &&
                skyfaller.Graphic != null &&
                skyfaller.Graphic.drawSize != Vector2.zero)
            {
                return skyfaller.Graphic.drawSize;
            }

            Vector2 skyfallerDefSize = ResolveDefGraphicSize(skyfaller != null ? skyfaller.def : null);
            if (skyfallerDefSize != Vector2.zero)
            {
                return skyfallerDefSize;
            }

            Thing innerThing = ResolveInnerThing(skyfaller);
            Vector2 innerDefSize = ResolveDefGraphicSize(innerThing != null ? innerThing.def : null);
            if (innerDefSize != Vector2.zero)
            {
                return innerDefSize;
            }

            ThingDef fallbackDef = innerThing != null && innerThing.def != null
                ? innerThing.def
                : skyfaller != null ? skyfaller.def : null;
            if (fallbackDef != null)
            {
                return new Vector2(
                    Mathf.Max(1f, fallbackDef.size.x),
                    Mathf.Max(1f, fallbackDef.size.z));
            }

            return new Vector2(1f, 1f);
        }

        private static Vector2 ResolveDefGraphicSize(ThingDef def)
        {
            if (def == null || def.graphicData == null)
            {
                return Vector2.zero;
            }

            return def.graphicData.drawSize != Vector2.zero
                ? def.graphicData.drawSize
                : Vector2.zero;
        }

        private static Thing ResolveInnerThing(Skyfaller skyfaller)
        {
            if (skyfaller == null ||
                skyfaller.innerContainer == null ||
                skyfaller.innerContainer.Count == 0)
            {
                return null;
            }

            return skyfaller.innerContainer[0];
        }
    }
}

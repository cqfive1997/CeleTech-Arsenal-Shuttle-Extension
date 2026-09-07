using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common
{
    internal static class ShuttleThingIconDrawer
    {
        internal static bool Draw(Rect rect, Thing thing)
        {
            if (thing == null)
            {
                return false;
            }

            if (TryDrawResolvedThingIcon(rect, thing))
            {
                return true;
            }

            if (TryDrawThingDefIcon(rect, thing))
            {
                return true;
            }

            return false;
        }

        private static bool TryDrawResolvedThingIcon(Rect rect, Thing thing)
        {
            if (thing == null || thing.Destroyed)
            {
                return false;
            }

            Color oldColor = GUI.color;
            try
            {
                Thing drawThing = thing.GetInnerIfMinified();
                if (drawThing == null || drawThing.def == null)
                {
                    return false;
                }

                Blueprint blueprint = drawThing as Blueprint;
                if (blueprint != null && blueprint.EntityToBuild() != null)
                {
                    Widgets.DefIcon(
                        rect,
                        blueprint.EntityToBuild(),
                        blueprint.EntityToBuildStuff(),
                        1f,
                        blueprint.EntityToBuildStyle(),
                        false,
                        null,
                        null,
                        null,
                        1f);
                    return true;
                }

                float scale;
                float angle;
                Vector2 iconProportions;
                Color color;
                Material material;
                Texture texture = Widgets.GetIconFor(
                    drawThing,
                    new Vector2(rect.width, rect.height),
                    null,
                    false,
                    out scale,
                    out angle,
                    out iconProportions,
                    out color,
                    out material);
                if (texture == null || texture == BaseContent.BadTex)
                {
                    return false;
                }

                GUI.color = color;
                ThingStyleDef styleDef = drawThing.StyleDef;
                if ((styleDef != null && styleDef.UIIcon != null) ||
                    !drawThing.def.uiIconPath.NullOrEmpty())
                {
                    rect.position += new Vector2(
                        drawThing.def.uiIconOffset.x * rect.size.x,
                        drawThing.def.uiIconOffset.y * rect.size.y);
                }

                DrawResolvedTexture(rect, drawThing.def, texture, scale, angle, material);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                GUI.color = oldColor;
            }
        }

        private static bool TryDrawThingDefIcon(Rect rect, Thing thing)
        {
            ThingDef thingDef = ResolveThingDef(thing);
            if (thingDef == null)
            {
                return false;
            }

            Material material;
            Texture2D texture;
            try
            {
                texture = Widgets.GetIconFor(thingDef, out material, null, null, null);
            }
            catch
            {
                return false;
            }

            if (texture == null || texture == BaseContent.BadTex)
            {
                return false;
            }

            Color oldColor = GUI.color;
            try
            {
                GUI.color = material != null
                    ? Color.white
                    : thingDef.uiIconColor;
                DrawResolvedTexture(
                    rect,
                    thingDef,
                    texture,
                    GenUI.IconDrawScale(thingDef),
                    thingDef.uiIconAngle,
                    material);
                return true;
            }
            finally
            {
                GUI.color = oldColor;
            }
        }

        private static ThingDef ResolveThingDef(Thing thing)
        {
            if (thing == null)
            {
                return null;
            }

            Thing innerThing = null;
            try
            {
                innerThing = thing.GetInnerIfMinified();
            }
            catch
            {
            }

            ThingDef thingDef = innerThing != null && innerThing.def != null
                ? innerThing.def
                : thing.def;
            if (thingDef != null &&
                thingDef.IsCorpse &&
                thingDef.ingestible != null &&
                thingDef.ingestible.sourceDef != null)
            {
                return thingDef.ingestible.sourceDef;
            }

            return thingDef;
        }

        private static void DrawResolvedTexture(
            Rect rect,
            ThingDef thingDef,
            Texture texture,
            float scale,
            float angle,
            Material material)
        {
            Vector2 drawSize = new Vector2(texture.width, texture.height);
            Rect texCoords = new Rect(0f, 0f, 1f, 1f);
            if (thingDef != null && thingDef.graphicData != null)
            {
                drawSize = thingDef.graphicData.drawSize;
            }

            Widgets.DrawTextureFitted(rect, texture, scale, drawSize, texCoords, angle, material, 1f);
        }
    }
}

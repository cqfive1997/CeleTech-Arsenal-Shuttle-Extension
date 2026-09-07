using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.FireControl
{
    internal sealed class ShuttleFireControlGizmoButtonDrawer
    {
        private static readonly Color IdleBackground =
            new Color(0.035f, 0.065f, 0.085f, 0.96f);
        private static readonly Color ActiveBackground =
            new Color(0.055f, 0.245f, 0.335f, 0.96f);
        private static readonly Color ActiveBorder =
            new Color(0.25f, 0.78f, 0.94f, 1f);
        private static readonly Color IdleBorder =
            new Color(0.18f, 0.5f, 0.62f, 0.95f);

        internal bool Draw(
            Rect rect,
            Texture2D icon,
            string labelKey,
            string descriptionKey,
            bool enabled,
            bool active)
        {
            return this.DrawCore(
                rect,
                icon,
                labelKey,
                ShuttleUIText.Tr(descriptionKey),
                enabled,
                active);
        }

        internal bool DrawTextDescription(
            Rect rect,
            Texture2D icon,
            string labelKey,
            string description,
            bool enabled,
            bool active)
        {
            return this.DrawCore(
                rect,
                icon,
                labelKey,
                description,
                enabled,
                active);
        }

        private bool DrawCore(
            Rect rect,
            Texture2D icon,
            string labelKey,
            string description,
            bool enabled,
            bool active)
        {
            Widgets.DrawBoxSolid(rect, active ? ActiveBackground : IdleBackground);

            Rect iconRect = rect.ContractedBy(1f);
            Texture2D drawIcon = icon ?? BaseContent.BadTex;
            Rect fittedIconRect = GetAspectFitRect(iconRect, drawIcon);
            if (enabled || active)
            {
                GUI.color = active
                    ? Color.white
                    : new Color(0.86f, 0.9f, 0.92f, 1f);
                GUI.DrawTexture(fittedIconRect, drawIcon, ScaleMode.StretchToFill, true);
            }
            else
            {
                GenUI.DrawTextureWithMaterial(
                    fittedIconRect,
                    drawIcon,
                    TexUI.GrayscaleGUI);
                Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, 0.48f));
            }

            GUI.color = active ? ActiveBorder : IdleBorder;
            Widgets.DrawBox(rect, 1);
            GUI.color = Color.white;
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            string disabledReason = enabled || active
                ? null
                : ShuttleUIText.Tr("CT_Shuttle_Defense_GroupNoApplicable");
            TooltipHandler.TipRegion(
                rect,
                this.BuildTooltip(
                    ShuttleUIText.Tr(labelKey),
                    description,
                    disabledReason));

            if (!Widgets.ButtonInvisible(rect))
            {
                return false;
            }

            if (enabled)
            {
                return true;
            }

            if (active)
            {
                return false;
            }

            Messages.Message(
                ShuttleUIText.Tr(
                    "CT_Shuttle_UI_OptionUnavailableFormat",
                    "DisabledCommand".Translate(),
                    disabledReason),
                MessageTypeDefOf.RejectInput,
                false);
            return false;
        }

        private static Rect GetAspectFitRect(Rect bounds, Texture2D texture)
        {
            if (texture == null || texture.width <= 0 || texture.height <= 0 ||
                bounds.width <= 0f || bounds.height <= 0f)
            {
                return bounds;
            }

            float textureAspect = (float)texture.width / texture.height;
            float boundsAspect = bounds.width / bounds.height;
            if (boundsAspect > textureAspect)
            {
                float width = bounds.height * textureAspect;
                return new Rect(
                    bounds.x + (bounds.width - width) / 2f,
                    bounds.y,
                    width,
                    bounds.height);
            }

            float height = bounds.width / textureAspect;
            return new Rect(
                bounds.x,
                bounds.y + (bounds.height - height) / 2f,
                bounds.width,
                height);
        }

        private string BuildTooltip(string label, string description, string disabledReason)
        {
            string tooltip = label + "\n" + description;
            if (!string.IsNullOrEmpty(disabledReason))
            {
                tooltip += "\n\n" + ShuttleUIText.Tr(
                    "CT_Shuttle_UI_OptionUnavailableFormat",
                    "DisabledCommand".Translate(),
                    disabledReason);
            }

            return tooltip;
        }
    }
}

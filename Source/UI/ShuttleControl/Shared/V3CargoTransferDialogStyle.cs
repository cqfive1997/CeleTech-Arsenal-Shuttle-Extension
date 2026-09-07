using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal static class V3CargoTransferDialogStyle
    {
        internal const float Gap = 10f;
        internal const float HeaderHeight = 68f;
        internal const float FooterHeight = 42f;
        internal const float CountHeight = 74f;
        internal const float CompactCountHeight = 48f;
        internal const float RowHeight = 58f;
        internal const float RowStride = 62f;
        internal const float ButtonHeight = 30f;
        internal const string CountControlName = "CT_Shuttle_CargoTransferV3_Count";

        internal static readonly Color CardColor =
            new Color(0.060f, 0.075f, 0.092f, 0.94f);
        internal static readonly Color SelectedCardColor =
            new Color(0.095f, 0.125f, 0.155f, 0.94f);
        internal static readonly Color DisabledCardColor =
            new Color(0.055f, 0.060f, 0.070f, 0.82f);
        internal static readonly Color AccentColor =
            new Color(0.24f, 0.62f, 0.78f, 0.84f);
        internal static readonly Color ColdColor =
            new Color(0.30f, 0.78f, 0.92f, 1f);
        internal static readonly Color YellowColor =
            new Color(0.95f, 0.72f, 0.28f, 1f);

        internal static void DrawCard(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, CardColor);
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.SubtleBorderColor,
                ShuttleUIStyle.ThinBorder);
        }

        internal static void DrawRowBackground(Rect rect, bool selected, bool disabled)
        {
            Widgets.DrawBoxSolid(
                rect,
                disabled ? DisabledCardColor : selected ? SelectedCardColor : CardColor);
            ShuttleUILayout.DrawRectBorder(
                rect,
                selected ? ColdColor : ShuttleUIStyle.SubtleBorderColor,
                selected ? ShuttleUIStyle.ThickBorder : ShuttleUIStyle.ThinBorder);
            if (selected)
            {
                Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), ColdColor);
            }

            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }
        }

        internal static void DrawThingIcon(Rect rect, Thing thing)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.030f, 0.036f, 0.044f, 0.78f));
            if (ShuttleThingIconDrawer.Draw(rect.ContractedBy(2f), thing))
            {
                ShuttleUILayout.DrawRectBorder(
                    rect,
                    ShuttleUIStyle.SubtleBorderColor,
                    ShuttleUIStyle.ThinBorder);
                return;
            }

            Widgets.DrawBoxSolid(rect.ContractedBy(10f), AccentColor);

            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.SubtleBorderColor,
                ShuttleUIStyle.ThinBorder);
        }

        internal static bool DrawButton(
            Rect rect,
            string label,
            bool enabled,
            Color accent,
            string tooltip)
        {
            bool clicked;
            bool activated = ShuttleV3DialogLayout.DrawDialogButton(
                rect,
                FitLabelText(label, rect.width - 12f),
                enabled,
                GetButtonKind(accent),
                tooltip,
                out clicked);
            if (clicked && !enabled)
            {
                ShuttleUICommandFeedback.ShowReject(
                    string.IsNullOrEmpty(tooltip)
                        ? V3CargoTransferText.Tr("CT_Shuttle_Command_ContextUnavailable")
                        : tooltip,
                    false);
                return false;
            }

            return activated;
        }

        internal static string FitLabelText(string text, float width)
        {
            return ShuttleUILayout.FitSingleLineLabelText(
                text,
                width,
                V3CargoTransferText.Tr("CT_Shuttle_Cargo_Unknown"));
        }

        private static ShuttleV3DialogButtonKind GetButtonKind(Color accent)
        {
            return IsNeutralAccent(accent)
                ? ShuttleV3DialogButtonKind.Normal
                : ShuttleV3DialogButtonKind.Primary;
        }

        private static bool IsNeutralAccent(Color accent)
        {
            Color neutral = ShuttleUIStyle.BorderColor;
            return Mathf.Abs(accent.r - neutral.r) < 0.001f &&
                Mathf.Abs(accent.g - neutral.g) < 0.001f &&
                Mathf.Abs(accent.b - neutral.b) < 0.001f &&
                Mathf.Abs(accent.a - neutral.a) < 0.001f;
        }
    }
}

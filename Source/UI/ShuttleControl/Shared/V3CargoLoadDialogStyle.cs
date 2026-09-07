using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal static class V3CargoLoadDialogStyle
    {
        internal const float Gap = 10f;
        internal const float HeaderHeight = 54f;
        internal const float FooterHeight = 42f;
        internal const float SummaryHeight = 58f;
        internal const float SummaryWidth = 280f;
        internal const float RowHeight = 50f;
        internal const float AvailableRowHeight = 36f;
        internal const float AvailableRowGap = 1f;
        internal const float AvailableHeaderHeight = 24f;
        internal const float SelectedRowHeight = 46f;
        internal const float TabHeight = 28f;
        internal const float ButtonHeight = 30f;
        internal const float ToolbarHeight = 34f;
        internal const float BatchButtonWidth = 142f;
        internal const float ManifestSectionGap = 10f;
        internal const float ManifestQueueRatio = 0.60f;
        internal const float ManifestQueueMinimumHeight = 180f;
        internal const float ManifestPlanMinimumHeight = 150f;
        internal const float ManifestEmptyQueueHeight = 64f;
        internal const float CompactQueueRowHeight = 46f;
        internal const float CompactQueueRowGap = 4f;
        internal const float FooterMeterHeight = 8f;
        internal const string SearchControlName = "CT_Shuttle_CargoLoadV3_Search";

        internal static readonly Color CardColor =
            ShuttleV3DialogStyle.CardColor;
        internal static readonly Color SelectedCardColor =
            ShuttleV3DialogStyle.SelectedColor;
        internal static readonly Color DisabledCardColor =
            ShuttleV3DialogStyle.DisabledColor;
        internal static readonly Color AccentColor =
            ShuttleV3DialogStyle.BlueStatusColor;
        internal static readonly Color GreenColor =
            ShuttleV3DialogStyle.GreenStatusColor;
        internal static readonly Color YellowColor =
            ShuttleV3DialogStyle.YellowStatusColor;
        internal static readonly Color RedColor =
            ShuttleV3DialogStyle.RedStatusColor;

        internal static void DrawSurface(Rect rect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(rect);
        }

        internal static void DrawRowBackground(Rect rect, bool selected, bool disabled)
        {
            ShuttleV3DialogLayout.DrawCardBackground(rect, selected, disabled, CardColor);
        }

        internal static void DrawThingIcon(Rect rect, Thing thing)
        {
            DrawThingIcon(rect, thing, false);
        }

        internal static void DrawThingIcon(Rect rect, Thing thing, bool compactIconMode)
        {
            // The compact path avoids resolving thousands of cargo graphics, but
            // Pawn.def.uiIcon is the missing-texture placeholder rather than a
            // usable portrait. Pawn rows are few and must keep the normal drawer.
            if (compactIconMode && !(thing is Pawn))
            {
                if (TryDrawCompactThingIcon(rect.ContractedBy(2f), thing))
                {
                    ShuttleUILayout.DrawRectBorder(
                        rect,
                        ShuttleUIStyle.SubtleBorderColor,
                        ShuttleUIStyle.ThinBorder);
                    return;
                }

                DrawFallbackThingIcon(rect, thing);
                return;
            }

            if (ShuttleThingIconDrawer.Draw(rect.ContractedBy(2f), thing))
            {
                ShuttleUILayout.DrawRectBorder(
                    rect,
                    ShuttleUIStyle.SubtleBorderColor,
                    ShuttleUIStyle.ThinBorder);
                return;
            }

            DrawFallbackThingIcon(rect, thing);
        }

        private static bool TryDrawCompactThingIcon(Rect rect, Thing thing)
        {
            if (thing == null ||
                thing.def == null ||
                thing.def.uiIcon == null ||
                thing.def.uiIcon == BaseContent.BadTex)
            {
                return false;
            }

            Color oldColor = GUI.color;
            try
            {
                GUI.color = thing.def.uiIconColor;
                Widgets.DrawTextureFitted(rect, thing.def.uiIcon, 1f);
                return true;
            }
            finally
            {
                GUI.color = oldColor;
            }
        }

        private static void DrawFallbackThingIcon(Rect rect, Thing thing)
        {
            Widgets.DrawBoxSolid(rect, ShuttleUIStyle.IconFallbackBackgroundColor);
            ShuttleV3DialogLayout.DrawIconOrFallback(
                rect.ContractedBy(6f),
                null,
                thing is Pawn ? "P" : "I",
                0.9f);
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.SubtleBorderColor,
                ShuttleUIStyle.ThinBorder);
        }

        internal static void DrawSectionHeader(Rect rect, string title)
        {
            ShuttleV3DialogLayout.DrawSectionHeader(rect, title);
        }

        internal static bool DrawButton(
            Rect rect,
            string label,
            bool enabled,
            Color accent,
            string tooltip)
        {
            bool clicked;
            ShuttleV3DialogLayout.DrawAccentButton(
                rect,
                label,
                enabled,
                accent,
                tooltip,
                out clicked);
            if (!clicked)
            {
                return false;
            }

            if (!enabled)
            {
                ShuttleUICommandFeedback.ShowReject(
                    string.IsNullOrEmpty(tooltip)
                        ? V3CargoLoadText.Tr("CT_Shuttle_Command_ContextUnavailable")
                        : tooltip,
                    false);
                return false;
            }

            return true;
        }

        internal static bool DrawTextAction(
            Rect rect,
            string label,
            bool enabled,
            Color color,
            string tooltip)
        {
            bool hovered = enabled && Mouse.IsOver(rect);
            Color textColor = enabled
                ? hovered ? Color.Lerp(color, Color.white, 0.22f) : color
                : ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect,
                string.IsNullOrEmpty(label) ? "-" : label,
                GameFont.Tiny,
                GameFont.Tiny,
                textColor,
                null,
                TextAnchor.MiddleRight);
            if (hovered && !string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }

            return enabled && Widgets.ButtonInvisible(rect);
        }

        internal static void DrawEmptyText(Rect rect, string text)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, 18f),
                FitLabelText(text, rect.width - 20f));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        internal static string FitLabelText(string text, float width)
        {
            return ShuttleUILayout.FitSingleLineLabelText(text, width);
        }

        internal static int GetStepFromCurrentEvent()
        {
            Event evt = Event.current;
            if (evt == null)
            {
                return 1;
            }

            if (evt.control && evt.shift)
            {
                return 1000;
            }

            if (evt.shift)
            {
                return 100;
            }

            return evt.control ? 10 : 1;
        }

        internal static void GetVisibleRowRange(
            Vector2 scroll,
            float viewportHeight,
            int rowCount,
            float rowStride,
            out int firstIndex,
            out int lastIndexExclusive)
        {
            if (rowCount <= 0 || rowStride <= 0f)
            {
                firstIndex = 0;
                lastIndexExclusive = 0;
                return;
            }

            firstIndex = Mathf.Clamp(Mathf.FloorToInt(scroll.y / rowStride), 0, rowCount - 1);
            int visibleRows = Mathf.CeilToInt(viewportHeight / rowStride) + 2;
            lastIndexExclusive = Mathf.Clamp(firstIndex + visibleRows, firstIndex, rowCount);
        }
    }
}

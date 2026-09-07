using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal enum V3MainCardAction
    {
        None,
        Manage,
        Enable,
        Disable,
        Remove
    }

    [StaticConstructorOnStartup]
    internal static class V3MainCardActionRailDrawer
    {
        private const float ExpandButtonSize = 30f;
        private const float CardEdgePadding = 10f;
        private const float ActionRailWidth = 34f;
        private const float ActionButtonSize = 36f;
        private const float ActionGap = 6f;
        private const float MenuPadding = 8f;
        private const float MenuBelowGap = 4f;
        private const float CompactMenuSideGap = 6f;
        private const float IconButtonInset = 5f;
        private const float MenuCornerLength = 10f;
        private const float MenuConnectorWidth = 2f;

        private static readonly Texture2D ExpandIcon =
            Load("UI/ShuttleControl/Actions/Expand_128");
        private static readonly Texture2D SettingIcon =
            Load("UI/ShuttleControl/Actions/Setting_128");
        private static readonly Texture2D PowerOnIcon =
            Load("UI/ShuttleControl/Actions/PowerOn_128");
        private static readonly Texture2D PowerOffIcon =
            Load("UI/ShuttleControl/Actions/PowerOff_128");
        private static readonly Texture2D RemoveIcon =
            Load("UI/ShuttleControl/Actions/Remove_128");

        internal static Rect GetExpandRect(Rect cardRect)
        {
            return GetExpandRect(cardRect, cardRect.y + CardEdgePadding);
        }

        internal static Rect GetExpandRect(Rect cardRect, float top)
        {
            float railX = cardRect.xMax - CardEdgePadding - ActionRailWidth;
            return new Rect(
                railX + ((ActionRailWidth - ExpandButtonSize) * 0.5f),
                top,
                ExpandButtonSize,
                ExpandButtonSize);
        }

        internal static bool IsMenuOpen(V3MainPageState state, string key)
        {
            return state != null &&
                !string.IsNullOrEmpty(key) &&
                state.OpenMainActionMenuKey == key;
        }

        internal static Rect GetSelectRect(Rect cardRect, Rect expandRect, bool menuOpen)
        {
            float selectRight = expandRect.x - 4f;
            if (menuOpen)
            {
                Rect menuRect = GetMenuRect(cardRect, expandRect);
                selectRight = Mathf.Min(selectRight, menuRect.x - 4f);
            }

            return new Rect(
                cardRect.x,
                cardRect.y,
                Mathf.Max(0f, selectRight - cardRect.x),
                cardRect.height);
        }

        internal static V3MainCardAction Draw(
            Rect cardRect,
            V3MainPageState state,
            string key,
            bool currentEnabled,
            bool canManage,
            bool canToggle,
            bool canRemove,
            string manageTooltip,
            string toggleTooltip,
            string removeTooltip,
            Rect expandRect)
        {
            if (state == null || string.IsNullOrEmpty(key))
            {
                return V3MainCardAction.None;
            }

            bool menuOpen = state.OpenMainActionMenuKey == key;
            if (DrawIconOnlyButton(
                expandRect,
                ExpandIcon,
                menuOpen ? "Close" : "Expand",
                true,
                ShuttleUIStyle.BlueStatusColor,
                ShuttleUIStyle.BlueStatusColor,
                ShuttleUIStyle.MutedTextColor,
                menuOpen))
            {
                state.OpenMainActionMenuKey = menuOpen ? null : key;
                return V3MainCardAction.None;
            }

            if (!menuOpen)
            {
                return V3MainCardAction.None;
            }

            Rect menuRect = GetMenuRect(cardRect, expandRect);
            bool drawMenuBelow = ShouldDrawMenuBelow(cardRect);
            Event current = Event.current;
            if (current != null &&
                current.type == EventType.MouseDown &&
                current.button == 0 &&
                !expandRect.Contains(current.mousePosition) &&
                !menuRect.Contains(current.mousePosition))
            {
                state.OpenMainActionMenuKey = null;
                return V3MainCardAction.None;
            }

            DrawMenuFrame(menuRect, expandRect, drawMenuBelow);

            Rect settingRect = new Rect(
                menuRect.x + MenuPadding,
                menuRect.y + MenuPadding,
                ActionButtonSize,
                ActionButtonSize);
            Rect powerRect = new Rect(
                settingRect.xMax + ActionGap,
                settingRect.y,
                ActionButtonSize,
                ActionButtonSize);
            Rect removeRect = new Rect(
                powerRect.xMax + ActionGap,
                settingRect.y,
                ActionButtonSize,
                ActionButtonSize);

            if (DrawIconOnlyButton(
                settingRect,
                SettingIcon,
                manageTooltip,
                canManage,
                ShuttleUIStyle.BlueStatusColor,
                ShuttleUIStyle.BlueStatusColor,
                ShuttleUIStyle.MutedTextColor,
                false))
            {
                state.OpenMainActionMenuKey = null;
                return V3MainCardAction.Manage;
            }

            Color toggleColor = currentEnabled
                ? ShuttleUIStyle.RedStatusColor
                : ShuttleUIStyle.GreenStatusColor;
            if (DrawIconOnlyButton(
                powerRect,
                currentEnabled ? PowerOffIcon : PowerOnIcon,
                toggleTooltip,
                canToggle,
                toggleColor,
                toggleColor,
                ShuttleUIStyle.MutedTextColor,
                false))
            {
                state.OpenMainActionMenuKey = null;
                return currentEnabled ? V3MainCardAction.Disable : V3MainCardAction.Enable;
            }

            if (DrawIconOnlyButton(
                removeRect,
                RemoveIcon,
                removeTooltip,
                canRemove,
                ShuttleUIStyle.RedStatusColor,
                ShuttleUIStyle.RedStatusColor,
                ShuttleUIStyle.MutedTextColor,
                false))
            {
                state.OpenMainActionMenuKey = null;
                return V3MainCardAction.Remove;
            }

            return V3MainCardAction.None;
        }

        internal static void DrawStatusIndicator(
            Rect rect,
            bool installed,
            bool enabled,
            bool activeOperation,
            string tooltip)
        {
            Color tint = !installed
                ? ShuttleUIStyle.MutedTextColor
                : (enabled ? ShuttleUIStyle.GreenStatusColor : ShuttleUIStyle.RedStatusColor);
            if (activeOperation)
            {
                tint = ShuttleUIStyle.YellowStatusColor;
            }

            Widgets.DrawBoxSolid(rect, new Color(0.025f, 0.050f, 0.065f, installed ? 0.12f : 0.06f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.WithAlpha(tint, installed || activeOperation ? 0.34f : 0.18f),
                1f);
            V3MainFrameDrawer.DrawAllCornerLines(
                rect,
                ShuttleUIStyle.WithAlpha(tint, installed || activeOperation ? 0.22f : 0.12f));

            Rect iconRect = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, rect.height - 10f);
            Color oldColor = GUI.color;
            GUI.color = ShuttleUIStyle.WithAlpha(tint, installed || activeOperation ? 0.82f : 0.46f);
            if (!installed)
            {
                DrawEmptyStatusGlyph(iconRect, GUI.color);
            }
            else
            {
                Widgets.DrawTextureFitted(iconRect, enabled ? PowerOnIcon : PowerOffIcon, 1f);
            }

            GUI.color = oldColor;
            ShuttleUITooltip.Tip(rect, tooltip);
        }

        internal static void DrawStatusGlyph(
            Rect rect,
            bool installed,
            bool enabled,
            bool activeOperation,
            string tooltip,
            bool highlighted)
        {
            Color tint = !installed
                ? ShuttleUIStyle.MutedTextColor
                : (enabled ? ShuttleUIStyle.GreenStatusColor : ShuttleUIStyle.RedStatusColor);
            if (activeOperation)
            {
                tint = ShuttleUIStyle.YellowStatusColor;
            }

            Color oldColor = GUI.color;
            GUI.color = ShuttleUIStyle.WithAlpha(tint, highlighted ? 0.98f : 0.74f);
            if (!installed)
            {
                DrawEmptyStatusGlyph(rect, GUI.color);
            }
            else
            {
                Rect iconRect = new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f);
                Widgets.DrawTextureFitted(iconRect, enabled ? PowerOnIcon : PowerOffIcon, 1f);
            }

            GUI.color = oldColor;
            ShuttleUITooltip.Tip(rect, tooltip);
        }

        internal static bool DrawManageButton(
            Rect rect,
            bool enabled,
            string tooltip,
            bool active)
        {
            return DrawIconOnlyButton(
                rect,
                SettingIcon,
                tooltip,
                enabled,
                ShuttleUIStyle.BlueStatusColor,
                ShuttleUIStyle.BlueStatusColor,
                ShuttleUIStyle.MutedTextColor,
                active);
        }

        internal static bool DrawToggleButton(
            Rect rect,
            bool currentEnabled,
            bool enabled,
            string tooltip)
        {
            Color tint = currentEnabled
                ? ShuttleUIStyle.RedStatusColor
                : ShuttleUIStyle.GreenStatusColor;
            return DrawIconOnlyButton(
                rect,
                currentEnabled ? PowerOffIcon : PowerOnIcon,
                tooltip,
                enabled,
                tint,
                tint,
                ShuttleUIStyle.MutedTextColor,
                false);
        }

        internal static bool DrawRemoveButton(
            Rect rect,
            bool enabled,
            string tooltip)
        {
            return DrawIconOnlyButton(
                rect,
                RemoveIcon,
                tooltip,
                enabled,
                ShuttleUIStyle.RedStatusColor,
                ShuttleUIStyle.RedStatusColor,
                ShuttleUIStyle.MutedTextColor,
                false);
        }

        private static Rect GetMenuRect(Rect cardRect, Rect expandRect)
        {
            float menuWidth = (ActionButtonSize * 3f) + (ActionGap * 2f) + (MenuPadding * 2f);
            float menuHeight = ActionButtonSize + (MenuPadding * 2f);
            bool drawMenuBelow = ShouldDrawMenuBelow(cardRect);
            float menuX = drawMenuBelow
                ? expandRect.xMax - menuWidth
                : expandRect.x - menuWidth - CompactMenuSideGap;
            float minMenuX = cardRect.x + CardEdgePadding;
            float maxMenuX = cardRect.xMax - menuWidth - CardEdgePadding;
            if (maxMenuX < minMenuX)
            {
                maxMenuX = minMenuX;
            }

            menuX = Mathf.Clamp(menuX, minMenuX, maxMenuX);
            return drawMenuBelow
                ? new Rect(menuX, expandRect.yMax + MenuBelowGap, menuWidth, menuHeight)
                : new Rect(menuX, expandRect.y - 2f, menuWidth, menuHeight);
        }

        private static bool ShouldDrawMenuBelow(Rect cardRect)
        {
            return cardRect.height >= 86f;
        }

        private static void DrawMenuFrame(Rect menuRect, Rect expandRect, bool connectorAbove)
        {
            Color border = ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.82f);
            Widgets.DrawBoxSolid(menuRect, new Color(0.018f, 0.040f, 0.055f, 0.96f));
            Widgets.DrawBoxSolid(
                new Rect(menuRect.x + 2f, menuRect.y + 2f, Mathf.Max(0f, menuRect.width - 4f), 1f),
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.20f));
            ShuttleUILayout.DrawRectBorder(menuRect, border, 1f);

            DrawMenuCorners(menuRect, border);
            float connectorX = Mathf.Clamp(expandRect.center.x, menuRect.x + 12f, menuRect.xMax - 12f);
            if (connectorAbove)
            {
                Widgets.DrawBoxSolid(
                    new Rect(
                        connectorX - (MenuConnectorWidth * 0.5f),
                        expandRect.yMax - 1f,
                        MenuConnectorWidth,
                        Mathf.Max(1f, menuRect.y - expandRect.yMax + 2f)),
                    border);
                Widgets.DrawBoxSolid(
                    new Rect(
                        connectorX - (MenuCornerLength * 0.5f),
                        menuRect.y,
                        MenuCornerLength,
                        2f),
                    border);
                return;
            }

            Widgets.DrawBoxSolid(
                new Rect(menuRect.xMax - 1f, expandRect.center.y - 1f, Mathf.Max(1f, expandRect.x - menuRect.xMax + 2f), 2f),
                border);
            Widgets.DrawBoxSolid(new Rect(menuRect.xMax - 2f, expandRect.center.y - 5f, 2f, 10f), border);
        }

        private static void DrawMenuCorners(Rect rect, Color color)
        {
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, MenuCornerLength, 2f), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 2f, MenuCornerLength), color);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - MenuCornerLength, rect.y, MenuCornerLength, 2f), color);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - 2f, rect.y, 2f, MenuCornerLength), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 2f, MenuCornerLength, 2f), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - MenuCornerLength, 2f, MenuCornerLength), color);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - MenuCornerLength, rect.yMax - 2f, MenuCornerLength, 2f), color);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - 2f, rect.yMax - MenuCornerLength, 2f, MenuCornerLength), color);
        }

        private static bool DrawIconOnlyButton(
            Rect rect,
            Texture2D icon,
            string tooltip,
            bool enabled,
            Color normalTint,
            Color hoverTint,
            Color disabledTint,
            bool active)
        {
            bool hovered = Mouse.IsOver(rect);
            Color tint = enabled ? (hovered ? hoverTint : normalTint) : disabledTint;
            float backgroundAlpha = active ? 0.30f : (hovered && enabled ? 0.20f : 0.08f);
            if (!enabled)
            {
                backgroundAlpha = 0.05f;
            }

            Widgets.DrawBoxSolid(rect, new Color(0.025f, 0.050f, 0.065f, backgroundAlpha));
            if (active)
            {
                Widgets.DrawBoxSolid(
                    new Rect(rect.x + 2f, rect.y + 2f, Mathf.Max(0f, rect.width - 4f), Mathf.Max(0f, rect.height - 4f)),
                    ShuttleUIStyle.WithAlpha(tint, 0.06f));
            }

            float borderAlpha = active
                ? 0.92f
                : (hovered && enabled ? 0.70f : (enabled ? 0.28f : 0.14f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.WithAlpha(tint, borderAlpha),
                active || hovered && enabled ? 2f : 1f);
            V3MainFrameDrawer.DrawAllCornerLines(
                rect,
                ShuttleUIStyle.WithAlpha(tint, active ? 0.86f : (hovered && enabled ? 0.58f : 0.20f)));

            if ((hovered && enabled) || active)
            {
                Widgets.DrawBoxSolid(
                    new Rect(rect.x + 2f, rect.y + 2f, Mathf.Max(0f, rect.width - 4f), 1f),
                    ShuttleUIStyle.WithAlpha(tint, active ? 0.42f : 0.26f));
            }

            Rect iconRect = new Rect(
                rect.x + IconButtonInset,
                rect.y + IconButtonInset,
                Mathf.Max(0f, rect.width - (IconButtonInset * 2f)),
                Mathf.Max(0f, rect.height - (IconButtonInset * 2f)));
            Color oldColor = GUI.color;
            GUI.color = enabled
                ? ShuttleUIStyle.WithAlpha(tint, hovered ? 0.96f : 0.72f)
                : ShuttleUIStyle.WithAlpha(tint, 0.30f);
            Widgets.DrawTextureFitted(iconRect, icon, 1f);
            GUI.color = oldColor;

            ShuttleUITooltip.Tip(rect, tooltip);
            return enabled && Widgets.ButtonInvisible(rect);
        }

        private static void DrawEmptyStatusGlyph(Rect rect, Color color)
        {
            ShuttleUILayout.DrawRectBorder(rect, color, 1f);
            float dotSize = Mathf.Min(5f, Mathf.Min(rect.width, rect.height));
            Widgets.DrawBoxSolid(
                new Rect(
                    rect.center.x - (dotSize * 0.5f),
                    rect.center.y - (dotSize * 0.5f),
                    dotSize,
                    dotSize),
                color);
        }

        private static Texture2D Load(string path)
        {
            Texture2D texture = ContentFinder<Texture2D>.Get(path, false);
            return texture ?? BaseContent.BadTex;
        }
    }
}

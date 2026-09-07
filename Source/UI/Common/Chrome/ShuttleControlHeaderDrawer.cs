using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome
{
    internal static class ShuttleControlHeaderDrawer
    {
        private const string DefaultLogoFallbackText = "CT";
        private static readonly Dictionary<Texture2D, TextureAlphaBounds> TextureAlphaBoundsCache =
            new Dictionary<Texture2D, TextureAlphaBounds>();

        internal static ShuttleControlHeaderTopRowLayout Draw(
            Rect headerRect,
            string title,
            Texture2D logo,
            IList<ShuttleHeaderStatusSpec> statusCards,
            IList<ShuttleHeaderCommandSpec> commands,
            IList<ShuttleHeaderMetricSpec> summaryMetrics)
        {
            return Draw(
                headerRect,
                title,
                logo,
                statusCards,
                commands,
                new ShuttleHeaderRibbonSpec(
                    ShuttleHeaderProgressSpec.Empty,
                    summaryMetrics));
        }

        internal static ShuttleControlHeaderTopRowLayout Draw(
            Rect headerRect,
            string title,
            Texture2D logo,
            IList<ShuttleHeaderStatusSpec> statusCards,
            IList<ShuttleHeaderCommandSpec> commands,
            ShuttleHeaderRibbonSpec ribbon)
        {
            return Draw(
                headerRect,
                title,
                logo,
                DefaultLogoFallbackText,
                statusCards,
                commands,
                ribbon);
        }

        internal static ShuttleControlHeaderTopRowLayout Draw(
            Rect headerRect,
            string title,
            Texture2D logo,
            string logoFallbackText,
            IList<ShuttleHeaderStatusSpec> statusCards,
            IList<ShuttleHeaderCommandSpec> commands,
            IList<ShuttleHeaderMetricSpec> summaryMetrics)
        {
            return Draw(
                headerRect,
                title,
                logo,
                logoFallbackText,
                statusCards,
                commands,
                new ShuttleHeaderRibbonSpec(
                    ShuttleHeaderProgressSpec.Empty,
                    summaryMetrics));
        }

        internal static ShuttleControlHeaderTopRowLayout Draw(
            Rect headerRect,
            string title,
            Texture2D logo,
            string logoFallbackText,
            IList<ShuttleHeaderStatusSpec> statusCards,
            IList<ShuttleHeaderCommandSpec> commands,
            ShuttleHeaderRibbonSpec ribbon)
        {
            ShuttleControlHeaderTopRowLayout layout =
                ShuttleControlHeaderLayout.CalculateTopRowLayout(
                    headerRect,
                    GetStatusCount(statusCards),
                    GetCommandCount(commands));
            if (headerRect.width <= 0f || headerRect.height <= 0f)
            {
                return layout;
            }

            ShuttleUILayout.DrawHeaderBackground(headerRect);
            DrawBrand(layout, logo, logoFallbackText);
            DrawStatusCards(layout, statusCards);
            DrawCommands(layout, commands);
            ShuttleSummaryRibbonDrawer.Draw(
                ShuttleControlHeaderLayout.GetSummaryRibbonRect(headerRect),
                ribbon);
            return layout;
        }

        private static void DrawBrand(
            ShuttleControlHeaderTopRowLayout layout,
            Texture2D logo,
            string fallbackText)
        {
            if (DrawConsoleLogo(layout, logo))
            {
                return;
            }

            DrawLogo(layout.LogoRect, logo, fallbackText);
        }

        private static void DrawLogo(Rect rect, Texture2D logo, string fallbackText)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            if (logo != null)
            {
                Color oldColor = GUI.color;
                GUI.color = Color.white;
                Widgets.DrawTextureFitted(rect, logo, 0.95f);
                GUI.color = oldColor;
                return;
            }

            Widgets.DrawBoxSolid(rect, ShuttleUIStyle.IconFallbackBackgroundColor);
            ShuttleUILayout.DrawRectBorder(rect, ShuttleUIStyle.SubtleBorderColor, ShuttleUIStyle.ThinBorder);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect.ContractedBy(5f),
                string.IsNullOrEmpty(fallbackText) ? DefaultLogoFallbackText : fallbackText,
                GameFont.Small,
                GameFont.Tiny,
                ShuttleUIStyle.HeaderTitleTextColor,
                null,
                TextAnchor.MiddleCenter);
        }

        private static bool DrawConsoleLogo(
            ShuttleControlHeaderTopRowLayout layout,
            Texture2D texture)
        {
            if (texture == null)
            {
                return false;
            }

            TextureAlphaBounds bounds = GetTextureAlphaBounds(texture);
            if (bounds.ContentWidth <= 0 || bounds.ContentHeight <= 0)
            {
                return false;
            }

            Rect brandRect = new Rect(
                layout.RowRect.x,
                layout.RowRect.y,
                Mathf.Max(0f, layout.StatusCardsRect.x - layout.RowRect.x),
                layout.RowRect.height);
            if (brandRect.width <= 1f || brandRect.height <= 1f)
            {
                return false;
            }

            float aspect = bounds.ContentWidth / (float)bounds.ContentHeight;
            float availableWidth = Mathf.Max(0f, brandRect.width - 10f);
            float contentHeight = brandRect.height * 1.16f;
            if (aspect > 0f)
            {
                contentHeight = Mathf.Min(contentHeight, availableWidth / aspect);
            }

            if (contentHeight <= 1f)
            {
                return false;
            }

            float contentWidth = contentHeight * aspect;
            Rect drawRect = new Rect(
                brandRect.x,
                brandRect.center.y - (contentHeight * 0.5f),
                contentWidth,
                contentHeight);

            Color oldColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(drawRect, texture, bounds.UvRect, true);
            GUI.color = oldColor;
            return true;
        }

        private static TextureAlphaBounds GetTextureAlphaBounds(Texture2D texture)
        {
            TextureAlphaBounds cached;
            if (TextureAlphaBoundsCache.TryGetValue(texture, out cached))
            {
                return cached;
            }

            TextureAlphaBounds bounds = ScanTextureAlphaBounds(texture);
            TextureAlphaBoundsCache[texture] = bounds;
            return bounds;
        }

        private static TextureAlphaBounds ScanTextureAlphaBounds(Texture2D texture)
        {
            if (texture == null || texture.width <= 0 || texture.height <= 0)
            {
                return TextureAlphaBounds.Empty;
            }

            try
            {
                Color32[] pixels = texture.GetPixels32();
                int minX = texture.width;
                int minY = texture.height;
                int maxX = -1;
                int maxY = -1;
                for (int y = 0; y < texture.height; y++)
                {
                    int rowOffset = y * texture.width;
                    for (int x = 0; x < texture.width; x++)
                    {
                        if (pixels[rowOffset + x].a <= 0)
                        {
                            continue;
                        }

                        if (x < minX)
                        {
                            minX = x;
                        }

                        if (y < minY)
                        {
                            minY = y;
                        }

                        if (x > maxX)
                        {
                            maxX = x;
                        }

                        if (y > maxY)
                        {
                            maxY = y;
                        }
                    }
                }

                if (maxX < minX || maxY < minY)
                {
                    return TextureAlphaBounds.Empty;
                }

                int contentWidth = maxX - minX + 1;
                int contentHeight = maxY - minY + 1;
                Rect uvRect = new Rect(
                    minX / (float)texture.width,
                    minY / (float)texture.height,
                    contentWidth / (float)texture.width,
                    contentHeight / (float)texture.height);
                return new TextureAlphaBounds(contentWidth, contentHeight, uvRect);
            }
            catch
            {
                return new TextureAlphaBounds(
                    texture.width,
                    texture.height,
                    new Rect(0f, 0f, 1f, 1f));
            }
        }

        private static void DrawStatusCards(
            ShuttleControlHeaderTopRowLayout layout,
            IList<ShuttleHeaderStatusSpec> statusCards)
        {
            int count = Mathf.Min(GetStatusCount(statusCards), layout.StatusCardCount);
            for (int i = 0; i < count; i++)
            {
                DrawStatusCard(layout.GetStatusCardRect(i), statusCards[i]);
            }
        }

        private static void DrawStatusCard(Rect rect, ShuttleHeaderStatusSpec spec)
        {
            ShuttleHeaderStatusCardDrawer.Draw(rect, spec);
        }

        private static void DrawCommands(
            ShuttleControlHeaderTopRowLayout layout,
            IList<ShuttleHeaderCommandSpec> commands)
        {
            int count = Mathf.Min(GetCommandCount(commands), layout.CommandCount);
            for (int i = 0; i < count; i++)
            {
                DrawCommand(layout.GetCommandRect(i), commands[i]);
            }
        }

        private static void DrawCommand(Rect rect, ShuttleHeaderCommandSpec command)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            bool enabled = command.Enabled && command.OnClick != null;
            ShuttleUIHeaderCommandButtonSpec spec =
                new ShuttleUIHeaderCommandButtonSpec(
                    rect,
                    command.Icon,
                    enabled ? command.OnClick : null,
                    command.Tooltip,
                    command.Tooltip);
            spec.FallbackText = GetFallbackText(command.Label);
            spec.Enabled = enabled;
            ShuttleUIHeaderCommandButtonDrawer.Draw(spec);
        }

        private static int GetStatusCount(IList<ShuttleHeaderStatusSpec> statusCards)
        {
            return statusCards != null ? statusCards.Count : 0;
        }

        private static int GetCommandCount(IList<ShuttleHeaderCommandSpec> commands)
        {
            return commands != null ? commands.Count : 0;
        }

        private static string GetFallbackText(string label)
        {
            return string.IsNullOrEmpty(label) ? "?" : label.Substring(0, 1);
        }

        private struct TextureAlphaBounds
        {
            internal static readonly TextureAlphaBounds Empty =
                new TextureAlphaBounds(0, 0, new Rect(0f, 0f, 0f, 0f));

            internal readonly int ContentWidth;
            internal readonly int ContentHeight;
            internal readonly Rect UvRect;

            internal TextureAlphaBounds(int contentWidth, int contentHeight, Rect uvRect)
            {
                this.ContentWidth = contentWidth;
                this.ContentHeight = contentHeight;
                this.UvRect = uvRect;
            }
        }
    }
}

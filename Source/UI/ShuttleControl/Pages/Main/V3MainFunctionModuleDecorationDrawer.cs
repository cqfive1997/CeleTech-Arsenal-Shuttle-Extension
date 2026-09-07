using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    [StaticConstructorOnStartup]
    internal static class V3MainFunctionModuleDecorationAssets
    {
        internal static readonly Texture2D BGPlanet =
            ContentFinder<Texture2D>.Get("UI/Shuttle/Console/BGPlanet", false);

        internal static readonly Texture2D Dragon =
            ContentFinder<Texture2D>.Get("Icon/CeleTech/Dragon", false);

        internal static readonly Texture2D CeleTechRow =
            ContentFinder<Texture2D>.Get("Icon/CeleTech/CeleTech_row", false);
    }

    internal static class V3MainFunctionModuleDecorationDrawer
    {
        internal static void Draw(Rect rect, V3MainText text)
        {
            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            Matrix4x4 oldMatrix = GUI.matrix;

            GUI.BeginGroup(rect);
            try
            {
                Rect local = new Rect(0f, 0f, rect.width, rect.height);
                DrawLocal(local);
            }
            finally
            {
                GUI.EndGroup();
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
            }
        }

        private static void DrawLocal(Rect local)
        {
            DrawBackground(local);
            DrawAssistantPortrait(local);
            DrawRightVeil(local);
            DrawBranding(local);
            DrawAiGrid(local);
            DrawScanLines(local);
            DrawAiNodes(local);
            DrawGlitchBands(local);
            DrawCornerAccents(local);
        }

        private static void DrawBackground(Rect local)
        {
            Widgets.DrawBoxSolid(local, new Color(0.012f, 0.018f, 0.024f, 0.98f));
        }

        private static void DrawAssistantPortrait(Rect local)
        {
            Texture2D portrait = V3MainFunctionModuleDecorationAssets.BGPlanet;
            if (portrait == null)
            {
                DrawMissingPortraitFallback(local);
                return;
            }

            float textureAspect = portrait.width > 0 && portrait.height > 0
                ? portrait.width / (float)portrait.height
                : 1f;
            float drawHeight = local.height * 1.82f;
            float drawWidth = drawHeight * textureAspect;
            if (drawWidth < local.width)
            {
                drawWidth = local.width;
                drawHeight = drawWidth / Mathf.Max(0.001f, textureAspect);
            }

            Rect drawRect = new Rect(
                local.x - (local.width * 0.18f),
                local.y - (local.height * 0.18f),
                drawWidth,
                drawHeight);

            Color oldColor = GUI.color;
            GUI.color = new Color(0.24f, 0.95f, 1f, 0.09f);
            GUI.DrawTexture(new Rect(drawRect.x + 2f, drawRect.y, drawRect.width, drawRect.height), portrait, ScaleMode.ScaleToFit, true);
            GUI.color = new Color(0.50f, 0.72f, 1f, 0.06f);
            GUI.DrawTexture(new Rect(drawRect.x - 2f, drawRect.y + 1f, drawRect.width, drawRect.height), portrait, ScaleMode.ScaleToFit, true);
            GUI.color = new Color(1f, 1f, 1f, 0.92f);
            GUI.DrawTexture(drawRect, portrait, ScaleMode.ScaleToFit, true);
            GUI.color = ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.10f);
            Widgets.DrawBoxSolid(
                new Rect(local.x + (local.width * 0.10f), local.y + (local.height * 0.24f), local.width * 0.34f, 1f),
                GUI.color);
            GUI.color = oldColor;
        }

        private static void DrawRightVeil(Rect local)
        {
            Color oldColor = GUI.color;

            Widgets.DrawBoxSolid(local, new Color(0.00f, 0.07f, 0.08f, 0.08f));

            for (int i = 0; i < 7; i++)
            {
                float x = local.x + (local.width * (0.38f + (0.078f * i)));
                float alpha = 0.040f + (0.032f * i);
                Widgets.DrawBoxSolid(
                    new Rect(x, local.y, local.xMax - x, local.height),
                    new Color(0f, 0f, 0f, alpha));
            }

            GUI.color = oldColor;
        }

        private static void DrawBranding(Rect local)
        {
            Texture2D dragon = V3MainFunctionModuleDecorationAssets.Dragon;
            Texture2D row = V3MainFunctionModuleDecorationAssets.CeleTechRow;
            Color oldColor = GUI.color;

            float dragonSize = Mathf.Min(local.height * 0.42f, local.width * 0.22f);
            Rect dragonRect = new Rect(
                local.xMax - dragonSize - (local.width * 0.18f),
                local.y + (local.height * 0.20f),
                dragonSize,
                dragonSize);

            if (dragon != null)
            {
                GUI.color = ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.06f);
                Widgets.DrawTextureFitted(dragonRect.ExpandedBy(8f), dragon, 1f);
                GUI.color = ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.23f);
                Widgets.DrawTextureFitted(dragonRect, dragon, 1f);
            }

            if (row != null)
            {
                float rowWidth = Mathf.Min(local.width * 0.34f, 148f);
                float rowHeight = row.height > 0
                    ? rowWidth * (row.height / (float)Mathf.Max(1, row.width))
                    : 18f;
                Rect rowRect = new Rect(
                    local.xMax - rowWidth - (local.width * 0.145f),
                    dragonRect.yMax + 9f,
                    rowWidth,
                    Mathf.Max(14f, rowHeight));

                GUI.color = ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.30f);
                GUI.DrawTexture(rowRect, row, ScaleMode.ScaleToFit, true);
                Widgets.DrawBoxSolid(
                    new Rect(rowRect.x, rowRect.yMax + 5f, rowRect.width * 0.72f, 1f),
                    ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.18f));
            }

            GUI.color = oldColor;
        }

        private static void DrawAiGrid(Rect local)
        {
            Color oldColor = GUI.color;
            Color gridColor = ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.035f);
            float startX = local.x + (local.width * 0.42f);
            float x = startX;
            while (x < local.xMax - 4f)
            {
                Widgets.DrawBoxSolid(new Rect(x, local.y + 4f, 1f, local.height - 8f), gridColor);
                x += 27f;
            }

            float y = local.y + 10f;
            while (y < local.yMax - 6f)
            {
                Widgets.DrawBoxSolid(new Rect(startX, y, local.xMax - startX - 6f, 1f), gridColor);
                y += 22f;
            }

            GUI.color = oldColor;
        }

        private static void DrawScanLines(Rect local)
        {
            Color oldColor = GUI.color;
            Color lineColor = ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.045f);
            float y = local.y + 6f;
            while (y < local.yMax)
            {
                Widgets.DrawBoxSolid(new Rect(local.x + 6f, y, local.width - 12f, 1f), lineColor);
                y += 12f;
            }

            Widgets.DrawBoxSolid(
                new Rect(local.x + (local.width * 0.54f), local.y + 12f, local.width * 0.30f, 1f),
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.26f));
            Widgets.DrawBoxSolid(
                new Rect(local.x + (local.width * 0.62f), local.y + 20f, local.width * 0.18f, 1f),
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.14f));
            Widgets.DrawBoxSolid(
                new Rect(local.x + (local.width * 0.58f), local.yMax - 22f, local.width * 0.24f, 1f),
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.14f));

            GUI.color = oldColor;
        }

        private static void DrawAiNodes(Rect local)
        {
            Color oldColor = GUI.color;
            Color node = ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.24f);
            Color line = ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.12f);

            float x1 = local.x + (local.width * 0.60f);
            float x2 = local.x + (local.width * 0.72f);
            float x3 = local.x + (local.width * 0.84f);
            float y1 = local.y + (local.height * 0.18f);
            float y2 = local.y + (local.height * 0.35f);
            float y3 = local.y + (local.height * 0.58f);
            float y4 = local.y + (local.height * 0.76f);

            DrawNodeLine(x1, y2, x2, y1, line);
            DrawNodeLine(x1, y2, x2, y3, line);
            DrawNodeLine(x2, y1, x3, y2, line);
            DrawNodeLine(x2, y3, x3, y4, line);
            DrawNode(x1, y2, node);
            DrawNode(x2, y1, node);
            DrawNode(x2, y3, node);
            DrawNode(x3, y2, node);
            DrawNode(x3, y4, node);

            GUI.color = oldColor;
        }

        private static void DrawNode(float x, float y, Color color)
        {
            Widgets.DrawBoxSolid(new Rect(x - 2f, y - 2f, 4f, 4f), color);
            Widgets.DrawBoxSolid(new Rect(x - 4f, y, 8f, 1f), ShuttleUIStyle.WithAlpha(color, 0.55f));
            Widgets.DrawBoxSolid(new Rect(x, y - 4f, 1f, 8f), ShuttleUIStyle.WithAlpha(color, 0.55f));
        }

        private static void DrawNodeLine(float x1, float y1, float x2, float y2, Color color)
        {
            Widgets.DrawBoxSolid(new Rect(Mathf.Min(x1, x2), y1, Mathf.Abs(x2 - x1), 1f), color);
            Widgets.DrawBoxSolid(new Rect(x2, Mathf.Min(y1, y2), 1f, Mathf.Abs(y2 - y1)), color);
        }

        private static void DrawGlitchBands(Rect local)
        {
            Color oldColor = GUI.color;
            Color bright = ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.20f);
            Color soft = ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.08f);

            Widgets.DrawBoxSolid(
                new Rect(local.x + (local.width * 0.08f), local.y + (local.height * 0.30f), local.width * 0.24f, 2f),
                bright);
            Widgets.DrawBoxSolid(
                new Rect(local.x + (local.width * 0.13f), local.y + (local.height * 0.34f), local.width * 0.15f, 1f),
                soft);
            Widgets.DrawBoxSolid(
                new Rect(local.x + (local.width * 0.52f), local.y + (local.height * 0.50f), local.width * 0.20f, 2f),
                soft);
            Widgets.DrawBoxSolid(
                new Rect(local.x + (local.width * 0.70f), local.y + (local.height * 0.84f), local.width * 0.18f, 1f),
                bright);

            GUI.color = oldColor;
        }

        private static void DrawCornerAccents(Rect local)
        {
            Color oldColor = GUI.color;
            Color strong = ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.42f);
            Color soft = ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.18f);
            float inset = 7f;
            float longEdge = Mathf.Min(34f, local.width * 0.12f);
            float shortEdge = 12f;

            Widgets.DrawBoxSolid(new Rect(local.x + inset, local.y + inset, longEdge, 1f), strong);
            Widgets.DrawBoxSolid(new Rect(local.x + inset, local.y + inset, 1f, shortEdge), strong);
            Widgets.DrawBoxSolid(new Rect(local.x + inset + longEdge + 5f, local.y + inset, 12f, 1f), soft);

            Widgets.DrawBoxSolid(new Rect(local.xMax - inset - longEdge, local.y + inset, longEdge, 1f), soft);
            Widgets.DrawBoxSolid(new Rect(local.xMax - inset - 1f, local.y + inset, 1f, shortEdge), soft);

            Widgets.DrawBoxSolid(new Rect(local.x + inset, local.yMax - inset - 1f, longEdge, 1f), soft);
            Widgets.DrawBoxSolid(new Rect(local.x + inset, local.yMax - inset - shortEdge, 1f, shortEdge), soft);

            Widgets.DrawBoxSolid(new Rect(local.xMax - inset - longEdge, local.yMax - inset - 1f, longEdge, 1f), strong);
            Widgets.DrawBoxSolid(new Rect(local.xMax - inset - 1f, local.yMax - inset - shortEdge, 1f, shortEdge), strong);

            GUI.color = oldColor;
        }

        private static void DrawMissingPortraitFallback(Rect local)
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(0.35f, 0.78f, 0.90f, 0.20f);
            Widgets.DrawBoxSolid(
                new Rect(local.x + 14f, local.center.y - 1f, local.width - 28f, 2f),
                GUI.color);
            Widgets.DrawBoxSolid(
                new Rect(local.x + 14f, local.center.y + 8f, (local.width - 28f) * 0.62f, 1f),
                ShuttleUIStyle.WithAlpha(GUI.color, 0.72f));
            GUI.color = oldColor;
        }
    }
}

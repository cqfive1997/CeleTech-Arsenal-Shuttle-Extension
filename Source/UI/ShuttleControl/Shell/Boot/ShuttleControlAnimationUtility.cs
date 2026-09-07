using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Boot
{
    internal static class ShuttleControlAnimationUtility
    {
        internal const float ShortTotalDuration = 1.15f;
        internal const float FirstBootTotalDuration = 13.00f;
        internal const float SystemUpdateTotalDuration = ShuttleControlSystemUpdateUtility.TotalDuration;
        internal const float TotalDuration = ShortTotalDuration;
        internal const float PhaseLineStart = 0.00f;
        internal const float PhaseLineDuration = 0.22f;
        internal const float PhasePanelStart = 0.14f;
        internal const float PhasePanelDuration = 0.34f;
        internal const float PhaseLogoStart = 0.24f;
        internal const float PhaseLogoDuration = 0.22f;
        internal const float PhaseUIStart = 0.76f;
        internal const float PhaseUIDuration = 0.34f;
        internal const float UIStartOffsetY = 12f;

        internal const float FirstBootIntroDuration = 0.60f;
        internal const float FirstBootSelfCheckStart = 0.60f;
        internal const float FirstBootSelfCheckDuration = 12.00f;
        internal const float FirstBootOutroStart = 12.60f;
        internal const float FirstBootOutroDuration = 0.40f;
        internal const float SystemUpdateIntroDuration = 0.50f;
        internal const float SystemUpdateOutroStart = 7.60f;
        internal const float SystemUpdateOutroDuration = 0.40f;

        private static readonly Color SubtleBorderColor =
            new Color(0.22f, 0.29f, 0.36f, 0.62f);
        private static readonly Color BlueStatusColor =
            new Color(0.28f, 0.64f, 0.86f, 1f);

        private static readonly string[] FirstBootStepKeys =
        {
            "CT_ShuttleControlV3_FirstBoot_Step_StructuralIntegrity",
            "CT_ShuttleControlV3_FirstBoot_Step_BusSystem",
            "CT_ShuttleControlV3_FirstBoot_Step_Segments",
            "CT_ShuttleControlV3_FirstBoot_Step_Propulsion",
            "CT_ShuttleControlV3_FirstBoot_Step_Defense",
            "CT_ShuttleControlV3_FirstBoot_Step_LogSystem"
        };

        private const string FirstBootTitleKey = "CT_ShuttleControlV3_FirstBoot_Title";
        private const string BootHintKey = "CT_ShuttleControlV3_BootHint_DisableExtraEffects";
        private const float BootHintPulseSpeed = 2.8f;
        private const float BootHintPulseMinAlpha = 0.72f;
        private const float BootHintPulseMaxAlpha = 1.00f;
        private const float BootHintStaticAlpha = 0.92f;
        private const float BackdropAlpha = 0.92f;
        private const float BackdropInset = 2f;

        internal static float GetTotalDuration(ShuttleControlBootAnimationMode mode)
        {
            if (mode == ShuttleControlBootAnimationMode.FirstBootLong)
            {
                return FirstBootTotalDuration;
            }

            return mode == ShuttleControlBootAnimationMode.SystemUpdate
                ? SystemUpdateTotalDuration
                : ShortTotalDuration;
        }

        internal static float EaseOutCubic(float t)
        {
            float clamped = Mathf.Clamp01(t);
            float inverse = 1f - clamped;
            return 1f - inverse * inverse * inverse;
        }

        internal static float EaseInOutCubic(float t)
        {
            float clamped = Mathf.Clamp01(t);
            if (clamped < 0.5f)
            {
                return 4f * clamped * clamped * clamped;
            }

            float shifted = 2f * clamped - 2f;
            return 0.5f * shifted * shifted * shifted + 1f;
        }

        internal static float Normalized(float elapsed, float start, float duration)
        {
            if (duration <= 0f)
            {
                return elapsed >= start ? 1f : 0f;
            }

            return Mathf.Clamp01((elapsed - start) / duration);
        }

        internal static float GetUIAlpha(float elapsed)
        {
            return EaseOutCubic(Normalized(elapsed, PhaseUIStart, PhaseUIDuration));
        }

        internal static float GetFirstBootUIAlpha(float elapsed)
        {
            return EaseOutCubic(Normalized(elapsed, FirstBootOutroStart, FirstBootOutroDuration));
        }

        internal static float GetSystemUpdateUIAlpha(float elapsed)
        {
            return EaseOutCubic(Normalized(elapsed, SystemUpdateOutroStart, SystemUpdateOutroDuration));
        }

        internal static float GetFirstBootSelfCheckProgress(float elapsed)
        {
            return Normalized(elapsed, FirstBootSelfCheckStart, FirstBootSelfCheckDuration);
        }

        internal static float GetSystemUpdateProgress(float elapsed)
        {
            return Mathf.Clamp01(Mathf.Max(0f, elapsed) / SystemUpdateTotalDuration);
        }

        internal static void DrawOpeningBackdrop(Rect targetRect, float elapsed)
        {
            Rect target = Inset(targetRect, BackdropInset);
            if (target.width <= 0f || target.height <= 0f)
            {
                return;
            }

            float lineT = EaseOutCubic(Normalized(elapsed, PhaseLineStart, PhaseLineDuration));
            float panelT = EaseInOutCubic(Normalized(elapsed, PhasePanelStart, PhasePanelDuration));
            float fadeT = EaseOutCubic(Normalized(elapsed, PhaseUIStart, PhaseUIDuration));
            float alpha = BackdropAlpha * (1f - fadeT);
            if (alpha <= 0.001f)
            {
                return;
            }

            float widthT = Mathf.Max(0.002f, Mathf.Max(lineT, panelT));
            float lineHeight = Mathf.Lerp(2f, 12f, lineT);
            float height = Mathf.Lerp(lineHeight, target.height, panelT);
            float width = target.width * widthT;
            Rect revealRect = new Rect(
                target.center.x - width * 0.5f,
                target.center.y - height * 0.5f,
                width,
                height);

            Widgets.DrawBoxSolid(revealRect, new Color(0f, 0f, 0f, alpha));
            DrawRevealEdges(revealRect, alpha);
        }

        internal static void DrawCenteredLogo(Rect targetRect, Texture2D logo, float elapsed)
        {
            float alpha = GetLogoAlpha(elapsed);
            if (alpha <= 0.001f)
            {
                return;
            }

            Rect target = Inset(targetRect, BackdropInset);
            if (target.width <= 0f || target.height <= 0f)
            {
                return;
            }

            Color previousColor = GUI.color;
            GameFont previousFont = Text.Font;
            TextAnchor previousAnchor = Text.Anchor;

            try
            {
                float logoSize = Mathf.Min(156f, Mathf.Min(target.width * 0.24f, target.height * 0.30f));
                bool drawIcon = logo != null && logoSize >= 24f;
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;

                if (drawIcon)
                {
                    Rect logoRect = new Rect(
                        target.center.x - logoSize * 0.5f,
                        target.center.y - logoSize * 0.62f,
                        logoSize,
                        logoSize);
                    GUI.color = new Color(1f, 1f, 1f, alpha);
                    Widgets.DrawTextureFitted(logoRect, logo, 1f);

                    Rect labelRect = new Rect(
                        target.x,
                        logoRect.yMax + 6f,
                        target.width,
                        34f);
                    GUI.color = new Color(0.82f, 0.93f, 1f, alpha);
                    Widgets.Label(labelRect, "CeleTech");
                    return;
                }

                Rect fallbackRect = new Rect(
                    target.x,
                    target.center.y - 18f,
                    target.width,
                    36f);
                GUI.color = new Color(0.82f, 0.93f, 1f, alpha);
                Widgets.Label(fallbackRect, "CeleTech");
            }
            finally
            {
                GUI.color = previousColor;
                Text.Font = previousFont;
                Text.Anchor = previousAnchor;
            }
        }

        internal static void DrawFirstBootBackdrop(Rect targetRect, float elapsed)
        {
            Rect target = Inset(targetRect, BackdropInset);
            if (target.width <= 0f || target.height <= 0f)
            {
                return;
            }

            float introT = EaseInOutCubic(Normalized(elapsed, 0f, FirstBootIntroDuration));
            float fadeT = EaseOutCubic(Normalized(elapsed, FirstBootOutroStart, FirstBootOutroDuration));
            float alpha = 0.96f * (1f - fadeT);
            if (alpha <= 0.001f)
            {
                return;
            }

            float width = Mathf.Lerp(Mathf.Max(2f, target.width * 0.02f), target.width, introT);
            float height = Mathf.Lerp(3f, target.height, introT);
            Rect revealRect = new Rect(
                target.center.x - width * 0.5f,
                target.center.y - height * 0.5f,
                width,
                height);

            Widgets.DrawBoxSolid(revealRect, new Color(0f, 0f, 0f, alpha));
            DrawRevealEdges(revealRect, alpha);
        }

        internal static void DrawSystemUpdateBackdrop(Rect targetRect, float elapsed)
        {
            Rect target = Inset(targetRect, BackdropInset);
            if (target.width <= 0f || target.height <= 0f)
            {
                return;
            }

            float introT = EaseInOutCubic(Normalized(elapsed, 0f, SystemUpdateIntroDuration));
            float fadeT = EaseOutCubic(Normalized(elapsed, SystemUpdateOutroStart, SystemUpdateOutroDuration));
            float alpha = 0.96f * (1f - fadeT);
            if (alpha <= 0.001f)
            {
                return;
            }

            float width = Mathf.Lerp(Mathf.Max(2f, target.width * 0.02f), target.width, introT);
            float height = Mathf.Lerp(3f, target.height, introT);
            Rect revealRect = new Rect(
                target.center.x - width * 0.5f,
                target.center.y - height * 0.5f,
                width,
                height);

            Widgets.DrawBoxSolid(revealRect, new Color(0f, 0f, 0f, alpha));
            DrawRevealEdges(revealRect, alpha);
        }

        internal static void DrawFirstBootSelfCheck(Rect targetRect, Texture2D logo, float elapsed)
        {
            float introAlpha = EaseOutCubic(Normalized(elapsed, 0.18f, 0.42f));
            float outroAlpha = 1f - EaseOutCubic(Normalized(elapsed, FirstBootOutroStart, FirstBootOutroDuration));
            float alpha = Mathf.Clamp01(introAlpha * outroAlpha);
            if (alpha <= 0.001f)
            {
                return;
            }

            Rect target = Inset(targetRect, 22f);
            if (target.width <= 0f || target.height <= 0f)
            {
                return;
            }

            Color previousColor = GUI.color;
            GameFont previousFont = Text.Font;
            TextAnchor previousAnchor = Text.Anchor;
            bool previousWordWrap = Text.WordWrap;

            try
            {
                Text.WordWrap = true;
                DrawFirstBootLogoAndTitle(target, logo, alpha);
                DrawFirstBootProgress(target, elapsed, alpha);
                DrawBootHint(target, alpha);
            }
            finally
            {
                GUI.color = previousColor;
                Text.Font = previousFont;
                Text.Anchor = previousAnchor;
                Text.WordWrap = previousWordWrap;
            }
        }

        internal static void DrawSystemUpdateSelfCheck(
            Rect targetRect,
            Texture2D logo,
            float elapsed,
            IReadOnlyList<string> stepKeys)
        {
            float introAlpha = EaseOutCubic(Normalized(elapsed, 0.18f, 0.42f));
            float outroAlpha = 1f - EaseOutCubic(Normalized(elapsed, SystemUpdateOutroStart, SystemUpdateOutroDuration));
            float alpha = Mathf.Clamp01(introAlpha * outroAlpha);
            if (alpha <= 0.001f)
            {
                return;
            }

            Rect target = Inset(targetRect, 22f);
            if (target.width <= 0f || target.height <= 0f)
            {
                return;
            }

            Color previousColor = GUI.color;
            GameFont previousFont = Text.Font;
            TextAnchor previousAnchor = Text.Anchor;
            bool previousWordWrap = Text.WordWrap;

            try
            {
                Text.WordWrap = true;
                DrawLogoAndTitle(target, logo, alpha, ShuttleControlSystemUpdateUtility.TitleKey);
                DrawProgress(
                    target,
                    alpha,
                    GetSystemUpdateProgress(elapsed),
                    ShuttleControlSystemUpdateUtility.GetStepKeyForElapsed(stepKeys, elapsed));
            }
            finally
            {
                GUI.color = previousColor;
                Text.Font = previousFont;
                Text.Anchor = previousAnchor;
                Text.WordWrap = previousWordWrap;
            }
        }

        private static float GetLogoAlpha(float elapsed)
        {
            float fadeIn = EaseOutCubic(Normalized(elapsed, PhaseLogoStart, PhaseLogoDuration));
            float fadeOut = 1f - EaseOutCubic(Normalized(elapsed, PhaseUIStart, PhaseUIDuration));
            return Mathf.Clamp01(fadeIn * fadeOut);
        }

        private static void DrawFirstBootLogoAndTitle(Rect target, Texture2D logo, float alpha)
        {
            DrawLogoAndTitle(target, logo, alpha, FirstBootTitleKey);
        }

        private static void DrawLogoAndTitle(Rect target, Texture2D logo, float alpha, string titleKey)
        {
            float contentWidth = Mathf.Min(820f, target.width - 48f);
            float logoSize = Mathf.Min(220f, Mathf.Min(target.width * 0.26f, target.height * 0.30f));
            float logoY = target.y + Mathf.Max(32f, target.height * 0.08f);
            Rect logoRect = new Rect(
                target.center.x - logoSize * 0.5f,
                logoY,
                logoSize,
                logoSize);

            if (logo != null)
            {
                GUI.color = new Color(1f, 1f, 1f, alpha);
                Widgets.DrawTextureFitted(logoRect, logo, 1f);
            }
            else
            {
                GUI.color = new Color(0.82f, 0.93f, 1f, alpha);
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(logoRect, "CeleTech");
            }

            Rect brandRect = new Rect(target.center.x - contentWidth * 0.5f, logoRect.yMax + 6f, contentWidth, 32f);
            GUI.color = new Color(0.82f, 0.93f, 1f, alpha);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(brandRect, "CeleTech");

            Rect titleRect = new Rect(target.center.x - contentWidth * 0.5f, brandRect.yMax + 18f, contentWidth, 62f);
            GUI.color = new Color(0.90f, 0.96f, 1f, alpha);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, titleKey.Translate().ToString());
        }

        private static void DrawFirstBootProgress(Rect target, float elapsed, float alpha)
        {
            float progress = GetFirstBootSelfCheckProgress(elapsed);
            DrawProgress(target, alpha, progress, GetFirstBootStepKey(progress));
        }

        private static void DrawProgress(Rect target, float alpha, float progress, string stepKey)
        {
            float contentWidth = Mathf.Min(760f, target.width - 70f);
            float top = target.y + Mathf.Max(414f, target.height * 0.61f);
            Rect barRect = new Rect(target.center.x - contentWidth * 0.5f, top, contentWidth, 24f);
            progress = Mathf.Clamp01(progress);

            Widgets.DrawBoxSolid(barRect, new Color(0.010f, 0.014f, 0.018f, 0.72f * alpha));
            Rect fillRect = new Rect(barRect.x + 2f, barRect.y + 2f, (barRect.width - 4f) * progress, barRect.height - 4f);
            Widgets.DrawBoxSolid(fillRect, WithAlpha(BlueStatusColor, 0.82f * alpha));
            DrawRectBorder(
                barRect,
                WithAlpha(SubtleBorderColor, 0.95f * alpha),
                1f);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = new Color(0.90f, 0.96f, 1f, alpha);
            Widgets.Label(barRect, Mathf.RoundToInt(progress * 100f).ToString("0") + "%");

            Rect stepRect = new Rect(barRect.x, barRect.yMax + 18f, barRect.width, 52f);
            string step = stepKey.Translate().ToString();
            GUI.color = new Color(0.78f, 0.90f, 1f, alpha);
            Text.Font = GameFont.Small;
            Widgets.Label(stepRect, step);
        }

        private static void DrawBootHint(Rect target, float alpha)
        {
            float contentWidth = Mathf.Min(900f, target.width - 84f);
            if (contentWidth <= 0f)
            {
                return;
            }

            Rect hintBounds = new Rect(
                target.center.x - contentWidth * 0.5f,
                target.yMax - 104f,
                contentWidth,
                74f);

            string hint = BootHintKey.Translate().ToString();
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            float textHeight = Text.CalcHeight(hint, hintBounds.width);
            if (textHeight > 52f)
            {
                Text.Font = GameFont.Tiny;
                textHeight = Text.CalcHeight(hint, hintBounds.width);
            }

            textHeight = Mathf.Clamp(textHeight, 22f, hintBounds.height);
            Rect hintRect = new Rect(
                hintBounds.x,
                hintBounds.center.y - textHeight * 0.5f,
                hintBounds.width,
                textHeight);

            float hintAlpha = alpha * GetBootHintAlphaMultiplier();
            GUI.color = new Color(0.80f, 0.90f, 0.98f, hintAlpha);
            Widgets.Label(hintRect, hint);
        }

        private static string GetFirstBootStepKey(float progress)
        {
            int index = Mathf.Clamp(
                Mathf.FloorToInt(Mathf.Clamp01(progress) * FirstBootStepKeys.Length),
                0,
                FirstBootStepKeys.Length - 1);
            return FirstBootStepKeys[index];
        }

        private static float GetBootHintAlphaMultiplier()
        {
            if (!ShouldPulseBootHint())
            {
                return BootHintStaticAlpha;
            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * BootHintPulseSpeed);
            return Mathf.Lerp(BootHintPulseMinAlpha, BootHintPulseMaxAlpha, pulse);
        }

        private static bool ShouldPulseBootHint()
        {
            ShuttleEffectiveSettings effective = CeleTechShuttleMod.EffectiveSettings;
            if (effective == null)
            {
                return true;
            }

            if (!effective.UseWindowAnimations || !effective.UseSystemUpdateAnimations)
            {
                return false;
            }

            return effective.VisualQuality != ShuttleVisualQuality.Minimal &&
                effective.VisualQuality != ShuttleVisualQuality.Performance;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private static void DrawRevealEdges(Rect revealRect, float backdropAlpha)
        {
            float edgeAlpha = 0.35f * Mathf.Clamp01(backdropAlpha / BackdropAlpha);
            if (edgeAlpha <= 0.001f)
            {
                return;
            }

            Color edge = BlueStatusColor;
            edge.a = edgeAlpha;
            float line = Mathf.Min(1.5f, Mathf.Max(1f, revealRect.height));
            Widgets.DrawBoxSolid(new Rect(revealRect.x, revealRect.y, revealRect.width, line), edge);
            Widgets.DrawBoxSolid(new Rect(revealRect.x, revealRect.yMax - line, revealRect.width, line), edge);
        }

        private static void DrawRectBorder(Rect rect, Color color, float thickness)
        {
            float line = Mathf.Max(1f, thickness);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, line), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - line, rect.width, line), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, line, rect.height), color);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - line, rect.y, line, rect.height), color);
        }

        private static Rect Inset(Rect rect, float inset)
        {
            return new Rect(
                rect.x + inset,
                rect.y + inset,
                Mathf.Max(0f, rect.width - inset * 2f),
                Mathf.Max(0f, rect.height - inset * 2f));
        }
    }
}

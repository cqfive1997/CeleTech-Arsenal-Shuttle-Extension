using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common
{
    internal static class ShuttleUIStyle
    {
        internal static readonly Color PanelColor =
            new Color(0.045f, 0.055f, 0.073f, 0.97f);
        internal static readonly Color HeaderColor =
            new Color(0.065f, 0.080f, 0.106f, 0.98f);
        internal static readonly Color CardColor =
            new Color(0.112f, 0.132f, 0.164f, 0.95f);
        internal static readonly Color LeftColumnCardColor =
            new Color(0.090f, 0.130f, 0.175f, 0.92f);
        internal static readonly Color RightTopCardColor =
            new Color(0.082f, 0.145f, 0.140f, 0.92f);
        internal static readonly Color RightBottomCardColor =
            new Color(0.108f, 0.124f, 0.152f, 0.94f);
        internal static readonly Color HeaderActionButtonColor =
            new Color(0.250f, 0.205f, 0.112f, 0.96f);
        internal static readonly Color TimeChipColor =
            new Color(0.105f, 0.155f, 0.195f, 0.72f);
        internal static readonly Color MutedCardColor =
            new Color(0.060f, 0.078f, 0.105f, 0.94f);
        internal static readonly Color SelectedColor =
            new Color(0.130f, 0.285f, 0.405f, 0.96f);
        internal static readonly Color DisabledColor =
            new Color(0.092f, 0.100f, 0.112f, 0.78f);
        internal static readonly Color BorderColor =
            new Color(0.37f, 0.47f, 0.57f, 0.78f);
        internal static readonly Color SubtleBorderColor =
            new Color(0.22f, 0.29f, 0.36f, 0.62f);
        internal static readonly Color MutedTextColor =
            new Color(0.65f, 0.71f, 0.77f, 1f);
        internal static readonly Color HeaderTitleTextColor =
            new Color(0.86f, 0.94f, 1f, 1f);
        internal static readonly Color SelectionCardDisabledTextColor =
            new Color(0.46f, 0.50f, 0.54f, 1f);
        internal static readonly Color SettingsPreviewCardColor =
            new Color(0.060f, 0.086f, 0.110f, 0.90f);
        internal static readonly Color SettingsInfoRowColor =
            new Color(0.075f, 0.095f, 0.122f, 0.82f);
        internal static readonly Color IconFallbackBackgroundColor =
            new Color(0f, 0f, 0f, 0.22f);
        internal static readonly Color GreenStatusColor =
            new Color(0.42f, 0.78f, 0.55f, 1f);
        internal static readonly Color BlueStatusColor =
            new Color(0.28f, 0.64f, 0.86f, 1f);
        internal static readonly Color YellowStatusColor =
            new Color(0.82f, 0.62f, 0.30f, 1f);
        internal static readonly Color RedStatusColor =
            new Color(0.86f, 0.34f, 0.30f, 1f);
        internal static readonly Color HeaderPowerMeterColor =
            new Color(0.808f, 1f, 0.576f, 1f);
        internal static readonly Color HeaderShieldMeterColor =
            new Color(0.576f, 0.976f, 1f, 1f);
        internal static readonly Color HeaderArmorMeterColor =
            new Color(1f, 0.753f, 0.208f, 1f);
        internal static readonly Color ButtonBgColor =
            new Color(0.10f, 0.16f, 0.20f, 0.88f);
        internal static readonly Color ButtonBgHoverColor =
            new Color(0.14f, 0.23f, 0.29f, 0.95f);
        internal static readonly Color ButtonDisabledBgColor =
            new Color(0.040f, 0.050f, 0.060f, 0.58f);
        internal static readonly Color ButtonBorderColor =
            new Color(0.45f, 0.68f, 0.82f, 0.95f);
        internal static readonly Color ButtonInnerHighlightColor =
            new Color(0.75f, 0.92f, 1f, 0.18f);
        internal static readonly Color ButtonTextColor =
            new Color(0.88f, 0.96f, 1f, 1f);
        internal static readonly Color PrimaryButtonBgColor =
            new Color(0.10f, 0.24f, 0.28f, 0.92f);
        internal static readonly Color PrimaryButtonBorderColor =
            new Color(0.36f, 0.90f, 0.95f, 1f);
        internal static readonly Color PrimaryButtonTextColor =
            new Color(0.70f, 1f, 1f, 1f);
        internal static readonly Color DangerButtonBgColor =
            new Color(0.22f, 0.10f, 0.10f, 0.82f);
        internal static readonly Color DangerButtonBorderColor =
            new Color(0.85f, 0.35f, 0.35f, 0.95f);
        internal static readonly Color DangerButtonTextColor =
            new Color(1f, 0.78f, 0.78f, 1f);
        internal static readonly Color ActionRowBgColor =
            new Color(0.07f, 0.10f, 0.13f, 0.78f);
        internal static readonly Color ActionRowBgHoverColor =
            new Color(0.12f, 0.18f, 0.22f, 0.88f);
        internal static readonly Color ActionRowBorderColor =
            new Color(0.27f, 0.42f, 0.52f, 0.85f);
        internal static readonly Color ActionRowAccentNormalColor =
            new Color(0.58f, 0.78f, 0.92f, 1f);
        internal static readonly Color ActionRowTextColor =
            new Color(0.78f, 0.88f, 0.94f, 1f);
        internal static readonly Color ActionRowAccentPrimaryColor =
            new Color(0.35f, 0.95f, 1f, 1f);
        internal static readonly Color ActionRowTextPrimaryColor =
            new Color(0.82f, 1f, 1f, 1f);
        internal static readonly Color ActionRowDangerBgColor =
            new Color(0.11f, 0.06f, 0.07f, 0.78f);
        internal static readonly Color ActionRowDangerBgHoverColor =
            new Color(0.18f, 0.09f, 0.10f, 0.88f);
        internal static readonly Color ActionRowDangerBorderColor =
            new Color(0.45f, 0.24f, 0.26f, 0.86f);
        internal static readonly Color ActionRowAccentDangerColor =
            new Color(0.90f, 0.38f, 0.38f, 1f);
        internal static readonly Color ActionRowDisabledBgColor =
            new Color(0.045f, 0.052f, 0.062f, 0.56f);

        internal const float HeaderHeight = 176f;
        internal const float Gap = 10f;
        internal const float SmallGap = 6f;
        internal const float StandardPadding = 8f;
        internal const float StandardGap = 6f;
        internal const float LargeGap = 10f;
        internal const float PageRightPanelWidth = 350f;
        internal const float StatusRowHeight = 34f;
        internal const float MessageHeight = 218f;
        internal const float ThinBorder = 1f;
        internal const float ThickBorder = 2f;
        internal const float HoverGlowAlpha = 0.12f;
        internal const float DisabledAlpha = 0.45f;
        internal const float StatusBadgeFillAlpha = 0.24f;
        internal const float StrongStatusBadgeFillAlpha = 0.30f;
        internal const float ButtonPressedDarken = 0.70f;
        internal const float ButtonHoverLighten = 0.055f;
        internal const float ButtonHoverAlpha = 0.97f;
        internal const float TransparentButtonSelectedAlpha = 0.62f;
        internal const float TransparentButtonAlpha = 0.38f;

        internal static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}

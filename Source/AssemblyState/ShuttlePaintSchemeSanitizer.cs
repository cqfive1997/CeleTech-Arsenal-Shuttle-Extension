using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    internal static class ShuttlePaintSchemeSanitizer
    {
        internal const bool DefaultEnabled = true;

        internal static readonly Color DefaultPrimaryColor = new Color(135f / 255f, 147f / 255f, 149f / 255f, 1f);
        internal static readonly Color DefaultSecondaryColor = new Color(78f / 255f, 91f / 255f, 96f / 255f, 1f);
        internal static readonly Color DefaultAccentColor = new Color(36f / 255f, 50f / 255f, 58f / 255f, 1f);

        internal static void Sanitize(ShuttlePaintScheme scheme)
        {
            if (scheme == null)
            {
                return;
            }

            bool useDefaultScheme =
                NeedsColorFallback(scheme.PrimaryColor) ||
                NeedsColorFallback(scheme.SecondaryColor) ||
                NeedsColorFallback(scheme.AccentColor);
            Color primary = SanitizeColor(scheme.PrimaryColor, DefaultPrimaryColor);
            Color secondary = SanitizeColor(scheme.SecondaryColor, DefaultSecondaryColor);
            Color accent = SanitizeColor(scheme.AccentColor, DefaultAccentColor);
            string presetKey = string.IsNullOrEmpty(scheme.PresetKey)
                ? ShuttlePaintScheme.DefaultPresetKey
                : scheme.PresetKey;
            if (useDefaultScheme)
            {
                primary = DefaultPrimaryColor;
                secondary = DefaultSecondaryColor;
                accent = DefaultAccentColor;
            }

            scheme.ApplySanitizedValues(
                useDefaultScheme ? DefaultEnabled : scheme.Enabled,
                primary,
                secondary,
                accent,
                presetKey);
        }

        internal static Color SanitizeColor(Color value, Color fallback)
        {
            if (NeedsColorFallback(value))
            {
                value = fallback;
            }

            return new Color(
                Mathf.Clamp01(value.r),
                Mathf.Clamp01(value.g),
                Mathf.Clamp01(value.b),
                1f);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool NeedsColorFallback(Color value)
        {
            return !IsFinite(value.r) ||
                !IsFinite(value.g) ||
                !IsFinite(value.b) ||
                !IsFinite(value.a) ||
                IsEmptyColor(value);
        }

        private static bool IsEmptyColor(Color value)
        {
            return Mathf.Abs(value.r) < 0.0001f &&
                Mathf.Abs(value.g) < 0.0001f &&
                Mathf.Abs(value.b) < 0.0001f &&
                Mathf.Abs(value.a) < 0.0001f;
        }
    }
}

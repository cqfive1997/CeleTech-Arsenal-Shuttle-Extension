using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    /// <summary>
    /// Durable per-shuttle paint configuration. This is static assembly configuration, not
    /// runtime state and not a global mod setting.
    /// </summary>
    internal sealed class ShuttlePaintScheme : IExposable
    {
        internal const string DefaultPresetKey = "default";

        private bool enabled = ShuttlePaintSchemeSanitizer.DefaultEnabled;
        private Color primaryColor = ShuttlePaintSchemeSanitizer.DefaultPrimaryColor;
        private Color secondaryColor = ShuttlePaintSchemeSanitizer.DefaultSecondaryColor;
        private Color accentColor = ShuttlePaintSchemeSanitizer.DefaultAccentColor;
        private string presetKey = DefaultPresetKey;

        internal bool Enabled
        {
            get
            {
                return this.enabled;
            }
        }

        internal Color PrimaryColor
        {
            get
            {
                return this.primaryColor;
            }
        }

        internal Color SecondaryColor
        {
            get
            {
                return this.secondaryColor;
            }
        }

        internal Color AccentColor
        {
            get
            {
                return this.accentColor;
            }
        }

        internal string PresetKey
        {
            get
            {
                return this.presetKey;
            }
        }

        internal void Set(
            bool enabled,
            Color primaryColor,
            Color secondaryColor,
            Color accentColor,
            string presetKey)
        {
            this.enabled = enabled;
            this.primaryColor = primaryColor;
            this.secondaryColor = secondaryColor;
            this.accentColor = accentColor;
            this.presetKey = string.IsNullOrEmpty(presetKey) ? null : presetKey;
            this.Sanitize();
        }

        internal void ResetToDefault()
        {
            this.enabled = ShuttlePaintSchemeSanitizer.DefaultEnabled;
            this.primaryColor = ShuttlePaintSchemeSanitizer.DefaultPrimaryColor;
            this.secondaryColor = ShuttlePaintSchemeSanitizer.DefaultSecondaryColor;
            this.accentColor = ShuttlePaintSchemeSanitizer.DefaultAccentColor;
            this.presetKey = DefaultPresetKey;
        }

        internal void EnsureInitialized()
        {
            this.Sanitize();
        }

        internal void Sanitize()
        {
            ShuttlePaintSchemeSanitizer.Sanitize(this);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.enabled, "enabled", ShuttlePaintSchemeSanitizer.DefaultEnabled);
            Scribe_Values.Look(ref this.primaryColor, "primaryColor", ShuttlePaintSchemeSanitizer.DefaultPrimaryColor);
            Scribe_Values.Look(ref this.secondaryColor, "secondaryColor", ShuttlePaintSchemeSanitizer.DefaultSecondaryColor);
            Scribe_Values.Look(ref this.accentColor, "accentColor", ShuttlePaintSchemeSanitizer.DefaultAccentColor);
            Scribe_Values.Look(ref this.presetKey, "presetKey", DefaultPresetKey);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
            }
        }

        internal void ApplySanitizedValues(
            bool enabled,
            Color primaryColor,
            Color secondaryColor,
            Color accentColor,
            string presetKey)
        {
            this.enabled = enabled;
            this.primaryColor = primaryColor;
            this.secondaryColor = secondaryColor;
            this.accentColor = accentColor;
            this.presetKey = string.IsNullOrEmpty(presetKey) ? DefaultPresetKey : presetKey;
        }
    }
}

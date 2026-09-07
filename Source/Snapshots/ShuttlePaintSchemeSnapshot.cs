using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Snapshots
{
    public sealed class ShuttlePaintSchemeSnapshot
    {
        public bool Enabled;
        public Color PrimaryColor;
        public Color SecondaryColor;
        public Color AccentColor;
        public string PresetKey;

        public static ShuttlePaintSchemeSnapshot Default
        {
            get
            {
                return new ShuttlePaintSchemeSnapshot
                {
                    Enabled = ShuttlePaintSchemeSanitizer.DefaultEnabled,
                    PrimaryColor = ShuttlePaintSchemeSanitizer.DefaultPrimaryColor,
                    SecondaryColor = ShuttlePaintSchemeSanitizer.DefaultSecondaryColor,
                    AccentColor = ShuttlePaintSchemeSanitizer.DefaultAccentColor,
                    PresetKey = ShuttlePaintScheme.DefaultPresetKey
                };
            }
        }

        internal static ShuttlePaintSchemeSnapshot FromScheme(ShuttlePaintScheme scheme)
        {
            if (scheme == null)
            {
                return Default;
            }

            scheme.EnsureInitialized();
            return new ShuttlePaintSchemeSnapshot
            {
                Enabled = scheme.Enabled,
                PrimaryColor = scheme.PrimaryColor,
                SecondaryColor = scheme.SecondaryColor,
                AccentColor = scheme.AccentColor,
                PresetKey = scheme.PresetKey
            };
        }

        public ShuttlePaintSchemeSnapshot Clone()
        {
            return new ShuttlePaintSchemeSnapshot
            {
                Enabled = this.Enabled,
                PrimaryColor = this.PrimaryColor,
                SecondaryColor = this.SecondaryColor,
                AccentColor = this.AccentColor,
                PresetKey = this.PresetKey
            };
        }
    }
}

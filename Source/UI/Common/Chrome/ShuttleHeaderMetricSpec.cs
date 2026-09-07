using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome
{
    internal struct ShuttleHeaderMetricSpec
    {
        internal readonly string Label;
        internal readonly string CompactLabel;
        internal readonly string Value;
        internal readonly string Tooltip;
        internal readonly Color AccentColor;

        internal ShuttleHeaderMetricSpec(
            string label,
            string value,
            string tooltip,
            Color accentColor)
            : this(label, null, value, tooltip, accentColor)
        {
        }

        internal ShuttleHeaderMetricSpec(
            string label,
            string compactLabel,
            string value,
            string tooltip,
            Color accentColor)
        {
            this.Label = label;
            this.CompactLabel = compactLabel;
            this.Value = value;
            this.Tooltip = tooltip;
            this.AccentColor = accentColor;
        }
    }
}

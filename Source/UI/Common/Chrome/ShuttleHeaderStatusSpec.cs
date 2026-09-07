using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome
{
    internal struct ShuttleHeaderStatusSpec
    {
        internal readonly string Label;
        internal readonly string Value;
        internal readonly string Tooltip;
        internal readonly Color SeverityColor;
        internal readonly float Progress01;

        internal ShuttleHeaderStatusSpec(
            string label,
            string value,
            string tooltip,
            Color severityColor)
            : this(label, value, tooltip, severityColor, 0f)
        {
        }

        internal ShuttleHeaderStatusSpec(
            string label,
            string value,
            string tooltip,
            Color severityColor,
            float progress01)
        {
            this.Label = label;
            this.Value = value;
            this.Tooltip = tooltip;
            this.SeverityColor = severityColor;
            this.Progress01 = Mathf.Clamp01(progress01);
        }
    }
}

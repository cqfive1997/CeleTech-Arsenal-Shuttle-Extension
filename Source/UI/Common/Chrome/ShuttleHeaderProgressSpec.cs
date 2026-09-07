using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome
{
    internal struct ShuttleHeaderProgressSpec
    {
        internal readonly bool Visible;
        internal readonly string Label;
        internal readonly string Tooltip;
        internal readonly float Progress01;
        internal readonly Color AccentColor;

        internal ShuttleHeaderProgressSpec(
            string label,
            string tooltip,
            float progress01,
            Color accentColor)
        {
            this.Visible = true;
            this.Label = label;
            this.Tooltip = tooltip;
            this.Progress01 = Mathf.Clamp01(progress01);
            this.AccentColor = accentColor;
        }

        internal static ShuttleHeaderProgressSpec Empty
        {
            get { return new ShuttleHeaderProgressSpec(); }
        }
    }
}

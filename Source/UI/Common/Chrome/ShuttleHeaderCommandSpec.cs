using System;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome
{
    internal struct ShuttleHeaderCommandSpec
    {
        internal readonly string Label;
        internal readonly string Tooltip;
        internal readonly Texture2D Icon;
        internal readonly Action OnClick;
        internal readonly bool Enabled;

        internal ShuttleHeaderCommandSpec(
            string label,
            string tooltip,
            Texture2D icon,
            Action onClick,
            bool enabled)
        {
            this.Label = label;
            this.Tooltip = tooltip;
            this.Icon = icon;
            this.OnClick = onClick;
            this.Enabled = enabled;
        }
    }
}

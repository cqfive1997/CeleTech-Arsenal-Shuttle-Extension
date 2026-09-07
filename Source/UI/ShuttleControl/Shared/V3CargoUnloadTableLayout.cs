using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoUnloadTableLayout
    {
        internal Rect IconRect;
        internal Rect InfoRect;
        internal Rect NameRect;
        internal Rect AvailableRect;
        internal Rect UnitMassRect;
        internal Rect SourceRect;
        internal Rect ActionRect;
        internal bool ShowUnitMass;
        internal bool ShowSource;
        internal bool ShowInfoButton;

        internal static V3CargoUnloadTableLayout Create(Rect rect)
        {
            V3CargoUnloadTableLayout layout = new V3CargoUnloadTableLayout();
            layout.ShowSource = rect.width >= 620f;
            layout.ShowUnitMass = rect.width >= 520f;
            layout.ShowInfoButton = rect.width >= 460f;
            float actionWidth = V3CargoUnloadQuantityEditor.Width;
            float sourceWidth = layout.ShowSource ? 136f : 0f;
            float massWidth = layout.ShowUnitMass ? 70f : 0f;
            const float availableWidth = 54f;
            const float gap = 7f;

            layout.ActionRect = new Rect(
                rect.xMax - actionWidth - 4f,
                rect.y + 3f,
                actionWidth,
                rect.height - 6f);
            float cursor = layout.ActionRect.x - gap;
            if (layout.ShowSource)
            {
                layout.SourceRect = new Rect(
                    cursor - sourceWidth,
                    rect.y,
                    sourceWidth,
                    rect.height);
                cursor = layout.SourceRect.x - gap;
            }

            if (layout.ShowUnitMass)
            {
                layout.UnitMassRect = new Rect(
                    cursor - massWidth,
                    rect.y,
                    massWidth,
                    rect.height);
                cursor = layout.UnitMassRect.x - gap;
            }

            layout.AvailableRect = new Rect(
                cursor - availableWidth,
                rect.y,
                availableWidth,
                rect.height);
            layout.IconRect = new Rect(
                rect.x + 3f,
                rect.y + 3f,
                rect.height - 6f,
                rect.height - 6f);
            float nameStart = layout.IconRect.xMax + gap;
            if (layout.ShowInfoButton)
            {
                layout.InfoRect = new Rect(
                    nameStart,
                    rect.y + (rect.height - 24f) / 2f,
                    24f,
                    24f);
                nameStart = layout.InfoRect.xMax + gap;
            }

            layout.NameRect = new Rect(
                nameStart,
                rect.y,
                Mathf.Max(30f, layout.AvailableRect.x - nameStart - gap),
                rect.height);
            return layout;
        }
    }
}

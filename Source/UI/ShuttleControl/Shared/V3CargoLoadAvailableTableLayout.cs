using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal struct V3CargoLoadAvailableTableLayout
    {
        private const float OuterPadding = 4f;
        private const float ColumnGap = 6f;
        private const float IconWidth = 28f;
        private const float InfoWidth = 24f;
        private const float AvailableWidth = 58f;
        private const float UnitMassWidth = 72f;
        private const float DestinationWidth = 82f;
        private const float DestinationThreshold = 650f;
        private const float UnitMassThreshold = 540f;
        private const float InfoButtonThreshold = 400f;

        internal readonly Rect IconRect;
        internal readonly Rect InfoRect;
        internal readonly Rect NameRect;
        internal readonly Rect AvailableRect;
        internal readonly Rect UnitMassRect;
        internal readonly Rect DestinationRect;
        internal readonly Rect QuantityRect;
        internal readonly bool ShowInfoButton;
        internal readonly bool ShowUnitMass;
        internal readonly bool ShowDestination;

        private V3CargoLoadAvailableTableLayout(
            Rect iconRect,
            Rect infoRect,
            Rect nameRect,
            Rect availableRect,
            Rect unitMassRect,
            Rect destinationRect,
            Rect quantityRect,
            bool showInfoButton,
            bool showUnitMass,
            bool showDestination)
        {
            this.IconRect = iconRect;
            this.InfoRect = infoRect;
            this.NameRect = nameRect;
            this.AvailableRect = availableRect;
            this.UnitMassRect = unitMassRect;
            this.DestinationRect = destinationRect;
            this.QuantityRect = quantityRect;
            this.ShowInfoButton = showInfoButton;
            this.ShowUnitMass = showUnitMass;
            this.ShowDestination = showDestination;
        }

        internal static V3CargoLoadAvailableTableLayout Create(Rect rect)
        {
            bool showDestination = rect.width >= DestinationThreshold;
            bool showUnitMass = rect.width >= UnitMassThreshold;
            bool showInfoButton = rect.width >= InfoButtonThreshold;
            float contentHeight = Mathf.Max(0f, rect.height);
            float iconSize = Mathf.Min(IconWidth, contentHeight - 4f);
            float iconY = rect.y + (contentHeight - iconSize) / 2f;
            Rect iconRect = new Rect(rect.x + OuterPadding, iconY, IconWidth, iconSize);
            Rect infoRect = new Rect();
            float nameStart = iconRect.xMax + ColumnGap;
            if (showInfoButton)
            {
                infoRect = new Rect(
                    nameStart,
                    rect.y + (contentHeight - InfoWidth) / 2f,
                    InfoWidth,
                    InfoWidth);
                nameStart = infoRect.xMax + ColumnGap;
            }

            Rect quantityRect = new Rect(
                rect.xMax - OuterPadding - V3CargoLoadQuantityEditor.Width,
                rect.y,
                V3CargoLoadQuantityEditor.Width,
                contentHeight);
            float right = quantityRect.x - ColumnGap;

            Rect destinationRect = new Rect();
            if (showDestination)
            {
                destinationRect = new Rect(
                    right - DestinationWidth,
                    rect.y,
                    DestinationWidth,
                    contentHeight);
                right = destinationRect.x - ColumnGap;
            }

            Rect unitMassRect = new Rect();
            if (showUnitMass)
            {
                unitMassRect = new Rect(
                    right - UnitMassWidth,
                    rect.y,
                    UnitMassWidth,
                    contentHeight);
                right = unitMassRect.x - ColumnGap;
            }

            Rect availableRect = new Rect(
                right - AvailableWidth,
                rect.y,
                AvailableWidth,
                contentHeight);
            Rect nameRect = new Rect(
                nameStart,
                rect.y,
                Mathf.Max(36f, availableRect.x - nameStart - ColumnGap),
                contentHeight);

            return new V3CargoLoadAvailableTableLayout(
                iconRect,
                infoRect,
                nameRect,
                availableRect,
                unitMassRect,
                destinationRect,
                quantityRect,
                showInfoButton,
                showUnitMass,
                showDestination);
        }
    }
}

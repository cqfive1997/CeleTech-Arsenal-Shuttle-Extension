using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome
{
    internal struct ShuttleControlHeaderTopRowLayout
    {
        internal Rect RowRect;
        internal Rect LogoRect;
        internal Rect TitleRect;
        internal Rect StatusCardsRect;
        internal float StatusCardWidth;
        internal float StatusCardGap;
        internal float CommandX;
        internal float CommandTileSize;
        internal float CommandTileGap;
        internal int StatusCardCount;
        internal int CommandCount;

        internal Rect GetStatusCardRect(int index)
        {
            if (index < 0 || index >= this.StatusCardCount || this.StatusCardWidth <= 0f)
            {
                return Rect.zero;
            }

            return new Rect(
                this.StatusCardsRect.x + ((this.StatusCardWidth + this.StatusCardGap) * index),
                this.StatusCardsRect.y,
                this.StatusCardWidth,
                this.StatusCardsRect.height);
        }

        internal Rect GetCommandRect(int index)
        {
            if (index < 0 || index >= this.CommandCount || this.CommandTileSize <= 0f)
            {
                return Rect.zero;
            }

            return new Rect(
                this.CommandX + ((this.CommandTileSize + this.CommandTileGap) * index),
                this.RowRect.y,
                this.CommandTileSize,
                this.RowRect.height);
        }
    }

    internal static class ShuttleControlHeaderLayout
    {
        internal const float HeaderHeight = 176f;
        internal const float HeaderSideInset = 12f;
        internal const float HeaderTopInset = 10f;
        internal const float HeaderRowHeight = 82f;
        internal const float LogoSize = 48f;
        internal const float SummaryRibbonTop = 102f;
        internal const float SummaryRibbonHeight = 58f;
        internal const float SummaryRibbonSideInset = 12f;

        private const float CompactLogoSize = 42f;
        private const float TitleGap = 6f;
        private const float TitleReserveGap = 14f;
        private const float TitleMinWidth = 168f;
        private const float TitleMaxWidth = 212f;
        private const float CompactTitleMinWidth = 120f;
        private const float CompactTitleMaxWidth = 176f;
        private const float StatusCardMinWidth = 74f;
        private const float StatusCardNarrowMinWidth = 44f;
        private const float StatusCardMaxWidth = 108f;
        private const float StatusCommandGap = 6f;
        private const float CommandTileGap = 4f;
        private const float MinCommandTileSize = 48f;
        private const int MaxStatusCards = 8;
        private const int MaxCommands = 8;

        internal static Rect GetHeaderRect(Rect fullRect)
        {
            float height = Mathf.Min(HeaderHeight, Mathf.Max(0f, fullRect.height));
            return new Rect(fullRect.x, fullRect.y, fullRect.width, height);
        }

        internal static Rect GetTopRowRect(Rect headerRect)
        {
            return new Rect(
                headerRect.x + HeaderSideInset,
                headerRect.y + HeaderTopInset,
                Mathf.Max(0f, headerRect.width - (HeaderSideInset * 2f)),
                Mathf.Min(HeaderRowHeight, Mathf.Max(0f, headerRect.height - HeaderTopInset)));
        }

        internal static Rect GetSummaryRibbonRect(Rect headerRect)
        {
            float y = headerRect.y + SummaryRibbonTop;
            float height = Mathf.Min(
                SummaryRibbonHeight,
                Mathf.Max(0f, headerRect.yMax - y - HeaderTopInset));
            return new Rect(
                headerRect.x + SummaryRibbonSideInset,
                y,
                Mathf.Max(0f, headerRect.width - (SummaryRibbonSideInset * 2f)),
                height);
        }

        internal static Rect GetBodyRect(Rect fullRect)
        {
            float headerHeight = Mathf.Min(HeaderHeight, Mathf.Max(0f, fullRect.height));
            return new Rect(
                fullRect.x,
                fullRect.y + headerHeight,
                fullRect.width,
                Mathf.Max(0f, fullRect.height - headerHeight));
        }

        internal static ShuttleControlHeaderTopRowLayout CalculateTopRowLayout(
            Rect headerRect,
            int statusCardCount,
            int commandCount)
        {
            int safeStatusCardCount = Mathf.Clamp(statusCardCount, 0, MaxStatusCards);
            int safeCommandCount = Mathf.Clamp(commandCount, 0, MaxCommands);
            Rect rowRect = GetTopRowRect(headerRect);
            float logoSize = safeStatusCardCount > 4 ? CompactLogoSize : LogoSize;
            Rect logoRect = new Rect(
                rowRect.x,
                rowRect.y + ((rowRect.height - logoSize) * 0.5f),
                logoSize,
                logoSize);

            float titleReserve = CalculateTitleReserve(rowRect.width, safeStatusCardCount);
            float minBrandWidth = logoRect.width + TitleReserveGap + titleReserve;
            float commandTileSize = safeCommandCount > 0 ? rowRect.height : 0f;
            float commandTotalWidth = GetCommandTotalWidth(safeCommandCount, commandTileSize);
            float statusTotalMinWidth = GetStatusTotalWidth(safeStatusCardCount, StatusCardMinWidth);
            float statusGapToCommands = safeStatusCardCount > 0 && safeCommandCount > 0
                ? StatusCommandGap
                : 0f;

            float minRequiredWidth = minBrandWidth + statusTotalMinWidth +
                statusGapToCommands + commandTotalWidth;
            if (safeCommandCount > 0 && rowRect.width < minRequiredWidth)
            {
                float commandGapWidth = CommandTileGap * GetGapCount(safeCommandCount);
                float availableForCommands = rowRect.width - minBrandWidth -
                    statusTotalMinWidth - statusGapToCommands - commandGapWidth;
                commandTileSize = Mathf.Min(
                    rowRect.height,
                    Mathf.Max(MinCommandTileSize, availableForCommands / safeCommandCount));
                commandTotalWidth = GetCommandTotalWidth(safeCommandCount, commandTileSize);
            }

            float commandX = rowRect.xMax - commandTotalWidth;
            float statusCardWidth = 0f;
            float statusCardsWidth = 0f;
            float firstStatusX = commandX;
            if (safeStatusCardCount > 0)
            {
                float availableStatusWidth = Mathf.Max(
                    0f,
                    commandX - rowRect.x - minBrandWidth - statusGapToCommands);
                statusCardWidth = CalculateStatusCardWidth(
                    availableStatusWidth,
                    safeStatusCardCount);
                statusCardsWidth = GetStatusTotalWidth(safeStatusCardCount, statusCardWidth);
                firstStatusX = commandX - statusGapToCommands - statusCardsWidth;
            }

            float titleRight = safeStatusCardCount > 0
                ? firstStatusX
                : commandX - statusGapToCommands;
            float titleWidth = Mathf.Max(
                0f,
                Mathf.Max(titleReserve, titleRight - logoRect.xMax - TitleReserveGap));
            Rect titleRect = new Rect(
                logoRect.xMax + TitleGap,
                rowRect.y + 16f,
                titleWidth,
                44f);
            Rect statusCardsRect = new Rect(
                firstStatusX,
                rowRect.y,
                statusCardsWidth,
                rowRect.height);

            ShuttleControlHeaderTopRowLayout layout = new ShuttleControlHeaderTopRowLayout();
            layout.RowRect = rowRect;
            layout.LogoRect = logoRect;
            layout.TitleRect = titleRect;
            layout.StatusCardsRect = statusCardsRect;
            layout.StatusCardWidth = statusCardWidth;
            layout.StatusCardGap = SmallStatusCardGap;
            layout.CommandX = commandX;
            layout.CommandTileSize = commandTileSize;
            layout.CommandTileGap = CommandTileGap;
            layout.StatusCardCount = safeStatusCardCount;
            layout.CommandCount = safeCommandCount;
            return layout;
        }

        private const float SmallStatusCardGap = 6f;

        private static float CalculateTitleReserve(float rowWidth, int statusCardCount)
        {
            float min = statusCardCount > 4 ? CompactTitleMinWidth : TitleMinWidth;
            float max = statusCardCount > 4 ? CompactTitleMaxWidth : TitleMaxWidth;
            float ratio = statusCardCount > 4 ? 0.12f : 0.16f;
            return Mathf.Clamp(rowWidth * ratio, min, max);
        }

        private static float CalculateStatusCardWidth(float availableWidth, int count)
        {
            if (count <= 0)
            {
                return 0f;
            }

            float gapWidth = SmallStatusCardGap * GetGapCount(count);
            float naturalWidth = (availableWidth - gapWidth) / count;
            if (availableWidth >= GetStatusTotalWidth(count, StatusCardMinWidth))
            {
                return Mathf.Max(
                    StatusCardMinWidth,
                    Mathf.Min(StatusCardMaxWidth, naturalWidth));
            }

            return Mathf.Max(
                StatusCardNarrowMinWidth,
                Mathf.Min(StatusCardMaxWidth, naturalWidth));
        }

        private static float GetCommandTotalWidth(int count, float tileSize)
        {
            if (count <= 0 || tileSize <= 0f)
            {
                return 0f;
            }

            return (tileSize * count) + (CommandTileGap * GetGapCount(count));
        }

        private static float GetStatusTotalWidth(int count, float tileWidth)
        {
            if (count <= 0 || tileWidth <= 0f)
            {
                return 0f;
            }

            return (tileWidth * count) + (SmallStatusCardGap * GetGapCount(count));
        }

        private static int GetGapCount(int count)
        {
            return count > 1 ? count - 1 : 0;
        }
    }
}

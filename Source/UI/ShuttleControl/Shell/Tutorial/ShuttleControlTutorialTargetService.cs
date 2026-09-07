using System.Collections.Generic;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttleControlTutorialTargetService :
        IShuttleTutorialTargetService
    {
        private readonly Dictionary<string, Rect> targets =
            new Dictionary<string, Rect>();
        private Rect windowRect;
        private bool hasFrame;

        internal ShuttleControlPageId CurrentPage { get; private set; }

        internal Rect WindowRect
        {
            get { return this.windowRect; }
        }

        internal void BeginFrame(ShuttleControlPageId page, Rect windowRect)
        {
            this.targets.Clear();
            this.CurrentPage = page;
            this.windowRect = windowRect;
            this.hasFrame = IsValid(windowRect);
        }

        public void Register(string targetId, Rect rect)
        {
            if (string.IsNullOrEmpty(targetId) || !IsValid(rect))
            {
                return;
            }

            Rect clipped = this.hasFrame
                ? Intersect(rect, this.windowRect)
                : rect;
            if (!IsValid(clipped))
            {
                return;
            }

            this.targets[targetId] = clipped;
        }

        public void RegisterClipped(string targetId, Rect rect, Rect visibleBounds)
        {
            this.Register(targetId, Intersect(rect, visibleBounds));
        }

        public Rect GetScrolledVisibleRect(
            Rect scrollRect,
            Rect localRect,
            Vector2 scroll)
        {
            Rect scrolledRect = new Rect(
                scrollRect.x + localRect.x - scroll.x,
                scrollRect.y + localRect.y - scroll.y,
                localRect.width,
                localRect.height);
            return Intersect(scrolledRect, scrollRect);
        }

        internal bool TryGet(string targetId, out Rect rect)
        {
            if (!string.IsNullOrEmpty(targetId))
            {
                return this.targets.TryGetValue(targetId, out rect);
            }

            rect = Rect.zero;
            return false;
        }

        private static Rect Intersect(Rect rect, Rect bounds)
        {
            return ShuttleTutorialRectUtility.Intersect(rect, bounds);
        }

        private static bool IsValid(Rect rect)
        {
            return rect.width > 1f && rect.height > 1f;
        }
    }
}

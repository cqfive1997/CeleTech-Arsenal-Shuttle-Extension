using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell
{
    internal interface IShuttleTutorialTargetService
    {
        void Register(string targetId, Rect rect);

        void RegisterClipped(string targetId, Rect rect, Rect visibleBounds);

        Rect GetScrolledVisibleRect(Rect scrollRect, Rect localRect, Vector2 scroll);
    }
}

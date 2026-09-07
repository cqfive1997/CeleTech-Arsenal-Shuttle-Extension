using System;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell
{
    internal interface IShuttleControlBootAnimation
    {
        bool Finished { get; }

        void PreOpen();

        bool ShouldBypassOpeningAnimation();

        void EnsureStartedAndUpdate();

        void FinishAndNotifyCompletedIfNeeded();

        void NotifyCompletedIfNeeded();

        bool HandleOpenAnimationInput();

        void DrawOpeningAnimatedContents(
            Rect inRect,
            Action<Rect, float> drawCurrentPageContentsClipped);
    }
}

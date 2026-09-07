using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Icons;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Boot
{
    internal sealed class ShuttleControlBootAnimationService :
        IShuttleControlBootAnimation
    {
        private readonly ShuttleControlBootAnimationController bootAnimationController;
        private readonly ShuttleControlIconRegistry iconRegistry;

        internal ShuttleControlBootAnimationService(
            IShuttleControlBootAnimationPort bootAnimationPort)
            : this(bootAnimationPort, new ShuttleControlIconRegistry())
        {
        }

        internal ShuttleControlBootAnimationService(
            IShuttleControlBootAnimationPort bootAnimationPort,
            ShuttleControlIconRegistry iconRegistry)
        {
            this.bootAnimationController =
                new ShuttleControlBootAnimationController(bootAnimationPort);
            this.iconRegistry = iconRegistry;
        }

        public bool Finished
        {
            get { return this.bootAnimationController.Finished; }
        }

        public void PreOpen()
        {
            this.bootAnimationController.PreOpen();
        }

        public bool ShouldBypassOpeningAnimation()
        {
            return this.bootAnimationController.ShouldBypassOpeningAnimation();
        }

        public void EnsureStartedAndUpdate()
        {
            this.bootAnimationController.EnsureStartedAndUpdate();
        }

        public void FinishAndNotifyCompletedIfNeeded()
        {
            this.bootAnimationController.FinishAndNotifyCompletedIfNeeded();
        }

        public void NotifyCompletedIfNeeded()
        {
            this.bootAnimationController.NotifyCompletedIfNeeded();
        }

        public bool HandleOpenAnimationInput()
        {
            return this.bootAnimationController.HandleOpenAnimationInput();
        }

        public void DrawOpeningAnimatedContents(
            Rect inRect,
            Action<Rect, float> drawCurrentPageContentsClipped)
        {
            this.bootAnimationController.DrawOpeningAnimatedContents(
                inRect,
                this.iconRegistry,
                drawCurrentPageContentsClipped);
        }
    }
}

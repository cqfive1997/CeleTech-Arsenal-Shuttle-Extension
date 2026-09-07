using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Icons;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Boot
{
    internal sealed class ShuttleControlBootAnimationController
    {
        private readonly IShuttleControlBootAnimationPort bootAnimationPort;
        private readonly ShuttleControlOpenAnimationState openAnimation =
            new ShuttleControlOpenAnimationState();

        private bool firstBootAnimationCompletionNotified;
        private bool systemUpdateAnimationCompletionNotified;
        private List<string> systemUpdateStepKeys;

        internal ShuttleControlBootAnimationController(
            IShuttleControlBootAnimationPort bootAnimationPort)
        {
            this.bootAnimationPort = bootAnimationPort;
        }

        internal bool Finished
        {
            get { return this.openAnimation.Finished; }
        }

        internal void PreOpen()
        {
            this.firstBootAnimationCompletionNotified = false;
            this.systemUpdateAnimationCompletionNotified = false;
            this.systemUpdateStepKeys = null;

            if (this.bootAnimationPort != null &&
                this.bootAnimationPort.HasPendingControlPanelSystemUpdateAnimation)
            {
                this.systemUpdateStepKeys =
                    ShuttleControlSystemUpdateUtility.SanitizeStepKeys(
                        this.bootAnimationPort.PendingSystemUpdateStepKeys);
                this.openAnimation.Restart(ShuttleControlBootAnimationMode.SystemUpdate);
                return;
            }

            this.TryScheduleSystemUpdateForNextOpen();
            this.openAnimation.Restart(this.GetOpenAnimationMode());
        }

        internal bool ShouldBypassOpeningAnimation()
        {
            ShuttleEffectiveSettings effective = CeleTechShuttleMod.EffectiveSettings;
            if (effective == null)
            {
                return false;
            }

            if (!effective.UseWindowAnimations)
            {
                return true;
            }

            return this.openAnimation.Mode == ShuttleControlBootAnimationMode.SystemUpdate &&
                !effective.UseSystemUpdateAnimations;
        }

        internal void EnsureStartedAndUpdate()
        {
            this.openAnimation.EnsureStarted();
            this.openAnimation.Update();
        }

        internal void FinishAndNotifyCompletedIfNeeded()
        {
            this.openAnimation.Finish();
            this.NotifyOpenAnimationCompletedIfNeeded();
        }

        internal void NotifyCompletedIfNeeded()
        {
            this.NotifyOpenAnimationCompletedIfNeeded();
        }

        internal bool HandleOpenAnimationInput()
        {
            Event current = Event.current;
            if (current == null)
            {
                return false;
            }

            if (current.type == EventType.MouseDown)
            {
                this.openAnimation.Finish();
                this.NotifyOpenAnimationCompletedIfNeeded();
                current.Use();
                return true;
            }

            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            {
                this.openAnimation.Finish();
                this.NotifyOpenAnimationCompletedIfNeeded();
                current.Use();
                return true;
            }

            if (current.isMouse || current.isKey)
            {
                current.Use();
            }

            return false;
        }

        internal void DrawOpeningAnimatedContents(
            Rect inRect,
            ShuttleControlIconRegistry iconRegistry,
            Action<Rect, float> drawCurrentPageContentsClipped)
        {
            if (this.openAnimation.Mode == ShuttleControlBootAnimationMode.SystemUpdate)
            {
                this.DrawSystemUpdateAnimatedContents(inRect, iconRegistry, drawCurrentPageContentsClipped);
                return;
            }

            if (this.openAnimation.Mode == ShuttleControlBootAnimationMode.FirstBootLong)
            {
                this.DrawFirstBootAnimatedContents(inRect, iconRegistry, drawCurrentPageContentsClipped);
                return;
            }

            float elapsed = this.openAnimation.Elapsed;
            float UIAlpha = ShuttleControlAnimationUtility.GetUIAlpha(elapsed);
            if (UIAlpha > 0.001f)
            {
                float offsetY = Mathf.Lerp(ShuttleControlAnimationUtility.UIStartOffsetY, 0f, UIAlpha);
                drawCurrentPageContentsClipped(inRect, offsetY);
            }

            ShuttleControlAnimationUtility.DrawOpeningBackdrop(inRect, elapsed);
            ShuttleControlAnimationUtility.DrawCenteredLogo(inRect, iconRegistry.GetIcon("logo"), elapsed);
        }

        private void DrawFirstBootAnimatedContents(
            Rect inRect,
            ShuttleControlIconRegistry iconRegistry,
            Action<Rect, float> drawCurrentPageContentsClipped)
        {
            float elapsed = this.openAnimation.Elapsed;
            float UIAlpha = ShuttleControlAnimationUtility.GetFirstBootUIAlpha(elapsed);
            if (UIAlpha > 0.001f)
            {
                float offsetY = Mathf.Lerp(ShuttleControlAnimationUtility.UIStartOffsetY, 0f, UIAlpha);
                drawCurrentPageContentsClipped(inRect, offsetY);
            }

            Texture2D logo = iconRegistry.GetIcon("logo");
            ShuttleControlAnimationUtility.DrawFirstBootBackdrop(inRect, elapsed);
            ShuttleControlAnimationUtility.DrawFirstBootSelfCheck(inRect, logo, elapsed);
        }

        private void DrawSystemUpdateAnimatedContents(
            Rect inRect,
            ShuttleControlIconRegistry iconRegistry,
            Action<Rect, float> drawCurrentPageContentsClipped)
        {
            float elapsed = this.openAnimation.Elapsed;
            float UIAlpha = ShuttleControlAnimationUtility.GetSystemUpdateUIAlpha(elapsed);
            if (UIAlpha > 0.001f)
            {
                float offsetY = Mathf.Lerp(ShuttleControlAnimationUtility.UIStartOffsetY, 0f, UIAlpha);
                drawCurrentPageContentsClipped(inRect, offsetY);
            }

            Texture2D logo = iconRegistry.GetIcon("logo");
            ShuttleControlAnimationUtility.DrawSystemUpdateBackdrop(inRect, elapsed);
            ShuttleControlAnimationUtility.DrawSystemUpdateSelfCheck(
                inRect,
                logo,
                elapsed,
                this.systemUpdateStepKeys);
        }

        private void TryScheduleSystemUpdateForNextOpen()
        {
            if (this.bootAnimationPort == null)
            {
                return;
            }

            string currentVersion = ShuttleModVersionUtility.ResolveCurrentModVersion();
            if (this.bootAnimationPort.ConsumeForcedControlPanelSystemUpdateScheduleForDev())
            {
                this.systemUpdateStepKeys = ShuttleControlSystemUpdateUtility.BuildRandomStepKeys();
                this.bootAnimationPort.ScheduleControlPanelSystemUpdateAnimationForVersion(
                    this.systemUpdateStepKeys,
                    ShuttleModVersionUtility.IsKnownVersion(currentVersion) ? currentVersion : null);
                return;
            }

            if (!ShuttleModVersionUtility.IsKnownVersion(currentVersion))
            {
                return;
            }

            string lastShownVersion = this.bootAnimationPort.LastShownControlPanelSystemUpdateVersion;
            if (string.IsNullOrEmpty(lastShownVersion))
            {
                this.bootAnimationPort.RecordControlPanelSystemUpdateVersion(currentVersion);
                return;
            }

            if (string.Equals(lastShownVersion, currentVersion, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    this.bootAnimationPort.PendingControlPanelSystemUpdateVersion,
                    currentVersion,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            this.systemUpdateStepKeys = ShuttleControlSystemUpdateUtility.BuildRandomStepKeys();
            this.bootAnimationPort.ScheduleControlPanelSystemUpdateAnimationForVersion(
                this.systemUpdateStepKeys,
                currentVersion);
        }

        private ShuttleControlBootAnimationMode GetOpenAnimationMode()
        {
            if (this.bootAnimationPort != null &&
                !this.bootAnimationPort.HasShownControlPanelFirstBootAnimation)
            {
                return ShuttleControlBootAnimationMode.FirstBootLong;
            }

            return ShuttleControlBootAnimationMode.NormalShort;
        }

        private void NotifyFirstBootAnimationCompletedIfNeeded()
        {
            if (this.firstBootAnimationCompletionNotified ||
                this.openAnimation.Mode != ShuttleControlBootAnimationMode.FirstBootLong)
            {
                return;
            }

            this.firstBootAnimationCompletionNotified = true;
            if (this.bootAnimationPort != null)
            {
                this.bootAnimationPort.MarkControlPanelFirstBootAnimationShown();
            }
        }

        private void NotifySystemUpdateAnimationCompletedIfNeeded()
        {
            if (this.systemUpdateAnimationCompletionNotified ||
                this.openAnimation.Mode != ShuttleControlBootAnimationMode.SystemUpdate)
            {
                return;
            }

            this.systemUpdateAnimationCompletionNotified = true;
            this.systemUpdateStepKeys = null;
            if (this.bootAnimationPort != null)
            {
                this.bootAnimationPort.MarkControlPanelSystemUpdateAnimationShown();
            }
        }

        private void NotifyOpenAnimationCompletedIfNeeded()
        {
            this.NotifyFirstBootAnimationCompletedIfNeeded();
            this.NotifySystemUpdateAnimationCompletedIfNeeded();
        }
    }
}

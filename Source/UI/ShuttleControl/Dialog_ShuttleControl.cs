using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Composition;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.State;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl
{
    /// <summary>
    /// Shuttle control shell entry. The control dialog accepts canonical page identity.
    /// </summary>
    internal sealed class Dialog_ShuttleControl : Window
    {
        private readonly ShuttleControlDialogComposition composition;
        private readonly Thing hostThing;
        private bool childModalOpen;

        internal Dialog_ShuttleControl(
            IShuttleControlReadPort controlReadPort,
            IShuttleCargoReadPort cargoReadPort,
            IShuttleLoadCargoReadPort loadCargoReadPort,
            IShuttleCommandExecutor commandExecutor,
            IShuttleWeaponBayReadPort weaponBayReadPort,
            IShuttleAutoWorkTableReadPort autoWorkTableReadPort,
            IShuttleControlBootAnimationPort bootAnimationPort)
            : this(
                controlReadPort,
                cargoReadPort,
                loadCargoReadPort,
                commandExecutor,
                weaponBayReadPort,
                autoWorkTableReadPort,
                bootAnimationPort,
                null,
                ShuttleControlPageId.Main)
        {
        }

        internal Dialog_ShuttleControl(
            IShuttleControlReadPort controlReadPort,
            IShuttleCargoReadPort cargoReadPort,
            IShuttleLoadCargoReadPort loadCargoReadPort,
            IShuttleCommandExecutor commandExecutor,
            IShuttleWeaponBayReadPort weaponBayReadPort,
            IShuttleAutoWorkTableReadPort autoWorkTableReadPort,
            IShuttleControlBootAnimationPort bootAnimationPort,
            Thing host,
            ShuttleControlPageId initialPage)
        {
            this.hostThing = host;
            this.composition = new ShuttleControlDialogComposition(
                new ShuttleControlState(initialPage),
                controlReadPort,
                cargoReadPort,
                loadCargoReadPort,
                commandExecutor,
                weaponBayReadPort,
                autoWorkTableReadPort,
                bootAnimationPort,
                this.GetHostRotation,
                delegate(bool value)
                {
                    this.childModalOpen = value;
                },
                delegate
                {
                    this.Close(false);
                });
            this.forcePause = false;
            this.doCloseX = true;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = false;
            this.draggable = true;
            this.resizeable = true;
            this.TrySwitchToPage(initialPage);
            this.forcePause = this.composition.ShouldPauseSimulation;
        }

        internal bool IsForHost(Thing candidateHost)
        {
            return candidateHost != null && object.ReferenceEquals(this.hostThing, candidateHost);
        }

        internal bool TrySwitchToPage(ShuttleControlPageId page)
        {
            if (!System.Enum.IsDefined(typeof(ShuttleControlPageId), page))
            {
                return false;
            }

            if (this.composition.State.CurrentPage == page)
            {
                return true;
            }

            return this.composition.TrySwitchToPage(page);
        }

        private Rot4 GetHostRotation()
        {
            return this.hostThing != null ? this.hostThing.Rotation : Rot4.East;
        }

        public override Vector2 InitialSize
        {
            get
            {
                const float targetWidth = 1200f;
                const float targetHeight = 820f;
                const float minWidth = 1000f;
                const float minHeight = 720f;
                const float screenMargin = 80f;
                float safeWidth = Mathf.Max(640f, Verse.UI.screenWidth - screenMargin);
                float safeHeight = Mathf.Max(520f, Verse.UI.screenHeight - screenMargin);
                float width = Mathf.Min(targetWidth, safeWidth);
                float height = Mathf.Min(targetHeight, safeHeight);
                if (safeWidth >= minWidth)
                {
                    width = Mathf.Max(minWidth, width);
                }

                if (safeHeight >= minHeight)
                {
                    height = Mathf.Max(minHeight, height);
                }

                return new Vector2(width, height);
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            this.composition.BootAnimation.PreOpen();
        }

        public override void PostClose()
        {
            this.composition.Host.ReleasePageCache();
            this.composition.Dispose();
            base.PostClose();
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (this.childModalOpen)
            {
                this.DrawSuspendedBackdrop(inRect);
                return;
            }

            IShuttleControlBootAnimation bootAnimation = this.composition.BootAnimation;
            if (bootAnimation.ShouldBypassOpeningAnimation())
            {
                bootAnimation.FinishAndNotifyCompletedIfNeeded();
                this.DrawCurrentPageContents(inRect);
                return;
            }

            bootAnimation.EnsureStartedAndUpdate();

            if (bootAnimation.Finished)
            {
                bootAnimation.NotifyCompletedIfNeeded();
                this.DrawCurrentPageContents(inRect);
                return;
            }

            if (bootAnimation.HandleOpenAnimationInput())
            {
                this.DrawCurrentPageContents(inRect);
                return;
            }

            bootAnimation.DrawOpeningAnimatedContents(
                inRect,
                this.DrawCurrentPageContentsClipped);
        }

        private void DrawSuspendedBackdrop(Rect inRect)
        {
            ShuttleUILayout.DrawPanelBackground(inRect);
            Rect messageRect = new Rect(inRect.x + 18f, inRect.y + 18f, inRect.width - 36f, 28f);
            Color oldColor = GUI.color;
            try
            {
                GUI.color = ShuttleUIStyle.MutedTextColor;
                ShuttleUILayout.SafeLabel(
                    messageRect,
                    "CT_Shuttle_UI_LoadCargoWindowActive".Translate().ToString());
            }
            finally
            {
                GUI.color = oldColor;
            }
        }

        private void DrawCurrentPageContentsClipped(Rect inRect, float offsetY)
        {
            GUI.BeginGroup(inRect);
            try
            {
                this.DrawCurrentPageContentsCore(
                    new Rect(0f, offsetY, inRect.width, inRect.height),
                    false);
            }
            finally
            {
                GUI.EndGroup();
            }
        }

        private void DrawCurrentPageContents(Rect inRect)
        {
            this.DrawCurrentPageContentsCore(inRect, true);
        }

        private void DrawCurrentPageContentsCore(Rect inRect, bool allowTutorial)
        {
            this.composition.CheckResearchInvalidation();
            ShuttleControlPageId requestedPage = this.composition.State.CurrentPage;
            ShuttleControlReadModel controlModel =
                this.composition.ReadModelSource.GetControlModelForUI(requestedPage);
            ShuttleControlPageId currentPage =
                this.composition.EnsureCurrentPageAvailableAndSync(controlModel);

            if (!allowTutorial)
            {
                this.composition.Host.Draw(inRect, controlModel);
                return;
            }

            this.composition.DrawWithTutorial(
                inRect,
                currentPage,
                controlModel,
                delegate
                {
                    this.composition.Host.Draw(inRect, controlModel);
                });
            this.forcePause = this.composition.ShouldPauseSimulation;
        }
    }
}

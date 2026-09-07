using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttleDialogTutorialController
    {
        private readonly ShuttleTutorialDefinition definition;
        private readonly string progressKey;
        private readonly ShuttleControlTutorialTargetService targets =
            new ShuttleControlTutorialTargetService();
        private readonly ShuttleTutorialOverlayDrawer overlay =
            new ShuttleTutorialOverlayDrawer();
        private readonly ShuttleTutorialSession session;

        internal ShuttleDialogTutorialController(
            ShuttleTutorialDefinition definition,
            string progressKey)
        {
            this.definition = definition;
            this.progressKey = progressKey;
            this.session = new ShuttleTutorialSession(
                new FixedDefinitionProvider(definition),
                progressKey);
        }

        internal bool ShouldPauseSimulation
        {
            get
            {
                if (this.session.IsActive)
                {
                    return true;
                }

                return this.HasUsableDefinition &&
                    ShuttleTutorialProgress.TutorialsEnabled &&
                    ShuttleTutorialProgress.AutoStartEnabled &&
                    !ShuttleTutorialProgress.HasCompletedPageKey(this.progressKey);
            }
        }

        internal void BeginFrame(Rect windowRect)
        {
            if (!this.HasUsableDefinition)
            {
                return;
            }

            this.session.EnsureStarted(this.definition.Page);
            this.overlay.HandleInputBeforePageDraw(
                this.session,
                this.definition.Page);
            this.targets.BeginFrame(this.definition.Page, windowRect);
        }

        internal void EndFrame(Rect windowRect)
        {
            if (!this.HasUsableDefinition)
            {
                return;
            }

            this.session.UpdateAfterPageDraw(
                this.definition.Page,
                this.targets);
            this.overlay.Draw(windowRect, this.session, this.targets);
        }

        internal void Register(string targetId, Rect rect)
        {
            if (this.HasUsableDefinition)
            {
                this.targets.Register(targetId, rect);
            }
        }

        private bool HasUsableDefinition
        {
            get
            {
                return this.definition != null &&
                    this.definition.Steps != null &&
                    this.definition.Steps.Count > 0 &&
                    !string.IsNullOrEmpty(this.progressKey);
            }
        }

        private sealed class FixedDefinitionProvider :
            IShuttleTutorialDefinitionProvider
        {
            private readonly ShuttleTutorialDefinition definition;

            internal FixedDefinitionProvider(ShuttleTutorialDefinition definition)
            {
                this.definition = definition;
            }

            public ShuttleTutorialDefinition GetDefinition(
                ShuttleControlPageId page)
            {
                return this.definition != null && this.definition.Page == page
                    ? this.definition
                    : null;
            }

            public ShuttleTutorialDefinition GetContextualDefinition(
                ShuttleControlPageId page,
                ShuttleTutorialEvent tutorialEvent)
            {
                return null;
            }
        }
    }
}

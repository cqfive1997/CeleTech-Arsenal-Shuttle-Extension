using System.Collections.Generic;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttleTutorialSession
    {
        private const int MissingTargetSkipFrames = 12;
        private const int MaxPendingContextEvents = 16;

        private readonly IShuttleTutorialDefinitionProvider definitions;
        private readonly string pageProgressKey;
        private readonly Queue<ShuttleTutorialEvent> pendingContextEvents =
            new Queue<ShuttleTutorialEvent>();
        private readonly HashSet<string> queuedOrSeenContextEventIDs =
            new HashSet<string>();
        private readonly HashSet<string> queuedContextKeys =
            new HashSet<string>();

        private ShuttleTutorialDefinition currentDefinition;
        private ShuttleTutorialEvent currentContextEvent;
        private string currentContextKey;
        private int currentStepIndex = -1;
        private int missingTargetFrames;
        private float currentStepStartTime;
        private bool currentDefinitionIsContextual;
        private int observedResetVersion;

        internal ShuttleTutorialSession(
            IShuttleTutorialDefinitionProvider definitions)
            : this(definitions, null)
        {
        }

        internal ShuttleTutorialSession(
            IShuttleTutorialDefinitionProvider definitions,
            string pageProgressKey)
        {
            this.definitions = definitions ?? new ShuttleTutorialEmptyDefinitionProvider();
            this.pageProgressKey = pageProgressKey;
            this.observedResetVersion = ShuttleTutorialProgress.ResetVersion;
        }

        internal bool IsActive
        {
            get { return this.CurrentStep != null; }
        }

        internal ShuttleControlPageId CurrentPage
        {
            get
            {
                return this.currentDefinition != null
                    ? this.currentDefinition.Page
                    : ShuttleControlPageId.Main;
            }
        }

        internal ShuttleTutorialStep CurrentStep
        {
            get
            {
                if (this.currentDefinition == null ||
                    this.currentDefinition.Steps == null ||
                    this.currentStepIndex < 0 ||
                    this.currentStepIndex >= this.currentDefinition.Steps.Count)
                {
                    return null;
                }

                return this.currentDefinition.Steps[this.currentStepIndex];
            }
        }

        internal ShuttleTutorialEvent CurrentContextEvent
        {
            get { return this.currentContextEvent; }
        }

        internal bool CanAdvanceCurrentStep
        {
            get
            {
                ShuttleTutorialStep step = this.CurrentStep;
                if (step == null || step.MinHoldSeconds <= 0f)
                {
                    return true;
                }

                return Time.realtimeSinceStartup - this.currentStepStartTime >=
                    step.MinHoldSeconds;
            }
        }

        internal void UpdateAfterPageDraw(
            ShuttleControlPageId page,
            ShuttleControlTutorialTargetService registry)
        {
            this.EnsureStarted(page);
            this.UpdateMissingTargetState(registry);
        }

        internal void EnsureStarted(ShuttleControlPageId page)
        {
            this.SyncProgressResetVersion();

            if (!ShuttleTutorialProgress.TutorialsEnabled)
            {
                this.ClearPendingContextEvents();
                this.CancelWithoutCompleting();
                return;
            }

            if (this.currentDefinition != null && this.currentDefinition.Page != page)
            {
                this.CancelWithoutCompleting();
            }

            if (this.currentDefinition == null)
            {
                this.TryStartContextualTutorial(page);
                if (this.currentDefinition == null &&
                    ShuttleTutorialProgress.AutoStartEnabled &&
                    !ShuttleTutorialProgress.HasCompletedPageKey(
                        this.GetPageProgressKey(page)))
                {
                    this.TryStartPage(page);
                }
            }
        }

        internal void EnqueueContextualEvent(ShuttleTutorialEvent tutorialEvent)
        {
            if (tutorialEvent == null ||
                !ShuttleTutorialProgress.TutorialsEnabled)
            {
                return;
            }

            if (!string.IsNullOrEmpty(tutorialEvent.EventID))
            {
                if (this.queuedOrSeenContextEventIDs.Contains(tutorialEvent.EventID))
                {
                    return;
                }

                this.queuedOrSeenContextEventIDs.Add(tutorialEvent.EventID);
            }

            string contextKey =
                ShuttleTutorialContextKeyUtility.GetSegmentInstalledContextKey(
                    tutorialEvent);
            if (!string.IsNullOrEmpty(contextKey))
            {
                if (ShuttleTutorialProgress.HasCompletedContext(contextKey) ||
                    contextKey == this.currentContextKey ||
                    this.queuedContextKeys.Contains(contextKey))
                {
                    return;
                }

                this.queuedContextKeys.Add(contextKey);
            }

            this.pendingContextEvents.Enqueue(tutorialEvent);
            while (this.pendingContextEvents.Count > MaxPendingContextEvents)
            {
                ShuttleTutorialEvent droppedEvent =
                    this.pendingContextEvents.Dequeue();
                this.RemoveQueuedContextKey(droppedEvent);
            }
        }

        internal void Advance()
        {
            if (this.currentDefinition == null)
            {
                return;
            }

            this.currentStepIndex++;
            this.missingTargetFrames = 0;
            this.currentStepStartTime = Time.realtimeSinceStartup;
            if (this.currentDefinition.Steps == null ||
                this.currentStepIndex >= this.currentDefinition.Steps.Count)
            {
                this.CompleteCurrentTutorial();
            }
        }

        internal void SkipCurrentPage()
        {
            this.CompleteCurrentTutorial();
        }

        internal void CancelWithoutCompleting()
        {
            this.currentDefinition = null;
            this.currentContextEvent = null;
            this.currentContextKey = null;
            this.currentStepIndex = -1;
            this.missingTargetFrames = 0;
            this.currentStepStartTime = 0f;
            this.currentDefinitionIsContextual = false;
        }

        private void TryStartPage(ShuttleControlPageId page)
        {
            this.CancelWithoutCompleting();
            ShuttleTutorialDefinition definition = this.definitions.GetDefinition(page);
            if (definition == null ||
                definition.Steps == null ||
                definition.Steps.Count <= 0)
            {
                return;
            }

            this.currentDefinition = definition;
            this.currentStepIndex = 0;
            this.currentStepStartTime = Time.realtimeSinceStartup;
        }

        private void TryStartContextualTutorial(ShuttleControlPageId page)
        {
            if (page != ShuttleControlPageId.Main)
            {
                return;
            }

            int guard = 0;
            while (this.pendingContextEvents.Count > 0 && guard < 8)
            {
                guard++;
                ShuttleTutorialEvent tutorialEvent = this.pendingContextEvents.Dequeue();
                this.RemoveQueuedContextKey(tutorialEvent);
                string contextKey =
                    ShuttleTutorialContextKeyUtility.GetSegmentInstalledContextKey(
                        tutorialEvent);
                if (!string.IsNullOrEmpty(contextKey) &&
                    ShuttleTutorialProgress.HasCompletedContext(contextKey))
                {
                    continue;
                }

                ShuttleTutorialDefinition definition =
                    this.definitions.GetContextualDefinition(page, tutorialEvent);
                if (definition == null ||
                    definition.Steps == null ||
                    definition.Steps.Count <= 0)
                {
                    continue;
                }

                this.currentDefinition = definition;
                this.currentContextEvent = tutorialEvent;
                this.currentContextKey = contextKey;
                this.currentDefinitionIsContextual = true;
                this.currentStepIndex = 0;
                this.currentStepStartTime = Time.realtimeSinceStartup;
                this.missingTargetFrames = 0;
                return;
            }
        }

        private void UpdateMissingTargetState(
            ShuttleControlTutorialTargetService registry)
        {
            ShuttleTutorialStep step = this.CurrentStep;
            RectProbeResult result = this.TryResolveCurrentTarget(registry, step);
            if (result == RectProbeResult.Found)
            {
                this.missingTargetFrames = 0;
                return;
            }

            if (result == RectProbeResult.NotApplicable)
            {
                return;
            }

            Event current = Event.current;
            if (current == null || current.type != EventType.Repaint)
            {
                return;
            }

            if (step != null && step.SkipIfTargetMissing)
            {
                this.Advance();
                return;
            }

            this.missingTargetFrames++;
            if (this.missingTargetFrames >= MissingTargetSkipFrames)
            {
                this.Advance();
            }
        }

        private RectProbeResult TryResolveCurrentTarget(
            ShuttleControlTutorialTargetService registry,
            ShuttleTutorialStep step)
        {
            if (registry == null || step == null || !step.HasTarget)
            {
                return RectProbeResult.NotApplicable;
            }

            Rect rect;
            return ShuttleTutorialStepTargetResolver.TryResolve(step, registry, out rect)
                ? RectProbeResult.Found
                : RectProbeResult.Missing;
        }

        private void CompleteCurrentTutorial()
        {
            if (this.currentDefinition == null)
            {
                this.CancelWithoutCompleting();
                return;
            }

            if (this.currentDefinitionIsContextual)
            {
                ShuttleTutorialProgress.MarkContextCompleted(this.currentContextKey);
            }
            else
            {
                ShuttleTutorialProgress.MarkPageCompletedKey(
                    this.GetPageProgressKey(this.currentDefinition.Page));
            }

            this.CancelWithoutCompleting();
        }

        private void ClearPendingContextEvents()
        {
            this.pendingContextEvents.Clear();
            this.queuedContextKeys.Clear();
        }

        private void SyncProgressResetVersion()
        {
            if (this.observedResetVersion == ShuttleTutorialProgress.ResetVersion)
            {
                return;
            }

            this.observedResetVersion = ShuttleTutorialProgress.ResetVersion;
            this.ClearPendingContextEvents();
            this.queuedOrSeenContextEventIDs.Clear();
            this.CancelWithoutCompleting();
        }

        private void RemoveQueuedContextKey(ShuttleTutorialEvent tutorialEvent)
        {
            string contextKey =
                ShuttleTutorialContextKeyUtility.GetSegmentInstalledContextKey(
                    tutorialEvent);
            if (!string.IsNullOrEmpty(contextKey))
            {
                this.queuedContextKeys.Remove(contextKey);
            }
        }

        private string GetPageProgressKey(ShuttleControlPageId page)
        {
            return string.IsNullOrEmpty(this.pageProgressKey)
                ? ShuttleTutorialProgress.GetPageKey(page)
                : this.pageProgressKey;
        }

        private enum RectProbeResult
        {
            NotApplicable,
            Missing,
            Found
        }
    }
}

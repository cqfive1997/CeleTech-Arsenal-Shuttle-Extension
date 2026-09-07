using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Onboarding;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Onboarding
{
    public sealed class ShuttleStarterPresetGameComponent : GameComponent
    {
        private const int CurrentSchemaVersion = 2;
        private const float PresentationDelaySeconds = 0.8f;

        private readonly Game game;
        private int schemaVersion;
        private bool selectionResolved;
        private bool promptPending;
        private ShuttleStarterPresetMode selectedMode = ShuttleStarterPresetMode.FromScratch;

        private bool dialogOpen;
        private float earliestPresentationRealtime;

        public ShuttleStarterPresetGameComponent(Game game)
        {
            this.game = game;
        }

        internal bool SelectionResolved
        {
            get { return this.selectionResolved; }
        }

        internal ShuttleStarterPresetMode SelectedMode
        {
            get { return this.selectedMode; }
        }

        internal static ShuttleStarterPresetGameComponent CurrentComponent
        {
            get
            {
                return Current.Game != null
                    ? Current.Game.GetComponent<ShuttleStarterPresetGameComponent>()
                    : null;
            }
        }

        public override void StartedNewGame()
        {
            this.schemaVersion = CurrentSchemaVersion;
            this.selectedMode = ShuttleStarterPresetMode.FromScratch;
            this.dialogOpen = false;
            this.earliestPresentationRealtime =
                Time.realtimeSinceStartup + PresentationDelaySeconds;

            this.promptPending = true;
            this.selectionResolved = false;
            ShuttleStarterResearchTreePolicy.Apply(
                this.selectedMode,
                this.selectionResolved);
            ShuttleStarterPresetBuildPolicy.ResetVanillaBuildCostCache();
        }

        public override void LoadedGame()
        {
            this.dialogOpen = false;
            this.earliestPresentationRealtime = Time.realtimeSinceStartup + 0.2f;

            if (this.schemaVersion <= 0)
            {
                // Missing schema means this is an established save created before the feature.
                // Resolve silently so an update never injects a delayed new-game prompt.
                this.schemaVersion = CurrentSchemaVersion;
                this.selectedMode = ShuttleStarterPresetMode.FromScratch;
                this.selectionResolved = true;
                this.promptPending = false;
            }
            else if (this.schemaVersion < CurrentSchemaVersion)
            {
                // Version 1 used per-host queue automation records. Selection remains valid,
                // while those obsolete labels are simply no longer read.
                this.schemaVersion = CurrentSchemaVersion;
                if (this.selectionResolved)
                {
                    this.promptPending = false;
                }
            }

            ShuttleStarterResearchTreePolicy.Apply(
                this.selectedMode,
                this.selectionResolved);
            ShuttleStarterPresetBuildPolicy.ResetVanillaBuildCostCache();
        }

        public override void GameComponentUpdate()
        {
            if (!this.promptPending || this.selectionResolved || this.dialogOpen)
            {
                return;
            }

            if (Time.realtimeSinceStartup < this.earliestPresentationRealtime ||
                Current.ProgramState != ProgramState.Playing ||
                Current.Game != this.game ||
                Find.CurrentMap == null ||
                Find.WindowStack == null ||
                LongEventHandler.AnyEventNowOrWaiting ||
                this.HasOtherDialogOpen())
            {
                return;
            }

            this.dialogOpen = true;
            Find.WindowStack.Add(new Dialog_ShuttleStarterPreset(this));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.schemaVersion, "starterPresetSchemaVersion", 0);
            Scribe_Values.Look(
                ref this.selectionResolved,
                "starterPresetSelectionResolved",
                false);
            Scribe_Values.Look(
                ref this.promptPending,
                "starterPresetPromptPending",
                false);
            Scribe_Values.Look(
                ref this.selectedMode,
                "starterPresetSelectedMode",
                ShuttleStarterPresetMode.FromScratch);

            if (Scribe.mode == LoadSaveMode.PostLoadInit &&
                !Enum.IsDefined(typeof(ShuttleStarterPresetMode), this.selectedMode))
            {
                this.selectedMode = ShuttleStarterPresetMode.FromScratch;
            }
        }

        internal void ResolveSelection(ShuttleStarterPresetMode mode)
        {
            if (!Enum.IsDefined(typeof(ShuttleStarterPresetMode), mode))
            {
                mode = ShuttleStarterPresetMode.FromScratch;
            }

            this.schemaVersion = CurrentSchemaVersion;
            this.selectedMode = mode;
            this.selectionResolved = true;
            this.promptPending = false;
            ShuttleStarterResearchTreePolicy.Apply(
                this.selectedMode,
                this.selectionResolved);
            ShuttleStarterPresetBuildPolicy.ResetVanillaBuildCostCache();
            if (mode == ShuttleStarterPresetMode.BasicFlightAndCargo)
            {
                ShuttleStarterPresetSpawnedHostApplicator.ApplyToSpawnedHosts();
            }
        }

        internal bool TrySetModeFromSettings(
            ShuttleStarterPresetMode mode,
            out string failureReason)
        {
            failureReason = null;
            if (!this.selectionResolved)
            {
                failureReason = "CT_Shuttle_StarterPreset_ModeChangeSelectionPending"
                    .Translate()
                    .ToString();
                return false;
            }

            if (!Enum.IsDefined(typeof(ShuttleStarterPresetMode), mode))
            {
                mode = ShuttleStarterPresetMode.FromScratch;
            }

            if (this.selectedMode == mode)
            {
                ShuttleStarterResearchTreePolicy.Apply(
                    this.selectedMode,
                    this.selectionResolved);
                return true;
            }

            if (ShuttleHostConstructionProjectGuard.HasPendingHostConstruction())
            {
                failureReason = "CT_Shuttle_StarterPreset_ModeChangeBlockedConstruction"
                    .Translate()
                    .ToString();
                return false;
            }

            this.schemaVersion = CurrentSchemaVersion;
            this.selectedMode = mode;
            this.selectionResolved = true;
            this.promptPending = false;
            ShuttleStarterResearchTreePolicy.Apply(
                this.selectedMode,
                this.selectionResolved);
            ShuttleStarterPresetBuildPolicy.ResetVanillaBuildCostCache();

            // A settings change controls future construction only. In particular,
            // do not retrofit already-spawned empty hosts with the basic package.
            return true;
        }

        internal void NotifyDialogClosed()
        {
            this.dialogOpen = false;
            if (!this.selectionResolved)
            {
                // Defensive fallback for external window-stack removal. Never loop the prompt.
                this.ResolveSelection(ShuttleStarterPresetMode.FromScratch);
            }
        }

        private bool HasOtherDialogOpen()
        {
            IList<Window> windows = Find.WindowStack.Windows;
            for (int i = 0; windows != null && i < windows.Count; i++)
            {
                Window window = windows[i];
                if (window != null &&
                    window.layer == WindowLayer.Dialog &&
                    !(window is Dialog_ShuttleStarterPreset))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

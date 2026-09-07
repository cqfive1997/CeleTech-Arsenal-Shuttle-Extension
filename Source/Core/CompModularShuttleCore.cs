using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.Paint;
using RimWorld;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    /// <summary>
    /// Thin host-comp properties for the modular shuttle core seam.
    /// Only the default assembly layout hook is exposed at this stage.
    /// </summary>
    public sealed class CompProperties_ModularShuttleCore : CompProperties
    {
        // Optional default layout applied on first creation only.
        public string defaultAssemblyLayoutDefName;

        // Initial fill percentage for the internal flight battery when runtime charge is first created.
        public float initialStoredEnergyPercent = 1f;

        public CompProperties_ModularShuttleCore()
        {
            this.compClass = typeof(CompModularShuttleCore);
        }
    }

    /// <summary>
    /// Thin host lifecycle seam for the modular shuttle.
    /// It owns controller persistence/binding, but does not own assembly rules or profile math.
    /// </summary>
    public sealed partial class CompModularShuttleCore : ThingComp,
        IShuttleProjectileInterceptorShieldPort,
        IShuttleSurfaceShieldPort,
        IShuttleHullIntegrityPort,
        IShuttleControlBootAnimationPort,
        IShuttleExternalSDKHost,
        IShuttleExternalSDKCargoHost,
        IShuttleExternalSDKCargoTransactionHost,
        IShuttleExternalSDKCargoTransactionDiagnosticsHost,
        IShuttleExternalSDKLaunchReadHost,
        IShuttleExternalSDKLaunchOccupantHandoffHost
    {
        private ShuttleController controller;
        private bool hasShownControlPanelFirstBootAnimation;
        private bool hasPendingControlPanelSystemUpdateAnimation;
        private string lastShownControlPanelSystemUpdateVersion;
        private string pendingControlPanelSystemUpdateVersion;
        private List<string> pendingSystemUpdateStepKeys;
        private bool forceControlPanelSystemUpdateScheduleForDev;

        internal ShuttleController Controller
        {
            get
            {
                return this.controller;
            }
        }

        public bool TryGetExternalReadSnapshotProvider(
            out IShuttleExternalReadSnapshotProvider provider)
        {
            provider = null;
            this.EnsureController();
            if (this.controller == null)
            {
                return false;
            }

            provider = new ExternalSDKReadSnapshotProvider(this.controller);
            return true;
        }

        public bool TryGetExternalReadSnapshot(out ShuttleExternalReadSnapshot snapshot)
        {
            snapshot = null;
            IShuttleExternalReadSnapshotProvider provider;
            return this.TryGetExternalReadSnapshotProvider(out provider) &&
                provider.TryGetReadSnapshot(out snapshot);
        }

        public bool TryGetExternalCargoReadProvider(
            out IShuttleExternalCargoReadProvider provider)
        {
            provider = null;
            this.EnsureController();
            if (this.controller == null)
            {
                return false;
            }

            provider = new ExternalSDKCargoReadProvider(this.controller);
            return true;
        }

        public bool TryGetExternalCargoReadSnapshot(out ShuttleExternalCargoReadSnapshot snapshot)
        {
            snapshot = null;
            IShuttleExternalCargoReadProvider provider;
            return this.TryGetExternalCargoReadProvider(out provider) &&
                provider.TryGetCargoReadSnapshot(out snapshot);
        }

        public bool TryGetExternalCargoTransactionProvider(
            out IShuttleExternalCargoTransactionProvider provider)
        {
            provider = null;
            this.EnsureController();
            if (this.controller == null)
            {
                return false;
            }

            provider = new ExternalSDKCargoTransactionProvider(this.controller);
            return true;
        }

        public bool TryGetExternalCargoTransactionDiagnosticsProvider(
            out IShuttleExternalCargoTransactionDiagnosticsProvider provider)
        {
            provider = ExternalSDKCargoTransactionDiagnosticsProvider.Instance;
            return provider != null;
        }

        public bool TryGetExternalLaunchReadProvider(
            out IShuttleExternalLaunchReadProvider provider)
        {
            provider = null;
            this.EnsureController();
            if (this.controller == null)
            {
                return false;
            }

            provider = new ExternalSDKLaunchReadProvider(this.controller);
            return true;
        }

        public bool TryGetExternalLaunchOccupantHandoffProvider(
            out IShuttleExternalLaunchOccupantHandoffProvider provider)
        {
            provider = null;
            this.EnsureController();
            if (this.controller == null)
            {
                return false;
            }

            provider = new ExternalSDKLaunchOccupantHandoffProvider(this.controller);
            return true;
        }

        public bool HasShownControlPanelFirstBootAnimation
        {
            get
            {
                return this.hasShownControlPanelFirstBootAnimation;
            }
        }

        public bool HasPendingControlPanelSystemUpdateAnimation
        {
            get
            {
                return this.hasPendingControlPanelSystemUpdateAnimation;
            }
        }

        public string LastShownControlPanelSystemUpdateVersion
        {
            get
            {
                return this.lastShownControlPanelSystemUpdateVersion;
            }
        }

        public string PendingControlPanelSystemUpdateVersion
        {
            get
            {
                return this.pendingControlPanelSystemUpdateVersion;
            }
        }

        public IReadOnlyList<string> PendingSystemUpdateStepKeys
        {
            get
            {
                return this.pendingSystemUpdateStepKeys;
            }
        }

        public void MarkControlPanelFirstBootAnimationShown()
        {
            this.hasShownControlPanelFirstBootAnimation = true;
        }

        public void ScheduleControlPanelSystemUpdateAnimation(IReadOnlyList<string> stepKeys)
        {
            this.ScheduleControlPanelSystemUpdateAnimationForVersion(stepKeys, null);
        }

        public void ScheduleControlPanelSystemUpdateAnimationForVersion(IReadOnlyList<string> stepKeys, string version)
        {
            this.hasPendingControlPanelSystemUpdateAnimation = true;
            this.pendingControlPanelSystemUpdateVersion = this.NormalizeSystemUpdateVersion(version);
            this.pendingSystemUpdateStepKeys =
                ShuttleControlSystemUpdateUtility.SanitizeStepKeys(stepKeys);
        }

        public void MarkControlPanelSystemUpdateAnimationShown()
        {
            if (!string.IsNullOrEmpty(this.pendingControlPanelSystemUpdateVersion))
            {
                this.lastShownControlPanelSystemUpdateVersion = this.pendingControlPanelSystemUpdateVersion;
            }

            this.hasPendingControlPanelSystemUpdateAnimation = false;
            this.pendingControlPanelSystemUpdateVersion = null;
            if (this.pendingSystemUpdateStepKeys != null)
            {
                this.pendingSystemUpdateStepKeys.Clear();
            }
        }

        public void RecordControlPanelSystemUpdateVersion(string version)
        {
            this.lastShownControlPanelSystemUpdateVersion = this.NormalizeSystemUpdateVersion(version);
        }

        public bool ConsumeForcedControlPanelSystemUpdateScheduleForDev()
        {
            if (!this.forceControlPanelSystemUpdateScheduleForDev)
            {
                return false;
            }

            this.forceControlPanelSystemUpdateScheduleForDev = false;
            return true;
        }

        public void ForceControlPanelSystemUpdateScheduleForDev()
        {
            this.forceControlPanelSystemUpdateScheduleForDev = true;
        }

        private CompProperties_ModularShuttleCore Props
        {
            get
            {
                return (CompProperties_ModularShuttleCore)this.props;
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);

            // Initialize runs before the host enters play, so only bind the host seam here.
            this.EnsureController();
            this.controller.Bind(this.parent as Building_ModularShuttle, this.parent);
            this.controller.ConfigureInitialStoredEnergyPercent(this.Props.initialStoredEnergyPercent);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(
                ref this.hasShownControlPanelFirstBootAnimation,
                "hasShownControlPanelFirstBootAnimation",
                false);
            Scribe_Values.Look(
                ref this.hasPendingControlPanelSystemUpdateAnimation,
                "hasPendingControlPanelSystemUpdateAnimation",
                false);
            Scribe_Values.Look(
                ref this.lastShownControlPanelSystemUpdateVersion,
                "lastShownControlPanelSystemUpdateVersion");
            Scribe_Values.Look(
                ref this.pendingControlPanelSystemUpdateVersion,
                "pendingControlPanelSystemUpdateVersion");
            Scribe_Collections.Look(
                ref this.pendingSystemUpdateStepKeys,
                "pendingSystemUpdateStepKeys",
                LookMode.Value);
            Scribe_Deep.Look(ref this.controller, "controller");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.SanitizePendingSystemUpdateAnimation();

                // Rebind transient host references after the controller is restored from save.
                this.EnsureController();
                this.controller.Bind(this.parent as Building_ModularShuttle, this.parent);
                this.controller.ConfigureInitialStoredEnergyPercent(this.Props.initialStoredEnergyPercent);
                this.controller.NotifyLoadedFromSave();
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            this.EnsureController();
            this.controller.Bind(this.parent as Building_ModularShuttle, this.parent);
            this.controller.ConfigureInitialStoredEnergyPercent(this.Props.initialStoredEnergyPercent);

            if (!respawningAfterLoad)
            {
                // The live shuttle host still needs the vanilla/Odyssey ship parent startup seam
                // on true first spawn so launch/arrival state can function like a real shuttle host.
                CompShuttle shuttleComp = this.parent.TryGetComp<CompShuttle>();
                if (shuttleComp != null && shuttleComp.shipParent != null)
                {
                    shuttleComp.shipParent.Start();
                }

                // First creation owns initial assembly bootstrap. Reloads must not run this path again.
                this.controller.InitializeAfterCreation(this.Props.defaultAssemblyLayoutDefName);
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            this.EnsureController();
            this.controller.Tick();
        }

        internal void TryApplyStarterPresetAfterSelection()
        {
            this.EnsureController();
            this.controller.Bind(this.parent as Building_ModularShuttle, this.parent);
            this.controller.ConfigureInitialStoredEnergyPercent(
                this.Props.initialStoredEnergyPercent);
            this.controller.TryApplyStarterPresetAfterSelection();
        }

        public override void PostDraw()
        {
            base.PostDraw();
            this.DrawPaintOverlay();
        }

        internal void NotifyCargoHauledToTransporter(CompTransporter transporter, Thing thing, int count)
        {
            this.EnsureController();
            this.controller.NotifyCargoHauledToTransporter(transporter, thing, count);
        }

        internal void ClearPendingLoadDestinations()
        {
            this.EnsureController();
            this.controller.ClearPendingLoadDestinations();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!Prefs.DevMode || !DebugSettings.godMode)
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = "[DEV] Fill shuttle batteries",
                defaultDesc = "Sets the shuttle runtime battery charge to current profile capacity.",
                action = this.FillStoredEnergyForDev
            };

            yield return new Command_Action
            {
                defaultLabel = "[DEV] Fill shuttle hull",
                defaultDesc = "Sets the shuttle hull integrity to current profile maximum.",
                action = this.FillHullForDev
            };

            yield return new Command_Action
            {
                defaultLabel = "CT_Shuttle_Dev_OpenV3ControlLabel".Translate().ToString(),
                defaultDesc = "CT_Shuttle_Dev_OpenV3ControlDesc".Translate().ToString(),
                action = this.OpenShuttleControlV3ExperimentalForDev
            };

        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (ShuttleCriticalDamagePolicy.ShouldRestrictPlayerInteractions(this.parent))
            {
                yield break;
            }

            if (selPawn == null ||
                selPawn.Destroyed ||
                selPawn.Dead ||
                selPawn.Downed ||
                !selPawn.IsColonistPlayerControlled)
            {
                yield break;
            }

            this.EnsureController();
            string reason;
            float repairHitPoints;
            int workTicks;
            string costSummary;
            if (!this.controller.TryGetHullRepairPlanForPawn(
                selPawn,
                out repairHitPoints,
                out workTicks,
                out costSummary,
                out reason))
            {
                if (this.IsBenignHullRepairUnavailableReason(reason))
                {
                    yield break;
                }

                yield return new FloatMenuOption(
                    string.IsNullOrEmpty(reason)
                        ? "CT_Shuttle_HullRepair_Failed".Translate().ToString()
                        : reason,
                    null);
                yield break;
            }

            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("CT_RepairShuttleHull");
            if (jobDef == null)
            {
                yield return new FloatMenuOption("CT_Shuttle_HullRepair_Failed".Translate().ToString(), null);
                yield break;
            }

            string optionLabel = "CT_Shuttle_HullRepair_FloatMenuOptionWithPlan".Translate(
                string.IsNullOrEmpty(costSummary)
                    ? "CT_Shuttle_HullRepair_CostSummaryEmpty".Translate().ToString()
                    : costSummary,
                workTicks.ToString()).ToString();
            yield return new FloatMenuOption(
                optionLabel,
                delegate
                {
                    if (selPawn.jobs == null)
                    {
                        return;
                    }

                    Job job = JobMaker.MakeJob(jobDef, this.parent);
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
        }

        private bool IsBenignHullRepairUnavailableReason(string reason)
        {
            return reason == "CT_Shuttle_HullRepair_NotNeeded".Translate().ToString();
        }

        private ShuttleProfile EnsureProfile()
        {
            this.EnsureController();
            return this.controller.EnsureProfile();
        }

        internal void OpenControlUI()
        {
            if (ShuttleCriticalDamagePolicy.TryRejectRestrictedInteraction(this.parent))
            {
                return;
            }

            this.EnsureProfile();
            ShuttleControlUIOpener.OpenMainControlWindow(
                this.parent,
                this.controller,
                this);
        }

        internal void OpenControlLoadingPage()
        {
            if (ShuttleCriticalDamagePolicy.TryRejectRestrictedInteraction(this.parent))
            {
                return;
            }

            this.EnsureProfile();
            ShuttleControlUIOpener.OpenCargoControlPage(
                this.parent,
                this.controller,
                this);
        }

        private void OpenShuttleControlV3ExperimentalForDev()
        {
            this.OpenControlUI();
        }

        internal void OpenQuickLoadCargoWindow()
        {
            if (ShuttleCriticalDamagePolicy.TryRejectRestrictedInteraction(this.parent))
            {
                return;
            }

            this.EnsureProfile();
            ShuttleControlUIOpener.OpenQuickLoadCargoWindow(this.controller);
        }

        internal void OpenQuickCargoUnloadWindow()
        {
            if (ShuttleCriticalDamagePolicy.TryRejectRestrictedInteraction(this.parent))
            {
                return;
            }

            this.EnsureProfile();
            ShuttleControlUIOpener.OpenQuickCargoUnloadWindow(this.controller);
        }

        internal void OpenQuickCrewUnloadDialog()
        {
            if (ShuttleCriticalDamagePolicy.TryRejectRestrictedInteraction(this.parent))
            {
                return;
            }

            this.EnsureProfile();
            ShuttleControlUIOpener.OpenQuickCrewUnloadDialog(this.controller);
        }

        internal void OpenQuickLaunchFlow()
        {
            if (ShuttleCriticalDamagePolicy.TryRejectRestrictedInteraction(this.parent))
            {
                return;
            }

            this.EnsureProfile();
            ShuttleControlUIOpener.OpenQuickLaunchFlow(this.controller);
        }

        internal void CloseOpenControlDialogForCriticalDamage()
        {
            ShuttleControlUIOpener.CloseOpenControlWindowForHost(this.parent);
        }

        internal void NotifyArrived()
        {
            this.EnsureController();
            this.controller.Bind(this.parent as Building_ModularShuttle, this.parent);
            this.controller.NotifyArrived();
        }

        private void FillStoredEnergyForDev()
        {
            this.EnsureController();
            string message;
            bool success = this.controller.TryFillStoredEnergyForDev(out message);
            Messages.Message(
                message ?? "[CeleTech Shuttle] Fill shuttle batteries failed.",
                success ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.RejectInput,
                false);
        }

        internal void FillActiveSurfaceShieldForDev()
        {
            this.EnsureController();
            string message;
            bool success = this.controller.TryFillActiveSurfaceShieldForDev(out message);
            Messages.Message(
                message ?? "[CeleTech Shuttle] Fill surface shield failed.",
                success ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.RejectInput,
                false);
        }

        private void FillHullForDev()
        {
            this.EnsureController();
            string message;
            bool success = this.controller.TryFillHullForDev(out message);
            Messages.Message(
                message ?? "[CeleTech Shuttle] Fill shuttle hull failed.",
                success ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.RejectInput,
                false);
        }

        private void RepairHullForDev()
        {
            this.EnsureController();
            string message;
            bool success = this.controller.TryRepairHullForDev(50f, out message);
            Messages.Message(
                message ?? "[CeleTech Shuttle] Repair shuttle hull failed.",
                success ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.RejectInput,
                false);
        }

        private void ScheduleControlPanelSystemUpdateForDev()
        {
            this.ForceControlPanelSystemUpdateScheduleForDev();
            Messages.Message(
                ShuttleControlSystemUpdateUtility.PreparedMessageKey.Translate(),
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        private void SanitizePendingSystemUpdateAnimation()
        {
            this.lastShownControlPanelSystemUpdateVersion =
                this.NormalizeSystemUpdateVersion(this.lastShownControlPanelSystemUpdateVersion);
            this.pendingControlPanelSystemUpdateVersion =
                this.NormalizeSystemUpdateVersion(this.pendingControlPanelSystemUpdateVersion);

            if (!this.hasPendingControlPanelSystemUpdateAnimation)
            {
                this.pendingControlPanelSystemUpdateVersion = null;
                if (this.pendingSystemUpdateStepKeys != null)
                {
                    this.pendingSystemUpdateStepKeys.Clear();
                }

                return;
            }

            this.pendingSystemUpdateStepKeys =
                ShuttleControlSystemUpdateUtility.SanitizeStepKeys(this.pendingSystemUpdateStepKeys);
        }

        private string NormalizeSystemUpdateVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return null;
            }

            string trimmed = version.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

        bool IShuttleProjectileInterceptorShieldPort.TryGetActiveProjectileInterceptorShield(
            out ShuttleProjectileInterceptorShieldSnapshot snapshot)
        {
            this.EnsureController();
            return this.controller.TryGetActiveProjectileInterceptorShield(out snapshot);
        }

        bool IShuttleProjectileInterceptorShieldPort.TryConsumeShieldRechargeEnergy(float amountWd)
        {
            this.EnsureController();
            return this.controller.TryConsumeShieldRechargeEnergy(amountWd);
        }

        bool IShuttleSurfaceShieldPort.TryGetActiveSurfaceShieldStatus(
            out ShuttleSurfaceShieldStatusSnapshot snapshot)
        {
            this.EnsureController();
            return this.controller.TryGetActiveSurfaceShieldStatus(out snapshot);
        }

        bool IShuttleSurfaceShieldPort.TryGetActiveSurfaceShieldVisualConfig(
            out ShuttleSurfaceShieldVisualConfigSnapshot snapshot)
        {
            this.EnsureController();
            return this.controller.TryGetActiveSurfaceShieldVisualConfig(out snapshot);
        }

        bool IShuttleSurfaceShieldPort.TrySetActiveSurfaceShieldRechargeSpeed(float multiplier)
        {
            this.EnsureController();
            return this.controller.TrySetActiveSurfaceShieldRechargeSpeed(multiplier);
        }

        bool IShuttleSurfaceShieldPort.TryAbsorbIncomingDamage(
            ref DamageInfo dinfo,
            out ShuttleSurfaceShieldAbsorbResult result)
        {
            this.EnsureController();
            return this.controller.TryAbsorbIncomingSurfaceShieldDamage(ref dinfo, out result);
        }

        bool IShuttleHullIntegrityPort.TryApplyIncomingHullDamage(
            ref DamageInfo dinfo,
            out ShuttleHullDamageResult result)
        {
            this.EnsureController();
            return this.controller.TryApplyIncomingHullDamage(ref dinfo, out result);
        }

        internal bool TryGetPaintSchemeForVisual(out ShuttlePaintSchemeSnapshot paintScheme)
        {
            paintScheme = ShuttlePaintSchemeSnapshot.Default;

            ShuttleController currentController = this.controller;
            if (currentController == null)
            {
                return false;
            }

            ShuttleAssemblyState state = currentController.AssemblyState;
            if (state == null)
            {
                return false;
            }

            ShuttlePaintScheme scheme = state.PaintScheme;
            if (scheme == null)
            {
                return false;
            }

            scheme.EnsureInitialized();
            paintScheme = ShuttlePaintSchemeSnapshot.FromScheme(scheme);
            return true;
        }

        private void DrawPaintOverlay()
        {
            if (this.parent == null || !this.parent.Spawned)
            {
                return;
            }

            ShuttlePaintSchemeSnapshot paintScheme;
            if (!this.TryGetPaintSchemeForVisual(out paintScheme) ||
                paintScheme == null ||
                !paintScheme.Enabled)
            {
                return;
            }

            ShuttlePaintMapOverlayDrawer.Draw(this.parent, paintScheme);
        }

        private void EnsureController()
        {
            if (this.controller == null)
            {
                // Controller lifetime is owned by the core comp.
                this.controller = new ShuttleController();
            }
        }
    }
}

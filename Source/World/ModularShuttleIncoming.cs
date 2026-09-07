using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx.Vapor;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.Paint;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.SkyfallerDeploy;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.World
{
    /// <summary>
    /// Incoming skyfaller for the live modular shuttle host.
    /// This stays intentionally thin: preserve vanilla/Odyssey arrival animation behavior
    /// and forward arrival notification to the shuttle-owned controller seam.
    /// </summary>
    public sealed class ModularShuttleIncoming : ShuttleIncoming
    {
        private static readonly SimpleCurve AngleCurve = new SimpleCurve
        {
            {
                new CurvePoint(0f, 30f),
                true
            },
            {
                new CurvePoint(1f, 0f),
                true
            }
        };

        private ThingWithComps Shuttle
        {
            get
            {
                if (this.innerContainer == null || this.innerContainer.Count == 0)
                {
                    return null;
                }

                for (int i = 0; i < this.innerContainer.Count; i++)
                {
                    ThingWithComps candidate = this.innerContainer[i] as ThingWithComps;
                    if (candidate != null && candidate.TryGetComp<CompModularShuttleCore>() != null)
                    {
                        return candidate;
                    }
                }

                return this.innerContainer[0] as ThingWithComps;
            }
        }

        public override Color DrawColor
        {
            get
            {
                ThingWithComps shuttle = this.Shuttle;
                if (shuttle != null)
                {
                    return shuttle.DrawColor;
                }

                return base.DrawColor;
            }
        }

        protected override void Impact()
        {
            ThingWithComps shuttle = this.Shuttle;
            if (shuttle != null)
            {
                RefrigeratedCargoRestoreResult refrigeratedRestoreResult =
                    ShuttleRefrigeratedCargoLaunchTransferService.TryRestoreBeforeImpact(
                    shuttle,
                    this.GetAssemblyState(shuttle),
                    this.GetRuntimeState(shuttle),
                    this.ResolveNormalCargoFallbackOwner(shuttle),
                    this.innerContainer);

                if (refrigeratedRestoreResult == null)
                {
                    string nullRestoreDebug = this.BuildRefrigeratedNullRestoreDebug(shuttle);
                    if (this.HasActiveRefrigeratedLaunchTransfer(shuttle))
                    {
                        Log.Error("[CeleTech Shuttle] Incoming impact held because refrigerated cargo restore returned null while active refrigerated transfer exists. " +
                            nullRestoreDebug);
                        Messages.Message(
                            "[CeleTech Shuttle] Refrigerated cargo restore returned no result while a transfer is active; landing impact held for recovery.",
                            MessageTypeDefOf.RejectInput,
                            false);
                        return;
                    }

                    Log.Warning("[CeleTech Shuttle] Refrigerated cargo restore returned null but no active refrigerated transfer exists. " +
                        nullRestoreDebug);
                }

                if (refrigeratedRestoreResult != null &&
                    refrigeratedRestoreResult.Status == RefrigeratedCargoRestoreStatus.FatalUnresolved)
                {
                    Log.Error("[CeleTech Shuttle] Incoming impact held because refrigerated cargo restore is fatal. " +
                        (refrigeratedRestoreResult.FailureReason ?? "null") +
                        " debug=" +
                        (refrigeratedRestoreResult.DebugDump ?? "null"));
                    Messages.Message(
                        "[CeleTech Shuttle] Refrigerated cargo restore failed; landing impact held for recovery. Check the log and refrigerated cargo manifest.",
                        MessageTypeDefOf.RejectInput,
                        false);
                    return;
                }

                if (refrigeratedRestoreResult != null &&
                    refrigeratedRestoreResult.Status == RefrigeratedCargoRestoreStatus.Quarantined)
                {
                    Log.Warning("[CeleTech Shuttle] Refrigerated cargo restore quarantined cargo before incoming impact. " +
                        (refrigeratedRestoreResult.FailureReason ?? "null") +
                        " debug=" +
                        (refrigeratedRestoreResult.DebugDump ?? "null"));
                }
                else if (refrigeratedRestoreResult != null && refrigeratedRestoreResult.AnyFallback)
                {
                    Log.Warning("[CeleTech Shuttle] Refrigerated cargo restore used explicit fallback before incoming impact. " +
                        (refrigeratedRestoreResult.FailureReason ?? "null") +
                        " debug=" +
                        (refrigeratedRestoreResult.DebugDump ?? "null"));
                }

                string holderRestoreFailureReason;
                ShuttleHolderIncomingRestoreFailureStatus holderRestoreFailureStatus;
                ShuttleHolderLaunchTransferService.TryRestoreBeforeIncomingImpact(
                    shuttle,
                    this.innerContainer,
                    this.Map,
                    this.Position,
                    out holderRestoreFailureReason,
                    out holderRestoreFailureStatus);

                if (!string.IsNullOrEmpty(holderRestoreFailureReason) && Prefs.DevMode)
                {
                    if (holderRestoreFailureStatus == ShuttleHolderIncomingRestoreFailureStatus.MedicalBayRestoreFatalUnresolved ||
                        holderRestoreFailureStatus == ShuttleHolderIncomingRestoreFailureStatus.HabitatRestoreFatalUnresolved ||
                        holderRestoreFailureStatus == ShuttleHolderIncomingRestoreFailureStatus.MechChargerRestoreFatalUnresolved ||
                        holderRestoreFailureStatus == ShuttleHolderIncomingRestoreFailureStatus.PrisonCellRestoreFatalUnresolved)
                    {
                        Log.Error(holderRestoreFailureReason);
                    }
                    else
                    {
                        Log.Warning(holderRestoreFailureReason);
                    }
                }

                if (holderRestoreFailureStatus == ShuttleHolderIncomingRestoreFailureStatus.MedicalBayRestoreFatalUnresolved)
                {
                    Log.Error("[CeleTech Shuttle] Incoming impact held because MedicalBay patient restore left manifest pawns in incoming/source owners. " +
                        (holderRestoreFailureReason ?? "null"));
                    if (Prefs.DevMode)
                    {
                        Messages.Message(
                            "[CeleTech Shuttle] MedicalBay incoming restore is fatal; impact held for recovery. Check the log and holder transfer manifest.",
                            MessageTypeDefOf.RejectInput,
                            false);
                    }

                    return;
                }

                if (holderRestoreFailureStatus == ShuttleHolderIncomingRestoreFailureStatus.HabitatRestoreFatalUnresolved)
                {
                    Log.Error("[CeleTech Shuttle] Incoming impact held because Habitat restore left manifest Things unresolved. " +
                        (holderRestoreFailureReason ?? "null"));
                    if (Prefs.DevMode)
                    {
                        Messages.Message(
                            "[CeleTech Shuttle] Habitat incoming restore is fatal; impact held for recovery. Check the log and holder transfer manifest.",
                            MessageTypeDefOf.RejectInput,
                            false);
                    }

                    return;
                }

                if (holderRestoreFailureStatus == ShuttleHolderIncomingRestoreFailureStatus.MechChargerRestoreFatalUnresolved)
                {
                    Log.Error("[CeleTech Shuttle] Incoming impact held because MechCharger restore left manifest mechs unresolved. " +
                        (holderRestoreFailureReason ?? "null"));
                    if (Prefs.DevMode)
                    {
                        Messages.Message(
                            "[CeleTech Shuttle] MechCharger incoming restore is fatal; impact held for recovery. Check the log and holder transfer manifest.",
                            MessageTypeDefOf.RejectInput,
                            false);
                    }

                    return;
                }

                if (holderRestoreFailureStatus == ShuttleHolderIncomingRestoreFailureStatus.PrisonCellRestoreFatalUnresolved)
                {
                    Log.Error("[CeleTech Shuttle] Incoming impact held because PrisonCell restore left manifest prisoners unresolved. " +
                        (holderRestoreFailureReason ?? "null"));
                    if (Prefs.DevMode)
                    {
                        Messages.Message(
                            "[CeleTech Shuttle] PrisonCell incoming restore is fatal; impact held for recovery. Check the log and holder transfer manifest.",
                            MessageTypeDefOf.RejectInput,
                            false);
                    }

                    return;
                }

                if (holderRestoreFailureStatus == ShuttleHolderIncomingRestoreFailureStatus.HabitatRestoreFailedQuarantined)
                {
                    Log.Warning("[CeleTech Shuttle] Habitat incoming restore used safe eject/quarantine before impact. " +
                        (holderRestoreFailureReason ?? "null"));
                }

                if (holderRestoreFailureStatus == ShuttleHolderIncomingRestoreFailureStatus.MechChargerRestoreFailedQuarantined)
                {
                    Log.Warning("[CeleTech Shuttle] MechCharger incoming restore quarantined one or more mechs before impact. " +
                        (holderRestoreFailureReason ?? "null"));
                }

                if (holderRestoreFailureStatus == ShuttleHolderIncomingRestoreFailureStatus.PrisonCellRestoreFailedQuarantined)
                {
                    Log.Warning("[CeleTech Shuttle] PrisonCell incoming restore quarantined one or more prisoners before impact. " +
                        (holderRestoreFailureReason ?? "null"));
                }

                // Non-fatal holder restore failures are logged, but ordinary landing still
                // continues so this hook does not strand normal shuttle arrivals.
                // MedicalBay/Habitat fatal failures return above because unresolved
                // manifest pawns or dining pairs must not be handed to vanilla impact
                // as ordinary cargo.
                if (!this.NotifyControllerArrived(shuttle))
                {
                    // Optional compatibility fallback if a downstream def reattaches vanilla launchable.
                    CompLaunchable compLaunchable = shuttle.TryGetComp<CompLaunchable>();
                    if (compLaunchable != null)
                    {
                        compLaunchable.Notify_Arrived();
                    }
                }
            }

            base.Impact();
        }

        private bool HasActiveRefrigeratedLaunchTransfer(ThingWithComps shuttle)
        {
            try
            {
                CompShuttleHolderLaunchTransferState state = shuttle != null
                    ? shuttle.TryGetComp<CompShuttleHolderLaunchTransferState>()
                    : null;
                return state != null && state.HasRefrigeratedCargoLaunchTransfer;
            }
            catch (System.Exception exception)
            {
                Log.Error("[CeleTech Shuttle] Failed to inspect refrigerated launch transfer state after null restore result; treating as active to fail closed. " +
                    exception);
                return true;
            }
        }

        private string BuildRefrigeratedNullRestoreDebug(ThingWithComps shuttle)
        {
            try
            {
                CompShuttleHolderLaunchTransferState state = shuttle != null
                    ? shuttle.TryGetComp<CompShuttleHolderLaunchTransferState>()
                    : null;
                bool hasTransfer = state != null && state.HasRefrigeratedCargoLaunchTransfer;
                return "shuttleThingID=" +
                    (shuttle != null ? shuttle.thingIDNumber : -1) +
                    " hasRefrigeratedCargoLaunchTransfer=" +
                    hasTransfer +
                    " refrigeratedManifestEntryCount=" +
                    (state != null ? state.RefrigeratedCargoLaunchManifestEntryCount : 0) +
                    " refrigeratedStagingHolderCount=" +
                    (state != null ? state.RefrigeratedLaunchStagingHolderCount : 0) +
                    " sourceMethod=ShuttleRefrigeratedCargoLaunchTransferService.TryRestoreBeforeImpact returned null";
            }
            catch (System.Exception exception)
            {
                return "shuttleThingID=" +
                    (shuttle != null ? shuttle.thingIDNumber : -1) +
                    " hasRefrigeratedCargoLaunchTransfer=unknown" +
                    " refrigeratedManifestEntryCount=-1" +
                    " refrigeratedStagingHolderCount=-1" +
                    " sourceMethod=ShuttleRefrigeratedCargoLaunchTransferService.TryRestoreBeforeImpact returned null" +
                    " diagnosticException=" +
                    exception;
            }
        }

        private bool NotifyControllerArrived(ThingWithComps shuttle)
        {
            if (shuttle == null)
            {
                return false;
            }

            CompModularShuttleCore core = shuttle.TryGetComp<CompModularShuttleCore>();
            if (core == null)
            {
                return false;
            }

            core.NotifyArrived();
            return true;
        }

        private ShuttleAssemblyState GetAssemblyState(ThingWithComps shuttle)
        {
            CompModularShuttleCore core = shuttle != null
                ? shuttle.TryGetComp<CompModularShuttleCore>()
                : null;
            return core != null && core.Controller != null
                ? core.Controller.AssemblyState
                : null;
        }

        private ShuttleRuntimeState GetRuntimeState(ThingWithComps shuttle)
        {
            CompModularShuttleCore core = shuttle != null
                ? shuttle.TryGetComp<CompModularShuttleCore>()
                : null;
            return core != null && core.Controller != null
                ? core.Controller.GetLaunchRuntimeState()
                : null;
        }

        private ThingOwner ResolveNormalCargoFallbackOwner(ThingWithComps shuttle)
        {
            ThingOwner fallbackOwner;
            string failureReason;
            if (ShuttleCargoArrivalFallbackService.TryGetNormalCargoFallbackOwner(
                shuttle,
                out fallbackOwner,
                out failureReason))
            {
                return fallbackOwner;
            }

            if (Prefs.DevMode && !string.IsNullOrEmpty(failureReason))
            {
                Log.Warning(failureReason);
            }

            return null;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (respawningAfterLoad || this.BeingTransportedOnGravship)
            {
                return;
            }

            this.angle = GetAngle(0f, this.Rotation);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (!this.hasImpacted)
            {
                Log.Error("Destroying modular shuttle skyfaller without ever having impacted.");
            }

            base.Destroy(mode);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 groundDrawLoc = drawLoc;
            float extraRotation;
            this.GetDrawPositionAndRotation(ref drawLoc, out extraRotation);

            Thing thingForGraphic = ShuttleSkyfallerDeployDrawUtility.ResolveThingForGraphic(this);
            Rot4 drawRotation = ShuttleSkyfallerDeployDrawUtility.ResolveDrawRotation(thingForGraphic, flip);
            float deploymentProgress = ShuttleSkyfallerDeployProgress.IncomingDeployProgress(this);
            bool shouldDrawDeployLayers = ShuttleSkyfallerDeployProgress.ShouldDrawDeployLayers(deploymentProgress);
            ShuttleSkyfallerDeployVisualAssets deployVisualAssets =
                ShuttleSkyfallerDeployVisualAssets.Shared;
            bool deployAssetsResolved = shouldDrawDeployLayers &&
                deployVisualAssets.TryResolve();

            if (WorldComponent_GravshipController.GravshipRenderInProgess)
            {
                return;
            }

            ShuttleVisualOverlayDrawer.DrawSkyfallerGroundShadow(
                this,
                groundDrawLoc,
                drawRotation,
                ShuttleVisualOverlayDrawer.IncomingGroundShadowAlpha(this.TimeInAnimation));

            if (!ShuttleFlightVfxVaporDebugSettings.ShouldUseVaporOnlyDebug)
            {
                ShuttleFlightVfxPlaceholderFlameDrawer.Shared.DrawUnderHull(
                    this,
                    drawLoc,
                    drawRotation,
                    extraRotation,
                    ShuttleFlightVfxMode.Incoming);
            }

            if (deployAssetsResolved)
            {
                ShuttleSkyfallerDeployVisualDrawer.Shared.DrawLandingGear(
                    this,
                    drawLoc,
                    drawRotation,
                    extraRotation,
                    deploymentProgress,
                    deployVisualAssets);
            }

            ShuttleSkyfallerTurretVisualDrawer.Shared.Draw(
                this,
                drawLoc,
                drawRotation,
                extraRotation);

            if (thingForGraphic != null && this.Graphic != null)
            {
                this.Graphic.Draw(drawLoc, drawRotation, thingForGraphic, extraRotation);
            }

            ShuttlePaintSchemeSnapshot paintScheme;
            if (this.TryGetPaintSchemeForVisual(out paintScheme))
            {
                ShuttleSkyfallerPaintOverlayDrawer.Draw(
                    this,
                    drawLoc,
                    drawRotation,
                    extraRotation,
                    paintScheme);
            }

            ShuttleVisualOverlayDrawer.DrawSkyfallerLightOverlay(
                this,
                drawLoc,
                drawRotation,
                extraRotation);

            if (ShuttleFlightVfxVaporDebugSettings.ShouldDrawVapor)
            {
                ShuttleFlightVfxVaporDrawer.Shared.Draw(
                    this,
                    drawLoc,
                    drawRotation,
                    extraRotation,
                    ShuttleFlightVfxMode.Incoming,
                    this.TimeInAnimation,
                    ShuttleVaporQualityProvider.CurrentProfile);
            }

            if (!ShuttleFlightVfxVaporDebugSettings.ShouldUseVaporOnlyDebug)
            {
                ShuttleFlightVfxPlaceholderFlameDrawer.Shared.DrawOverHull(
                    this,
                    drawLoc,
                    drawRotation,
                    extraRotation,
                    ShuttleFlightVfxMode.Incoming);
            }

            if (Prefs.DevMode && ShuttleFlightVfxDebugSettings.ShouldDrawThrusterAnchors)
            {
                ShuttleFlightVfxDebugAnchorDrawer.Shared.Draw(this, drawLoc, drawRotation, extraRotation);
            }
        }

        protected override void GetDrawPositionAndRotation(ref Vector3 drawLoc, out float extraRotation)
        {
            extraRotation = 0f;
            this.angle = GetAngle(this.TimeInAnimation, this.Rotation);

            switch (this.Rotation.AsInt)
            {
                case 1:
                    extraRotation += this.def.skyfaller.rotationCurve.Evaluate(this.TimeInAnimation);
                    break;
                case 3:
                    extraRotation -= this.def.skyfaller.rotationCurve.Evaluate(this.TimeInAnimation);
                    break;
            }

            drawLoc.z += this.def.skyfaller.zPositionCurve.Evaluate(this.TimeInAnimation);
        }

        public override float DrawAngle()
        {
            float num = 0f;
            switch (this.Rotation.AsInt)
            {
                case 1:
                    num += this.def.skyfaller.rotationCurve.Evaluate(this.TimeInAnimation);
                    break;
                case 3:
                    num -= this.def.skyfaller.rotationCurve.Evaluate(this.TimeInAnimation);
                    break;
            }

            return num;
        }

        private static float GetAngle(float timeInAnimation, Rot4 rotation)
        {
            int asInt = rotation.AsInt;
            if (asInt == 1)
            {
                return rotation.Opposite.AsAngle + AngleCurve.Evaluate(timeInAnimation);
            }

            if (asInt != 3)
            {
                return rotation.Opposite.AsAngle;
            }

            return rotation.Opposite.AsAngle - AngleCurve.Evaluate(timeInAnimation);
        }

        private bool TryGetPaintSchemeForVisual(out ShuttlePaintSchemeSnapshot paintScheme)
        {
            paintScheme = null;

            ThingWithComps shuttle = this.Shuttle;
            if (shuttle == null)
            {
                return false;
            }

            CompModularShuttleCore core = shuttle.TryGetComp<CompModularShuttleCore>();
            return core != null && core.TryGetPaintSchemeForVisual(out paintScheme);
        }
    }
}

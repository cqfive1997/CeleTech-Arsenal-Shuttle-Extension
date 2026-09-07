using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx.Vapor;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.Paint;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.SkyfallerDeploy;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.World
{
    /// <summary>
    /// Leaving skyfaller for the live modular shuttle host.
    /// This keeps only the launch animation/handoff surface and does not own energy,
    /// cargo, target validation, or reactor/grid logic.
    /// </summary>
    public sealed class ModularShuttleLeaving : FlyShipLeaving
    {
        private static readonly SimpleCurve AngleCurve = new SimpleCurve
        {
            {
                new CurvePoint(0f, 0f),
                true
            },
            {
                new CurvePoint(1f, 20f),
                true
            }
        };

        public override Color DrawColor
        {
            get
            {
                if (this.Contents == null)
                {
                    return base.DrawColor;
                }

                Thing shuttle = this.Contents.GetShuttle();
                if (shuttle == null)
                {
                    return base.DrawColor;
                }

                return shuttle.DrawColor;
            }
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

        public override void ExposeData()
        {
            base.ExposeData();

            if (Scribe.mode != LoadSaveMode.PostLoadInit || this.Contents == null)
            {
                return;
            }

            TransportersArrivalAction wrappedArrivalAction =
                ModularShuttleVisitSiteArrivalUtility.WrapVisitSiteArrivalForModularShuttle(
                this.arrivalAction,
                this.Contents.GetShuttle());
            this.arrivalAction = ModularShuttleSafeArrivalAction.Wrap(
                wrappedArrivalAction,
                this.Contents.GetShuttle());
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 groundDrawLoc = drawLoc;
            float extraRotation;
            this.GetDrawPositionAndRotation(ref drawLoc, out extraRotation);

            Thing thingForGraphic = ShuttleSkyfallerDeployDrawUtility.ResolveThingForGraphic(this);
            Rot4 drawRotation = ShuttleSkyfallerDeployDrawUtility.ResolveDrawRotation(thingForGraphic, flip);
            float deploymentProgress =
                ShuttleSkyfallerDeployProgress.LeavingDeployProgress(
                    this,
                    this.TimeInAnimation);
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
                ShuttleVisualOverlayDrawer.LeavingGroundShadowAlpha(this.TimeInAnimation));

            if (!ShuttleFlightVfxVaporDebugSettings.ShouldUseVaporOnlyDebug)
            {
                ShuttleFlightVfxPlaceholderFlameDrawer.Shared.DrawUnderHull(
                    this,
                    drawLoc,
                    drawRotation,
                    extraRotation,
                    ShuttleFlightVfxMode.Leaving);
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
                    ShuttleFlightVfxMode.Leaving,
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
                    ShuttleFlightVfxMode.Leaving);
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
                return rotation.AsAngle - AngleCurve.Evaluate(timeInAnimation);
            }

            if (asInt != 3)
            {
                return rotation.AsAngle;
            }

            return rotation.AsAngle + AngleCurve.Evaluate(timeInAnimation);
        }

        private bool TryGetPaintSchemeForVisual(out ShuttlePaintSchemeSnapshot paintScheme)
        {
            paintScheme = null;

            if (this.Contents == null)
            {
                return false;
            }

            ThingWithComps shuttle = this.Contents.GetShuttle() as ThingWithComps;
            if (shuttle == null)
            {
                return false;
            }

            CompModularShuttleCore core = shuttle.TryGetComp<CompModularShuttleCore>();
            return core != null && core.TryGetPaintSchemeForVisual(out paintScheme);
        }
    }
}

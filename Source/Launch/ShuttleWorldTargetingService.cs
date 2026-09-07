using System;
using System.Collections.Generic;
using System.Linq;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Flight;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Bridges;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.World;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    public sealed class ShuttleWorldTargetingService
    {
        private readonly IShuttleFlightEnergyCalculator energyCalculator;
        private readonly IShuttleCargoBackend cargoBackend;

        public ShuttleWorldTargetingService(
            IShuttleFlightEnergyCalculator energyCalculator,
            IShuttleCargoBackend cargoBackend)
        {
            this.energyCalculator = energyCalculator;
            this.cargoBackend = cargoBackend;
        }

        public ShuttleLaunchResult BeginTargeting(ShuttleLaunchTargetingContext context)
        {
            if (context == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_TargetingContextUnavailable".Translate().ToString());
            }

            ThingWithComps host = context.Host;
            if (host == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_HostUnavailable".Translate().ToString());
            }

            if (!host.Spawned || host.Map == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_TargetingRequiresSpawned".Translate().ToString());
            }

            PlanetTile originTile = host.Tile;
            if (!originTile.Valid)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_OriginTileInvalid".Translate().ToString());
            }

            CameraJumper.TryJump(CameraJumper.GetWorldTarget(new GlobalTargetInfo(originTile)), CameraJumper.MovementMode.Pan);
            Find.WorldSelector.ClearSelection();
            // This is the shuttle-owned world targeter. It deliberately uses RimWorld's generic
            // WorldTargeter API instead of CompLaunchable's gizmo/targeting helpers.
            Find.WorldTargeter.BeginTargeting(
                delegate(GlobalTargetInfo target)
                {
                    return this.ChooseWorldTarget(context, target);
                },
                true,
                null,
                true,
                null,
                delegate(GlobalTargetInfo target)
                {
                    return this.BuildTargetingLabel(context, target);
                },
                null,
                originTile,
                true);

            return ShuttleLaunchResult.Succeeded("CT_Shuttle_Launch_TargetingOpened".Translate().ToString(), null);
        }

        private bool ChooseWorldTarget(ShuttleLaunchTargetingContext context, GlobalTargetInfo target)
        {
            // Hover validation is repeated on click, and the confirm command validates again
            // before spending charge. This keeps launch safe if state changes while targeting.
            ShuttleLaunchResult targetResult = this.ValidateTargetForUI(context, target, true);
            if (!targetResult.Success)
            {
                Messages.Message(targetResult.Message, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            ThingWithComps host = context.Host;
            List<FloatMenuOption> options = this.GetArrivalOptions(
                host,
                target.Tile,
                delegate(PlanetTile destinationTile, TransportersArrivalAction arrivalAction)
                {
                    ShuttleCommandResult result = context.CommandExecutor != null
                        ? context.CommandExecutor.Execute(new ConfirmLaunchCommand(destinationTile, arrivalAction))
                        : ShuttleCommandResult.Failed("CT_Shuttle_Command_ExecutorUnavailable".Translate().ToString());
                    if (!result.Success)
                    {
                        Messages.Message(result.Message, MessageTypeDefOf.RejectInput, false);
                    }
                    else
                    {
                        Find.WorldTargeter.StopTargeting();
                    }
                });

            if (options.Count == 0)
            {
                Messages.Message("CT_Shuttle_Launch_Failed_NoArrivalAction".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (options.Count == 1)
            {
                FloatMenuOption option = options[0];
                if (option.Disabled)
                {
                    Messages.Message(option.Label, MessageTypeDefOf.RejectInput, false);
                    return false;
                }

                option.action();
                return true;
            }

            Find.WindowStack.Add(new FloatMenu(options));
            return false;
        }

        private TaggedString BuildTargetingLabel(ShuttleLaunchTargetingContext context, GlobalTargetInfo target)
        {
            if (!target.IsValid)
            {
                return null;
            }

            ShuttleLaunchResult result = this.ValidateTargetForUI(context, target, false);
            if (!result.Success)
            {
                GUI.color = ColorLibrary.RedReadable;
                return result.Message;
            }

            ShuttleFlightEnergyQuote quote = result.Quote;
            if (quote == null)
            {
                return "CT_Shuttle_UI_Launch".Translate().ToString();
            }

            return "CT_Shuttle_Launch_TargetLabel".Translate(
                quote.RequiredEnergyWd.ToString("0.#"),
                quote.AvailableEnergyWd.ToString("0.#"),
                quote.DistanceTiles,
                quote.MaxDistanceTilesAtCurrentLoad).ToString();
        }

        private ShuttleLaunchResult ValidateTargetForUI(ShuttleLaunchTargetingContext context, GlobalTargetInfo target, bool includeArrivalOptions)
        {
            if (context == null || context.Host == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_HostUnavailable".Translate().ToString());
            }

            if (!target.IsValid)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_DestinationInvalid".Translate().ToString());
            }

            ThingWithComps host = context.Host;
            ShuttleProfile profile = context.GetProfileForRead != null
                ? context.GetProfileForRead()
                : null;
            PlanetTile originTile = host.Tile;
            PlanetTile destinationTile = target.Tile;
            if (!destinationTile.Valid)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_DestinationTileInvalid".Translate().ToString());
            }

            if (originTile.Layer != destinationTile.Layer && !originTile.Layer.HasConnectionPathTo(destinationTile.Layer))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_NoWorldPath".Translate().ToString());
            }

            if (target.HasWorldObject &&
                !target.WorldObject.def.validLaunchTarget &&
                !ModularShuttleCaravanWorldObjectCompatUtility.HasSupportedWorldObjectAt(destinationTile))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_InvalidWorldObjectTarget".Translate().ToString());
            }

            if (target.HasWorldObject &&
                !ShuttleSignalJammerAccessPolicy.CanReach(target.WorldObject, profile))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_SignalJammerRequired".Translate().ToString());
            }

            if (Find.World.Impassable(destinationTile))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_DestinationNotLaunchable".Translate().ToString());
            }

            if (includeArrivalOptions)
            {
                List<FloatMenuOption> options = this.GetArrivalOptions(host, destinationTile, delegate(PlanetTile tile, TransportersArrivalAction action) { });
                if (options.Count == 0)
                {
                    return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_NoArrivalAction".Translate().ToString());
                }
            }

            ShuttleCargoSnapshot cargoSnapshot = context.BuildCargoSnapshot != null ? context.BuildCargoSnapshot() : null;
            if (cargoSnapshot != null &&
                cargoSnapshot.HasTransporter &&
                !ShuttleLaunchCrewRequirementUtility.HasLaunchController(cargoSnapshot, context.Host, profile))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_CockpitColonistRequired".Translate().ToString());
            }

            ShuttleFlightEnergyQuote quote = this.BuildQuote(context, destinationTile, cargoSnapshot, profile);
            if (quote == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_QuoteUnavailable".Translate().ToString());
            }

            if (!quote.CanLaunch)
            {
                return ShuttleLaunchResult.Failed(quote.FailureReason, quote);
            }

            return ShuttleLaunchResult.Succeeded("CT_Shuttle_Launch_TargetValid".Translate().ToString(), quote);
        }

        private ShuttleFlightEnergyQuote BuildQuote(
            ShuttleLaunchTargetingContext context,
            PlanetTile destinationTile,
            ShuttleCargoSnapshot cargoSnapshot,
            ShuttleProfile profile = null)
        {
            if (context == null || context.Host == null)
            {
                return null;
            }

            ThingWithComps host = context.Host;
            // WorldTargeter calls labels frequently while hovering; quote from the pure
            // profile cache so hover UI does not reconcile/write host comps.
            if (profile == null)
            {
                profile = context.GetProfileForRead != null ? context.GetProfileForRead() : null;
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState != null ? context.GetRuntimeState() : null;
            int distanceTiles = this.GetDistanceTiles(host.Tile, destinationTile);
            float rangeDistanceFactor = this.GetRangeDistanceFactor(destinationTile);
            return this.energyCalculator.Quote(profile, runtimeState, cargoSnapshot, distanceTiles, rangeDistanceFactor);
        }

        private List<FloatMenuOption> GetArrivalOptions(
            ThingWithComps host,
            PlanetTile tile,
            Action<PlanetTile, TransportersArrivalAction> launchAction)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            Action<PlanetTile, TransportersArrivalAction> loggedLaunchAction =
                delegate(PlanetTile destinationTile, TransportersArrivalAction arrivalAction)
                {
                    string habitatTransferFailureReason;
                    string habitatTransferMode;
                    bool canUseHabitatTransfer = ShuttleHolderLaunchTransferService.CanUseAnySupportedHabitatHolderLaunchTransfer(
                        host,
                        arrivalAction,
                        out habitatTransferFailureReason,
                        out habitatTransferMode);
                    ShuttleHolderLaunchTransferService.LogHabitatLivingArrivalDiagnostics(
                        host,
                        destinationTile,
                        arrivalAction,
                        "world-target-option-selected",
                        canUseHabitatTransfer,
                        habitatTransferFailureReason,
                        habitatTransferMode);

                    launchAction(destinationTile, arrivalAction);
                };

            List<IThingHolder> holders = this.cargoBackend != null
                ? this.cargoBackend.ResolveTransporterHoldersForLaunch(host)
                : new List<IThingHolder>();
            if (TransportersArrivalAction_FormCaravan.CanFormCaravanAt(holders, tile) &&
                !Find.WorldObjects.AnySettlementBaseAt(tile) &&
                !Find.WorldObjects.AnySiteAt(tile) &&
                !ModularShuttleCaravanWorldObjectCompatUtility.HasSupportedWorldObjectAt(tile))
            {
                options.Add(new FloatMenuOption("FormCaravanHere".Translate(), delegate
                {
                    loggedLaunchAction(tile, new TransportersArrivalAction_FormCaravan("MessageShuttleArrived"));
                }));
            }

            IEnumerable<FloatMenuOption> emptyTileLandingOptions =
                ModularShuttleEmptyTileLandingTargeter.GetFloatMenuOptions(tile, loggedLaunchAction, host);
            if (emptyTileLandingOptions != null)
            {
                options.AddRange(emptyTileLandingOptions);
            }

            List<WorldObject> worldObjects = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < worldObjects.Count; i++)
            {
                WorldObject worldObject = worldObjects[i];
                if (worldObject == null || worldObject.Tile != tile)
                {
                    continue;
                }

                int optionCountBeforeWorldObject = options.Count;
                IEnumerable<FloatMenuOption> worldOptions = worldObject.GetShuttleFloatMenuOptions(holders, loggedLaunchAction);
                if (worldOptions != null)
                {
                    options.AddRange(worldOptions);
                }

                if (!HasEnabledOptionAdded(options, optionCountBeforeWorldObject))
                {
                    IEnumerable<FloatMenuOption> caravanWorldObjectOptions =
                        ModularShuttleCaravanWorldObjectCompatUtility.GetFloatMenuOptions(
                            worldObject,
                            holders,
                            loggedLaunchAction);
                    if (caravanWorldObjectOptions != null)
                    {
                        options.AddRange(caravanWorldObjectOptions);
                    }
                }

                if (!HasEnabledOptionAdded(options, optionCountBeforeWorldObject))
                {
                    this.TryAddMedicalBayTransferVisitSiteOptions(
                        host,
                        worldObject,
                        tile,
                        loggedLaunchAction,
                        options);
                }

                if (!HasEnabledOptionAdded(options, optionCountBeforeWorldObject))
                {
                    this.TryAddHabitatTransferVisitSiteOptions(
                        host,
                        worldObject,
                        tile,
                        loggedLaunchAction,
                        options);
                }
            }

            if (options.Count == 0 && !Find.World.Impassable(tile))
            {
                options.Add(new FloatMenuOption("TransportPodsContentsWillBeLost".Translate(), delegate
                {
                    loggedLaunchAction(tile, null);
                }));
            }

            return options;
        }

        private static bool HasEnabledOptionAdded(
            List<FloatMenuOption> options,
            int startIndex)
        {
            if (options == null)
            {
                return false;
            }

            int firstIndex = startIndex > 0 ? startIndex : 0;
            for (int i = firstIndex; i < options.Count; i++)
            {
                FloatMenuOption option = options[i];
                if (option != null && !option.Disabled)
                {
                    return true;
                }
            }

            return false;
        }

        private void TryAddMedicalBayTransferVisitSiteOptions(
            ThingWithComps host,
            WorldObject worldObject,
            PlanetTile tile,
            Action<PlanetTile, TransportersArrivalAction> launchAction,
            List<FloatMenuOption> options)
        {
            Site site = worldObject as Site;
            if (site == null || !site.Spawned || options == null)
            {
                return;
            }

            CompShuttleMedicalBayOccupancy medicalBay = host != null
                ? host.TryGetComp<CompShuttleMedicalBayOccupancy>()
                : null;
            if (medicalBay == null || !medicalBay.HasPatients)
            {
                return;
            }

            string transferFailureReason;
            bool canUseMedicalBayTransfer;
            bool hasHabitatTransfer = ShuttleHolderLaunchTransferService.NeedsAnyHabitatLaunchTransfer(host);
            bool hasMechChargerTransfer = ShuttleHolderLaunchTransferService.NeedsMechChargerLaunchTransfer(host);
            if (hasHabitatTransfer && hasMechChargerTransfer)
            {
                canUseMedicalBayTransfer =
                    ShuttleHolderLaunchTransferService.CanUseMedicalBayPatientLaunchTransferWithFullHolderConcurrency(
                        host,
                        out transferFailureReason);
            }
            else if (hasHabitatTransfer)
            {
                canUseMedicalBayTransfer =
                    ShuttleHolderLaunchTransferService.CanUseMedicalBayPatientLaunchTransferWithHabitatConcurrency(
                        host,
                        out transferFailureReason);
            }
            else
            {
                canUseMedicalBayTransfer =
                    ShuttleHolderLaunchTransferService.CanUseMedicalBayPatientLaunchTransfer(
                        host,
                        out transferFailureReason);
            }

            if (!canUseMedicalBayTransfer)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning("[CeleTech Shuttle] MedicalBay transfer-aware VisitSite option rejected: " + transferFailureReason);
                }

                return;
            }

            if (!this.HasNonDownedMedicalBayColonistForArrival(host))
            {
                return;
            }

            if (site.def == null ||
                site.def.mapGenerator == null ||
                site.def.mapGenerator == MapGeneratorDefOf.Space)
            {
                return;
            }

            List<IThingHolder> transferAwareHolders = this.BuildMedicalBayVisitSiteHolders(host);
            if (transferAwareHolders.Count == 0)
            {
                return;
            }

            IEnumerable<FloatMenuOption> visitSiteOptions =
                TransportersArrivalAction_VisitSite.GetFloatMenuOptions(launchAction, transferAwareHolders, site);
            if (visitSiteOptions != null)
            {
                options.AddRange(visitSiteOptions);
            }

            if (Prefs.DevMode)
            {
                Log.Message(
                    "[CeleTech Shuttle] Added MedicalBay patient transfer-aware VisitSite arrival options. " +
                    "site=" +
                    (site.def != null ? site.def.defName : "null") +
                    " tile=" +
                    (tile.Valid ? tile.ToString() : "invalid"));
            }
        }

        private void TryAddHabitatTransferVisitSiteOptions(
            ThingWithComps host,
            WorldObject worldObject,
            PlanetTile tile,
            Action<PlanetTile, TransportersArrivalAction> launchAction,
            List<FloatMenuOption> options)
        {
            Site site = worldObject as Site;
            if (site == null || !site.Spawned || options == null)
            {
                return;
            }

            CompShuttleHabitatOccupancy habitat = host != null
                ? host.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null || !habitat.HasAnyOccupants)
            {
                return;
            }

            string transferFailureReason;
            string transferMode;
            if (!ShuttleHolderLaunchTransferService.CanUseAnySupportedHabitatHolderLaunchTransfer(
                host,
                out transferFailureReason,
                out transferMode))
            {
                ShuttleHolderLaunchTransferService.LogHabitatLivingArrivalDiagnostics(
                    host,
                    tile,
                    null,
                    "world-target-visit-site-rejected",
                    false,
                    transferFailureReason,
                    transferMode);
                return;
            }

            if (!this.HasNonDownedHabitatColonistForArrival(host))
            {
                return;
            }

            if (site.def == null ||
                site.def.mapGenerator == null ||
                site.def.mapGenerator == MapGeneratorDefOf.Space)
            {
                return;
            }

            List<IThingHolder> transferAwareHolders = this.BuildHabitatLivingVisitSiteHolders(host);
            if (transferAwareHolders.Count == 0)
            {
                return;
            }

            IEnumerable<FloatMenuOption> visitSiteOptions =
                TransportersArrivalAction_VisitSite.GetFloatMenuOptions(launchAction, transferAwareHolders, site);
            if (visitSiteOptions != null)
            {
                options.AddRange(visitSiteOptions);
            }

            if (Prefs.DevMode)
            {
                Log.Message(
                    "[CeleTech Shuttle] Added Habitat holder transfer-aware VisitSite arrival options. " +
                    "transferMode=" +
                    transferMode +
                    " " +
                    "site=" +
                    (site.def != null ? site.def.defName : "null") +
                    " tile=" +
                    (tile.Valid ? tile.ToString() : "invalid"));
            }
        }

        private List<IThingHolder> BuildHabitatLivingVisitSiteHolders(ThingWithComps host)
        {
            List<IThingHolder> holders = this.cargoBackend != null
                ? new List<IThingHolder>(this.cargoBackend.ResolveTransporterHoldersForLaunch(host))
                : new List<IThingHolder>();
            CompShuttleHabitatOccupancy habitat = host != null
                ? host.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat != null)
            {
                holders.Add(habitat);
            }

            return holders;
        }

        private List<IThingHolder> BuildMedicalBayVisitSiteHolders(ThingWithComps host)
        {
            List<IThingHolder> holders = this.cargoBackend != null
                ? new List<IThingHolder>(this.cargoBackend.ResolveTransporterHoldersForLaunch(host))
                : new List<IThingHolder>();
            CompShuttleMedicalBayOccupancy medicalBay = host != null
                ? host.TryGetComp<CompShuttleMedicalBayOccupancy>()
                : null;
            if (medicalBay != null)
            {
                holders.Add(medicalBay);
            }

            return holders;
        }

        private bool HasNonDownedHabitatColonistForArrival(ThingWithComps host)
        {
            CompShuttleHabitatOccupancy habitat = host != null
                ? host.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null)
            {
                return false;
            }

            return TransportersArrivalActionUtility.AnyNonDownedColonist(new List<IThingHolder>
            {
                habitat
            });
        }

        private bool HasNonDownedMedicalBayColonistForArrival(ThingWithComps host)
        {
            CompShuttleMedicalBayOccupancy medicalBay = host != null
                ? host.TryGetComp<CompShuttleMedicalBayOccupancy>()
                : null;
            if (medicalBay == null)
            {
                return false;
            }

            return TransportersArrivalActionUtility.AnyNonDownedColonist(new List<IThingHolder>
            {
                medicalBay
            });
        }

        private int GetDistanceTiles(PlanetTile originTile, PlanetTile destinationTile)
        {
            return Find.WorldGrid.TraversalDistanceBetween(originTile, destinationTile, true, int.MaxValue, true);
        }

        private float GetRangeDistanceFactor(PlanetTile destinationTile)
        {
            if (!destinationTile.Valid || destinationTile.Layer == null || destinationTile.Layer.Def == null)
            {
                return 1f;
            }

            return destinationTile.Layer.Def.rangeDistanceFactor;
        }
    }
}

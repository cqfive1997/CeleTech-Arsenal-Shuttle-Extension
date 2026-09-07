using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Flight;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using CeleTech.ShuttleExtension.ModularShuttle.World;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static class ModularShuttleCaravanLaunchService
    {
        private const float Epsilon = 0.0001f;
        private const string WorldObjectDefName = "CT_ModularShuttleWorldObject";
        private static readonly IShuttleFlightEnergyCalculator EnergyCalculator =
            new DefaultShuttleFlightEnergyCalculator();

        internal static Gizmo CreateLaunchGizmo(Caravan caravan, ThingWithComps shuttle)
        {
            Command_Action command = new Command_Action();
            command.defaultLabel = "CommandLaunchGroup".Translate();
            command.defaultDesc = "CommandLaunchGroupDesc".Translate();
            command.icon = CompLaunchable.LaunchCommandTex;
            command.action = delegate
            {
                BeginTargeting(caravan, shuttle);
            };

            ShuttleLaunchResult result = ValidateLaunchReadiness(caravan, shuttle);
            if (!result.Success)
            {
                command.Disable(result.Message);
            }

            return command;
        }

        private static void BeginTargeting(Caravan caravan, ThingWithComps shuttle)
        {
            ThingWithComps currentShuttle;
            if (!ModularShuttleCaravanUtility.TryFindHeldModularShuttleCached(caravan, out currentShuttle))
            {
                Messages.Message("CT_Shuttle_Launch_Failed_HostUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            ShuttleLaunchResult result = ValidateLaunchReadiness(caravan, currentShuttle);
            if (!result.Success)
            {
                Messages.Message(result.Message, MessageTypeDefOf.RejectInput, false);
                return;
            }

            PlanetTile originTile = caravan.Tile;
            CameraJumper.TryJump(CameraJumper.GetWorldTarget(new GlobalTargetInfo(originTile)), CameraJumper.MovementMode.Pan);
            Find.WorldSelector.ClearSelection();
            Find.WorldTargeter.BeginTargeting(
                delegate(GlobalTargetInfo target)
                {
                    return ChooseWorldTarget(caravan, currentShuttle, target);
                },
                true,
                CompLaunchable.TargeterMouseAttachment,
                true,
                null,
                delegate(GlobalTargetInfo target)
                {
                    return BuildTargetingLabel(caravan, currentShuttle, target);
                },
                null,
                originTile,
                true);
        }

        private static bool ChooseWorldTarget(Caravan caravan, ThingWithComps shuttle, GlobalTargetInfo target)
        {
            ShuttleFlightEnergyQuote quote;
            ShuttleLaunchResult result = ValidateDestination(caravan, shuttle, target.Tile, out quote);
            if (!result.Success)
            {
                Messages.Message(result.Message, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            List<FloatMenuOption> options = GetArrivalOptions(caravan, shuttle, target.Tile);
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

        private static TaggedString BuildTargetingLabel(Caravan caravan, ThingWithComps shuttle, GlobalTargetInfo target)
        {
            if (!target.IsValid)
            {
                return null;
            }

            ShuttleFlightEnergyQuote quote;
            ShuttleLaunchResult result = ValidateDestination(caravan, shuttle, target.Tile, out quote);
            if (!result.Success)
            {
                GUI.color = ColorLibrary.RedReadable;
                return result.Message;
            }

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

        private static List<FloatMenuOption> GetArrivalOptions(
            Caravan caravan,
            ThingWithComps shuttle,
            PlanetTile tile)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            List<IThingHolder> holders = new List<IThingHolder>();
            holders.Add(caravan);
            bool hasSupportedCaravanWorldObject =
                ModularShuttleCaravanWorldObjectCompatUtility.HasSupportedWorldObjectAt(tile);
            IEnumerable<FloatMenuOption> worldOptions = CompLaunchable.GetOptionsForTile(
                tile,
                holders,
                delegate(PlanetTile destinationTile, TransportersArrivalAction arrivalAction)
                {
                    TransportersArrivalAction wrappedArrivalAction =
                        ModularShuttleVisitSiteArrivalUtility.WrapVisitSiteArrivalForModularShuttle(
                            arrivalAction,
                            shuttle);
                    ShuttleLaunchResult launchResult = ExecuteLaunch(
                        caravan,
                        shuttle,
                        destinationTile,
                        wrappedArrivalAction);
                    if (!launchResult.Success)
                    {
                        Messages.Message(launchResult.Message, MessageTypeDefOf.RejectInput, false);
                    }
                });
            if (worldOptions != null && !hasSupportedCaravanWorldObject)
            {
                options.AddRange(worldOptions);
            }

            List<WorldObject> worldObjects = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < worldObjects.Count; i++)
            {
                WorldObject worldObject = worldObjects[i];
                if (worldObject == null || worldObject.Tile != tile)
                {
                    continue;
                }

                IEnumerable<FloatMenuOption> caravanWorldObjectOptions =
                    ModularShuttleCaravanWorldObjectCompatUtility.GetFloatMenuOptions(
                        worldObject,
                        holders,
                        delegate(PlanetTile destinationTile, TransportersArrivalAction arrivalAction)
                        {
                            ShuttleLaunchResult launchResult = ExecuteLaunch(
                                caravan,
                                shuttle,
                                destinationTile,
                                arrivalAction);
                            if (!launchResult.Success)
                            {
                                Messages.Message(launchResult.Message, MessageTypeDefOf.RejectInput, false);
                            }
                        });
                if (caravanWorldObjectOptions != null)
                {
                    options.AddRange(caravanWorldObjectOptions);
                }
            }

            return options;
        }

        private static ShuttleLaunchResult ExecuteLaunch(
            Caravan caravan,
            ThingWithComps shuttle,
            PlanetTile destinationTile,
            TransportersArrivalAction arrivalAction)
        {
            ShuttleFlightEnergyQuote quote;
            ShuttleLaunchResult validation = ValidateDestination(caravan, shuttle, destinationTile, out quote);
            if (!validation.Success)
            {
                return validation;
            }

            ShuttleController controller = GetController(shuttle);
            ShuttleRuntimeState runtimeState = controller.GetLaunchRuntimeState();
            ShuttleProfile profile = controller.BuildLaunchProfile();
            int previousLastLaunchTick = runtimeState.Launch.LastLaunchTick;
            int previousCooldownEndTick = runtimeState.Launch.CooldownEndTick;

            WorldObjectDef worldObjectDef = DefDatabase<WorldObjectDef>.GetNamedSilentFail(WorldObjectDefName);
            if (worldObjectDef == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_WorldObjectDefUnavailable".Translate().ToString(), quote);
            }

            ActiveTransporterInfo transporter = new ActiveTransporterInfo();
            transporter.sentTransporterDef = shuttle.def;
            Pawn owner = CaravanInventoryUtility.GetOwnerOf(caravan, shuttle);
            if (owner == null || owner.inventory == null || owner.inventory.innerContainer == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_HostUnavailable".Translate().ToString(), quote);
            }

            if (!TryConsumeLaunchEnergy(runtimeState, profile, quote, out previousLastLaunchTick, out previousCooldownEndTick))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_StoredChargeInsufficient".Translate().ToString(), quote);
            }

            ShuttlePassengerWeaponCargoTransfer.TransferReceipt passengerWeaponReceipt = null;
            try
            {
                Find.WorldTargeter.StopTargeting();
                int movedWeaponCount;
                string passengerWeaponFailure;
                if (!ShuttlePassengerWeaponCargoTransfer.TryMoveFromPawns(
                    caravan.pawns,
                    transporter.innerContainer,
                    out passengerWeaponReceipt,
                    out movedWeaponCount,
                    out passengerWeaponFailure))
                {
                    RollbackRuntime(
                        runtimeState,
                        profile,
                        quote.RequiredEnergyWd,
                        previousLastLaunchTick,
                        previousCooldownEndTick);
                    return ShuttleLaunchResult.Failed(
                        "CT_Shuttle_Launch_Failed_CargoHandoffFailed".Translate(
                            passengerWeaponFailure ?? "Passenger weapon cargo transfer failed.").ToString(),
                        quote);
                }

                owner.inventory.innerContainer.Remove(shuttle);
                CaravanShuttleUtility.LoadCaravanItemsIntoContainer(caravan.pawns, transporter.innerContainer);
                transporter.innerContainer.TryAddRangeOrTransfer(caravan.pawns, true, false);
                transporter.SetShuttle(shuttle);

                TravellingTransporters travellingTransporters =
                    (TravellingTransporters)WorldObjectMaker.MakeWorldObject(worldObjectDef);
                travellingTransporters.SetFaction(Faction.OfPlayer);
                travellingTransporters.destinationTile = destinationTile;
                travellingTransporters.arrivalAction =
                    ModularShuttleSafeArrivalAction.Wrap(arrivalAction, shuttle);
                travellingTransporters.Tile = GetLaunchOriginTile(caravan.Tile, destinationTile);
                travellingTransporters.AddTransporter(transporter, true);
                Find.WorldObjects.Add(travellingTransporters);
                caravan.Destroy();
                CameraJumper.TryJump(travellingTransporters, CameraJumper.MovementMode.Pan);
                return ShuttleLaunchResult.Succeeded("CT_Shuttle_Launch_Succeeded".Translate().ToString(), quote);
            }
            catch (System.Exception exception)
            {
                string passengerWeaponRollbackFailure;
                if (passengerWeaponReceipt != null &&
                    !passengerWeaponReceipt.TryRollback(out passengerWeaponRollbackFailure))
                {
                    Log.Error("[CeleTech Shuttle] Caravan passenger weapon cargo rollback failed: " +
                        passengerWeaponRollbackFailure);
                }

                RollbackRuntime(runtimeState, profile, quote.RequiredEnergyWd, previousLastLaunchTick, previousCooldownEndTick);
                Log.Error("[CeleTech Shuttle] Caravan-held modular shuttle launch failed after validation: " + exception);
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_SkyfallerCreateFailedWithReason".Translate(exception.Message).ToString(), quote);
            }
        }

        private static ShuttleLaunchResult ValidateDestination(
            Caravan caravan,
            ThingWithComps shuttle,
            PlanetTile destinationTile,
            out ShuttleFlightEnergyQuote quote)
        {
            quote = null;
            ShuttleController controller = GetController(shuttle);
            if (controller == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_ProfileUnavailable".Translate().ToString());
            }

            ShuttleProfile profile = controller.BuildLaunchProfile();
            ShuttleLaunchResult basicResult = ValidateBasicTarget(
                caravan,
                shuttle,
                destinationTile,
                profile);
            if (!basicResult.Success)
            {
                return basicResult;
            }

            ShuttleRuntimeState runtimeState = controller.GetLaunchRuntimeState();
            ShuttleCargoSnapshot launchCargoSnapshot = controller.BuildLaunchCargoSnapshot();
            ShuttleLaunchResult stateResult =
                ValidateStateForLaunch(caravan, shuttle, profile, runtimeState, launchCargoSnapshot);
            if (!stateResult.Success)
            {
                return stateResult;
            }

            ShuttleCargoSnapshot cargoSnapshot =
                ModularShuttleCaravanUtility.BuildCaravanCargoSnapshot(
                    caravan,
                    shuttle,
                    profile,
                    launchCargoSnapshot);
            quote = EnergyCalculator.Quote(
                profile,
                runtimeState,
                cargoSnapshot,
                GetDistanceTiles(caravan.Tile, destinationTile),
                GetRangeDistanceFactor(destinationTile));
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

        private static ShuttleLaunchResult ValidateLaunchReadiness(Caravan caravan, ThingWithComps shuttle)
        {
            if (caravan == null || shuttle == null || shuttle.Destroyed)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_HostUnavailable".Translate().ToString());
            }

            if (!caravan.Spawned || !caravan.Tile.Valid)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_OriginTileInvalid".Translate().ToString());
            }

            ShuttleController controller = GetController(shuttle);
            if (controller == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_ProfileUnavailable".Translate().ToString());
            }

            return ValidateStateForLaunch(
                caravan,
                shuttle,
                controller.BuildLaunchProfile(),
                controller.GetLaunchRuntimeState(),
                controller.BuildLaunchCargoSnapshot());
        }

        private static ShuttleLaunchResult ValidateBasicTarget(
            Caravan caravan,
            ThingWithComps shuttle,
            PlanetTile destinationTile,
            ShuttleProfile profile)
        {
            if (caravan == null || shuttle == null || shuttle.Destroyed)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_HostUnavailable".Translate().ToString());
            }

            if (!caravan.Spawned || !caravan.Tile.Valid)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_OriginTileInvalid".Translate().ToString());
            }

            if (!destinationTile.Valid)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_DestinationTileInvalid".Translate().ToString());
            }

            if (caravan.Tile.Layer != destinationTile.Layer &&
                !caravan.Tile.Layer.HasConnectionPathTo(destinationTile.Layer))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_NoWorldPath".Translate().ToString());
            }

            WorldObject worldObject = Find.WorldObjects.WorldObjectAt<WorldObject>(destinationTile);
            if (worldObject != null &&
                !worldObject.def.validLaunchTarget &&
                !ModularShuttleCaravanWorldObjectCompatUtility.HasSupportedWorldObjectAt(destinationTile))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_InvalidWorldObjectTarget".Translate().ToString());
            }

            if (!ShuttleSignalJammerAccessPolicy.CanReach(worldObject, profile))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_SignalJammerRequired".Translate().ToString());
            }

            if (Find.World.Impassable(destinationTile))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_DestinationNotLaunchable".Translate().ToString());
            }

            return ShuttleLaunchResult.Succeeded("CT_Shuttle_Launch_TargetValid".Translate().ToString(), null);
        }

        private static ShuttleLaunchResult ValidateStateForLaunch(
            Caravan caravan,
            ThingWithComps shuttle,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ShuttleCargoSnapshot launchCargoSnapshot)
        {
            if (profile == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_ProfileUnavailable".Translate().ToString());
            }

            if (runtimeState == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_RuntimeUnavailable".Translate().ToString());
            }

            runtimeState.EnsureInitialized();
            if (runtimeState.MedicalProcedures != null && runtimeState.MedicalProcedures.HasActiveProcedure)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_ActiveMedicalProcedure".Translate().ToString());
            }

            int ticksGame = ShuttleTickUtility.TicksGameOrZero();
            if (GetLaunchCooldownTicks(profile) > 0 &&
                runtimeState.Launch.CooldownEndTick > ticksGame)
            {
                int remainingTicks = runtimeState.Launch.CooldownEndTick - ticksGame;
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_CooldownRemaining".Translate(
                    remainingTicks.ToStringTicksToPeriod(true, false, true, true, false)).ToString());
            }

            if (!ModularShuttleCaravanUtility.HasLaunchController(caravan, launchCargoSnapshot, profile))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_CockpitColonistRequired".Translate().ToString());
            }

            string manifestFailureReason;
            if (!ModularShuttleCaravanManifestAudit.TryValidateTransitManifestReachability(
                caravan,
                shuttle,
                out manifestFailureReason))
            {
                Log.Error("[CeleTech Shuttle] Caravan-held shuttle launch blocked by unreachable holder manifest: " +
                    (manifestFailureReason ?? "unknown"));
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_HolderTransferManifestActive".Translate().ToString());
            }

            ProfileBuildIssue issue = GetFirstProfileError(profile);
            if (issue != null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_AssemblyNotReady".Translate(issue.Message).ToString());
            }

            return ShuttleLaunchResult.Succeeded("CT_Shuttle_Launch_Validated".Translate().ToString(), null);
        }

        private static bool TryConsumeLaunchEnergy(
            ShuttleRuntimeState runtimeState,
            ShuttleProfile profile,
            ShuttleFlightEnergyQuote quote,
            out int previousLastLaunchTick,
            out int previousCooldownEndTick)
        {
            previousLastLaunchTick = runtimeState.Launch.LastLaunchTick;
            previousCooldownEndTick = runtimeState.Launch.CooldownEndTick;
            if (runtimeState.Power.StoredEnergyWd + Epsilon < quote.RequiredEnergyWd)
            {
                return false;
            }

            runtimeState.Power.StoredEnergyWd -= quote.RequiredEnergyWd;
            if (runtimeState.Power.StoredEnergyWd < 0f)
            {
                runtimeState.Power.StoredEnergyWd = 0f;
            }

            int ticksGame = ShuttleTickUtility.TicksGameOrZero();
            runtimeState.Launch.LastLaunchTick = ticksGame;
            runtimeState.Launch.CooldownEndTick = ticksGame + GetLaunchCooldownTicks(profile);
            return true;
        }

        private static void RollbackRuntime(
            ShuttleRuntimeState runtimeState,
            ShuttleProfile profile,
            float refundEnergyWd,
            int previousLastLaunchTick,
            int previousCooldownEndTick)
        {
            if (runtimeState == null)
            {
                return;
            }

            runtimeState.EnsureInitialized();
            runtimeState.Power.StoredEnergyWd += refundEnergyWd;
            float capacity = profile != null && profile.Power != null ? profile.Power.EnergyStorageCapacityWd : runtimeState.Power.StoredEnergyWd;
            if (runtimeState.Power.StoredEnergyWd > capacity)
            {
                runtimeState.Power.StoredEnergyWd = capacity;
            }

            runtimeState.Launch.LastLaunchTick = previousLastLaunchTick;
            runtimeState.Launch.CooldownEndTick = previousCooldownEndTick;
        }

        private static ProfileBuildIssue GetFirstProfileError(ShuttleProfile profile)
        {
            if (profile == null || profile.Issues == null)
            {
                return null;
            }

            for (int i = 0; i < profile.Issues.Count; i++)
            {
                ProfileBuildIssue issue = profile.Issues[i];
                if (issue != null && issue.Severity == ProfileBuildIssueSeverity.Error)
                {
                    return issue;
                }
            }

            return null;
        }

        private static ShuttleController GetController(ThingWithComps shuttle)
        {
            CompModularShuttleCore core = shuttle != null
                ? shuttle.TryGetComp<CompModularShuttleCore>()
                : null;
            return core != null ? core.Controller : null;
        }

        private static int GetDistanceTiles(PlanetTile originTile, PlanetTile destinationTile)
        {
            return Find.WorldGrid.TraversalDistanceBetween(originTile, destinationTile, true, int.MaxValue, true);
        }

        private static float GetRangeDistanceFactor(PlanetTile destinationTile)
        {
            if (!destinationTile.Valid || destinationTile.Layer == null || destinationTile.Layer.Def == null)
            {
                return 1f;
            }

            return destinationTile.Layer.Def.rangeDistanceFactor;
        }

        private static int GetLaunchCooldownTicks(ShuttleProfile profile)
        {
            return profile != null && profile.Flight != null && profile.Flight.LaunchCooldownTicks >= 0
                ? profile.Flight.LaunchCooldownTicks
                : 0;
        }

        private static PlanetTile GetLaunchOriginTile(PlanetTile originTile, PlanetTile destinationTile)
        {
            if (originTile.Layer == destinationTile.Layer)
            {
                return originTile;
            }

            return destinationTile.Layer.GetClosestTile_NewTemp(originTile, false);
        }
    }
}

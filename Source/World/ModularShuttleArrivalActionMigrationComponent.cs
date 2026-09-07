using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using CeleTech.ShuttleExtension.ModularShuttle.Patches;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.External;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.World
{
    public sealed class ModularShuttleArrivalActionMigrationComponent : GameComponent
    {
        private readonly Game game;
        private bool loadedGameMigrationDone;

        public ModularShuttleArrivalActionMigrationComponent(Game game)
        {
            this.game = game;
        }

        public override void LoadedGame()
        {
            ClearStaticRuntimeCaches();
            this.MigrateExistingInFlightArrivalActions("loaded-game");
            this.loadedGameMigrationDone = true;
        }

        public override void StartedNewGame()
        {
            ClearStaticRuntimeCaches();
        }

        public override void GameComponentTick()
        {
            if (this.loadedGameMigrationDone || Find.TickManager == null || Find.TickManager.TicksGame % 250 != 0)
            {
                return;
            }

            this.MigrateExistingInFlightArrivalActions("tick-safety");
            this.loadedGameMigrationDone = true;
        }

        private void MigrateExistingInFlightArrivalActions(string reason)
        {
            int changed = 0;
            changed += this.MigrateTravellingTransporters();
            changed += this.MigrateLeavingSkyfallers();

            if (changed > 0 && Prefs.DevMode)
            {
                Log.Message(
                    "[CeleTech Shuttle] Migrated modular shuttle VisitSite arrival actions to safe landing mode. " +
                    "count=" +
                    changed +
                    " reason=" +
                    reason);
            }
        }

        private static void ClearStaticRuntimeCaches()
        {
            ShuttleJoyKindMapCache.Clear();
            ShuttleAssemblyWorkEffectUtility.ClearAllEffecters();
            ExternalRuntimeMetricsRegistry.Clear();
            ExternalModulePanelGuard.ClearSessionState();
            ModularShuttleCaravanUtility.ClearCachedHeldModularShuttle();
        }

        private int MigrateTravellingTransporters()
        {
            if (Find.WorldObjects == null)
            {
                return 0;
            }

            int changed = 0;
            List<WorldObject> worldObjects = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < worldObjects.Count; i++)
            {
                TravellingTransporters travellingTransporters = worldObjects[i] as TravellingTransporters;
                if (travellingTransporters == null ||
                    !ModularShuttleVisitSiteArrivalUtility.ContainsModularShuttle(travellingTransporters))
                {
                    continue;
                }

                TransportersArrivalAction visitWrapped =
                    ModularShuttleVisitSiteArrivalUtility.WrapVisitSiteArrival(travellingTransporters.arrivalAction);
                TransportersArrivalAction wrapped =
                    ModularShuttleSafeArrivalAction.Wrap(visitWrapped, travellingTransporters);
                if (!ReferenceEquals(wrapped, travellingTransporters.arrivalAction))
                {
                    travellingTransporters.arrivalAction = wrapped;
                    changed++;
                }
            }

            return changed;
        }

        private int MigrateLeavingSkyfallers()
        {
            if (this.game == null || this.game.Maps == null)
            {
                return 0;
            }

            int changed = 0;
            for (int i = 0; i < this.game.Maps.Count; i++)
            {
                Map map = this.game.Maps[i];
                if (map == null || map.listerThings == null)
                {
                    continue;
                }

                List<Thing> activeTransporters = map.listerThings.ThingsInGroup(ThingRequestGroup.ActiveTransporter);
                for (int j = 0; j < activeTransporters.Count; j++)
                {
                    ModularShuttleLeaving leaving = activeTransporters[j] as ModularShuttleLeaving;
                    if (leaving == null || leaving.Contents == null)
                    {
                        continue;
                    }

                    TransportersArrivalAction visitWrapped =
                        ModularShuttleVisitSiteArrivalUtility.WrapVisitSiteArrivalForModularShuttle(
                            leaving.arrivalAction,
                            leaving.Contents.GetShuttle());
                    TransportersArrivalAction wrapped =
                        ModularShuttleSafeArrivalAction.Wrap(
                            visitWrapped,
                            leaving.Contents.GetShuttle());
                    if (!ReferenceEquals(wrapped, leaving.arrivalAction))
                    {
                        leaving.arrivalAction = wrapped;
                        changed++;
                    }
                }
            }

            return changed;
        }
    }
}

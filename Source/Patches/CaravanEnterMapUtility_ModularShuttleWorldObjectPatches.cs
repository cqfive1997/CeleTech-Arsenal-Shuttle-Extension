using System;
using System.Collections.Generic;
using System.Reflection;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using CeleTech.ShuttleExtension.ModularShuttle.World;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Patches
{
    [HarmonyPatch]
    internal static class CaravanEnterMapUtility_ModularShuttleWorldObjectEnterPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(CaravanEnterMapUtility),
                nameof(CaravanEnterMapUtility.Enter),
                new Type[]
                {
                    typeof(Caravan),
                    typeof(Map),
                    typeof(Func<Pawn, IntVec3>),
                    typeof(CaravanDropInventoryMode),
                    typeof(bool)
                });
        }

        private static void Prefix(Caravan caravan, Map map, ref LandingState __state)
        {
            __state = null;
            if (caravan == null ||
                map == null ||
                !ModularShuttleCaravanWorldObjectArrivalRegistry.IsTracked(caravan))
            {
                return;
            }

            ThingWithComps shuttle;
            Pawn owner;
            if (!ModularShuttleCaravanUtility.TryRemoveHeldModularShuttle(caravan, out shuttle, out owner))
            {
                return;
            }

            __state = new LandingState(caravan, map, shuttle, caravan.PawnsListForReading);
        }

        private static void Postfix(LandingState __state)
        {
            if (__state == null)
            {
                return;
            }

            try
            {
                DropTrackedShuttle(__state);
            }
            finally
            {
                ModularShuttleCaravanWorldObjectArrivalRegistry.Unregister(__state.Caravan);
            }
        }

        private static void DropTrackedShuttle(LandingState state)
        {
            if (state == null || state.Shuttle == null || state.Map == null)
            {
                return;
            }

            ActiveTransporterInfo transporter = new ActiveTransporterInfo();
            transporter.sentTransporterDef = state.Shuttle.def;
            transporter.SetShuttle(state.Shuttle);

            List<ActiveTransporterInfo> transporters = new List<ActiveTransporterInfo>();
            transporters.Add(transporter);

            IntVec3 preferredCell = state.GetPreferredLandingCell();
            try
            {
                Thing dropped = ModularShuttleVisitSiteArrivalUtility.DropModularShuttleAtSafeCell(
                    transporters,
                    state.Map,
                    preferredCell);
                if (dropped == null)
                {
                    ReturnShuttleToPawnInventory(state, state.Shuttle);
                    Log.Error("[CeleTech Shuttle] Failed to place modular shuttle on caravan-world-object attack map; returned it to pawn inventory.");
                }
            }
            catch (Exception exception)
            {
                ReturnShuttleToPawnInventory(state, state.Shuttle);
                Log.Error("[CeleTech Shuttle] Exception while placing modular shuttle on caravan-world-object attack map; returned it to pawn inventory. exception=" +
                    exception);
            }
        }

        private static void ReturnShuttleToPawnInventory(LandingState state, ThingWithComps shuttle)
        {
            if (state == null || shuttle == null)
            {
                return;
            }

            Pawn pawn = state.GetFirstSpawnedPawn();
            if (pawn != null && pawn.inventory != null && pawn.inventory.innerContainer != null)
            {
                pawn.inventory.innerContainer.TryAddOrTransfer(shuttle, true);
            }
        }

        private sealed class LandingState
        {
            internal readonly Caravan Caravan;
            internal readonly Map Map;
            internal readonly ThingWithComps Shuttle;
            private readonly List<Pawn> pawns;

            internal LandingState(
                Caravan caravan,
                Map map,
                ThingWithComps shuttle,
                List<Pawn> sourcePawns)
            {
                this.Caravan = caravan;
                this.Map = map;
                this.Shuttle = shuttle;
                this.pawns = sourcePawns != null
                    ? new List<Pawn>(sourcePawns)
                    : new List<Pawn>();
            }

            internal IntVec3 GetPreferredLandingCell()
            {
                Pawn pawn = this.GetFirstSpawnedPawn();
                return pawn != null ? pawn.Position : IntVec3.Invalid;
            }

            internal Pawn GetFirstSpawnedPawn()
            {
                for (int i = 0; i < this.pawns.Count; i++)
                {
                    Pawn pawn = this.pawns[i];
                    if (pawn != null &&
                        !pawn.Destroyed &&
                        pawn.Spawned &&
                        pawn.Map == this.Map)
                    {
                        return pawn;
                    }
                }

                return null;
            }
        }
    }
}

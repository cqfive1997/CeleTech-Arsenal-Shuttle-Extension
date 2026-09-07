using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static partial class ShuttleHolderLaunchTransferService
    {
        internal static void LogHabitatLivingArrivalDiagnostics(
            ThingWithComps shuttleHost,
            PlanetTile destinationTile,
            TransportersArrivalAction arrivalAction,
            string context,
            bool? canUseHabitatLivingTransfer,
            string failureReason,
            string transferMode = null)
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            string actionType = arrivalAction != null ? arrivalAction.GetType().FullName : "null";
            string actionString = arrivalAction != null ? arrivalAction.ToString() : "null";
            ThingDef incomingDef = DefDatabase<ThingDef>.GetNamedSilentFail(IncomingSkyfallerDefName);
            string incomingDefName = incomingDef != null ? incomingDef.defName : "null";
            string worldObjectDefs = FormatWorldObjectsAt(destinationTile);
            bool supportedArrival = IsSupportedHabitatLivingArrivalAction(arrivalAction);

            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            CompShuttleMedicalBayOccupancy medicalBay = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>()
                : null;

            int sleepCount = habitat != null ? habitat.SleepingOccupantCount : 0;
            int diningCount = habitat != null ? habitat.DiningOccupantCount : 0;
            int joyCount = habitat != null ? habitat.JoyOccupantCount : 0;
            int totalCount = habitat != null ? habitat.TotalOccupantCount : 0;
            int unknownCount = totalCount - sleepCount - diningCount - joyCount;
            if (unknownCount < 0)
            {
                unknownCount = 0;
            }

            bool canUse = canUseHabitatLivingTransfer.HasValue && canUseHabitatLivingTransfer.Value;
            string canUseText = canUseHabitatLivingTransfer.HasValue
                ? canUse.ToString()
                : "not-evaluated";

            Log.Message(
                "[CeleTech Shuttle] Living Habitat arrival action diagnostic." +
                " context=" + (context ?? "null") +
                " destinationTile=" + (destinationTile.Valid ? destinationTile.ToString() : "invalid") +
                " worldObjects=" + worldObjectDefs +
                " arrivalActionType=" + actionType +
                " arrivalAction=" + actionString +
                " incomingSkyfallerDef=" + incomingDefName +
                " supportedArrivalAction=" + supportedArrival +
                " transferMode=" + (transferMode ?? "unknown") +
                " canUseHabitatLivingTransfer=" + canUseText +
                " failureReason=" + (failureReason ?? "null") +
                " habitatSleep=" + sleepCount +
                " habitatDining=" + diningCount +
                " habitatJoy=" + joyCount +
                " habitatUnknown=" + unknownCount +
                " habitatTotal=" + totalCount +
                " medicalBayPatients=" + (medicalBay != null ? medicalBay.PatientCount : 0) +
                " activeManifest=" + (state != null && state.HasActiveManifest));
        }

        private static string FormatWorldObjectsAt(PlanetTile tile)
        {
            if (!tile.Valid || Find.WorldObjects == null)
            {
                return "none";
            }

            string result = null;
            List<WorldObject> worldObjects = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < worldObjects.Count; i++)
            {
                WorldObject worldObject = worldObjects[i];
                if (worldObject == null || worldObject.Tile != tile)
                {
                    continue;
                }

                string defName = worldObject.def != null ? worldObject.def.defName : "null";
                result = result == null ? defName : result + "," + defName;
            }

            return result ?? "none";
        }
    }
}

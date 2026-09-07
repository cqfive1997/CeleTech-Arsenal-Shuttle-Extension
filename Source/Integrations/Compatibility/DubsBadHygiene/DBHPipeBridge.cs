using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CeleTech.ShuttleExtension.AdditionalModule;
using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.DubsBadHygiene
{
    internal sealed class DBHPipeBridge
    {
        private const int MaxPipeSearchRadius = 4;

        private readonly CT_Shuttle_DBHIntegrationDef integration;
        private readonly DBHPipeReflectionBridge reflectionBridge;

        public DBHPipeBridge(CT_Shuttle_DBHIntegrationDef integration)
        {
            this.integration = integration;
            this.reflectionBridge = new DBHPipeReflectionBridge(integration);
        }

        public DBHPipeBridgeResult CheckAndService(
            ShuttleExternalRuntimeContext context,
            float cleanWater,
            float cleanWaterCapacity,
            float sewage,
            float sewageCapacity)
        {
            DBHPipeBridgeResult result = NewResult(cleanWater, sewage);

            if (!DBHCompatibility.IsLoaded)
            {
                result.Result = Tr("CT_Shuttle_Addon_DBH_Block_DBHMissing");
                result.BlockedReason = Tr("CT_Shuttle_Addon_DBH_Block_DBHMissing");
                result.BridgeMode = Tr("CT_Shuttle_Addon_DBH_PipeMode_DbhMissing");
                return result;
            }

            if (context == null)
            {
                result.Result = Tr("CT_Shuttle_Addon_DBH_Pipe_RuntimeContextMissing");
                result.BlockedReason = Tr("CT_Shuttle_Addon_DBH_Pipe_RuntimeContextMissing");
                result.BridgeMode = Tr("CT_Shuttle_Addon_DBH_PipeMode_NoContext");
                return result;
            }

            if (integration == null)
            {
                result.Result = Tr("CT_Shuttle_Addon_DBH_Pipe_IntegrationMissing");
                result.BlockedReason = Tr("CT_Shuttle_Addon_DBH_Pipe_IntegrationMissing");
                result.BridgeMode = Tr("CT_Shuttle_Addon_DBH_PipeMode_NoIntegration");
                return result;
            }

            if (integration != null &&
                !integration.allowPipeRefill &&
                !integration.allowPipeSewageDrain)
            {
                result.Result = Tr("CT_Shuttle_Addon_DBH_Pipe_ServiceDisabled");
                result.BlockedReason = null;
                result.BridgeMode = Tr("CT_Shuttle_Addon_DBH_PipeMode_Disabled");
                return result;
            }

            ShuttleExternalHostInfo hostInfo;
            if (!TryGetHostInfo(context, out hostInfo, result))
            {
                return result;
            }

            DBHPipeSearchCells searchCells = BuildPipeSearchCells(hostInfo);
            result.CheckedOccupiedCells = searchCells.OccupiedCells.Count;
            result.CheckedAdjacentCells = searchCells.AdjacentCells.Count;
            if (searchCells.Count == 0)
            {
                result.Result = Tr("CT_Shuttle_Addon_DBH_Pipe_NoExternalCells");
                result.BlockedReason = null;
                result.BridgeMode = Tr("CT_Shuttle_Addon_DBH_PipeMode_NotConnected");
                return result;
            }

            // DBH 3.0.2436 models its shipped hygiene plumbing as one PipeType.Sewage
            // PlumbingNet. That net owns both WaterTowers and Sewers, and exposes both
            // PullWater and PushSewage. PipeType.Water exists in the enum for graphics and
            // compatibility, but sewagePipeStuff/sewagePipeHidden and all shipped hygiene
            // fixtures use PipeType.Sewage.
            DBHPipeConnection plumbingConnection;
            string plumbingConnectionReason;
            reflectionBridge.TryFindPipeConnection(
                hostInfo.UnsafeLiveMap,
                searchCells,
                DBHPipeNetworkKind.Sewage,
                out plumbingConnection,
                out plumbingConnectionReason);
            CaptureSearchDiagnostics(result, reflectionBridge);

            DBHPipeConnection waterConnection = integration.allowPipeRefill
                ? plumbingConnection
                : null;
            DBHPipeConnection sewageConnection = integration.allowPipeSewageDrain
                ? plumbingConnection
                : null;
            string waterConnectionReason = integration.allowPipeRefill
                ? plumbingConnectionReason
                : null;
            string sewageConnectionReason = integration.allowPipeSewageDrain
                ? plumbingConnectionReason
                : null;
            ApplyConnectionDetails(result, waterConnection, true);
            ApplyConnectionDetails(result, sewageConnection, false);

            result.WaterPipeConnected = waterConnection != null;
            result.SewagePipeConnected = sewageConnection != null;
            result.PipeConnected = result.WaterPipeConnected || result.SewagePipeConnected;
            result.ResolveFailureReason = reflectionBridge.ResolveFailureReason;

            if (reflectionBridge.ResolveFailed)
            {
                result.Result = reflectionBridge.ResolveFailureReason;
                result.BlockedReason = reflectionBridge.ResolveFailureReason;
                result.BridgeMode = Tr("CT_Shuttle_Addon_DBH_PipeMode_DetectedBridgeUnavailable");
                return result;
            }

            if (!result.PipeConnected)
            {
                result.Result = BuildNoConnectionResult(waterConnectionReason, sewageConnectionReason);
                result.BlockedReason = null;
                result.BridgeMode = Tr("CT_Shuttle_Addon_DBH_PipeMode_NotConnected");
                return result;
            }

            bool allEnabledConnectionsPresent =
                (!integration.allowPipeRefill || result.WaterPipeConnected) &&
                (!integration.allowPipeSewageDrain || result.SewagePipeConnected);
            result.BridgeMode = allEnabledConnectionsPresent
                ? Tr("CT_Shuttle_Addon_DBH_PipeMode_Connected")
                : Tr("CT_Shuttle_Addon_DBH_PipeMode_Partial");

            if (integration.allowPipeRefill && !result.WaterPipeConnected)
            {
                AppendResult(result, Tr("CT_Shuttle_Addon_DBH_Pipe_NoWaterConnection"));
            }

            if (integration.allowPipeSewageDrain && !result.SewagePipeConnected)
            {
                AppendResult(result, Tr("CT_Shuttle_Addon_DBH_Pipe_NoSewageConnection"));
            }

            bool didService = false;

            if (result.WaterPipeConnected &&
                cleanWater < cleanWaterCapacity &&
                integration.pipeFillRatePerService > 0f)
            {
                float waterToPull = Min(integration.pipeFillRatePerService, cleanWaterCapacity - cleanWater);
                string contamination;
                string pullReason;
                // DBH 1.6 PlumbingNet.PullWater is all-or-nothing: it returns false before
                // mutation when total storage is below the requested amount, otherwise the
                // requested amount is fully deducted from water towers.
                if (reflectionBridge.TryPullWater(waterConnection.PipeNet, waterToPull, out contamination, out pullReason))
                {
                    result.CleanWater = cleanWater + waterToPull;
                    result.WaterAdded = waterToPull;
                    result.LastContaminationLevel = contamination;
                    AppendResult(result, Tr("CT_Shuttle_Addon_DBH_Pipe_RefilledWater", FormatAmount(waterToPull)));
                    didService = true;
                }
                else
                {
                    AppendResult(result, pullReason);
                    result.BlockedReason = pullReason;
                }
            }
            else if (result.WaterPipeConnected && cleanWater >= cleanWaterCapacity)
            {
                AppendResult(result, Tr("CT_Shuttle_Addon_DBH_Pipe_CleanWaterAlreadyFull"));
            }
            else if (result.WaterPipeConnected && integration.pipeFillRatePerService <= 0f)
            {
                AppendResult(result, Tr("CT_Shuttle_Addon_DBH_Pipe_FillRateNotPositive"));
            }

            if (result.SewagePipeConnected &&
                sewage > 0f &&
                integration.pipeSewageDrainRatePerService > 0f)
            {
                float sewageToPush = Min(integration.pipeSewageDrainRatePerService, sewage);
                string pushReason;
                // DBH 1.6 PlumbingNet.PushSewage is also all-or-nothing from the caller's
                // perspective: it returns false when no unblocked sewer exists, otherwise
                // the full requested amount is split into sewer buffers.
                if (reflectionBridge.TryPushSewage(sewageConnection.PipeNet, sewageToPush, out pushReason))
                {
                    result.Sewage = sewage - sewageToPush;
                    result.SewageDrained = sewageToPush;
                    AppendResult(result, Tr("CT_Shuttle_Addon_DBH_Pipe_DrainedSewage", FormatAmount(sewageToPush)));
                    didService = true;
                }
                else
                {
                    AppendResult(result, pushReason);
                    if (string.IsNullOrEmpty(result.BlockedReason))
                    {
                        result.BlockedReason = pushReason;
                    }
                }
            }

            if (!didService && string.IsNullOrEmpty(result.Result))
            {
                result.Result = Tr("CT_Shuttle_Addon_DBH_Pipe_Connected");
            }

            return result;
        }

        public DBHPipeBridgeResult TryDrainSewage(
            ShuttleExternalRuntimeContext context,
            float cleanWater,
            float cleanWaterCapacity,
            float sewage,
            float sewageCapacity)
        {
            DBHPipeBridgeResult result = CheckAndService(context, cleanWater, cleanWaterCapacity, sewage, sewageCapacity);
            if (result.SewagePipeConnected && result.SewageDrained <= 0f && sewage > 0f)
            {
                result.BlockedReason = string.IsNullOrEmpty(result.BlockedReason)
                    ? "CT_Shuttle_Addon_Pipe_NoSewageFacility".Translate().ToString()
                    : result.BlockedReason;
            }
            else if (!result.SewagePipeConnected && sewage > 0f)
            {
                result.BlockedReason = Tr("CT_Shuttle_Addon_DBH_Pipe_NoSewageConnection");
            }

            return result;
        }

        private static DBHPipeBridgeResult NewResult(float cleanWater, float sewage)
        {
            return new DBHPipeBridgeResult
            {
                CleanWater = cleanWater,
                Sewage = sewage,
                WaterAdded = 0f,
                SewageDrained = 0f,
                BridgeMode = Tr("CT_Shuttle_Addon_DBH_PipeMode_NotChecked")
            };
        }

        private static bool TryGetHostInfo(
            ShuttleExternalRuntimeContext context,
            out ShuttleExternalHostInfo hostInfo,
            DBHPipeBridgeResult result)
        {
            hostInfo = null;
            if (!context.SupportsHostRead)
            {
                result.Result = Tr("CT_Shuttle_Addon_DBH_Pipe_HostApiMissing");
                result.BlockedReason = Tr("CT_Shuttle_Addon_DBH_Pipe_HostApiMissing");
                result.BridgeMode = Tr("CT_Shuttle_Addon_DBH_PipeMode_NoHostApi");
                return false;
            }

            if (!context.TryGetHostInfo(out hostInfo) || hostInfo == null)
            {
                result.Result = Tr("CT_Shuttle_Addon_DBH_Pipe_HostInfoMissing");
                result.BlockedReason = Tr("CT_Shuttle_Addon_DBH_Pipe_HostInfoMissing");
                result.BridgeMode = Tr("CT_Shuttle_Addon_DBH_PipeMode_NoHostInfo");
                return false;
            }

            if (hostInfo.UnsafeLiveMap == null || !hostInfo.Spawned)
            {
                result.Result = Tr("CT_Shuttle_Addon_DBH_Pipe_ShuttleNotSpawned");
                result.BlockedReason = null;
                result.BridgeMode = Tr("CT_Shuttle_Addon_DBH_PipeMode_ShuttleNotSpawned");
                return false;
            }

            return true;
        }

        private DBHPipeSearchCells BuildPipeSearchCells(ShuttleExternalHostInfo hostInfo)
        {
            DBHPipeSearchCells result = new DBHPipeSearchCells();
            Map map = hostInfo != null ? hostInfo.UnsafeLiveMap : null;
            if (hostInfo == null || map == null)
            {
                return result;
            }

            HashSet<IntVec3> occupied = new HashSet<IntVec3>();
            if (hostInfo.OccupiedCells != null)
            {
                for (int i = 0; i < hostInfo.OccupiedCells.Count; i++)
                {
                    AddCell(hostInfo.OccupiedCells[i], map, null, occupied, result.OccupiedCells);
                }
            }

            HashSet<IntVec3> adjacentSeen = new HashSet<IntVec3>();
            int radius = ClampSearchRadius(integration != null ? integration.pipeSearchRadius : 1);
            if (radius <= 1 || (integration != null && integration.requireAdjacentPipe))
            {
                AddCells(hostInfo.AdjacentExternalCells, map, occupied, adjacentSeen, result.AdjacentCells);
                return result;
            }

            if (hostInfo.AdjacentExternalCells == null)
            {
                return result;
            }

            for (int i = 0; i < hostInfo.AdjacentExternalCells.Count; i++)
            {
                IntVec3 center = hostInfo.AdjacentExternalCells[i];
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        IntVec3 cell = new IntVec3(center.x + dx, center.y, center.z + dz);
                        AddCell(cell, map, occupied, adjacentSeen, result.AdjacentCells);
                    }
                }
            }

            return result;
        }

        private static void CaptureSearchDiagnostics(
            DBHPipeBridgeResult result,
            DBHPipeReflectionBridge reflectionBridge)
        {
            if (result == null || reflectionBridge == null)
            {
                return;
            }

            result.FoundMapComp = result.FoundMapComp || reflectionBridge.LastFoundMapComp;
            result.FoundPipeGrid = result.FoundPipeGrid || reflectionBridge.LastFoundPipeGrid;
            if (!string.IsNullOrEmpty(reflectionBridge.LastPipeNetNullReason))
            {
                result.PipeNetNullReason = reflectionBridge.LastPipeNetNullReason;
            }
        }

        private static void ApplyConnectionDetails(
            DBHPipeBridgeResult result,
            DBHPipeConnection connection,
            bool water)
        {
            if (result == null || connection == null)
            {
                return;
            }

            result.PipeNetFound = result.PipeNetFound || connection.PipeNet != null;
            result.FoundMapComp = result.FoundMapComp || connection.FoundMapComp;
            result.FoundPipeGrid = result.FoundPipeGrid || connection.FoundPipeGrid;
            result.FoundNetByMapComp = result.FoundNetByMapComp || connection.FoundNetByMapComp;
            result.FoundNetByThingComp = result.FoundNetByThingComp || connection.FoundNetByThingComp;

            string cell = connection.Cell.ToString();
            if (water)
            {
                result.WaterPipeCell = cell;
                result.WaterPipeId = connection.PipeId;
                result.WaterPipeTypeName = connection.PipeTypeName;
            }
            else
            {
                result.SewagePipeCell = cell;
                result.SewagePipeId = connection.PipeId;
                result.SewagePipeTypeName = connection.PipeTypeName;
            }

            if (!string.IsNullOrEmpty(result.PipeCell))
            {
                return;
            }

            // Preserve the legacy single-connection fields for existing diagnostics while
            // recording the exact water/sewage endpoints above.
            result.PipeCell = cell;
            result.PipeThingDefName = connection.ThingDefName;
            result.PipeCompTypeName = connection.CompTypeName;
            result.PipeTypeName = connection.PipeTypeName;
            result.PipeId = connection.PipeId;
            result.PipeNetNullReason = connection.PipeNetNullReason;
        }

        private static string BuildNoConnectionResult(string waterReason, string sewageReason)
        {
            if (string.IsNullOrEmpty(waterReason) && string.IsNullOrEmpty(sewageReason))
            {
                return Tr("CT_Shuttle_Addon_DBH_Pipe_NoAdjacentConnection");
            }

            if (string.IsNullOrEmpty(waterReason))
            {
                return sewageReason;
            }

            if (string.IsNullOrEmpty(sewageReason) || sewageReason == waterReason)
            {
                return waterReason;
            }

            return waterReason + " " + sewageReason;
        }

        private static void AddCells(
            IReadOnlyList<IntVec3> cells,
            Map map,
            HashSet<IntVec3> occupied,
            HashSet<IntVec3> seen,
            List<IntVec3> result)
        {
            if (cells == null)
            {
                return;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                AddCell(cells[i], map, occupied, seen, result);
            }
        }

        private static void AddCell(
            IntVec3 cell,
            Map map,
            HashSet<IntVec3> occupied,
            HashSet<IntVec3> seen,
            List<IntVec3> result)
        {
            if (!cell.IsValid ||
                map == null ||
                !cell.InBounds(map) ||
                (occupied != null && occupied.Contains(cell)) ||
                seen.Contains(cell))
            {
                return;
            }

            seen.Add(cell);
            result.Add(cell);
        }

        private static int ClampSearchRadius(int radius)
        {
            if (radius < 0)
            {
                return 0;
            }

            return radius > MaxPipeSearchRadius ? MaxPipeSearchRadius : radius;
        }

        private static bool ContainsName(List<string> names, string candidate)
        {
            if (names == null || string.IsNullOrWhiteSpace(candidate))
            {
                return false;
            }

            for (int i = 0; i < names.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(names[i]) && names[i].Trim() == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AppendResult(DBHPipeBridgeResult result, string message)
        {
            if (result == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            result.Result = string.IsNullOrEmpty(result.Result)
                ? message
                : result.Result + " " + message;
        }

        private static float Min(float left, float right)
        {
            return left < right ? left : right;
        }

        private static string FormatAmount(float amount)
        {
            return amount.ToString("0.#");
        }

        private static string Tr(string key)
        {
            return key.Translate().ToString();
        }

        private static string Tr(string key, params object[] args)
        {
            return string.Format(Tr(key), args);
        }

        private sealed class DBHPipeReflectionBridge
        {
            private const string CompPipeTypeName = "DubsBadHygiene.CompPipe";
            private const string HygienePipeMapCompTypeName = "DubsBadHygiene.HygienePipeMapComp";
            private const string MapComponentHygieneTypeName = "DubsBadHygiene.MapComponent_Hygiene";
            private const string PlumbingNetTypeName = "DubsBadHygiene.PlumbingNet";
            private readonly CT_Shuttle_DBHIntegrationDef integration;
            private bool resolveAttempted;
            private bool resolved;
            private bool mapLookupAvailable;
            private string resolveFailureReason;
            private Type compPipeType;
            private Type hygienePipeMapCompType;
            private Type mapComponentHygieneType;
            private Type plumbingNetType;
            private Type pipeTypeType;
            private PropertyInfo pipeNetProperty;
            private FieldInfo pipeNetField;
            private FieldInfo pipeNetRefField;
            private PropertyInfo hygienePipeCompProperty;
            private FieldInfo hygienePipeCompField;
            private MethodInfo idAtMethod;
            private MethodInfo zoneAtMethod;
            private MethodInfo regenPipeGridsMethod;
            private FieldInfo pipeNetsField;
            private PropertyInfo pipeNetsProperty;
            private MethodInfo pullWaterMethod;
            private MethodInfo pushSewageMethod;
            private FieldInfo netTypeField;
            private FieldInfo netIdField;
            private PropertyInfo netTypeProperty;
            private PropertyInfo netIdProperty;
            private object[] pipeTypes;
            private int[] pipeTypeValues;
            private bool lastFoundMapComp;
            private bool lastFoundPipeGrid;
            private string lastPipeNetNullReason;
            private Map lastRefreshedMap;
            private int lastPipeGridRefreshTick = int.MinValue;

            public DBHPipeReflectionBridge(CT_Shuttle_DBHIntegrationDef integration)
            {
                this.integration = integration;
            }

            public bool ResolveFailed
            {
                get { return resolveAttempted && !resolved; }
            }

            public string ResolveFailureReason
            {
                get { return resolveFailureReason; }
            }

            public bool LastFoundMapComp
            {
                get { return lastFoundMapComp; }
            }

            public bool LastFoundPipeGrid
            {
                get { return lastFoundPipeGrid; }
            }

            public string LastPipeNetNullReason
            {
                get { return lastPipeNetNullReason; }
            }

            public bool TryFindPipeConnection(
                Map map,
                DBHPipeSearchCells cells,
                DBHPipeNetworkKind networkKind,
                out DBHPipeConnection connection,
                out string reason)
            {
                connection = null;
                reason = null;
                lastFoundMapComp = false;
                lastFoundPipeGrid = false;
                lastPipeNetNullReason = null;

                if (!Resolve())
                {
                    reason = resolveFailureReason;
                    return false;
                }

                if (map == null || cells == null || cells.Count == 0)
                {
                    reason = "No cells to inspect.";
                    return false;
                }

                // Find the DBH map component once for this network lookup. The previous path
                // repeated the component scan and dirty-grid refresh for occupied and adjacent
                // cells even though both read the same map state.
                object pipeMapComp = mapLookupAvailable
                    ? FindPipeMapComp(map)
                    : null;
                if (pipeMapComp != null)
                {
                    lastFoundMapComp = true;
                    lastFoundPipeGrid = true;
                    TryRefreshPipeGrid(pipeMapComp, map);
                }

                if (TryFindPipeConnectionByMapComp(pipeMapComp, cells.OccupiedCells, "occupied", networkKind, out connection, out reason) ||
                    TryFindPipeConnectionByMapComp(pipeMapComp, cells.AdjacentCells, "adjacent", networkKind, out connection, out reason))
                {
                    return true;
                }

                string mapCompReason = reason;
                if (pipeMapComp != null)
                {
                    // HygienePipeMapComp is DBH's authoritative topology. Once that path is
                    // available and refreshed, scanning every Thing/Comp on the same cells is
                    // redundant and is especially wasteful for the common disconnected case.
                    return false;
                }

                if (TryFindPipeConnectionByThingComp(map, cells.OccupiedCells, "occupied", networkKind, out connection, out reason) ||
                    TryFindPipeConnectionByThingComp(map, cells.AdjacentCells, "adjacent", networkKind, out connection, out reason))
                {
                    return true;
                }

                reason = string.IsNullOrEmpty(mapCompReason)
                    ? reason
                    : mapCompReason + " " + reason;
                return false;
            }

            private bool TryFindPipeConnectionByMapComp(
                object pipeMapComp,
                IReadOnlyList<IntVec3> cells,
                string source,
                DBHPipeNetworkKind networkKind,
                out DBHPipeConnection connection,
                out string reason)
            {
                connection = null;
                reason = null;
                if (cells == null || cells.Count == 0)
                {
                    reason = "No " + source + " cells to inspect by DBH pipe grid.";
                    return false;
                }

                if (pipeMapComp == null)
                {
                    reason = "DBH HygienePipeMapComp was not found on the map.";
                    return false;
                }

                for (int i = 0; i < cells.Count; i++)
                {
                    object pipeNet;
                    int pipeId;
                    string pipeTypeName;
                    if (!TryGetPipeNetAt(pipeMapComp, cells[i], networkKind, out pipeNet, out pipeId, out pipeTypeName))
                    {
                        continue;
                    }

                    connection = new DBHPipeConnection
                    {
                        Cell = cells[i],
                        ThingDefName = null,
                        CompTypeName = null,
                        PipeNet = pipeNet,
                        FoundMapComp = true,
                        FoundPipeGrid = true,
                        FoundNetByMapComp = true,
                        Source = source,
                        PipeId = pipeId,
                        PipeTypeName = pipeTypeName
                    };
                    reason = "DBH pipe net found by pipe grid on " + source + " cell " + cells[i] + ".";
                    return true;
                }

                reason = "DBH " + networkKind + " pipe grid found no pipe net in " + source + " cells.";
                return false;
            }

            private bool TryFindPipeConnectionByThingComp(
                Map map,
                IReadOnlyList<IntVec3> cells,
                string source,
                DBHPipeNetworkKind networkKind,
                out DBHPipeConnection connection,
                out string reason)
            {
                connection = null;
                reason = null;
                if (map == null || cells == null || cells.Count == 0)
                {
                    reason = "No " + source + " cells to inspect by thing comps.";
                    return false;
                }

                for (int i = 0; i < cells.Count; i++)
                {
                    List<Thing> things = map.thingGrid.ThingsListAt(cells[i]);
                    for (int j = 0; j < things.Count; j++)
                    {
                        ThingWithComps thing = things[j] as ThingWithComps;
                        if (thing == null || thing.AllComps == null)
                        {
                            continue;
                        }

                        for (int k = 0; k < thing.AllComps.Count; k++)
                        {
                            ThingComp comp = thing.AllComps[k];
                            if (!IsCompPipe(comp))
                            {
                                continue;
                            }

                            string nullReason;
                            object pipeNet = GetPipeNetFromComp(comp, out nullReason);
                            if (pipeNet == null)
                            {
                                reason = nullReason;
                                lastPipeNetNullReason = nullReason;
                                continue;
                            }

                            int pipeTypeValue;
                            object ignoredPipeType;
                            if (!TryResolvePipeType(networkKind, out ignoredPipeType, out pipeTypeValue) ||
                                ReadIntMember(pipeNet, netTypeField, netTypeProperty, -1) != pipeTypeValue)
                            {
                                continue;
                            }

                            connection = new DBHPipeConnection
                            {
                                Cell = cells[i],
                                ThingDefName = thing.def != null ? thing.def.defName : null,
                                CompTypeName = comp.GetType().FullName,
                                PipeNet = pipeNet,
                                FoundMapComp = false,
                                FoundPipeGrid = false,
                                FoundNetByThingComp = true,
                                Source = source,
                                PipeId = ReadIntMember(pipeNet, netIdField, netIdProperty, -1),
                                PipeTypeName = networkKind.ToString(),
                                PipeNetNullReason = nullReason
                            };
                            reason = "DBH pipe net found by thing comp on " + source + " cell " + cells[i] + ".";
                            return true;
                        }
                    }
                }

                if (string.IsNullOrEmpty(reason))
                {
                    reason = "No DBH " + networkKind + " CompPipe thing found in " + source + " cells.";
                }

                return false;
            }

            public bool TryPullWater(object pipeNet, float amount, out string contaminationLevel, out string reason)
            {
                contaminationLevel = null;
                reason = null;

                if (pipeNet == null || amount <= 0f)
                {
                    reason = "Invalid DBH water pull request.";
                    return false;
                }

                if (!Resolve())
                {
                    reason = resolveFailureReason;
                    return false;
                }

                if (pullWaterMethod == null)
                {
                    reason = "DBH PlumbingNet.PullWater(float, out contamination) could not be resolved.";
                    return false;
                }

                try
                {
                    ParameterInfo[] parameters = pullWaterMethod.GetParameters();
                    Type contaminationType = parameters[1].ParameterType.GetElementType();
                    object contaminationValue = contaminationType != null && contaminationType.IsValueType
                        ? Activator.CreateInstance(contaminationType)
                        : null;
                    object[] args = new object[] { amount, contaminationValue };
                    object rawResult = pullWaterMethod.Invoke(pipeNet, args);
                    bool success = rawResult is bool && (bool)rawResult;
                    contaminationLevel = args[1] != null ? args[1].ToString() : null;
                    if (!success)
                    {
                        reason = "CT_Shuttle_Addon_Pipe_NoDrawableWater".Translate().ToString();
                    }

                    return success;
                }
                catch (Exception ex)
                {
                    reason = "DBH water pull reflection failed: " + RootMessage(ex);
                    AdditionalModuleLog.WarningOnce("dbh-pipe-pullwater-reflection", reason);
                    return false;
                }
            }

            public bool TryPushSewage(object pipeNet, float amount, out string reason)
            {
                reason = null;

                if (pipeNet == null || amount <= 0f)
                {
                    reason = "Invalid DBH sewage push request.";
                    return false;
                }

                if (!Resolve())
                {
                    reason = resolveFailureReason;
                    return false;
                }

                if (pushSewageMethod == null)
                {
                    reason = "DBH PlumbingNet.PushSewage(float) could not be resolved.";
                    return false;
                }

                try
                {
                    object rawResult = pushSewageMethod.Invoke(pipeNet, new object[] { amount });
                    bool success = rawResult is bool && (bool)rawResult;
                    if (!success)
                    {
                        reason = "CT_Shuttle_Addon_Pipe_NoSewageFacility".Translate().ToString();
                    }

                    return success;
                }
                catch (Exception ex)
                {
                    reason = "DBH sewage push reflection failed: " + RootMessage(ex);
                    AdditionalModuleLog.WarningOnce("dbh-pipe-pushsewage-reflection", reason);
                    return false;
                }
            }

            private bool Resolve()
            {
                if (resolveAttempted)
                {
                    return resolved;
                }

                resolveAttempted = true;
                compPipeType = FindType(CompPipeTypeName);
                hygienePipeMapCompType = FindType(HygienePipeMapCompTypeName);
                mapComponentHygieneType = FindType(MapComponentHygieneTypeName);
                plumbingNetType = FindType(PlumbingNetTypeName);
                pipeTypeType = FindType("DubsBadHygiene.PipeType");

                if (compPipeType != null)
                {
                    pipeNetProperty = compPipeType.GetProperty("pipeNet", BindingFlags.Instance | BindingFlags.Public);
                    pipeNetField = compPipeType.GetField("pipeNet", BindingFlags.Instance | BindingFlags.Public);
                    pipeNetRefField = compPipeType.GetField("pipeNetRef", BindingFlags.Instance | BindingFlags.Public);
                    if (plumbingNetType == null)
                    {
                        plumbingNetType = pipeNetProperty != null
                            ? pipeNetProperty.PropertyType
                            : pipeNetField != null
                                ? pipeNetField.FieldType
                                : pipeNetRefField != null ? pipeNetRefField.FieldType : null;
                    }
                }

                if (mapComponentHygieneType != null)
                {
                    hygienePipeCompProperty = mapComponentHygieneType.GetProperty("PipeComp", BindingFlags.Instance | BindingFlags.Public);
                    hygienePipeCompField = mapComponentHygieneType.GetField("PipeComp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }

                if (hygienePipeMapCompType != null)
                {
                    idAtMethod = hygienePipeMapCompType.GetMethod("IDAt", BindingFlags.Instance | BindingFlags.Public);
                    zoneAtMethod = hygienePipeMapCompType.GetMethod("ZoneAt", BindingFlags.Instance | BindingFlags.Public);
                    regenPipeGridsMethod = hygienePipeMapCompType.GetMethod(
                        "RegenPipeGrids",
                        BindingFlags.Instance | BindingFlags.Public);
                    pipeNetsField = hygienePipeMapCompType.GetField("PipeNets", BindingFlags.Instance | BindingFlags.Public);
                    pipeNetsProperty = hygienePipeMapCompType.GetProperty("PipeNets", BindingFlags.Instance | BindingFlags.Public);
                }

                if (plumbingNetType != null)
                {
                    netTypeField = FindFieldAny(plumbingNetType, "NetType", "type");
                    netIdField = FindFieldAny(plumbingNetType, "NetID", "id");
                    netTypeProperty = FindPropertyAny(plumbingNetType, "NetType", "type");
                    netIdProperty = FindPropertyAny(plumbingNetType, "NetID", "id");
                    pullWaterMethod = FindPullWaterMethod(plumbingNetType);
                    pushSewageMethod = FindSingleFloatBoolMethod(plumbingNetType, "PushSewage");
                }

                if (pipeTypeType != null)
                {
                    ResolvePipeTypes();
                }

                bool mapPathAvailable = hygienePipeMapCompType != null &&
                    pipeTypeType != null &&
                    pipeTypes != null &&
                    pipeTypeValues != null &&
                    idAtMethod != null &&
                    zoneAtMethod != null &&
                    (pipeNetsField != null || pipeNetsProperty != null);
                mapLookupAvailable = mapPathAvailable;
                bool thingPathAvailable = compPipeType != null &&
                    (pipeNetProperty != null || pipeNetField != null || pipeNetRefField != null);

                if (!mapPathAvailable && !thingPathAvailable)
                {
                    resolveFailureReason = BuildResolveFailureReason(mapPathAvailable, thingPathAvailable);
                    AdditionalModuleLog.WarningOnce("dbh-pipe-connection-members-missing", resolveFailureReason);
                    return false;
                }

                if (pullWaterMethod == null)
                {
                    AdditionalModuleLog.WarningOnce(
                        "dbh-pipe-pullwater-missing",
                        "DBH PlumbingNet.PullWater(float, out contamination) could not be resolved. Pipe refill will be unavailable.");
                }

                if (pushSewageMethod == null)
                {
                    AdditionalModuleLog.WarningOnce(
                        "dbh-pipe-pushsewage-missing",
                        "DBH PlumbingNet.PushSewage(float) could not be resolved. Pipe sewage drain will be unavailable.");
                }

                resolved = mapPathAvailable || thingPathAvailable;
                resolveFailureReason = resolved ? null : BuildResolveFailureReason(mapPathAvailable, thingPathAvailable);
                return true;
            }

            private bool IsCompPipe(ThingComp comp)
            {
                if (comp == null)
                {
                    return false;
                }

                Type type = comp.GetType();
                return compPipeType != null && compPipeType.IsAssignableFrom(type);
            }

            private object FindPipeMapComp(Map map)
            {
                if (map == null || map.components == null || hygienePipeMapCompType == null)
                {
                    return null;
                }

                for (int i = 0; i < map.components.Count; i++)
                {
                    object component = map.components[i];
                    if (component != null && hygienePipeMapCompType.IsAssignableFrom(component.GetType()))
                    {
                        return component;
                    }
                }

                for (int i = 0; i < map.components.Count; i++)
                {
                    object component = map.components[i];
                    if (component == null ||
                        mapComponentHygieneType == null ||
                        !mapComponentHygieneType.IsAssignableFrom(component.GetType()))
                    {
                        continue;
                    }

                    object pipeComp = null;
                    if (hygienePipeCompProperty != null)
                    {
                        pipeComp = hygienePipeCompProperty.GetValue(component, null);
                    }

                    if (pipeComp == null && hygienePipeCompField != null)
                    {
                        pipeComp = hygienePipeCompField.GetValue(component);
                    }

                    if (pipeComp != null && hygienePipeMapCompType.IsAssignableFrom(pipeComp.GetType()))
                    {
                        return pipeComp;
                    }
                }

                return null;
            }

            private void TryRefreshPipeGrid(object pipeMapComp, Map map)
            {
                if (pipeMapComp == null || regenPipeGridsMethod == null)
                {
                    return;
                }

                int ticksGame = Find.TickManager != null
                    ? Find.TickManager.TicksGame
                    : int.MinValue;
                if (map != null &&
                    object.ReferenceEquals(map, lastRefreshedMap) &&
                    ticksGame != int.MinValue &&
                    ticksGame == lastPipeGridRefreshTick)
                {
                    return;
                }

                try
                {
                    // DBH marks pipe grids dirty on placement/removal. Its ordinary CompPipe
                    // getter regenerates them lazily; reflection-only consumers must request
                    // the same refresh before calling ZoneAt/IDAt.
                    regenPipeGridsMethod.Invoke(pipeMapComp, null);
                    lastRefreshedMap = map;
                    lastPipeGridRefreshTick = ticksGame;
                }
                catch (Exception ex)
                {
                    AdditionalModuleLog.WarningOnce(
                        "dbh-pipe-grid-refresh",
                        "DBH pipe grid refresh failed: " + RootMessage(ex));
                }
            }

            private bool TryGetPipeNetAt(
                object pipeMapComp,
                IntVec3 cell,
                DBHPipeNetworkKind networkKind,
                out object pipeNet,
                out int pipeId,
                out string pipeTypeName)
            {
                pipeNet = null;
                pipeId = -1;
                pipeTypeName = null;
                object pipeType;
                int pipeTypeValue;
                if (!TryResolvePipeType(networkKind, out pipeType, out pipeTypeValue))
                {
                    return false;
                }

                bool zoneAt = (bool)zoneAtMethod.Invoke(pipeMapComp, new object[] { cell, pipeType });
                if (!zoneAt)
                {
                    return false;
                }

                object rawId = idAtMethod.Invoke(pipeMapComp, new object[] { cell, pipeType });
                if (!(rawId is int) || (int)rawId <= 0)
                {
                    return false;
                }

                pipeId = (int)rawId;
                pipeTypeName = pipeType.ToString();
                pipeNet = FindPipeNetById(pipeMapComp, pipeId, pipeTypeValue);
                return pipeNet != null;
            }

            private bool TryResolvePipeType(
                DBHPipeNetworkKind networkKind,
                out object pipeType,
                out int pipeTypeValue)
            {
                pipeType = null;
                pipeTypeValue = -1;
                if (pipeTypes == null || pipeTypeValues == null)
                {
                    return false;
                }

                string expectedName = networkKind.ToString();
                for (int i = 0; i < pipeTypes.Length && i < pipeTypeValues.Length; i++)
                {
                    object candidate = pipeTypes[i];
                    if (candidate != null && candidate.ToString() == expectedName)
                    {
                        pipeType = candidate;
                        pipeTypeValue = pipeTypeValues[i];
                        return true;
                    }
                }

                return false;
            }

            private object FindPipeNetById(object pipeMapComp, int pipeId, int pipeType)
            {
                IEnumerable pipeNets = ReadPipeNets(pipeMapComp);
                if (pipeNets == null)
                {
                    lastPipeNetNullReason = "DBH HygienePipeMapComp PipeNets could not be read.";
                    return null;
                }

                foreach (object candidateEntry in pipeNets)
                {
                    object candidate = UnwrapDictionaryEntry(candidateEntry);
                    if (candidate == null)
                    {
                        continue;
                    }

                    int candidateId = ReadIntMember(candidate, netIdField, netIdProperty, -1);
                    int candidateType = ReadIntMember(candidate, netTypeField, netTypeProperty, -1);
                    if (candidateId == pipeId && candidateType == pipeType)
                    {
                        return candidate;
                    }
                }

                return null;
            }

            private void ResolvePipeTypes()
            {
                try
                {
                    object sewage = Enum.Parse(pipeTypeType, "Sewage");
                    object water = Enum.Parse(pipeTypeType, "Water");
                    pipeTypes = new object[] { sewage, water };
                    pipeTypeValues = new int[]
                    {
                        Convert.ToInt32(sewage),
                        Convert.ToInt32(water)
                    };
                }
                catch (Exception ex)
                {
                    pipeTypes = null;
                    pipeTypeValues = null;
                    AdditionalModuleLog.WarningOnce(
                        "dbh-pipe-type-reflection",
                        "DBH PipeType enum values could not be resolved: " + RootMessage(ex));
                }
            }

            private IEnumerable ReadPipeNets(object pipeMapComp)
            {
                object value = null;
                if (pipeNetsField != null)
                {
                    value = pipeNetsField.GetValue(pipeMapComp);
                }
                else if (pipeNetsProperty != null)
                {
                    value = pipeNetsProperty.GetValue(pipeMapComp, null);
                }

                IDictionary dictionary = value as IDictionary;
                if (dictionary != null)
                {
                    return dictionary.Values;
                }

                return value as IEnumerable;
            }

            private static object UnwrapDictionaryEntry(object value)
            {
                if (value is DictionaryEntry)
                {
                    return ((DictionaryEntry)value).Value;
                }

                return value;
            }

            private object GetPipeNetFromComp(ThingComp comp, out string nullReason)
            {
                nullReason = null;
                if (comp == null)
                {
                    nullReason = "DBH CompPipe was null.";
                    return null;
                }

                object pipeNet = null;
                try
                {
                    if (pipeNetProperty != null)
                    {
                        pipeNet = pipeNetProperty.GetValue(comp, null);
                    }

                    if (pipeNet == null && pipeNetField != null)
                    {
                        pipeNet = pipeNetField.GetValue(comp);
                    }

                    if (pipeNet == null && pipeNetRefField != null)
                    {
                        pipeNet = pipeNetRefField.GetValue(comp);
                    }
                }
                catch (Exception ex)
                {
                    nullReason = "DBH CompPipe pipeNet reflection failed: " + RootMessage(ex);
                    AdditionalModuleLog.WarningOnce("dbh-pipe-comp-pipenet-reflection", nullReason);
                    return null;
                }

                if (pipeNet == null)
                {
                    nullReason = "DBH CompPipe was found but pipeNet was null.";
                }

                return pipeNet;
            }

            private static int ReadIntMember(object instance, FieldInfo field, PropertyInfo property, int fallback)
            {
                if (instance == null)
                {
                    return fallback;
                }

                object value = null;
                if (field != null)
                {
                    value = field.GetValue(instance);
                }
                else if (property != null)
                {
                    value = property.GetValue(instance, null);
                }

                return value is int ? (int)value : fallback;
            }

            private static string BuildResolveFailureReason(bool mapPathAvailable, bool thingPathAvailable)
            {
                return "DBH pipe bridge could not resolve connection members. mapPath=" +
                    mapPathAvailable + ", thingPath=" + thingPathAvailable + ".";
            }

            private static Type FindType(string fullName)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    Type type = assemblies[i].GetType(fullName, false);
                    if (type != null)
                    {
                        return type;
                    }
                }

                return null;
            }

            private static FieldInfo FindFieldAny(Type type, params string[] names)
            {
                if (type == null || names == null)
                {
                    return null;
                }

                for (int i = 0; i < names.Length; i++)
                {
                    FieldInfo field = type.GetField(names[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field != null)
                    {
                        return field;
                    }
                }

                return null;
            }

            private static PropertyInfo FindPropertyAny(Type type, params string[] names)
            {
                if (type == null || names == null)
                {
                    return null;
                }

                for (int i = 0; i < names.Length; i++)
                {
                    PropertyInfo property = type.GetProperty(names[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (property != null)
                    {
                        return property;
                    }
                }

                return null;
            }

            private static MethodInfo FindPullWaterMethod(Type plumbingNetType)
            {
                if (plumbingNetType == null)
                {
                    return null;
                }

                MethodInfo[] methods = plumbingNetType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.Name != "PullWater" || method.ReturnType != typeof(bool))
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 2 &&
                        parameters[0].ParameterType == typeof(float) &&
                        parameters[1].ParameterType.IsByRef)
                    {
                        return method;
                    }
                }

                return null;
            }

            private static MethodInfo FindSingleFloatBoolMethod(Type type, string methodName)
            {
                if (type == null || string.IsNullOrEmpty(methodName))
                {
                    return null;
                }

                MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.Name != methodName || method.ReturnType != typeof(bool))
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(float))
                    {
                        return method;
                    }
                }

                return null;
            }

            private static string RootMessage(Exception ex)
            {
                TargetInvocationException targetInvocationException = ex as TargetInvocationException;
                if (targetInvocationException != null && targetInvocationException.InnerException != null)
                {
                    return targetInvocationException.InnerException.Message;
                }

                return ex != null ? ex.Message : "unknown error";
            }
        }
    }

    internal enum DBHPipeNetworkKind
    {
        Sewage
    }

    internal sealed class DBHPipeConnection
    {
        public IntVec3 Cell;
        public string ThingDefName;
        public string CompTypeName;
        public object PipeNet;
        public bool FoundMapComp;
        public bool FoundPipeGrid;
        public bool FoundNetByMapComp;
        public bool FoundNetByThingComp;
        public string Source;
        public int PipeId;
        public string PipeTypeName;
        public string PipeNetNullReason;
    }

    internal sealed class DBHPipeSearchCells
    {
        public readonly List<IntVec3> OccupiedCells = new List<IntVec3>();
        public readonly List<IntVec3> AdjacentCells = new List<IntVec3>();

        public int Count
        {
            get { return OccupiedCells.Count + AdjacentCells.Count; }
        }
    }

    internal sealed class DBHPipeBridgeResult
    {
        public bool PipeConnected;
        public bool WaterPipeConnected;
        public bool SewagePipeConnected;
        public float CleanWater;
        public float Sewage;
        public float WaterAdded;
        public float SewageDrained;
        public string Result;
        public string BlockedReason;
        public string BridgeMode;
        public string PipeCell;
        public string PipeThingDefName;
        public string PipeCompTypeName;
        public string PipeTypeName;
        public int PipeId;
        public bool PipeNetFound;
        public string PipeNetNullReason;
        public string LastContaminationLevel;
        public string ResolveFailureReason;
        public int CheckedOccupiedCells;
        public int CheckedAdjacentCells;
        public bool FoundMapComp;
        public bool FoundPipeGrid;
        public bool FoundNetByMapComp;
        public bool FoundNetByThingComp;
        public string WaterPipeCell;
        public string SewagePipeCell;
        public string WaterPipeTypeName;
        public string SewagePipeTypeName;
        public int WaterPipeId;
        public int SewagePipeId;
    }
}

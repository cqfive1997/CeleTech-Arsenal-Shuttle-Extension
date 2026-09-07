using System;
using System.Collections.Generic;
using System.Text;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Patches
{
    [HarmonyPatch(typeof(JoyUtility), nameof(JoyUtility.JoyKindsOnMapTempList))]
    internal static class JoyUtility_JoyKindsOnMapTempList_Patch
    {
        private static void Postfix(Map map, ref List<JoyKindDef> __result)
        {
            if (map == null || __result == null)
            {
                return;
            }

            try
            {
                ShuttleJoyKindMapCache.AppendJoyKindsForMap(map, __result);
            }
            catch (Exception ex)
            {
                Log.ErrorOnce(
                    "[CeleTech.Shuttle] Failed to append shuttle joy kinds: " + ex,
                    91342017);
            }
        }
    }

    [HarmonyPatch(typeof(JoyUtility), nameof(JoyUtility.JoyKindsOnMapString))]
    internal static class JoyUtility_JoyKindsOnMapString_Patch
    {
        private static void Postfix(Map map, ref string __result)
        {
            if (map == null)
            {
                return;
            }

            try
            {
                ShuttleJoyKindMapCache.AppendJoyKindLabelsForMap(map, ref __result);
            }
            catch (Exception ex)
            {
                Log.ErrorOnce(
                    "[CeleTech.Shuttle] Failed to append shuttle joy kind labels: " + ex,
                    91342018);
            }
        }
    }

    internal static class ShuttleJoyKindMapCache
    {
        private const int RefreshIntervalTicks = 250;
        private const string ModularShuttleHostDefName = "CT_ModularShuttleHost";

        private static readonly Dictionary<int, CacheEntry> CacheByMapId =
            new Dictionary<int, CacheEntry>();

        private static ThingDef cachedShuttleDef;

        internal static void Clear()
        {
            CacheByMapId.Clear();
            cachedShuttleDef = null;
        }

        internal static void AppendJoyKindsForMap(Map map, List<JoyKindDef> destination)
        {
            if (destination == null)
            {
                return;
            }

            List<JoyKindDef> joyKinds = GetJoyKindsForMap(map);
            for (int i = 0; i < joyKinds.Count; i++)
            {
                JoyKindDef joyKind = joyKinds[i];
                if (joyKind != null && !destination.Contains(joyKind))
                {
                    destination.Add(joyKind);
                }
            }
        }

        internal static void AppendJoyKindLabelsForMap(Map map, ref string result)
        {
            List<JoyKindDef> joyKinds = GetJoyKindsForMap(map);
            if (joyKinds.Count == 0)
            {
                return;
            }

            StringBuilder builder = new StringBuilder(result ?? string.Empty);
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            for (int i = 0; i < joyKinds.Count; i++)
            {
                JoyKindDef joyKind = joyKinds[i];
                if (joyKind != null)
                {
                    builder.Append("  - ");
                    builder.AppendLine(joyKind.LabelCap);
                }
            }

            result = builder.ToString().TrimEndNewlines();
        }

        private static List<JoyKindDef> GetJoyKindsForMap(Map map)
        {
            if (map == null)
            {
                return new List<JoyKindDef>();
            }

            PruneDeadMaps();
            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            CacheEntry cacheEntry;
            if (CacheByMapId.TryGetValue(map.uniqueID, out cacheEntry) &&
                ticksGame - cacheEntry.LastRefreshTick < RefreshIntervalTicks)
            {
                return cacheEntry.JoyKinds;
            }

            List<JoyKindDef> refreshed = BuildJoyKindsForMap(map);
            CacheByMapId[map.uniqueID] = new CacheEntry(ticksGame, refreshed);
            return refreshed;
        }

        private static void PruneDeadMaps()
        {
            if (Current.Game == null || Current.Game.Maps == null || CacheByMapId.Count == 0)
            {
                return;
            }

            List<int> staleMapIds = null;
            foreach (int mapId in CacheByMapId.Keys)
            {
                if (!MapExists(mapId))
                {
                    if (staleMapIds == null)
                    {
                        staleMapIds = new List<int>();
                    }

                    staleMapIds.Add(mapId);
                }
            }

            for (int i = 0; staleMapIds != null && i < staleMapIds.Count; i++)
            {
                CacheByMapId.Remove(staleMapIds[i]);
            }
        }

        private static bool MapExists(int mapId)
        {
            List<Map> maps = Current.Game != null ? Current.Game.Maps : null;
            for (int i = 0; maps != null && i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map != null && map.uniqueID == mapId)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<JoyKindDef> BuildJoyKindsForMap(Map map)
        {
            List<JoyKindDef> joyKinds = new List<JoyKindDef>();
            ThingDef shuttleDef = GetShuttleDef();
            if (map == null || shuttleDef == null || map.listerThings == null)
            {
                return joyKinds;
            }

            List<Thing> shuttles = map.listerThings.ThingsOfDef(shuttleDef);
            if (shuttles == null)
            {
                return joyKinds;
            }

            for (int i = 0; i < shuttles.Count; i++)
            {
                ThingWithComps shuttleHost = shuttles[i] as ThingWithComps;
                if (shuttleHost == null || shuttleHost.Destroyed || !shuttleHost.Spawned)
                {
                    continue;
                }

                CompModularShuttleCore core = shuttleHost.TryGetComp<CompModularShuttleCore>();
                ShuttleController controller = core != null ? core.Controller : null;
                if (controller == null || !IsInternalBusPowered(controller))
                {
                    continue;
                }

                ShuttleProfile profile = controller.GetProfileForRead();
                HabitatProfile habitat = profile != null ? profile.Habitat : null;
                if (habitat == null || !habitat.SupportsJoy || habitat.JoyKinds == null)
                {
                    continue;
                }

                for (int j = 0; j < habitat.JoyKinds.Count; j++)
                {
                    JoyKindDef joyKind = habitat.JoyKinds[j];
                    if (joyKind != null && !joyKinds.Contains(joyKind))
                    {
                        joyKinds.Add(joyKind);
                    }
                }
            }

            return joyKinds;
        }

        private static bool IsInternalBusPowered(ShuttleController controller)
        {
            ShuttlePowerRuntimeSnapshot powerSnapshot =
                controller != null ? controller.BuildPowerRuntimeSnapshot() : null;
            return powerSnapshot != null && powerSnapshot.InternalBusPowered;
        }

        private static ThingDef GetShuttleDef()
        {
            if (cachedShuttleDef == null)
            {
                cachedShuttleDef = DefDatabase<ThingDef>.GetNamedSilentFail(ModularShuttleHostDefName);
            }

            return cachedShuttleDef;
        }

        private sealed class CacheEntry
        {
            internal CacheEntry(int lastRefreshTick, List<JoyKindDef> joyKinds)
            {
                this.LastRefreshTick = lastRefreshTick;
                this.JoyKinds = joyKinds ?? new List<JoyKindDef>();
            }

            internal int LastRefreshTick { get; private set; }

            internal List<JoyKindDef> JoyKinds { get; private set; }
        }
    }
}

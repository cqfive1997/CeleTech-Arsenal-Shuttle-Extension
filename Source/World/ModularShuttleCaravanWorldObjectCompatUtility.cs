using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.World
{
    internal static class ModularShuttleCaravanWorldObjectCompatUtility
    {
        private const string NotifyCaravanArrivedMethodName = "Notify_CaravanArrived";

        private static readonly Dictionary<Type, MethodInfo> NotifyCaravanArrivedMethods =
            new Dictionary<Type, MethodInfo>();

        private static readonly HashSet<string> SupportedWorldObjectDefNames =
            new HashSet<string>
            {
                "TaleOfMilira_TheRuinOutpost",
                "TaleOfMilira_TheRuinOutpost_m",
                "TaleOfMilira_TheDestroyOutpost",
                "TaleOfMilira_TheDestroyOutpost_m",
                "TaleOfMilira_TheBattlefield",
                "TaleOfMilira_TheBattlefield_m",
                "TaleOfMilira_TheMiliraSOSSignal",
                "TaleOfMilira_TheMiliraSOSSignal_m",
                "TaleOfMilira_TheChurchAndMilira",
                "TaleOfMilira_TheChurchAndMilira_m",
                "TaleOfMilira_TheMiliraSupply"
            };

        private static readonly Dictionary<string, string> VisitTranslationKeys =
            new Dictionary<string, string>
            {
                { "TaleOfMilira_TheRuinOutpost", "TaleOfMilira.VisitTheRuinOutpost" },
                { "TaleOfMilira_TheRuinOutpost_m", "TaleOfMilira.VisitTheRuinOutpost_m" },
                { "TaleOfMilira_TheDestroyOutpost", "TaleOfMilira.VisitTheDestroyOutpost" },
                { "TaleOfMilira_TheDestroyOutpost_m", "TaleOfMilira.VisitTheDestroyOutpost_m" },
                { "TaleOfMilira_TheBattlefield", "TaleOfMilira.VisitTheBattlefield" },
                { "TaleOfMilira_TheBattlefield_m", "TaleOfMilira.VisitTheBattlefield_m" },
                { "TaleOfMilira_TheMiliraSOSSignal", "TaleOfMilira.VisitTheMiliraSOSSignal" },
                { "TaleOfMilira_TheMiliraSOSSignal_m", "TaleOfMilira.VisitTheMiliraSOSSignal" },
                { "TaleOfMilira_TheChurchAndMilira", "TaleOfMilira.VisitTheChurchAndMilira" },
                { "TaleOfMilira_TheChurchAndMilira_m", "TaleOfMilira.VisitTheChurchAndMilira" },
                { "TaleOfMilira_TheMiliraSupply", "TaleOfMilira.VisitTheMiliraSupply" }
            };

        internal static bool IsSupported(WorldObject worldObject)
        {
            return worldObject != null &&
                worldObject.def != null &&
                SupportedWorldObjectDefNames.Contains(worldObject.def.defName) &&
                TryGetNotifyCaravanArrivedMethod(worldObject, out MethodInfo ignoredMethod);
        }

        internal static bool HasSupportedWorldObjectAt(PlanetTile tile)
        {
            if (!tile.Valid || Find.WorldObjects == null)
            {
                return false;
            }

            List<WorldObject> worldObjects = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < worldObjects.Count; i++)
            {
                WorldObject worldObject = worldObjects[i];
                if (worldObject != null && worldObject.Tile == tile && IsSupported(worldObject))
                {
                    return true;
                }
            }

            return false;
        }

        internal static IEnumerable<FloatMenuOption> GetFloatMenuOptions(
            WorldObject worldObject,
            IEnumerable<IThingHolder> holders,
            Action<PlanetTile, TransportersArrivalAction> launchAction)
        {
            if (worldObject == null || launchAction == null)
            {
                yield break;
            }

            ThingWithComps shuttle;
            if (!ModularShuttleExistingMapLandingTargeter.TryResolveModularShuttle(holders, out shuttle))
            {
                yield break;
            }

            string label = GetVisitLabel(worldObject);
            foreach (FloatMenuOption option in TransportersArrivalActionUtility.GetFloatMenuOptions<ModularShuttleVisitCaravanWorldObjectArrivalAction>(
                () => CanVisit(holders, worldObject),
                () => new ModularShuttleVisitCaravanWorldObjectArrivalAction(worldObject),
                label,
                launchAction,
                worldObject.Tile,
                null))
            {
                yield return option;
            }
        }

        internal static FloatMenuAcceptanceReport CanVisit(
            IEnumerable<IThingHolder> holders,
            WorldObject worldObject)
        {
            if (!IsSupported(worldObject))
            {
                return false;
            }

            if (!worldObject.Spawned)
            {
                return false;
            }

            if (!worldObject.Tile.Valid ||
                Find.World.Impassable(worldObject.Tile) ||
                !worldObject.Tile.LayerDef.canFormCaravans)
            {
                return false;
            }

            if (holders == null ||
                !TransportersArrivalActionUtility.AnyPotentialCaravanOwner(holders, Faction.OfPlayer))
            {
                return false;
            }

            ThingWithComps shuttle;
            if (!ModularShuttleExistingMapLandingTargeter.TryResolveModularShuttle(holders, out shuttle))
            {
                return false;
            }

            return true;
        }

        internal static bool TryNotifyCaravanArrived(WorldObject worldObject, Caravan caravan)
        {
            if (worldObject == null || caravan == null)
            {
                return false;
            }

            MethodInfo method;
            if (!TryGetNotifyCaravanArrivedMethod(worldObject, out method))
            {
                return false;
            }

            try
            {
                method.Invoke(worldObject, new object[] { caravan });
                return true;
            }
            catch (TargetInvocationException exception)
            {
                Exception inner = exception.InnerException ?? exception;
                Log.Error("[CeleTech Shuttle] Milira caravan-world-object compatibility failed while notifying arrival. worldObject=" +
                    DescribeWorldObject(worldObject) +
                    " exception=" +
                    inner);
                return false;
            }
            catch (Exception exception)
            {
                Log.Error("[CeleTech Shuttle] Milira caravan-world-object compatibility failed while notifying arrival. worldObject=" +
                    DescribeWorldObject(worldObject) +
                    " exception=" +
                    exception);
                return false;
            }
        }

        private static string GetVisitLabel(WorldObject worldObject)
        {
            string defName = worldObject != null && worldObject.def != null ? worldObject.def.defName : null;
            string key;
            if (!defName.NullOrEmpty() && VisitTranslationKeys.TryGetValue(defName, out key))
            {
                return key.Translate(worldObject.Label).ToString();
            }

            return "ApproachSite".Translate(worldObject != null ? worldObject.Label : "unknown").ToString();
        }

        private static bool TryGetNotifyCaravanArrivedMethod(
            WorldObject worldObject,
            out MethodInfo method)
        {
            method = null;
            if (worldObject == null)
            {
                return false;
            }

            Type type = worldObject.GetType();
            if (NotifyCaravanArrivedMethods.TryGetValue(type, out method))
            {
                return method != null;
            }

            method = type.GetMethod(
                NotifyCaravanArrivedMethodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(Caravan) },
                null);
            NotifyCaravanArrivedMethods[type] = method;
            return method != null;
        }

        private static string DescribeWorldObject(WorldObject worldObject)
        {
            if (worldObject == null)
            {
                return "null";
            }

            return (worldObject.def != null ? worldObject.def.defName : worldObject.GetType().FullName) +
                " tile=" +
                (worldObject.Tile.Valid ? worldObject.Tile.ToString() : "invalid");
        }
    }
}

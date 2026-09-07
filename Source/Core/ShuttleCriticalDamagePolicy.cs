using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    internal static class ShuttleCriticalDamagePolicy
    {
        private const float CriticalHitPointsPct = 0.2f;

        internal static bool ShouldRestrictPlayerInteractions(Thing thing)
        {
            return IsCriticallyDamaged(thing) && !AllowsDevOrGodInteractions();
        }

        internal static bool TryRejectRestrictedInteraction(Thing thing)
        {
            if (!ShouldRestrictPlayerInteractions(thing))
            {
                return false;
            }

            Messages.Message(
                "CT_Shuttle_CriticalDamage_InteractionBlocked".Translate(
                    GetCriticalHitPointsPercentLabel()).ToString(),
                thing,
                MessageTypeDefOf.RejectInput,
                false);
            return true;
        }

        internal static bool TryConvertKillToCriticalDamage(Building building)
        {
            if (building == null || building.Destroyed)
            {
                return false;
            }

            int criticalHitPoints = GetCriticalHitPoints(building);
            if (criticalHitPoints <= 0)
            {
                return false;
            }

            if (building.HitPoints < criticalHitPoints)
            {
                building.HitPoints = criticalHitPoints;
            }

            NotifyRepairableChanged(building);
            return true;
        }

        internal static bool ClampToCriticalDamageFloor(Building building)
        {
            if (building == null || building.Destroyed)
            {
                return false;
            }

            int criticalHitPoints = GetCriticalHitPoints(building);
            if (criticalHitPoints <= 0 || building.HitPoints > criticalHitPoints)
            {
                return false;
            }

            if (building.HitPoints < criticalHitPoints)
            {
                building.HitPoints = criticalHitPoints;
            }

            NotifyRepairableChanged(building);
            return true;
        }

        internal static bool IsCriticallyDamaged(Thing thing)
        {
            int criticalHitPoints = GetCriticalHitPoints(thing);
            return criticalHitPoints > 0 &&
                thing != null &&
                !thing.Destroyed &&
                thing.HitPoints <= criticalHitPoints;
        }

        internal static string GetInspectString(Thing thing)
        {
            if (!IsCriticallyDamaged(thing))
            {
                return null;
            }

            return "CT_Shuttle_CriticalDamage_InspectLine".Translate(
                GetCriticalHitPointsPercentLabel()).ToString();
        }

        internal static void ClearBlockedDesignations(Building building)
        {
            if (!ShouldRestrictPlayerInteractions(building) ||
                building.Map == null ||
                building.Map.designationManager == null)
            {
                return;
            }

            RemoveDesignation(building, DesignationDefOf.Deconstruct);
            RemoveDesignation(building, DesignationDefOf.Uninstall);
        }

        private static int GetCriticalHitPoints(Thing thing)
        {
            if (thing == null || thing.def == null || !thing.def.useHitPoints)
            {
                return 0;
            }

            int maxHitPoints = Mathf.Max(0, thing.MaxHitPoints);
            if (maxHitPoints <= 0)
            {
                return 0;
            }

            return Mathf.Max(1, Mathf.CeilToInt(maxHitPoints * CriticalHitPointsPct));
        }

        private static string GetCriticalHitPointsPercentLabel()
        {
            return Mathf.RoundToInt(CriticalHitPointsPct * 100f).ToString();
        }

        private static bool AllowsDevOrGodInteractions()
        {
            return Prefs.DevMode || DebugSettings.godMode;
        }

        private static void NotifyRepairableChanged(Building building)
        {
            if (building == null ||
                !building.Spawned ||
                building.Map == null ||
                building.Map.listerBuildingsRepairable == null)
            {
                return;
            }

            building.Map.listerBuildingsRepairable.Notify_BuildingTookDamage(building);
        }

        private static void RemoveDesignation(Building building, DesignationDef designationDef)
        {
            if (building == null || designationDef == null)
            {
                return;
            }

            Designation designation = building.Map.designationManager.DesignationOn(
                building,
                designationDef);
            if (designation != null)
            {
                building.Map.designationManager.RemoveDesignation(designation);
            }
        }
    }
}

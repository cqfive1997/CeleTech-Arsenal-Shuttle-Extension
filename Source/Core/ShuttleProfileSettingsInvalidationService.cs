using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    /// <summary>
    /// Invalidates and reconciles loaded shuttle profiles after a global
    /// profile-affecting setting changes. The controller remains the owner of
    /// profile rebuild and external-host synchronization.
    /// </summary>
    internal static class ShuttleProfileSettingsInvalidationService
    {
        internal static void MarkLoadedShuttlesDirty(
            ShuttleController currentController,
            ProfileDirtyReason reason)
        {
            HashSet<ShuttleController> markedControllers =
                new HashSet<ShuttleController>();
            MarkController(currentController, reason, markedControllers);

            List<Map> maps = Find.Maps;
            for (int mapIndex = 0; maps != null && mapIndex < maps.Count; mapIndex++)
            {
                Map map = maps[mapIndex];
                List<Thing> buildings = map != null && map.listerThings != null
                    ? map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
                    : null;
                for (int thingIndex = 0;
                    buildings != null && thingIndex < buildings.Count;
                    thingIndex++)
                {
                    ThingWithComps host = buildings[thingIndex] as ThingWithComps;
                    CompModularShuttleCore core = host != null
                        ? host.TryGetComp<CompModularShuttleCore>()
                        : null;
                    MarkController(
                        core != null ? core.Controller : null,
                        reason,
                        markedControllers);
                }
            }
        }

        private static void MarkController(
            ShuttleController controller,
            ProfileDirtyReason reason,
            HashSet<ShuttleController> markedControllers)
        {
            if (controller == null ||
                reason == ProfileDirtyReason.None ||
                !markedControllers.Add(controller))
            {
                return;
            }

            controller.MarkProfileDirtyForSettings(reason);
            controller.ReconcileProfileToHost();
        }
    }
}

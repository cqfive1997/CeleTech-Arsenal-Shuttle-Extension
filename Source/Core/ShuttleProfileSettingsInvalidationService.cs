using System.Collections.Generic;
using System.Linq;
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

            if (maps == null)
            {
                return;
            }

            for (int mapIndex = 0; mapIndex < maps.Count; mapIndex++)
            {
                var listerThings = maps[mapIndex]?.listerThings;

                if (listerThings == null)
                {
                    continue;
                }

                var hosts = CompModularShuttleCore.GetOwnerThingDefs().Where(thingDef => thingDef.IsBuildingArtificial).SelectMany(listerThings.ThingsOfDef);

                foreach (var building in hosts)
                {
                    ThingWithComps host = building as ThingWithComps;
                    CompModularShuttleCore core = host?.TryGetComp<CompModularShuttleCore>();
                    MarkController(core?.Controller, reason, markedControllers);
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

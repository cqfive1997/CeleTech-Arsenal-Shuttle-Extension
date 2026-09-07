using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class SurfaceShieldVisualDevGizmoProvider
    {
        internal static IEnumerable<Gizmo> GetGizmos(CompModularShuttleSurfaceShieldVisual comp)
        {
            if (!Prefs.DevMode || !DebugSettings.godMode || comp == null || comp.parent == null)
            {
                yield break;
            }

            CompModularShuttleCore core = comp.parent.GetComp<CompModularShuttleCore>();
            if (core == null)
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = "DEV: Restore Surface Shield",
                defaultDesc = "Restores/recharges the active surface shield through the existing shield runtime debug command path.",
                action = core.FillActiveSurfaceShieldForDev
            };
        }
    }
}

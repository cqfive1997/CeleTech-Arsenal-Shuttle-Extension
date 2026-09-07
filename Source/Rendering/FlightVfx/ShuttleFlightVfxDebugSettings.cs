using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal static class ShuttleFlightVfxDebugSettings
    {
        private const bool DrawThrusterAnchors = false;
        private const bool LogThrusterLayoutCounts = false;
        private const bool LogThrusterDrawDecisions = false;
        private const bool ForceVisibleThrusters = false;

        internal static bool ShouldDrawPrototypeVfx
        {
            get
            {
                return Prefs.DevMode;
            }
        }

        internal static bool ShouldDrawThrusterAnchors
        {
            get
            {
                return Prefs.DevMode && DrawThrusterAnchors;
            }
        }

        internal static bool ShouldLogThrusterLayoutCounts
        {
            get
            {
                return Prefs.DevMode && LogThrusterLayoutCounts;
            }
        }

        internal static bool ShouldLogThrusterDrawDecisions
        {
            get
            {
                return Prefs.DevMode && LogThrusterDrawDecisions;
            }
        }

        internal static bool ShouldForceVisibleThrusters
        {
            get
            {
                return Prefs.DevMode && ForceVisibleThrusters;
            }
        }
    }
}

using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx.Vapor
{
    internal static class ShuttleFlightVfxVaporDebugSettings
    {
        private const bool DrawVapor = false;
        private const bool DrawVaporRibbon = true;
        private const bool DrawVaporFlecks = true;
        private const bool LogVaporAvailability = false;
        private const bool LogVaporDrawDecisions = false;
        private const bool LogVaporMeshDiagnostics = true;
        private const bool CalibrationMode = false;
        private const bool VaporOnlyDebug = false;
        private const bool TintCoreAnchorColors = false;
        private const string AnchorIdFilter = "";
        private static readonly ShuttleFlightVfxVaporDebugKindFilter KindFilter =
            ShuttleFlightVfxVaporDebugKindFilter.All;
        internal const bool ForceVisibleVapor = false;

        internal static bool ShouldDrawVapor
        {
            get
            {
                return DrawVapor;
            }
        }

        internal static bool ShouldDrawVaporRibbon
        {
            get
            {
                return DrawVapor && DrawVaporRibbon;
            }
        }

        internal static bool ShouldDrawVaporFlecks
        {
            get
            {
                return DrawVapor && DrawVaporFlecks;
            }
        }

        internal static bool ShouldLogAvailability
        {
            get
            {
                return DrawVapor && Prefs.DevMode && LogVaporAvailability;
            }
        }

        internal static bool ShouldLogDrawDecisions
        {
            get
            {
                return DrawVapor && Prefs.DevMode && LogVaporDrawDecisions;
            }
        }

        internal static bool ShouldLogMeshDiagnostics
        {
            get
            {
                return DrawVapor && Prefs.DevMode && LogVaporMeshDiagnostics;
            }
        }

        internal static bool ShouldForceVisibleVapor
        {
            get
            {
                return DrawVapor && Prefs.DevMode && ForceVisibleVapor;
            }
        }

        internal static bool ShouldDrawCalibrationOverlay
        {
            get
            {
                return DrawVapor && Prefs.DevMode && CalibrationMode;
            }
        }

        internal static bool ShouldUseVaporOnlyDebug
        {
            get
            {
                return DrawVapor && Prefs.DevMode && VaporOnlyDebug;
            }
        }

        internal static bool ShouldTintCoreAnchors
        {
            get
            {
                return DrawVapor && Prefs.DevMode && (VaporOnlyDebug || TintCoreAnchorColors);
            }
        }

        internal static bool ShouldDrawKind(ShuttleFlightVfxVaporKind kind)
        {
            if (!Prefs.DevMode)
            {
                return true;
            }

            if (KindFilter == ShuttleFlightVfxVaporDebugKindFilter.EdgeRibbonOnly)
            {
                return kind == ShuttleFlightVfxVaporKind.EdgeRibbon;
            }

            if (KindFilter == ShuttleFlightVfxVaporDebugKindFilter.ShoulderPuffOnly)
            {
                return kind == ShuttleFlightVfxVaporKind.ShoulderPuff;
            }

            if (KindFilter == ShuttleFlightVfxVaporDebugKindFilter.CurlPuffOnly)
            {
                return kind == ShuttleFlightVfxVaporKind.CurlPuff;
            }

            return true;
        }

        internal static bool ShouldDrawAnchorId(string anchorId)
        {
            if (!Prefs.DevMode ||
                string.IsNullOrEmpty(AnchorIdFilter))
            {
                return true;
            }

            return anchorId == AnchorIdFilter;
        }
    }

    internal enum ShuttleFlightVfxVaporDebugKindFilter
    {
        All,
        EdgeRibbonOnly,
        ShoulderPuffOnly,
        CurlPuffOnly
    }
}

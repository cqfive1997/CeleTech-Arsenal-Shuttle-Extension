using System.Globalization;
using System.Text;
using CombatExtended;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal sealed class CeRuntimeProbeMuzzleLineTrace
    {
        private const int MaxReportedCoverCells = 16;

        internal int VisitedCellCount { get; private set; }

        internal int ReportedCoverCellCount { get; private set; }

        internal float HorizontalDistance { get; private set; }

        internal float ShotHeight { get; private set; }

        internal float EffectiveMinRange { get; private set; }

        internal float EffectiveRange { get; private set; }

        internal string CoverSummary { get; private set; }

        internal static CeRuntimeProbeMuzzleLineTrace Capture(
            Map map,
            Thing caster,
            Thing targetThing,
            Vector3 shotSource,
            IntVec3 targetLocation,
            float effectiveMinRange,
            float effectiveRange)
        {
            CeRuntimeProbeMuzzleLineTrace trace = new CeRuntimeProbeMuzzleLineTrace();
            trace.ShotHeight = shotSource.y;
            trace.EffectiveMinRange = effectiveMinRange;
            trace.EffectiveRange = effectiveRange;
            Vector3 targetPosition = targetThing != null
                ? new Vector3(
                    targetThing.DrawPos.x,
                    new CollisionVertical(targetThing).Max,
                    targetThing.DrawPos.z)
                : targetLocation.ToVector3Shifted();
            Vector3 horizontalDelta = targetPosition - shotSource;
            trace.HorizontalDistance = Mathf.Sqrt(
                horizontalDelta.x * horizontalDelta.x +
                horizontalDelta.z * horizontalDelta.z);

            if (map == null)
            {
                trace.CoverSummary = "map-null";
                return trace;
            }

            LightingTracker lightingTracker = map.GetLightingTracker();
            Ray ray = new Ray(shotSource, targetPosition - shotSource);
            IntVec3 sourceCell = shotSource.ToIntVec3();
            StringBuilder summary = new StringBuilder(320);
            foreach (IntVec3 cell in GenSightCE.PointsOnLineOfSight(
                shotSource,
                targetLocation.ToVector3Shifted()))
            {
                trace.VisitedCellCount++;
                if (cell == sourceCell || cell == targetLocation)
                {
                    continue;
                }

                Thing selected = GridsUtility.GetFirstPawn(cell, map) ??
                    GridsUtility.GetCover(cell, map);
                if (selected == null)
                {
                    continue;
                }

                if (trace.ReportedCoverCellCount >= MaxReportedCoverCells)
                {
                    continue;
                }

                if (summary.Length > 0)
                {
                    summary.Append('|');
                }

                Bounds bounds = CE_Utility.GetBoundsFor(selected);
                summary.Append(cell.ToString());
                summary.Append(':');
                summary.Append(selected.def != null ? selected.def.defName : "def-null");
                summary.Append(":caster=");
                summary.Append(ReferenceEquals(selected, caster));
                summary.Append(":target=");
                summary.Append(ReferenceEquals(selected, targetThing));
                summary.Append(":plant=");
                summary.Append(selected.IsPlant());
                summary.Append(":fillage=");
                summary.Append(selected.def != null
                    ? selected.def.Fillage.ToString()
                    : "null");
                summary.Append(":coverHeight=");
                summary.Append(lightingTracker != null
                    ? lightingTracker.HighestCoverAt(cell).ToString(
                        "0.###",
                        CultureInfo.InvariantCulture)
                    : "null");
                summary.Append(":boundsHit=");
                summary.Append(bounds.IntersectRay(ray));
                trace.ReportedCoverCellCount++;
            }

            trace.CoverSummary = summary.Length > 0 ? summary.ToString() : "none";
            return trace;
        }
    }
}

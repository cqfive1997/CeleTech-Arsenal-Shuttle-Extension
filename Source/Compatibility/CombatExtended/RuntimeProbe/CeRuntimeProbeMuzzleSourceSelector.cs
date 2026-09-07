using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal static class CeRuntimeProbeMuzzleSourceSelector
    {
        internal static List<IntVec3> BuildAllowedCells(Thing host)
        {
            List<IntVec3> cells = new List<IntVec3>();
            if (host == null || !host.Spawned || host.Map == null)
            {
                return cells;
            }

            CellRect bounds = CeRuntimeProbeMuzzleSource.GetSelectionBounds(host);
            foreach (IntVec3 cell in bounds)
            {
                if (cell.InBounds(host.Map))
                {
                    cells.Add(cell);
                }
            }

            return cells;
        }

        internal static bool CanSelect(
            Thing host,
            LocalTargetInfo target)
        {
            return target.IsValid && CanSelectCell(host, target.Cell);
        }

        internal static bool CanSelect(
            Thing host,
            TargetInfo target)
        {
            return target.IsValid && CanSelectCell(host, target.Cell);
        }

        private static bool CanSelectCell(Thing host, IntVec3 cell)
        {
            return host != null &&
                host.Spawned &&
                host.Map != null &&
                cell.IsValid &&
                cell.InBounds(host.Map) &&
                CeRuntimeProbeMuzzleSource.GetSelectionBounds(host)
                    .Contains(cell);
        }

        internal static bool TryCapture(
            Thing host,
            LocalTargetInfo target,
            out CeRuntimeProbeMuzzleSource source)
        {
            source = null;
            if (!CanSelect(host, target))
            {
                return false;
            }

            Vector3 drawPosition = UI.MouseMapPosition();
            drawPosition.y = host.DrawPos.y;
            CeRuntimeProbeMuzzleSource captured =
                new CeRuntimeProbeMuzzleSource(target.Cell, drawPosition);
            if (!captured.IsValidFor(host))
            {
                return false;
            }

            source = captured;
            return true;
        }

        internal static void DrawAllowedArea(
            List<IntVec3> allowedCells,
            LocalTargetInfo target)
        {
            if (allowedCells != null && allowedCells.Count > 0)
            {
                GenDraw.DrawFieldEdges(allowedCells, Color.cyan);
            }

            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }

        internal static void DrawCommittedSource(
            CeRuntimeProbeMuzzleSource source,
            LocalTargetInfo target)
        {
            if (source == null)
            {
                return;
            }

            GenDraw.DrawTargetHighlightWithLayer(
                source.DrawPosition,
                AltitudeLayer.MetaOverlays);
            if (target.IsValid)
            {
                GenDraw.DrawLineBetween(
                    source.DrawPosition,
                    target.CenterVector3,
                    SimpleColor.Cyan,
                    0.15f);
            }
        }
    }
}

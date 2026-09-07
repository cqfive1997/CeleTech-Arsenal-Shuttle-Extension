using System.Globalization;
using System.Text;
using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Failure-only diagnostic for the transient production shot probe. It is never called by the
    /// ordinary firing hot path and does not alter CE's line decision.
    /// </summary>
    internal static class CeWeaponShotFailureTrace
    {
        private const int MaxReportedCoverCells = 16;

        internal static string Capture(
            Thing host,
            CeWeaponMuzzleVerb verb,
            LocalTargetInfo target)
        {
            if (host == null || verb == null || host.Map == null || !target.IsValid)
            {
                return "context-missing";
            }

            Vector3 muzzle = verb.LastMuzzleDrawPos;
            Vector3 trueCenter = GenThing.TrueCenter(host);
            Vector3 shotSource = new Vector3(muzzle.x, trueCenter.y, muzzle.z);
            Thing targetThing = target.Thing;
            Vector3 targetPosition = targetThing != null
                ? new Vector3(
                    targetThing.DrawPos.x,
                    new CollisionVertical(targetThing).Max,
                    targetThing.DrawPos.z)
                : target.Cell.ToVector3Shifted();
            Vector3 delta = targetPosition - shotSource;
            float horizontalDistance = Mathf.Sqrt(
                delta.x * delta.x + delta.z * delta.z);

            StringBuilder output = new StringBuilder(480);
            output.Append("distance=")
                .Append(horizontalDistance.ToString("0.###", CultureInfo.InvariantCulture))
                .Append(" min=")
                .Append(verb.verbProps.minRange.ToString("0.###", CultureInfo.InvariantCulture))
                .Append(" max=")
                .Append(verb.EffectiveRange.ToString("0.###", CultureInfo.InvariantCulture))
                .Append(" covers=");

            Ray ray = new Ray(shotSource, targetPosition - shotSource);
            IntVec3 sourceCell = shotSource.ToIntVec3();
            int reported = 0;
            foreach (IntVec3 cell in GenSightCE.PointsOnLineOfSight(
                shotSource,
                target.Cell.ToVector3Shifted()))
            {
                if (cell == sourceCell || cell == target.Cell)
                {
                    continue;
                }

                Thing selected = GridsUtility.GetFirstPawn(cell, host.Map) ??
                    GridsUtility.GetCover(cell, host.Map);
                if (selected == null || reported >= MaxReportedCoverCells)
                {
                    continue;
                }

                if (reported > 0)
                {
                    output.Append('|');
                }

                Bounds bounds = CE_Utility.GetBoundsFor(selected);
                output.Append(cell)
                    .Append(':')
                    .Append(selected.def != null ? selected.def.defName : "def-null")
                    .Append(":caster=").Append(ReferenceEquals(selected, host))
                    .Append(":target=").Append(ReferenceEquals(selected, targetThing))
                    .Append(":fillage=")
                    .Append(selected.def != null ? selected.def.Fillage.ToString() : "null")
                    .Append(":boundsHit=").Append(bounds.IntersectRay(ray));
                reported++;
            }

            if (reported == 0)
            {
                output.Append("none");
            }

            return output.ToString();
        }
    }
}

using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal sealed class CeRuntimeProbeMuzzleObservation
    {
        internal bool ReplacementInstalled { get; set; }

        internal int ShootLineCallCount { get; private set; }

        internal int CollisionRayCallCount { get; private set; }

        internal int ShiftCallCount { get; private set; }

        internal int SourceInjectionCount { get; private set; }

        internal int SuccessfulShotCount { get; private set; }

        internal int SpawnedProjectileCount { get; private set; }

        internal IntVec3 IncomingRoot { get; private set; }

        internal IntVec3 AppliedRoot { get; private set; }

        internal IntVec3 ResultingLineSource { get; private set; }

        internal bool ShootLineAccepted { get; private set; }

        internal Vector2 CollisionRaySourceBeforeInjection { get; private set; }

        internal Vector2 CollisionRaySource { get; private set; }

        internal bool CollisionRaySourceInjected { get; private set; }

        internal CeRuntimeProbeMuzzleLineTrace LineTrace { get; private set; }

        internal Vector2 SourceBeforeInjection { get; private set; }

        internal Vector2 FinalProjectileSource { get; private set; }

        internal Vector2 ObservedProjectileOrigin { get; private set; }

        internal bool ProjectileOriginObserved { get; private set; }

        internal bool ProjectileOriginMatchesSource { get; private set; }

        internal float FinalShotAngle { get; private set; }

        internal float FinalShotRotation { get; private set; }

        internal float FinalDistance { get; private set; }

        internal bool HasMultiBarrelOffset { get; private set; }

        internal bool StockDrawPositionEligible { get; private set; }

        internal bool ProbeFlashRendered { get; private set; }

        internal void RecordShootLine(
            IntVec3 incomingRoot,
            IntVec3 appliedRoot,
            ShootLine resultingLine,
            bool accepted)
        {
            this.ShootLineCallCount++;
            this.IncomingRoot = incomingRoot;
            this.AppliedRoot = appliedRoot;
            this.ResultingLineSource = resultingLine.Source;
            this.ShootLineAccepted = accepted;
        }

        internal void RecordCollisionRaySource(
            Vector3 sourceBeforeInjection,
            Vector3 source,
            bool sourceInjected,
            CeRuntimeProbeMuzzleLineTrace lineTrace)
        {
            this.CollisionRayCallCount++;
            this.CollisionRaySourceBeforeInjection = new Vector2(
                sourceBeforeInjection.x,
                sourceBeforeInjection.z);
            this.CollisionRaySource = new Vector2(source.x, source.z);
            this.CollisionRaySourceInjected = sourceInjected;
            this.LineTrace = lineTrace;
        }

        internal void RecordShift(
            Vector2 sourceBeforeInjection,
            Vector2 finalProjectileSource,
            float shotAngle,
            float shotRotation,
            float distance,
            bool sourceInjected,
            bool hasMultiBarrelOffset)
        {
            this.ShiftCallCount++;
            if (sourceInjected)
            {
                this.SourceInjectionCount++;
            }

            this.SourceBeforeInjection = sourceBeforeInjection;
            this.FinalProjectileSource = finalProjectileSource;
            this.FinalShotAngle = shotAngle;
            this.FinalShotRotation = shotRotation;
            this.FinalDistance = distance;
            this.HasMultiBarrelOffset = hasMultiBarrelOffset;
        }

        internal void RecordSuccessfulShot(
            bool stockDrawPositionEligible,
            bool flashRendered)
        {
            this.SuccessfulShotCount++;
            this.StockDrawPositionEligible = stockDrawPositionEligible;
            this.ProbeFlashRendered = this.ProbeFlashRendered || flashRendered;
        }

        internal void RecordProjectileCreated()
        {
            this.SpawnedProjectileCount++;
        }

        internal void RecordProjectileOrigin(
            Vector2 projectileOrigin,
            Vector2 expectedSource)
        {
            this.ObservedProjectileOrigin = projectileOrigin;
            this.ProjectileOriginObserved = true;
            this.ProjectileOriginMatchesSource =
                (projectileOrigin - expectedSource).sqrMagnitude <= 0.000001f;
        }
    }
}

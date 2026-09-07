using System.Globalization;
using System.Text;
using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal static class CeRuntimeProbeMuzzleReportWriter
    {
        internal static void Write(
            CeRuntimeProbeMuzzleSession session,
            string phase,
            string note)
        {
            if (session == null)
            {
                Log.Message("[CeleTech Shuttle][CE Muzzle Probe] phase=" + phase +
                    " session=null");
                return;
            }

            CeRuntimeProbeMuzzleObservation observation = session.Observation;
            CeRuntimeProbeMuzzleSource source = session.MuzzleSource;
            CompAmmoUser ammo = session.GetAmmo();
            Vector3 hostCenter = session.Host != null
                ? GenThing.TrueCenter(session.Host)
                : Vector3.zero;
            StringBuilder builder = new StringBuilder(640);
            builder.Append("[CeleTech Shuttle][CE Muzzle Probe]");
            Append(builder, "phase", phase);
            Append(builder, "weapon", session.Gun != null
                ? session.Gun.def.defName
                : "null");
            Append(builder, "host", session.Host != null
                ? session.Host.LabelShortCap
                : "null");
            Append(builder, "hostRotation", session.Host != null
                ? session.Host.Rotation.ToString()
                : "null");
            Append(builder, "target", session.TargetDescription);
            Append(builder, "sourceCell", source != null
                ? source.Cell.ToString()
                : "invalid");
            Append(builder, "sourceMode", source != null && source.UsesExistingResolver
                ? "existing-resolver"
                : "manual");
            Append(builder, "resolverModule", source != null && source.UsesExistingResolver
                ? source.ModuleInstanceID
                : "not-applicable");
            Append(builder, "resolverModuleDef", source != null && source.UsesExistingResolver
                ? source.ModuleDefName
                : "not-applicable");
            Append(builder, "resolverWeaponDef", source != null && source.UsesExistingResolver
                ? source.WeaponDefName
                : "not-applicable");
            Append(builder, "resolverSlot", source != null && source.UsesExistingResolver
                ? source.ParentSlotID
                : "not-applicable");
            Append(builder, "resolvedUsesFallback", source != null &&
                source.UsesExistingResolver &&
                source.ResolverUsesFallback);
            Append(builder, "resolvedMuzzleIndex", source != null && source.UsesExistingResolver
                ? source.ResolverMuzzleIndex
                : -1);
            Append(builder, "sourceXZ", source != null
                ? Format(source.DrawPosition.x, source.DrawPosition.z)
                : "null");
            Append(builder, "hostCenterXZ", Format(hostCenter.x, hostCenter.z));
            Append(builder, "sourceDistance", source != null
                ? Format(HorizontalDistance(source.DrawPosition, hostCenter))
                : "null");
            Append(builder, "sourceNearHost", source != null &&
                source.IsNearHost(session.Host));
            Append(builder, "sourceDistinct", source != null &&
                HorizontalDistance(source.DrawPosition, hostCenter) > 0.05f);
            Append(builder, "replacementInstalled", observation != null &&
                observation.ReplacementInstalled);
            Append(builder, "lineCalls", observation != null
                ? observation.ShootLineCallCount
                : 0);
            Append(builder, "incomingRoot", observation != null
                ? observation.IncomingRoot.ToString()
                : "invalid");
            Append(builder, "appliedRoot", observation != null
                ? observation.AppliedRoot.ToString()
                : "invalid");
            Append(builder, "lineSource", observation != null
                ? observation.ResultingLineSource.ToString()
                : "invalid");
            Append(builder, "lineAccepted", observation != null &&
                observation.ShootLineAccepted);
            Append(builder, "lineRootMatches", observation != null &&
                source != null &&
                observation.AppliedRoot == source.Cell &&
                observation.ResultingLineSource == source.Cell);
            Append(builder, "collisionRayCalls", observation != null
                ? observation.CollisionRayCallCount
                : 0);
            Append(builder, "collisionRaySourceBeforeXZ", observation != null
                ? Format(observation.CollisionRaySourceBeforeInjection)
                : "null");
            Append(builder, "collisionRaySourceXZ", observation != null
                ? Format(observation.CollisionRaySource)
                : "null");
            Append(builder, "collisionRaySourceInjected", observation != null &&
                observation.CollisionRaySourceInjected);
            Append(builder, "collisionRaySourceMatches", observation != null &&
                source != null &&
                (observation.CollisionRaySource - new Vector2(
                    source.DrawPosition.x,
                    source.DrawPosition.z)).sqrMagnitude <= 0.000001f);
            CeRuntimeProbeMuzzleLineTrace lineTrace = observation != null
                ? observation.LineTrace
                : null;
            Append(builder, "lineDistance", lineTrace != null
                ? Format(lineTrace.HorizontalDistance)
                : "null");
            Append(builder, "lineShotHeight", lineTrace != null
                ? Format(lineTrace.ShotHeight)
                : "null");
            Append(builder, "lineEffectiveMinRange", lineTrace != null
                ? Format(lineTrace.EffectiveMinRange)
                : "null");
            Append(builder, "lineEffectiveRange", lineTrace != null
                ? Format(lineTrace.EffectiveRange)
                : "null");
            Append(builder, "lineVisitedCells", lineTrace != null
                ? lineTrace.VisitedCellCount
                : 0);
            Append(builder, "lineReportedCoverCells", lineTrace != null
                ? lineTrace.ReportedCoverCellCount
                : 0);
            Append(builder, "lineCoverTrace", lineTrace != null
                ? lineTrace.CoverSummary
                : "unavailable");
            Append(builder, "shiftCalls", observation != null
                ? observation.ShiftCallCount
                : 0);
            Append(builder, "sourceInjections", observation != null
                ? observation.SourceInjectionCount
                : 0);
            Append(builder, "sourceBeforeXZ", observation != null
                ? Format(observation.SourceBeforeInjection)
                : "null");
            Append(builder, "projectileSourceXZ", observation != null
                ? Format(observation.FinalProjectileSource)
                : "null");
            Append(builder, "projectileSourceDelta", observation != null && source != null
                ? Format(
                    observation.FinalProjectileSource.x - source.DrawPosition.x,
                    observation.FinalProjectileSource.y - source.DrawPosition.z)
                : "null");
            Append(builder, "spawnedProjectiles", observation != null
                ? observation.SpawnedProjectileCount
                : 0);
            Append(builder, "observedProjectileOriginXZ", observation != null &&
                observation.ProjectileOriginObserved
                ? Format(observation.ObservedProjectileOrigin)
                : "not-observed");
            Append(builder, "projectileOriginMatchesSource", observation != null &&
                observation.ProjectileOriginObserved &&
                observation.ProjectileOriginMatchesSource);
            Append(builder, "multiBarrelOffset", observation != null &&
                observation.HasMultiBarrelOffset);
            Append(builder, "shotAngle", observation != null
                ? Format(observation.FinalShotAngle)
                : "null");
            Append(builder, "shotRotation", observation != null
                ? Format(observation.FinalShotRotation)
                : "null");
            Append(builder, "distance", observation != null
                ? Format(observation.FinalDistance)
                : "null");
            Append(builder, "successfulShots", observation != null
                ? observation.SuccessfulShotCount
                : 0);
            Append(builder, "probeFlashRendered", observation != null &&
                observation.ProbeFlashRendered);
            Append(builder, "stockDrawPosEligible", observation != null &&
                observation.StockDrawPositionEligible);
            Append(builder, "castAttempted", session.CastAttempted);
            Append(builder, "castAccepted", session.CastAccepted);
            Append(builder, "active", session.Active);
            Append(builder, "trackerTicks", session.TrackerTickCount);
            Append(builder, "callbacks", session.CompletionCallbackCount);
            Append(builder, "initialLoaded", session.InitialLoadedCount);
            Append(builder, "loaded", ammo != null ? ammo.CurMagCount : -1);
            Append(builder, "consumed", ammo != null
                ? session.InitialLoadedCount - ammo.CurMagCount
                : -1);
            Append(builder, "ownerAttached", ammo != null &&
                session.AmmoOwner != null &&
                ReferenceEquals(ammo.turret, session.AmmoOwner.Turret));
            Append(builder, "ownerContext", session.AmmoOwner != null &&
                session.AmmoOwner.ContextMatches);
            Append(builder, "transient", true);
            Append(builder, "saveSupported", false);
            if (!string.IsNullOrEmpty(note))
            {
                Append(builder, "note", note);
            }

            Log.Message(builder.ToString());
        }

        private static float HorizontalDistance(Vector3 left, Vector3 right)
        {
            float x = left.x - right.x;
            float z = left.z - right.z;
            return Mathf.Sqrt(x * x + z * z);
        }

        private static string Format(Vector2 value)
        {
            return Format(value.x, value.y);
        }

        private static string Format(float x, float z)
        {
            return x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                z.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Format(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static void Append(StringBuilder builder, string key, object value)
        {
            builder.Append(' ');
            builder.Append(key);
            builder.Append('=');
            builder.Append(value ?? "null");
        }
    }
}

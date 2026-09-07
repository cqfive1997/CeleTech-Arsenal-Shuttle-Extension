using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal sealed class CeRuntimeProbeMuzzleVerb : Verb_ShootCE
    {
        private CeRuntimeProbeMuzzleSource muzzleSource;
        private CeRuntimeProbeMuzzleObservation observation;
        private ProjectileCE lastSpawnedProjectile;

        internal void Configure(
            CeRuntimeProbeMuzzleSource source,
            CeRuntimeProbeMuzzleObservation muzzleObservation)
        {
            this.muzzleSource = source;
            this.observation = muzzleObservation;
            this.drawPos = source != null ? source.DrawPosition : Vector3.zero;
        }

        public override bool TryFindCEShootLineFromTo(
            IntVec3 root,
            LocalTargetInfo target,
            out ShootLine resultingLine,
            out Vector3 targetPosition)
        {
            IntVec3 appliedRoot = this.HasValidSource
                ? this.muzzleSource.Cell
                : root;
            bool accepted = base.TryFindCEShootLineFromTo(
                appliedRoot,
                target,
                out resultingLine,
                out targetPosition);
            if (this.observation != null)
            {
                this.observation.RecordShootLine(
                    root,
                    appliedRoot,
                    resultingLine,
                    accepted);
            }

            return accepted;
        }

        public override bool TryCastShot()
        {
            this.lastSpawnedProjectile = null;
            bool accepted = base.TryCastShot();
            if (this.observation != null && this.lastSpawnedProjectile != null)
            {
                this.observation.RecordProjectileOrigin(
                    this.lastSpawnedProjectile.origin,
                    this.sourceLoc);
            }

            return accepted;
        }

        protected override bool CanHitCellFromCellIgnoringRange(
            Vector3 shotSource,
            IntVec3 targetLocation,
            Thing targetThing = null)
        {
            Vector3 appliedSource = shotSource;
            bool sourceInjected = false;
            if (this.HasValidSource)
            {
                appliedSource.x = this.muzzleSource.DrawPosition.x;
                appliedSource.z = this.muzzleSource.DrawPosition.z;
                sourceInjected = true;
            }

            if (this.observation != null)
            {
                LocalTargetInfo target = targetThing != null
                    ? new LocalTargetInfo(targetThing)
                    : new LocalTargetInfo(targetLocation);
                CeRuntimeProbeMuzzleLineTrace lineTrace =
                    CeRuntimeProbeMuzzleLineTrace.Capture(
                        this.caster != null ? this.caster.Map : null,
                        this.caster,
                        targetThing,
                        appliedSource,
                        targetLocation,
                        this.verbProps.EffectiveMinRange(target, this.caster),
                        this.EffectiveRange);
                this.observation.RecordCollisionRaySource(
                    shotSource,
                    appliedSource,
                    sourceInjected,
                    lineTrace);
            }

            return base.CanHitCellFromCellIgnoringRange(
                appliedSource,
                targetLocation,
                targetThing);
        }

        public override void ShiftTarget(
            ShiftVecReport report,
            Vector3 targetPosition,
            out float targetHeight,
            bool calculateMechanicalOnly = false,
            bool isInstant = false)
        {
            base.ShiftTarget(
                report,
                targetPosition,
                out targetHeight,
                calculateMechanicalOnly,
                isInstant);

            Vector2 sourceBeforeInjection = this.sourceLoc;
            bool sourceInjected = false;
            bool hasMultiBarrelOffset = this.multiBarrelExt != null;
            if (!calculateMechanicalOnly && this.HasValidSource)
            {
                this.ApplyMuzzleSource(targetHeight);
                sourceInjected = true;
            }

            if (this.observation != null)
            {
                this.observation.RecordShift(
                    sourceBeforeInjection,
                    this.sourceLoc,
                    this.shotAngle,
                    this.shotRotation,
                    this.distance,
                    sourceInjected,
                    hasMultiBarrelOffset);
            }
        }

        protected override bool OnCastSuccessful()
        {
            bool accepted = base.OnCastSuccessful();
            if (!accepted)
            {
                return false;
            }

            bool stockDrawPositionEligible = this.ShooterPawn != null;
            bool flashRendered = false;
            if (this.HasValidSource &&
                this.caster.Map != null &&
                this.VerbPropsCE != null &&
                this.VerbPropsCE.muzzleFlashScale > 0.01f)
            {
                FleckMakerCE.Static(
                    this.muzzleSource.DrawPosition,
                    this.caster.Map,
                    FleckDefOf.ShotFlash,
                    this.VerbPropsCE.muzzleFlashScale);
                flashRendered = true;
            }

            if (this.observation != null)
            {
                this.observation.RecordSuccessfulShot(
                    stockDrawPositionEligible,
                    flashRendered);
            }

            return true;
        }

        protected override ProjectileCE SpawnProjectile()
        {
            ProjectileCE projectile = base.SpawnProjectile();
            this.lastSpawnedProjectile = projectile;
            if (this.observation != null)
            {
                this.observation.RecordProjectileCreated();
            }

            return projectile;
        }

        private bool HasValidSource
        {
            get
            {
                return this.muzzleSource != null &&
                    this.muzzleSource.IsValidFor(this.caster);
            }
        }

        private void ApplyMuzzleSource(float targetHeight)
        {
            Vector3 originalSource = GenThing.TrueCenter(this.caster);
            Vector3 selectedSource = new Vector3(
                this.muzzleSource.DrawPosition.x,
                originalSource.y,
                this.muzzleSource.DrawPosition.z);
            Vector3 shiftedTarget = new Vector3(
                this.newTargetLoc.x,
                targetHeight,
                this.newTargetLoc.y);

            if (!this.LockRotationAndAngle)
            {
                float originalAngle = this.ShotAngle(
                    Vector3Utility.WithY(originalSource, this.ShotHeight),
                    shiftedTarget);
                float selectedAngle = this.ShotAngle(
                    Vector3Utility.WithY(selectedSource, this.ShotHeight),
                    shiftedTarget);
                this.shotAngle += selectedAngle - originalAngle;
                this.lastShotAngle = selectedAngle;

                float originalRotation = this.ShotRotation(
                    originalSource,
                    shiftedTarget);
                float selectedRotation = this.ShotRotation(
                    selectedSource,
                    shiftedTarget);
                this.shotRotation = NormalizeAngle(
                    this.shotRotation + Mathf.DeltaAngle(originalRotation, selectedRotation));
                this.lastShotRotation = selectedRotation;
            }

            this.sourceLoc.Set(
                this.muzzleSource.DrawPosition.x,
                this.muzzleSource.DrawPosition.z);
            MultiBarrelExtension extension = this.multiBarrelExt;
            if (extension != null)
            {
                this.sourceLoc += Vector2Utility.RotatedBy(
                    extension.GetOffsetFor(this.multiBarrelIndex),
                    this.shotRotation);
            }

            this.distance = (this.newTargetLoc - this.sourceLoc).magnitude;
        }

        private static float NormalizeAngle(float value)
        {
            value %= 360f;
            return value < 0f ? value + 360f : value;
        }
    }
}

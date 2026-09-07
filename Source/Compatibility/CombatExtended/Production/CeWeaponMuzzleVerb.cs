using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// CE shot consumer for the shuttle's existing authored muzzle resolver. It changes only the
    /// source geometry; CE retains line, cover, spread, collision, projectile and ammo policy.
    /// </summary>
    public sealed class CeWeaponMuzzleVerb :
        Verb_ShootCE,
        IShuttleWeaponValidationMuzzle
    {
        private static readonly ShuttleWeaponMuzzleResolver MuzzleResolver =
            new ShuttleWeaponMuzzleResolver();

        private string moduleInstanceID;
        private string parentSlotID;
        private ShuttleWeaponModuleDef weaponDef;
        private ShuttleWeaponRuntimeState runtimeState;
        private ShuttleWeaponMuzzleSource muzzleSource;
        private bool hasMuzzleSource;
        private ProjectileCE lastSpawnedProjectile;
        private Vector2 lastProjectileOrigin;
        private bool lastProjectileOriginObserved;
        private string lastMuzzleFailure;

        internal IntVec3 LastMuzzleCell
        {
            get { return this.hasMuzzleSource ? this.muzzleSource.Cell : IntVec3.Invalid; }
        }

        internal Vector3 LastMuzzleDrawPos
        {
            get { return this.hasMuzzleSource ? this.muzzleSource.DrawPos : Vector3.zero; }
        }

        internal bool LastProjectileOriginObserved
        {
            get { return this.lastProjectileOriginObserved; }
        }

        internal Vector2 LastProjectileOrigin
        {
            get { return this.lastProjectileOrigin; }
        }

        internal string LastMuzzleFailure
        {
            get { return this.lastMuzzleFailure; }
        }

        internal void ConfigureRuntimeBinding(
            string boundModuleInstanceID,
            string boundParentSlotID,
            ShuttleWeaponModuleDef boundWeaponDef,
            ShuttleWeaponRuntimeState boundRuntimeState)
        {
            this.moduleInstanceID = boundModuleInstanceID;
            this.parentSlotID = boundParentSlotID;
            this.weaponDef = boundWeaponDef;
            this.runtimeState = boundRuntimeState;
            this.hasMuzzleSource = false;
            this.lastMuzzleFailure = null;
        }

        internal void ClearRuntimeBinding()
        {
            this.moduleInstanceID = null;
            this.parentSlotID = null;
            this.weaponDef = null;
            this.runtimeState = null;
            this.hasMuzzleSource = false;
            this.lastMuzzleFailure = null;
        }

        internal bool TryPrepareMuzzle(
            LocalTargetInfo target,
            bool advanceMuzzle,
            out string failureReason)
        {
            failureReason = null;
            this.hasMuzzleSource = false;
            if (this.caster == null ||
                !this.caster.Spawned ||
                string.IsNullOrEmpty(this.moduleInstanceID) ||
                this.weaponDef == null ||
                this.runtimeState == null ||
                !target.IsValid ||
                !target.Cell.IsValid ||
                !target.Cell.InBounds(this.caster.Map))
            {
                failureReason = "ce-muzzle-binding-missing";
                this.lastMuzzleFailure = failureReason;
                return false;
            }

            ShuttleWeaponMuzzleSource resolved = MuzzleResolver.Resolve(
                this.caster as ThingWithComps,
                this.parentSlotID,
                this.weaponDef,
                this.runtimeState,
                target,
                advanceMuzzle);
            if (!IsValidSource(this.caster, resolved))
            {
                failureReason = "ce-muzzle-source-invalid";
                this.lastMuzzleFailure = failureReason;
                return false;
            }

            if (advanceMuzzle)
            {
                this.runtimeState.SetLastResolvedMuzzleForRuntimeOnly(
                    resolved.Cell,
                    resolved.DrawPos);
            }

            this.muzzleSource = resolved;
            this.hasMuzzleSource = true;
            this.drawPos = resolved.DrawPos;
            this.lastMuzzleFailure = null;
            return true;
        }

        bool IShuttleWeaponValidationMuzzle.TrySetValidationMuzzle(
            ShuttleWeaponMuzzleSource source)
        {
            if (!IsValidSource(this.caster, source))
            {
                return false;
            }

            this.muzzleSource = source;
            this.hasMuzzleSource = true;
            this.drawPos = source.DrawPos;
            return true;
        }

        public override bool TryFindCEShootLineFromTo(
            IntVec3 root,
            LocalTargetInfo target,
            out ShootLine resultingLine,
            out Vector3 targetPosition)
        {
            if (!this.HasValidSource)
            {
                resultingLine = default(ShootLine);
                targetPosition = Vector3.zero;
                return false;
            }

            return base.TryFindCEShootLineFromTo(
                this.muzzleSource.Cell,
                target,
                out resultingLine,
                out targetPosition);
        }

        public override bool TryCastShot()
        {
            string failureReason;
            if (!this.TryPrepareMuzzle(this.CurrentTarget, true, out failureReason))
            {
                return false;
            }

            this.lastSpawnedProjectile = null;
            this.lastProjectileOriginObserved = false;
            bool accepted = base.TryCastShot();
            if (accepted && this.lastSpawnedProjectile != null)
            {
                this.lastProjectileOrigin = this.lastSpawnedProjectile.origin;
                this.lastProjectileOriginObserved = true;
            }

            return accepted;
        }

        protected override bool CanHitCellFromCellIgnoringRange(
            Vector3 shotSource,
            IntVec3 targetLocation,
            Thing targetThing = null)
        {
            if (this.HasValidSource)
            {
                shotSource.x = this.muzzleSource.DrawPos.x;
                shotSource.z = this.muzzleSource.DrawPos.z;
            }

            return base.CanHitCellFromCellIgnoringRange(
                shotSource,
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

            if (!calculateMechanicalOnly && this.HasValidSource)
            {
                this.ApplyMuzzleSource(targetHeight);
            }
        }

        protected override bool OnCastSuccessful()
        {
            bool accepted = base.OnCastSuccessful();
            if (accepted &&
                this.HasValidSource &&
                this.caster.Map != null &&
                this.VerbPropsCE != null &&
                this.VerbPropsCE.muzzleFlashScale > 0.01f)
            {
                FleckMakerCE.Static(
                    this.muzzleSource.DrawPos,
                    this.caster.Map,
                    FleckDefOf.ShotFlash,
                    this.VerbPropsCE.muzzleFlashScale);
            }

            return accepted;
        }

        protected override ProjectileCE SpawnProjectile()
        {
            ProjectileCE projectile = base.SpawnProjectile();
            this.lastSpawnedProjectile = projectile;
            return projectile;
        }

        private bool HasValidSource
        {
            get { return this.hasMuzzleSource && IsValidSource(this.caster, this.muzzleSource); }
        }

        private void ApplyMuzzleSource(float targetHeight)
        {
            Vector3 originalSource = GenThing.TrueCenter(this.caster);
            Vector3 selectedSource = new Vector3(
                this.muzzleSource.DrawPos.x,
                originalSource.y,
                this.muzzleSource.DrawPos.z);
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

                float originalRotation = this.ShotRotation(originalSource, shiftedTarget);
                float selectedRotation = this.ShotRotation(selectedSource, shiftedTarget);
                this.shotRotation = NormalizeAngle(
                    this.shotRotation + Mathf.DeltaAngle(originalRotation, selectedRotation));
                this.lastShotRotation = selectedRotation;
            }

            this.sourceLoc.Set(
                this.muzzleSource.DrawPos.x,
                this.muzzleSource.DrawPos.z);
            MultiBarrelExtension extension = this.multiBarrelExt;
            if (extension != null)
            {
                this.sourceLoc += Vector2Utility.RotatedBy(
                    extension.GetOffsetFor(this.multiBarrelIndex),
                    this.shotRotation);
            }

            this.distance = (this.newTargetLoc - this.sourceLoc).magnitude;
        }

        private static bool IsValidSource(Thing caster, ShuttleWeaponMuzzleSource source)
        {
            return caster != null &&
                caster.Spawned &&
                caster.Map != null &&
                source.Cell.IsValid &&
                source.Cell.InBounds(caster.Map) &&
                IsFinite(source.DrawPos.x) &&
                IsFinite(source.DrawPos.z) &&
                source.DrawPos.ToIntVec3() == source.Cell;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float NormalizeAngle(float value)
        {
            value %= 360f;
            return value < 0f ? value + 360f : value;
        }
    }
}

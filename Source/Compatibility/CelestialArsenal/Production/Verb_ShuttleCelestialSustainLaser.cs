using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CeleTech.ShuttleExtension.ModularShuttle.Weapons;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    /// <summary>
    /// Thin Verse lifecycle bridge for the shuttle-owned HPJ-L01 clone. Beam presentation,
    /// retargeting and damage remain in focused collaborators.
    /// </summary>
    public sealed class Verb_ShuttleCelestialSustainLaser : Verb,
        IShuttleMuzzleVerb,
        IShuttleWeaponValidationMuzzle
    {
        private readonly CelestialSustainLaserBeamEffects beamEffects =
            new CelestialSustainLaserBeamEffects();
        private readonly CelestialSustainLaserRetargetController retargetController =
            new CelestialSustainLaserRetargetController();
        private readonly CelestialSustainLaserMuzzleState muzzle =
            new CelestialSustainLaserMuzzleState();
        private readonly CelestialSustainLaserShotExecutor shotExecutor =
            new CelestialSustainLaserShotExecutor();
        private readonly CelestialSustainLaserCompAccess compAccess =
            new CelestialSustainLaserCompAccess();

        private Vector3 lastBeamEndPos = Vector3.zero;

        internal CompProperties_ShuttleSustainLaserData LaserProps
        {
            get
            {
                CompShuttleSustainLaserData data = this.LaserData;
                return data != null ? data.Props : null;
            }
        }

        internal int BurstShotsLeftForDiagnostics
        {
            get { return this.burstShotsLeft; }
        }

        internal int TicksToNextPulseForDiagnostics
        {
            get { return this.ticksToNextBurstShot; }
        }

        internal int LastPulseTickForDiagnostics
        {
            get { return this.lastShotTick; }
        }

        internal bool BeamActiveForDiagnostics
        {
            get { return this.beamEffects.IsActive; }
        }

        private CompShuttleSustainLaserData LaserData
        {
            get
            {
                return this.compAccess.GetData(this.EquipmentSource);
            }
        }

        protected override int ShotsPerBurst
        {
            get { return this.verbProps != null ? this.verbProps.burstShotCount : 1; }
        }

        public override float? AimAngleOverride
        {
            get
            {
                if (this.state != VerbState.Bursting || !this.muzzle.HasSource)
                {
                    return null;
                }

                return (this.ResolveBeamEndPosition() - this.muzzle.DrawPos).AngleFlat();
            }
        }

        public void SetShuttleMuzzleSource(IntVec3 sourceCell, Vector3 sourceDrawPos)
        {
            this.muzzle.Set(sourceCell, sourceDrawPos);
        }

        public void SetShuttleFireControlTuning(
            float directFireAccuracyMultiplier,
            float directFireAccuracyBonus,
            float directFireAccuracyFloor,
            float forcedMissRadiusMultiplier)
        {
            // The Celestial sustained laser is deterministic direct damage. These projectile
            // accuracy inputs deliberately have no effect, matching the source weapon contract.
        }

        bool IShuttleWeaponValidationMuzzle.TrySetValidationMuzzle(
            ShuttleWeaponMuzzleSource source)
        {
            return this.muzzle.TrySet(source);
        }

        public override void WarmupComplete()
        {
            if (!this.HasUsableMuzzle())
            {
                this.Reset();
                return;
            }

            base.WarmupComplete();
            this.lastBeamEndPos = this.CurrentTarget.IsValid
                ? this.CurrentTarget.CenterVector3
                : this.muzzle.DrawPos;
            this.beamEffects.Begin(
                this.Caster,
                this.muzzle.Cell,
                this.muzzle.DrawPos,
                this.ResolveBeamEndPosition(),
                this.LaserProps);
        }

        public override void BurstingTick()
        {
            this.TryRetargetIfNeeded();
            base.BurstingTick();

            Vector3 beamEnd = this.ResolveBeamEndPosition();
            this.lastBeamEndPos = beamEnd;
            this.retargetController.RefreshMovingEndpoint(this.currentTarget);
            if (!this.beamEffects.IsActive && this.state == VerbState.Bursting)
            {
                this.beamEffects.Begin(
                    this.Caster,
                    this.muzzle.Cell,
                    this.muzzle.DrawPos,
                    beamEnd,
                    this.LaserProps);
            }

            this.beamEffects.Update(
                this.Caster,
                this.muzzle.Cell,
                this.muzzle.DrawPos,
                beamEnd,
                this.burstShotsLeft > 0 && this.state == VerbState.Bursting);
        }

        public override void Reset()
        {
            base.Reset();
            this.beamEffects.End();
            this.retargetController.Reset();
        }

        protected override bool TryCastShot()
        {
            this.TryRetargetIfNeeded();
            if (!this.HasUsableMuzzle())
            {
                return false;
            }

            CelestialSustainLaserShotResult result = this.shotExecutor.Execute(
                this.Caster,
                this.EquipmentSource,
                this.verbProps,
                this.currentTarget,
                this.muzzle.Cell,
                this.LaserData,
                this.retargetController,
                this.HasLineOfSightFromMuzzle);
            if (result == CelestialSustainLaserShotResult.Failed)
            {
                return false;
            }

            if (result == CelestialSustainLaserShotResult.Applied)
            {
                this.beamEffects.NotifyDamagePulse(
                    this.Caster,
                    this.ResolveBeamEndPosition(),
                    this.LaserProps);
            }

            this.lastShotTick = Find.TickManager != null
                ? Find.TickManager.TicksGame
                : -1;
            return true;
        }

        private bool HasUsableMuzzle()
        {
            return this.muzzle.IsUsable(this.caster);
        }

        private Vector3 ResolveBeamEndPosition()
        {
            return this.retargetController.ResolveBeamEnd(
                this.currentTarget,
                this.lastBeamEndPos != Vector3.zero
                    ? this.lastBeamEndPos
                    : this.muzzle.DrawPos);
        }

        private void TryRetargetIfNeeded()
        {
            if (this.burstShotsLeft <= 0 || this.caster == null || this.caster.Map == null)
            {
                return;
            }

            LocalTargetInfo nextTarget;
            if (this.retargetController.TryBeginRetarget(
                    this.Caster,
                    this.currentTarget,
                    this.ResolveBeamEndPosition(),
                    this.LaserProps,
                    this.burstShotsLeft,
                    this.CanRetargetTo,
                    out nextTarget))
            {
                this.currentTarget = nextTarget;
            }
        }

        private bool CanRetargetTo(LocalTargetInfo target)
        {
            if (!target.IsValid ||
                !this.CanHitTargetFrom(this.muzzle.Cell, target))
            {
                return false;
            }

            if (this.verbProps == null || !this.verbProps.requireLineOfSight)
            {
                return true;
            }

            ShootLine line;
            return this.TryFindShootLineFromTo(
                this.muzzle.Cell,
                target,
                out line,
                false);
        }

        private bool HasLineOfSightFromMuzzle(LocalTargetInfo target)
        {
            ShootLine line;
            return this.TryFindShootLineFromTo(
                this.muzzle.Cell,
                target,
                out line,
                false);
        }

    }
}

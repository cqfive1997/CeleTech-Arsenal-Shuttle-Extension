using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CombatExtended;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Owns one bounded transient burst. The installed runtime state remains a read-only muzzle
    /// input; the prepared CE gun and its owner token are released when the batch reports.
    /// </summary>
    internal sealed class CeWeaponProductionBurstProbeSession
    {
        private const int MaxTrackerTicks = 2000;
        private const float OriginTolerance = 0.001f;

        private readonly CeWeaponProductionBurstProbeReport report;
        private readonly ShuttleWeaponAmmoState sourceAmmo;
        private readonly string sourceAmmoDefName;
        private readonly Thing preparedGun;
        private readonly CompAmmoUser magazine;
        private readonly CeWeaponVerbTrackerDriver verbTrackerDriver =
            new CeWeaponVerbTrackerDriver();

        private CeWeaponAmmoOwnerAdapter owner;
        private CeWeaponMuzzleVerb verb;
        private bool active;
        private bool finished;
        private bool released;

        internal CeWeaponProductionBurstProbeSession(
            CeWeaponProductionBurstProbeReport report,
            ShuttleWeaponAmmoState sourceAmmo,
            string sourceAmmoDefName,
            Thing preparedGun,
            CompAmmoUser magazine)
        {
            this.report = report;
            this.sourceAmmo = sourceAmmo;
            this.sourceAmmoDefName = sourceAmmoDefName;
            this.preparedGun = preparedGun;
            this.magazine = magazine;
        }

        internal CeWeaponProductionBurstProbeReport Report
        {
            get { return this.report; }
        }

        internal bool Finished
        {
            get { return this.finished; }
        }

        internal void Attach(
            CeWeaponMuzzleVerb boundVerb,
            CeWeaponAmmoOwnerAdapter boundOwner)
        {
            this.verb = boundVerb;
            this.owner = boundOwner;
            this.report.OwnerReady = boundOwner != null;
        }

        internal void Begin(
            CeWeaponShotLifecycle lifecycle,
            Thing host,
            LocalTargetInfo target)
        {
            if (this.finished)
            {
                return;
            }

            this.active = true;
            string failureReason;
            this.report.CastAccepted = lifecycle.TryStart(
                this.verb,
                target,
                out failureReason);
            if (this.report.CastAccepted)
            {
                return;
            }

            this.active = false;
            this.report.Failure = failureReason;
            this.report.FailureTrace = CeWeaponShotFailureTrace.Capture(
                host,
                this.verb,
                target);
            this.Complete();
        }

        internal void NotifyCastComplete()
        {
            this.report.CompletionCallbacks++;
            this.active = false;
        }

        internal void Fail(string failureReason)
        {
            if (!string.IsNullOrEmpty(failureReason))
            {
                this.report.Failure = failureReason;
            }

            this.active = false;
            this.Complete();
        }

        internal void Tick()
        {
            if (this.finished)
            {
                return;
            }

            if (!this.active)
            {
                this.Complete();
                return;
            }

            this.report.TrackerTicks++;
            string failureReason;
            if (!this.verbTrackerDriver.TryTick(
                    this.preparedGun,
                    out failureReason))
            {
                this.Fail(failureReason);
                return;
            }
            if (!this.active)
            {
                this.Complete();
                return;
            }

            if (this.report.TrackerTicks >= MaxTrackerTicks)
            {
                this.Fail("ce-burst-completion-timeout");
            }
        }

        internal void Release()
        {
            if (this.released)
            {
                return;
            }

            this.released = true;
            if (this.owner != null)
            {
                this.owner.Release(this.magazine);
                this.owner = null;
            }

            CeWeaponRuntimeGunAccess.ReleaseVerb(this.preparedGun);
        }

        private void Complete()
        {
            if (this.finished)
            {
                return;
            }

            this.finished = true;
            this.active = false;
            this.report.CeLoadedAfter = this.magazine != null
                ? this.magazine.CurMagCount
                : -1;
            this.report.SourceLoadedAfter = this.sourceAmmo != null
                ? this.sourceAmmo.LoadedAmmoCount
                : -1;
            this.report.SourceUnchanged = this.sourceAmmo != null &&
                this.sourceAmmo.SelectedAmmoDefName == this.sourceAmmoDefName &&
                this.report.SourceLoadedAfter == this.report.SourceLoadedBefore;

            if (this.verb != null)
            {
                this.report.MuzzleCell = this.verb.LastMuzzleCell;
                this.report.MuzzleDrawPos = this.verb.LastMuzzleDrawPos;
                this.report.ProjectileOriginObserved =
                    this.verb.LastProjectileOriginObserved;
                if (this.report.ProjectileOriginObserved)
                {
                    Vector2 origin = this.verb.LastProjectileOrigin;
                    this.report.ProjectileOriginMatches =
                        Mathf.Abs(origin.x - this.report.MuzzleDrawPos.x) <= OriginTolerance &&
                        Mathf.Abs(origin.y - this.report.MuzzleDrawPos.z) <= OriginTolerance;
                }
            }

            if (string.IsNullOrEmpty(this.report.Failure))
            {
                if (this.report.CompletionCallbacks != 1)
                {
                    this.report.Failure = "ce-burst-completion-callback-mismatch";
                }
                else if (this.report.CeLoadedBefore - this.report.CeLoadedAfter !=
                    this.report.ExpectedShots)
                {
                    this.report.Failure = "ce-burst-ammo-delta-mismatch";
                }
                else if (!this.report.SourceUnchanged)
                {
                    this.report.Failure = "legacy-source-mutated-by-ce-burst-probe";
                }
                else if (!this.report.ProjectileOriginObserved ||
                    !this.report.ProjectileOriginMatches)
                {
                    this.report.Failure = "ce-burst-projectile-origin-mismatch";
                }
            }
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CombatExtended;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Bounded isolated-state probe for one target-to-cooldown CE production cycle.
    /// </summary>
    internal sealed class CeWeaponProductionCycleProbeSession
    {
        private const int MaxTicks = 3000;
        private const float OriginTolerance = 0.001f;

        private readonly CeWeaponProductionCycleProbeReport report;
        private readonly ShuttleWeaponAmmoState sourceAmmo;
        private readonly string sourceAmmoDefName;
        private readonly ShuttleWeaponRuntimeState cycleState;
        private readonly ShuttleRuntimeState shuttleRuntimeState;
        private readonly ShuttleModuleRuntimeContext context;
        private readonly CeWeaponCycleDriver cycleDriver;
        private readonly CeWeaponFiringPowerProjection powerProjection;
        private readonly CeWeaponProductionCycleProbePowerSink powerSink;
        private readonly Thing preparedGun;
        private readonly CompAmmoUser magazine;

        private CeWeaponAmmoOwnerAdapter owner;
        private CeWeaponMuzzleVerb verb;
        private bool finished;
        private bool released;
        private bool callbackObserved;
        private int tickCount;
        private int burstStartTick = -1;

        internal CeWeaponProductionCycleProbeSession(
            CeWeaponProductionCycleProbeReport report,
            ShuttleWeaponAmmoState sourceAmmo,
            string sourceAmmoDefName,
            ShuttleWeaponRuntimeState cycleState,
            ShuttleRuntimeState shuttleRuntimeState,
            ShuttleModuleRuntimeContext context,
            CeWeaponCycleDriver cycleDriver,
            CeWeaponFiringPowerProjection powerProjection,
            CeWeaponProductionCycleProbePowerSink powerSink,
            Thing preparedGun,
            CompAmmoUser magazine)
        {
            this.report = report;
            this.sourceAmmo = sourceAmmo;
            this.sourceAmmoDefName = sourceAmmoDefName;
            this.cycleState = cycleState;
            this.shuttleRuntimeState = shuttleRuntimeState;
            this.context = context;
            this.cycleDriver = cycleDriver;
            this.powerProjection = powerProjection;
            this.powerSink = powerSink;
            this.preparedGun = preparedGun;
            this.magazine = magazine;
        }

        internal CeWeaponProductionCycleProbeReport Report
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
        }

        internal void NotifyCastComplete()
        {
            this.report.CompletionCallbacks++;
            this.callbackObserved = true;
            this.report.CooldownTicksAtCompletion =
                this.cycleState.GetCooldownTicksForRuntimeOnly();
            if (this.burstStartTick >= 0)
            {
                this.report.BurstTrackerTicks = this.tickCount - this.burstStartTick;
            }

            this.cycleState.ClearForcedTargetForRuntimeOnly();
            this.cycleState.ResetCurrentTargetForRuntimeOnly();
        }

        internal void Fail(string failureReason)
        {
            if (!string.IsNullOrEmpty(failureReason))
            {
                this.report.Failure = failureReason;
            }

            this.Complete();
        }

        internal void Tick()
        {
            if (this.finished)
            {
                return;
            }

            this.tickCount++;
            this.report.TotalTicks = this.tickCount;
            int ticksGame = Find.TickManager != null
                ? Find.TickManager.TicksGame
                : this.tickCount;
            this.context.RefreshPowerDemandFrame(
                this.shuttleRuntimeState,
                this.powerSink,
                ticksGame);
            this.powerSink.BeginFrame();
            this.powerProjection.Collect(this.context, this.verb);
            this.powerSink.EndFrame();

            if (this.cycleState.GetWarmupTicksForRuntimeOnly() > 0)
            {
                this.report.ObservedWarmupTicks++;
            }

            string failureReason;
            if (!this.cycleDriver.Tick(this.context, this.verb, out failureReason))
            {
                this.Fail(failureReason);
                return;
            }

            if (!this.report.TargetAcquired &&
                this.cycleState.GetCurrentTargetForRuntimeOnly().IsValid)
            {
                this.report.TargetAcquired = true;
            }

            if (!this.report.BurstStarted &&
                this.verb != null &&
                this.verb.state == VerbState.Bursting)
            {
                this.report.BurstStarted = true;
                this.burstStartTick = this.tickCount;
            }

            if (this.callbackObserved &&
                this.cycleState.GetCooldownTicksForRuntimeOnly() <= 0)
            {
                this.report.CooldownCompleted = true;
                this.Complete();
                return;
            }

            if (this.tickCount >= MaxTicks)
            {
                this.Fail("ce-production-cycle-timeout");
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
            this.report.CeLoadedAfter = this.magazine != null
                ? this.magazine.CurMagCount
                : -1;
            this.report.SourceLoadedAfter = this.sourceAmmo != null
                ? this.sourceAmmo.LoadedAmmoCount
                : -1;
            this.report.SourceUnchanged = this.sourceAmmo != null &&
                this.sourceAmmo.SelectedAmmoDefName == this.sourceAmmoDefName &&
                this.report.SourceLoadedAfter == this.report.SourceLoadedBefore;
            this.report.PowerDemandTicks = this.powerSink != null
                ? this.powerSink.DemandTicks
                : 0;
            this.report.PeakPowerWatts = this.powerSink != null
                ? this.powerSink.PeakWatts
                : 0f;

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

            if (string.IsNullOrEmpty(this.report.Failure) && !this.report.Passed)
            {
                this.report.Failure = "ce-production-cycle-postcondition-failed";
            }
        }
    }
}

using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    internal sealed class CeWeaponProductionCycleProbeReport
    {
        internal string ModuleInstanceID { get; set; }
        internal string ModuleDefName { get; set; }
        internal int HostRotation { get; set; }
        internal string Target { get; set; }
        internal int ExpectedShots { get; set; }
        internal int ExpectedWarmupTicks { get; set; }
        internal int ObservedWarmupTicks { get; set; }
        internal int ExpectedCooldownTicks { get; set; }
        internal int CooldownTicksAtCompletion { get; set; }
        internal bool CooldownCompleted { get; set; }
        internal int TotalTicks { get; set; }
        internal int BurstTrackerTicks { get; set; }
        internal int SourceLoadedBefore { get; set; }
        internal int SourceLoadedAfter { get; set; }
        internal int CeLoadedBefore { get; set; }
        internal int CeLoadedAfter { get; set; }
        internal int CompletionCallbacks { get; set; }
        internal bool OwnerReady { get; set; }
        internal bool TargetAccepted { get; set; }
        internal bool TargetAcquired { get; set; }
        internal bool BurstStarted { get; set; }
        internal bool SourceUnchanged { get; set; }
        internal int PowerDemandTicks { get; set; }
        internal float ExpectedPowerWatts { get; set; }
        internal float PeakPowerWatts { get; set; }
        internal IntVec3 MuzzleCell { get; set; }
        internal Vector3 MuzzleDrawPos { get; set; }
        internal bool ProjectileOriginObserved { get; set; }
        internal bool ProjectileOriginMatches { get; set; }
        internal string Failure { get; set; }

        internal bool Passed
        {
            get
            {
                return this.ExpectedShots > 1 &&
                    this.OwnerReady &&
                    this.TargetAccepted &&
                    this.TargetAcquired &&
                    this.BurstStarted &&
                    this.CompletionCallbacks == 1 &&
                    this.ObservedWarmupTicks == this.ExpectedWarmupTicks &&
                    this.CooldownTicksAtCompletion == this.ExpectedCooldownTicks &&
                    this.CooldownCompleted &&
                    this.CeLoadedBefore - this.CeLoadedAfter == this.ExpectedShots &&
                    this.SourceUnchanged &&
                    this.PowerDemandTicks > 0 &&
                    Mathf.Abs(this.PeakPowerWatts - this.ExpectedPowerWatts) <= 0.01f &&
                    this.MuzzleCell.IsValid &&
                    this.ProjectileOriginObserved &&
                    this.ProjectileOriginMatches &&
                    string.IsNullOrEmpty(this.Failure);
            }
        }
    }
}

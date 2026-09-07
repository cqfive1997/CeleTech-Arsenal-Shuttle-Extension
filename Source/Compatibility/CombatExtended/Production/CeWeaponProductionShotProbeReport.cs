using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    internal sealed class CeWeaponProductionShotProbeReport
    {
        internal string ModuleInstanceID { get; set; }

        internal string ModuleDefName { get; set; }

        internal int SourceLoadedBefore { get; set; }

        internal int SourceLoadedAfter { get; set; }

        internal int CeLoadedBefore { get; set; }

        internal int CeLoadedAfter { get; set; }

        internal int CompletionCallbacks { get; set; }

        internal bool OwnerReady { get; set; }

        internal bool CastAccepted { get; set; }

        internal bool SourceUnchanged { get; set; }

        internal IntVec3 MuzzleCell { get; set; }

        internal Vector3 MuzzleDrawPos { get; set; }

        internal bool ProjectileOriginObserved { get; set; }

        internal bool ProjectileOriginMatches { get; set; }

        internal string Failure { get; set; }

        internal string FailureTrace { get; set; }

        internal bool Passed
        {
            get
            {
                return this.OwnerReady &&
                    this.CastAccepted &&
                    this.CompletionCallbacks == 1 &&
                    this.CeLoadedBefore - this.CeLoadedAfter == 1 &&
                    this.SourceUnchanged &&
                    this.MuzzleCell.IsValid &&
                    this.ProjectileOriginObserved &&
                    this.ProjectileOriginMatches &&
                    string.IsNullOrEmpty(this.Failure);
            }
        }
    }
}

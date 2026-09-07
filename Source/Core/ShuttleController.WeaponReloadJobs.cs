using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    public sealed partial class ShuttleController
    {
        internal bool TryGetManualWeaponReloadPlan(
            Pawn pawn,
            out ShuttleWeaponManualReloadPlan plan,
            out string reason)
        {
            plan = null;
            reason = null;
            if (pawn == null || pawn.Dead || pawn.Downed || pawn.Drafted)
            {
                reason = "CT_Shuttle_WeaponAmmo_NoManualReloadJob".Translate().ToString();
                return false;
            }

            if (this.moduleRuntimeCoordinator == null)
            {
                reason = "CT_Shuttle_Command_WeaponRuntimeCoordinatorUnavailable".Translate().ToString();
                return false;
            }

            this.ReconcileProfileToHost();
            this.EnsureRuntimeState();
            return this.moduleRuntimeCoordinator.CanManualWeaponReloadJob(
                this.shuttleHost,
                this.assemblyState,
                this.GetProfileForRead(),
                this.runtimeState,
                this.cargoBackend,
                this.powerSystem,
                out plan,
                out reason);
        }

        internal bool TryClaimManualWeaponReloadJob(
            Pawn pawn,
            int jobLoadID,
            ThingDef carriedAmmoThingDef,
            out ShuttleWeaponManualReloadPlan plan,
            out string reason)
        {
            plan = null;
            reason = null;
            if (this.moduleRuntimeCoordinator == null)
            {
                reason = "CT_Shuttle_Command_WeaponRuntimeCoordinatorUnavailable".Translate().ToString();
                return false;
            }

            this.ReconcileProfileToHost();
            this.EnsureRuntimeState();
            return this.moduleRuntimeCoordinator.TryClaimManualWeaponReloadJob(
                this.shuttleHost,
                this.assemblyState,
                this.GetProfileForRead(),
                this.runtimeState,
                this.cargoBackend,
                this.powerSystem,
                pawn,
                jobLoadID,
                carriedAmmoThingDef,
                out plan,
                out reason);
        }

        internal bool TryCompleteManualWeaponReloadJob(
            string moduleInstanceID,
            Pawn pawn,
            int jobLoadID,
            out string reason)
        {
            reason = null;
            if (this.moduleRuntimeCoordinator == null)
            {
                reason = "CT_Shuttle_Command_WeaponRuntimeCoordinatorUnavailable".Translate().ToString();
                return false;
            }

            this.ReconcileProfileToHost();
            this.EnsureRuntimeState();
            return this.moduleRuntimeCoordinator.TryCompleteManualWeaponReloadJob(
                this.shuttleHost,
                this.assemblyState,
                this.GetProfileForRead(),
                this.runtimeState,
                this.cargoBackend,
                this.powerSystem,
                moduleInstanceID,
                pawn,
                jobLoadID,
                out reason);
        }

        internal void ReleaseManualWeaponReloadJob(
            string moduleInstanceID,
            Pawn pawn,
            int jobLoadID)
        {
            if (this.moduleRuntimeCoordinator == null ||
                this.assemblyState == null ||
                this.runtimeState == null ||
                string.IsNullOrEmpty(moduleInstanceID))
            {
                return;
            }

            this.moduleRuntimeCoordinator.TryReleaseManualWeaponReloadJob(
                this.assemblyState,
                this.runtimeState,
                moduleInstanceID,
                pawn,
                jobLoadID);
        }

        internal bool IsManualWeaponReloadJobActive(
            string moduleInstanceID,
            Pawn pawn,
            int jobLoadID)
        {
            if (this.runtimeState == null ||
                this.runtimeState.Modules == null ||
                string.IsNullOrEmpty(moduleInstanceID))
            {
                return false;
            }

            IShuttleModuleRuntimeState runtimePayload;
            if (!this.runtimeState.Modules.TryGetState(
                moduleInstanceID,
                ShuttleWeaponRuntimeSystem.WeaponRuntimeSystemKey,
                out runtimePayload))
            {
                return false;
            }

            ShuttleWeaponRuntimeState weaponState = runtimePayload as ShuttleWeaponRuntimeState;
            return weaponState != null &&
                weaponState.AmmoForRuntimeOnly != null &&
                weaponState.AmmoForRuntimeOnly.IsManualReloadClaim(
                    pawn != null ? pawn.thingIDNumber : -1,
                    jobLoadID);
        }
    }
}

using System;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Adapts the existing Pawn hauling Job to the sole CE magazine. The carried stack is consumed
    /// only after the magazine accepts the exact delta, and the magazine is rolled back on failure.
    /// </summary>
    internal sealed class CeWeaponManualReloadDriver : IShuttleWeaponManualReloadDriver
    {
        private readonly CeWeaponReloadDriver reloadDriver;
        private readonly CeWeaponMagazineAccessor magazineAccessor =
            new CeWeaponMagazineAccessor();

        internal CeWeaponManualReloadDriver(CeWeaponReloadDriver reloadDriver)
        {
            this.reloadDriver = reloadDriver;
        }

        public bool TryGetPlan(
            ShuttleModuleRuntimeContext context,
            out ShuttleWeaponManualReloadPlan plan,
            out string failureReason)
        {
            return this.TryBuildPlan(
                context,
                false,
                null,
                -1,
                null,
                out plan,
                out failureReason);
        }

        public bool TryClaim(
            ShuttleModuleRuntimeContext context,
            Pawn pawn,
            int jobLoadID,
            ThingDef requiredAmmoThingDef,
            out ShuttleWeaponManualReloadPlan plan,
            out string failureReason)
        {
            return this.TryBuildPlan(
                context,
                true,
                pawn,
                jobLoadID,
                requiredAmmoThingDef,
                out plan,
                out failureReason);
        }

        public bool TryComplete(
            ShuttleModuleRuntimeContext context,
            Pawn pawn,
            int jobLoadID,
            out string failureReason)
        {
            failureReason = null;
            ShuttleWeaponRuntimeState state;
            ShuttleWeaponModuleDef weaponDef;
            CompAmmoUser magazine;
            AmmoDef ammoDef;
            ShuttleWeaponAmmoState commonState;
            if (!TryResolve(
                    context,
                    out state,
                    out weaponDef,
                    out magazine,
                    out ammoDef,
                    out commonState,
                    out failureReason) ||
                pawn == null ||
                !commonState.IsManualReloadClaim(pawn.thingIDNumber, jobLoadID))
            {
                if (string.IsNullOrEmpty(failureReason))
                {
                    failureReason = "CT_Shuttle_WeaponAmmo_NoManualReloadJob"
                        .Translate()
                        .ToString();
                }

                return false;
            }

            CeWeaponMuzzleVerb verb = CeWeaponRuntimeGunAccess.GetMuzzleVerb(
                state.GunForRuntimeOnly);
            Thing carried = pawn.carryTracker != null
                ? pawn.carryTracker.CarriedThing
                : null;
            int needed = magazine.MagSize - magazine.CurMagCount;
            if (verb == null || verb.state == VerbState.Bursting ||
                carried == null || carried.Destroyed || carried.def != ammoDef ||
                carried.stackCount <= 0 || needed <= 0)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_Incompatible".Translate().ToString();
                return false;
            }

            int offered = Math.Min(needed, carried.stackCount);
            int loadedBefore = magazine.CurMagCount;
            string mutationFailure;
            if (!this.magazineAccessor.TryStageExact(
                    magazine,
                    ammoDef,
                    loadedBefore + offered,
                    out mutationFailure))
            {
                failureReason = "CT_Shuttle_WeaponAmmo_ReloadFailed".Translate().ToString();
                return false;
            }

            if (!TryConsumeCarriedExact(carried, offered))
            {
                string rollbackFailure;
                bool rolledBack = this.magazineAccessor.TryStageExact(
                    magazine,
                    ammoDef,
                    loadedBefore,
                    out rollbackFailure);
                failureReason = rolledBack
                    ? "CT_Shuttle_WeaponAmmo_ReloadFailed".Translate().ToString()
                    : "ce-pawn-reload-magazine-rollback-failed";
                return false;
            }

            ShuttleWeaponReloadRequestKind completedKind =
                commonState.ActiveReloadRequestKind != ShuttleWeaponReloadRequestKind.None
                    ? commonState.ActiveReloadRequestKind
                    : commonState.ReloadRequestKind;
            ShuttleWeaponReloadRequestKind continuationKind =
                ShuttleWeaponReloadPolicy.ResolveContinuation(
                    completedKind,
                    commonState.ReloadRequestKind);
            commonState.FinishReload();
            if (magazine.CurMagCount < magazine.MagSize)
            {
                commonState.RequestReload(
                    continuationKind != ShuttleWeaponReloadRequestKind.None
                        ? continuationKind
                        : ShuttleWeaponReloadRequestKind.Manual);
            }

            if (ShuttleDiagnosticGate.ShouldLogDetailedPerformanceBreakdowns)
            {
                Log.Message(
                    "[CeleTech Shuttle][CE Reload] pawn commit" +
                    " module=" + context.ModuleInstanceID +
                    " weapon=" + weaponDef.defName +
                    " ammo=" + ammoDef.defName +
                    " magazine=" + loadedBefore + "->" + magazine.CurMagCount +
                    " committed=" + offered);
            }

            return true;
        }

        private bool TryBuildPlan(
            ShuttleModuleRuntimeContext context,
            bool claim,
            Pawn pawn,
            int jobLoadID,
            ThingDef requiredAmmoThingDef,
            out ShuttleWeaponManualReloadPlan plan,
            out string failureReason)
        {
            plan = null;
            ShuttleWeaponRuntimeState state;
            ShuttleWeaponModuleDef weaponDef;
            CompAmmoUser magazine;
            AmmoDef ammoDef;
            ShuttleWeaponAmmoState commonState;
            if (!TryResolve(
                    context,
                    out state,
                    out weaponDef,
                    out magazine,
                    out ammoDef,
                    out commonState,
                    out failureReason))
            {
                return false;
            }

            CeWeaponMuzzleVerb verb = CeWeaponRuntimeGunAccess.GetMuzzleVerb(
                state.GunForRuntimeOnly);
            ShuttleWeaponModuleAmmoExtension extension =
                weaponDef.GetModExtension<ShuttleWeaponModuleAmmoExtension>();
            if (extension == null || verb == null || verb.state == VerbState.Bursting ||
                !commonState.ReloadRequested ||
                commonState.ReloadInProgress ||
                commonState.ManualReloadJobActive ||
                commonState.ReloadExecutorKind != ShuttleWeaponReloadExecutorKind.None ||
                !commonState.ManualReloadAllowed ||
                commonState.ReloadRequestKind == ShuttleWeaponReloadRequestKind.AutoTopOff ||
                magazine.CurMagCount >= magazine.MagSize)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoManualReloadJob"
                    .Translate()
                    .ToString();
                return false;
            }

            string cargoFailure;
            if (this.reloadDriver != null &&
                this.reloadDriver.CanHandleFromCargo(context, magazine, out cargoFailure))
            {
                failureReason = null;
                return false;
            }

            if (requiredAmmoThingDef != null && requiredAmmoThingDef != ammoDef)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_Incompatible".Translate().ToString();
                return false;
            }

            if (claim &&
                (pawn == null ||
                 !commonState.TryMarkManualReloadJobActive(
                    context.TicksGame,
                    pawn.thingIDNumber,
                    jobLoadID)))
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoManualReloadJob"
                    .Translate()
                    .ToString();
                return false;
            }

            plan = new ShuttleWeaponManualReloadPlan(
                context.ModuleInstanceID,
                ammoDef,
                magazine.MagSize - magazine.CurMagCount,
                Math.Max(1, extension.reloadWorkTicks));
            failureReason = null;
            return true;
        }

        private static bool TryResolve(
            ShuttleModuleRuntimeContext context,
            out ShuttleWeaponRuntimeState state,
            out ShuttleWeaponModuleDef weaponDef,
            out CompAmmoUser magazine,
            out AmmoDef ammoDef,
            out ShuttleWeaponAmmoState commonState,
            out string failureReason)
        {
            state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            weaponDef = context != null
                ? context.ModuleDef as ShuttleWeaponModuleDef
                : null;
            magazine = state != null &&
                state.MagazineAuthorityBackendIdForRuntimeOnly == CeWeaponBackendFactory.Id
                    ? CeWeaponRuntimeGunAccess.GetMagazine(state.GunForRuntimeOnly)
                    : null;
            ammoDef = magazine != null
                ? magazine.SelectedAmmo ?? magazine.CurrentAmmo
                : null;
            commonState = state != null ? state.AmmoForRuntimeOnly : null;
            failureReason = null;
            if (context == null || state == null || weaponDef == null ||
                magazine == null || ammoDef == null || commonState == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString();
                return false;
            }

            return true;
        }

        private static bool TryConsumeCarriedExact(Thing carried, int count)
        {
            if (carried == null || carried.Destroyed || count <= 0 ||
                carried.stackCount < count)
            {
                return false;
            }

            if (carried.stackCount == count)
            {
                carried.Destroy(DestroyMode.Vanish);
                return true;
            }

            Thing consumed = carried.SplitOff(count);
            if (consumed == null || consumed.Destroyed || consumed.stackCount != count)
            {
                return false;
            }

            consumed.Destroy(DestroyMode.Vanish);
            return true;
        }
    }
}

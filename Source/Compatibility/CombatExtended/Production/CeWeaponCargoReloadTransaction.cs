using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Commits one synchronous cargo-stack delta to the CE magazine and verifies exact
    /// conservation. Reload scheduling and power admission remain in the reload driver.
    /// </summary>
    internal sealed class CeWeaponCargoReloadTransaction
    {
        private const int PartialContinuationDelayTicks = 45;

        private readonly CeWeaponMagazineAccessor magazineAccessor =
            new CeWeaponMagazineAccessor();

        internal bool TryCommit(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState commonState,
            CompAmmoUser magazine,
            out string failureReason)
        {
            failureReason = null;
            AmmoDef ammoDef = magazine != null
                ? magazine.SelectedAmmo ?? magazine.CurrentAmmo
                : null;
            int loadedBefore = magazine != null ? magazine.CurMagCount : -1;
            int needed = magazine != null ? magazine.MagSize - loadedBefore : 0;
            if (context == null || commonState == null || magazine == null ||
                ammoDef == null || needed <= 0)
            {
                failureReason = "ce-cargo-reload-context-invalid";
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
            ShuttleWeaponCargoCommitResult result;
            string mutationFailure = null;
            string transactionFailure;
            bool committed = ShuttleWeaponCargoCommitIntegrationPort.TryCommit(
                context.Host,
                context.CargoResourceBroker,
                ammoDef.defName,
                needed,
                "CE shuttle weapon reload",
                delegate(int offered)
                {
                    return magazine.CurMagCount == loadedBefore &&
                        offered > 0 &&
                        offered <= needed &&
                        this.magazineAccessor.TryStageExact(
                            magazine,
                            ammoDef,
                            loadedBefore + offered,
                            out mutationFailure);
                },
                delegate
                {
                    string rollbackFailure;
                    return this.magazineAccessor.TryStageExact(
                        magazine,
                        ammoDef,
                        loadedBefore,
                        out rollbackFailure);
                },
                out result,
                out transactionFailure);
            failureReason = !string.IsNullOrEmpty(transactionFailure)
                ? transactionFailure
                : mutationFailure;
            if (!committed || result == null || !result.CargoConsumed ||
                result.OfferedCount <= 0 ||
                magazine.CurMagCount - loadedBefore != result.OfferedCount)
            {
                if (string.IsNullOrEmpty(failureReason))
                {
                    failureReason = "ce-cargo-reload-postcondition-failed";
                }

                return false;
            }

            commonState.FinishReload();
            if (magazine.CurMagCount < magazine.MagSize &&
                context.CargoResourceBroker.CountStored(ammoDef) > 0)
            {
                commonState.RequestReload(
                    continuationKind != ShuttleWeaponReloadRequestKind.None
                        ? continuationKind
                        : ShuttleWeaponReloadRequestKind.RequiredForFire);
                commonState.ScheduleAutomaticReloadRetry(
                    context.TicksGame,
                    PartialContinuationDelayTicks);
            }

            if (ShuttleDiagnosticGate.ShouldLogDetailedPerformanceBreakdowns)
            {
                Log.Message(
                    "[CeleTech Shuttle][CE Reload] cargo commit" +
                    " module=" + Safe(context.ModuleInstanceID) +
                    " weapon=" + Safe(weaponDef != null ? weaponDef.defName : null) +
                    " ammo=" + ammoDef.defName +
                    " magazine=" + loadedBefore + "->" + magazine.CurMagCount +
                    " cargo=" + result.StoredBefore + "->" + result.StoredAfter +
                    " committed=" + result.OfferedCount);
            }

            return true;
        }

        private static string Safe(string value)
        {
            return string.IsNullOrEmpty(value) ? "<unknown>" : value;
        }
    }
}

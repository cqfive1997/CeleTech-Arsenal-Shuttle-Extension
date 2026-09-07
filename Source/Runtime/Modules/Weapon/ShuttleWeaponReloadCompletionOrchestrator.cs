using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Applies retry and partial-continuation aftermath around one completed core transaction.
    /// It does not select supplies, advance work, own Pawn Jobs or mutate magazine contents.
    /// </summary>
    internal sealed class ShuttleWeaponReloadCompletionOrchestrator
    {
        private const string ReloadConsumeReason = "shuttle weapon reload";
        private readonly ShuttleWeaponAmmoDefinitionCatalog ammoDefinitions;
        private readonly CoreMagazineDriver coreMagazine;
        private readonly ShuttleWeaponReloadCoordinator reloadCoordinator;
        private readonly ShuttleWeaponReloadCompletionTransaction completionTransaction;

        internal ShuttleWeaponReloadCompletionOrchestrator(
            ShuttleWeaponAmmoDefinitionCatalog ammoDefinitions,
            CoreMagazineDriver coreMagazine,
            ShuttleWeaponReloadCoordinator reloadCoordinator,
            ShuttleWeaponReloadCompletionTransaction completionTransaction)
        {
            this.ammoDefinitions = ammoDefinitions ??
                new ShuttleWeaponAmmoDefinitionCatalog();
            this.coreMagazine = coreMagazine ?? new CoreMagazineDriver(this.ammoDefinitions);
            this.reloadCoordinator = reloadCoordinator ??
                new ShuttleWeaponReloadCoordinator();
            this.completionTransaction = completionTransaction ??
                new ShuttleWeaponReloadCompletionTransaction(this.coreMagazine, null);
        }

        internal bool TryComplete(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState,
            IShuttleWeaponAmmoSupply supply,
            string sourceUnavailableReason,
            out string failureReason)
        {
            failureReason = null;
            ShuttleWeaponModuleAmmoExtension extension =
                this.ammoDefinitions.GetExtension(weaponDef);
            if (extension == null || ammoState == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString();
                return false;
            }

            ShuttleWeaponReloadRequestKind requestKind =
                this.reloadCoordinator.ResolveAndRememberCompletionRequestKind(
                    ammoState,
                    this.coreMagazine.CanFire(weaponDef, ammoState));
            ShuttleWeaponReloadRequestKind pendingRequestKind = ammoState.ReloadRequestKind;
            ShuttleWeaponReloadCompletionResult completion =
                this.completionTransaction.Complete(
                    weaponDef,
                    ammoState,
                    supply,
                    "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString(),
                    !string.IsNullOrEmpty(sourceUnavailableReason)
                        ? sourceUnavailableReason
                        : "CT_Shuttle_WeaponAmmo_SourceUnavailable".Translate().ToString(),
                    "CT_Shuttle_WeaponAmmo_SourceEmpty".Translate().ToString(),
                    "CT_Shuttle_WeaponAmmo_ReloadFailed".Translate().ToString(),
                    ReloadConsumeReason);
            failureReason = completion.FailureReason;
            if (!completion.Succeeded)
            {
                this.reloadCoordinator.ScheduleAutomaticFailureRetry(
                    ammoState,
                    requestKind,
                    failureReason,
                    GetTicksGame(context));
                return false;
            }

            if (!completion.TransferCommitted)
            {
                ShuttleWeaponRuntimeProfiler.RecordReloadCompletion(
                    true, false, false, false, false, false);
                return true;
            }

            bool continuedRequiredForFire;
            bool continuedAutoTopOff;
            bool stoppedManual;
            bool stoppedSourceEmpty;
            this.ApplyContinuation(
                context,
                weaponDef,
                ammoState,
                extension,
                ShuttleWeaponReloadPolicy.ResolveContinuation(
                    requestKind,
                    pendingRequestKind),
                completion,
                out continuedRequiredForFire,
                out continuedAutoTopOff,
                out stoppedManual,
                out stoppedSourceEmpty);

            ShuttleWeaponRuntimeProfiler.RecordReloadCompletion(
                completion.FullReload,
                completion.PartialReload,
                continuedRequiredForFire,
                continuedAutoTopOff,
                stoppedManual,
                stoppedSourceEmpty);
            ShuttleWeaponReloadBaselineRecorder.RecordCompletion(
                context,
                weaponDef,
                ammoState,
                completion.SelectedAmmo,
                supply,
                requestKind,
                completion.CompletedExecutorKind,
                completion.LoadedBefore,
                completion.RequestedCount,
                completion.CommittedCount,
                completion.FullReload);
            return true;
        }

        private void ApplyContinuation(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState,
            ShuttleWeaponModuleAmmoExtension extension,
            ShuttleWeaponReloadRequestKind requestKind,
            ShuttleWeaponReloadCompletionResult completion,
            out bool continuedRequiredForFire,
            out bool continuedAutoTopOff,
            out bool stoppedManual,
            out bool stoppedSourceEmpty)
        {
            continuedRequiredForFire = false;
            continuedAutoTopOff = false;
            stoppedManual = false;
            stoppedSourceEmpty = false;
            if (!completion.PartialReload || ammoState == null || extension == null)
            {
                return;
            }

            ShuttleWeaponPartialReloadAction action =
                ShuttleWeaponReloadPolicy.ResolvePartialReloadAction(
                    requestKind,
                    ammoState.AutoReloadEnabled,
                    ammoState.LoadedAmmoCount,
                    extension.ammoPerShot,
                    this.coreMagazine.GetNeededLoadCount(weaponDef, ammoState) > 0,
                    completion.SourceStillHasAmmo);
            if (action == ShuttleWeaponPartialReloadAction.QueueManual)
            {
                this.reloadCoordinator.QueueRequest(
                    ammoState,
                    ShuttleWeaponReloadRequestKind.Manual);
                return;
            }

            if (action == ShuttleWeaponPartialReloadAction.QueueRequiredForFire)
            {
                string sourceEmptyReason =
                    "CT_Shuttle_WeaponAmmo_SourceEmpty".Translate().ToString();
                this.reloadCoordinator.QueueRequest(
                    ammoState,
                    ShuttleWeaponReloadRequestKind.RequiredForFire);
                if (!completion.SourceStillHasAmmo)
                {
                    ammoState.SetLastReloadBlockerReason(sourceEmptyReason);
                    ammoState.SetLastAutomaticReloadBlockerReason(sourceEmptyReason);
                }

                this.reloadCoordinator.ScheduleRetry(
                    ammoState,
                    GetTicksGame(context),
                    completion.SourceStillHasAmmo
                        ? ShuttleWeaponReloadPolicy.PartialReloadContinuationDelayTicks
                        : ShuttleWeaponReloadPolicy.GetAutomaticRetryDelayTicks(sourceEmptyReason));
                continuedRequiredForFire = true;
                return;
            }

            if (action == ShuttleWeaponPartialReloadAction.QueueAutoTopOff)
            {
                this.reloadCoordinator.QueueRequest(
                    ammoState,
                    ShuttleWeaponReloadRequestKind.AutoTopOff);
                this.reloadCoordinator.ScheduleRetry(
                    ammoState,
                    GetTicksGame(context),
                    ShuttleWeaponReloadPolicy.PartialReloadContinuationDelayTicks);
                continuedAutoTopOff = true;
                return;
            }

            stoppedSourceEmpty =
                action == ShuttleWeaponPartialReloadAction.StopSourceEmpty;
        }

        private static int GetTicksGame(ShuttleModuleRuntimeContext context)
        {
            if (context != null)
            {
                return context.TicksGame;
            }

            return Find.TickManager != null ? Find.TickManager.TicksGame : -1;
        }
    }
}

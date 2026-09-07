using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal delegate bool HabitatLaunchExportValidationDelegate(out string failureReason);

    internal delegate bool HabitatLaunchExportRollbackDelegate(
        ShuttleHolderLaunchManifest manifest,
        out string notice);

    internal sealed class HabitatLaunchExportAccess
    {
        private readonly ThingOwner<Thing> habitatHeldThings;
        private readonly List<ShuttleHabitatOccupantRecord> sleepingOccupants;
        private readonly List<ShuttleHabitatDiningOccupantRecord> diningOccupants;
        private readonly List<ShuttleHabitatJoyOccupantRecord> joyOccupants;
        private readonly ThingOwner destination;
        private readonly Action ensureInitialized;
        private readonly HabitatLaunchExportValidationDelegate validateLivingExport;
        private readonly HabitatLaunchExportValidationDelegate validateMixedExport;
        private readonly HabitatLaunchExportValidationDelegate validateJoyOnlyExport;
        private readonly HabitatLaunchExportRollbackDelegate rollbackLivingExport;
        private readonly HabitatLaunchExportRollbackDelegate rollbackJoyExport;
        private readonly HabitatLaunchExportRollbackDelegate rollbackMixedExport;

        internal HabitatLaunchExportAccess(
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatOccupantRecord> sleepingOccupants,
            List<ShuttleHabitatDiningOccupantRecord> diningOccupants,
            List<ShuttleHabitatJoyOccupantRecord> joyOccupants,
            ThingOwner destination,
            Action ensureInitialized,
            HabitatLaunchExportValidationDelegate validateLivingExport,
            HabitatLaunchExportValidationDelegate validateMixedExport,
            HabitatLaunchExportValidationDelegate validateJoyOnlyExport,
            HabitatLaunchExportRollbackDelegate rollbackLivingExport,
            HabitatLaunchExportRollbackDelegate rollbackJoyExport,
            HabitatLaunchExportRollbackDelegate rollbackMixedExport)
        {
            this.habitatHeldThings = habitatHeldThings;
            this.sleepingOccupants = sleepingOccupants;
            this.diningOccupants = diningOccupants;
            this.joyOccupants = joyOccupants;
            this.destination = destination;
            this.ensureInitialized = ensureInitialized;
            this.validateLivingExport = validateLivingExport;
            this.validateMixedExport = validateMixedExport;
            this.validateJoyOnlyExport = validateJoyOnlyExport;
            this.rollbackLivingExport = rollbackLivingExport;
            this.rollbackJoyExport = rollbackJoyExport;
            this.rollbackMixedExport = rollbackMixedExport;
        }

        internal bool HasDestination
        {
            get { return this.destination != null; }
        }

        internal int SleepingRecordCount
        {
            get { return this.sleepingOccupants != null ? this.sleepingOccupants.Count : 0; }
        }

        internal int DiningRecordCount
        {
            get { return this.diningOccupants != null ? this.diningOccupants.Count : 0; }
        }

        internal int JoyRecordCount
        {
            get { return this.joyOccupants != null ? this.joyOccupants.Count : 0; }
        }

        internal void EnsureInitialized()
        {
            if (this.ensureInitialized != null)
            {
                this.ensureInitialized();
            }
        }

        internal bool TryValidateLivingExportState(out string failureReason)
        {
            failureReason = null;
            return this.validateLivingExport != null && this.validateLivingExport(out failureReason);
        }

        internal bool TryValidateMixedExportState(out string failureReason)
        {
            failureReason = null;
            return this.validateMixedExport != null && this.validateMixedExport(out failureReason);
        }

        internal bool CanTransferJoyOnlyForLaunch(out string failureReason)
        {
            failureReason = null;
            return this.validateJoyOnlyExport != null && this.validateJoyOnlyExport(out failureReason);
        }

        internal ShuttleHabitatOccupantRecord GetSleepingRecord(int index)
        {
            return this.sleepingOccupants[index];
        }

        internal ShuttleHabitatDiningOccupantRecord GetDiningRecord(int index)
        {
            return this.diningOccupants[index];
        }

        internal ShuttleHabitatJoyOccupantRecord GetJoyRecord(int index)
        {
            return this.joyOccupants[index];
        }

        internal void RemoveSleepingRecordAt(int index)
        {
            this.sleepingOccupants.RemoveAt(index);
        }

        internal void RemoveDiningRecordAt(int index)
        {
            this.diningOccupants.RemoveAt(index);
        }

        internal void RemoveJoyRecordAt(int index)
        {
            this.joyOccupants.RemoveAt(index);
        }

        internal bool IsContainedJoyPawn(Pawn pawn)
        {
            return HabitatOccupancyQueryService.IsContainedJoyPawn(
                this.habitatHeldThings,
                this.joyOccupants,
                pawn);
        }

        internal bool TryAddOrTransferToDestination(Thing thing)
        {
            return this.destination != null && this.destination.TryAddOrTransfer(thing, false);
        }

        internal bool TryRollbackLivingExport(ShuttleHolderLaunchManifest manifest, out string notice)
        {
            notice = null;
            return this.rollbackLivingExport != null && this.rollbackLivingExport(manifest, out notice);
        }

        internal bool TryRollbackJoyExport(ShuttleHolderLaunchManifest manifest, out string notice)
        {
            notice = null;
            return this.rollbackJoyExport != null && this.rollbackJoyExport(manifest, out notice);
        }

        internal bool TryRollbackMixedExport(ShuttleHolderLaunchManifest manifest, out string notice)
        {
            notice = null;
            return this.rollbackMixedExport != null && this.rollbackMixedExport(manifest, out notice);
        }

    }
}

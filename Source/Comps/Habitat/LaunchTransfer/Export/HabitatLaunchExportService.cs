using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatLaunchExportService
    {
        internal static bool TryExportLivingForLaunchCore(
            HabitatLaunchExportAccess access,
            ShuttleHolderLaunchManifest manifest,
            string sleepDebugNote,
            string diningPawnDebugNote,
            string diningFoodDebugNote,
            string noEligibleReason,
            out string failureReason)
        {
            failureReason = null;
            access.EnsureInitialized();
            if (manifest == null)
            {
                failureReason = "Habitat living export manifest is missing.";
                return false;
            }

            manifest.EnsureInitialized();
            if (!access.HasDestination)
            {
                failureReason = "Habitat living export destination is missing.";
                return false;
            }

            if (!access.TryValidateLivingExportState(out failureReason))
            {
                return false;
            }

            if (access.SleepingRecordCount == 0 && access.DiningRecordCount == 0)
            {
                failureReason = noEligibleReason;
                return false;
            }

            int exportTick = ShuttleTickUtility.TicksGameOrMinusOne();
            HabitatLaunchTransferTransaction transaction =
                new HabitatLaunchTransferTransaction(access, manifest, "Living");
            try
            {
                for (int i = access.SleepingRecordCount - 1; i >= 0; i--)
                {
                    ShuttleHabitatOccupantRecord record = access.GetSleepingRecord(i);
                    Pawn pawn = record.Pawn;
                    ShuttleHolderLaunchManifestEntry entry = manifest.AddHabitatSleepEntry(
                        pawn.thingIDNumber,
                        HabitatLaunchTransferManifestHelper.GetThingDefName(pawn),
                        HabitatLaunchTransferManifestHelper.GetThingLabel(pawn),
                        HabitatLaunchTransferManifestHelper.BuildHabitatActivityID(
                            "Sleep",
                            pawn,
                            null,
                            exportTick,
                            i),
                        i,
                        record.RestStartTick,
                        record.RestedTicks,
                        record.IsSleeping,
                        exportTick,
                        ShuttleHolderLaunchManifestConstants.PhaseExported,
                        sleepDebugNote);

                    if (!transaction.TryMoveThingToDestination(
                        pawn,
                        entry,
                        "Failed to move Habitat sleep pawn to ActiveTransporterInfo.innerContainer. thingID=" + pawn.thingIDNumber,
                        out failureReason))
                    {
                        return false;
                    }

                    access.RemoveSleepingRecordAt(i);
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseInFlight;
                }

                for (int i = access.DiningRecordCount - 1; i >= 0; i--)
                {
                    ShuttleHabitatDiningOccupantRecord record = access.GetDiningRecord(i);
                    Pawn pawn = record.Pawn;
                    Thing food = record.Food;
                    string activityID = HabitatLaunchTransferManifestHelper.BuildHabitatActivityID(
                        "Dining",
                        pawn,
                        food,
                        exportTick,
                        i);
                    ShuttleHolderLaunchManifestEntry pawnEntry = manifest.AddHabitatDiningPawnEntry(
                        pawn.thingIDNumber,
                        HabitatLaunchTransferManifestHelper.GetThingDefName(pawn),
                        HabitatLaunchTransferManifestHelper.GetThingLabel(pawn),
                        activityID,
                        i,
                        food.thingIDNumber,
                        HabitatLaunchTransferManifestHelper.GetThingDefName(food),
                        HabitatLaunchTransferManifestHelper.GetThingLabel(food),
                        food.stackCount,
                        record.DiningStartTick,
                        record.ChewTicksLeft,
                        record.ChewTicksTotal,
                        record.IsDining,
                        record.IsFinalized,
                        exportTick,
                        ShuttleHolderLaunchManifestConstants.PhaseExported,
                        diningPawnDebugNote);
                    ShuttleHolderLaunchManifestEntry foodEntry = manifest.AddHabitatDiningFoodEntry(
                        food.thingIDNumber,
                        HabitatLaunchTransferManifestHelper.GetThingDefName(food),
                        HabitatLaunchTransferManifestHelper.GetThingLabel(food),
                        food.stackCount,
                        activityID,
                        i,
                        record.DiningStartTick,
                        record.ChewTicksLeft,
                        record.ChewTicksTotal,
                        record.IsDining,
                        record.IsFinalized,
                        exportTick,
                        ShuttleHolderLaunchManifestConstants.PhaseExported,
                        diningFoodDebugNote);

                    if (!transaction.TryMoveDiningPairToDestination(
                        pawn,
                        food,
                        pawnEntry,
                        foodEntry,
                        "Failed to move Habitat dining pawn to ActiveTransporterInfo.innerContainer. activityID=" + activityID,
                        "Failed to move Habitat dining food to ActiveTransporterInfo.innerContainer. activityID=" + activityID,
                        out failureReason))
                    {
                        return false;
                    }

                    access.RemoveDiningRecordAt(i);
                    pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseInFlight;
                    foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseInFlight;
                }

                transaction.MarkCompleted();
                failureReason = null;
                return true;
            }
            finally
            {
                transaction.RollbackLivingIfIncomplete(ref failureReason);
            }
        }

        internal static bool TryExportJoyForLaunchCore(
            HabitatLaunchExportAccess access,
            ShuttleHolderLaunchManifest manifest,
            string debugNote,
            string destinationLabel,
            string noEligibleReason,
            out string failureReason)
        {
            failureReason = null;
            access.EnsureInitialized();
            if (manifest == null)
            {
                failureReason = "Habitat joy export manifest is missing.";
                return false;
            }

            manifest.EnsureInitialized();
            if (!access.HasDestination)
            {
                failureReason = "Habitat joy export destination is missing.";
                return false;
            }

            if (!access.CanTransferJoyOnlyForLaunch(out failureReason))
            {
                return false;
            }

            if (access.JoyRecordCount == 0)
            {
                failureReason = noEligibleReason;
                return false;
            }

            int exportTick = ShuttleTickUtility.TicksGameOrMinusOne();
            HabitatLaunchTransferTransaction transaction =
                new HabitatLaunchTransferTransaction(access, manifest, "Joy");
            try
            {
                for (int i = access.JoyRecordCount - 1; i >= 0; i--)
                {
                    ShuttleHabitatJoyOccupantRecord record = access.GetJoyRecord(i);
                    Pawn pawn = record != null ? record.Pawn : null;
                    JoyKindDef joyKind = record != null ? record.JoyKind : null;
                    if (record == null ||
                        !record.IsJoying ||
                        !access.IsContainedJoyPawn(pawn) ||
                        joyKind == null)
                    {
                        failureReason = "Habitat joy record is invalid or unsupported for export. index=" + i;
                        return false;
                    }

                    ShuttleHolderLaunchManifestEntry entry = manifest.AddHabitatJoyEntry(
                        pawn.thingIDNumber,
                        HabitatLaunchTransferManifestHelper.GetThingDefName(pawn),
                        HabitatLaunchTransferManifestHelper.GetThingLabel(pawn),
                        HabitatLaunchTransferManifestHelper.BuildHabitatActivityID(
                            "Joy",
                            pawn,
                            null,
                            exportTick,
                            i),
                        i,
                        joyKind.defName,
                        joyKind.label,
                        record.JoyStartTick,
                        record.JoyTicks,
                        record.JoyGainRate,
                        record.MaxJoyTicks,
                        record.IsJoying,
                        exportTick,
                        ShuttleHolderLaunchManifestConstants.PhaseExported,
                        debugNote);

                    if (!transaction.TryMoveThingToDestination(
                        pawn,
                        entry,
                        "Failed to move Habitat joy pawn to " + destinationLabel + ". thingID=" + pawn.thingIDNumber,
                        out failureReason))
                    {
                        return false;
                    }

                    access.RemoveJoyRecordAt(i);
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseInFlight;
                }

                transaction.MarkCompleted();
                failureReason = null;
                return true;
            }
            finally
            {
                transaction.RollbackJoyIfIncomplete(ref failureReason);
            }
        }

        // Shared mixed export transaction. Live records are removed only after all
        // associated Things have reached the destination owner.
        internal static bool TryExportMixedForLaunchCore(
            HabitatLaunchExportAccess access,
            ShuttleHolderLaunchManifest manifest,
            string sleepDebugNote,
            string diningPawnDebugNote,
            string diningFoodDebugNote,
            string joyDebugNote,
            string destinationLabel,
            out string failureReason)
        {
            failureReason = null;
            access.EnsureInitialized();
            if (manifest == null)
            {
                failureReason = "Habitat mixed export manifest is missing.";
                return false;
            }

            manifest.EnsureInitialized();
            if (!access.HasDestination)
            {
                failureReason = "Habitat mixed export destination is missing.";
                return false;
            }

            if (!access.TryValidateMixedExportState(out failureReason))
            {
                return false;
            }

            int exportTick = ShuttleTickUtility.TicksGameOrMinusOne();
            HabitatLaunchTransferTransaction transaction =
                new HabitatLaunchTransferTransaction(access, manifest, "Mixed");
            try
            {
                for (int i = access.SleepingRecordCount - 1; i >= 0; i--)
                {
                    ShuttleHabitatOccupantRecord record = access.GetSleepingRecord(i);
                    Pawn pawn = record.Pawn;
                    ShuttleHolderLaunchManifestEntry entry = manifest.AddHabitatSleepEntry(
                        pawn.thingIDNumber,
                        HabitatLaunchTransferManifestHelper.GetThingDefName(pawn),
                        HabitatLaunchTransferManifestHelper.GetThingLabel(pawn),
                        HabitatLaunchTransferManifestHelper.BuildHabitatActivityID(
                            "MixedSleep",
                            pawn,
                            null,
                            exportTick,
                            i),
                        i,
                        record.RestStartTick,
                        record.RestedTicks,
                        record.IsSleeping,
                        exportTick,
                        ShuttleHolderLaunchManifestConstants.PhaseExported,
                        sleepDebugNote);

                    if (!transaction.TryMoveThingToDestination(
                        pawn,
                        entry,
                        "Failed to move Habitat mixed sleep pawn to " +
                            destinationLabel +
                            ". thingID=" +
                            pawn.thingIDNumber,
                        out failureReason))
                    {
                        return false;
                    }

                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseInFlight;
                }

                for (int i = access.DiningRecordCount - 1; i >= 0; i--)
                {
                    ShuttleHabitatDiningOccupantRecord record = access.GetDiningRecord(i);
                    Pawn pawn = record.Pawn;
                    Thing food = record.Food;
                    string activityID = HabitatLaunchTransferManifestHelper.BuildHabitatActivityID(
                        "MixedDining",
                        pawn,
                        food,
                        exportTick,
                        i);
                    ShuttleHolderLaunchManifestEntry pawnEntry = manifest.AddHabitatDiningPawnEntry(
                        pawn.thingIDNumber,
                        HabitatLaunchTransferManifestHelper.GetThingDefName(pawn),
                        HabitatLaunchTransferManifestHelper.GetThingLabel(pawn),
                        activityID,
                        i,
                        food.thingIDNumber,
                        HabitatLaunchTransferManifestHelper.GetThingDefName(food),
                        HabitatLaunchTransferManifestHelper.GetThingLabel(food),
                        food.stackCount,
                        record.DiningStartTick,
                        record.ChewTicksLeft,
                        record.ChewTicksTotal,
                        record.IsDining,
                        record.IsFinalized,
                        exportTick,
                        ShuttleHolderLaunchManifestConstants.PhaseExported,
                        diningPawnDebugNote);
                    ShuttleHolderLaunchManifestEntry foodEntry = manifest.AddHabitatDiningFoodEntry(
                        food.thingIDNumber,
                        HabitatLaunchTransferManifestHelper.GetThingDefName(food),
                        HabitatLaunchTransferManifestHelper.GetThingLabel(food),
                        food.stackCount,
                        activityID,
                        i,
                        record.DiningStartTick,
                        record.ChewTicksLeft,
                        record.ChewTicksTotal,
                        record.IsDining,
                        record.IsFinalized,
                        exportTick,
                        ShuttleHolderLaunchManifestConstants.PhaseExported,
                        diningFoodDebugNote);

                    if (!transaction.TryMoveDiningPairToDestination(
                        pawn,
                        food,
                        pawnEntry,
                        foodEntry,
                        "Failed to move Habitat mixed dining pawn to " +
                            destinationLabel +
                            ". activityID=" +
                            activityID,
                        "Failed to move Habitat mixed dining food to " +
                            destinationLabel +
                            ". activityID=" +
                            activityID,
                        out failureReason))
                    {
                        return false;
                    }

                    pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseInFlight;
                    foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseInFlight;
                }

                for (int i = access.JoyRecordCount - 1; i >= 0; i--)
                {
                    ShuttleHabitatJoyOccupantRecord record = access.GetJoyRecord(i);
                    Pawn pawn = record.Pawn;
                    JoyKindDef joyKind = record.JoyKind;
                    ShuttleHolderLaunchManifestEntry entry = manifest.AddHabitatJoyEntry(
                        pawn.thingIDNumber,
                        HabitatLaunchTransferManifestHelper.GetThingDefName(pawn),
                        HabitatLaunchTransferManifestHelper.GetThingLabel(pawn),
                        HabitatLaunchTransferManifestHelper.BuildHabitatActivityID(
                            "MixedJoy",
                            pawn,
                            null,
                            exportTick,
                            i),
                        i,
                        joyKind.defName,
                        joyKind.label,
                        record.JoyStartTick,
                        record.JoyTicks,
                        record.JoyGainRate,
                        record.MaxJoyTicks,
                        record.IsJoying,
                        exportTick,
                        ShuttleHolderLaunchManifestConstants.PhaseExported,
                        joyDebugNote);

                    if (!transaction.TryMoveThingToDestination(
                        pawn,
                        entry,
                        "Failed to move Habitat mixed joy pawn to " +
                            destinationLabel +
                            ". thingID=" +
                            pawn.thingIDNumber,
                        out failureReason))
                    {
                        return false;
                    }

                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseInFlight;
                }

                for (int i = access.SleepingRecordCount - 1; i >= 0; i--)
                {
                    access.RemoveSleepingRecordAt(i);
                }

                for (int i = access.DiningRecordCount - 1; i >= 0; i--)
                {
                    access.RemoveDiningRecordAt(i);
                }

                for (int i = access.JoyRecordCount - 1; i >= 0; i--)
                {
                    access.RemoveJoyRecordAt(i);
                }

                transaction.MarkCompleted();
                failureReason = null;
                return true;
            }
            finally
            {
                transaction.RollbackMixedIfIncomplete(ref failureReason);
            }
        }
    }
}

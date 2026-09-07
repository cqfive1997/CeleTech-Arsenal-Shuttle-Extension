using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatRestoreCommitter
    {
            internal static void CommitHabitatLivingRestorePlan(
                HabitatRestoreAccess access,
                HabitatLivingRestorePlan plan)
            {
                if (plan == null)
                {
                    return;
                }

                for (int i = 0; i < plan.SleepEntries.Count; i++)
                {
                    HabitatSleepRestorePlanEntry sleepEntry = plan.SleepEntries[i];
                    if (!access.HasRecordFor(sleepEntry.Pawn))
                    {
                        access.AddSleepingRecord(
                            sleepEntry.Pawn,
                            sleepEntry.Entry.RestStartTick,
                            sleepEntry.Entry.RestedTicks,
                            sleepEntry.Entry.IsSleeping);
                    }

                    sleepEntry.Entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRestored;
                    sleepEntry.Entry.DebugNotes = "Restored by formal Living Habitat restore transaction before incoming base.Impact.";
                }

                for (int i = 0; i < plan.DiningEntries.Count; i++)
                {
                    HabitatDiningRestorePlanEntry diningEntry = plan.DiningEntries[i];
                    if (!access.HasDiningRecordFor(diningEntry.Pawn))
                    {
                        access.AddDiningRecord(
                            diningEntry.Pawn,
                            diningEntry.FoodForRestore,
                            diningEntry.PawnEntry.DiningStartTick,
                            diningEntry.PawnEntry.ChewTicksLeft,
                            diningEntry.PawnEntry.ChewTicksTotal,
                            diningEntry.PawnEntry.IsDining,
                            diningEntry.PawnEntry.Finalized);
                    }

                    diningEntry.PawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRestored;
                    diningEntry.PawnEntry.DebugNotes = "Restored by formal Living Habitat restore transaction before incoming base.Impact.";
                    diningEntry.FoodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRestored;
                    diningEntry.FoodEntry.DebugNotes = diningEntry.FoodWasSplit
                        ? "Restored by formal Living Habitat restore transaction after splitting planned food stack."
                        : "Restored by formal Living Habitat restore transaction before incoming base.Impact.";
                }
            }

            internal static void CommitHabitatMixedRestorePlan(
                HabitatRestoreAccess access,
                HabitatMixedRestorePlan plan)
            {
                if (plan == null)
                {
                    return;
                }

                for (int i = 0; i < plan.SleepEntries.Count; i++)
                {
                    HabitatMixedSleepRestorePlanEntry sleepEntry = plan.SleepEntries[i];
                    if (!access.HasRecordFor(sleepEntry.Pawn))
                    {
                        access.AddSleepingRecord(
                            sleepEntry.Pawn,
                            sleepEntry.RestStartTick,
                            sleepEntry.RestedTicks,
                            sleepEntry.IsSleeping);
                    }

                    sleepEntry.Entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRestored;
                    sleepEntry.Entry.DebugNotes = "Restored by unified Habitat mixed restore transaction.";
                }

                for (int i = 0; i < plan.DiningEntries.Count; i++)
                {
                    HabitatMixedDiningRestorePlanEntry diningEntry = plan.DiningEntries[i];
                    Thing food = diningEntry.FoodForRestore != null
                        ? diningEntry.FoodForRestore
                        : diningEntry.FoodAllocation != null
                            ? diningEntry.FoodAllocation.SourceThing
                            : null;
                    if (!access.HasDiningRecordFor(diningEntry.Pawn))
                    {
                        access.AddDiningRecord(
                            diningEntry.Pawn,
                            food,
                            diningEntry.DiningStartTick,
                            diningEntry.ChewTicksLeft,
                            diningEntry.ChewTicksTotal,
                            diningEntry.IsDining,
                            diningEntry.Finalized);
                    }

                    diningEntry.PawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRestored;
                    diningEntry.PawnEntry.DebugNotes = "Restored by unified Habitat mixed restore transaction.";
                    diningEntry.FoodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRestored;
                    diningEntry.FoodEntry.DebugNotes = "Restored by unified Habitat mixed restore transaction.";
                }

                for (int i = 0; i < plan.JoyEntries.Count; i++)
                {
                    HabitatMixedJoyRestorePlanEntry joyEntry = plan.JoyEntries[i];
                    if (!access.HasJoyRecordFor(joyEntry.Pawn))
                    {
                        access.AddJoyRecord(
                            joyEntry.Pawn,
                            joyEntry.JoyKind,
                            joyEntry.JoyStartTick,
                            joyEntry.JoyTicks,
                            joyEntry.JoyGainRate,
                            joyEntry.MaxJoyTicks,
                            joyEntry.IsJoying);
                    }

                    joyEntry.Entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRestored;
                    joyEntry.Entry.DebugNotes = "Restored by unified Habitat mixed restore transaction.";
                }
            }
    }
}

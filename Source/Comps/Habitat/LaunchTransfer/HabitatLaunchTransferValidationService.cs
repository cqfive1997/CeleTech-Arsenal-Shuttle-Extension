using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatLaunchTransferValidationService
    {
        internal static bool CanTransferLivingForLaunch(
            HabitatLaunchTransferValidationAccess access,
            out string failureReason)
        {
            if (!TryValidateLivingExportState(access, out failureReason))
            {
                return false;
            }

            if (access.SleepingRecordCount == 0 && access.DiningRecordCount == 0)
            {
                failureReason = "No Habitat sleep or dining occupants are eligible for Living Habitat launch transfer.";
                return false;
            }

            return true;
        }

        internal static bool CanTransferJoyOnlyForLaunch(
            HabitatLaunchTransferValidationAccess access,
            out string failureReason)
        {
            if (access.SleepingRecordCount > 0 || access.DiningRecordCount > 0)
            {
                failureReason = "Habitat joy launch transfer is joy-only; mixed sleep/dining + joy occupants must use the mixed Habitat launch transfer.";
                return false;
            }

            if (access.JoyRecordCount == 0)
            {
                failureReason = "No Habitat joy occupants are eligible for Habitat joy launch transfer.";
                return false;
            }

            for (int i = 0; i < access.JoyRecordCount; i++)
            {
                ShuttleHabitatJoyOccupantRecord record = access.GetJoyRecord(i);
                Pawn pawn = record != null ? record.Pawn : null;
                JoyKindDef joyKind = record != null ? record.JoyKind : null;
                if (record == null ||
                    !record.IsJoying ||
                    !access.IsContainedJoyPawn(pawn) ||
                    joyKind == null)
                {
                    failureReason = "Habitat joy record is invalid or unsupported for Habitat joy launch transfer. index=" + i;
                    return false;
                }
            }

            if (access.CountHeldPawns() != access.JoyRecordCount)
            {
                failureReason = "Habitat holder contains unsupported or unknown pawns; joy launch transfer fails closed.";
                return false;
            }

            Thing orphanThing = access.FindUnassignedDiningFood();
            if (orphanThing != null)
            {
                failureReason = "Habitat holder contains unsupported non-pawn content; joy launch transfer fails closed. thingID=" +
                    orphanThing.thingIDNumber;
                return false;
            }

            failureReason = null;
            return true;
        }

        internal static bool CanTransferMixedForLaunch(
            HabitatLaunchTransferValidationAccess access,
            out string failureReason)
        {
            return TryValidateMixedExportState(access, out failureReason);
        }

        internal static bool TryValidateMixedExportState(
            HabitatLaunchTransferValidationAccess access,
            out string failureReason)
        {
            failureReason = null;
            bool hasLiving = access.SleepingRecordCount > 0 || access.DiningRecordCount > 0;
            bool hasJoy = access.JoyRecordCount > 0;
            if (!hasLiving || !hasJoy)
            {
                failureReason = "Habitat mixed launch transfer requires at least one sleep/dining occupant and at least one joy occupant.";
                return false;
            }

            for (int i = 0; i < access.SleepingRecordCount; i++)
            {
                ShuttleHabitatOccupantRecord record = access.GetSleepingRecord(i);
                if (record == null || !record.IsSleeping || !access.IsContainedSleepingPawn(record.Pawn))
                {
                    failureReason = "Habitat mixed export found invalid sleep record. index=" + i;
                    return false;
                }
            }

            for (int i = 0; i < access.DiningRecordCount; i++)
            {
                ShuttleHabitatDiningOccupantRecord record = access.GetDiningRecord(i);
                if (record == null ||
                    !record.IsDining ||
                    !access.IsContainedDiningPawn(record.Pawn) ||
                    !access.IsContainedDiningFood(record.Food))
                {
                    failureReason = "Habitat mixed export found invalid dining record. index=" + i;
                    return false;
                }
            }

            for (int i = 0; i < access.JoyRecordCount; i++)
            {
                ShuttleHabitatJoyOccupantRecord record = access.GetJoyRecord(i);
                Pawn pawn = record != null ? record.Pawn : null;
                JoyKindDef joyKind = record != null ? record.JoyKind : null;
                if (record == null ||
                    !record.IsJoying ||
                    !access.IsContainedJoyPawn(pawn) ||
                    joyKind == null)
                {
                    failureReason = "Habitat mixed export found invalid joy record. index=" + i;
                    return false;
                }
            }

            int supportedPawns = access.SleepingRecordCount + access.DiningRecordCount + access.JoyRecordCount;
            if (access.CountHeldPawns() != supportedPawns)
            {
                failureReason = "Habitat holder contains unsupported or duplicate pawns; mixed export fails closed.";
                return false;
            }

            Thing orphanThing = access.FindUnassignedDiningFood();
            if (orphanThing != null)
            {
                failureReason = "Habitat holder contains unsupported non-pawn content; mixed export fails closed. thingID=" +
                    orphanThing.thingIDNumber;
                return false;
            }

            return true;
        }

        internal static bool TryValidateLivingExportState(
            HabitatLaunchTransferValidationAccess access,
            out string failureReason)
        {
            failureReason = null;
            if (access.JoyRecordCount > 0)
            {
                failureReason = "Habitat joy occupants are not supported by Living Habitat launch transfer.";
                return false;
            }

            for (int i = 0; i < access.SleepingRecordCount; i++)
            {
                ShuttleHabitatOccupantRecord record = access.GetSleepingRecord(i);
                if (record == null || !record.IsSleeping || !access.IsContainedSleepingPawn(record.Pawn))
                {
                    failureReason = "Habitat sleep record is invalid or unsupported for Living Habitat launch transfer. index=" + i;
                    return false;
                }
            }

            for (int i = 0; i < access.DiningRecordCount; i++)
            {
                ShuttleHabitatDiningOccupantRecord record = access.GetDiningRecord(i);
                if (record == null ||
                    !record.IsDining ||
                    !access.IsContainedDiningPawn(record.Pawn) ||
                    !access.IsContainedDiningFood(record.Food))
                {
                    failureReason = "Habitat dining record is invalid or unsupported for Living Habitat launch transfer. index=" + i;
                    return false;
                }
            }

            int supportedPawns = access.SleepingRecordCount + access.DiningRecordCount;
            if (access.CountHeldPawns() != supportedPawns)
            {
                failureReason = "Habitat holder contains unsupported or unknown pawns; Living Habitat launch transfer fails closed.";
                return false;
            }

            Thing orphanDiningFood = access.FindUnassignedDiningFood();
            if (orphanDiningFood != null)
            {
                failureReason = "Habitat holder contains unassigned dining food; Living Habitat launch transfer fails closed. thingID=" + orphanDiningFood.thingIDNumber;
                return false;
            }

            return true;
        }

        internal static bool TryValidateHabitatMixedRestoreHolderConflicts(
            HabitatLaunchTransferValidationAccess access,
            HashSet<int> plannedPawnIDs,
            HashSet<int> plannedFoodSourceThingIDs,
            out string failureReason)
        {
            failureReason = null;
            if (access.HeldThingCount == 0)
            {
                return true;
            }

            for (int i = 0; i < access.HeldThingCount; i++)
            {
                Thing thing = access.GetHeldThing(i);
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                Pawn pawn = thing as Pawn;
                if (pawn != null)
                {
                    if (plannedPawnIDs == null || !plannedPawnIDs.Contains(pawn.thingIDNumber))
                    {
                        failureReason = "Habitat mixed restore plan found unsupported held pawn before restore. thingID=" +
                            pawn.thingIDNumber +
                            " label=" +
                            pawn.LabelShortCap;
                        return false;
                    }

                    continue;
                }

                if (plannedFoodSourceThingIDs == null ||
                    !plannedFoodSourceThingIDs.Contains(thing.thingIDNumber))
                {
                    failureReason = "Habitat mixed restore plan found unsupported held non-pawn thing before restore. thingID=" +
                        thing.thingIDNumber +
                        " defName=" +
                        (thing.def != null ? thing.def.defName : "null") +
                        " label=" +
                        thing.LabelShortCap;
                    return false;
                }
            }

            return true;
        }

        internal static bool TryValidateHabitatMixedLocalStagingRestorePlan(
            HabitatMixedRestorePlan plan,
            out string failureReason)
        {
            failureReason = null;
            if (plan == null)
            {
                failureReason = "Cannot validate a null Habitat mixed restore plan.";
                return false;
            }

            bool hasLiving = plan.SleepEntries.Count > 0 || plan.DiningEntries.Count > 0;
            bool hasJoy = plan.JoyEntries.Count > 0;
            if (!hasLiving || !hasJoy)
            {
                failureReason = "Habitat mixed local staging restore requires at least one living entry and one joy entry.";
                return false;
            }

            for (int i = 0; i < plan.FoodAllocations.Count; i++)
            {
                HabitatMixedFoodAllocation allocation = plan.FoodAllocations[i];
                if (allocation == null || allocation.SourceThing == null || allocation.SourceThing.Destroyed)
                {
                    failureReason = "Habitat mixed local staging restore has an invalid food allocation.";
                    return false;
                }

                if (allocation.WouldRequireSplit)
                {
                    failureReason = "Habitat mixed local staging restore refuses split food allocation; local staging should preserve exact dining food Things. activityID=" +
                        (allocation.Entry != null ? allocation.Entry.ActivityID ?? "null" : "null") +
                        " sourceThingID=" +
                        allocation.SourceThing.thingIDNumber +
                        " sourceStackCount=" +
                        allocation.SourceThing.stackCount +
                        " requiredStackCount=" +
                        allocation.RequiredStackCount;
                    return false;
                }
            }

            return true;
        }
    }
}

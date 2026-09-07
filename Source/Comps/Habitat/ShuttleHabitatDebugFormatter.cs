using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class ShuttleHabitatDebugFormatter
    {
        internal static string DescribeDiningFoodRollbackState(Thing food, bool contained)
        {
            if (food == null)
            {
                return "food=null contained=false safeOwner=true";
            }

            return "foodThingID=" +
                food.thingIDNumber +
                " foodDefName=" +
                (food.def != null ? food.def.defName : "null") +
                " destroyed=" +
                food.Destroyed +
                " spawned=" +
                food.Spawned +
                " holdingOwner=" +
                (food.holdingOwner != null ? food.holdingOwner.GetType().Name : "null") +
                " contained=" +
                contained +
                " stackCount=" +
                food.stackCount;
        }

        internal static string DescribeDiningStopState(
            Pawn pawn,
            bool pawnContained,
            string foodRollbackState)
        {
            return "pawn=" +
                (pawn != null ? pawn.ToString() : "null") +
                " pawnDestroyed=" +
                (pawn != null && pawn.Destroyed) +
                " pawnSpawned=" +
                (pawn != null && pawn.Spawned) +
                " pawnHoldingOwner=" +
                (pawn != null && pawn.holdingOwner != null ? pawn.holdingOwner.GetType().Name : "null") +
                " pawnContained=" +
                pawnContained +
                " " +
                foodRollbackState;
        }

        internal static string DumpMixedRestorePlan(
            HabitatMixedRestorePlan plan)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("mixedRestorePlan sleep=");
            builder.Append(plan.SleepEntries.Count);
            builder.Append(" dining=");
            builder.Append(plan.DiningEntries.Count);
            builder.Append(" joy=");
            builder.Append(plan.JoyEntries.Count);
            builder.Append(" foodAllocations=");
            builder.Append(plan.FoodAllocations.Count);

            for (int i = 0; i < plan.SleepEntries.Count; i++)
            {
                builder.AppendLine();
                builder.Append("  sleep[");
                builder.Append(i);
                builder.Append("] ");
                builder.Append(plan.SleepEntries[i].DumpForDebug());
            }

            for (int i = 0; i < plan.DiningEntries.Count; i++)
            {
                builder.AppendLine();
                builder.Append("  dining[");
                builder.Append(i);
                builder.Append("] ");
                builder.Append(plan.DiningEntries[i].DumpForDebug());
            }

            for (int i = 0; i < plan.JoyEntries.Count; i++)
            {
                builder.AppendLine();
                builder.Append("  joy[");
                builder.Append(i);
                builder.Append("] ");
                builder.Append(plan.JoyEntries[i].DumpForDebug());
            }

            return builder.ToString();
        }

        internal static string DumpMixedSleepRestorePlanEntry(
            HabitatMixedSleepRestorePlanEntry entry)
        {
            return "thingID=" +
                (entry.Pawn != null ? entry.Pawn.thingIDNumber : -1) +
                " label=" +
                (entry.Entry != null ? entry.Entry.Label : "null") +
                " activityID=" +
                (entry.Entry != null ? entry.Entry.ActivityID : "null") +
                " source=" +
                (entry.SourceOwner != null ? entry.SourceOwner.Label : "null") +
                " restStartTick=" +
                entry.RestStartTick +
                " restedTicks=" +
                entry.RestedTicks +
                " isSleeping=" +
                entry.IsSleeping;
        }

        internal static string DumpMixedDiningRestorePlanEntry(
            HabitatMixedDiningRestorePlanEntry entry)
        {
            return "activityID=" +
                (entry.ActivityID ?? "null") +
                " pawnThingID=" +
                (entry.Pawn != null ? entry.Pawn.thingIDNumber : -1) +
                " pawnLabel=" +
                (entry.PawnEntry != null ? entry.PawnEntry.Label : "null") +
                " pawnSource=" +
                (entry.PawnSourceOwner != null ? entry.PawnSourceOwner.Label : "null") +
                " food=" +
                (entry.FoodAllocation != null ? entry.FoodAllocation.DumpForDebug() : "null") +
                " diningStartTick=" +
                entry.DiningStartTick +
                " chewTicksLeft=" +
                entry.ChewTicksLeft +
                " chewTicksTotal=" +
                entry.ChewTicksTotal +
                " isDining=" +
                entry.IsDining +
                " finalized=" +
                entry.Finalized;
        }

        internal static string DumpMixedJoyRestorePlanEntry(
            HabitatMixedJoyRestorePlanEntry entry)
        {
            return "thingID=" +
                (entry.Pawn != null ? entry.Pawn.thingIDNumber : -1) +
                " label=" +
                (entry.Entry != null ? entry.Entry.Label : "null") +
                " activityID=" +
                (entry.ActivityID ?? "null") +
                " joyKind=" +
                (entry.JoyKind != null ? entry.JoyKind.defName : "null") +
                " source=" +
                (entry.SourceOwner != null ? entry.SourceOwner.Label : "null") +
                " joyStartTick=" +
                entry.JoyStartTick +
                " joyTicks=" +
                entry.JoyTicks +
                " joyGainRate=" +
                entry.JoyGainRate +
                " maxJoyTicks=" +
                entry.MaxJoyTicks +
                " isJoying=" +
                entry.IsJoying;
        }

        internal static string DumpMixedFoodAllocation(
            HabitatMixedFoodAllocation allocation)
        {
            return "foodThingID=" +
                (allocation.SourceThing != null ? allocation.SourceThing.thingIDNumber : -1) +
                " manifestThingID=" +
                (allocation.Entry != null ? allocation.Entry.ThingID : -1) +
                " defName=" +
                (allocation.SourceThing != null && allocation.SourceThing.def != null ? allocation.SourceThing.def.defName : "null") +
                " label=" +
                (allocation.Entry != null ? allocation.Entry.Label : "null") +
                " activityID=" +
                (allocation.Entry != null ? allocation.Entry.ActivityID : "null") +
                " source=" +
                (allocation.SourceOwner != null ? allocation.SourceOwner.Label : "null") +
                " requiredStackCount=" +
                allocation.RequiredStackCount +
                " sourceStackCountBefore=" +
                allocation.SourceStackCountBefore +
                " reservedStackCountBefore=" +
                allocation.ReservedStackCountBefore +
                " wouldRequireSplit=" +
                allocation.WouldRequireSplit;
        }
    }
}

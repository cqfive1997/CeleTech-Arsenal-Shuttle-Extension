using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat
{
    internal static class HabitatRestThoughtUtility
    {
        internal const int MinTicksForHabitatSleepThought = 2500;

        private const string SleptInBedroomDefName = "SleptInBedroom";
        private const string SleepDisturbedDefName = "SleepDisturbed";
        private const string SleptInBarracksDefName = "SleptInBarracks";

        internal static void TryApplyHabitatSleepThought(
            Pawn pawn,
            HabitatProfile habitat,
            int restedTicks)
        {
            if (restedTicks < MinTicksForHabitatSleepThought)
            {
                return;
            }

            MemoryThoughtHandler memories = GetMemoryThoughts(pawn);
            if (memories == null || habitat == null)
            {
                return;
            }

            ThoughtDef sleptInBedroom = DefDatabase<ThoughtDef>.GetNamedSilentFail(SleptInBedroomDefName);
            if (sleptInBedroom == null ||
                !sleptInBedroom.IsMemory ||
                sleptInBedroom.stages == null ||
                sleptInBedroom.stages.Count == 0)
            {
                return;
            }

            int stageIndex = Clamp(
                habitat.SleepThoughtStageIndex,
                0,
                sleptInBedroom.stages.Count - 1);

            memories.RemoveMemoriesOfDef(sleptInBedroom);
            memories.TryGainMemory(ThoughtMaker.MakeThought(sleptInBedroom, stageIndex), null);
        }

        internal static void RemoveSuppressedNegativeSleepThoughts(
            Pawn pawn,
            HabitatProfile habitat)
        {
            MemoryThoughtHandler memories = GetMemoryThoughts(pawn);
            if (memories == null || habitat == null)
            {
                return;
            }

            if (habitat.SuppressSleepDisturbedThoughts)
            {
                RemoveMemoryIfAvailable(memories, SleepDisturbedDefName);
            }

            if (habitat.SuppressBarracksThoughts)
            {
                RemoveMemoryIfAvailable(memories, SleptInBarracksDefName);
            }
        }

        private static MemoryThoughtHandler GetMemoryThoughts(Pawn pawn)
        {
            if (pawn == null ||
                pawn.needs == null ||
                pawn.needs.mood == null ||
                pawn.needs.mood.thoughts == null)
            {
                return null;
            }

            return pawn.needs.mood.thoughts.memories;
        }

        private static void RemoveMemoryIfAvailable(
            MemoryThoughtHandler memories,
            string defName)
        {
            ThoughtDef thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(defName);
            if (thoughtDef == null || !thoughtDef.IsMemory)
            {
                return;
            }

            memories.RemoveMemoriesOfDef(thoughtDef);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat
{
    internal static class HabitatJoyThoughtUtility
    {
        internal const int MinimumThoughtJoyTicks = 600;
        private const string HabitatJoyThoughtDefName = "CT_Shuttle_RecreationRoomEnjoyed";

        internal static void TryApplyHabitatJoyThought(
            Pawn pawn,
            HabitatProfile habitat,
            int joyTicks)
        {
            if (pawn == null ||
                habitat == null ||
                joyTicks < MinimumThoughtJoyTicks ||
                pawn.needs == null ||
                pawn.needs.mood == null ||
                pawn.needs.mood.thoughts == null ||
                pawn.needs.mood.thoughts.memories == null)
            {
                return;
            }

            ThoughtDef thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(HabitatJoyThoughtDefName);
            if (thoughtDef == null || thoughtDef.stages == null || thoughtDef.stages.Count == 0)
            {
                return;
            }

            int stageIndex = habitat.JoyThoughtStageIndex;
            if (stageIndex < 0)
            {
                stageIndex = 0;
            }
            else if (stageIndex >= thoughtDef.stages.Count)
            {
                stageIndex = thoughtDef.stages.Count - 1;
            }

            pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(thoughtDef);
            pawn.needs.mood.thoughts.memories.TryGainMemory(
                ThoughtMaker.MakeThought(thoughtDef, stageIndex),
                null);
        }
    }
}

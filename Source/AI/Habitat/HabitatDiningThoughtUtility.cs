using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat
{
    internal static class HabitatDiningThoughtUtility
    {
        private const string AteInImpressiveDiningRoomDefName = "AteInImpressiveDiningRoom";

        internal static void TryApplyHabitatDiningThought(Pawn pawn, HabitatProfile habitat)
        {
            if (pawn == null ||
                pawn.needs == null ||
                pawn.needs.mood == null ||
                pawn.needs.mood.thoughts == null ||
                habitat == null)
            {
                return;
            }

            ThoughtDef thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(AteInImpressiveDiningRoomDefName);
            if (thoughtDef == null || thoughtDef.stages == null || thoughtDef.stages.Count == 0)
            {
                return;
            }

            int stageIndex = ClampStageIndex(habitat.DiningThoughtStageIndex, thoughtDef);
            pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(thoughtDef);
            pawn.needs.mood.thoughts.memories.TryGainMemory(
                ThoughtMaker.MakeThought(thoughtDef, stageIndex));
        }

        private static int ClampStageIndex(int stageIndex, ThoughtDef thoughtDef)
        {
            if (stageIndex < 0)
            {
                return 0;
            }

            int maxStageIndex = thoughtDef.stages.Count - 1;
            return stageIndex > maxStageIndex ? maxStageIndex : stageIndex;
        }
    }
}

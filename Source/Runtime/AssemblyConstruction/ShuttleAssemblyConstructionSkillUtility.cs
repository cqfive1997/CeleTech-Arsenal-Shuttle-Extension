using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    internal static class ShuttleAssemblyConstructionSkillUtility
    {
        private const float ConstructionSpeedGlobalFactor = 1.7f;
        private const float XpPerTick = 0.25f;

        internal static float GetConstructionWorkPerTick(
            Pawn pawn,
            ShuttleAssemblyConstructionOrder order)
        {
            if (pawn == null)
            {
                return 0f;
            }

            float work = pawn.GetStatValue(
                StatDefOf.ConstructionSpeed,
                true,
                -1) * ConstructionSpeedGlobalFactor;

            ThingDef selectedStuff = ResolveSelectedStuff(order);
            if (selectedStuff != null)
            {
                work *= selectedStuff.GetStatValueAbstract(
                    StatDefOf.ConstructionSpeedFactor,
                    null);
            }

            return Mathf.Max(work, 0f);
        }

        internal static void LearnConstructionWork(Pawn pawn, int tickCount)
        {
            if (pawn == null || pawn.skills == null || tickCount <= 0)
            {
                return;
            }

            pawn.skills.Learn(
                SkillDefOf.Construction,
                tickCount * XpPerTick,
                false,
                false);
        }

        private static ThingDef ResolveSelectedStuff(ShuttleAssemblyConstructionOrder order)
        {
            if (order == null || string.IsNullOrEmpty(order.SelectedStuffDefName))
            {
                return null;
            }

            return DefDatabase<ThingDef>.GetNamedSilentFail(order.SelectedStuffDefName);
        }
    }
}

using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Prisoners
{
    /// <summary>
    /// Minimal held-prisoner needs runtime. P6A only advances food level and
    /// deliberately avoids full pawn/holder ticking, jobs, guest logic, and warden logic.
    /// Starvation hediffs and other health effects are intentionally outside this service.
    /// </summary>
    internal sealed class ShuttlePrisonerNeedsService
    {
        internal void TickFoodNeeds(
            CompShuttlePrisonCellOccupancy occupancy,
            int elapsedTicks)
        {
            if (occupancy == null || elapsedTicks <= 0)
            {
                return;
            }

            IReadOnlyList<Pawn> prisoners = occupancy.HeldPrisonersForReading;
            if (prisoners == null || prisoners.Count == 0)
            {
                return;
            }

            for (int i = 0; i < prisoners.Count; i++)
            {
                this.TickPrisonerFoodNeed(occupancy, prisoners[i], elapsedTicks);
            }
        }

        private void TickPrisonerFoodNeed(
            CompShuttlePrisonCellOccupancy occupancy,
            Pawn prisoner,
            int elapsedTicks)
        {
            if (prisoner == null ||
                prisoner.Destroyed ||
                prisoner.Dead ||
                prisoner.RaceProps == null ||
                !prisoner.RaceProps.Humanlike ||
                !occupancy.ContainsPrisoner(prisoner) ||
                prisoner.needs == null ||
                prisoner.needs.food == null)
            {
                return;
            }

            Need_Food food = prisoner.needs.food;
            float fallPerTick = this.GetFoodFallPerTickSafe(prisoner, food);
            if (fallPerTick <= 0f)
            {
                return;
            }

            float currentLevel = food.CurLevel;
            if (!this.IsFinite(currentLevel))
            {
                if (Prefs.DevMode)
                {
                    Log.Warning("[CeleTech Shuttle] PrisonCell held prisoner food level was invalid; clamping to zero. prisoner=" +
                        prisoner);
                }

                food.CurLevel = 0f;
                return;
            }

            float fall = fallPerTick * elapsedTicks;
            if (!this.IsFinite(fall) || fall <= 0f)
            {
                return;
            }

            float nextLevel = currentLevel - fall;
            food.CurLevel = nextLevel > 0f ? nextLevel : 0f;
        }

        private float GetFoodFallPerTickSafe(Pawn prisoner, Need_Food food)
        {
            if (food == null)
            {
                return 0f;
            }

            float value = food.FoodFallPerTick;
            if (this.IsFinite(value) && value >= 0f)
            {
                return value;
            }

            if (Prefs.DevMode)
            {
                Log.Warning("[CeleTech Shuttle] PrisonCell held prisoner FoodFallPerTick was invalid; hunger tick skipped. prisoner=" +
                    prisoner +
                    " value=" +
                    value);
            }

            return 0f;
        }

        private bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

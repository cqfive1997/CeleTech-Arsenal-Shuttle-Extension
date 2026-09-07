using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections
{
    /// <summary>
    /// Derived static Habitat/Recreation capability. This is rebuildable read-side data;
    /// live occupants, dining food, and transfer manifest state must not be saved here.
    /// </summary>
    public sealed class HabitatProfile
    {
        private static readonly HabitatProfile empty = new HabitatProfile(
            false,
            false,
            false,
            0,
            0,
            0,
            0,
            1f,
            false,
            false,
            false,
            false,
            false,
            null,
            false,
            0,
            0,
            0,
            1f,
            null);

        public HabitatProfile(
            bool hasHabitat,
            bool supportsSleep,
            bool supportsDining,
            int sleepSlots,
            int diningSlots,
            int sleepThoughtStageIndex,
            int diningThoughtStageIndex,
            float restEffectiveness,
            bool suppressSleepDisturbedThoughts,
            bool suppressBarracksThoughts,
            bool allowsInventoryFood,
            bool allowsCargoFoodWithdrawal,
            bool requiresCargoLogisticsForFoodWithdrawal,
            IReadOnlyList<ThingDef> preferredAutoFoodDefs,
            bool supportsJoy,
            int joySlots,
            int joyKindCapacity,
            int joyThoughtStageIndex,
            float joyGainFactor,
            IReadOnlyList<JoyKindDef> joyKinds)
        {
            this.HasHabitat = hasHabitat;
            this.SleepSlots = Max(0, sleepSlots);
            this.DiningSlots = Max(0, diningSlots);
            this.SupportsSleep = supportsSleep && this.SleepSlots > 0;
            this.SupportsDining = supportsDining && this.DiningSlots > 0;
            this.SleepThoughtStageIndex = this.SupportsSleep ? Max(0, sleepThoughtStageIndex) : 0;
            this.DiningThoughtStageIndex = this.SupportsDining ? Max(0, diningThoughtStageIndex) : 0;
            this.RestEffectiveness = IsFinitePositive(restEffectiveness) ? restEffectiveness : 1f;
            this.SuppressSleepDisturbedThoughts = suppressSleepDisturbedThoughts;
            this.SuppressBarracksThoughts = suppressBarracksThoughts;
            this.AllowsInventoryFood = allowsInventoryFood;
            this.AllowsCargoFoodWithdrawal = allowsCargoFoodWithdrawal;
            this.RequiresCargoLogisticsForFoodWithdrawal =
                allowsCargoFoodWithdrawal && requiresCargoLogisticsForFoodWithdrawal;
            this.PreferredAutoFoodDefs = CopyPreferredFoodDefs(preferredAutoFoodDefs);
            this.JoySlots = Max(0, joySlots);
            this.JoyKindCapacity = Max(0, joyKindCapacity);
            this.JoyThoughtStageIndex = Max(0, joyThoughtStageIndex);
            this.JoyGainFactor = IsFinitePositive(joyGainFactor) ? joyGainFactor : 1f;
            this.JoyKinds = CopyJoyKinds(joyKinds);
            this.SupportsJoy = supportsJoy && this.JoySlots > 0 && this.JoyKinds.Count > 0;
        }

        public static HabitatProfile Empty
        {
            get
            {
                return empty;
            }
        }

        public bool HasHabitat { get; private set; }
        public bool SupportsSleep { get; private set; }
        public bool SupportsDining { get; private set; }
        public int SleepSlots { get; private set; }
        public int DiningSlots { get; private set; }
        public int SleepThoughtStageIndex { get; private set; }
        public int DiningThoughtStageIndex { get; private set; }
        public float RestEffectiveness { get; private set; }
        public bool SuppressSleepDisturbedThoughts { get; private set; }
        public bool SuppressBarracksThoughts { get; private set; }
        public bool AllowsInventoryFood { get; private set; }
        public bool AllowsCargoFoodWithdrawal { get; private set; }
        public bool RequiresCargoLogisticsForFoodWithdrawal { get; private set; }
        public IReadOnlyList<ThingDef> PreferredAutoFoodDefs { get; private set; }
        public bool SupportsJoy { get; private set; }
        public int JoySlots { get; private set; }
        public int JoyKindCapacity { get; private set; }
        public int JoyThoughtStageIndex { get; private set; }
        public float JoyGainFactor { get; private set; }
        public IReadOnlyList<JoyKindDef> JoyKinds { get; private set; }

        public bool SupportsJoyKind(JoyKindDef joyKind)
        {
            if (joyKind == null || !this.SupportsJoy || this.JoyKinds == null)
            {
                return false;
            }

            for (int i = 0; i < this.JoyKinds.Count; i++)
            {
                if (this.JoyKinds[i] == joyKind)
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<ThingDef> CopyPreferredFoodDefs(IReadOnlyList<ThingDef> preferredAutoFoodDefs)
        {
            List<ThingDef> copy = new List<ThingDef>();
            if (preferredAutoFoodDefs == null)
            {
                return copy;
            }

            for (int i = 0; i < preferredAutoFoodDefs.Count; i++)
            {
                ThingDef foodDef = preferredAutoFoodDefs[i];
                if (foodDef != null && !copy.Contains(foodDef))
                {
                    copy.Add(foodDef);
                }
            }

            return copy;
        }

        private static IReadOnlyList<JoyKindDef> CopyJoyKinds(IReadOnlyList<JoyKindDef> joyKinds)
        {
            List<JoyKindDef> copy = new List<JoyKindDef>();
            if (joyKinds == null)
            {
                return copy;
            }

            for (int i = 0; i < joyKinds.Count; i++)
            {
                JoyKindDef joyKind = joyKinds[i];
                if (joyKind != null && !copy.Contains(joyKind))
                {
                    copy.Add(joyKind);
                }
            }

            return copy;
        }

        private static bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }

        private static int Max(int a, int b)
        {
            return a > b ? a : b;
        }
    }
}

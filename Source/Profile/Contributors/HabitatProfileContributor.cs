using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;
using RimWorld;
using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Contributors
{
    /// <summary>
    /// Static Habitat/Recreation profile contribution. It exposes capability and slot
    /// counts only; live pawns, dining food, and holder transfer stay in comps/services.
    /// </summary>
    public sealed class HabitatProfileContributor : IShuttleProfileContributor
    {
        private static readonly HabitatProfileContributor instance = new HabitatProfileContributor();

        private HabitatProfileContributor()
        {
        }

        public static HabitatProfileContributor Instance
        {
            get
            {
                return instance;
            }
        }

        public void Contribute(ShuttleProfileContributionContext context)
        {
            if (context == null || context.Contributions == null)
            {
                return;
            }

            ShuttleHabitatModuleDef habitatDef = context.ModuleDef as ShuttleHabitatModuleDef;
            if (habitatDef == null)
            {
                return;
            }

            IShuttleHabitatProfileContributionSink habitatSink =
                context.Contributions as IShuttleHabitatProfileContributionSink;
            if (habitatSink == null)
            {
                return;
            }

            habitatSink.AddHabitatModule(
                habitatDef.habitatSupportsSleep,
                habitatDef.habitatSleepSlots,
                habitatDef.habitatSleepThoughtStageIndex,
                habitatDef.habitatSupportsDining,
                habitatDef.habitatDiningSlots,
                habitatDef.habitatDiningThoughtStageIndex,
                habitatDef.habitatRestEffectiveness,
                habitatDef.suppressSleepDisturbedThoughts,
                habitatDef.suppressBarracksThoughts,
                habitatDef.allowInventoryFood,
                habitatDef.allowCargoFoodWithdrawal,
                habitatDef.requireCargoLogisticsForFoodWithdrawal,
                habitatDef.preferredAutoFoodDefs);

            IShuttleHabitatJoyProfileContributionSink joySink =
                context.Contributions as IShuttleHabitatJoyProfileContributionSink;
            if (joySink == null)
            {
                return;
            }

            IReadOnlyList<JoyKindDef> selectedJoyKinds =
                HabitatJoySelectionUtility.GetSelectedJoyKinds(context.Module, habitatDef);
            joySink.AddHabitatJoyModule(
                habitatDef.habitatSupportsJoy,
                habitatDef.habitatJoySlots,
                habitatDef.habitatJoyKindCapacity,
                habitatDef.habitatJoyThoughtStageIndex,
                habitatDef.habitatJoyGainFactor,
                selectedJoyKinds);
        }
    }
}

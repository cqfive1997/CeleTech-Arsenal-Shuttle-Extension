using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Food
{
    /// <summary>
    /// Selection-side helper over already-loaded ordinary cargo and active refrigerated cargo.
    /// It returns copied source identity plus the selected Thing for immediate nutrition
    /// calculations, but it never removes, transfers, destroys, or deposits cargo.
    /// </summary>
    internal sealed class ShuttleCargoFoodCandidateSelector
    {
        private readonly IShuttleCargoBackend cargoBackend;
        private readonly ShuttleRuntimeState runtimeState;
        private readonly ShuttleAssemblyState assemblyState;

        internal ShuttleCargoFoodCandidateSelector(
            IShuttleCargoBackend cargoBackend,
            ShuttleRuntimeState runtimeState,
            ShuttleAssemblyState assemblyState)
        {
            this.cargoBackend = cargoBackend;
            this.runtimeState = runtimeState;
            this.assemblyState = assemblyState;
        }

        internal bool TryFindBestFood(
            ThingWithComps shuttleHost,
            Pawn eater,
            ShuttleCargoFoodSelectionPolicy policy,
            out ShuttleCargoFoodCandidate bestCandidate)
        {
            bestCandidate = null;
            if (this.cargoBackend == null || policy == null)
            {
                return false;
            }

            this.AddOrdinaryCargoCandidates(
                shuttleHost,
                eater,
                policy,
                ref bestCandidate);
            if (policy.AllowRefrigerated)
            {
                this.AddRefrigeratedCargoCandidates(
                    shuttleHost,
                    eater,
                    policy,
                    ref bestCandidate);
            }

            return bestCandidate != null;
        }

        internal int ComputeIngestCount(Pawn eater, Thing food)
        {
            if (eater == null || food == null || food.def == null || food.stackCount <= 0)
            {
                return 0;
            }

            float nutrition = this.GetNutritionPerItem(eater, food);
            if (nutrition <= 0f)
            {
                return 0;
            }

            int count = FoodUtility.WillIngestStackCountOf(eater, food.def, nutrition);
            if (count < 1)
            {
                count = 1;
            }

            return count > food.stackCount ? food.stackCount : count;
        }

        internal float GetNutritionPerItem(Pawn eater, Thing food)
        {
            if (eater == null || food == null)
            {
                return 0f;
            }

            float nutrition = FoodUtility.NutritionForEater(eater, food);
            if (nutrition <= 0f)
            {
                nutrition = food.GetStatValue(StatDefOf.Nutrition);
            }

            return nutrition > 0f ? nutrition : 0f;
        }

        private void AddOrdinaryCargoCandidates(
            ThingWithComps shuttleHost,
            Pawn eater,
            ShuttleCargoFoodSelectionPolicy policy,
            ref ShuttleCargoFoodCandidate bestCandidate)
        {
            List<CompTransporter> transporters =
                this.cargoBackend.ResolveTransportersForLaunch(shuttleHost);
            if (transporters == null)
            {
                return;
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                ThingOwner contents = transporter != null
                    ? transporter.GetDirectlyHeldThings()
                    : null;
                this.AddCandidatesFromHolder(
                    eater,
                    policy,
                    contents,
                    ShuttleCargoInventorySourceKind.RegularCargo,
                    i,
                    null,
                    0,
                    ref bestCandidate);
            }
        }

        private void AddRefrigeratedCargoCandidates(
            ThingWithComps shuttleHost,
            Pawn eater,
            ShuttleCargoFoodSelectionPolicy policy,
            ref ShuttleCargoFoodCandidate bestCandidate)
        {
            CompShuttleRefrigeratedCargoRegistry registry = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleRefrigeratedCargoRegistry>()
                : null;
            if (registry == null)
            {
                return;
            }

            if (this.assemblyState != null && this.runtimeState != null)
            {
                registry.Reconcile(this.assemblyState, this.runtimeState);
            }

            IReadOnlyList<RefrigeratedCargoRecord> records = registry.Records;
            for (int i = 0; records != null && i < records.Count; i++)
            {
                RefrigeratedCargoRecord record = records[i];
                if (record == null || !record.CoolingActive || !record.HasContents)
                {
                    continue;
                }

                this.AddCandidatesFromHolder(
                    eater,
                    policy,
                    record.GetDirectlyHeldThings(),
                    ShuttleCargoInventorySourceKind.RefrigeratedCargo,
                    i,
                    record.ModuleInstanceID,
                    1,
                    ref bestCandidate);
            }
        }

        private void AddCandidatesFromHolder(
            Pawn eater,
            ShuttleCargoFoodSelectionPolicy policy,
            ThingOwner contents,
            ShuttleCargoInventorySourceKind sourceKind,
            int sourceIndex,
            string moduleInstanceID,
            int sourcePriority,
            ref ShuttleCargoFoodCandidate bestCandidate)
        {
            if (contents == null)
            {
                return;
            }

            for (int i = 0; i < contents.Count; i++)
            {
                Thing food = contents[i];
                if (!this.IsUsableFood(
                    eater,
                    food,
                    policy.MinimumPreferability,
                    policy.MaximumPreferability))
                {
                    continue;
                }

                CargoStackRef stackRef = new CargoStackRef(
                    food.thingIDNumber,
                    food.def.defName,
                    food.stackCount,
                    sourceKind,
                    sourceIndex,
                    moduleInstanceID);
                ShuttleCargoFoodCandidate candidate = new ShuttleCargoFoodCandidate(
                    stackRef,
                    food,
                    sourcePriority);
                if (this.IsBetterCandidate(eater, candidate, bestCandidate, policy))
                {
                    bestCandidate = candidate;
                }
            }
        }

        private bool IsBetterCandidate(
            Pawn eater,
            ShuttleCargoFoodCandidate candidate,
            ShuttleCargoFoodCandidate currentBest,
            ShuttleCargoFoodSelectionPolicy policy)
        {
            if (candidate == null || candidate.Food == null)
            {
                return false;
            }

            if (currentBest == null || currentBest.Food == null)
            {
                return true;
            }

            if (policy.Mode == ShuttleCargoFoodSelectionMode.PreferredDefsThenHighest)
            {
                int candidatePreferredIndex = this.GetPreferredIndex(
                    policy.PreferredFoodDefs,
                    candidate.Food.def);
                int bestPreferredIndex = this.GetPreferredIndex(
                    policy.PreferredFoodDefs,
                    currentBest.Food.def);
                if (candidatePreferredIndex != bestPreferredIndex)
                {
                    return candidatePreferredIndex < bestPreferredIndex;
                }

                if (candidatePreferredIndex == int.MaxValue)
                {
                    FoodPreferability candidateTier = candidate.Food.def.ingestible.preferability;
                    FoodPreferability bestTier = currentBest.Food.def.ingestible.preferability;
                    if (candidateTier != bestTier)
                    {
                        return candidateTier > bestTier;
                    }
                }

                return this.HasBetterNutritionOrStack(eater, candidate.Food, currentBest.Food);
            }

            FoodPreferability candidatePreferability =
                candidate.Food.def.ingestible.preferability;
            FoodPreferability bestPreferability =
                currentBest.Food.def.ingestible.preferability;
            if (candidatePreferability != bestPreferability)
            {
                return candidatePreferability < bestPreferability;
            }

            if (candidate.SourcePriority != currentBest.SourcePriority)
            {
                return candidate.SourcePriority < currentBest.SourcePriority;
            }

            return this.HasBetterNutritionOrStack(eater, candidate.Food, currentBest.Food);
        }

        private bool HasBetterNutritionOrStack(Pawn eater, Thing candidate, Thing currentBest)
        {
            float candidateNutrition = this.GetNutritionPerItem(eater, candidate);
            float bestNutrition = this.GetNutritionPerItem(eater, currentBest);
            return candidateNutrition > bestNutrition ||
                (candidateNutrition == bestNutrition &&
                    candidate.stackCount > currentBest.stackCount);
        }

        private int GetPreferredIndex(
            IReadOnlyList<ThingDef> preferredFoodDefs,
            ThingDef foodDef)
        {
            for (int i = 0; preferredFoodDefs != null && i < preferredFoodDefs.Count; i++)
            {
                if (preferredFoodDefs[i] == foodDef)
                {
                    return i;
                }
            }

            return int.MaxValue;
        }

        private bool IsUsableFood(
            Pawn eater,
            Thing food,
            FoodPreferability minimumPreferability,
            FoodPreferability maximumPreferability)
        {
            if (eater == null ||
                food == null ||
                food.Destroyed ||
                food.def == null ||
                food.stackCount <= 0 ||
                food.def.ingestible == null ||
                !food.def.IsNutritionGivingIngestible ||
                !food.IngestibleNow ||
                food.def.IsDrug ||
                food.def.ingestible.preferability < minimumPreferability ||
                food.def.ingestible.preferability > maximumPreferability)
            {
                return false;
            }

            return eater.WillEat(food, eater, true, false);
        }
    }

    internal sealed class ShuttleCargoFoodCandidate
    {
        internal ShuttleCargoFoodCandidate(
            CargoStackRef stackRef,
            Thing food,
            int sourcePriority)
        {
            this.StackRef = stackRef;
            this.Food = food;
            this.SourcePriority = sourcePriority;
        }

        internal CargoStackRef StackRef { get; private set; }

        internal Thing Food { get; private set; }

        internal int SourcePriority { get; private set; }
    }
}

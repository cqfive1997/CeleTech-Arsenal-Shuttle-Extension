using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoLoadSelectionModel
    {
        private readonly V3CargoLoadTransferableMetricsResolver metricsResolver;
        private ShuttleLoadCargoReadModel readModel;
        private List<TransferableOneWay> passengerTransferables;
        private List<TransferableOneWay> cargoTransferables;
        private bool selectionCountCacheValid;
        private int cachedSelectedCount;
        private int cachedSelectedEntryCount;
        private bool selectionMassCacheValid;
        private float cachedSelectedMass;
        private float cachedRegularSelectedMass;
        private float cachedColdSelectedMass;
        private readonly List<TransferableOneWay> selectedTransferablesCache =
            new List<TransferableOneWay>();
        private readonly Dictionary<TransferableOneWay, int> maximumSelectableCountCache =
            new Dictionary<TransferableOneWay, int>();
        private bool selectedTransferablesCacheValid;

        internal V3CargoLoadSelectionModel(
            ShuttleLoadCargoReadModel readModel,
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables,
            V3CargoLoadTransferableMetricsResolver metricsResolver)
        {
            this.metricsResolver = metricsResolver;
            this.Rebind(readModel, passengerTransferables, cargoTransferables);
        }

        internal void Rebind(
            ShuttleLoadCargoReadModel readModel,
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables)
        {
            this.readModel = readModel;
            this.passengerTransferables = passengerTransferables ?? new List<TransferableOneWay>();
            this.cargoTransferables = cargoTransferables ?? new List<TransferableOneWay>();
            this.InvalidateSelectionCaches();
        }

        internal List<TransferableOneWay> GetActiveTransferables(bool passengers)
        {
            return passengers ? this.passengerTransferables : this.cargoTransferables;
        }

        internal List<TransferableOneWay> GetSelectedTransferables()
        {
            if (!this.selectedTransferablesCacheValid)
            {
                this.selectedTransferablesCache.Clear();
                this.AddSelectedTransferables(this.selectedTransferablesCache, this.passengerTransferables);
                this.AddSelectedTransferables(this.selectedTransferablesCache, this.cargoTransferables);
                this.selectedTransferablesCacheValid = true;
            }

            return this.selectedTransferablesCache;
        }

        internal void ClearAllSelections()
        {
            this.ClearTransferableSelections(this.passengerTransferables);
            this.ClearTransferableSelections(this.cargoTransferables);
            this.InvalidateSelectionCaches();
        }

        internal Dictionary<TransferableOneWay, int> CaptureSelectionCounts()
        {
            Dictionary<TransferableOneWay, int> counts =
                new Dictionary<TransferableOneWay, int>();
            this.CaptureSelectionCounts(this.passengerTransferables, counts);
            this.CaptureSelectionCounts(this.cargoTransferables, counts);
            return counts;
        }

        internal void RestoreSelectionCounts(
            Dictionary<TransferableOneWay, int> counts)
        {
            this.RestoreSelectionCounts(this.passengerTransferables, counts);
            this.RestoreSelectionCounts(this.cargoTransferables, counts);
            this.InvalidateSelectionCaches();
        }

        internal void AdjustSelection(
            TransferableOneWay transferable,
            int delta,
            out string rejectMessageKey)
        {
            rejectMessageKey = null;
            if (transferable == null || delta == 0)
            {
                return;
            }

            int current = transferable.CountToTransfer;
            int max = this.GetMaxCount(transferable);
            int next = Mathf.Clamp(current + delta, 0, max);
            if (next > current && !this.CanAddCount(transferable, next - current))
            {
                rejectMessageKey = "CT_Shuttle_Cargo_SelectedExceedsCapacity";
                return;
            }

            transferable.AdjustTo(next);
            this.InvalidateSelectionCaches();
        }

        internal void SelectAll(
            TransferableOneWay transferable,
            out string rejectMessageKey)
        {
            rejectMessageKey = null;
            if (transferable == null)
            {
                return;
            }

            int max = this.GetMaxCount(transferable);
            int delta = max - transferable.CountToTransfer;
            if (delta <= 0)
            {
                return;
            }

            if (!this.CanAddCount(transferable, delta))
            {
                rejectMessageKey = "CT_Shuttle_Cargo_SelectedExceedsCapacity";
                return;
            }

            transferable.AdjustTo(max);
            this.InvalidateSelectionCaches();
        }

        internal bool TrySetSelectionCountExact(
            TransferableOneWay transferable,
            int requestedCount,
            out int appliedCount)
        {
            appliedCount = transferable != null ? transferable.CountToTransfer : 0;
            if (transferable == null)
            {
                return false;
            }

            int current = transferable.CountToTransfer;
            int desired = Mathf.Clamp(requestedCount, 0, this.GetMaxCount(transferable));
            if (desired == current)
            {
                appliedCount = current;
                return true;
            }

            if (desired > current && !this.CanAddCount(transferable, desired - current))
            {
                return false;
            }

            transferable.AdjustTo(desired);
            appliedCount = desired;
            this.InvalidateSelectionCaches();
            return true;
        }

        internal int SetSelectionCountToFit(
            TransferableOneWay transferable,
            int requestedCount)
        {
            if (transferable == null)
            {
                return 0;
            }

            int current = transferable.CountToTransfer;
            int desired = Mathf.Clamp(requestedCount, 0, this.GetMaxCount(transferable));
            if (desired <= current)
            {
                if (desired != current)
                {
                    transferable.AdjustTo(desired);
                    this.InvalidateSelectionCaches();
                }

                return desired;
            }

            int additional = this.GetMaximumAdditionalFitCount(
                transferable,
                desired - current);
            if (additional <= 0)
            {
                return current;
            }

            int applied = current + additional;
            transferable.AdjustTo(applied);
            this.InvalidateSelectionCaches();
            return applied;
        }

        internal int SelectMaximumFit(TransferableOneWay transferable)
        {
            return transferable == null
                ? 0
                : this.SetSelectionCountToFit(
                    transferable,
                    this.GetMaxCount(transferable));
        }

        internal int GetMaximumSelectableCount(TransferableOneWay transferable)
        {
            if (transferable == null)
            {
                return 0;
            }

            int cachedMaximum;
            if (this.maximumSelectableCountCache.TryGetValue(
                    transferable,
                    out cachedMaximum))
            {
                return cachedMaximum;
            }

            int current = transferable.CountToTransfer;
            int maximum = this.GetMaxCount(transferable);
            if (current >= maximum)
            {
                this.maximumSelectableCountCache[transferable] = maximum;
                return maximum;
            }

            int result = current + this.GetMaximumAdditionalFitCount(
                transferable,
                maximum - current);
            this.maximumSelectableCountCache[transferable] = result;
            return result;
        }

        internal bool CanAddCount(TransferableOneWay transferable, int count)
        {
            return this.CanAddCountWithUnitMass(transferable, count, this.GetUnitMass(transferable));
        }

        internal bool CanAddCountWithUnitMass(
            TransferableOneWay transferable,
            int count,
            float unitMass)
        {
            if (transferable == null || count <= 0 || unitMass <= 0f)
            {
                return false;
            }

            return this.CanFitAddedMass(transferable, V3CargoLoadTransferableMetricsResolver.CalculateMass(unitMass, count));
        }

        private int GetMaximumAdditionalFitCount(
            TransferableOneWay transferable,
            int requestedAdditional)
        {
            if (transferable == null || requestedAdditional <= 0)
            {
                return 0;
            }

            if (this.CanAddCount(transferable, requestedAdditional))
            {
                return requestedAdditional;
            }

            int low = 1;
            int high = requestedAdditional - 1;
            int best = 0;
            while (low <= high)
            {
                int middle = low + ((high - low) / 2);
                if (this.CanAddCount(transferable, middle))
                {
                    best = middle;
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return best;
        }

        internal void InvalidateSelectionCaches()
        {
            this.selectionCountCacheValid = false;
            this.selectionMassCacheValid = false;
            this.selectedTransferablesCacheValid = false;
            this.maximumSelectableCountCache.Clear();
        }

        internal int SelectedCount()
        {
            this.EnsureSelectionCountCache();
            return this.cachedSelectedCount;
        }

        internal int SelectedEntryCount()
        {
            this.EnsureSelectionCountCache();
            return this.cachedSelectedEntryCount;
        }

        internal float SelectedMass()
        {
            this.EnsureSelectionMassCache();
            return this.cachedSelectedMass;
        }

        internal float AvailableMass()
        {
            return this.readModel != null ? this.readModel.AvailableMassKg : 0f;
        }

        internal float MassCapacity()
        {
            if (this.readModel == null)
            {
                return 0f;
            }

            return this.readModel.RefrigeratedMassSharesOverallCapacity
                ? this.readModel.MassCapacityKg
                : this.readModel.MassCapacityKg +
                    this.readModel.RefrigeratedAutoTransferCapacityKg;
        }

        internal float RegularAvailableMass()
        {
            if (this.readModel == null)
            {
                return 0f;
            }

            if (this.readModel.RefrigeratedMassSharesOverallCapacity)
            {
                return Mathf.Max(0f, this.readModel.RegularAvailableMassKg);
            }

            if (this.readModel.RegularAvailableMassKg > 0f)
            {
                return this.readModel.RegularAvailableMassKg;
            }

            float derivedRegularAvailable = this.readModel.MassCapacityKg - this.readModel.ExistingMassUsageKg;
            if (derivedRegularAvailable > 0f)
            {
                return derivedRegularAvailable;
            }

            float inferredRegularAvailable = this.readModel.AvailableMassKg -
                this.readModel.RefrigeratedAutoTransferAvailableMassKg;
            if (inferredRegularAvailable > 0f)
            {
                return inferredRegularAvailable;
            }

            if (this.readModel.MassCapacityKg <= 0f &&
                this.readModel.ExistingMassUsageKg <= 0f &&
                this.readModel.AvailableMassKg > 0f)
            {
                return this.readModel.AvailableMassKg;
            }

            return 0f;
        }

        internal float RefrigeratedAvailableMass()
        {
            return this.readModel != null
                ? this.readModel.RefrigeratedAutoTransferAvailableMassKg
                : 0f;
        }

        internal float RemainingMass()
        {
            return Mathf.Max(0f, this.AvailableMass() - this.SelectedMass());
        }

        internal bool HasSelectionOverCapacity()
        {
            float regularMass;
            float coldMass;
            this.GetSelectedMassByDestination(out regularMass, out coldMass);
            if (this.readModel != null &&
                this.readModel.RefrigeratedMassSharesOverallCapacity)
            {
                return regularMass + coldMass > this.AvailableMass() + 0.001f;
            }

            return regularMass > this.RegularAvailableMass() + 0.001f ||
                coldMass > this.RefrigeratedAvailableMass() + 0.001f;
        }

        internal bool CanFitAddedMass(TransferableOneWay transferable, float addedMass)
        {
            if (transferable == null || addedMass <= 0f)
            {
                return false;
            }

            float regularMass;
            float coldMass;
            this.GetSelectedMassByDestination(out regularMass, out coldMass);
            ShuttleCargoLoadAdmissionResult admission = this.GetAdmission(transferable);
            if (this.readModel != null &&
                this.readModel.RefrigeratedMassSharesOverallCapacity)
            {
                bool accepted = admission == null ||
                    admission.RegularAccepted ||
                    admission.RefrigeratedAccepted;
                return accepted &&
                    regularMass + coldMass + addedMass <=
                        this.AvailableMass() + 0.001f;
            }

            if (admission != null &&
                admission.RefrigeratedAccepted &&
                addedMass <= admission.RefrigeratedAvailableMassKg + 0.001f &&
                coldMass + addedMass <= this.RefrigeratedAvailableMass() + 0.001f)
            {
                return true;
            }

            return (admission == null || admission.RegularAccepted) &&
                regularMass + addedMass <= this.RegularAvailableMass() + 0.001f;
        }

        internal void GetSelectedMassByDestination(out float regularMass, out float coldMass)
        {
            this.EnsureSelectionMassCache();
            regularMass = this.cachedRegularSelectedMass;
            coldMass = this.cachedColdSelectedMass;
        }

        internal Dictionary<string, int> CaptureSelectedCountsByKey()
        {
            Dictionary<string, int> selectedCountsByKey = new Dictionary<string, int>();
            this.CaptureSelectedCountsByKey(this.passengerTransferables, selectedCountsByKey);
            this.CaptureSelectedCountsByKey(this.cargoTransferables, selectedCountsByKey);
            return selectedCountsByKey;
        }

        internal void RestoreSelectedCountsByKey(Dictionary<string, int> selectedCountsByKey)
        {
            if (selectedCountsByKey == null || selectedCountsByKey.Count == 0)
            {
                return;
            }

            this.RestoreSelectedCountsByKey(this.passengerTransferables, selectedCountsByKey);
            this.RestoreSelectedCountsByKey(this.cargoTransferables, selectedCountsByKey);
        }

        internal ShuttleCargoLoadAdmissionResult GetAdmission(TransferableOneWay transferable)
        {
            if (this.readModel == null || this.metricsResolver == null)
            {
                return null;
            }

            return this.readModel.GetCargoAdmissionFor(this.metricsResolver.GetDisplayThing(transferable));
        }

        internal bool IsHeldPassenger(TransferableOneWay transferable)
        {
            if (this.readModel == null || this.metricsResolver == null)
            {
                return false;
            }

            return this.readModel.IsHeldPassenger(
                this.metricsResolver.GetDisplayThing(transferable));
        }

        private void AddSelectedTransferables(
            List<TransferableOneWay> selected,
            List<TransferableOneWay> source)
        {
            if (selected == null || source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                TransferableOneWay transferable = source[i];
                if (transferable != null && transferable.CountToTransfer > 0)
                {
                    selected.Add(transferable);
                }
            }
        }

        private void ClearTransferableSelections(List<TransferableOneWay> transferables)
        {
            if (transferables == null)
            {
                return;
            }

            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableOneWay transferable = transferables[i];
                if (transferable != null && transferable.CountToTransfer != 0)
                {
                    transferable.AdjustTo(0);
                }
            }
        }

        private void CaptureSelectionCounts(
            List<TransferableOneWay> transferables,
            Dictionary<TransferableOneWay, int> counts)
        {
            if (transferables == null || counts == null)
            {
                return;
            }

            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableOneWay transferable = transferables[i];
                if (transferable != null)
                {
                    counts[transferable] = transferable.CountToTransfer;
                }
            }
        }

        private void RestoreSelectionCounts(
            List<TransferableOneWay> transferables,
            Dictionary<TransferableOneWay, int> counts)
        {
            if (transferables == null)
            {
                return;
            }

            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableOneWay transferable = transferables[i];
                if (transferable == null)
                {
                    continue;
                }

                int savedCount = 0;
                if (counts != null)
                {
                    counts.TryGetValue(transferable, out savedCount);
                }

                transferable.AdjustTo(
                    Mathf.Clamp(savedCount, 0, this.GetMaxCount(transferable)));
            }
        }

        private void EnsureSelectionCountCache()
        {
            if (this.selectionCountCacheValid)
            {
                return;
            }

            long selectedCount =
                (long)this.CountSelectedFallback(this.passengerTransferables) +
                this.CountSelectedFallback(this.cargoTransferables);
            this.cachedSelectedCount = selectedCount >= int.MaxValue
                ? int.MaxValue
                : (int)selectedCount;
            this.cachedSelectedEntryCount =
                this.CountSelectedEntries(this.passengerTransferables) +
                this.CountSelectedEntries(this.cargoTransferables);
            this.selectionCountCacheValid = true;
        }

        private void EnsureSelectionMassCache()
        {
            if (this.selectionMassCacheValid)
            {
                return;
            }

            this.cachedRegularSelectedMass = this.GetSelectedMassFallback(this.passengerTransferables);
            this.cachedColdSelectedMass = 0f;
            this.AddCargoSelectedMassByDestination(
                this.cargoTransferables,
                ref this.cachedRegularSelectedMass,
                ref this.cachedColdSelectedMass);
            this.cachedSelectedMass = this.cachedRegularSelectedMass + this.cachedColdSelectedMass;
            this.selectionMassCacheValid = true;
        }

        private int CountSelectedEntries(List<TransferableOneWay> transferables)
        {
            int count = 0;
            if (transferables == null)
            {
                return count;
            }

            for (int i = 0; i < transferables.Count; i++)
            {
                if (transferables[i] != null && transferables[i].CountToTransfer > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountSelectedFallback(List<TransferableOneWay> transferables)
        {
            long count = 0L;
            if (transferables == null)
            {
                return 0;
            }

            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableOneWay transferable = transferables[i];
                if (transferable == null || transferable.CountToTransfer <= 0)
                {
                    continue;
                }

                count += transferable.CountToTransfer;
                if (count >= int.MaxValue)
                {
                    return int.MaxValue;
                }
            }

            return (int)count;
        }

        private float GetSelectedMassFallback(List<TransferableOneWay> transferables)
        {
            float total = 0f;
            if (transferables == null)
            {
                return total;
            }

            for (int i = 0; i < transferables.Count; i++)
            {
                total += this.GetSelectedMassForTransferable(transferables[i]);
            }

            return total;
        }

        private void AddCargoSelectedMassByDestination(
            List<TransferableOneWay> transferables,
            ref float regularMass,
            ref float coldMass)
        {
            if (transferables == null)
            {
                return;
            }

            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableOneWay transferable = transferables[i];
                if (transferable == null || transferable.CountToTransfer <= 0)
                {
                    continue;
                }

                float mass = this.GetSelectedMassForTransferable(transferable);
                ShuttleCargoLoadAdmissionResult admission = this.GetAdmission(transferable);
                if (this.readModel != null &&
                    this.readModel.RefrigeratedMassSharesOverallCapacity)
                {
                    if (admission != null &&
                        admission.RefrigeratedAccepted &&
                        regularMass + coldMass + mass <= this.AvailableMass() + 0.001f)
                    {
                        coldMass += mass;
                    }
                    else
                    {
                        regularMass += mass;
                    }
                }
                else if (admission != null &&
                    admission.RefrigeratedAccepted &&
                    mass <= admission.RefrigeratedAvailableMassKg + 0.001f &&
                    coldMass + mass <= this.RefrigeratedAvailableMass() + 0.001f)
                {
                    coldMass += mass;
                }
                else if ((admission == null || admission.RegularAccepted) &&
                    regularMass + mass <= this.RegularAvailableMass() + 0.001f)
                {
                    regularMass += mass;
                }
                else
                {
                    regularMass += mass;
                }
            }
        }

        private void CaptureSelectedCountsByKey(
            List<TransferableOneWay> transferables,
            Dictionary<string, int> selectedCountsByKey)
        {
            if (transferables == null || selectedCountsByKey == null)
            {
                return;
            }

            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableOneWay transferable = transferables[i];
                if (transferable == null || transferable.CountToTransfer <= 0)
                {
                    continue;
                }

                string key = this.GetTransferableSelectionKey(transferable);
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                int existing;
                selectedCountsByKey.TryGetValue(key, out existing);
                selectedCountsByKey[key] = existing + transferable.CountToTransfer;
            }
        }

        private void RestoreSelectedCountsByKey(
            List<TransferableOneWay> transferables,
            Dictionary<string, int> selectedCountsByKey)
        {
            if (transferables == null || selectedCountsByKey == null || selectedCountsByKey.Count == 0)
            {
                return;
            }

            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableOneWay transferable = transferables[i];
                string key = this.GetTransferableSelectionKey(transferable);
                int savedCount;
                if (string.IsNullOrEmpty(key) ||
                    !selectedCountsByKey.TryGetValue(key, out savedCount) ||
                    savedCount <= 0)
                {
                    continue;
                }

                int restoredCount = Mathf.Min(savedCount, this.GetMaxCount(transferable));
                if (restoredCount <= 0)
                {
                    continue;
                }

                transferable.AdjustTo(restoredCount);
                savedCount -= restoredCount;
                if (savedCount > 0)
                {
                    selectedCountsByKey[key] = savedCount;
                }
                else
                {
                    selectedCountsByKey.Remove(key);
                }
            }

            this.InvalidateSelectionCaches();
        }

        private string GetTransferableSelectionKey(TransferableOneWay transferable)
        {
            Thing thing = this.metricsResolver != null
                ? this.metricsResolver.GetDisplayThing(transferable)
                : null;
            if (thing == null || thing.def == null)
            {
                return null;
            }

            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                return "pawn:" + pawn.thingIDNumber;
            }

            if (thing.thingIDNumber > 0)
            {
                return "thing:" + thing.thingIDNumber;
            }

            string stuffDefName = thing.Stuff != null ? thing.Stuff.defName : string.Empty;
            string quality = string.Empty;
            ThingWithComps thingWithComps = thing as ThingWithComps;
            CompQuality qualityComp = thingWithComps != null ? thingWithComps.GetComp<CompQuality>() : null;
            if (qualityComp != null)
            {
                quality = qualityComp.Quality.ToString();
            }

            return "stack:" + thing.def.defName + "|" + stuffDefName + "|" + quality + "|" + thing.HitPoints;
        }

        internal int GetMaxCount(TransferableOneWay transferable)
        {
            return this.metricsResolver != null ? this.metricsResolver.GetMaxCount(transferable) : 0;
        }

        private float GetUnitMass(TransferableOneWay transferable)
        {
            return this.metricsResolver != null ? this.metricsResolver.GetUnitMass(transferable) : 0f;
        }

        private float GetSelectedMassForTransferable(TransferableOneWay transferable)
        {
            return this.metricsResolver != null
                ? this.metricsResolver.GetSelectedMassForTransferable(transferable)
                : 0f;
        }
    }
}

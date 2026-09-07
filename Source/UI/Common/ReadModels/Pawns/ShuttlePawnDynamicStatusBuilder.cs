using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.MechCharging;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns
{
    internal sealed class ShuttlePawnDynamicStatusBuilder
    {
        private const float ConsciousnessDownThreshold = 0.05f;

        internal ShuttlePawnDynamicStatusSnapshot Build(
            ShuttlePawnPresenceSnapshot presenceSnapshot)
        {
            return this.Build(presenceSnapshot, ShuttlePawnPresenceKind.None);
        }

        internal ShuttlePawnDynamicStatusSnapshot Build(
            ShuttlePawnPresenceSnapshot presenceSnapshot,
            ShuttlePawnPresenceKind includedKinds)
        {
            List<ShuttlePawnDynamicStatusRecord> records =
                new List<ShuttlePawnDynamicStatusRecord>();
            if (presenceSnapshot == null || presenceSnapshot.Records == null)
            {
                return new ShuttlePawnDynamicStatusSnapshot(records, 0, 0);
            }

            for (int i = 0; i < presenceSnapshot.Records.Count; i++)
            {
                ShuttlePawnPresenceRecord presence =
                    presenceSnapshot.Records[i];
                if (presence == null || this.IsMedicalPatientSource(presence))
                {
                    continue;
                }

                this.ApplyPresenceRecord(records, presence, includedKinds);
            }

            for (int i = 0; i < presenceSnapshot.Records.Count; i++)
            {
                ShuttlePawnPresenceRecord presence =
                    presenceSnapshot.Records[i];
                if (presence == null || !this.IsMedicalPatientSource(presence))
                {
                    continue;
                }

                this.ApplyPresenceRecord(records, presence, includedKinds);
            }

            return new ShuttlePawnDynamicStatusSnapshot(
                records,
                presenceSnapshot.ProfileRevision,
                presenceSnapshot.CargoSnapshotRevision);
        }

        private void ApplyPresenceRecord(
            List<ShuttlePawnDynamicStatusRecord> records,
            ShuttlePawnPresenceRecord presence,
            ShuttlePawnPresenceKind includedKinds)
        {
            if (records == null ||
                presence == null ||
                !this.ShouldIncludePresence(presence, includedKinds))
            {
                return;
            }

            int pawnThingID = this.GetPawnThingID(presence);
            if (pawnThingID <= 0)
            {
                return;
            }

            ShuttlePawnDynamicStatusRecord status =
                this.GetOrCreateRecord(records, presence, pawnThingID);
            this.ApplyPresenceFacts(status, presence);
        }

        private bool IsMedicalPatientSource(ShuttlePawnPresenceRecord presence)
        {
            return presence != null &&
                (presence.SourceModel as ShuttleMedicalPatientReadModel) != null;
        }

        private void ApplyPresenceFacts(
            ShuttlePawnDynamicStatusRecord status,
            ShuttlePawnPresenceRecord presence)
        {
            if (status == null || presence == null)
            {
                return;
            }

            ShuttleMedicalPatientReadModel medicalPatient =
                presence.SourceModel as ShuttleMedicalPatientReadModel;
            if (medicalPatient != null)
            {
                this.ApplyMedicalPatientFacts(status, medicalPatient);
                this.ApplyMedicalPatientPawnSupplement(status, presence.Pawn);
                return;
            }

            this.ApplyPawnFacts(status, presence.Pawn);
            this.ApplySourceFacts(status, presence.SourceModel);
        }

        private bool ShouldIncludePresence(
            ShuttlePawnPresenceRecord presence,
            ShuttlePawnPresenceKind includedKinds)
        {
            if (presence == null)
            {
                return false;
            }

            if (includedKinds == ShuttlePawnPresenceKind.None)
            {
                return true;
            }

            return (presence.Kind & includedKinds) != ShuttlePawnPresenceKind.None;
        }

        private int GetPawnThingID(ShuttlePawnPresenceRecord presence)
        {
            if (presence == null)
            {
                return 0;
            }

            if (presence.PawnThingID > 0)
            {
                return presence.PawnThingID;
            }

            Pawn pawn = presence.Pawn != null
                ? presence.Pawn
                : presence.DisplayThing as Pawn;
            return pawn != null ? pawn.thingIDNumber : 0;
        }

        private ShuttlePawnDynamicStatusRecord GetOrCreateRecord(
            List<ShuttlePawnDynamicStatusRecord> records,
            ShuttlePawnPresenceRecord presence,
            int pawnThingID)
        {
            for (int i = 0; i < records.Count; i++)
            {
                ShuttlePawnDynamicStatusRecord existing = records[i];
                if (existing != null && existing.PawnThingID == pawnThingID)
                {
                    this.FillIdentity(existing, presence, pawnThingID);
                    return existing;
                }
            }

            ShuttlePawnDynamicStatusRecord record =
                new ShuttlePawnDynamicStatusRecord();
            this.FillIdentity(record, presence, pawnThingID);
            records.Add(record);
            return record;
        }

        private void FillIdentity(
            ShuttlePawnDynamicStatusRecord status,
            ShuttlePawnPresenceRecord presence,
            int pawnThingID)
        {
            if (status == null)
            {
                return;
            }

            status.PawnThingID = pawnThingID;
            if (string.IsNullOrEmpty(status.StableKey))
            {
                status.StableKey = presence != null &&
                    !string.IsNullOrEmpty(presence.StableKey)
                        ? presence.StableKey
                        : pawnThingID.ToString();
            }

            if (status.DisplayThing == null && presence != null)
            {
                status.DisplayThing = presence.DisplayThing;
            }

            if (status.Pawn == null && presence != null)
            {
                status.Pawn = presence.Pawn != null
                    ? presence.Pawn
                    : presence.DisplayThing as Pawn;
            }
        }

        private void ApplySourceFacts(
            ShuttlePawnDynamicStatusRecord status,
            object sourceModel)
        {
            if (status == null || sourceModel == null)
            {
                return;
            }

            ShuttleMedicalPatientReadModel medicalPatient =
                sourceModel as ShuttleMedicalPatientReadModel;
            if (medicalPatient != null)
            {
                this.ApplyMedicalPatientFacts(status, medicalPatient);
                return;
            }

            ShuttleHabitatOccupantReadModel habitatOccupant =
                sourceModel as ShuttleHabitatOccupantReadModel;
            if (habitatOccupant != null)
            {
                this.ApplyHabitatOccupantFacts(status, habitatOccupant);
                return;
            }

            ShuttleMechChargingPawnReadModel chargingMech =
                sourceModel as ShuttleMechChargingPawnReadModel;
            if (chargingMech != null)
            {
                this.ApplyMechChargingFacts(status, chargingMech);
            }
        }

        private void ApplyMedicalPatientFacts(
            ShuttlePawnDynamicStatusRecord status,
            ShuttleMedicalPatientReadModel source)
        {
            if (status == null || source == null)
            {
                return;
            }

            status.IsDowned = source.IsDowned;
            status.IsConsciousKnown = source.IsConsciousKnown;
            status.IsConscious = source.IsConscious;
            this.SetKnownPct(ref status.ConsciousnessPct, source.ConsciousnessPct);
            this.SetKnownPct(ref status.PainPct, source.PainPct);
            status.IsBleeding = source.IsBleeding;
            if (source.BleedRateTotal > status.BleedRateTotal)
            {
                status.BleedRateTotal = source.BleedRateTotal;
            }

            if (source.BleedingHediffCount > status.BleedingHediffCount)
            {
                status.BleedingHediffCount = source.BleedingHediffCount;
            }

            status.BleedSeverityPct = source.IsBleeding
                ? Mathf.Clamp01(Mathf.Max(
                    source.BleedRateTotal,
                    source.BleedingHediffCount * 0.20f))
                : 0f;
            this.SetKnownPct(ref status.FoodPct, source.FoodPct);
            this.SetKnownPct(ref status.RestPct, source.RestPct);
            this.SetKnownPct(ref status.JoyPct, source.JoyPct);
            this.SetKnownPct(ref status.MoodPct, source.MoodPct);
            this.ApplyMechDurabilityFacts(
                status,
                source.HasMechDurability,
                source.DurabilityPct,
                source.DamagePct);
            this.ApplyMechEnergyFacts(
                status,
                source.HasMechEnergy,
                source.EnergyPct);
        }

        private void ApplyMedicalPatientPawnSupplement(
            ShuttlePawnDynamicStatusRecord status,
            Pawn pawn)
        {
            if (status == null || pawn == null)
            {
                return;
            }

            status.Pawn = pawn;
            if (status.DisplayThing == null)
            {
                status.DisplayThing = pawn;
            }

            status.IsDead = pawn.Dead;
            this.SetKnownPct(
                ref status.MovingPct,
                this.GetCapacityPct(pawn, PawnCapacityDefOf.Moving));
        }

        private void ApplyHabitatOccupantFacts(
            ShuttlePawnDynamicStatusRecord status,
            ShuttleHabitatOccupantReadModel source)
        {
            if (status == null || source == null)
            {
                return;
            }

            this.SetKnownPct(ref status.FoodPct, source.FoodPct);
            this.SetKnownPct(ref status.RestPct, source.RestPct);
            this.SetKnownPct(ref status.JoyPct, source.JoyPct);
            this.SetKnownPct(ref status.MoodPct, source.MoodPct);
        }

        private void ApplyMechChargingFacts(
            ShuttlePawnDynamicStatusRecord status,
            ShuttleMechChargingPawnReadModel source)
        {
            if (status == null || source == null)
            {
                return;
            }

            this.ApplyMechEnergyFacts(status, source.EnergyPct >= 0f, source.EnergyPct);
        }

        private void ApplyPawnFacts(
            ShuttlePawnDynamicStatusRecord status,
            Pawn pawn)
        {
            if (status == null || pawn == null)
            {
                return;
            }

            status.Pawn = pawn;
            if (status.DisplayThing == null)
            {
                status.DisplayThing = pawn;
            }

            status.IsDead = pawn.Dead;
            status.IsDowned = pawn.Downed;
            this.ApplyCapacityFacts(status, pawn);
            this.SetKnownPct(ref status.PainPct, this.GetPainPct(pawn));
            this.ApplyBleedingFacts(status, pawn);
            this.ApplyNeedFacts(status, pawn);
            this.ApplyDirectMechFacts(status, pawn);
        }

        private void ApplyCapacityFacts(
            ShuttlePawnDynamicStatusRecord status,
            Pawn pawn)
        {
            float consciousness = this.GetCapacityPct(
                pawn,
                PawnCapacityDefOf.Consciousness);
            if (consciousness >= 0f)
            {
                status.ConsciousnessPct = consciousness;
                status.IsConsciousKnown = true;
                status.IsConscious = consciousness > ConsciousnessDownThreshold;
            }

            this.SetKnownPct(
                ref status.MovingPct,
                this.GetCapacityPct(pawn, PawnCapacityDefOf.Moving));
        }

        private void ApplyBleedingFacts(
            ShuttlePawnDynamicStatusRecord status,
            Pawn pawn)
        {
            if (status == null ||
                pawn == null ||
                pawn.health == null ||
                pawn.health.hediffSet == null)
            {
                return;
            }

            float bleedRate = pawn.health.hediffSet.BleedRateTotal;
            if (bleedRate > status.BleedRateTotal)
            {
                status.BleedRateTotal = bleedRate;
            }

            if (bleedRate > 0f)
            {
                status.IsBleeding = true;
            }

            status.BleedSeverityPct = status.IsBleeding
                ? Mathf.Clamp01(Mathf.Max(
                    status.BleedRateTotal,
                    status.BleedingHediffCount * 0.20f))
                : 0f;
        }

        private void ApplyNeedFacts(
            ShuttlePawnDynamicStatusRecord status,
            Pawn pawn)
        {
            if (status == null || pawn == null || pawn.needs == null)
            {
                return;
            }

            this.SetKnownPct(
                ref status.FoodPct,
                pawn.needs.food != null
                    ? pawn.needs.food.CurLevelPercentage
                    : -1f);
            this.SetKnownPct(
                ref status.RestPct,
                pawn.needs.rest != null
                    ? pawn.needs.rest.CurLevelPercentage
                    : -1f);
            this.SetKnownPct(
                ref status.JoyPct,
                pawn.needs.joy != null
                    ? pawn.needs.joy.CurLevelPercentage
                    : -1f);
            this.SetKnownPct(
                ref status.MoodPct,
                pawn.needs.mood != null
                    ? pawn.needs.mood.CurLevelPercentage
                    : -1f);
        }

        private void ApplyDirectMechFacts(
            ShuttlePawnDynamicStatusRecord status,
            Pawn pawn)
        {
            if (status == null ||
                pawn == null ||
                pawn.RaceProps == null ||
                !pawn.RaceProps.IsMechanoid)
            {
                return;
            }

            float durabilityPct = this.GetSummaryHealthPct(pawn);
            this.ApplyMechDurabilityFacts(
                status,
                durabilityPct >= 0f,
                durabilityPct,
                durabilityPct >= 0f ? Mathf.Clamp01(1f - durabilityPct) : -1f);

            float energyPct = ShuttleMechChargeNeedUtility.GetChargeNeedPct(pawn);
            this.ApplyMechEnergyFacts(status, energyPct >= 0f, energyPct);
        }

        private void ApplyMechDurabilityFacts(
            ShuttlePawnDynamicStatusRecord status,
            bool hasMechDurability,
            float durabilityPct,
            float damagePct)
        {
            if (status == null || !hasMechDurability)
            {
                return;
            }

            status.HasMechDurability = true;
            this.SetKnownPct(ref status.DurabilityPct, durabilityPct);
            this.SetKnownPct(ref status.DamagePct, damagePct);
        }

        private void ApplyMechEnergyFacts(
            ShuttlePawnDynamicStatusRecord status,
            bool hasMechEnergy,
            float energyPct)
        {
            if (status == null || !hasMechEnergy)
            {
                return;
            }

            status.HasMechEnergy = true;
            this.SetKnownPct(ref status.EnergyPct, energyPct);
        }

        private float GetCapacityPct(Pawn pawn, PawnCapacityDef capacityDef)
        {
            if (pawn == null ||
                pawn.health == null ||
                pawn.health.capacities == null ||
                capacityDef == null)
            {
                return -1f;
            }

            return this.Clamp01(pawn.health.capacities.GetLevel(capacityDef));
        }

        private float GetPainPct(Pawn pawn)
        {
            if (pawn == null ||
                pawn.health == null ||
                pawn.health.hediffSet == null)
            {
                return -1f;
            }

            return this.Clamp01(pawn.health.hediffSet.PainTotal);
        }

        private float GetSummaryHealthPct(Pawn pawn)
        {
            if (pawn == null ||
                pawn.health == null ||
                pawn.health.summaryHealth == null)
            {
                return -1f;
            }

            return this.Clamp01(pawn.health.summaryHealth.SummaryHealthPercent);
        }

        private void SetKnownPct(ref float target, float value)
        {
            if (value >= 0f)
            {
                target = this.Clamp01(value);
            }
        }

        private float Clamp01(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return -1f;
            }

            return Mathf.Clamp01(value);
        }
    }
}

using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    /// <summary>
    /// Medical-facing adapter for real Medicine in ordinary loaded cargo. Read projection uses
    /// copied Cargo stack identities and mutation is delegated to the Cargo transaction broker.
    /// Pending load queues, map stockpiles, and refrigerated cargo remain excluded.
    /// </summary>
    internal sealed class ShuttleLoadedCargoMedicineSupplySource : IShuttleMedicalSupplySource
    {
        public IReadOnlyList<ShuttleMedicineSupplyReadModel> ListLoadedMedicineForPatient(
            ThingWithComps shuttleHost,
            Pawn patient)
        {
            List<ShuttleMedicineSupplyReadModel> medicines =
                new List<ShuttleMedicineSupplyReadModel>();
            IShuttleCargoResourceBroker cargoBroker;
            ShuttleCargoInventorySnapshot snapshot;
            if (!this.TryGetSnapshot(shuttleHost, out cargoBroker, out snapshot))
            {
                return medicines;
            }

            IReadOnlyList<CargoStackRef> stackRefs = snapshot.StackRefs;
            for (int i = 0; stackRefs != null && i < stackRefs.Count; i++)
            {
                CargoStackRef stackRef = stackRefs[i];
                ThingDef medicineDef;
                if (!this.TryResolveRegularMedicine(stackRef, out medicineDef))
                {
                    continue;
                }

                medicines.Add(this.BuildReadModel(stackRef, medicineDef, patient));
            }

            medicines.Sort(this.CompareMedicine);
            return medicines;
        }

        internal bool TryTakeOneLoadedMedicineForPatient(
            ThingWithComps shuttleHost,
            Pawn patient,
            out Medicine medicine,
            out IShuttleMedicalSupplyWithdrawal withdrawal,
            out string failReason,
            bool allowMedicineWhenPatientSettingsMissing = false)
        {
            medicine = null;
            withdrawal = null;
            failReason = null;

            IShuttleCargoResourceBroker cargoBroker;
            CargoMedicineCandidate candidate;
            if (!this.TryFindBestLoadedMedicineForPatient(
                shuttleHost,
                patient,
                allowMedicineWhenPatientSettingsMissing,
                out cargoBroker,
                out candidate,
                out failReason))
            {
                return false;
            }

            ShuttleCargoWithdrawal cargoWithdrawal;
            string transactionFailure;
            if (!cargoBroker.TryBeginExactWithdrawal(
                candidate.StackRef,
                1,
                ShuttleCargoAccessRequirement.None,
                "Medical loaded cargo medicine",
                out cargoWithdrawal,
                out transactionFailure))
            {
                failReason = "CT_Shuttle_MedicalBay_LoadedCargoMedicineUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            medicine = cargoWithdrawal != null ? cargoWithdrawal.Thing as Medicine : null;
            if (!this.IsUsableMedicine(medicine) || medicine.stackCount != 1)
            {
                string rollbackFailure;
                if (cargoWithdrawal != null)
                {
                    cargoWithdrawal.TryRollBackToSource(out rollbackFailure);
                }

                failReason = "CT_Shuttle_MedicalBay_LoadedCargoMedicineUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            withdrawal = new LoadedCargoMedicineWithdrawal(
                cargoWithdrawal,
                candidate.SourceLabel,
                candidate.StackRef.ThingIDNumber,
                candidate.StackRef.Count);
            return true;
        }

        internal bool CanProvideLoadedMedicineForPatient(
            ThingWithComps shuttleHost,
            Pawn patient,
            out string failReason,
            bool allowMedicineWhenPatientSettingsMissing = false)
        {
            IShuttleCargoResourceBroker cargoBroker;
            CargoMedicineCandidate candidate;
            return this.TryFindBestLoadedMedicineForPatient(
                shuttleHost,
                patient,
                allowMedicineWhenPatientSettingsMissing,
                out cargoBroker,
                out candidate,
                out failReason);
        }

        internal bool HasAnyLoadedMedicine(ThingWithComps shuttleHost)
        {
            IShuttleCargoResourceBroker cargoBroker;
            ShuttleCargoInventorySnapshot snapshot;
            if (!this.TryGetSnapshot(shuttleHost, out cargoBroker, out snapshot))
            {
                return false;
            }

            IReadOnlyList<CargoStackRef> stackRefs = snapshot.StackRefs;
            for (int i = 0; stackRefs != null && i < stackRefs.Count; i++)
            {
                ThingDef medicineDef;
                if (this.TryResolveRegularMedicine(stackRefs[i], out medicineDef))
                {
                    return true;
                }
            }

            return false;
        }

        private ShuttleMedicineSupplyReadModel BuildReadModel(
            CargoStackRef stackRef,
            ThingDef medicineDef,
            Pawn patient)
        {
            ShuttleMedicineSupplyReadModel model = new ShuttleMedicineSupplyReadModel();
            model.MedicineDef = medicineDef;
            model.MedicineDefName = medicineDef != null ? medicineDef.defName : string.Empty;
            model.Label = medicineDef != null ? medicineDef.LabelCap.ToString() : string.Empty;
            model.StackCount = stackRef != null ? stackRef.Count : 0;
            model.Potency = this.GetMedicinePotency(medicineDef);
            model.AllowedByPatientMedCare = this.PatientAllowsMedicine(patient, medicineDef);
            model.SourceLabel = this.BuildSourceLabel(stackRef);
            model.SourceThingID = stackRef != null ? stackRef.ThingIDNumber : -1;
            return model;
        }

        private bool TryFindBestLoadedMedicineForPatient(
            ThingWithComps shuttleHost,
            Pawn patient,
            bool allowMedicineWhenPatientSettingsMissing,
            out IShuttleCargoResourceBroker cargoBroker,
            out CargoMedicineCandidate bestCandidate,
            out string failReason)
        {
            cargoBroker = null;
            bestCandidate = null;
            failReason = null;
            if (patient == null)
            {
                failReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                return false;
            }

            ShuttleCargoInventorySnapshot snapshot;
            if (!this.TryGetSnapshot(shuttleHost, out cargoBroker, out snapshot))
            {
                failReason = "CT_Shuttle_MedicalBay_NoLoadedCargoMedicine"
                    .Translate()
                    .ToString();
                return false;
            }

            bool foundMedicine = false;
            bool foundDisallowedMedicine = false;
            IReadOnlyList<CargoStackRef> stackRefs = snapshot.StackRefs;
            for (int i = 0; stackRefs != null && i < stackRefs.Count; i++)
            {
                CargoStackRef stackRef = stackRefs[i];
                ThingDef medicineDef;
                if (!this.TryResolveRegularMedicine(stackRef, out medicineDef))
                {
                    continue;
                }

                foundMedicine = true;
                if (!this.PatientAllowsMedicine(
                    patient,
                    medicineDef,
                    allowMedicineWhenPatientSettingsMissing))
                {
                    foundDisallowedMedicine = true;
                    continue;
                }

                CargoMedicineCandidate candidate = new CargoMedicineCandidate(
                    stackRef,
                    medicineDef,
                    this.BuildSourceLabel(stackRef));
                if (bestCandidate == null ||
                    this.CompareCandidates(candidate, bestCandidate) < 0)
                {
                    bestCandidate = candidate;
                }
            }

            if (bestCandidate != null)
            {
                return true;
            }

            failReason = foundMedicine && foundDisallowedMedicine
                ? "CT_Shuttle_MedicalBay_NoAllowedLoadedCargoMedicine".Translate().ToString()
                : "CT_Shuttle_MedicalBay_NoLoadedCargoMedicine".Translate().ToString();
            return false;
        }

        private bool TryGetSnapshot(
            ThingWithComps shuttleHost,
            out IShuttleCargoResourceBroker cargoBroker,
            out ShuttleCargoInventorySnapshot snapshot)
        {
            snapshot = null;
            if (!ShuttleCargoTransactionResolver.TryResolveBroker(
                shuttleHost,
                out cargoBroker))
            {
                return false;
            }

            snapshot = cargoBroker.GetInventorySnapshot();
            return snapshot != null && snapshot.IsAvailable;
        }

        private bool TryResolveRegularMedicine(
            CargoStackRef stackRef,
            out ThingDef medicineDef)
        {
            medicineDef = null;
            if (stackRef == null ||
                stackRef.SourceKind != ShuttleCargoInventorySourceKind.RegularCargo ||
                stackRef.Count <= 0 ||
                string.IsNullOrEmpty(stackRef.DefName))
            {
                return false;
            }

            medicineDef = DefDatabase<ThingDef>.GetNamedSilentFail(stackRef.DefName);
            return medicineDef != null && medicineDef.IsMedicine;
        }

        private bool IsUsableMedicine(Medicine medicine)
        {
            return medicine != null &&
                !medicine.Destroyed &&
                medicine.def != null &&
                medicine.def.IsMedicine &&
                medicine.stackCount > 0;
        }

        private bool PatientAllowsMedicine(
            Pawn patient,
            ThingDef medicineDef,
            bool allowMedicineWhenPatientSettingsMissing = false)
        {
            if (patient == null || medicineDef == null || !medicineDef.IsMedicine)
            {
                return false;
            }

            if (patient.playerSettings == null)
            {
                return allowMedicineWhenPatientSettingsMissing;
            }

            return MedicalCareUtility.AllowsMedicine(patient.playerSettings.medCare, medicineDef);
        }

        private float GetMedicinePotency(ThingDef medicineDef)
        {
            return medicineDef != null && StatDefOf.MedicalPotency != null
                ? medicineDef.GetStatValueAbstract(StatDefOf.MedicalPotency)
                : 0f;
        }

        private string BuildSourceLabel(CargoStackRef stackRef)
        {
            return "CT_Shuttle_MedicalBay_LoadedCargoMedicine".Translate().ToString() +
                " " + ((stackRef != null ? stackRef.SourceIndex : -1) + 1).ToString();
        }

        private int CompareMedicine(
            ShuttleMedicineSupplyReadModel left,
            ShuttleMedicineSupplyReadModel right)
        {
            if (left == null || right == null)
            {
                return left == right ? 0 : (left == null ? 1 : -1);
            }

            int allowedCompare = right.AllowedByPatientMedCare.CompareTo(
                left.AllowedByPatientMedCare);
            if (allowedCompare != 0)
            {
                return allowedCompare;
            }

            int potencyCompare = right.Potency.CompareTo(left.Potency);
            if (potencyCompare != 0)
            {
                return potencyCompare;
            }

            int labelCompare = string.Compare(
                left.Label,
                right.Label,
                StringComparison.OrdinalIgnoreCase);
            return labelCompare != 0
                ? labelCompare
                : left.SourceThingID.CompareTo(right.SourceThingID);
        }

        private int CompareCandidates(
            CargoMedicineCandidate left,
            CargoMedicineCandidate right)
        {
            if (left == null || right == null)
            {
                return left == right ? 0 : (left == null ? 1 : -1);
            }

            int potencyCompare = this.GetMedicinePotency(right.MedicineDef).CompareTo(
                this.GetMedicinePotency(left.MedicineDef));
            if (potencyCompare != 0)
            {
                return potencyCompare;
            }

            string leftLabel = left.MedicineDef != null
                ? left.MedicineDef.LabelCap.ToString()
                : string.Empty;
            string rightLabel = right.MedicineDef != null
                ? right.MedicineDef.LabelCap.ToString()
                : string.Empty;
            int labelCompare = string.Compare(
                leftLabel,
                rightLabel,
                StringComparison.OrdinalIgnoreCase);
            return labelCompare != 0
                ? labelCompare
                : left.StackRef.ThingIDNumber.CompareTo(right.StackRef.ThingIDNumber);
        }

        private sealed class CargoMedicineCandidate
        {
            internal CargoMedicineCandidate(
                CargoStackRef stackRef,
                ThingDef medicineDef,
                string sourceLabel)
            {
                this.StackRef = stackRef;
                this.MedicineDef = medicineDef;
                this.SourceLabel = sourceLabel;
            }

            internal CargoStackRef StackRef { get; private set; }

            internal ThingDef MedicineDef { get; private set; }

            internal string SourceLabel { get; private set; }
        }

        private sealed class LoadedCargoMedicineWithdrawal : IShuttleMedicalSupplyWithdrawal
        {
            private readonly ShuttleCargoWithdrawal cargoWithdrawal;
            private readonly string sourceLabel;
            private readonly int sourceThingID;
            private readonly int sourceStackCountBefore;

            internal LoadedCargoMedicineWithdrawal(
                ShuttleCargoWithdrawal cargoWithdrawal,
                string sourceLabel,
                int sourceThingID,
                int sourceStackCountBefore)
            {
                this.cargoWithdrawal = cargoWithdrawal;
                this.sourceLabel = sourceLabel;
                this.sourceThingID = sourceThingID;
                this.sourceStackCountBefore = sourceStackCountBefore;
                Thing thing = cargoWithdrawal != null ? cargoWithdrawal.Thing : null;
                this.TakenThingID = thing != null ? thing.thingIDNumber : -1;
            }

            public Medicine Medicine
            {
                get
                {
                    return this.cargoWithdrawal != null
                        ? this.cargoWithdrawal.Thing as Medicine
                        : null;
                }
            }

            public string MedicineDefName
            {
                get
                {
                    Medicine medicine = this.Medicine;
                    return medicine != null && medicine.def != null
                        ? medicine.def.defName
                        : "null";
                }
            }

            public string SourceLabel
            {
                get { return this.sourceLabel ?? string.Empty; }
            }

            public int SourceThingID
            {
                get { return this.sourceThingID; }
            }

            public int TakenThingID { get; private set; }

            public int SourceStackCountBefore
            {
                get { return this.sourceStackCountBefore; }
            }

            public int SourceStackCountCurrent
            {
                get
                {
                    return this.cargoWithdrawal != null
                        ? this.cargoWithdrawal.CountOriginalSourceStack()
                        : -1;
                }
            }

            public void CommitConsumed()
            {
                if (this.cargoWithdrawal != null)
                {
                    this.cargoWithdrawal.CommitReleased();
                }
            }

            public bool RollbackOrDrop(
                Pawn doctor,
                ThingWithComps shuttleHost,
                out string notice)
            {
                notice = null;
                if (this.cargoWithdrawal == null)
                {
                    return false;
                }

                Medicine medicine = this.Medicine;
                if (medicine == null || medicine.Destroyed || medicine.stackCount <= 0)
                {
                    this.cargoWithdrawal.CommitAlreadyConsumed();
                    return true;
                }

                string failureReason;
                if (this.cargoWithdrawal.TryRollBackToSource(out failureReason))
                {
                    notice = "CT_Shuttle_MedicalBay_LoadedCargoMedicineReturned"
                        .Translate()
                        .ToString();
                    return true;
                }

                ThingOwner doctorInventory = doctor != null && doctor.inventory != null
                    ? doctor.inventory.innerContainer
                    : null;
                if (doctorInventory != null &&
                    this.cargoWithdrawal.TryRecoverToOwner(
                        doctorInventory,
                        out failureReason))
                {
                    notice = "CT_Shuttle_MedicalBay_MedicineReturned".Translate().ToString();
                    return true;
                }

                if (shuttleHost != null &&
                    this.cargoWithdrawal.TryRecoverNear(
                        shuttleHost,
                        out failureReason))
                {
                    notice = "CT_Shuttle_MedicalBay_MedicineDroppedOnFailure"
                        .Translate()
                        .ToString();
                    return true;
                }

                if (doctor != null &&
                    this.cargoWithdrawal.TryRecoverNear(
                        doctor,
                        out failureReason))
                {
                    notice = "CT_Shuttle_MedicalBay_MedicineDroppedOnFailure"
                        .Translate()
                        .ToString();
                    return true;
                }

                if (Prefs.DevMode)
                {
                    Log.Warning(
                        "[CeleTech Shuttle] Medical Bay loaded cargo medicine rollback failed; " +
                        "medicine remains in current state. def=" + this.MedicineDefName +
                        " sourceThingID=" + this.sourceThingID +
                        " takenThingID=" + this.TakenThingID +
                        " failure=" + (failureReason ?? "null"));
                }

                return false;
            }
        }
    }
}

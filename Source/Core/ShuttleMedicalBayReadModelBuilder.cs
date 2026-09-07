using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.MechCharging;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    /// <summary>
    /// Builds a read-only Medical Bay UI snapshot from profile capability and occupancy state.
    /// Health and need values are read for display only.
    /// </summary>
    internal sealed class ShuttleMedicalBayReadModelBuilder
    {
        private const int MaxAdmissionCandidateReadModels = 48;
        private const int MaxAdmissionCarrierReadModels = 16;
        private const float SevereBleedRateThreshold = 0.30f;
        private readonly MedicalBayResourceReadModelBuilder resourceBuilder;
        private readonly MedicalBayPatientSnapshotBuilder patientSnapshotBuilder;
        private readonly MedicalBayTreatmentStatusFormatter treatmentStatusFormatter =
            new MedicalBayTreatmentStatusFormatter();

        internal ShuttleMedicalBayReadModelBuilder()
        {
            this.resourceBuilder = new MedicalBayResourceReadModelBuilder();
            this.patientSnapshotBuilder = new MedicalBayPatientSnapshotBuilder(this.resourceBuilder);
        }

        internal ShuttleMedicalBayReadModel Build(
            ShuttleProfile profile,
            ThingWithComps shuttleHost,
            ShuttleRuntimeState runtimeState = null,
            bool includeActionDetails = true)
        {
            ShuttleMedicalBayReadModel model = new ShuttleMedicalBayReadModel();
            MedicalBayProfile medicalBay = profile != null ? profile.MedicalBay : null;
            if (medicalBay != null)
            {
                model.HasMedicalBay = medicalBay.HasMedicalBay;
                model.MedicalPatientSlots = medicalBay.HasMedicalBay ? medicalBay.MedicalPatientSlots : 0;
                model.SupportsPassiveComfort = medicalBay.SupportsPassiveComfort;
                model.PassiveJoyCapPct = medicalBay.SupportsPassiveComfort
                    ? medicalBay.PassiveJoyCapPct
                    : -1f;
            }

            model.MedicalBayPoweredKnown = shuttleHost != null;
            model.MedicalBayPowered = model.HasMedicalBay &&
                model.MedicalBayPoweredKnown &&
                MedicalBayAdmissionValidator.IsMedicalBayPowered(shuttleHost);

            IReadOnlyList<ShuttleMedicineSupplyReadModel> loadedCargoMedicine =
                this.resourceBuilder.ListLoadedCargoMedicine(shuttleHost, null);
            this.resourceBuilder.ApplyLoadedCargoMedicineSummary(model, loadedCargoMedicine, 0);

            CompShuttleMedicalBayOccupancy occupancy = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>()
                : null;
            if (occupancy == null)
            {
                model.Patients = new List<ShuttleMedicalPatientReadModel>();
                this.treatmentStatusFormatter.ApplyActiveProcedure(model, shuttleHost, runtimeState, null);
                return model;
            }

            model.PatientCount = occupancy.PatientCount;
            model.FreePatientSlots = occupancy.FreePatientSlots;
            model.HasPatients = occupancy.HasPatients;

            List<ShuttleMedicalPatientReadModel> patients =
                new List<ShuttleMedicalPatientReadModel>();
            IReadOnlyList<ShuttleMedicalPatientSnapshot> snapshots =
                occupancy.BuildPatientSnapshotsForReading();
            bool medicalBayPowered = shuttleHost != null &&
                MedicalBayAdmissionValidator.IsMedicalBayPowered(shuttleHost);
            HashSet<int> anyPatientAllowedMedicineThingIds = new HashSet<int>();
            for (int i = 0; i < snapshots.Count; i++)
            {
                ShuttleMedicalPatientSnapshot snapshot = snapshots[i];
                if (snapshot != null)
                {
                    ShuttleMedicalPatientReadModel patientModel = this.patientSnapshotBuilder.BuildPatientModel(
                        snapshot,
                        medicalBay,
                        medicalBayPowered,
                        shuttleHost,
                        includeActionDetails);
                    this.resourceBuilder.TrackPatientAllowedCargoMedicine(
                        patientModel,
                        anyPatientAllowedMedicineThingIds);
                    patients.Add(patientModel);
                }
            }

            model.Patients = patients;
            model.LoadedCargoMedicineAllowedCount = this.resourceBuilder.CountAllowedMedicineByThingIds(
                loadedCargoMedicine,
                anyPatientAllowedMedicineThingIds);
            this.treatmentStatusFormatter.ApplyActiveProcedure(model, shuttleHost, runtimeState, occupancy);
            model.AdmissionCandidates = includeActionDetails
                ? this.BuildAdmissionCandidates(
                    shuttleHost,
                    medicalBay,
                    occupancy,
                    patients)
                : new List<ShuttleMedicalAdmissionCandidateReadModel>();
            return model;
        }

        private IReadOnlyList<ShuttleMedicalAdmissionCandidateReadModel> BuildAdmissionCandidates(
            ThingWithComps shuttleHost,
            MedicalBayProfile medicalBay,
            CompShuttleMedicalBayOccupancy occupancy,
            IReadOnlyList<ShuttleMedicalPatientReadModel> heldPatients)
        {
            List<ShuttleMedicalAdmissionCandidateReadModel> candidates =
                new List<ShuttleMedicalAdmissionCandidateReadModel>();
            HashSet<int> seenPawnIds = new HashSet<int>();
            int currentAdmissionUsers =
                MedicalBayAdmissionValidator.CountCurrentMedicalBayAdmissionUsers(shuttleHost);

            if (heldPatients != null)
            {
                for (int i = 0; i < heldPatients.Count; i++)
                {
                    ShuttleMedicalPatientReadModel patient = heldPatients[i];
                    ShuttleMedicalAdmissionCandidateReadModel candidate =
                        this.BuildAdmissionCandidateFromHeldPatient(patient);
                    if (candidate == null || !seenPawnIds.Add(candidate.PawnThingID))
                    {
                        continue;
                    }

                    candidates.Add(candidate);
                }
            }

            this.AddMapAdmissionCandidates(
                candidates,
                seenPawnIds,
                shuttleHost,
                medicalBay,
                occupancy,
                currentAdmissionUsers);
            this.AddHabitatAdmissionCandidates(
                candidates,
                seenPawnIds,
                shuttleHost,
                medicalBay,
                occupancy,
                currentAdmissionUsers);
            candidates.Sort(this.CompareAdmissionCandidates);
            if (candidates.Count > MaxAdmissionCandidateReadModels)
            {
                candidates.RemoveRange(MaxAdmissionCandidateReadModels, candidates.Count - MaxAdmissionCandidateReadModels);
            }

            return candidates;
        }

        private void AddMapAdmissionCandidates(
            List<ShuttleMedicalAdmissionCandidateReadModel> candidates,
            HashSet<int> seenPawnIds,
            ThingWithComps shuttleHost,
            MedicalBayProfile medicalBay,
            CompShuttleMedicalBayOccupancy occupancy,
            int currentAdmissionUsers)
        {
            if (candidates == null ||
                seenPawnIds == null ||
                shuttleHost == null ||
                shuttleHost.Map == null ||
                shuttleHost.Map.mapPawns == null)
            {
                return;
            }

            IReadOnlyList<Pawn> pawns = shuttleHost.Map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!this.IsAdmissionCandidatePawn(pawn) || !seenPawnIds.Add(pawn.thingIDNumber))
                {
                    continue;
                }

                ShuttleMedicalAdmissionCandidateReadModel candidate =
                    this.BuildAdmissionCandidateFromPawn(
                        pawn,
                        shuttleHost,
                        medicalBay,
                        occupancy,
                        false,
                        currentAdmissionUsers);
                if (candidate != null)
                {
                    candidates.Add(candidate);
                }
            }
        }

        private void AddHabitatAdmissionCandidates(
            List<ShuttleMedicalAdmissionCandidateReadModel> candidates,
            HashSet<int> seenPawnIds,
            ThingWithComps shuttleHost,
            MedicalBayProfile medicalBay,
            CompShuttleMedicalBayOccupancy occupancy,
            int currentAdmissionUsers)
        {
            if (candidates == null || seenPawnIds == null || shuttleHost == null)
            {
                return;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>();
            List<Pawn> occupants = habitat != null ? habitat.OccupantsForReading : null;
            if (occupants == null)
            {
                return;
            }

            for (int i = 0; i < occupants.Count; i++)
            {
                Pawn pawn = occupants[i];
                if (!this.IsAdmissionCandidatePawn(pawn) || !seenPawnIds.Add(pawn.thingIDNumber))
                {
                    continue;
                }

                ShuttleMedicalAdmissionCandidateReadModel candidate =
                    this.BuildAdmissionCandidateFromPawn(
                        pawn,
                        shuttleHost,
                        medicalBay,
                        occupancy,
                        true,
                        currentAdmissionUsers);
                if (candidate != null)
                {
                    candidates.Add(candidate);
                }
            }
        }

        private ShuttleMedicalAdmissionCandidateReadModel BuildAdmissionCandidateFromHeldPatient(
            ShuttleMedicalPatientReadModel patient)
        {
            if (patient == null)
            {
                return null;
            }

            Pawn pawn = patient.DisplayThing as Pawn;
            int pawnThingId = patient.PawnThingID > 0
                ? patient.PawnThingID
                : pawn != null ? pawn.thingIDNumber : 0;
            if (pawnThingId <= 0)
            {
                return null;
            }

            string kindKey = this.GetAdmissionKindKey(pawn);
            string triageKey = this.GetAdmissionTriageKey(patient, pawn);
            ShuttleMedicalAdmissionCandidateReadModel candidate =
                this.CreateAdmissionCandidateShell(pawn, pawnThingId, patient.PawnLabel, kindKey, triageKey);
            candidate.DisplayThing = patient.DisplayThing;
            candidate.AdmissionModeKey = "NotReceivable";
            candidate.ReasonText =
                "CT_Shuttle_Medical_Admission_AlreadyInBay".Translate().ToString();
            candidate.IsAlreadyInMedicalBay = true;
            this.FinalizeAdmissionCandidate(candidate, false, false);
            return candidate;
        }

        private ShuttleMedicalAdmissionCandidateReadModel BuildAdmissionCandidateFromPawn(
            Pawn pawn,
            ThingWithComps shuttleHost,
            MedicalBayProfile medicalBay,
            CompShuttleMedicalBayOccupancy occupancy,
            bool insideOtherShuttleHolder,
            int currentAdmissionUsers)
        {
            if (pawn == null || pawn.thingIDNumber <= 0)
            {
                return null;
            }

            string kindKey = this.GetAdmissionKindKey(pawn);
            string triageKey = this.GetAdmissionTriageKey(pawn);
            ShuttleMedicalAdmissionCandidateReadModel candidate =
                this.CreateAdmissionCandidateShell(
                    pawn,
                    pawn.thingIDNumber,
                    pawn.LabelShortCap,
                    kindKey,
                    triageKey);

            if (occupancy != null && occupancy.ContainsPatient(pawn))
            {
                candidate.AdmissionModeKey = "NotReceivable";
                candidate.ReasonText =
                    "CT_Shuttle_Medical_Admission_AlreadyInBay".Translate().ToString();
                candidate.IsAlreadyInMedicalBay = true;
                this.FinalizeAdmissionCandidate(candidate, false, false);
                return candidate;
            }

            string bayBlockedReason;
            if (!this.CanUseMedicalBayForAdmissionDisplay(
                shuttleHost,
                medicalBay,
                occupancy,
                currentAdmissionUsers,
                out bayBlockedReason))
            {
                candidate.AdmissionModeKey = "NotReceivable";
                candidate.ReasonText = bayBlockedReason;
                this.FinalizeAdmissionCandidate(candidate, false, false);
                return candidate;
            }

            if (insideOtherShuttleHolder)
            {
                candidate.AdmissionModeKey = "Pending";
                candidate.ReasonText =
                    "CT_Shuttle_Medical_Admission_OtherCompartmentUnavailable".Translate().ToString();
                this.FinalizeAdmissionCandidate(candidate, false, false);
                return candidate;
            }

            string carryReason;
            if (this.PatientNeedsCarryForAdmissionDisplay(pawn, out carryReason))
            {
                candidate.AdmissionModeKey = "NeedsCarry";
                candidate.ReasonText = carryReason;
                candidate.CarryCandidates =
                    this.BuildCarryAdmissionCarrierCandidates(shuttleHost, pawn);
                this.FinalizeAdmissionCandidate(candidate, true, true);
                return candidate;
            }

            string failReason;
            if (this.CanSelfEnterForAdmissionDisplay(pawn, shuttleHost, out failReason))
            {
                candidate.AdmissionModeKey = "SelfEnter";
                candidate.ReasonText =
                    "CT_Shuttle_Medical_Admission_CanSelfEnter".Translate().ToString();
                this.FinalizeAdmissionCandidate(candidate, true, false);
                return candidate;
            }

            candidate.AdmissionModeKey = "NotReceivable";
            candidate.ReasonText = !string.IsNullOrEmpty(failReason)
                ? failReason
                : "CT_Shuttle_Medical_AdmitUnavailable".Translate().ToString();
            this.FinalizeAdmissionCandidate(candidate, false, false);
            return candidate;
        }

        private ShuttleMedicalAdmissionCandidateReadModel CreateAdmissionCandidateShell(
            Pawn pawn,
            int pawnThingId,
            string label,
            string kindKey,
            string triageKey)
        {
            ShuttleMedicalAdmissionCandidateReadModel candidate =
                new ShuttleMedicalAdmissionCandidateReadModel();
            candidate.PawnThingID = pawnThingId;
            candidate.Id = pawnThingId > 0
                ? pawnThingId.ToString()
                : (!string.IsNullOrEmpty(label) ? label : "candidate");
            candidate.Label = !string.IsNullOrEmpty(label)
                ? label
                : "CT_Shuttle_Medical_UnknownCandidate".Translate().ToString();
            candidate.DisplayThing = pawn;
            candidate.KindKey = !string.IsNullOrEmpty(kindKey) ? kindKey : "Human";
            candidate.KindLabel = this.GetAdmissionKindLabel(candidate.KindKey);
            candidate.TriageKey = !string.IsNullOrEmpty(triageKey) ? triageKey : "Stable";
            candidate.TriageLabel = this.GetAdmissionTriageLabel(candidate.TriageKey);
            return candidate;
        }

        private void FinalizeAdmissionCandidate(
            ShuttleMedicalAdmissionCandidateReadModel candidate,
            bool canAdmit,
            bool needsCarry)
        {
            if (candidate == null)
            {
                return;
            }

            candidate.CanAdmit = canAdmit;
            candidate.NeedsCarry = needsCarry;
            candidate.AdmissionModeLabel = this.GetAdmissionModeLabel(candidate.AdmissionModeKey);
            candidate.Tooltip = candidate.Label + "\n" +
                candidate.KindLabel + " / " + candidate.TriageLabel + "\n" +
                candidate.AdmissionModeLabel + "\n" +
                candidate.ReasonText + "\n" +
                "CT_Shuttle_Medical_Admission_CandidateActionTooltip".Translate().ToString();
        }

        private IReadOnlyList<ShuttleMedicalAdmissionCarrierReadModel> BuildCarryAdmissionCarrierCandidates(
            ThingWithComps shuttleHost,
            Pawn patient)
        {
            List<ShuttleMedicalAdmissionCarrierReadModel> carriers =
                new List<ShuttleMedicalAdmissionCarrierReadModel>();
            if (shuttleHost == null ||
                shuttleHost.Map == null ||
                shuttleHost.Map.mapPawns == null ||
                patient == null)
            {
                return carriers;
            }

            IReadOnlyList<Pawn> pawns = shuttleHost.Map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return carriers;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn carrierPawn = pawns[i];
                if (!this.IsAdmissionCarrierCandidatePawn(carrierPawn, patient))
                {
                    continue;
                }

                string failReason;
                bool canCarry = this.CanUseCarrierForAdmissionDisplay(
                    carrierPawn,
                    patient,
                    shuttleHost,
                    out failReason);
                ShuttleMedicalAdmissionCarrierReadModel carrier =
                    this.BuildAdmissionCarrierModel(carrierPawn, canCarry, failReason);
                carriers.Add(carrier);
            }

            carriers.Sort(this.CompareAdmissionCarriers);
            if (carriers.Count > MaxAdmissionCarrierReadModels)
            {
                carriers.RemoveRange(MaxAdmissionCarrierReadModels, carriers.Count - MaxAdmissionCarrierReadModels);
            }

            return carriers;
        }

        private ShuttleMedicalAdmissionCarrierReadModel BuildAdmissionCarrierModel(
            Pawn carrierPawn,
            bool canCarry,
            string failReason)
        {
            ShuttleMedicalAdmissionCarrierReadModel carrier =
                new ShuttleMedicalAdmissionCarrierReadModel();
            carrier.PawnThingID = carrierPawn != null ? carrierPawn.thingIDNumber : 0;
            carrier.Id = carrier.PawnThingID > 0 ? carrier.PawnThingID.ToString() : "carrier";
            carrier.Label = carrierPawn != null
                ? carrierPawn.LabelShortCap
                : "CT_Shuttle_Medical_UnknownCarrier".Translate().ToString();
            carrier.DisplayThing = carrierPawn;
            carrier.CanCarry = canCarry;
            carrier.StatusText = canCarry
                ? "CT_Shuttle_Medical_CanCarry".Translate().ToString()
                : "CT_Shuttle_Medical_Unavailable".Translate().ToString();
            carrier.ReasonText = canCarry
                ? "CT_Shuttle_Medical_Admission_CarrierAvailable".Translate().ToString()
                : (!string.IsNullOrEmpty(failReason)
                    ? failReason
                    : "CT_Shuttle_Medical_AdmitCarryUnavailable".Translate().ToString());
            carrier.Tooltip = carrier.Label + "\n" +
                carrier.StatusText + "\n" +
                carrier.ReasonText + "\n" +
                "CT_Shuttle_Medical_Admission_CarrierActionTooltip".Translate().ToString();
            return carrier;
        }

        private bool IsAdmissionCarrierCandidatePawn(Pawn carrierPawn, Pawn patient)
        {
            if (carrierPawn == null ||
                carrierPawn.Destroyed ||
                carrierPawn.Dead ||
                carrierPawn.thingIDNumber <= 0 ||
                carrierPawn == patient ||
                carrierPawn.RaceProps == null ||
                !carrierPawn.RaceProps.Humanlike)
            {
                return false;
            }

            return carrierPawn.Faction == Faction.OfPlayer && carrierPawn.IsColonist;
        }

        private int CompareAdmissionCarriers(
            ShuttleMedicalAdmissionCarrierReadModel left,
            ShuttleMedicalAdmissionCarrierReadModel right)
        {
            bool leftCanCarry = left != null && left.CanCarry;
            bool rightCanCarry = right != null && right.CanCarry;
            if (leftCanCarry != rightCanCarry)
            {
                return leftCanCarry ? -1 : 1;
            }

            string leftLabel = left != null ? left.Label : string.Empty;
            string rightLabel = right != null ? right.Label : string.Empty;
            return string.Compare(leftLabel, rightLabel, StringComparison.OrdinalIgnoreCase);
        }

        private bool CanUseMedicalBayForAdmissionDisplay(
            ThingWithComps shuttleHost,
            MedicalBayProfile medicalBay,
            CompShuttleMedicalBayOccupancy occupancy,
            int currentAdmissionUsers,
            out string reason)
        {
            reason = null;
            if (shuttleHost == null ||
                !shuttleHost.Spawned ||
                shuttleHost.Map == null ||
                shuttleHost.Faction != Faction.OfPlayer)
            {
                reason = "CT_Shuttle_Medical_StateUnavailableCannotAdmit".Translate().ToString();
                return false;
            }

            if (medicalBay == null || !medicalBay.HasMedicalBay)
            {
                reason = "CT_Shuttle_Medical_NotInstalled".Translate().ToString();
                return false;
            }

            if (!MedicalBayAdmissionValidator.IsMedicalBayPowered(shuttleHost))
            {
                reason = "CT_Shuttle_Medical_Unpowered".Translate().ToString();
                return false;
            }

            if (medicalBay.MedicalPatientSlots <= 0)
            {
                reason = "CT_Shuttle_Medical_Admission_NoBeds".Translate().ToString();
                return false;
            }

            if (occupancy == null)
            {
                reason = "CT_Shuttle_Medical_Admission_StatusUnavailable".Translate().ToString();
                return false;
            }

            if (currentAdmissionUsers >= medicalBay.MedicalPatientSlots)
            {
                reason = "CT_Shuttle_Medical_Admission_BayFull".Translate().ToString();
                return false;
            }

            return true;
        }

        private bool CanSelfEnterForAdmissionDisplay(
            Pawn patient,
            ThingWithComps shuttleHost,
            out string failReason)
        {
            failReason = null;
            if (patient == null ||
                patient.Destroyed ||
                patient.Dead ||
                !patient.Spawned ||
                patient.Map == null ||
                shuttleHost == null ||
                patient.Map != shuttleHost.Map ||
                patient.Downed ||
                patient.Drafted ||
                patient.MentalState != null ||
                this.GetCapacityPct(patient, PawnCapacityDefOf.Moving) <= 0.01f)
            {
                failReason = "CT_Shuttle_MedicalBay_CannotSelfAdmitUnavailable".Translate().ToString();
                return false;
            }

            return true;
        }

        private bool CanUseCarrierForAdmissionDisplay(
            Pawn carrier,
            Pawn patient,
            ThingWithComps shuttleHost,
            out string failReason)
        {
            failReason = null;
            if (!this.IsAdmissionCarrierCandidatePawn(carrier, patient) ||
                carrier.Destroyed ||
                carrier.Dead ||
                !carrier.Spawned ||
                carrier.Map == null ||
                shuttleHost == null ||
                carrier.Map != shuttleHost.Map ||
                carrier.Downed ||
                carrier.Drafted ||
                carrier.MentalState != null ||
                carrier.carryTracker == null ||
                this.GetCapacityPct(carrier, PawnCapacityDefOf.Moving) <= 0.01f ||
                this.GetCapacityPct(carrier, PawnCapacityDefOf.Manipulation) <= 0.01f)
            {
                failReason = "CT_Shuttle_MedicalBay_CarrierUnavailable".Translate().ToString();
                return false;
            }

            Thing carriedThing = carrier.carryTracker.CarriedThing;
            if (carriedThing != null && carriedThing != patient)
            {
                failReason = "CT_Shuttle_MedicalBay_CarrierUnavailable".Translate().ToString();
                return false;
            }

            // Exact path and reservation checks are intentionally deferred to the
            // command/job boundary for the player-selected carrier/patient pair.
            return true;
        }

        private bool PatientNeedsCarryForAdmissionDisplay(Pawn pawn, out string reason)
        {
            reason = null;
            if (pawn == null)
            {
                reason = "CT_Shuttle_Medical_AdmitCandidatePending".Translate().ToString();
                return false;
            }

            if (pawn.Downed)
            {
                reason = "CT_Shuttle_Medical_Admission_CarryReason_Downed".Translate().ToString();
                return true;
            }

            float consciousness = this.GetCapacityPct(pawn, PawnCapacityDefOf.Consciousness);
            if (consciousness >= 0f && consciousness <= 0.05f)
            {
                reason =
                    "CT_Shuttle_Medical_Admission_CarryReason_LowConsciousness".Translate().ToString();
                return true;
            }

            float moving = this.GetCapacityPct(pawn, PawnCapacityDefOf.Moving);
            if (moving >= 0f && moving <= 0.01f)
            {
                reason =
                    "CT_Shuttle_Medical_Admission_CarryReason_LowMobility".Translate().ToString();
                return true;
            }

            if (pawn.RaceProps != null && pawn.RaceProps.IsMechanoid)
            {
                float energyPct = this.GetMechEnergyPct(pawn);
                if (energyPct >= 0f && energyPct <= 0.01f)
                {
                    reason =
                        "CT_Shuttle_Medical_Admission_CarryReason_LowMechEnergy".Translate().ToString();
                    return true;
                }
            }

            if (MedicalBayAdmissionValidator.PatientRequiresCarriedAdmission(pawn))
            {
                reason =
                    "CT_Shuttle_Medical_Admission_CarryReason_CannotSelfEnter".Translate().ToString();
                return true;
            }

            return false;
        }

        private bool IsAdmissionCandidatePawn(Pawn pawn)
        {
            string unusedFailureReason;
            return pawn != null &&
                pawn.thingIDNumber > 0 &&
                MedicalBayPatientAccessPolicy.CanReceiveCare(
                    pawn,
                    out unusedFailureReason);
        }

        private string GetAdmissionKindKey(Pawn pawn)
        {
            if (pawn == null || pawn.RaceProps == null)
            {
                return "Human";
            }

            if (pawn.RaceProps.IsMechanoid)
            {
                return "Mechanoid";
            }

            if (pawn.RaceProps.Animal)
            {
                return "Animal";
            }

            if (pawn.IsPrisonerOfColony)
            {
                return "Prisoner";
            }

            if (pawn.IsSlaveOfColony)
            {
                return "Slave";
            }

            if (pawn.IsColonist)
            {
                return "Colonist";
            }

            if ((pawn.guest != null && pawn.guest.HostFaction == Faction.OfPlayer) ||
                pawn.IsQuestLodger())
            {
                return "Visitor";
            }

            if (pawn.Faction != null &&
                pawn.Faction.RelationKindWith(Faction.OfPlayer) == FactionRelationKind.Ally)
            {
                return "Ally";
            }

            return "Human";
        }

        private string GetAdmissionKindLabel(string kindKey)
        {
            if (kindKey == "Prisoner")
            {
                return "CT_Shuttle_Crew_Prisoner".Translate().ToString();
            }

            if (kindKey == "Slave")
            {
                return "CT_Shuttle_Crew_Slave".Translate().ToString();
            }

            if (kindKey == "Colonist")
            {
                return "CT_Shuttle_Crew_Colonist".Translate().ToString();
            }

            if (kindKey == "Visitor")
            {
                return "CT_Shuttle_Medical_Kind_Visitor".Translate().ToString();
            }

            if (kindKey == "Ally")
            {
                return "CT_Shuttle_Medical_Kind_Ally".Translate().ToString();
            }

            if (kindKey == "Mechanoid")
            {
                return "CT_Shuttle_Crew_Mechanoid".Translate().ToString();
            }

            if (kindKey == "Animal")
            {
                return "CT_Shuttle_Crew_Animal".Translate().ToString();
            }

            return "CT_Shuttle_Crew_Human".Translate().ToString();
        }

        private string GetAdmissionTriageKey(
            ShuttleMedicalPatientReadModel patient,
            Pawn pawn)
        {
            if (patient != null)
            {
                if (patient.HasMedicalEmergency &&
                    (patient.BleedRateTotal >= SevereBleedRateThreshold ||
                        (patient.ConsciousnessPct >= 0f && patient.ConsciousnessPct < 0.25f) ||
                        patient.PainPct >= 0.85f ||
                        (patient.IsDowned && patient.NeedsTend)))
                {
                    return "Critical";
                }

                if (patient.HasMedicalEmergency ||
                    patient.BleedRateTotal >= 0.15f ||
                    patient.UntendedHediffCount >= 2)
                {
                    return "Severe";
                }

                if (patient.NeedsTend ||
                    patient.IsBleeding ||
                    patient.HasInfectionLikeHediff ||
                    patient.PainPct >= 0.40f)
                {
                    return "Moderate";
                }
            }

            return this.GetAdmissionTriageKey(pawn);
        }

        private string GetAdmissionTriageKey(Pawn pawn)
        {
            if (pawn == null)
            {
                return "Stable";
            }

            float bleedRate = this.GetBleedRateTotal(pawn);
            float consciousness = this.GetCapacityPct(pawn, PawnCapacityDefOf.Consciousness);
            float pain = this.GetPainPct(pawn);
            float moving = this.GetCapacityPct(pawn, PawnCapacityDefOf.Moving);

            if (pawn.Downed ||
                bleedRate >= SevereBleedRateThreshold ||
                (consciousness >= 0f && consciousness < 0.25f) ||
                pain >= 0.85f)
            {
                return "Critical";
            }

            if (bleedRate >= 0.15f ||
                (consciousness >= 0f && consciousness < 0.45f) ||
                pain >= 0.65f ||
                this.HasMajorAdmissionInjury(pawn))
            {
                return "Severe";
            }

            if (bleedRate > 0f ||
                pain >= 0.40f ||
                (moving >= 0f && moving < 0.60f) ||
                this.HasUntendedTendableHediff(pawn))
            {
                return "Moderate";
            }

            return "Stable";
        }

        private string GetAdmissionTriageLabel(string triageKey)
        {
            if (triageKey == "Critical")
            {
                return "CT_Shuttle_Medical_Triage_Critical".Translate().ToString();
            }

            if (triageKey == "Severe")
            {
                return "CT_Shuttle_Medical_Triage_Severe".Translate().ToString();
            }

            if (triageKey == "Moderate")
            {
                return "CT_Shuttle_Medical_Triage_Observation".Translate().ToString();
            }

            return "CT_Shuttle_Medical_Triage_Stable".Translate().ToString();
        }

        private string GetAdmissionModeLabel(string modeKey)
        {
            if (modeKey == "SelfEnter")
            {
                return "CT_Shuttle_Medical_SelfEnter".Translate().ToString();
            }

            if (modeKey == "NeedsCarry")
            {
                return "CT_Shuttle_Medical_NeedsCarry".Translate().ToString();
            }

            if (modeKey == "NotReceivable")
            {
                return "CT_Shuttle_Medical_NotReceivable".Translate().ToString();
            }

            return "CT_Shuttle_Medical_AdmissionMode_Pending".Translate().ToString();
        }

        private bool HasMajorAdmissionInjury(Pawn pawn)
        {
            if (pawn == null ||
                pawn.health == null ||
                pawn.health.hediffSet == null ||
                pawn.health.hediffSet.hediffs == null)
            {
                return false;
            }

            for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
            {
                Hediff_Injury injury = pawn.health.hediffSet.hediffs[i] as Hediff_Injury;
                if (injury != null && injury.Severity >= 8f)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasUntendedTendableHediff(Pawn pawn)
        {
            if (pawn == null ||
                pawn.health == null ||
                pawn.health.hediffSet == null ||
                pawn.health.hediffSet.hediffs == null)
            {
                return false;
            }

            for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
            {
                Hediff hediff = pawn.health.hediffSet.hediffs[i];
                if (hediff != null && this.IsHediffTendable(hediff) && !this.IsHediffTended(hediff))
                {
                    return true;
                }
            }

            return false;
        }

        private int CompareAdmissionCandidates(
            ShuttleMedicalAdmissionCandidateReadModel left,
            ShuttleMedicalAdmissionCandidateReadModel right)
        {
            int leftPriority = this.GetAdmissionCandidatePriority(left);
            int rightPriority = this.GetAdmissionCandidatePriority(right);
            int priorityCompare = rightPriority.CompareTo(leftPriority);
            if (priorityCompare != 0)
            {
                return priorityCompare;
            }

            string leftLabel = left != null ? left.Label : string.Empty;
            string rightLabel = right != null ? right.Label : string.Empty;
            return string.Compare(leftLabel, rightLabel, StringComparison.OrdinalIgnoreCase);
        }

        private int GetAdmissionCandidatePriority(ShuttleMedicalAdmissionCandidateReadModel candidate)
        {
            if (candidate == null)
            {
                return 0;
            }

            int priority = 0;
            if (candidate.AdmissionModeKey == "NeedsCarry")
            {
                priority += 400;
            }
            else if (candidate.AdmissionModeKey == "SelfEnter")
            {
                priority += 300;
            }
            else if (candidate.AdmissionModeKey == "Pending")
            {
                priority += 100;
            }

            if (candidate.TriageKey == "Critical")
            {
                priority += 80;
            }
            else if (candidate.TriageKey == "Severe")
            {
                priority += 60;
            }
            else if (candidate.TriageKey == "Moderate")
            {
                priority += 30;
            }

            return priority;
        }

        private bool IsHediffTendable(Hediff hediff)
        {
            if (hediff == null)
            {
                return false;
            }

            return hediff.TendableNow(false);
        }

        private bool IsHediffTended(Hediff hediff)
        {
            HediffComp_TendDuration tendComp = this.GetTendDurationComp(hediff);
            return tendComp != null && tendComp.IsTended;
        }

        private HediffComp_TendDuration GetTendDurationComp(Hediff hediff)
        {
            HediffWithComps withComps = hediff as HediffWithComps;
            return withComps != null ? withComps.GetComp<HediffComp_TendDuration>() : null;
        }

        private float GetCapacityPct(Pawn pawn, PawnCapacityDef capacityDef)
        {
            if (pawn == null || pawn.health == null || pawn.health.capacities == null || capacityDef == null)
            {
                return -1f;
            }

            return pawn.health.capacities.GetLevel(capacityDef);
        }

        private float GetPainPct(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null)
            {
                return -1f;
            }

            return pawn.health.hediffSet.PainTotal;
        }

        private float GetBleedRateTotal(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null)
            {
                return 0f;
            }

            return pawn.health.hediffSet.BleedRateTotal;
        }

        private float GetMechEnergyPct(Pawn pawn)
        {
            return ShuttleMechChargeNeedUtility.GetChargeNeedPct(pawn);
        }
    }
}

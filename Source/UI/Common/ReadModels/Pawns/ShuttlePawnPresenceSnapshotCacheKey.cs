using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns
{
    internal sealed class ShuttlePawnPresenceSnapshotCacheKey
    {
        internal readonly int ProfileRevision;
        internal readonly int CargoSnapshotRevision;
        internal readonly int ControlFingerprint;
        internal readonly int CargoPawnFingerprint;

        private ShuttlePawnPresenceSnapshotCacheKey(
            int profileRevision,
            int cargoSnapshotRevision,
            int controlFingerprint,
            int cargoPawnFingerprint)
        {
            this.ProfileRevision = profileRevision;
            this.CargoSnapshotRevision = cargoSnapshotRevision;
            this.ControlFingerprint = controlFingerprint;
            this.CargoPawnFingerprint = cargoPawnFingerprint;
        }

        internal static ShuttlePawnPresenceSnapshotCacheKey Create(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot,
            int cargoSnapshotRevision)
        {
            return new ShuttlePawnPresenceSnapshotCacheKey(
                controlModel != null ? controlModel.ProfileRevision : 0,
                cargoSnapshotRevision,
                BuildControlFingerprint(controlModel),
                BuildCargoPawnFingerprint(cargoSnapshot));
        }

        internal bool Matches(ShuttlePawnPresenceSnapshotCacheKey other)
        {
            return other != null &&
                this.ProfileRevision == other.ProfileRevision &&
                this.CargoSnapshotRevision == other.CargoSnapshotRevision &&
                this.ControlFingerprint == other.ControlFingerprint &&
                this.CargoPawnFingerprint == other.CargoPawnFingerprint;
        }

        private static int BuildControlFingerprint(
            ShuttleControlReadModel controlModel)
        {
            unchecked
            {
                int hash = 17;
                if (controlModel == null)
                {
                    return hash;
                }

                hash = AddHash(hash, controlModel.ProfileRevision);
                hash = AddBoolHash(hash, controlModel.IsProfileDirty);
                hash = AddHabitatHash(hash, controlModel.Habitat);
                hash = AddMedicalBayHash(hash, controlModel.MedicalBay);
                hash = AddMechChargerHash(hash, controlModel.MechCharger);
                hash = AddPrisonCellHash(hash, controlModel.PrisonCell);
                return hash;
            }
        }

        private static int BuildCargoPawnFingerprint(
            ShuttleCargoSnapshot cargoSnapshot)
        {
            unchecked
            {
                int hash = 23;
                if (cargoSnapshot == null)
                {
                    return hash;
                }

                hash = AddBoolHash(hash, cargoSnapshot.HasTransporter);
                hash = AddHash(hash, cargoSnapshot.CargoRegionCount);
                hash = AddHash(
                    hash,
                    cargoSnapshot.Items != null ? cargoSnapshot.Items.Count : 0);

                for (int i = 0;
                    cargoSnapshot.Items != null && i < cargoSnapshot.Items.Count;
                    i++)
                {
                    hash = AddHash(hash, i);
                    hash = AddCargoItemHash(hash, cargoSnapshot.Items[i]);
                }

                return hash;
            }
        }

        private static int AddCargoItemHash(
            int hash,
            ShuttleCargoItemSnapshot item)
        {
            unchecked
            {
                if (item == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddStringHash(hash, item.Label);
                hash = AddStringHash(hash, item.DefName);
                hash = AddStringHash(hash, item.Category);
                hash = AddHash(hash, item.CargoRegionIndex);
                hash = AddHash(hash, item.TransporterIndex);
                hash = AddHash(hash, item.LoadedIndex);
                hash = AddHash(hash, item.QueueIndex);
                hash = AddHash(hash, item.ThingIDNumber);
                hash = AddHash(hash, item.StackCount);
                hash = AddFloatHash(hash, item.Mass);
                hash = AddBoolHash(hash, item.IsLoaded);
                hash = AddBoolHash(hash, item.IsAssignedToLoad);
                hash = AddBoolHash(hash, item.IsPawn);
                hash = AddThingHash(hash, item.DisplayThing);
                return hash;
            }
        }

        private static int AddHabitatHash(
            int hash,
            ShuttleHabitatReadModel habitat)
        {
            unchecked
            {
                if (habitat == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddBoolHash(hash, habitat.HasHabitat);
                hash = AddBoolHash(hash, habitat.SupportsSleep);
                hash = AddBoolHash(hash, habitat.SupportsDining);
                hash = AddHash(hash, habitat.SleepSlots);
                hash = AddHash(hash, habitat.DiningSlots);
                hash = AddBoolHash(hash, habitat.SupportsJoy);
                hash = AddHash(hash, habitat.JoySlots);
                hash = AddHash(hash, habitat.JoyOccupants);
                hash = AddHash(hash, habitat.SleepingOccupants);
                hash = AddHash(hash, habitat.DiningOccupants);
                hash = AddHash(hash, habitat.TotalOccupants);
                hash = AddBoolHash(hash, habitat.HasAnyOccupants);
                hash = AddBoolHash(hash, habitat.CanEjectOccupants);
                hash = AddHash(
                    hash,
                    habitat.Occupants != null ? habitat.Occupants.Count : 0);

                for (int i = 0;
                    habitat.Occupants != null && i < habitat.Occupants.Count;
                    i++)
                {
                    hash = AddHash(hash, i);
                    hash = AddHabitatOccupantHash(hash, habitat.Occupants[i]);
                }

                return hash;
            }
        }

        private static int AddHabitatOccupantHash(
            int hash,
            ShuttleHabitatOccupantReadModel occupant)
        {
            unchecked
            {
                if (occupant == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddHash(hash, occupant.PawnThingID);
                hash = AddStringHash(hash, occupant.LabelShort);
                hash = AddStringHash(hash, occupant.LabelCap);
                hash = AddThingHash(hash, occupant.DisplayThing);
                hash = AddHash(hash, (int)occupant.Activity);
                hash = AddStringHash(hash, occupant.ActivityLabelKey);
                hash = AddStringHash(hash, occupant.FoodLabel);
                hash = AddFloatHash(hash, occupant.FoodPct);
                hash = AddFloatHash(hash, occupant.RestPct);
                hash = AddFloatHash(hash, occupant.JoyPct);
                hash = AddFloatHash(hash, occupant.MoodPct);
                hash = AddHash(hash, (int)occupant.MostUrgentNeed);
                hash = AddStringHash(hash, occupant.MostUrgentNeedLabelKey);
                hash = AddFloatHash(hash, occupant.MostUrgentNeedPct);
                hash = AddBoolHash(hash, occupant.IsCritical);
                return hash;
            }
        }

        private static int AddMedicalBayHash(
            int hash,
            ShuttleMedicalBayReadModel medicalBay)
        {
            unchecked
            {
                if (medicalBay == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddBoolHash(hash, medicalBay.HasMedicalBay);
                hash = AddBoolHash(hash, medicalBay.MedicalBayPoweredKnown);
                hash = AddBoolHash(hash, medicalBay.MedicalBayPowered);
                hash = AddHash(hash, medicalBay.MedicalPatientSlots);
                hash = AddHash(hash, medicalBay.PatientCount);
                hash = AddHash(hash, medicalBay.FreePatientSlots);
                hash = AddBoolHash(hash, medicalBay.HasPatients);
                hash = AddBoolHash(hash, medicalBay.HasActiveProcedure);
                hash = AddStringHash(hash, medicalBay.ActiveProcedureType);
                hash = AddStringHash(hash, medicalBay.ActiveProcedureStatus);
                hash = AddHash(hash, medicalBay.ActiveProcedureID);
                hash = AddHash(hash, medicalBay.ActiveProcedureDoctorThingID);
                hash = AddHash(hash, medicalBay.ActiveProcedurePatientThingID);
                hash = AddStringHash(hash, medicalBay.ActiveProcedureDoctorLabel);
                hash = AddStringHash(hash, medicalBay.ActiveProcedurePatientLabel);
                hash = AddThingHash(hash, medicalBay.ActiveProcedureDoctorDisplayThing);
                hash = AddHash(hash, medicalBay.ActiveProcedureWorkTicksDone);
                hash = AddHash(hash, medicalBay.ActiveProcedureWorkTicksTotal);
                hash = AddFloatHash(hash, medicalBay.ActiveProcedureProgress);
                hash = AddHash(hash, medicalBay.ActiveProcedureTendCyclesCompleted);
                hash = AddHash(hash, medicalBay.ActiveProcedureLastKnownRemainingTendableCount);
                hash = AddStringHash(hash, medicalBay.ActiveProcedureStatusLabel);
                hash = AddStringHash(hash, medicalBay.ActiveProcedureTooltip);
                hash = AddMedicalPatientsHash(hash, medicalBay.Patients);
                hash = AddMedicalAdmissionCandidatesHash(
                    hash,
                    medicalBay.AdmissionCandidates);
                return hash;
            }
        }

        private static int AddMedicalPatientsHash(
            int hash,
            IReadOnlyList<ShuttleMedicalPatientReadModel> patients)
        {
            unchecked
            {
                hash = AddHash(hash, patients != null ? patients.Count : 0);
                for (int i = 0; patients != null && i < patients.Count; i++)
                {
                    hash = AddHash(hash, i);
                    hash = AddMedicalPatientHash(hash, patients[i]);
                }

                return hash;
            }
        }

        private static int AddMedicalPatientHash(
            int hash,
            ShuttleMedicalPatientReadModel patient)
        {
            unchecked
            {
                if (patient == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddHash(hash, patient.PawnThingID);
                hash = AddStringHash(hash, patient.PawnLabel);
                hash = AddThingHash(hash, patient.DisplayThing);
                hash = AddBoolHash(hash, patient.IsDowned);
                hash = AddBoolHash(hash, patient.IsConscious);
                hash = AddBoolHash(hash, patient.IsConsciousKnown);
                hash = AddFloatHash(hash, patient.ConsciousnessPct);
                hash = AddFloatHash(hash, patient.PainPct);
                hash = AddBoolHash(hash, patient.IsBleeding);
                hash = AddStringHash(hash, patient.BleedingLabel);
                hash = AddBoolHash(hash, patient.HasMedicalEmergency);
                hash = AddBoolHash(hash, patient.NeedsTend);
                hash = AddBoolHash(hash, patient.HasUntendedInjury);
                hash = AddBoolHash(hash, patient.HasTendedInjury);
                hash = AddHash(hash, patient.TendableHediffCount);
                hash = AddHash(hash, patient.UntendedHediffCount);
                hash = AddHash(hash, patient.TendedHediffCount);
                hash = AddHash(hash, patient.BleedingHediffCount);
                hash = AddFloatHash(hash, patient.BleedRateTotal);
                hash = AddBoolHash(hash, patient.HasInfectionLikeHediff);
                hash = AddStringHash(hash, patient.MedicalSummaryLabel);
                hash = AddStringHash(hash, patient.TendSummaryLabel);
                hash = AddStringHash(hash, patient.InfectionSummaryLabel);
                hash = AddStringHash(hash, patient.MedicineNeedLabel);
                hash = AddBoolHash(hash, patient.IsActiveProcedurePatient);
                hash = AddStringHash(hash, patient.ActiveProcedureStatusLabel);
                hash = AddStringHash(hash, patient.ActiveProcedureTooltip);
                hash = AddFloatHash(hash, patient.FoodPct);
                hash = AddFloatHash(hash, patient.RestPct);
                hash = AddFloatHash(hash, patient.JoyPct);
                hash = AddFloatHash(hash, patient.MoodPct);
                hash = AddBoolHash(hash, patient.HasMechDurability);
                hash = AddFloatHash(hash, patient.DurabilityPct);
                hash = AddFloatHash(hash, patient.DamagePct);
                hash = AddBoolHash(hash, patient.HasMechEnergy);
                hash = AddFloatHash(hash, patient.EnergyPct);
                hash = AddStringHash(hash, patient.AdmissionMode);
                hash = AddHash(hash, patient.AdmissionTick);
                hash = AddBoolHash(hash, patient.SupportsPassiveComfort);
                hash = AddBoolHash(hash, patient.PassiveComfortActive);
                hash = AddStringHash(hash, patient.PassiveComfortInactiveReason);
                hash = AddBoolHash(hash, patient.PassiveJoyActive);
                hash = AddStringHash(hash, patient.PassiveJoyInactiveReason);
                hash = AddBoolHash(hash, patient.PassiveMoodComfortActive);
                hash = AddStringHash(hash, patient.PassiveMoodComfortInactiveReason);
                hash = AddFloatHash(hash, patient.PassiveJoyCapPct);
                return hash;
            }
        }

        private static int AddMedicalAdmissionCandidatesHash(
            int hash,
            IReadOnlyList<ShuttleMedicalAdmissionCandidateReadModel> candidates)
        {
            unchecked
            {
                hash = AddHash(hash, candidates != null ? candidates.Count : 0);
                for (int i = 0; candidates != null && i < candidates.Count; i++)
                {
                    hash = AddHash(hash, i);
                    hash = AddMedicalAdmissionCandidateHash(hash, candidates[i]);
                }

                return hash;
            }
        }

        private static int AddMedicalAdmissionCandidateHash(
            int hash,
            ShuttleMedicalAdmissionCandidateReadModel candidate)
        {
            unchecked
            {
                if (candidate == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddHash(hash, candidate.PawnThingID);
                hash = AddStringHash(hash, candidate.Id);
                hash = AddStringHash(hash, candidate.Label);
                hash = AddThingHash(hash, candidate.DisplayThing);
                hash = AddStringHash(hash, candidate.KindKey);
                hash = AddStringHash(hash, candidate.KindLabel);
                hash = AddStringHash(hash, candidate.TriageKey);
                hash = AddStringHash(hash, candidate.TriageLabel);
                hash = AddStringHash(hash, candidate.AdmissionModeKey);
                hash = AddStringHash(hash, candidate.AdmissionModeLabel);
                hash = AddStringHash(hash, candidate.ReasonText);
                hash = AddBoolHash(hash, candidate.CanAdmit);
                hash = AddBoolHash(hash, candidate.NeedsCarry);
                hash = AddBoolHash(hash, candidate.IsAlreadyInMedicalBay);
                hash = AddStringHash(hash, candidate.Tooltip);
                hash = AddHash(
                    hash,
                    candidate.CarryCandidates != null
                        ? candidate.CarryCandidates.Count
                        : 0);

                for (int i = 0;
                    candidate.CarryCandidates != null &&
                    i < candidate.CarryCandidates.Count;
                    i++)
                {
                    hash = AddHash(hash, i);
                    hash = AddMedicalAdmissionCarrierHash(
                        hash,
                        candidate.CarryCandidates[i]);
                }

                return hash;
            }
        }

        private static int AddMedicalAdmissionCarrierHash(
            int hash,
            ShuttleMedicalAdmissionCarrierReadModel carrier)
        {
            unchecked
            {
                if (carrier == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddHash(hash, carrier.PawnThingID);
                hash = AddStringHash(hash, carrier.Id);
                hash = AddStringHash(hash, carrier.Label);
                hash = AddThingHash(hash, carrier.DisplayThing);
                hash = AddBoolHash(hash, carrier.CanCarry);
                hash = AddStringHash(hash, carrier.StatusText);
                hash = AddStringHash(hash, carrier.ReasonText);
                hash = AddStringHash(hash, carrier.Tooltip);
                return hash;
            }
        }

        private static int AddMechChargerHash(
            int hash,
            ShuttleMechChargerReadModel mechCharger)
        {
            unchecked
            {
                if (mechCharger == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddBoolHash(hash, mechCharger.HasMechCharger);
                hash = AddBoolHash(hash, mechCharger.MechChargerPoweredKnown);
                hash = AddBoolHash(hash, mechCharger.MechChargerPowered);
                hash = AddHash(hash, mechCharger.MechChargeSlots);
                hash = AddHash(hash, mechCharger.ChargingMechCount);
                hash = AddHash(hash, mechCharger.FreeChargingSlots);
                hash = AddBoolHash(hash, mechCharger.HasChargingMechs);
                hash = AddHash(
                    hash,
                    mechCharger.ChargingMechs != null
                        ? mechCharger.ChargingMechs.Count
                        : 0);

                for (int i = 0;
                    mechCharger.ChargingMechs != null &&
                    i < mechCharger.ChargingMechs.Count;
                    i++)
                {
                    hash = AddHash(hash, i);
                    hash = AddMechChargingPawnHash(
                        hash,
                        mechCharger.ChargingMechs[i]);
                }

                return hash;
            }
        }

        private static int AddMechChargingPawnHash(
            int hash,
            ShuttleMechChargingPawnReadModel chargingPawn)
        {
            unchecked
            {
                if (chargingPawn == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddHash(hash, chargingPawn.PawnThingID);
                hash = AddStringHash(hash, chargingPawn.PawnLabel);
                hash = AddThingHash(hash, chargingPawn.DisplayThing);
                hash = AddFloatHash(hash, chargingPawn.EnergyPct);
                return hash;
            }
        }

        private static int AddPrisonCellHash(
            int hash,
            ShuttlePrisonCellReadModel prisonCell)
        {
            unchecked
            {
                if (prisonCell == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddBoolHash(hash, prisonCell.HasPrisonCell);
                hash = AddHash(hash, prisonCell.PrisonerSlots);
                hash = AddHash(hash, prisonCell.PrisonerCount);
                hash = AddHash(hash, prisonCell.FreePrisonerSlots);
                hash = AddBoolHash(hash, prisonCell.CanAdmitMore);
                hash = AddStringHash(hash, prisonCell.UnavailableReason);
                hash = AddHeldPrisonersHash(hash, prisonCell.Prisoners);
                hash = AddPrisonCandidatesHash(hash, prisonCell.Candidates);
                hash = AddPrisonCarriersHash(hash, prisonCell.Carriers);
                hash = AddPrisonFeedersHash(hash, prisonCell.Feeders);
                hash = AddPrisonDoctorsHash(hash, prisonCell.Doctors);
                return hash;
            }
        }

        private static int AddHeldPrisonersHash(
            int hash,
            IReadOnlyList<ShuttleHeldPrisonerReadModel> prisoners)
        {
            unchecked
            {
                hash = AddHash(hash, prisoners != null ? prisoners.Count : 0);
                for (int i = 0; prisoners != null && i < prisoners.Count; i++)
                {
                    hash = AddHash(hash, i);
                    hash = AddHeldPrisonerHash(hash, prisoners[i]);
                }

                return hash;
            }
        }

        private static int AddHeldPrisonerHash(
            int hash,
            ShuttleHeldPrisonerReadModel prisoner)
        {
            unchecked
            {
                if (prisoner == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddHash(hash, prisoner.ThingIDNumber);
                hash = AddStringHash(hash, prisoner.Label);
                hash = AddStringHash(hash, prisoner.FactionLabel);
                hash = AddStringHash(hash, prisoner.HostFactionLabel);
                hash = AddStringHash(hash, prisoner.InteractionModeLabel);
                hash = AddFloatHash(hash, prisoner.FoodLevelPercent);
                hash = AddStringHash(hash, prisoner.FoodLabel);
                hash = AddStringHash(hash, prisoner.HealthLabel);
                hash = AddBoolHash(hash, prisoner.IsDowned);
                hash = AddBoolHash(hash, prisoner.IsDead);
                hash = AddBoolHash(hash, prisoner.CanEject);
                hash = AddBoolHash(hash, prisoner.NeedsFeeding);
                hash = AddBoolHash(hash, prisoner.CanFeed);
                hash = AddStringHash(hash, prisoner.FeedTooltip);
                hash = AddBoolHash(hash, prisoner.NeedsTending);
                hash = AddBoolHash(hash, prisoner.CanTend);
                hash = AddStringHash(hash, prisoner.TendTooltip);
                hash = AddStringHash(hash, prisoner.TendStatusLabel);
                hash = AddThingHash(hash, prisoner.DisplayThing);
                return hash;
            }
        }

        private static int AddPrisonCandidatesHash(
            int hash,
            IReadOnlyList<ShuttlePrisonerCandidateReadModel> candidates)
        {
            unchecked
            {
                hash = AddHash(hash, candidates != null ? candidates.Count : 0);
                for (int i = 0; candidates != null && i < candidates.Count; i++)
                {
                    hash = AddHash(hash, i);
                    hash = AddPrisonCandidateHash(hash, candidates[i]);
                }

                return hash;
            }
        }

        private static int AddPrisonCandidateHash(
            int hash,
            ShuttlePrisonerCandidateReadModel candidate)
        {
            unchecked
            {
                if (candidate == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddHash(hash, candidate.ThingIDNumber);
                hash = AddStringHash(hash, candidate.Label);
                hash = AddStringHash(hash, candidate.FactionLabel);
                hash = AddStringHash(hash, candidate.ReasonLabel);
                hash = AddBoolHash(hash, candidate.IsExistingPrisoner);
                hash = AddBoolHash(hash, candidate.IsDownedHostile);
                hash = AddBoolHash(hash, candidate.CanAdmit);
                hash = AddStringHash(hash, candidate.CannotAdmitReason);
                hash = AddThingHash(hash, candidate.DisplayThing);
                return hash;
            }
        }

        private static int AddPrisonCarriersHash(
            int hash,
            IReadOnlyList<ShuttlePrisonerCarrierReadModel> carriers)
        {
            unchecked
            {
                hash = AddHash(hash, carriers != null ? carriers.Count : 0);
                for (int i = 0; carriers != null && i < carriers.Count; i++)
                {
                    hash = AddHash(hash, i);
                    hash = AddPrisonCarrierHash(hash, carriers[i]);
                }

                return hash;
            }
        }

        private static int AddPrisonCarrierHash(
            int hash,
            ShuttlePrisonerCarrierReadModel carrier)
        {
            unchecked
            {
                if (carrier == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddHash(hash, carrier.ThingIDNumber);
                hash = AddStringHash(hash, carrier.Label);
                hash = AddBoolHash(hash, carrier.CanCarry);
                hash = AddStringHash(hash, carrier.CannotCarryReason);
                hash = AddThingHash(hash, carrier.DisplayThing);
                return hash;
            }
        }

        private static int AddPrisonFeedersHash(
            int hash,
            IReadOnlyList<ShuttlePrisonerFeederReadModel> feeders)
        {
            unchecked
            {
                hash = AddHash(hash, feeders != null ? feeders.Count : 0);
                for (int i = 0; feeders != null && i < feeders.Count; i++)
                {
                    hash = AddHash(hash, i);
                    hash = AddPrisonFeederHash(hash, feeders[i]);
                }

                return hash;
            }
        }

        private static int AddPrisonFeederHash(
            int hash,
            ShuttlePrisonerFeederReadModel feeder)
        {
            unchecked
            {
                if (feeder == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddHash(hash, feeder.ThingIDNumber);
                hash = AddStringHash(hash, feeder.Label);
                hash = AddBoolHash(hash, feeder.CanFeed);
                hash = AddStringHash(hash, feeder.CannotFeedReason);
                hash = AddThingHash(hash, feeder.DisplayThing);
                return hash;
            }
        }

        private static int AddPrisonDoctorsHash(
            int hash,
            IReadOnlyList<ShuttlePrisonerDoctorReadModel> doctors)
        {
            unchecked
            {
                hash = AddHash(hash, doctors != null ? doctors.Count : 0);
                for (int i = 0; doctors != null && i < doctors.Count; i++)
                {
                    hash = AddHash(hash, i);
                    hash = AddPrisonDoctorHash(hash, doctors[i]);
                }

                return hash;
            }
        }

        private static int AddPrisonDoctorHash(
            int hash,
            ShuttlePrisonerDoctorReadModel doctor)
        {
            unchecked
            {
                if (doctor == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddHash(hash, doctor.ThingIDNumber);
                hash = AddStringHash(hash, doctor.Label);
                hash = AddBoolHash(hash, doctor.CanTend);
                hash = AddStringHash(hash, doctor.CannotTendReason);
                hash = AddThingHash(hash, doctor.DisplayThing);
                return hash;
            }
        }

        private static int AddThingHash(int hash, Thing thing)
        {
            unchecked
            {
                if (thing == null)
                {
                    return AddHash(hash, 0);
                }

                hash = AddHash(hash, thing.thingIDNumber);
                hash = AddStringHash(hash, thing.def != null ? thing.def.defName : null);
                hash = AddStringHash(hash, thing.LabelCap.ToString());

                Pawn pawn = thing as Pawn;
                if (pawn == null)
                {
                    return hash;
                }

                hash = AddBoolHash(hash, pawn.RaceProps != null && pawn.RaceProps.Humanlike);
                hash = AddBoolHash(hash, pawn.RaceProps != null && pawn.RaceProps.Animal);
                hash = AddBoolHash(hash, pawn.RaceProps != null && pawn.RaceProps.IsMechanoid);
                hash = AddBoolHash(hash, pawn.IsColonist);
                hash = AddBoolHash(hash, pawn.IsPrisonerOfColony || pawn.IsPrisoner);
                hash = AddBoolHash(hash, pawn.IsSlaveOfColony || pawn.IsSlave);
                return hash;
            }
        }

        private static int AddBoolHash(int hash, bool value)
        {
            return AddHash(hash, value ? 1 : 0);
        }

        private static int AddFloatHash(int hash, float value)
        {
            return AddHash(hash, StableFloatHash(value));
        }

        private static int AddHash(int hash, int value)
        {
            unchecked
            {
                return (hash * 397) ^ value;
            }
        }

        private static int AddStringHash(int hash, string value)
        {
            unchecked
            {
                return AddHash(hash, StableStringHash(value));
            }
        }

        private static int StableFloatHash(float value)
        {
            if (float.IsNaN(value))
            {
                return int.MinValue;
            }

            if (float.IsPositiveInfinity(value))
            {
                return int.MaxValue;
            }

            if (float.IsNegativeInfinity(value))
            {
                return int.MinValue + 1;
            }

            return value.GetHashCode();
        }

        private static int StableStringHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            unchecked
            {
                int hash = 29;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = (hash * 397) ^ value[i];
                }

                return hash;
            }
        }
    }
}

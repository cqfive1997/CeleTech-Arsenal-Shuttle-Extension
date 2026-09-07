using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Medical
{
    internal sealed class ShuttleMedicalTreatmentUIActions :
        IShuttleMedicalTreatmentUIActions
    {
        private const int MaxDoctorMenuOptions = 28;

        private readonly IShuttleCommandExecutor commandExecutor;
        private readonly IShuttleMedicalBayActionPort actionPort;
        private readonly MedicalBayTreatmentService treatmentService =
            new MedicalBayTreatmentService();

        internal ShuttleMedicalTreatmentUIActions(
            IShuttleCommandExecutor commandExecutor,
            IShuttleMedicalBayActionPort actionPort)
        {
            this.commandExecutor = commandExecutor;
            this.actionPort = actionPort;
        }

        public bool CanTreatPatient(ShuttleMedicalPatientActionTarget patient)
        {
            return this.commandExecutor != null &&
                this.actionPort != null &&
                patient != null &&
                patient.PatientThingID > 0 &&
                patient.Kind == ShuttleMedicalActionPatientKind.Human &&
                !patient.TreatmentBlockedByActiveProcedure &&
                patient.NeedsTreatment;
        }

        public void OpenDoctorMenu(
            ShuttleMedicalPatientActionTarget patient,
            bool? useAvailableMedicineFilter)
        {
            if (!this.CanTreatPatient(patient))
            {
                this.ShowReject(this.GetTreatPatientTooltip(patient));
                return;
            }

            ThingWithComps shuttleHost = this.actionPort.GetMedicalBayActionHost();
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            options.Add(new FloatMenuOption(
                this.Tr("CT_Shuttle_MedicalBay_SelectDoctor"),
                null));

            bool hasEnabledOption = false;
            this.BuildDoctorOptions(
                shuttleHost,
                patient,
                useAvailableMedicineFilter,
                options,
                out hasEnabledOption);
            if (!hasEnabledOption)
            {
                options.Add(new FloatMenuOption(
                    this.Tr("CT_Shuttle_Medical_NoAvailableDoctor"),
                    null));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        public string GetTreatPatientTooltip(ShuttleMedicalPatientActionTarget patient)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (this.actionPort == null)
            {
                return this.Tr("CT_Shuttle_Medical_TreatmentCommandFailed");
            }

            if (patient == null || patient.PatientThingID <= 0)
            {
                return this.Tr("CT_Shuttle_MedicalBay_InvalidPatient");
            }

            if (patient.IsActiveProcedurePatient)
            {
                return this.Tr("CT_Shuttle_MedicalProcedure_PatientActive");
            }

            if (patient.TreatmentBlockedByActiveProcedure)
            {
                return this.Tr("CT_Shuttle_MedicalProcedure_TreatmentBlocked");
            }

            if (patient.Kind != ShuttleMedicalActionPatientKind.Human)
            {
                return this.Tr("CT_Shuttle_Medical_TreatmentHumanOnly");
            }

            if (!patient.NeedsTreatment)
            {
                return this.Tr("CT_Shuttle_MedicalBay_NoTendableHediffs");
            }

            return this.Tr("CT_Shuttle_Medical_TreatWithDoctorTooltip");
        }

        private void BuildDoctorOptions(
            ThingWithComps shuttleHost,
            ShuttleMedicalPatientActionTarget patient,
            bool? useAvailableMedicineFilter,
            List<FloatMenuOption> options,
            out bool hasEnabledOption)
        {
            hasEnabledOption = false;
            IReadOnlyList<Pawn> pawns = shuttleHost != null &&
                shuttleHost.Map != null &&
                shuttleHost.Map.mapPawns != null
                    ? shuttleHost.Map.mapPawns.AllPawnsSpawned
                    : null;
            if (pawns == null)
            {
                return;
            }

            int added = 0;
            for (int i = 0; i < pawns.Count && added < MaxDoctorMenuOptions; i++)
            {
                Pawn doctor = pawns[i];
                if (!this.IsPotentialDoctorMenuPawn(doctor, shuttleHost))
                {
                    continue;
                }

                string noMedicineFailReason;
                string medicineFailReason;
                bool canNoMedicine = this.treatmentService.CanTendContainedPatientNoMedicine(
                    shuttleHost,
                    doctor,
                    patient.PatientThingID,
                    out noMedicineFailReason);
                bool canUseMedicine =
                    this.treatmentService.CanTendContainedPatientWithAnyAllowedMedicine(
                        shuttleHost,
                        doctor,
                        patient.PatientThingID,
                        out medicineFailReason);

                if (canUseMedicine &&
                    (!useAvailableMedicineFilter.HasValue ||
                        useAvailableMedicineFilter.Value))
                {
                    this.AddDoctorOption(options, doctor, patient, true);
                    hasEnabledOption = true;
                    added++;
                }

                if (canNoMedicine &&
                    added < MaxDoctorMenuOptions &&
                    (!useAvailableMedicineFilter.HasValue ||
                        !useAvailableMedicineFilter.Value))
                {
                    this.AddDoctorOption(options, doctor, patient, false);
                    hasEnabledOption = true;
                    added++;
                }

                if (!canUseMedicine && !canNoMedicine && added < MaxDoctorMenuOptions)
                {
                    string reason = !string.IsNullOrEmpty(noMedicineFailReason)
                        ? noMedicineFailReason
                        : medicineFailReason;
                    options.Add(new FloatMenuOption(
                        doctor.LabelShortCap + " - " + this.ValueOrFallback(
                            reason,
                            this.Tr("CT_Shuttle_MedicalBay_DoctorUnavailable")),
                        null));
                    added++;
                }
            }
        }

        private void AddDoctorOption(
            List<FloatMenuOption> options,
            Pawn doctor,
            ShuttleMedicalPatientActionTarget patient,
            bool useAvailableMedicine)
        {
            string modeNotice = useAvailableMedicine
                ? this.Tr("CT_Shuttle_MedicalBay_AvailableMedicineNotice")
                : this.Tr("CT_Shuttle_MedicalBay_NoMedicineTendNotice");
            string doctorLabel = this.BuildDoctorOptionLabel(doctor);
            int doctorThingID = doctor.thingIDNumber;
            int patientThingID = patient.PatientThingID;
            options.Add(new FloatMenuOption(
                doctorLabel + " - " + modeNotice,
                delegate
                {
                    this.TreatPatient(
                        patientThingID,
                        doctorThingID,
                        useAvailableMedicine);
                }));
        }

        private bool TreatPatient(
            int patientThingID,
            int doctorThingID,
            bool useAvailableMedicine)
        {
            if (this.commandExecutor == null)
            {
                this.ShowReject(this.Tr("CT_Shuttle_Command_ExecutorUnavailable"));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new TreatMedicalBayPatientCommand(
                    patientThingID,
                    doctorThingID,
                    useAvailableMedicine));
            return this.ShowResult(result);
        }

        private string BuildDoctorOptionLabel(Pawn doctor)
        {
            if (doctor == null)
            {
                return string.Empty;
            }

            int medicineLevel = 0;
            if (doctor.skills != null)
            {
                SkillRecord skill = doctor.skills.GetSkill(SkillDefOf.Medicine);
                if (skill != null)
                {
                    medicineLevel = skill.Level;
                }
            }

            return doctor.LabelShortCap + " (" +
                SkillDefOf.Medicine.LabelCap + " " +
                medicineLevel.ToString() + ")";
        }

        private bool IsPotentialDoctorMenuPawn(Pawn doctor, ThingWithComps shuttleHost)
        {
            return doctor != null &&
                !doctor.Destroyed &&
                !doctor.Dead &&
                !doctor.Downed &&
                doctor.Spawned &&
                doctor.thingIDNumber > 0 &&
                doctor.RaceProps != null &&
                doctor.RaceProps.Humanlike &&
                doctor.Faction == Faction.OfPlayer &&
                !doctor.WorkTypeIsDisabled(WorkTypeDefOf.Doctor) &&
                shuttleHost != null &&
                shuttleHost.Map != null &&
                doctor.Map == shuttleHost.Map;
        }

        private bool ShowResult(ShuttleCommandResult result)
        {
            return ShuttleUICommandFeedback.ShowResult(result);
        }

        private void ShowReject(string message)
        {
            ShuttleUICommandFeedback.ShowReject(message);
        }

        private string ValueOrFallback(string value, string fallback)
        {
            return !string.IsNullOrEmpty(value) ? value : fallback;
        }

        private string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }
    }
}

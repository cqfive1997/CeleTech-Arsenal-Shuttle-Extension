using System;
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
    internal sealed class ShuttleMedicalSurgeryUIActions :
        IShuttleMedicalSurgeryUIActions
    {
        private const int MaxDoctorMenuOptions = 28;

        private readonly IShuttleCommandExecutor commandExecutor;
        private readonly IShuttleMedicalBayActionPort actionPort;
        private readonly Action<IShuttleMedicalSurgeryUIActions, ShuttleMedicalPatientActionTarget> openSurgerySelector;
        private readonly VirtualMedicalBaySurgeryService surgeryService =
            new VirtualMedicalBaySurgeryService();

        internal ShuttleMedicalSurgeryUIActions(
            IShuttleCommandExecutor commandExecutor,
            IShuttleMedicalBayActionPort actionPort,
            Action<IShuttleMedicalSurgeryUIActions, ShuttleMedicalPatientActionTarget> openSurgerySelector)
        {
            this.commandExecutor = commandExecutor;
            this.actionPort = actionPort;
            this.openSurgerySelector = openSurgerySelector;
        }

        public bool CanScheduleSurgery(ShuttleMedicalPatientActionTarget patient)
        {
            return this.commandExecutor != null &&
                this.actionPort != null &&
                patient != null &&
                patient.PatientThingID > 0 &&
                patient.Kind == ShuttleMedicalActionPatientKind.Human &&
                !patient.TreatmentBlockedByActiveProcedure &&
                patient.SurgeryOptions != null &&
                patient.SurgeryOptions.Count > 0;
        }

        public void OpenSurgeryMenu(ShuttleMedicalPatientActionTarget patient)
        {
            if (!this.CanScheduleSurgery(patient))
            {
                this.ShowReject(this.GetScheduleSurgeryTooltip(patient));
                return;
            }

            if (this.openSurgerySelector == null)
            {
                this.ShowReject(this.Tr("CT_Shuttle_Command_ExecutorUnavailable"));
                return;
            }

            this.openSurgerySelector(this, patient);
        }

        public string GetScheduleSurgeryTooltip(ShuttleMedicalPatientActionTarget patient)
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

            if (patient.SurgeryOptions == null || patient.SurgeryOptions.Count == 0)
            {
                return this.Tr("CT_Shuttle_MedicalSurgery_NoOptions");
            }

            return this.Tr("CT_Shuttle_MedicalSurgery_SelectOperation");
        }

        public void OpenSurgeryDoctorMenu(
            ShuttleMedicalPatientActionTarget patient,
            ShuttleMedicalSurgeryOptionActionTarget surgeryOption)
        {
            if (this.commandExecutor == null ||
                this.actionPort == null ||
                patient == null ||
                surgeryOption == null ||
                !surgeryOption.CanSchedule)
            {
                this.ShowReject(this.GetScheduleSurgeryTooltip(patient));
                return;
            }

            ThingWithComps shuttleHost = this.actionPort.GetMedicalBayActionHost();
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            options.Add(new FloatMenuOption(
                this.Tr("CT_Shuttle_MedicalSurgery_SelectDoctor"),
                null));

            IReadOnlyList<Pawn> pawns = shuttleHost != null &&
                shuttleHost.Map != null &&
                shuttleHost.Map.mapPawns != null
                    ? shuttleHost.Map.mapPawns.AllPawnsSpawned
                    : null;
            bool hasEnabledOption = false;
            if (pawns != null)
            {
                int added = 0;
                for (int i = 0; i < pawns.Count && added < MaxDoctorMenuOptions; i++)
                {
                    Pawn doctor = pawns[i];
                    if (!this.IsPotentialDoctorMenuPawn(doctor, shuttleHost))
                    {
                        continue;
                    }

                    string reason = null;
                    bool canSchedule = this.surgeryService != null &&
                        this.surgeryService.CanApplySurgeryByDefName(
                            shuttleHost,
                            doctor,
                            patient.PatientThingID,
                            surgeryOption.RecipeDefName,
                            surgeryOption.BodyPartIndex,
                            out reason);
                    if (!canSchedule)
                    {
                        options.Add(new FloatMenuOption(
                            this.BuildDoctorOptionLabel(doctor) + " - " +
                                this.ValueOrFallback(
                                    reason,
                                    this.Tr("CT_Shuttle_MedicalBay_DoctorUnavailable")),
                            null));
                        added++;
                        continue;
                    }

                    int patientThingID = patient.PatientThingID;
                    int doctorThingID = doctor.thingIDNumber;
                    string recipeDefName = surgeryOption.RecipeDefName;
                    int bodyPartIndex = surgeryOption.BodyPartIndex;
                    options.Add(new FloatMenuOption(
                        this.BuildDoctorOptionLabel(doctor),
                        delegate
                        {
                            this.ScheduleSurgery(
                                patientThingID,
                                doctorThingID,
                                recipeDefName,
                                bodyPartIndex);
                        }));
                    hasEnabledOption = true;
                    added++;
                }
            }

            if (!hasEnabledOption)
            {
                options.Add(new FloatMenuOption(
                    this.Tr("CT_Shuttle_Medical_NoAvailableDoctor"),
                    null));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private bool ScheduleSurgery(
            int patientThingID,
            int doctorThingID,
            string recipeDefName,
            int bodyPartIndex)
        {
            if (this.commandExecutor == null)
            {
                this.ShowReject(this.Tr("CT_Shuttle_Command_ExecutorUnavailable"));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new ScheduleMedicalBaySurgeryCommand(
                    patientThingID,
                    doctorThingID,
                    recipeDefName,
                    bodyPartIndex));
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

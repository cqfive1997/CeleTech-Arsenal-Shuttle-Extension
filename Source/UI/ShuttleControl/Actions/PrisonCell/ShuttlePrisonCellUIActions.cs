using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.PrisonCell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.PrisonCell
{
    internal sealed class ShuttlePrisonCellUIActions : IShuttlePrisonCellUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttlePrisonCellUIActions(IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool CanCarryToCell(
            ShuttlePrisonerCandidateReadModel candidate,
            ShuttlePrisonerCarrierReadModel carrier)
        {
            return this.commandExecutor != null &&
                candidate != null &&
                carrier != null &&
                candidate.CanAdmit &&
                carrier.CanCarry &&
                candidate.ThingIDNumber > 0 &&
                carrier.ThingIDNumber > 0;
        }

        public bool CarryToCell(
            ShuttlePrisonerCandidateReadModel candidate,
            ShuttlePrisonerCarrierReadModel carrier)
        {
            if (!this.CanCarryToCell(candidate, carrier))
            {
                this.ShowReject(this.GetCarryTooltip(candidate, carrier));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new CarryPrisonerToShuttleCellCommand(
                    candidate.ThingIDNumber,
                    carrier.ThingIDNumber));
            return this.ShowResult(result);
        }

        public string GetCarryTooltip(
            ShuttlePrisonerCandidateReadModel candidate,
            ShuttlePrisonerCarrierReadModel carrier)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (candidate == null || candidate.ThingIDNumber <= 0)
            {
                return this.Tr("CT_Shuttle_PrisonCell_InvalidPrisoner");
            }

            if (!candidate.CanAdmit)
            {
                return !string.IsNullOrEmpty(candidate.CannotAdmitReason)
                    ? candidate.CannotAdmitReason
                    : this.Tr("CT_Shuttle_PrisonCell_CannotAdmit");
            }

            if (carrier == null || carrier.ThingIDNumber <= 0)
            {
                return this.Tr("CT_Shuttle_PrisonCell_NoCarrier");
            }

            if (!carrier.CanCarry)
            {
                return !string.IsNullOrEmpty(carrier.CannotCarryReason)
                    ? carrier.CannotCarryReason
                    : this.Tr("CT_Shuttle_PrisonCell_CarrierUnavailable");
            }

            return this.Tr("CT_Shuttle_PrisonCell_CarryTooltip");
        }

        public bool CanEject(ShuttleHeldPrisonerReadModel prisoner)
        {
            return this.commandExecutor != null &&
                prisoner != null &&
                prisoner.CanEject &&
                prisoner.ThingIDNumber > 0;
        }

        public bool Eject(ShuttleHeldPrisonerReadModel prisoner)
        {
            if (!this.CanEject(prisoner))
            {
                this.ShowReject(this.GetEjectTooltip(prisoner));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new EjectShuttlePrisonerCommand(prisoner.ThingIDNumber));
            return this.ShowResult(result);
        }

        public string GetEjectTooltip(ShuttleHeldPrisonerReadModel prisoner)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (prisoner == null || prisoner.ThingIDNumber <= 0)
            {
                return this.Tr("CT_Shuttle_PrisonCell_InvalidPrisoner");
            }

            if (!prisoner.CanEject)
            {
                return this.Tr("CT_Shuttle_PrisonCell_EjectFailed");
            }

            return this.Tr("CT_Shuttle_PrisonCell_EjectTooltip");
        }

        public bool CanFeed(
            ShuttleHeldPrisonerReadModel prisoner,
            ShuttlePrisonerFeederReadModel feeder)
        {
            return this.commandExecutor != null &&
                prisoner != null &&
                feeder != null &&
                prisoner.CanFeed &&
                feeder.CanFeed &&
                prisoner.ThingIDNumber > 0 &&
                feeder.ThingIDNumber > 0;
        }

        public bool Feed(
            ShuttleHeldPrisonerReadModel prisoner,
            ShuttlePrisonerFeederReadModel feeder)
        {
            if (!this.CanFeed(prisoner, feeder))
            {
                this.ShowReject(this.GetFeedTooltip(prisoner, feeder));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new FeedShuttlePrisonerCommand(
                    prisoner.ThingIDNumber,
                    feeder.ThingIDNumber));
            return this.ShowResult(result);
        }

        public string GetFeedTooltip(
            ShuttleHeldPrisonerReadModel prisoner,
            ShuttlePrisonerFeederReadModel feeder)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (prisoner == null || prisoner.ThingIDNumber <= 0)
            {
                return this.Tr("CT_Shuttle_PrisonCell_InvalidPrisoner");
            }

            if (!prisoner.CanFeed)
            {
                return !string.IsNullOrEmpty(prisoner.FeedTooltip)
                    ? prisoner.FeedTooltip
                    : this.Tr("CT_Shuttle_PrisonCell_FeedFailed");
            }

            if (feeder == null || feeder.ThingIDNumber <= 0)
            {
                return this.Tr("CT_Shuttle_PrisonCell_NoFeeder");
            }

            if (!feeder.CanFeed)
            {
                return !string.IsNullOrEmpty(feeder.CannotFeedReason)
                    ? feeder.CannotFeedReason
                    : this.Tr("CT_Shuttle_PrisonCell_FeederUnavailable");
            }

            return this.Tr("CT_Shuttle_PrisonCell_FeedTooltip");
        }

        public bool CanTend(
            ShuttleHeldPrisonerReadModel prisoner,
            ShuttlePrisonerDoctorReadModel doctor)
        {
            return this.commandExecutor != null &&
                prisoner != null &&
                doctor != null &&
                prisoner.CanTend &&
                doctor.CanTend &&
                prisoner.ThingIDNumber > 0 &&
                doctor.ThingIDNumber > 0;
        }

        public bool Tend(
            ShuttleHeldPrisonerReadModel prisoner,
            ShuttlePrisonerDoctorReadModel doctor)
        {
            if (!this.CanTend(prisoner, doctor))
            {
                this.ShowReject(this.GetTendTooltip(prisoner, doctor));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new TendShuttlePrisonerCommand(
                    prisoner.ThingIDNumber,
                    doctor.ThingIDNumber));
            return this.ShowResult(result);
        }

        public string GetTendTooltip(
            ShuttleHeldPrisonerReadModel prisoner,
            ShuttlePrisonerDoctorReadModel doctor)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (prisoner == null || prisoner.ThingIDNumber <= 0)
            {
                return this.Tr("CT_Shuttle_PrisonCell_InvalidPrisoner");
            }

            if (!prisoner.CanTend)
            {
                return !string.IsNullOrEmpty(prisoner.TendTooltip)
                    ? prisoner.TendTooltip
                    : this.Tr("CT_Shuttle_PrisonCell_TendFailed");
            }

            if (doctor == null || doctor.ThingIDNumber <= 0)
            {
                return this.Tr("CT_Shuttle_PrisonCell_NoDoctor");
            }

            if (!doctor.CanTend)
            {
                return !string.IsNullOrEmpty(doctor.CannotTendReason)
                    ? doctor.CannotTendReason
                    : this.Tr("CT_Shuttle_PrisonCell_DoctorUnavailable");
            }

            return this.Tr("CT_Shuttle_PrisonCell_TendTooltip");
        }

        private bool ShowResult(ShuttleCommandResult result)
        {
            return ShuttleUICommandFeedback.ShowResult(result);
        }

        private void ShowReject(string message)
        {
            ShuttleUICommandFeedback.ShowReject(message);
        }

        private string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }
    }
}

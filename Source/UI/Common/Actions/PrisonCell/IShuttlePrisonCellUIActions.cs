using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.PrisonCell
{
    internal interface IShuttlePrisonCellUIActions
    {
        bool CanCarryToCell(
            ShuttlePrisonerCandidateReadModel candidate,
            ShuttlePrisonerCarrierReadModel carrier);

        bool CarryToCell(
            ShuttlePrisonerCandidateReadModel candidate,
            ShuttlePrisonerCarrierReadModel carrier);

        string GetCarryTooltip(
            ShuttlePrisonerCandidateReadModel candidate,
            ShuttlePrisonerCarrierReadModel carrier);

        bool CanEject(ShuttleHeldPrisonerReadModel prisoner);

        bool Eject(ShuttleHeldPrisonerReadModel prisoner);

        string GetEjectTooltip(ShuttleHeldPrisonerReadModel prisoner);

        bool CanFeed(
            ShuttleHeldPrisonerReadModel prisoner,
            ShuttlePrisonerFeederReadModel feeder);

        bool Feed(
            ShuttleHeldPrisonerReadModel prisoner,
            ShuttlePrisonerFeederReadModel feeder);

        string GetFeedTooltip(
            ShuttleHeldPrisonerReadModel prisoner,
            ShuttlePrisonerFeederReadModel feeder);

        bool CanTend(
            ShuttleHeldPrisonerReadModel prisoner,
            ShuttlePrisonerDoctorReadModel doctor);

        bool Tend(
            ShuttleHeldPrisonerReadModel prisoner,
            ShuttlePrisonerDoctorReadModel doctor);

        string GetTendTooltip(
            ShuttleHeldPrisonerReadModel prisoner,
            ShuttlePrisonerDoctorReadModel doctor);
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell
{
    internal static class V3PrisonCellSelection
    {
        internal static void EnsureSelection(
            ShuttlePrisonCellReadModel model,
            V3PrisonCellPageState state)
        {
            if (model == null || state == null)
            {
                return;
            }

            if (GetSelectedCandidate(model, state) == null)
            {
                state.SelectedCandidateThingID =
                    model.Candidates != null && model.Candidates.Count > 0
                        ? model.Candidates[0].ThingIDNumber
                        : -1;
            }

            if (GetSelectedHeldPrisoner(model, state) == null)
            {
                state.SelectedHeldPrisonerThingID =
                    model.Prisoners != null && model.Prisoners.Count > 0
                        ? model.Prisoners[0].ThingIDNumber
                        : -1;
            }

            if (GetSelectedCarrier(model, state) == null)
            {
                state.SelectedCarrierThingID =
                    model.Carriers != null && model.Carriers.Count > 0
                        ? model.Carriers[0].ThingIDNumber
                        : -1;
            }

            if (GetSelectedFeeder(model, state) == null)
            {
                state.SelectedFeederThingID =
                    model.Feeders != null && model.Feeders.Count > 0
                        ? model.Feeders[0].ThingIDNumber
                        : -1;
            }

            if (GetSelectedDoctor(model, state) == null)
            {
                state.SelectedDoctorThingID =
                    model.Doctors != null && model.Doctors.Count > 0
                        ? model.Doctors[0].ThingIDNumber
                        : -1;
            }
        }

        internal static ShuttleHeldPrisonerReadModel GetSelectedHeldPrisoner(
            ShuttlePrisonCellReadModel model,
            V3PrisonCellPageState state)
        {
            if (model == null || model.Prisoners == null || state == null)
            {
                return null;
            }

            for (int i = 0; i < model.Prisoners.Count; i++)
            {
                ShuttleHeldPrisonerReadModel prisoner = model.Prisoners[i];
                if (prisoner != null &&
                    prisoner.ThingIDNumber == state.SelectedHeldPrisonerThingID)
                {
                    return prisoner;
                }
            }

            return null;
        }

        internal static ShuttlePrisonerCandidateReadModel GetSelectedCandidate(
            ShuttlePrisonCellReadModel model,
            V3PrisonCellPageState state)
        {
            if (model == null || model.Candidates == null || state == null)
            {
                return null;
            }

            for (int i = 0; i < model.Candidates.Count; i++)
            {
                ShuttlePrisonerCandidateReadModel candidate = model.Candidates[i];
                if (candidate != null &&
                    candidate.ThingIDNumber == state.SelectedCandidateThingID)
                {
                    return candidate;
                }
            }

            return null;
        }

        internal static ShuttlePrisonerCarrierReadModel GetSelectedCarrier(
            ShuttlePrisonCellReadModel model,
            V3PrisonCellPageState state)
        {
            if (model == null || model.Carriers == null || state == null)
            {
                return null;
            }

            for (int i = 0; i < model.Carriers.Count; i++)
            {
                ShuttlePrisonerCarrierReadModel carrier = model.Carriers[i];
                if (carrier != null &&
                    carrier.ThingIDNumber == state.SelectedCarrierThingID)
                {
                    return carrier;
                }
            }

            return null;
        }

        internal static ShuttlePrisonerFeederReadModel GetSelectedFeeder(
            ShuttlePrisonCellReadModel model,
            V3PrisonCellPageState state)
        {
            if (model == null || model.Feeders == null || state == null)
            {
                return null;
            }

            for (int i = 0; i < model.Feeders.Count; i++)
            {
                ShuttlePrisonerFeederReadModel feeder = model.Feeders[i];
                if (feeder != null &&
                    feeder.ThingIDNumber == state.SelectedFeederThingID)
                {
                    return feeder;
                }
            }

            return null;
        }

        internal static ShuttlePrisonerDoctorReadModel GetSelectedDoctor(
            ShuttlePrisonCellReadModel model,
            V3PrisonCellPageState state)
        {
            if (model == null || model.Doctors == null || state == null)
            {
                return null;
            }

            for (int i = 0; i < model.Doctors.Count; i++)
            {
                ShuttlePrisonerDoctorReadModel doctor = model.Doctors[i];
                if (doctor != null &&
                    doctor.ThingIDNumber == state.SelectedDoctorThingID)
                {
                    return doctor;
                }
            }

            return null;
        }
    }
}

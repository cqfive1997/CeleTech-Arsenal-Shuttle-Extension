using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell
{
    internal sealed class V3PrisonCellWorkerModelBuilder
    {
        internal IReadOnlyList<ShuttlePrisonerCarrierReadModel> BuildCarriers(
            IReadOnlyList<ShuttlePrisonerCarrierReadModel> source)
        {
            List<ShuttlePrisonerCarrierReadModel> carriers =
                new List<ShuttlePrisonerCarrierReadModel>();
            if (source == null)
            {
                return carriers;
            }

            for (int i = 0; i < source.Count; i++)
            {
                ShuttlePrisonerCarrierReadModel carrier =
                    this.CopyCarrier(source[i]);
                if (carrier != null)
                {
                    carriers.Add(carrier);
                }
            }

            return carriers;
        }

        internal IReadOnlyList<ShuttlePrisonerFeederReadModel> BuildFeeders(
            IReadOnlyList<ShuttlePrisonerFeederReadModel> source)
        {
            List<ShuttlePrisonerFeederReadModel> feeders =
                new List<ShuttlePrisonerFeederReadModel>();
            if (source == null)
            {
                return feeders;
            }

            for (int i = 0; i < source.Count; i++)
            {
                ShuttlePrisonerFeederReadModel feeder = this.CopyFeeder(source[i]);
                if (feeder != null)
                {
                    feeders.Add(feeder);
                }
            }

            return feeders;
        }

        internal IReadOnlyList<ShuttlePrisonerDoctorReadModel> BuildDoctors(
            IReadOnlyList<ShuttlePrisonerDoctorReadModel> source)
        {
            List<ShuttlePrisonerDoctorReadModel> doctors =
                new List<ShuttlePrisonerDoctorReadModel>();
            if (source == null)
            {
                return doctors;
            }

            for (int i = 0; i < source.Count; i++)
            {
                ShuttlePrisonerDoctorReadModel doctor = this.CopyDoctor(source[i]);
                if (doctor != null)
                {
                    doctors.Add(doctor);
                }
            }

            return doctors;
        }

        private ShuttlePrisonerCarrierReadModel CopyCarrier(
            ShuttlePrisonerCarrierReadModel source)
        {
            if (source == null)
            {
                return null;
            }

            ShuttlePrisonerCarrierReadModel target =
                new ShuttlePrisonerCarrierReadModel();
            target.ThingIDNumber = source.ThingIDNumber;
            target.Label = source.Label;
            target.CanCarry = source.CanCarry;
            target.CannotCarryReason = source.CannotCarryReason;
            target.DisplayThing = source.DisplayThing;
            return target;
        }

        private ShuttlePrisonerFeederReadModel CopyFeeder(
            ShuttlePrisonerFeederReadModel source)
        {
            if (source == null)
            {
                return null;
            }

            ShuttlePrisonerFeederReadModel target =
                new ShuttlePrisonerFeederReadModel();
            target.ThingIDNumber = source.ThingIDNumber;
            target.Label = source.Label;
            target.CanFeed = source.CanFeed;
            target.CannotFeedReason = source.CannotFeedReason;
            target.DisplayThing = source.DisplayThing;
            return target;
        }

        private ShuttlePrisonerDoctorReadModel CopyDoctor(
            ShuttlePrisonerDoctorReadModel source)
        {
            if (source == null)
            {
                return null;
            }

            ShuttlePrisonerDoctorReadModel target =
                new ShuttlePrisonerDoctorReadModel();
            target.ThingIDNumber = source.ThingIDNumber;
            target.Label = source.Label;
            target.CanTend = source.CanTend;
            target.CannotTendReason = source.CannotTendReason;
            target.DisplayThing = source.DisplayThing;
            return target;
        }
    }
}

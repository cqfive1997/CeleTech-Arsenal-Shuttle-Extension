using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell
{
    internal sealed class V3PrisonCandidateModelBuilder
    {
        internal IReadOnlyList<ShuttlePrisonerCandidateReadModel> BuildCandidates(
            IReadOnlyList<ShuttlePrisonerCandidateReadModel> source)
        {
            List<ShuttlePrisonerCandidateReadModel> candidates =
                new List<ShuttlePrisonerCandidateReadModel>();
            if (source == null)
            {
                return candidates;
            }

            for (int i = 0; i < source.Count; i++)
            {
                ShuttlePrisonerCandidateReadModel candidate =
                    this.CopyCandidate(source[i]);
                if (candidate != null)
                {
                    candidates.Add(candidate);
                }
            }

            return candidates;
        }

        private ShuttlePrisonerCandidateReadModel CopyCandidate(
            ShuttlePrisonerCandidateReadModel source)
        {
            if (source == null)
            {
                return null;
            }

            ShuttlePrisonerCandidateReadModel target =
                new ShuttlePrisonerCandidateReadModel();
            target.ThingIDNumber = source.ThingIDNumber;
            target.Label = source.Label;
            target.FactionLabel = source.FactionLabel;
            target.ReasonLabel = source.ReasonLabel;
            target.IsExistingPrisoner = source.IsExistingPrisoner;
            target.IsDownedHostile = source.IsDownedHostile;
            target.CanAdmit = source.CanAdmit;
            target.CannotAdmitReason = source.CannotAdmitReason;
            target.DisplayThing = source.DisplayThing;
            return target;
        }
    }
}

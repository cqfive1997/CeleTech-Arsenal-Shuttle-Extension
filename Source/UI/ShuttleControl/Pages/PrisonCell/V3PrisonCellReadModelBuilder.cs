using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell
{
    internal sealed class V3PrisonCellReadModelBuilder
    {
        private readonly V3PrisonCellStatusModelBuilder statusBuilder =
            new V3PrisonCellStatusModelBuilder();
        private readonly V3PrisonerCardModelBuilder prisonerBuilder =
            new V3PrisonerCardModelBuilder();
        private readonly V3PrisonCandidateModelBuilder candidateBuilder =
            new V3PrisonCandidateModelBuilder();
        private readonly V3PrisonCellWorkerModelBuilder workerBuilder =
            new V3PrisonCellWorkerModelBuilder();

        internal ShuttlePrisonCellReadModel Build(
            ShuttleControlReadModel controlModel)
        {
            ShuttlePrisonCellReadModel source = controlModel != null
                ? controlModel.PrisonCell
                : null;
            ShuttlePrisonCellReadModel target = new ShuttlePrisonCellReadModel();
            if (source == null)
            {
                return target;
            }

            this.statusBuilder.CopyStatus(source, target);
            target.Prisoners = this.prisonerBuilder.BuildPrisoners(source.Prisoners);
            target.Candidates = this.candidateBuilder.BuildCandidates(source.Candidates);
            target.Carriers = this.workerBuilder.BuildCarriers(source.Carriers);
            target.Feeders = this.workerBuilder.BuildFeeders(source.Feeders);
            target.Doctors = this.workerBuilder.BuildDoctors(source.Doctors);
            return target;
        }
    }
}

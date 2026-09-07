using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalPageModelBuilder
    {
        private readonly V3MedicalReadModelBuilder medicalBuilder =
            new V3MedicalReadModelBuilder();

        internal void Fill(V3MedicalPageModel model, V3MedicalPageInputs inputs)
        {
            if (model == null)
            {
                return;
            }

            model.ControlModel = inputs != null && inputs.ControlModel != null
                ? inputs.ControlModel
                : new ShuttleControlReadModel();
            model.MedicalModel = this.medicalBuilder.Build(
                model.ControlModel,
                inputs != null ? inputs.PawnPresenceSnapshot : null,
                inputs != null ? inputs.PawnDynamicStatusSnapshot : null);
        }
    }
}

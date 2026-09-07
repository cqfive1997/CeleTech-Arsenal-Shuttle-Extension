using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    internal sealed class V3ExternalModulesPageModelBuilder
    {
        private readonly ExternalModuleUIReadModelBuilder summaryBuilder =
            new ExternalModuleUIReadModelBuilder();

        internal void Fill(
            V3ExternalModulesPageModel model,
            V3ExternalModulesPageInputs inputs,
            ShuttlePageDrawContext context)
        {
            if (model == null)
            {
                return;
            }

            model.ControlModel = inputs != null && inputs.ControlModel != null
                ? inputs.ControlModel
                : new ShuttleControlReadModel();
            // ExternalModules needs the full runtime list; Main uses badge models.
            model.ExternalModuleModels = inputs != null
                ? inputs.ExternalModuleModels
                : null;
            model.Summary = this.summaryBuilder.BuildSummary(model.ExternalModuleModels);
            this.FillSegmentLabels(model, model.ControlModel);
            model.RuntimeEnablementActions =
                context != null && context.ExternalModulesPageContext != null
                    ? context.ExternalModulesPageContext.RuntimeEnablementActions
                    : null;
            model.ExternalModuleDetailsActions =
                context != null && context.ExternalModulesPageContext != null
                    ? context.ExternalModulesPageContext.ModuleDetailsActions
                    : null;
        }

        private void FillSegmentLabels(
            V3ExternalModulesPageModel model,
            ShuttleControlReadModel controlModel)
        {
            if (model.SegmentLabelsByModuleId == null)
            {
                model.SegmentLabelsByModuleId =
                    new Dictionary<string, string>(System.StringComparer.Ordinal);
            }

            model.SegmentLabelsByModuleId.Clear();
            if (controlModel == null || controlModel.SegmentSlots == null)
            {
                return;
            }

            for (int i = 0; i < controlModel.SegmentSlots.Count; i++)
            {
                ShuttleControlSegmentSlotModel segment = controlModel.SegmentSlots[i];
                if (segment == null || segment.ModuleSlots == null)
                {
                    continue;
                }

                string segmentLabel = ShuttleControlDisplayNameResolver.ResolveSegmentLabel(segment);
                for (int j = 0; j < segment.ModuleSlots.Count; j++)
                {
                    ShuttleControlModuleSlotModel module = segment.ModuleSlots[j];
                    if (module != null && !string.IsNullOrEmpty(module.InstalledModuleInstanceID))
                    {
                        model.SegmentLabelsByModuleId[module.InstalledModuleInstanceID] =
                            segmentLabel;
                    }
                }
            }
        }
    }
}

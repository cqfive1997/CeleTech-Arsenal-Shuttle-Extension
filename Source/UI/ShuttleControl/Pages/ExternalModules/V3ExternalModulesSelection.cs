using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    internal sealed class V3ExternalModulesSelection
    {
        internal ExternalModuleUIReadModel EnsureSelected(
            V3ExternalModulesPageState state,
            IReadOnlyList<ExternalModuleUIReadModel> models)
        {
            ExternalModuleUIReadModel selected = this.FindSelected(state, models);
            if (selected != null)
            {
                return selected;
            }

            selected = models != null && models.Count > 0 ? models[0] : null;
            this.Select(state, selected);
            return selected;
        }

        internal ExternalModuleUIReadModel FindSelected(
            V3ExternalModulesPageState state,
            IReadOnlyList<ExternalModuleUIReadModel> models)
        {
            if (state == null || models == null)
            {
                return null;
            }

            for (int i = 0; i < models.Count; i++)
            {
                ExternalModuleUIReadModel model = models[i];
                if (model != null &&
                    model.ModuleInstanceID == state.SelectedModuleInstanceID &&
                    model.RuntimeSystemKey == state.SelectedRuntimeSystemKey)
                {
                    return model;
                }
            }

            return null;
        }

        internal void Select(
            V3ExternalModulesPageState state,
            ExternalModuleUIReadModel model)
        {
            if (state == null)
            {
                return;
            }

            state.SelectedModuleInstanceID = model != null ? model.ModuleInstanceID : null;
            state.SelectedRuntimeSystemKey = model != null ? model.RuntimeSystemKey : null;
        }

        internal bool IsSelected(
            V3ExternalModulesPageState state,
            ExternalModuleUIReadModel model)
        {
            return state != null &&
                model != null &&
                model.ModuleInstanceID == state.SelectedModuleInstanceID &&
                model.RuntimeSystemKey == state.SelectedRuntimeSystemKey;
        }
    }
}

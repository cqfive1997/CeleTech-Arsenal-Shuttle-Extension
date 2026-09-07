namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoLogisticsBayStateBuilder
    {
        internal V3CargoBayLogisticsState Build(V3CargoPageReadModel pageModel)
        {
            V3CargoBayLogisticsState state = new V3CargoBayLogisticsState();
            if (pageModel == null || pageModel.Bays == null)
            {
                return state;
            }

            for (int i = 0; i < pageModel.Bays.Count; i++)
            {
                V3CargoBayCardModel bay = pageModel.Bays[i];
                if (bay == null)
                {
                    continue;
                }

                if (bay.IsRefrigerated)
                {
                    state.HasRefrigeratedCargo = true;
                    state.RefrigeratedEnabled |= bay.IsEnabled;
                    state.RefrigeratedAutoTransfer |=
                        bay.IsEnabled && bay.AutoTransferEnabled;
                }
                else
                {
                    state.HasStandardCargo = true;
                }
            }

            return state;
        }
    }

    internal struct V3CargoBayLogisticsState
    {
        internal bool HasStandardCargo;
        internal bool HasRefrigeratedCargo;
        internal bool RefrigeratedEnabled;
        internal bool RefrigeratedAutoTransfer;
    }
}

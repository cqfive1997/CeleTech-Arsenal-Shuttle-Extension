namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo
{
    internal interface IShuttleCargoBayUIActions
    {
        void OpenBayConfig(ShuttleCargoBayActionTarget bay);

        bool CanApplyBayConfig(
            ShuttleCargoBayActionTarget bay,
            ShuttleCargoBayConfigActionTarget config);

        bool ApplyBayConfig(
            ShuttleCargoBayActionTarget bay,
            ShuttleCargoBayConfigActionTarget config);

        string GetBayConfigApplyTooltip(
            ShuttleCargoBayActionTarget bay,
            ShuttleCargoBayConfigActionTarget config);

        void OpenBayContents(
            ShuttleCargoBayActionTarget bay,
            ShuttleCargoPageActionContext pageContext);
    }
}

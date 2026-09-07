namespace CeleTech.ShuttleExtension.ModularShuttle.API.UI
{
    /// <summary>
    /// Stable descriptor shape for future multi-panel selection and ordering.
    /// The current host uses registration data directly but keeps this public DTO reserved.
    /// </summary>
    public sealed class ShuttleExternalModulePanelDescriptor
    {
        public ShuttleExternalModulePanelDescriptor(
            string panelKey,
            string runtimeSystemKey,
            string label,
            int order)
        {
            this.PanelKey = panelKey;
            this.RuntimeSystemKey = runtimeSystemKey;
            this.Label = label;
            this.Order = order;
        }

        public string PanelKey { get; private set; }

        public string RuntimeSystemKey { get; private set; }

        public string Label { get; private set; }

        public int Order { get; private set; }
    }
}

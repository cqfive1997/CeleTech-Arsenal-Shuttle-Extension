using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.UI
{
    /// <summary>
    /// Recommended base class for panel providers. The no-op defaults preserve compatibility
    /// when optional provider hooks are added to the SDK.
    /// </summary>
    public abstract class ShuttleExternalModulePanelProviderBase :
        IShuttleExternalModulePanelProvider
    {
        public abstract string RuntimeSystemKey { get; }

        public virtual bool CanShow(ShuttleExternalModulePanelContext context)
        {
            return true;
        }

        public virtual float GetPreferredHeight(
            ShuttleExternalModulePanelContext context,
            float width)
        {
            return 160f;
        }

        public virtual void DrawPanel(
            Rect rect,
            ShuttleExternalModulePanelContext context)
        {
        }

        public virtual void CollectCommands(
            ShuttleExternalModulePanelContext context,
            IShuttleExternalModulePanelCommandSink sink)
        {
        }
    }
}

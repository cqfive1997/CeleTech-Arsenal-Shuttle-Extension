using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings
{
    /// <summary>
    /// Native V3 Settings page. It owns V3 state/model/layout and routes actions
    /// through the adapter-phase settings action boundary.
    /// </summary>
    internal sealed class V3SettingsPage : IShuttleIntegratedHeaderPageV3
    {
        private readonly V3SettingsPageModel model = new V3SettingsPageModel();
        private readonly V3SettingsPageModelBuilder modelBuilder =
            new V3SettingsPageModelBuilder();
        private readonly V3SettingsPageState fallbackState = new V3SettingsPageState();
        private readonly V3SettingsHeader header = new V3SettingsHeader();
        private readonly V3SettingsSectionPanel sectionPanel =
            new V3SettingsSectionPanel();

        public ShuttleControlPageId Page
        {
            get { return ShuttleControlPageId.Settings; }
        }

        public void OnEnter(ShuttlePageDrawContext context)
        {
        }

        public void OnExit(ShuttlePageDrawContext context)
        {
        }

        public void Draw(Rect rect, ShuttlePageDrawContext context)
        {
            if (context == null)
            {
                return;
            }

            V3SettingsPageState state = this.GetPageState(context);
            this.modelBuilder.Fill(this.model, context.SettingsInputs);

            ShuttlePageFrameLayout layout = ShuttlePageFrameLayout.HeaderBody(rect);

            this.header.Draw(layout.HeaderRect, this.model, context);
            this.sectionPanel.Draw(
                layout.BodyRect,
                this.model,
                state,
                context);
        }

        private V3SettingsPageState GetPageState(ShuttlePageDrawContext context)
        {
            if (context != null && context.State != null && context.State.Settings != null)
            {
                return context.State.Settings;
            }

            return this.fallbackState;
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    /// <summary>
    /// Native V3 Main shell and module/segment surface.
    /// </summary>
    internal sealed class V3MainPage : IShuttleControlPageV3
    {
        private readonly V3MainText text = new V3MainText();
        private readonly V3MainPageModel model = new V3MainPageModel();
        private readonly V3MainPageModelBuilder modelBuilder = new V3MainPageModelBuilder();
        private readonly V3MainPageState fallbackState = new V3MainPageState();
        private readonly V3MainModuleAreaPanel moduleAreaPanel;
        private readonly V3MainHeaderPanel headerPanel;
        private readonly V3SharedMessagePanel messagePanel =
            new V3SharedMessagePanel();

        internal V3MainPage()
        {
            V3MainPanelDrawer panel = new V3MainPanelDrawer(this.text);
            V3MainModuleSelection selection = new V3MainModuleSelection();
            this.moduleAreaPanel = new V3MainModuleAreaPanel(
                this.text,
                panel,
                selection);
            this.headerPanel = new V3MainHeaderPanel(
                this.text);
        }

        public ShuttleControlPageId Page
        {
            get { return ShuttleControlPageId.Main; }
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

            V3MainPageState state = this.GetPageState(context);
            this.modelBuilder.Fill(this.model, context.MainInputs);

            ShuttlePageFrameLayout layout = ShuttlePageFrameLayout.HeaderBody(rect);
            this.headerPanel.Draw(layout.HeaderRect, this.model, context);
            this.DrawBody(layout.BodyRect, context, state);
        }

        private void DrawBody(
            Rect rect,
            ShuttlePageDrawContext context,
            V3MainPageState state)
        {
            V3MainModuleAreaLayout layout = V3MainModuleAreaLayout.FromBodyRect(rect);
            this.moduleAreaPanel.Draw(layout, this.model, state, context);
            this.messagePanel.Draw(
                layout.MessageRect,
                context,
                state.MessagePanelState,
                ref state.MessageScroll);
        }

        private V3MainPageState GetPageState(ShuttlePageDrawContext context)
        {
            if (context != null && context.State != null && context.State.Main != null)
            {
                return context.State.Main;
            }

            return this.fallbackState;
        }
    }
}

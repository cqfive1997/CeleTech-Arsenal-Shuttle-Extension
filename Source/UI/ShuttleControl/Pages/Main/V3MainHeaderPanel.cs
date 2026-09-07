using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainHeaderPanel
    {
        internal const float HeaderHeight = ShuttleControlHeaderLayout.HeaderHeight;

        private readonly V3MainHeaderSpecBuilder specBuilder;

        internal V3MainHeaderPanel(V3MainText text)
        {
            this.specBuilder = new V3MainHeaderSpecBuilder(text);
        }

        internal void Draw(
            Rect rect,
            V3MainPageModel model,
            ShuttlePageDrawContext context)
        {
            if (model == null || context == null)
            {
                return;
            }

            ShuttleControlHeaderTopRowLayout layout =
                ShuttleControlHeaderDrawer.Draw(
                rect,
                this.specBuilder.GetTitle(model),
                this.specBuilder.GetLogo(context),
                this.specBuilder.BuildStatusSpecs(model),
                this.specBuilder.BuildCommandSpecs(context),
                this.specBuilder.BuildRibbonSpec(model));
            this.RegisterTutorialTargets(context, layout);
        }

        private void RegisterTutorialTargets(
            ShuttlePageDrawContext context,
            ShuttleControlHeaderTopRowLayout layout)
        {
            if (context == null ||
                context.Services == null ||
                context.Services.TutorialTargets == null)
            {
                return;
            }

            IShuttleTutorialTargetService tutorialTargets =
                context.Services.TutorialTargets;
            tutorialTargets.Register(ShuttleTutorialTargetIds.HeaderLogo, layout.LogoRect);
            tutorialTargets.Register(
                ShuttleTutorialTargetIds.HeaderStatusCards,
                layout.StatusCardsRect);
            tutorialTargets.Register(
                ShuttleTutorialTargetIds.HeaderLoadCargoButton,
                layout.GetCommandRect(0));
            tutorialTargets.Register(
                ShuttleTutorialTargetIds.HeaderCargoUnloadButton,
                layout.GetCommandRect(1));
            tutorialTargets.Register(
                ShuttleTutorialTargetIds.HeaderPageSwitchButton,
                layout.GetCommandRect(2));
            tutorialTargets.Register(
                ShuttleTutorialTargetIds.HeaderLaunchButton,
                layout.GetCommandRect(3));
        }
    }
}

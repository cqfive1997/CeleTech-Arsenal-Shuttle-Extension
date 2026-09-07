using CeleTech.ShuttleExtension.ModularShuttle.UI;
using CeleTech.ShuttleExtension.ModularShuttle.UI.FireControl;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    public sealed partial class CompModularShuttleCore
    {
        private Gizmo_ShuttleFireControl cachedFireControlGizmo;

        internal Gizmo GetFireControlGizmo()
        {
            this.EnsureController();
            if (this.controller == null)
            {
                return null;
            }

            if (this.cachedFireControlGizmo == null)
            {
                ShuttleControllerUIPort uiPort =
                    new ShuttleControllerUIPort(this.controller);
                this.cachedFireControlGizmo = ShuttleFireControlGizmoProvider.Create(
                    uiPort,
                    uiPort,
                    this.OpenControlDefensePage);
            }

            return this.cachedFireControlGizmo != null &&
                this.cachedFireControlGizmo.ShouldDisplay
                ? this.cachedFireControlGizmo
                : null;
        }

        internal void OpenControlDefensePage()
        {
            if (ShuttleCriticalDamagePolicy.TryRejectRestrictedInteraction(this.parent))
            {
                return;
            }

            this.EnsureProfile();
            ShuttleControlUIOpener.OpenDefenseControlPage(
                this.parent,
                this.controller,
                this);
        }
    }
}

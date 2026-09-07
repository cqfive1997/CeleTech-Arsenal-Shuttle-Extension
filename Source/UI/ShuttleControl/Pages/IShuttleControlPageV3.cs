using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages
{
    /// <summary>
    /// Minimal V3 page contract. Page identity is UI-neutral; V2 page ids are
    /// translated only at legacy adapter boundaries.
    /// </summary>
    internal interface IShuttleControlPageV3
    {
        ShuttleControlPageId Page { get; }

        void OnEnter(ShuttlePageDrawContext context);

        void OnExit(ShuttlePageDrawContext context);

        void Draw(Rect rect, ShuttlePageDrawContext context);
    }
}

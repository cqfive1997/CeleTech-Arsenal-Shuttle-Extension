using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.FireControl
{
    internal sealed class ShuttleFireControlGizmoTextures
    {
        internal ShuttleFireControlGizmoTextures(
            Texture2D openFire,
            Texture2D holdFire,
            Texture2D target,
            Texture2D clearTarget,
            Texture2D mode,
            Texture2D link,
            Texture2D reload,
            Texture2D openDefense)
        {
            this.OpenFire = openFire;
            this.HoldFire = holdFire;
            this.Target = target;
            this.ClearTarget = clearTarget;
            this.Mode = mode;
            this.Link = link;
            this.Reload = reload;
            this.OpenDefense = openDefense;
        }

        internal Texture2D OpenFire { get; private set; }
        internal Texture2D HoldFire { get; private set; }
        internal Texture2D Target { get; private set; }
        internal Texture2D ClearTarget { get; private set; }
        internal Texture2D Mode { get; private set; }
        internal Texture2D Link { get; private set; }
        internal Texture2D Reload { get; private set; }
        internal Texture2D OpenDefense { get; private set; }
    }
}

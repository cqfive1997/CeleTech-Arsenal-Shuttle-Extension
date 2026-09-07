namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface
{
    /// <summary>
    /// Detached visual-only configuration for the active Surface Shield module.
    /// It carries static asset paths and read-only status values; it never exposes
    /// mutable runtime state to presentation comps. Mask/noise paths remain
    /// static visual assets; presentation code may load them but must fall back
    /// gracefully when they are absent.
    /// </summary>
    internal sealed class ShuttleSurfaceShieldVisualConfigSnapshot
    {
        public bool HasConfig;
        public string ModuleInstanceID;
        public string SurfaceShaderBundlePath;
        public string SurfaceMaterialAssetName;
        public string SurfaceMaskTexturePath;
        public string SurfaceNoiseTexturePath;
        public float SelectedRadius;
        public float MinRadius;
        public float MaxRadius;
        public float HitPointsPercent;
        public string StatusKey;
        public bool Online;
    }
}

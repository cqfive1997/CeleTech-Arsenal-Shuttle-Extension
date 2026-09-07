using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.CustomShaders
{
    internal static class ShuttlePlatformAssetBundleEnvironment
    {
        internal static ShuttleAssetBundlePlatform CurrentPlatform
        {
            get { return FromRuntimePlatform(Application.platform); }
        }

        internal static ShuttleAssetBundlePlatform FromRuntimePlatform(
            RuntimePlatform runtimePlatform)
        {
            switch (runtimePlatform)
            {
                case RuntimePlatform.WindowsPlayer:
                    return ShuttleAssetBundlePlatform.Windows;
                case RuntimePlatform.OSXPlayer:
                    return ShuttleAssetBundlePlatform.MacOS;
                case RuntimePlatform.LinuxPlayer:
                    return ShuttleAssetBundlePlatform.Linux;
                default:
                    return ShuttleAssetBundlePlatform.Unknown;
            }
        }
    }
}

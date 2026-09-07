using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell
{
    internal interface IShuttlePaintPreviewService
    {
        bool TryGetPreview(ShuttlePaintSchemeSnapshot paintScheme, out Texture texture);

        bool TryGetPreview(
            ShuttlePaintSchemeSnapshot paintScheme,
            int weaponStationMask,
            out Texture texture);

        bool TryGetPreview(
            ShuttlePaintSchemeSnapshot paintScheme,
            int weaponStationMask,
            Rot4 rotation,
            out Texture texture);
    }
}

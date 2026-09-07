using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Owns the narrow vanilla-Lord reset required before assigning a fresh
    /// transporter group. It never edits cargo or load selections.
    /// </summary>
    internal static class ShuttleTransporterLordLifecycle
    {
        internal static void ResetBeforeNewManifest(
            ThingWithComps host,
            List<CompTransporter> transporters)
        {
            if (host == null || host.Map == null || transporters == null)
            {
                return;
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter != null)
                {
                    // A stale Lord can outlive the loading flags. TryRemoveLord is
                    // idempotent and must run before vanilla replaces groupID.
                    transporter.TryRemoveLord(host.Map);
                }
            }
        }
    }
}

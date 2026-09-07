using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed class CompProperties_ModularTransporter : CompProperties_Transporter
    {
        public CompProperties_ModularTransporter()
        {
            this.compClass = typeof(CompModularTransporter);
        }
    }

    /// <summary>
    /// Transporter backend for the modular shuttle. Cargo truth still lives in the
    /// vanilla transporter containers, but player-facing cargo UI is shuttle-owned.
    /// </summary>
    public sealed class CompModularTransporter : CompTransporter, INotifyHauledTo
    {
        public void Notify_HauledTo(Pawn hauler, Thing thing, int count)
        {
            if (thing == null || count <= 0 || this.parent == null)
            {
                return;
            }

            CompModularShuttleCore core = this.parent.TryGetComp<CompModularShuttleCore>();
            if (core != null)
            {
                core.NotifyCargoHauledToTransporter(this, thing, count);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield break;
        }

        public override string CompInspectStringExtra()
        {
            return null;
        }
    }
}

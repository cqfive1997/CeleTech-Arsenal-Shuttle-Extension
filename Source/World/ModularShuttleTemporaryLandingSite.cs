using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.World
{
    public sealed class ModularShuttleTemporaryLandingSite : MapParent
    {
        private static readonly StringBuilder settleFailReason = new StringBuilder();

        public override Material Material
        {
            get
            {
                return this.def != null ? this.def.Material : base.Material;
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if (this.HasMap && Find.WorldSelector.SingleSelectedObject == this)
            {
                Command settleCommand = SettleInExistingMapUtility.SettleCommand(this.Map, true);
                settleFailReason.Length = 0;
                if (!TileFinder.IsValidTileForNewSettlement(this.Tile, settleFailReason, false))
                {
                    settleCommand.Disable(settleFailReason.ToString());
                }

                yield return settleCommand;
                settleFailReason.Length = 0;
            }
        }

        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
        {
            alsoRemoveWorldObject = false;
            if (!this.HasMap ||
                this.Map.mapPawns.AnyPawnBlockingMapRemoval ||
                ModularShuttleLandingMapHoldService.ContainsSpawnedModularShuttle(this.Map) ||
                this.Map.AnyBuildingBlockingMapRemoval ||
                TransporterUtility.IncomingTransporterPreventingMapRemoval(this.Map))
            {
                return false;
            }

            alsoRemoveWorldObject = true;
            return true;
        }

    }
}

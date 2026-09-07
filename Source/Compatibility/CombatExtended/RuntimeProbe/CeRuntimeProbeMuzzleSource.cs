using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal sealed class CeRuntimeProbeMuzzleSource
    {
        private const int HostSelectionMarginCells = 2;

        internal CeRuntimeProbeMuzzleSource(IntVec3 cell, Vector3 drawPosition)
            : this(
                cell,
                drawPosition,
                false,
                null,
                null,
                null,
                null,
                false,
                0)
        {
        }

        internal CeRuntimeProbeMuzzleSource(
            IntVec3 cell,
            Vector3 drawPosition,
            bool usesExistingResolver,
            string moduleInstanceID,
            string moduleDefName,
            string weaponDefName,
            string parentSlotID,
            bool resolverUsesFallback,
            int resolverMuzzleIndex)
        {
            this.Cell = cell;
            this.DrawPosition = drawPosition;
            this.UsesExistingResolver = usesExistingResolver;
            this.ModuleInstanceID = moduleInstanceID;
            this.ModuleDefName = moduleDefName;
            this.WeaponDefName = weaponDefName;
            this.ParentSlotID = parentSlotID;
            this.ResolverUsesFallback = resolverUsesFallback;
            this.ResolverMuzzleIndex = resolverMuzzleIndex;
        }

        internal IntVec3 Cell { get; private set; }

        internal Vector3 DrawPosition { get; private set; }

        internal bool UsesExistingResolver { get; private set; }

        internal string ModuleInstanceID { get; private set; }

        internal string ModuleDefName { get; private set; }

        internal string WeaponDefName { get; private set; }

        internal string ParentSlotID { get; private set; }

        internal bool ResolverUsesFallback { get; private set; }

        internal int ResolverMuzzleIndex { get; private set; }

        internal bool IsValidFor(Thing host)
        {
            return host != null &&
                host.Spawned &&
                host.Map != null &&
                this.Cell.IsValid &&
                this.Cell.InBounds(host.Map) &&
                IsFinite(this.DrawPosition.x) &&
                IsFinite(this.DrawPosition.z) &&
                this.DrawPosition.ToIntVec3() == this.Cell &&
                (this.UsesExistingResolver || this.IsNearHost(host));
        }

        internal bool IsNearHost(Thing host)
        {
            return host != null &&
                host.Spawned &&
                host.OccupiedRect()
                    .ExpandedBy(HostSelectionMarginCells)
                    .Contains(this.Cell);
        }

        internal static CellRect GetSelectionBounds(Thing host)
        {
            return host != null && host.Spawned
                ? host.OccupiedRect().ExpandedBy(HostSelectionMarginCells)
                : CellRect.Empty;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

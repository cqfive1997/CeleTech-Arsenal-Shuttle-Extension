using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    /// <summary>
    /// Runtime-only exact source supplied by the shuttle visual muzzle resolver.
    /// </summary>
    internal sealed class CelestialSustainLaserMuzzleState
    {
        internal CelestialSustainLaserMuzzleState()
        {
            this.Cell = IntVec3.Invalid;
            this.DrawPos = Vector3.zero;
        }

        internal IntVec3 Cell { get; private set; }

        internal Vector3 DrawPos { get; private set; }

        internal bool HasSource { get; private set; }

        internal void Set(IntVec3 cell, Vector3 drawPos)
        {
            this.Cell = cell.IsValid ? cell : IntVec3.Invalid;
            this.DrawPos = drawPos;
            this.HasSource = cell.IsValid;
        }

        internal bool TrySet(ShuttleWeaponMuzzleSource source)
        {
            if (!source.Cell.IsValid)
            {
                return false;
            }

            this.Set(source.Cell, source.DrawPos);
            return true;
        }

        internal bool IsUsable(Thing caster)
        {
            return caster != null && caster.Map != null &&
                this.HasSource && this.Cell.IsValid &&
                this.Cell.InBounds(caster.Map);
        }
    }
}

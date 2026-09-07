using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Short-lived, in-memory transfer candidate. It is never stored on RuntimeState or Scribed.
    /// </summary>
    internal sealed class CeWeaponPreparedMagazineTransfer
    {
        internal CeWeaponPreparedMagazineTransfer(
            CeWeaponCompatibilitySpec spec,
            Thing expectedSourceGun,
            ThingWithComps preparedGun,
            string sourceAmmoDefName,
            int sourceLoadedCount)
        {
            this.Spec = spec;
            this.ExpectedSourceGun = expectedSourceGun;
            this.PreparedGun = preparedGun;
            this.SourceAmmoDefName = sourceAmmoDefName;
            this.SourceLoadedCount = sourceLoadedCount;
        }

        internal CeWeaponCompatibilitySpec Spec { get; private set; }

        internal Thing ExpectedSourceGun { get; private set; }

        internal ThingWithComps PreparedGun { get; private set; }

        internal string SourceAmmoDefName { get; private set; }

        internal int SourceLoadedCount { get; private set; }
    }
}

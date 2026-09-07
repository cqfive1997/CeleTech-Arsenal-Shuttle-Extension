using System;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// The narrow production boundary for advancing a CE-owned VerbTracker. Targeting, power,
    /// cooldown and reload remain with their respective shuttle policies.
    /// </summary>
    internal sealed class CeWeaponVerbTrackerDriver
    {
        internal bool TryTick(Thing gun, out string failureReason)
        {
            failureReason = null;
            CompEquippable equippable = CeWeaponRuntimeGunAccess.GetEquippable(gun);
            if (equippable == null || equippable.verbTracker == null)
            {
                failureReason = "ce-verb-tracker-missing";
                return false;
            }

            try
            {
                equippable.verbTracker.VerbsTick();
                return true;
            }
            catch (Exception exception)
            {
                failureReason = "ce-verb-tracker-tick-exception:" +
                    exception.GetType().Name;
                return false;
            }
        }
    }
}

using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense
{
    internal interface IShuttleDefenseWeaponGroupUIActions
    {
        bool SetAllHoldFire(
            IList<ShuttleDefenseWeaponActionTarget> weapons,
            bool holdFire);

        bool ReloadAll(IList<ShuttleDefenseWeaponActionTarget> weapons);

        bool CancelReloadAll(IList<ShuttleDefenseWeaponActionTarget> weapons);

        bool SetAllFireControlLinked(
            IList<ShuttleDefenseWeaponActionTarget> weapons,
            bool linked);

        bool ClearAllForcedTargets(
            IList<ShuttleDefenseWeaponActionTarget> weapons);

        bool SetAllForcedTarget(
            IList<ShuttleDefenseWeaponActionTarget> weapons,
            LocalTargetInfo target);

        bool SetAllFireControlMode(
            IList<ShuttleDefenseWeaponActionTarget> weapons,
            ShuttleWeaponFireControlMode mode);
    }
}

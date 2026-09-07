using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Defense
{
    internal interface IShuttleDefenseTargeterService
    {
        bool BeginForcedTargeting(
            ShuttleDefenseWeaponActionTarget weapon,
            Action<LocalTargetInfo> onTargetSelected);
    }
}

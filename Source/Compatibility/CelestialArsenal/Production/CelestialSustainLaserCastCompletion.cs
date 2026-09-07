using System;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    internal sealed class CelestialSustainLaserCastCompletion
    {
        private readonly CelestialSustainLaserFireDriver fireDriver;
        private readonly ShuttleWeaponModuleDef weaponDef;
        private readonly ShuttleWeaponRuntimeState state;
        private readonly Verb verb;

        internal CelestialSustainLaserCastCompletion(
            CelestialSustainLaserFireDriver fireDriver,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb verb)
        {
            this.fireDriver = fireDriver;
            this.weaponDef = weaponDef;
            this.state = state;
            this.verb = verb;
        }

        internal void Invoke()
        {
            this.fireDriver.NotifyCastComplete(this.state, this.weaponDef, this.verb);
        }

        internal static bool Matches(
            Action callback,
            CelestialSustainLaserFireDriver fireDriver,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb verb)
        {
            CelestialSustainLaserCastCompletion completion = callback != null
                ? callback.Target as CelestialSustainLaserCastCompletion
                : null;
            return completion != null &&
                ReferenceEquals(completion.fireDriver, fireDriver) &&
                ReferenceEquals(completion.weaponDef, weaponDef) &&
                ReferenceEquals(completion.state, state) &&
                ReferenceEquals(completion.verb, verb);
        }
    }
}

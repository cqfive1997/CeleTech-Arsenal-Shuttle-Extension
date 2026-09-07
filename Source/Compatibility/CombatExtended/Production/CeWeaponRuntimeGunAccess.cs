using System;
using System.Runtime.CompilerServices;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    internal static class CeWeaponRuntimeGunAccess
    {
        private static readonly ConditionalWeakTable<ThingWithComps, CachedComponents>
            ComponentCache =
                new ConditionalWeakTable<ThingWithComps, CachedComponents>();

        internal static CompEquippable GetEquippable(Thing gun)
        {
            ThingWithComps typed;
            CachedComponents cached = GetCachedComponents(gun, out typed);
            if (cached == null)
            {
                return null;
            }

            if (!cached.EquippableResolved)
            {
                cached.Equippable = typed.TryGetComp<CompEquippable>();
                cached.EquippableResolved = true;
            }

            return cached.Equippable;
        }

        internal static CompAmmoUser GetMagazine(Thing gun)
        {
            ThingWithComps typed;
            CachedComponents cached = GetCachedComponents(gun, out typed);
            if (cached == null)
            {
                return null;
            }

            if (!cached.MagazineResolved)
            {
                cached.Magazine = typed.TryGetComp<CompAmmoUser>();
                cached.MagazineResolved = true;
            }

            return cached.Magazine;
        }

        internal static CompFireModes GetFireModes(Thing gun)
        {
            ThingWithComps typed;
            CachedComponents cached = GetCachedComponents(gun, out typed);
            if (cached == null)
            {
                return null;
            }

            if (!cached.FireModesResolved)
            {
                cached.FireModes = typed.TryGetComp<CompFireModes>();
                cached.FireModesResolved = true;
            }

            return cached.FireModes;
        }

        internal static CeWeaponMuzzleVerb GetMuzzleVerb(Thing gun)
        {
            ThingWithComps typed;
            CachedComponents cached = GetCachedComponents(gun, out typed);
            if (cached == null)
            {
                return null;
            }

            if (cached.MuzzleVerb != null)
            {
                return cached.MuzzleVerb;
            }

            CompEquippable equippable = GetEquippable(gun);
            CeWeaponMuzzleVerb verb = equippable != null
                ? equippable.PrimaryVerb as CeWeaponMuzzleVerb
                : null;
            if (verb != null)
            {
                cached.MuzzleVerb = verb;
            }

            return verb;
        }

        internal static void ReleaseVerb(Thing gun)
        {
            CeWeaponMuzzleVerb verb = GetMuzzleVerb(gun);
            if (verb == null)
            {
                return;
            }

            verb.Reset();
            verb.caster = null;
            verb.castCompleteCallback = null;
            verb.ClearRuntimeBinding();
        }

        internal static bool TryBindVerb(
            Thing gun,
            Thing host,
            string moduleInstanceID,
            string parentSlotID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState runtimeState,
            Action completionCallback,
            out CeWeaponMuzzleVerb verb,
            out string failureReason)
        {
            verb = GetMuzzleVerb(gun);
            failureReason = null;
            if (verb == null || host == null || string.IsNullOrEmpty(moduleInstanceID))
            {
                failureReason = "ce-runtime-verb-or-host-missing";
                return false;
            }

            verb.ConfigureRuntimeBinding(
                moduleInstanceID,
                parentSlotID,
                weaponDef,
                runtimeState);
            verb.caster = host;
            verb.castCompleteCallback = completionCallback;
            return true;
        }

        private static CachedComponents GetCachedComponents(
            Thing gun,
            out ThingWithComps typed)
        {
            typed = gun as ThingWithComps;
            return typed != null
                ? ComponentCache.GetOrCreateValue(typed)
                : null;
        }

        private sealed class CachedComponents
        {
            public CachedComponents()
            {
            }

            internal bool EquippableResolved;
            internal CompEquippable Equippable;
            internal bool MagazineResolved;
            internal CompAmmoUser Magazine;
            internal bool FireModesResolved;
            internal CompFireModes FireModes;
            internal CeWeaponMuzzleVerb MuzzleVerb;
        }
    }
}

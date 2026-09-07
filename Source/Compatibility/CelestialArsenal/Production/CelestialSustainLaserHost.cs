using System;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;
namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    /// <summary>
    /// Owns only hidden-gun creation, exact Verb binding and one tracker tick.
    /// </summary>
    internal sealed class CelestialSustainLaserHost
    {
        private readonly ShuttleWeaponMuzzleResolver muzzleResolver;
        private readonly CelestialSustainLaserBindingCache bindingCache =
            new CelestialSustainLaserBindingCache();
        internal CelestialSustainLaserHost(ShuttleWeaponMuzzleResolver muzzleResolver)
        {
            this.muzzleResolver = muzzleResolver;
        }
        internal bool TryEnsureReady(
            ShuttleModuleRuntimeContext context,
            CelestialSustainLaserFireDriver fireDriver,
            out Verb_ShuttleCelestialSustainLaser readyVerb)
        {
            readyVerb = null;
            ThingWithComps shuttle = context != null ? context.Host : null;
            ShuttleWeaponModuleDef weaponDef = context != null
                ? context.ModuleDef as ShuttleWeaponModuleDef
                : null;
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            if (shuttle == null || weaponDef == null || weaponDef.weaponDef == null ||
                state == null || fireDriver == null ||
                state.MagazineAuthorityBackendIdForRuntimeOnly !=
                    ShuttleWeaponRuntimeState.CoreMagazineAuthorityId)
            {
                return false;
            }
            CelestialSustainLaserBindingCache.Binding cachedBinding;
            if (this.bindingCache.TryGetReady(
                    state,
                    shuttle,
                    weaponDef,
                    fireDriver,
                    out cachedBinding))
            {
                readyVerb = cachedBinding.Verb;
                this.RefreshBurstMuzzle(
                    context,
                    weaponDef,
                    state,
                    readyVerb);
                return true;
            }
            state.SanitizeForRuntimeOnly();
            ThingWithComps gun = state.GunForRuntimeOnly as ThingWithComps;
            if (gun == null || gun.Destroyed || gun.def != weaponDef.weaponDef)
            {
                gun = ThingMaker.MakeThing(weaponDef.weaponDef) as ThingWithComps;
                if (gun == null)
                {
                    return false;
                }

                state.GunForRuntimeOnly = gun;
                state.ResetCurrentTargetForRuntimeOnly();
                state.ResetForcedTargetForRuntimeOnly();
                state.ClearVerbBindingForRuntimeOnly();
            }
            CompEquippable equippable = gun.TryGetComp<CompEquippable>();
            Verb_ShuttleCelestialSustainLaser verb = equippable != null
                ? equippable.PrimaryVerb as Verb_ShuttleCelestialSustainLaser
                : null;
            CompShuttleSustainLaserData data =
                gun.TryGetComp<CompShuttleSustainLaserData>();
            if (equippable == null || equippable.verbTracker == null ||
                verb == null || data == null)
            {
                return false;
            }
            Action completionCallback = verb.castCompleteCallback;
            if (!state.AreVerbsBoundForRuntimeOnly(shuttle, gun) ||
                !CelestialSustainLaserCastCompletion.Matches(
                    completionCallback,
                    fireDriver,
                    weaponDef,
                    state,
                    verb))
            {
                verb.caster = shuttle;
                CelestialSustainLaserCastCompletion completion =
                    new CelestialSustainLaserCastCompletion(
                        fireDriver,
                        weaponDef,
                        state,
                        verb);
                completionCallback = completion.Invoke;
                verb.castCompleteCallback = completionCallback;
                state.MarkVerbsBoundForRuntimeOnly(shuttle, gun);
            }
            this.bindingCache.Store(
                state,
                shuttle,
                gun,
                weaponDef,
                fireDriver,
                equippable,
                verb,
                data,
                completionCallback);
            readyVerb = verb;
            this.RefreshBurstMuzzle(context, weaponDef, state, verb);
            return true;
        }

        private void RefreshBurstMuzzle(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb_ShuttleCelestialSustainLaser verb)
        {
            if (verb == null || verb.state != VerbState.Bursting ||
                this.muzzleResolver == null)
            {
                return;
            }
            LocalTargetInfo target = verb.CurrentTarget.IsValid
                ? verb.CurrentTarget
                : state.GetCurrentTargetForRuntimeOnly();
            ShuttleWeaponMuzzleSource source = this.muzzleResolver.Resolve(
                context,
                weaponDef,
                state,
                target);
            verb.SetShuttleMuzzleSource(source.Cell, source.DrawPos);
            state.SetLastResolvedMuzzleForRuntimeOnly(source.Cell, source.DrawPos);
        }
        internal Verb_ShuttleCelestialSustainLaser GetVerb(
            ShuttleModuleRuntimeContext context)
        {
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            CelestialSustainLaserBindingCache.Binding cachedBinding;
            if (this.bindingCache.TryGet(state, out cachedBinding))
            {
                return cachedBinding.Verb;
            }
            return GetVerb(state != null ? state.GunForRuntimeOnly : null);
        }
        internal void TickVerbs(ShuttleModuleRuntimeContext context)
        {
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            CelestialSustainLaserBindingCache.Binding cachedBinding;
            CompEquippable equippable = this.bindingCache.TryGet(
                state,
                out cachedBinding)
                ? cachedBinding.Equippable
                : GetEquippable(state != null ? state.GunForRuntimeOnly : null);
            if (equippable != null && equippable.verbTracker != null)
            {
                equippable.verbTracker.VerbsTick();
            }
        }
        internal void AbortActiveCast(ShuttleModuleRuntimeContext context)
        {
            Verb_ShuttleCelestialSustainLaser verb = this.GetVerb(context);
            if (verb != null && verb.state == VerbState.Bursting)
            {
                verb.Reset();
            }
        }
        private static CompEquippable GetEquippable(Thing gun)
        {
            return gun != null
                ? gun.TryGetComp<CompEquippable>()
                : null;
        }
        private static Verb_ShuttleCelestialSustainLaser GetVerb(Thing gun)
        {
            CompEquippable equippable = GetEquippable(gun);
            return equippable != null
                ? equippable.PrimaryVerb as Verb_ShuttleCelestialSustainLaser
                : null;
        }
    }
}

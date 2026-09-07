using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Owns only the supported hidden-gun creation, Verb binding and one VerbTracker tick seam.
    /// It never selects targets, reserves ammunition or advances shuttle cycle policy.
    /// </summary>
    internal sealed class NativeVerbWeaponHost
    {
        private readonly NativeVerbMuzzleInstaller muzzleInstaller;
        private readonly ConditionalWeakTable<ShuttleWeaponRuntimeState, CachedBinding>
            cachedBindings =
                new ConditionalWeakTable<ShuttleWeaponRuntimeState, CachedBinding>();

        internal NativeVerbWeaponHost()
            : this(new NativeVerbMuzzleInstaller())
        {
        }

        internal NativeVerbWeaponHost(NativeVerbMuzzleInstaller muzzleInstaller)
        {
            this.muzzleInstaller = muzzleInstaller;
        }

        internal bool EnsureReady(
            ShuttleModuleRuntimeContext context,
            NativeVerbWeaponFireDriver fireDriver)
        {
            ThingWithComps host = context != null ? context.Host : null;
            ShuttleWeaponModuleDef weaponDef = context != null
                ? context.ModuleDef as ShuttleWeaponModuleDef
                : null;
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            if (host == null || weaponDef == null || weaponDef.weaponDef == null ||
                state == null || fireDriver == null ||
                state.MagazineAuthorityBackendIdForRuntimeOnly !=
                    ShuttleWeaponRuntimeState.CoreMagazineAuthorityId)
            {
                return false;
            }

            CachedBinding cachedBinding;
            if (this.TryGetCachedBinding(
                state,
                host,
                weaponDef,
                fireDriver,
                out cachedBinding))
            {
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
            CeleTech.ShuttleExtension.ModularShuttle.Weapons.Verb_ShuttleMuzzleShoot muzzleVerb;
            string muzzleFailure = null;
            if (equippable == null)
            {
                muzzleFailure = "native-equippable-comp-missing";
            }
            else if (this.muzzleInstaller == null)
            {
                muzzleFailure = "native-muzzle-installer-missing";
            }

            if (muzzleFailure != null ||
                !this.muzzleInstaller.TryEnsureInstalled(
                    gun,
                    out muzzleVerb,
                    out muzzleFailure))
            {
                Log.WarningOnce(
                    "[CeleTech Shuttle][Native Backend] Hidden-gun muzzle installation failed: " +
                    (muzzleFailure ?? "unknown") + ".",
                    WarningKey(weaponDef.defName, muzzleFailure));
                return false;
            }

            List<Verb> verbs = equippable != null ? equippable.AllVerbs : null;
            if (verbs == null || verbs.Count != 1 || verbs[0] == null)
            {
                return false;
            }

            if (state.AreVerbsBoundForRuntimeOnly(host, gun) &&
                CallbacksMatch(verbs, fireDriver, weaponDef, state))
            {
                this.CacheBinding(
                    state,
                    host,
                    gun,
                    weaponDef,
                    fireDriver,
                    equippable,
                    verbs[0]);
                return true;
            }

            for (int i = 0; i < verbs.Count; i++)
            {
                Verb verb = verbs[i];
                verb.caster = host;
                NativeVerbCastCompletion completion = new NativeVerbCastCompletion(
                    fireDriver,
                    weaponDef,
                    state,
                    verb);
                verb.castCompleteCallback = completion.Invoke;
            }

            state.MarkVerbsBoundForRuntimeOnly(host, gun);
            this.CacheBinding(
                state,
                host,
                gun,
                weaponDef,
                fireDriver,
                equippable,
                verbs[0]);
            return true;
        }

        private bool TryGetCachedBinding(
            ShuttleWeaponRuntimeState state,
            ThingWithComps host,
            ShuttleWeaponModuleDef weaponDef,
            NativeVerbWeaponFireDriver fireDriver,
            out CachedBinding cachedBinding)
        {
            if (state != null &&
                this.cachedBindings.TryGetValue(state, out cachedBinding) &&
                cachedBinding != null &&
                object.ReferenceEquals(cachedBinding.Host, host) &&
                object.ReferenceEquals(cachedBinding.WeaponDef, weaponDef) &&
                object.ReferenceEquals(cachedBinding.FireDriver, fireDriver) &&
                object.ReferenceEquals(cachedBinding.Gun, state.GunForRuntimeOnly) &&
                cachedBinding.Gun != null &&
                !cachedBinding.Gun.Destroyed &&
                cachedBinding.Gun.def == weaponDef.weaponDef &&
                cachedBinding.Equippable != null &&
                cachedBinding.PrimaryVerb != null &&
                state.AreVerbsBoundForRuntimeOnly(host, cachedBinding.Gun))
            {
                return true;
            }

            if (state != null)
            {
                this.cachedBindings.Remove(state);
            }

            cachedBinding = null;
            return false;
        }

        private void CacheBinding(
            ShuttleWeaponRuntimeState state,
            ThingWithComps host,
            ThingWithComps gun,
            ShuttleWeaponModuleDef weaponDef,
            NativeVerbWeaponFireDriver fireDriver,
            CompEquippable equippable,
            Verb primaryVerb)
        {
            if (state == null || host == null || gun == null || weaponDef == null ||
                fireDriver == null || equippable == null || primaryVerb == null)
            {
                return;
            }

            this.cachedBindings.Remove(state);
            this.cachedBindings.Add(
                state,
                new CachedBinding(
                    host,
                    gun,
                    weaponDef,
                    fireDriver,
                    equippable,
                    primaryVerb));
        }

        private static int WarningKey(string defName, string failureReason)
        {
            unchecked
            {
                string text = (defName ?? string.Empty) + "|" +
                    (failureReason ?? string.Empty);
                int hash = 74192317;
                for (int i = 0; i < text.Length; i++)
                {
                    hash = (hash * 397) ^ text[i];
                }

                return hash;
            }
        }

        internal Verb GetPrimaryVerb(ShuttleModuleRuntimeContext context)
        {
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            CachedBinding cachedBinding;
            if (state != null &&
                this.cachedBindings.TryGetValue(state, out cachedBinding) &&
                cachedBinding != null &&
                object.ReferenceEquals(cachedBinding.Gun, state.GunForRuntimeOnly) &&
                cachedBinding.Gun != null &&
                !cachedBinding.Gun.Destroyed &&
                cachedBinding.PrimaryVerb != null &&
                state.AreVerbsBoundForRuntimeOnly(cachedBinding.Host, cachedBinding.Gun))
            {
                return cachedBinding.PrimaryVerb;
            }

            return this.GetPrimaryVerb(state != null ? state.GunForRuntimeOnly : null);
        }

        internal Verb GetPrimaryVerb(Thing gun)
        {
            CompEquippable equippable = gun != null
                ? gun.TryGetComp<CompEquippable>()
                : null;
            return equippable != null ? equippable.PrimaryVerb : null;
        }

        internal void TickVerbTracker(ShuttleModuleRuntimeContext context)
        {
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            Thing gun = state != null ? state.GunForRuntimeOnly : null;
            CachedBinding cachedBinding;
            CompEquippable equippable = state != null &&
                this.cachedBindings.TryGetValue(state, out cachedBinding) &&
                cachedBinding != null &&
                object.ReferenceEquals(cachedBinding.Gun, gun) &&
                cachedBinding.Gun != null &&
                !cachedBinding.Gun.Destroyed &&
                state.AreVerbsBoundForRuntimeOnly(cachedBinding.Host, cachedBinding.Gun)
                    ? cachedBinding.Equippable
                    : (gun != null ? gun.TryGetComp<CompEquippable>() : null);
            if (equippable != null && equippable.verbTracker != null)
            {
                equippable.verbTracker.VerbsTick();
            }
        }

        private sealed class CachedBinding
        {
            internal CachedBinding(
                ThingWithComps host,
                ThingWithComps gun,
                ShuttleWeaponModuleDef weaponDef,
                NativeVerbWeaponFireDriver fireDriver,
                CompEquippable equippable,
                Verb primaryVerb)
            {
                this.Host = host;
                this.Gun = gun;
                this.WeaponDef = weaponDef;
                this.FireDriver = fireDriver;
                this.Equippable = equippable;
                this.PrimaryVerb = primaryVerb;
            }

            internal ThingWithComps Host { get; private set; }

            internal ThingWithComps Gun { get; private set; }

            internal ShuttleWeaponModuleDef WeaponDef { get; private set; }

            internal NativeVerbWeaponFireDriver FireDriver { get; private set; }

            internal CompEquippable Equippable { get; private set; }

            internal Verb PrimaryVerb { get; private set; }
        }

        private static bool CallbacksMatch(
            List<Verb> verbs,
            NativeVerbWeaponFireDriver fireDriver,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state)
        {
            for (int i = 0; i < verbs.Count; i++)
            {
                Verb verb = verbs[i];
                NativeVerbCastCompletion completion = verb != null &&
                    verb.castCompleteCallback != null
                    ? verb.castCompleteCallback.Target as NativeVerbCastCompletion
                    : null;
                if (completion == null ||
                    !completion.Matches(fireDriver, weaponDef, state, verb))
                {
                    return false;
                }
            }

            return true;
        }

        private sealed class NativeVerbCastCompletion
        {
            private readonly NativeVerbWeaponFireDriver fireDriver;
            private readonly ShuttleWeaponModuleDef weaponDef;
            private readonly ShuttleWeaponRuntimeState state;
            private readonly Verb verb;

            internal NativeVerbCastCompletion(
                NativeVerbWeaponFireDriver fireDriver,
                ShuttleWeaponModuleDef weaponDef,
                ShuttleWeaponRuntimeState state,
                Verb verb)
            {
                this.fireDriver = fireDriver;
                this.weaponDef = weaponDef;
                this.state = state;
                this.verb = verb;
            }

            internal bool Matches(
                NativeVerbWeaponFireDriver expectedDriver,
                ShuttleWeaponModuleDef expectedDef,
                ShuttleWeaponRuntimeState expectedState,
                Verb expectedVerb)
            {
                return ReferenceEquals(this.fireDriver, expectedDriver) &&
                    ReferenceEquals(this.weaponDef, expectedDef) &&
                    ReferenceEquals(this.state, expectedState) &&
                    ReferenceEquals(this.verb, expectedVerb);
            }

            internal void Invoke()
            {
                this.fireDriver.NotifyCastComplete(
                    this.state,
                    this.weaponDef,
                    this.verb);
            }
        }
    }
}

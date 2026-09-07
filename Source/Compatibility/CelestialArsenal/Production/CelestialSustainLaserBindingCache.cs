using System;
using System.Runtime.CompilerServices;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    /// <summary>
    /// Holds transient hidden-gun handles for the shared Particle Lance backend. Entries are weakly
    /// keyed by durable runtime state and are rebuilt after load or whenever binding inputs change.
    /// </summary>
    internal sealed class CelestialSustainLaserBindingCache
    {
        private readonly ConditionalWeakTable<ShuttleWeaponRuntimeState, Binding> bindings =
            new ConditionalWeakTable<ShuttleWeaponRuntimeState, Binding>();

        internal bool TryGetReady(
            ShuttleWeaponRuntimeState state,
            ThingWithComps host,
            ShuttleWeaponModuleDef weaponDef,
            CelestialSustainLaserFireDriver fireDriver,
            out Binding binding)
        {
            if (this.TryGet(state, out binding) &&
                ReferenceEquals(binding.Host, host) &&
                ReferenceEquals(binding.WeaponDef, weaponDef) &&
                ReferenceEquals(binding.FireDriver, fireDriver) &&
                weaponDef != null &&
                binding.Gun.def == weaponDef.weaponDef)
            {
                return true;
            }

            this.Remove(state);
            binding = null;
            return false;
        }

        internal bool TryGet(
            ShuttleWeaponRuntimeState state,
            out Binding binding)
        {
            if (state != null &&
                this.bindings.TryGetValue(state, out binding) &&
                binding != null &&
                ReferenceEquals(binding.Gun, state.GunForRuntimeOnly) &&
                binding.Gun != null &&
                !binding.Gun.Destroyed &&
                binding.Host != null &&
                binding.WeaponDef != null &&
                binding.Gun.def == binding.WeaponDef.weaponDef &&
                binding.Equippable != null &&
                binding.Equippable.verbTracker != null &&
                binding.Verb != null &&
                ReferenceEquals(binding.Equippable.PrimaryVerb, binding.Verb) &&
                ReferenceEquals(binding.Verb.caster, binding.Host) &&
                binding.Data != null &&
                binding.CompletionCallback != null &&
                ReferenceEquals(binding.Verb.castCompleteCallback, binding.CompletionCallback) &&
                state.AreVerbsBoundForRuntimeOnly(binding.Host, binding.Gun))
            {
                return true;
            }

            this.Remove(state);
            binding = null;
            return false;
        }

        internal void Store(
            ShuttleWeaponRuntimeState state,
            ThingWithComps host,
            ThingWithComps gun,
            ShuttleWeaponModuleDef weaponDef,
            CelestialSustainLaserFireDriver fireDriver,
            CompEquippable equippable,
            Verb_ShuttleCelestialSustainLaser verb,
            CompShuttleSustainLaserData data,
            Action completionCallback)
        {
            if (state == null || host == null || gun == null || weaponDef == null ||
                fireDriver == null || equippable == null || verb == null || data == null ||
                completionCallback == null)
            {
                return;
            }

            this.Remove(state);
            this.bindings.Add(
                state,
                new Binding(
                    host,
                    gun,
                    weaponDef,
                    fireDriver,
                    equippable,
                    verb,
                    data,
                    completionCallback));
        }

        private void Remove(ShuttleWeaponRuntimeState state)
        {
            if (state != null)
            {
                this.bindings.Remove(state);
            }
        }

        internal sealed class Binding
        {
            internal Binding(
                ThingWithComps host,
                ThingWithComps gun,
                ShuttleWeaponModuleDef weaponDef,
                CelestialSustainLaserFireDriver fireDriver,
                CompEquippable equippable,
                Verb_ShuttleCelestialSustainLaser verb,
                CompShuttleSustainLaserData data,
                Action completionCallback)
            {
                this.Host = host;
                this.Gun = gun;
                this.WeaponDef = weaponDef;
                this.FireDriver = fireDriver;
                this.Equippable = equippable;
                this.Verb = verb;
                this.Data = data;
                this.CompletionCallback = completionCallback;
            }

            internal ThingWithComps Host { get; private set; }

            internal ThingWithComps Gun { get; private set; }

            internal ShuttleWeaponModuleDef WeaponDef { get; private set; }

            internal CelestialSustainLaserFireDriver FireDriver { get; private set; }

            internal CompEquippable Equippable { get; private set; }

            internal Verb_ShuttleCelestialSustainLaser Verb { get; private set; }

            internal CompShuttleSustainLaserData Data { get; private set; }

            internal Action CompletionCallback { get; private set; }
        }
    }
}

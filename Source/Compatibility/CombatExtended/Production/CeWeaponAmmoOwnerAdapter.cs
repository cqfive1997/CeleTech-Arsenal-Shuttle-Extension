using System;
using System.Collections.Generic;
using CombatExtended;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Supplies the minimal CE turret-owner context required by CompAmmoUser. It neither searches
    /// targets nor owns weapon policy; the shuttle remains the Verb caster and command authority.
    /// </summary>
    internal sealed class CeWeaponAmmoOwnerAdapter : IThingHolder
    {
        private Thing host;
        private readonly ThingOwner<Thing> heldThings;
        private readonly CeWeaponTurretOwnerToken token;

        private CeWeaponAmmoOwnerAdapter(Thing host)
        {
            this.host = host;
            this.heldThings = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            this.token = new CeWeaponTurretOwnerToken();
            this.token.def = host.def;
            this.token.OwnerAdapter = this;
            this.Refresh(host);

            if (!this.heldThings.TryAdd(this.token, false))
            {
                throw new InvalidOperationException(
                    "The CE owner token could not enter its transient holder.");
            }
        }

        internal Building_Turret Turret
        {
            get { return this.token; }
        }

        internal bool ContextMatches(Thing expectedHost)
        {
            return expectedHost != null &&
                ReferenceEquals(this.host, expectedHost) &&
                !this.token.Spawned &&
                this.token.Position == expectedHost.Position &&
                this.token.MapHeld == expectedHost.MapHeld &&
                this.token.Faction == expectedHost.Faction;
        }

        public IThingHolder ParentHolder
        {
            get { return this.host != null ? this.host.MapHeld : null; }
        }

        internal static bool TryEnsure(
            CompAmmoUser magazine,
            Thing host,
            out CeWeaponAmmoOwnerAdapter owner,
            out string failureReason)
        {
            owner = null;
            failureReason = null;
            if (magazine == null || host == null || !host.Spawned)
            {
                failureReason = "ce-magazine-or-spawned-host-missing";
                return false;
            }

            CeWeaponTurretOwnerToken existing = magazine.turret as CeWeaponTurretOwnerToken;
            if (existing != null && existing.OwnerAdapter != null)
            {
                owner = existing.OwnerAdapter;
                owner.Refresh(host);
                if (owner.ContextMatches(host))
                {
                    return true;
                }

                owner = null;
                failureReason = "ce-owner-context-mismatch";
                return false;
            }

            if (magazine.turret != null)
            {
                failureReason = "ce-magazine-has-foreign-owner";
                return false;
            }

            try
            {
                owner = new CeWeaponAmmoOwnerAdapter(host);
                magazine.turret = owner.Turret;
                return owner.ContextMatches(host);
            }
            catch (Exception exception)
            {
                owner = null;
                failureReason = "ce-owner-attach-exception:" + exception.GetType().Name;
                return false;
            }
        }

        internal void Refresh(Thing nextHost)
        {
            if (nextHost == null)
            {
                return;
            }

            this.host = nextHost;
            this.token.Position = nextHost.Position;
            if (this.token.Faction != nextHost.Faction)
            {
                this.token.SetFactionDirect(nextHost.Faction);
            }
        }

        internal void Release(CompAmmoUser magazine)
        {
            if (magazine != null && ReferenceEquals(magazine.turret, this.token))
            {
                magazine.turret = null;
            }

            if (this.heldThings.Contains(this.token))
            {
                this.heldThings.Remove(this.token);
            }

            this.token.OwnerAdapter = null;
            this.host = null;
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return this.heldThings;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
        }
    }

    internal sealed class CeWeaponTurretOwnerToken : Building_Turret
    {
        internal CeWeaponAmmoOwnerAdapter OwnerAdapter { get; set; }

        public override LocalTargetInfo CurrentTarget
        {
            get { return LocalTargetInfo.Invalid; }
        }

        public override Verb AttackVerb
        {
            get { return null; }
        }

        public override void OrderAttack(LocalTargetInfo targ)
        {
            throw new InvalidOperationException(
                "The CE magazine owner token cannot control shuttle firing.");
        }
    }
}

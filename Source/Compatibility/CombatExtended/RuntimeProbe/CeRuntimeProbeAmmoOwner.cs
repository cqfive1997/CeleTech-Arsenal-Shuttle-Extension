using System;
using System.Collections.Generic;
using CombatExtended;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal sealed class CeRuntimeProbeAmmoOwner : IThingHolder
    {
        private readonly Thing host;
        private readonly ThingOwner<Thing> heldThings;
        private readonly CeRuntimeProbeTurretToken turretToken;

        private CeRuntimeProbeAmmoOwner(Thing host)
        {
            this.host = host;
            this.heldThings = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            this.turretToken = new CeRuntimeProbeTurretToken();
            this.turretToken.def = host.def;
            this.RefreshContext();

            if (!this.heldThings.TryAdd(this.turretToken, false))
            {
                throw new InvalidOperationException(
                    "The CE owner token could not enter its transient holder.");
            }
        }

        internal Building_Turret Turret
        {
            get { return this.turretToken; }
        }

        internal bool ContextMatches
        {
            get
            {
                return this.host != null &&
                    !this.turretToken.Spawned &&
                    this.turretToken.Position == this.host.Position &&
                    this.turretToken.MapHeld == this.host.MapHeld;
            }
        }

        public IThingHolder ParentHolder
        {
            get { return this.host != null ? this.host.MapHeld : null; }
        }

        internal static bool TryAttach(
            CompAmmoUser ammo,
            Thing host,
            out CeRuntimeProbeAmmoOwner owner,
            out string failure)
        {
            owner = null;
            failure = null;
            if (ammo == null || host == null || !host.Spawned)
            {
                failure = "The CE magazine or spawned shuttle host is unavailable.";
                return false;
            }

            if (ammo.turret != null)
            {
                failure = "The CE magazine already has a turret owner; the probe will not replace it.";
                return false;
            }

            try
            {
                owner = new CeRuntimeProbeAmmoOwner(host);
                ammo.turret = owner.Turret;
                return true;
            }
            catch (Exception exception)
            {
                failure = "The CE owner token could not be attached: " +
                    exception.GetType().Name + ": " + exception.Message;
                owner = null;
                return false;
            }
        }

        internal void RefreshContext()
        {
            if (this.host == null)
            {
                return;
            }

            this.turretToken.Position = this.host.Position;
            if (this.turretToken.Faction != this.host.Faction)
            {
                this.turretToken.SetFactionDirect(this.host.Faction);
            }
        }

        internal void Release(CompAmmoUser ammo)
        {
            if (ammo != null && ReferenceEquals(ammo.turret, this.turretToken))
            {
                ammo.turret = null;
            }

            if (this.heldThings.Contains(this.turretToken))
            {
                this.heldThings.Remove(this.turretToken);
            }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return this.heldThings;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
        }
    }

    internal sealed class CeRuntimeProbeTurretToken : Building_Turret
    {
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
                "The CE runtime-probe owner token cannot control firing.");
        }
    }
}

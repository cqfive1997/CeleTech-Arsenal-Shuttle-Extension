using System.Text;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal static class CeRuntimeProbeReportWriter
    {
        internal static void Write(
            CeRuntimeProbeSession session,
            string phase,
            string note)
        {
            if (session == null)
            {
                Log.Message("[CeleTech Shuttle][CE Runtime Probe] phase=" + phase + " session=null");
                return;
            }

            ThingWithComps gun = session.Gun;
            CompAmmoUser ammo = session.GetAmmo();
            CompFireModes modes = session.GetModes();
            Verb_ShootCE verb = CeRuntimeProbeGunAccess.GetVerb(gun);
            StringBuilder builder = new StringBuilder(420);
            builder.Append("[CeleTech Shuttle][CE Runtime Probe]");
            Append(builder, "phase", phase);
            Append(builder, "weapon", gun != null ? gun.def.defName : "null");
            Append(builder, "host", session.Host != null ? session.Host.LabelShortCap : "null");
            Append(builder, "target", session.TargetDescription);
            Append(builder, "castAttempted", session.CastAttempted);
            Append(builder, "castAccepted", session.CastAccepted);
            Append(builder, "active", session.Active);
            Append(builder, "trackerTicks", session.TrackerTickCount);
            Append(builder, "callbacks", session.CompletionCallbackCount);
            Append(builder, "initialLoaded", session.InitialLoadedCount);
            Append(builder, "loaded", ammo != null ? ammo.CurMagCount : -1);
            Append(builder, "consumed", ammo != null
                ? session.InitialLoadedCount - ammo.CurMagCount
                : -1);
            Append(builder, "magSize", ammo != null ? ammo.MagSize : -1);
            Append(builder, "selectedAmmo", ammo != null && ammo.SelectedAmmo != null
                ? ammo.SelectedAmmo.defName
                : "null");
            Append(builder, "ownerAdapter", session.UseAmmoOwnerAdapter);
            Append(builder, "burstRequested", session.UseBurstFire);
            Append(builder, "shotsPerBurst", verb != null ? verb.ShotsPerBurst : -1);
            Append(builder, "holdForSaveLoad", session.HoldBurstForSaveLoad);
            Append(builder, "resumeAuthorized", session.BurstResumeAuthorized);
            Append(builder, "postLoadRebindPending", session.PostLoadRebindPending);
            Append(builder, "postLoadRebindAttempts", session.PostLoadRebindAttemptCount);
            Append(builder, "verbState", verb != null ? verb.state.ToString() : "null");
            Append(builder, "verbBursting", verb != null && verb.Bursting);
            Append(builder, "ownerAttached", ammo != null &&
                session.AmmoOwner != null &&
                ReferenceEquals(ammo.turret, session.AmmoOwner.Turret));
            Append(builder, "ownerContext", session.AmmoOwner != null &&
                session.AmmoOwner.ContextMatches);
            Append(builder, "isEquippedGun", ammo != null && ammo.IsEquippedGun);
            Append(builder, "holder", ammo != null && ammo.Holder != null
                ? ammo.Holder.LabelShortCap
                : "null");
            Append(builder, "turret", ammo != null && ammo.turret != null
                ? ammo.turret.GetType().Name
                : "null");
            Append(builder, "fireMode", modes != null ? modes.CurrentFireMode.ToString() : "null");
            Append(builder, "aimMode", modes != null ? modes.CurrentAimMode.ToString() : "null");
            Append(
                builder,
                "savedSnapshotMatches",
                phase == "post-load"
                    ? session.PersistedSnapshotMatches.ToString()
                    : "not-checked");
            if (!string.IsNullOrEmpty(note))
            {
                Append(builder, "note", note);
            }

            Log.Message(builder.ToString());
        }

        private static void Append(StringBuilder builder, string key, object value)
        {
            builder.Append(' ');
            builder.Append(key);
            builder.Append('=');
            builder.Append(value ?? "null");
        }
    }
}

using System;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal static class CeRuntimeProbeSessionRunner
    {
        private const int MaxPostLoadRebindAttempts = 60;

        internal static bool TryCreateAndStart(
            Thing host,
            CeRuntimeProbeCandidate candidate,
            LocalTargetInfo target,
            bool useAmmoOwnerAdapter,
            bool useBurstFire,
            bool holdBurstForSaveLoad,
            out CeRuntimeProbeSession session,
            out string failure)
        {
            session = null;
            failure = null;
            if (host == null || !host.Spawned)
            {
                failure = "The selected shuttle host is not spawned.";
                return false;
            }

            ThingWithComps gun;
            if (!CeRuntimeProbeGunFactory.TryCreate(
                candidate,
                useAmmoOwnerAdapter && !useBurstFire ? 1 : -1,
                useBurstFire,
                out gun,
                out failure))
            {
                return false;
            }

            session = new CeRuntimeProbeSession(
                gun,
                host,
                target,
                useAmmoOwnerAdapter,
                useBurstFire,
                holdBurstForSaveLoad);
            return TryStart(session, out failure);
        }

        internal static void Tick(CeRuntimeProbeSession session, int maxTrackerTicks)
        {
            if (session.PostLoadRebindPending && !TryCompletePostLoadRebind(session))
            {
                return;
            }

            if (session.PostLoadReportPending)
            {
                session.PostLoadReportPending = false;
                CeRuntimeProbeReportWriter.Write(session, "post-load", null);
            }

            if (!session.Active)
            {
                return;
            }

            if (session.HoldBurstForSaveLoad && !session.BurstResumeAuthorized)
            {
                return;
            }

            CompEquippable equippable = CeRuntimeProbeGunAccess.GetEquippable(session.Gun);
            if (equippable == null || session.Host == null || !session.Host.Spawned)
            {
                Stop(session, "The hidden gun or shuttle host became unavailable.");
                return;
            }

            session.TrackerTickCount++;
            equippable.verbTracker.VerbsTick();

            if (session.CompletionReportPending)
            {
                session.CompletionReportPending = false;
                session.CaptureExpectedState();
                CeRuntimeProbeReportWriter.Write(session, "cast-complete", null);
                return;
            }

            if (session.TrackerTickCount >= maxTrackerTicks)
            {
                Stop(session, "The bounded probe timed out before CE reported cast completion.");
            }
        }

        internal static bool Rebind(
            CeRuntimeProbeSession session,
            out string failure)
        {
            failure = null;
            if (session == null)
            {
                failure = "The CE runtime-probe session is unavailable.";
                return false;
            }

            if (session.UseAmmoOwnerAdapter &&
                !EnsureAmmoOwner(session, out failure))
            {
                return false;
            }

            if (!CeRuntimeProbeGunAccess.TryBind(
                session.Gun,
                session.Host,
                delegate { NotifyCastComplete(session); }))
            {
                failure = "The hidden CE Verb could not bind to the loaded shuttle host.";
                return false;
            }

            return true;
        }

        internal static void Release(CeRuntimeProbeSession session)
        {
            if (session == null)
            {
                return;
            }

            CeRuntimeProbeAmmoOwner owner = session.AmmoOwner;
            if (owner != null)
            {
                owner.Release(session.GetAmmo());
                session.AmmoOwner = null;
            }

            CeRuntimeProbeGunAccess.Release(session.Gun);
            session.ClearReferences();
        }

        internal static bool ResumeHeldBurst(
            CeRuntimeProbeSession session,
            out string failure)
        {
            failure = null;
            if (session == null || !session.Active)
            {
                failure = "No active CE burst is waiting to resume.";
                return false;
            }

            if (!session.HoldBurstForSaveLoad)
            {
                failure = "The current CE probe is not a held save/load burst.";
                return false;
            }

            if (session.BurstResumeAuthorized)
            {
                failure = "The held CE burst has already been resumed.";
                return false;
            }

            Verb_ShootCE verb = CeRuntimeProbeGunAccess.GetVerb(session.Gun);
            if (verb == null || verb.state != VerbState.Bursting || !verb.Bursting)
            {
                failure = "The loaded CE Verb is no longer in its active burst state.";
                CeRuntimeProbeReportWriter.Write(session, "resume-rejected", failure);
                return false;
            }

            session.BurstResumeAuthorized = true;
            CeRuntimeProbeReportWriter.Write(session, "resume-requested", null);
            return true;
        }

        private static bool TryStart(
            CeRuntimeProbeSession session,
            out string failure)
        {
            failure = null;
            Verb_ShootCE verb = CeRuntimeProbeGunAccess.GetVerb(session.Gun);
            CompAmmoUser ammo = session.GetAmmo();
            if (verb == null || ammo == null)
            {
                failure = "The constructed hidden gun lost its CE Verb or magazine.";
                return false;
            }

            if (!Rebind(session, out failure))
            {
                if (string.IsNullOrEmpty(failure))
                {
                    failure = "The hidden CE Verb could not bind to the selected host.";
                }

                return false;
            }

            session.InitialLoadedCount = ammo.CurMagCount;
            session.CastAttempted = true;
            session.Active = true;

            try
            {
                session.CastAccepted = verb.TryStartCastOn(
                    session.ResolveTarget(),
                    false,
                    true,
                    false,
                    false);
            }
            catch (Exception exception)
            {
                session.Active = false;
                failure = "CE cast threw " + exception.GetType().Name + ": " + exception.Message;
                CeRuntimeProbeReportWriter.Write(session, "cast-exception", failure);
                return false;
            }

            if (!session.CastAccepted)
            {
                session.Active = false;
                failure = "CE rejected TryStartCastOn for the selected host/target.";
                CeRuntimeProbeReportWriter.Write(session, "cast-rejected", failure);
                return false;
            }

            if (session.CompletionReportPending)
            {
                session.CompletionReportPending = false;
                session.CaptureExpectedState();
                CeRuntimeProbeReportWriter.Write(session, "cast-complete-immediate", null);
            }
            else
            {
                CeRuntimeProbeReportWriter.Write(session, "cast-started", null);
            }

            return true;
        }

        private static void NotifyCastComplete(CeRuntimeProbeSession session)
        {
            session.CallbackObserved = true;
            session.CompletionCallbackCount++;
            session.Active = false;
            session.CompletionReportPending = true;
        }

        private static void Stop(CeRuntimeProbeSession session, string reason)
        {
            session.Active = false;
            session.CaptureExpectedState();
            CeRuntimeProbeReportWriter.Write(session, "stopped", reason);
        }

        private static bool TryCompletePostLoadRebind(CeRuntimeProbeSession session)
        {
            string failure;
            if (Rebind(session, out failure))
            {
                session.PostLoadRebindPending = false;
                session.PostLoadRebindAttemptCount = 0;
                return true;
            }

            session.PostLoadRebindAttemptCount++;
            if (session.PostLoadRebindAttemptCount < MaxPostLoadRebindAttempts)
            {
                return false;
            }

            session.PostLoadRebindPending = false;
            session.PostLoadReportPending = false;
            session.Active = false;
            CeRuntimeProbeReportWriter.Write(
                session,
                "post-load-rebind-failed",
                failure ?? "The loaded host did not become available for rebinding.");
            return false;
        }

        private static bool EnsureAmmoOwner(
            CeRuntimeProbeSession session,
            out string failure)
        {
            failure = null;
            CeRuntimeProbeAmmoOwner owner = session.AmmoOwner;
            CompAmmoUser ammo = session.GetAmmo();
            if (owner != null)
            {
                owner.RefreshContext();
                if (ammo != null && ReferenceEquals(ammo.turret, owner.Turret))
                {
                    return true;
                }

                failure = "The CE owner token was detached from the magazine.";
                return false;
            }

            if (!CeRuntimeProbeAmmoOwner.TryAttach(
                ammo,
                session.Host,
                out owner,
                out failure))
            {
                return false;
            }

            session.AmmoOwner = owner;
            return true;
        }
    }
}

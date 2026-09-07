using System;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal static class CeRuntimeProbeMuzzleSessionRunner
    {
        private const int MaxTrackerTicks = 2000;

        internal static bool TryCreateAndStart(
            Thing host,
            CeRuntimeProbeCandidate candidate,
            LocalTargetInfo target,
            CeRuntimeProbeMuzzleSource muzzleSource,
            out CeRuntimeProbeMuzzleSession session,
            out string failure)
        {
            session = null;
            failure = null;
            if (host == null || !host.Spawned)
            {
                failure = "The selected shuttle host is not spawned.";
                return false;
            }

            if (muzzleSource == null || !muzzleSource.IsValidFor(host))
            {
                failure = "The selected muzzle point is invalid for the shuttle map.";
                return false;
            }

            ThingWithComps gun;
            if (!CeRuntimeProbeGunFactory.TryCreate(
                candidate,
                -1,
                false,
                out gun,
                out failure))
            {
                return false;
            }

            CeRuntimeProbeMuzzleObservation observation =
                new CeRuntimeProbeMuzzleObservation();
            CeRuntimeProbeMuzzleVerb muzzleVerb;
            if (!CeRuntimeProbeMuzzleVerbInstaller.TryInstall(
                gun,
                muzzleSource,
                observation,
                out muzzleVerb,
                out failure))
            {
                return false;
            }

            CeRuntimeProbeMuzzleSession createdSession = new CeRuntimeProbeMuzzleSession(
                gun,
                host,
                target,
                muzzleSource,
                observation);
            session = createdSession;

            CeRuntimeProbeAmmoOwner owner;
            if (!CeRuntimeProbeAmmoOwner.TryAttach(
                createdSession.GetAmmo(),
                host,
                out owner,
                out failure))
            {
                return false;
            }

            createdSession.AmmoOwner = owner;
            if (!CeRuntimeProbeGunAccess.TryBind(
                gun,
                host,
                delegate { NotifyCastComplete(createdSession); }))
            {
                failure = "The muzzle-probe CE Verb could not bind to the shuttle host.";
                return false;
            }

            return TryStart(createdSession, muzzleVerb, out failure);
        }

        internal static void Tick(CeRuntimeProbeMuzzleSession session)
        {
            if (session == null || !session.Active)
            {
                return;
            }

            CompEquippable equippable = CeRuntimeProbeGunAccess.GetEquippable(session.Gun);
            if (equippable == null || session.Host == null || !session.Host.Spawned)
            {
                Stop(session, "The hidden muzzle gun or shuttle host became unavailable.");
                return;
            }

            session.TrackerTickCount++;
            equippable.verbTracker.VerbsTick();
            if (session.CompletionReportPending)
            {
                session.CompletionReportPending = false;
                session.Report("muzzle-cast-complete", null);
                return;
            }

            if (session.TrackerTickCount >= MaxTrackerTicks)
            {
                Stop(session, "The bounded muzzle probe timed out before cast completion.");
            }
        }

        internal static void Release(CeRuntimeProbeMuzzleSession session)
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
            session.Active = false;
        }

        private static bool TryStart(
            CeRuntimeProbeMuzzleSession session,
            CeRuntimeProbeMuzzleVerb verb,
            out string failure)
        {
            failure = null;
            CompAmmoUser ammo = session.GetAmmo();
            if (verb == null || ammo == null)
            {
                failure = "The muzzle probe lost its typed CE Verb or magazine.";
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
                failure = "CE muzzle cast threw " + exception.GetType().Name +
                    ": " + exception.Message;
                session.Report("muzzle-cast-exception", failure);
                return false;
            }

            if (!session.CastAccepted)
            {
                session.Active = false;
                failure = "CE rejected TryStartCastOn for the selected muzzle/target.";
                session.Report("muzzle-cast-rejected", failure);
                return false;
            }

            if (session.CompletionReportPending)
            {
                session.CompletionReportPending = false;
                session.Report("muzzle-cast-complete-immediate", null);
            }
            else
            {
                session.Report("muzzle-cast-started", null);
            }

            return true;
        }

        private static void NotifyCastComplete(CeRuntimeProbeMuzzleSession session)
        {
            session.CompletionCallbackCount++;
            session.Active = false;
            session.CompletionReportPending = true;
        }

        private static void Stop(CeRuntimeProbeMuzzleSession session, string reason)
        {
            session.Active = false;
            session.Report("muzzle-stopped", reason);
        }
    }
}

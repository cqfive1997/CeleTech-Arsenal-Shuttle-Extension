using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal static class CeRuntimeProbeReloadSessionRunner
    {
        private const int MaxPostLoadRebindFailures = 60;

        internal static bool TryCreate(
            Thing host,
            CeRuntimeProbeCandidate candidate,
            out CeRuntimeProbeReloadSession session,
            out string failure)
        {
            session = null;
            failure = null;
            if (host == null || !host.Spawned)
            {
                failure = "The selected shuttle host is not spawned.";
                return false;
            }

            if (candidate == null || candidate.InitialAmmo == null)
            {
                failure = "The CE reload candidate has no selected ammunition.";
                return false;
            }

            if (candidate.InitialAmmo.ammoCount != 1)
            {
                failure = "The first reload probe supports only CE ammunition whose ammoCount is exactly one.";
                return false;
            }

            ThingWithComps gun;
            if (!CeRuntimeProbeGunFactory.TryCreate(
                candidate,
                0,
                false,
                out gun,
                out failure))
            {
                return false;
            }

            CompAmmoUser ammo = CeRuntimeProbeGunAccess.GetAmmo(gun);
            if (ammo == null || !ammo.UseAmmo || !ammo.HasMagazine || ammo.MagSize <= 1)
            {
                failure = "The CE reload candidate does not expose an active multi-round magazine.";
                CeRuntimeProbeGunAccess.Release(gun);
                return false;
            }

            ammo.CurrentAmmo = candidate.InitialAmmo;
            ammo.SelectedAmmo = candidate.InitialAmmo;
            ammo.CurMagCount = 0;

            session = new CeRuntimeProbeReloadSession(gun, host);
            if (!Rebind(session, out failure))
            {
                return false;
            }

            session.Active = true;
            session.CaptureExpectedState();
            session.Report("reload-started", null, null);
            return true;
        }

        internal static void Tick(CeRuntimeProbeReloadSession session)
        {
            if (session == null || !session.PostLoadRebindPending)
            {
                return;
            }

            string failure;
            if (Rebind(session, out failure))
            {
                session.PostLoadRebindPending = false;
                if (session.PostLoadReportPending)
                {
                    session.PostLoadReportPending = false;
                    session.Report("reload-post-load", null, null);
                }

                return;
            }

            session.PostLoadRebindFailureCount++;
            if (session.PostLoadRebindFailureCount < MaxPostLoadRebindFailures)
            {
                return;
            }

            session.PostLoadRebindPending = false;
            session.PostLoadReportPending = false;
            session.Active = false;
            session.Report(
                "reload-post-load-rebind-failed",
                null,
                failure ?? "The loaded host did not become available for reload-probe rebinding.");
        }

        internal static bool IsReadyForAction(
            CeRuntimeProbeReloadSession session,
            out string failure)
        {
            failure = null;
            if (session == null || !session.Active)
            {
                failure = "No active CE reload probe is available.";
                return false;
            }

            if (session.PostLoadRebindPending)
            {
                failure = "The CE reload probe is still restoring its owner context.";
                return false;
            }

            CompAmmoUser ammo = session.GetAmmo();
            if (ammo == null || session.Host == null || !session.Host.Spawned)
            {
                failure = "The CE magazine or selected shuttle host is unavailable.";
                return false;
            }

            if (session.AmmoOwner == null ||
                !ReferenceEquals(ammo.turret, session.AmmoOwner.Turret) ||
                !session.AmmoOwner.ContextMatches)
            {
                failure = "The CE reload probe lost its typed owner context.";
                return false;
            }

            return true;
        }

        internal static void Release(CeRuntimeProbeReloadSession session)
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

        private static bool Rebind(
            CeRuntimeProbeReloadSession session,
            out string failure)
        {
            failure = null;
            if (session == null)
            {
                failure = "The CE reload-probe session is unavailable.";
                return false;
            }

            CompAmmoUser ammo = session.GetAmmo();
            CeRuntimeProbeAmmoOwner owner = session.AmmoOwner;
            if (owner != null)
            {
                owner.RefreshContext();
                if (ammo == null || !ReferenceEquals(ammo.turret, owner.Turret))
                {
                    failure = "The CE owner token was detached from the reload-probe magazine.";
                    return false;
                }
            }
            else if (!CeRuntimeProbeAmmoOwner.TryAttach(
                ammo,
                session.Host,
                out owner,
                out failure))
            {
                return false;
            }
            else
            {
                session.AmmoOwner = owner;
            }

            if (!CeRuntimeProbeGunAccess.TryBind(session.Gun, session.Host, null))
            {
                failure = "The hidden CE Verb could not bind to the loaded shuttle host.";
                return false;
            }

            return true;
        }
    }
}

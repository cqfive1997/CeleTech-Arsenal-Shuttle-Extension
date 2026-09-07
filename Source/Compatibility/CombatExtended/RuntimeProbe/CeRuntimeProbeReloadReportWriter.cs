using System;
using System.Text;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal static class CeRuntimeProbeReloadReportWriter
    {
        internal static void Write(
            CeRuntimeProbeReloadSession session,
            string phase,
            CeRuntimeProbeReloadOperation operation,
            string note)
        {
            if (session == null)
            {
                Log.Message("[CeleTech Shuttle][CE Reload Probe] phase=" + phase + " session=null");
                return;
            }

            CompAmmoUser ammo = session.GetAmmo();
            StringBuilder builder = new StringBuilder(520);
            builder.Append("[CeleTech Shuttle][CE Reload Probe]");
            Append(builder, "phase", phase);
            Append(builder, "weapon", session.Gun != null
                ? session.Gun.def.defName
                : "null");
            Append(builder, "host", session.Host != null
                ? session.Host.LabelShortCap
                : "null");
            Append(builder, "active", session.Active);
            Append(builder, "loaded", ammo != null ? ammo.CurMagCount : -1);
            Append(builder, "magSize", ammo != null ? ammo.MagSize : -1);
            Append(builder, "missing", ammo != null
                ? Math.Max(0, ammo.MagSize - ammo.CurMagCount)
                : -1);
            Append(builder, "full", ammo != null && ammo.FullMagazine);
            Append(builder, "currentAmmo", DefName(ammo != null ? ammo.CurrentAmmo : null));
            Append(builder, "selectedAmmo", DefName(ammo != null ? ammo.SelectedAmmo : null));
            Append(builder, "selectedAmmoClass", ammo != null && ammo.SelectedAmmo != null
                ? ammo.SelectedAmmo.GetType().FullName
                : "null");
            Append(builder, "selectedAmmoSet", DefName(ammo != null ? ammo.CurAmmoSet : null));
            Append(builder, "selectedAmmoPerThing", ammo != null && ammo.SelectedAmmo != null
                ? ammo.SelectedAmmo.ammoCount
                : -1);
            Append(builder, "magazineAuthority", "CombatExtended.CompAmmoUser");
            Append(builder, "requiredCargoThingDef", DefName(ammo != null
                ? ammo.SelectedAmmo
                : null));
            Append(builder, "stagedAmmo", DefName(session.StagedAmmo));
            Append(builder, "requested", session.RequestedCount);
            Append(builder, "staged", session.StagedCount);
            Append(builder, "shortfall", Math.Max(0, session.RequestedCount - session.StagedCount));
            Append(builder, "ownerAttached", ammo != null &&
                session.AmmoOwner != null &&
                ReferenceEquals(ammo.turret, session.AmmoOwner.Turret));
            Append(builder, "ownerContext", session.AmmoOwner != null &&
                session.AmmoOwner.ContextMatches);
            Append(builder, "postLoadRebindPending", session.PostLoadRebindPending);
            Append(builder, "postLoadRebindFailures", session.PostLoadRebindFailureCount);
            Append(
                builder,
                "savedSnapshotMatches",
                phase == "reload-post-load"
                    ? session.PersistedSnapshotMatches.ToString()
                    : "not-checked");
            Append(
                builder,
                "sourceModel",
                operation != null && !string.IsNullOrEmpty(operation.SourceModel)
                    ? operation.SourceModel
                    : "detached-count");
            Append(builder, "commitPath", "direct-ce-magazine");
            Append(builder, "ceReloadEntryPointInvoked", false);
            Append(builder, "pawnReloadJobCreated", false);

            if (operation != null)
            {
                Append(builder, "opRequested", operation.RequestedCount);
                Append(builder, "opOffered", operation.OfferedCount);
                Append(builder, "opStagedBefore", operation.StagedBefore);
                Append(builder, "opStagedAfter", operation.StagedAfter);
                Append(builder, "opLoadedBefore", operation.LoadedBefore);
                Append(builder, "opLoadedAfter", operation.LoadedAfter);
                Append(builder, "opCommitted", operation.CommittedCount);
                Append(builder, "opRolledBack", operation.RolledBackCount);
                Append(builder, "magazineMutationRolledBack", operation.MagazineMutationRolledBack);
                if (!string.IsNullOrEmpty(operation.SourceModel))
                {
                    Append(builder, "cargoStoredBefore", operation.CargoStoredBefore);
                    Append(builder, "cargoStoredAfter", operation.CargoStoredAfter);
                    Append(builder, "cargoWithdrawn", operation.CargoWithdrawn);
                    Append(builder, "cargoSourceThingID", operation.CargoSourceThingID);
                    Append(builder, "cargoSourceStackBefore", operation.CargoSourceStackBefore);
                    Append(
                        builder,
                        "cargoSourceStackAfterWithdrawal",
                        operation.CargoSourceStackAfterWithdrawal);
                    Append(builder, "cargoConsumed", operation.CargoConsumed);
                    Append(builder, "cargoRolledBack", operation.CargoRolledBack);
                    Append(
                        builder,
                        "cargoRecoveryMode",
                        operation.CargoRecoveryMode ?? "none");
                    Append(
                        builder,
                        "magazineCommitAccepted",
                        operation.MagazineCommitAccepted);
                    Append(
                        builder,
                        "magazineRollbackRequested",
                        operation.MagazineRollbackRequested);
                    Append(
                        builder,
                        "magazineRollbackAccepted",
                        operation.MagazineRollbackAccepted);
                }
            }

            if (!string.IsNullOrEmpty(note))
            {
                Append(builder, "note", note);
            }

            Log.Message(builder.ToString());
        }

        private static string DefName(Def def)
        {
            return def != null ? def.defName : "null";
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

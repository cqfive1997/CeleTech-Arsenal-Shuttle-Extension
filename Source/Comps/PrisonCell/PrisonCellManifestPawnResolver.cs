using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal enum PrisonCellManifestPawnResolutionKind
    {
        Missing,
        LivePawn,
        DeadPawnCorpse,
        Invalid,
        Ambiguous
    }

    /// <summary>
    /// Read-only identity result for one Prison Cell manifest Pawn. A dead Pawn
    /// remains identifiable through the top-level Corpse that owns InnerPawn.
    /// </summary>
    internal sealed class PrisonCellManifestPawnResolution
    {
        internal PrisonCellManifestPawnResolution(
            PrisonCellManifestPawnResolutionKind kind,
            Pawn pawn,
            Thing containerThing,
            ThingOwner sourceOwner,
            string failureReason)
        {
            this.Kind = kind;
            this.Pawn = pawn;
            this.ContainerThing = containerThing;
            this.SourceOwner = sourceOwner;
            this.FailureReason = failureReason;
        }

        internal PrisonCellManifestPawnResolutionKind Kind { get; private set; }

        internal Pawn Pawn { get; private set; }

        internal Thing ContainerThing { get; private set; }

        internal ThingOwner SourceOwner { get; private set; }

        internal string FailureReason { get; private set; }
    }

    /// <summary>
    /// Resolves only the two representations valid for a Prison Cell manifest:
    /// a top-level live Pawn or a top-level Corpse containing that Pawn.
    /// It never moves Things or mutates manifest state.
    /// </summary>
    internal static class PrisonCellManifestPawnResolver
    {
        internal static PrisonCellManifestPawnResolution ResolveInOwners(
            ShuttleHolderLaunchManifestEntry entry,
            ThingOwner primarySource,
            ThingOwner secondarySource)
        {
            if (entry == null || entry.ThingID <= 0)
            {
                return Invalid("PrisonCell manifest Pawn resolution requires a valid entry and thingID.");
            }

            Candidate candidate = null;
            bool ambiguous = false;
            InspectOwner(primarySource, entry.ThingID, ref candidate, ref ambiguous);
            if (!object.ReferenceEquals(primarySource, secondarySource))
            {
                InspectOwner(secondarySource, entry.ThingID, ref candidate, ref ambiguous);
            }

            if (ambiguous)
            {
                return new PrisonCellManifestPawnResolution(
                    PrisonCellManifestPawnResolutionKind.Ambiguous,
                    null,
                    null,
                    null,
                    "PrisonCell manifest Pawn resolution found multiple source representations for thingID=" +
                        entry.ThingID +
                        ".");
            }

            if (candidate == null)
            {
                return new PrisonCellManifestPawnResolution(
                    PrisonCellManifestPawnResolutionKind.Missing,
                    null,
                    null,
                    null,
                    null);
            }

            string validationFailure;
            if (!ValidateCandidate(entry, candidate, out validationFailure))
            {
                return Invalid(validationFailure);
            }

            return new PrisonCellManifestPawnResolution(
                candidate.Corpse != null
                    ? PrisonCellManifestPawnResolutionKind.DeadPawnCorpse
                    : PrisonCellManifestPawnResolutionKind.LivePawn,
                candidate.Pawn,
                candidate.ContainerThing,
                candidate.SourceOwner,
                null);
        }

        private static void InspectOwner(
            ThingOwner owner,
            int thingID,
            ref Candidate candidate,
            ref bool ambiguous)
        {
            if (owner == null || ambiguous)
            {
                return;
            }

            for (int i = 0; i < owner.Count; i++)
            {
                Thing thing = owner[i];
                Pawn pawn = thing as Pawn;
                Corpse corpse = null;
                if (pawn == null)
                {
                    corpse = thing as Corpse;
                    pawn = corpse != null ? corpse.InnerPawn : null;
                }

                if (pawn == null || pawn.thingIDNumber != thingID)
                {
                    continue;
                }

                Candidate found = new Candidate(pawn, corpse, thing, owner);
                if (candidate == null)
                {
                    candidate = found;
                    continue;
                }

                if (!object.ReferenceEquals(candidate.ContainerThing, found.ContainerThing))
                {
                    ambiguous = true;
                    return;
                }
            }
        }

        private static bool ValidateCandidate(
            ShuttleHolderLaunchManifestEntry entry,
            Candidate candidate,
            out string failureReason)
        {
            failureReason = null;
            if (candidate == null ||
                candidate.Pawn == null ||
                candidate.ContainerThing == null ||
                candidate.SourceOwner == null ||
                candidate.Pawn.Destroyed ||
                candidate.ContainerThing.Destroyed)
            {
                failureReason = "PrisonCell manifest thingID=" +
                    entry.ThingID +
                    " resolved to an invalid Pawn or container Thing.";
                return false;
            }

            if (candidate.Corpse == null && candidate.Pawn.Dead)
            {
                failureReason = "PrisonCell manifest thingID=" +
                    entry.ThingID +
                    " resolved to a dead top-level Pawn without a Corpse.";
                return false;
            }

            if (candidate.Corpse != null && !candidate.Pawn.Dead)
            {
                failureReason = "PrisonCell manifest thingID=" +
                    entry.ThingID +
                    " resolved to a Corpse whose InnerPawn is not dead.";
                return false;
            }

            string pawnDefName = candidate.Pawn.def != null
                ? candidate.Pawn.def.defName
                : null;
            if (!string.IsNullOrEmpty(entry.DefName) && pawnDefName != entry.DefName)
            {
                failureReason = "PrisonCell manifest thingID=" +
                    entry.ThingID +
                    " def mismatch. manifest=" +
                    entry.DefName +
                    " source=" +
                    (pawnDefName ?? "null") +
                    ".";
                return false;
            }

            if (candidate.Pawn.RaceProps == null ||
                !candidate.Pawn.RaceProps.Humanlike)
            {
                failureReason = "PrisonCell manifest thingID=" +
                    entry.ThingID +
                    " resolved to a non-humanlike Pawn.";
                return false;
            }

            return true;
        }

        private static PrisonCellManifestPawnResolution Invalid(string failureReason)
        {
            return new PrisonCellManifestPawnResolution(
                PrisonCellManifestPawnResolutionKind.Invalid,
                null,
                null,
                null,
                failureReason);
        }

        private sealed class Candidate
        {
            internal Candidate(
                Pawn pawn,
                Corpse corpse,
                Thing containerThing,
                ThingOwner sourceOwner)
            {
                this.Pawn = pawn;
                this.Corpse = corpse;
                this.ContainerThing = containerThing;
                this.SourceOwner = sourceOwner;
            }

            internal Pawn Pawn { get; private set; }

            internal Corpse Corpse { get; private set; }

            internal Thing ContainerThing { get; private set; }

            internal ThingOwner SourceOwner { get; private set; }
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions
{
    /// <summary>
    /// Short-lived transaction handle for one exact cargo withdrawal. Cargo owns source
    /// rollback, transporter notification, emergency recovery, and inventory invalidation;
    /// the caller owns the domain decision to consume or retain the withdrawn Thing.
    /// </summary>
    internal sealed class ShuttleCargoWithdrawal
    {
        private readonly ThingWithComps shuttleHost;
        private readonly CompTransporter sourceTransporter;
        private readonly ThingOwner sourceContents;
        private readonly System.Action onCargoChanged;
        private readonly string reason;
        private bool completed;

        internal ShuttleCargoWithdrawal(
            ThingWithComps shuttleHost,
            CargoTakeRecord takeRecord,
            int sourceThingID,
            int sourceStackCountBefore,
            string reason,
            System.Action onCargoChanged)
        {
            this.shuttleHost = shuttleHost;
            this.sourceTransporter = takeRecord != null
                ? takeRecord.SourceTransporter
                : null;
            this.sourceContents = takeRecord != null
                ? takeRecord.SourceContents
                : null;
            this.Thing = takeRecord != null ? takeRecord.Thing : null;
            this.reason = reason;
            this.onCargoChanged = onCargoChanged;
            this.SourceThingID = sourceThingID;
            this.SourceStackCountBefore = sourceStackCountBefore;
        }

        internal Thing Thing { get; private set; }

        internal ThingWithComps ShuttleHost
        {
            get { return this.shuttleHost; }
        }

        internal int SourceThingID { get; private set; }

        internal int SourceStackCountBefore { get; private set; }

        internal bool IsCompleted
        {
            get { return this.completed; }
        }

        internal bool TryTransferTo(ThingOwner<Thing> destination, out string failureReason)
        {
            failureReason = null;
            if (this.completed)
            {
                failureReason = "Cargo withdrawal is already completed.";
                return false;
            }

            Thing thing = this.Thing;
            if (destination == null || thing == null || thing.Destroyed || thing.stackCount <= 0)
            {
                failureReason = "Cargo withdrawal destination or Thing is unavailable.";
                return false;
            }

            if (thing.holdingOwner == destination)
            {
                return true;
            }

            if (!destination.TryAddOrTransfer(thing, false))
            {
                failureReason = "Cargo withdrawal destination rejected the Thing.";
                return false;
            }

            this.NotifyCargoChanged();
            return true;
        }

        internal bool CommitTransferred()
        {
            if (this.completed)
            {
                return false;
            }

            Thing thing = this.Thing;
            if (thing == null || thing.Destroyed || thing.stackCount <= 0 || thing.holdingOwner == null)
            {
                return false;
            }

            this.completed = true;
            this.NotifyCargoChanged();
            return true;
        }

        internal bool CommitConsumed()
        {
            if (this.completed)
            {
                return false;
            }

            Thing thing = this.Thing;
            if (thing != null && !thing.Destroyed)
            {
                try
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
                catch (System.Exception exception)
                {
                    Log.Error(
                        "[CeleTech Shuttle] Cargo withdrawal consume commit failed. reason=" +
                        (this.reason ?? "null") + " exception=" + exception);
                    return false;
                }
            }

            this.completed = true;
            this.NotifyCargoChanged();
            return true;
        }

        internal bool CommitAlreadyConsumed()
        {
            if (this.completed)
            {
                return false;
            }

            Thing thing = this.Thing;
            if (thing != null && !thing.Destroyed && thing.stackCount > 0)
            {
                return false;
            }

            this.completed = true;
            this.NotifyCargoChanged();
            return true;
        }

        internal bool CommitReleased()
        {
            if (this.completed)
            {
                return false;
            }

            this.completed = true;
            this.NotifyCargoChanged();
            return true;
        }

        internal bool TryRollBackToSource(out string failureReason)
        {
            failureReason = null;
            if (this.completed)
            {
                failureReason = "Cargo withdrawal is already completed.";
                return false;
            }

            Thing thing = this.Thing;
            if (thing == null || thing.Destroyed || thing.stackCount <= 0)
            {
                failureReason = "Withdrawn cargo Thing is unavailable for rollback.";
                return false;
            }

            if (thing.holdingOwner == this.sourceContents)
            {
                this.completed = true;
                this.NotifyCargoChanged();
                return true;
            }

            if (this.sourceContents == null ||
                !this.sourceContents.TryAddOrTransfer(thing, false))
            {
                failureReason = "Original cargo holder rejected withdrawal rollback.";
                return false;
            }

            if (this.sourceTransporter != null)
            {
                this.sourceTransporter.Notify_ThingAdded(thing);
            }

            this.completed = true;
            this.NotifyCargoChanged();
            return true;
        }

        internal bool TryRecoverToOwner(
            ThingOwner destination,
            out string failureReason)
        {
            failureReason = null;
            if (this.completed)
            {
                failureReason = "Cargo withdrawal is already completed.";
                return false;
            }

            Thing thing = this.Thing;
            if (destination == null || thing == null || thing.Destroyed || thing.stackCount <= 0)
            {
                failureReason = "Cargo recovery destination or Thing is unavailable.";
                return false;
            }

            if (thing.holdingOwner != destination &&
                !destination.TryAddOrTransfer(thing, false))
            {
                failureReason = "Cargo recovery destination rejected the Thing.";
                return false;
            }

            this.completed = true;
            this.NotifyCargoChanged();
            return true;
        }

        internal bool TryRecoverThroughTransferState(
            ThingOwner preferredOwner,
            ThingOwner fallbackOwner,
            out ShuttleTransferRecoveryStatus recoveryStatus,
            out string failureReason)
        {
            recoveryStatus = ShuttleTransferRecoveryStatus.None;
            failureReason = null;
            if (this.completed)
            {
                failureReason = "Cargo withdrawal is already completed.";
                return false;
            }

            Thing thing = this.Thing;
            CompShuttleHolderLaunchTransferState transferState = this.shuttleHost != null
                ? this.shuttleHost.TryGetComp<CompShuttleHolderLaunchTransferState>()
                : null;
            if (transferState == null || thing == null || thing.Destroyed || thing.stackCount <= 0)
            {
                failureReason = "Cargo transfer recovery state or Thing is unavailable.";
                return false;
            }

            ThingOwner effectivePreferredOwner = preferredOwner ?? this.sourceContents;
            if (!transferState.TryRecoverTransferThing(
                thing,
                "Cargo withdrawal rollback failed. reason=" + (this.reason ?? "null"),
                effectivePreferredOwner,
                fallbackOwner,
                this.shuttleHost != null ? this.shuttleHost.Map : null,
                this.shuttleHost != null ? this.shuttleHost.Position : IntVec3.Invalid,
                out recoveryStatus,
                out failureReason))
            {
                return false;
            }

            if (recoveryStatus == ShuttleTransferRecoveryStatus.RecoveredToPreferredOwner &&
                effectivePreferredOwner == this.sourceContents &&
                this.sourceTransporter != null)
            {
                this.sourceTransporter.Notify_ThingAdded(thing);
            }

            this.completed = true;
            this.NotifyCargoChanged();
            return true;
        }

        internal bool TryRecoverNear(Thing anchor, out string failureReason)
        {
            failureReason = null;
            if (this.completed)
            {
                failureReason = "Cargo withdrawal is already completed.";
                return false;
            }

            Thing thing = this.Thing;
            if (anchor == null || !anchor.Spawned || anchor.Map == null ||
                thing == null || thing.Destroyed || thing.stackCount <= 0)
            {
                failureReason = "Cargo recovery map position or Thing is unavailable.";
                return false;
            }

            if (!GenPlace.TryPlaceThing(
                thing,
                anchor.Position,
                anchor.Map,
                ThingPlaceMode.Near))
            {
                failureReason = "Cargo recovery could not place the Thing near the target.";
                return false;
            }

            this.completed = true;
            this.NotifyCargoChanged();
            return true;
        }

        internal int CountOriginalSourceStack()
        {
            if (this.sourceContents == null || this.SourceThingID <= 0)
            {
                return -1;
            }

            int count = 0;
            for (int i = 0; i < this.sourceContents.Count; i++)
            {
                Thing thing = this.sourceContents[i];
                if (thing != null && thing.thingIDNumber == this.SourceThingID)
                {
                    count += thing.stackCount;
                }
            }

            return count;
        }

        private void NotifyCargoChanged()
        {
            if (this.onCargoChanged != null)
            {
                this.onCargoChanged();
            }
        }
    }
}

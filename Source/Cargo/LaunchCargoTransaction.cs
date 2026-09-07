using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    internal sealed class LaunchCargoTransaction
    {
        private readonly VanillaTransporterAdapter transporterAdapter;
        private static bool devInjectFailureAfterSetShuttle;

        public LaunchCargoTransaction(VanillaTransporterAdapter transporterAdapter)
        {
            this.transporterAdapter = transporterAdapter;
        }

        internal static bool DevSetShuttleFailureInjectionArmed
        {
            get
            {
                return Prefs.DevMode && devInjectFailureAfterSetShuttle;
            }
        }

        internal static void ArmDevSetShuttleFailureInjectionForNextCommit()
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            devInjectFailureAfterSetShuttle = true;
        }

        public bool PlanLaunchCargoHandoffs(
            ThingWithComps host,
            Map map,
            ThingDef activeTransporterDef,
            out List<ShuttleLaunchCargoHandoffPlan> plans,
            out string failureReason)
        {
            plans = new List<ShuttleLaunchCargoHandoffPlan>();
            failureReason = null;

            if (host == null)
            {
                failureReason = "CT_Shuttle_Launch_Failed_HostUnavailable".Translate().ToString();
                return false;
            }

            if (map == null)
            {
                failureReason = "CT_Shuttle_Launch_Failed_MapUnavailable".Translate().ToString();
                return false;
            }

            if (activeTransporterDef == null)
            {
                failureReason = "CT_Shuttle_Launch_Failed_ActiveTransporterDefUnavailable".Translate().ToString();
                return false;
            }

            List<CompTransporter> transporters = this.transporterAdapter.ResolveTransportersForLaunch(host);
            if (transporters.Count == 0)
            {
                failureReason = "CT_Shuttle_Launch_Failed_TransporterBackendUnavailable".Translate().ToString();
                return false;
            }

            int groupID;
            if (!this.transporterAdapter.TryEnsureLaunchGroupID(
                transporters,
                out groupID,
                out failureReason))
            {
                return false;
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter == null || transporter.parent == null)
                {
                    continue;
                }

                IntVec3 spawnCell = transporter.parent.Position;
                if (!spawnCell.IsValid || !spawnCell.InBounds(map))
                {
                    failureReason = "CT_Shuttle_Launch_Failed_SpawnCellInvalid".Translate().ToString();
                    return false;
                }

                plans.Add(new ShuttleLaunchCargoHandoffPlan(
                    transporter,
                    spawnCell,
                    groupID,
                    activeTransporterDef,
                    host.def,
                    transporter.parent.Rotation,
                    transporter.parent.HasComp<CompShuttle>()));
            }

            if (plans.Count == 0)
            {
                failureReason = "CT_Shuttle_Launch_Failed_NoTransporterToLaunch".Translate().ToString();
                return false;
            }

            return true;
        }

        public bool CommitLaunchCargoHandoffs(
            List<ShuttleLaunchCargoHandoffPlan> plans,
            Map rollbackMap,
            out List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            handoffs = new List<ShuttleLaunchCargoHandoff>();
            failureReason = null;

            if (plans == null || plans.Count == 0)
            {
                failureReason = "CT_Shuttle_Launch_Failed_HandoffPlanUnavailable".Translate().ToString();
                return false;
            }

            try
            {
                for (int i = 0; i < plans.Count; i++)
                {
                    ShuttleLaunchCargoHandoffPlan plan = plans[i];
                    if (!this.TryCommitSingleHandoff(plan, handoffs, out failureReason))
                    {
                        this.RollbackCommittedHandoffsAfterFailedCommit(handoffs, rollbackMap, ref failureReason);
                        return false;
                    }
                }
            }
            catch (Exception exception)
            {
                failureReason = "CT_Shuttle_Launch_Failed_CargoCommitFailed".Translate(exception.Message).ToString();
                this.RollbackCommittedHandoffsAfterFailedCommit(handoffs, rollbackMap, ref failureReason);
                return false;
            }

            if (handoffs.Count == 0)
            {
                failureReason = "CT_Shuttle_Launch_Failed_NoTransporterToLaunch".Translate().ToString();
                this.RollbackCommittedHandoffsAfterFailedCommit(handoffs, rollbackMap, ref failureReason);
                return false;
            }

            return true;
        }

        public LaunchCargoRollbackResult RollbackLaunchCargoHandoffs(List<ShuttleLaunchCargoHandoff> handoffs, Map map)
        {
            if (handoffs == null)
            {
                return LaunchCargoRollbackResult.Success();
            }

            bool recoveredWithDrop = false;
            List<string> failures = new List<string>();
            for (int i = handoffs.Count - 1; i >= 0; i--)
            {
                ShuttleLaunchCargoHandoff handoff = handoffs[i];
                if (handoff == null || handoff.ActiveTransporter == null || handoff.ActiveTransporter.Contents == null)
                {
                    continue;
                }

                CompTransporter sourceTransporter = handoff.SourceTransporter;
                if (sourceTransporter == null || sourceTransporter.parent == null || sourceTransporter.parent.Destroyed)
                {
                    string dropFailureReason;
                    if (this.TryDropRollbackCargoNearLaunchCell(handoff, map, out dropFailureReason))
                    {
                        recoveredWithDrop = true;
                    }
                    else
                    {
                        failures.Add(dropFailureReason ?? "Could not drop rollback cargo for missing source transporter.");
                    }

                    continue;
                }

                try
                {
                    if (handoff.SourceIsShuttle)
                    {
                        string shuttleFailureReason;
                        if (!this.RollbackCommittedShuttleHandoff(
                            handoff,
                            sourceTransporter,
                            map,
                            out shuttleFailureReason))
                        {
                            failures.Add(shuttleFailureReason ?? "Committed shuttle handoff rollback failed.");
                        }

                        continue;
                    }

                    ThingOwner activeContents = this.GetActiveContents(handoff);
                    string transferFailureReason;
                    bool transferred = this.TryAddRangeOrTransferAndVerifyEmptied(
                        sourceTransporter.GetDirectlyHeldThings(),
                        handoff.ActiveTransporter.Contents.innerContainer,
                        out transferFailureReason);
                    if (!transferred)
                    {
                        failures.Add("Launch cargo rollback TryAddRangeOrTransfer to source failed postcondition. reason=" +
                            (transferFailureReason ?? "null") +
                            " " +
                            this.DescribeHandoffWithActiveContents(handoff, activeContents));
                    }

                    if (!this.ActiveContentsAreEmpty(activeContents))
                    {
                        failures.Add("Launch cargo rollback to source transporter did not empty ActiveTransporterInfo.innerContainer. " +
                            this.DescribeHandoffWithActiveContents(handoff, activeContents));
                    }
                }
                catch (Exception exception)
                {
                    failures.Add("Could not roll back shuttle launch cargo. " +
                        this.DescribeHandoff(handoff) +
                        " exception=" +
                        exception.Message);
                }
            }

            if (failures.Count > 0)
            {
                string failureReason = string.Join(" | ", failures.ToArray());
                Log.Error("[CeleTech Shuttle] Launch cargo rollback fatal unresolved: " + failureReason);
                return LaunchCargoRollbackResult.Fatal(failureReason);
            }

            return recoveredWithDrop
                ? LaunchCargoRollbackResult.RecoveredWithDrop("Rollback recovered cargo by dropping one or more handoffs near their launch cells.")
                : LaunchCargoRollbackResult.Success();
        }

        private bool RollbackCommittedShuttleHandoff(
            ShuttleLaunchCargoHandoff handoff,
            CompTransporter sourceTransporter,
            Map map,
            out string failureReason)
        {
            failureReason = null;
            if (handoff == null ||
                handoff.ActiveTransporter == null ||
                handoff.ActiveTransporter.Contents == null ||
                sourceTransporter == null)
            {
                failureReason = "Committed shuttle handoff rollback is missing required state.";
                return false;
            }

            ThingOwner activeContents = handoff.ActiveTransporter.Contents.innerContainer;
            if (activeContents == null)
            {
                failureReason = "Committed shuttle handoff rollback is missing ActiveTransporterInfo.innerContainer.";
                return false;
            }

            Thing shuttle = handoff.ActiveTransporter.Contents.GetShuttle();
            bool shuttleInDedicatedSlot = shuttle != null;
            bool shuttleInInnerContainer = false;
            if (shuttle == null)
            {
                shuttle = sourceTransporter.parent;
                shuttleInInnerContainer = shuttle != null && activeContents.Contains(shuttle);
            }

            bool needsRespawn = shuttle != null && !shuttle.Destroyed && !shuttle.Spawned;
            if (needsRespawn && !this.CanRespawnHandoffShuttle(handoff, map))
            {
                failureReason = "Committed shuttle handoff rollback refused to remove shuttle host because no valid rollback map/cell is available. " +
                    this.DescribeHandoff(handoff);
                return false;
            }

            if (shuttleInDedicatedSlot)
            {
                handoff.ActiveTransporter.Contents.RemoveShuttle();
            }
            else if (shuttleInInnerContainer)
            {
                activeContents.Remove(shuttle);
            }

            try
            {
                ThingOwner sourceContents = sourceTransporter.GetDirectlyHeldThings();
                if (sourceContents == null)
                {
                    failureReason = "Committed shuttle handoff rollback is missing source transporter holder. " +
                        this.DescribeHandoffWithActiveContents(handoff, activeContents);
                    this.TryRestoreShuttleToHandoff(handoff, shuttle);
                    return false;
                }

                string transferFailureReason;
                bool transferred = this.TryAddRangeOrTransferAndVerifyEmptied(
                    sourceContents,
                    activeContents,
                    out transferFailureReason);
                if (!transferred)
                {
                    failureReason = "Committed shuttle handoff rollback TryAddRangeOrTransfer to source failed postcondition. reason=" +
                        (transferFailureReason ?? "null") +
                        " " +
                        this.DescribeHandoffWithActiveContents(handoff, activeContents);
                    this.TryRestoreShuttleToHandoff(handoff, shuttle);
                    return false;
                }

                if (!this.ActiveContentsAreEmpty(activeContents))
                {
                    failureReason = "Committed shuttle handoff rollback did not empty ActiveTransporterInfo.innerContainer. " +
                        this.DescribeHandoffWithActiveContents(handoff, activeContents);
                    this.TryRestoreShuttleToHandoff(handoff, shuttle);
                    return false;
                }

                if (shuttle == null || shuttle.Destroyed || shuttle.Spawned)
                {
                    return true;
                }

                if (handoff.Plan != null)
                {
                    shuttle.Rotation = handoff.Plan.Rotation;
                }

                GenSpawn.Spawn(shuttle, handoff.SpawnCell, map, WipeMode.Vanish);
                return true;
            }
            catch (Exception exception)
            {
                bool restoredToHandoff = this.TryRestoreShuttleToHandoff(handoff, shuttle);
                failureReason = "Committed shuttle handoff rollback threw after shuttle host removal. restoredToHandoff=" +
                    restoredToHandoff +
                    " " +
                    this.DescribeHandoff(handoff) +
                    " exception=" +
                    exception.Message;
                return false;
            }
        }

        public LaunchCargoFinalizeResult FinalizeLaunchCargoHandoffs(List<ShuttleLaunchCargoHandoff> handoffs, Map map)
        {
            if (handoffs == null)
            {
                return LaunchCargoFinalizeResult.Success();
            }

            List<string> failures = new List<string>();
            for (int i = 0; i < handoffs.Count; i++)
            {
                ShuttleLaunchCargoHandoff handoff = handoffs[i];
                CompTransporter sourceTransporter = handoff != null ? handoff.SourceTransporter : null;
                if (sourceTransporter == null || sourceTransporter.parent == null)
                {
                    continue;
                }

                sourceTransporter.TryRemoveLord(map);
                if (!handoff.SourceIsShuttle && !sourceTransporter.parent.Destroyed)
                {
                    sourceTransporter.CleanUpLoadingVars(map);
                    string heldThingDump;
                    if (ShuttleHeldThingScanner.ContainsRecoverableHeldThings(sourceTransporter.parent, out heldThingDump))
                    {
                        failures.Add("Refusing to DestroyMode.Vanish launch source transporter because recursive holder scan found recoverable held Things. " +
                            (heldThingDump ?? "null") +
                            " " +
                            this.DescribeHandoff(handoff));
                        continue;
                    }

                    sourceTransporter.parent.Destroy(DestroyMode.Vanish);
                }
            }

            if (failures.Count > 0)
            {
                string failureReason = string.Join(" | ", failures.ToArray());
                Log.Error("[CeleTech Shuttle] Launch cargo finalize fatal blocked: " + failureReason);
                return LaunchCargoFinalizeResult.Fatal(failureReason);
            }

            return LaunchCargoFinalizeResult.Success();
        }

        private bool TryDropRollbackCargoNearLaunchCell(
            ShuttleLaunchCargoHandoff handoff,
            Map map,
            out string failureReason)
        {
            failureReason = null;
            if (handoff == null || handoff.ActiveTransporter == null || handoff.ActiveTransporter.Contents == null)
            {
                failureReason = "Drop rollback cargo failed because handoff or ActiveTransporterInfo is missing.";
                return false;
            }

            ThingOwner activeContents = handoff.ActiveTransporter.Contents.innerContainer;
            if (map != null && handoff.SpawnCell.IsValid && handoff.SpawnCell.InBounds(map))
            {
                try
                {
                    activeContents.TryDropAll(
                        handoff.SpawnCell,
                        map,
                        ThingPlaceMode.Near,
                        null,
                        null,
                        true);
                    if (!this.ActiveContentsAreEmpty(activeContents))
                    {
                        failureReason = "Drop rollback cargo did not empty ActiveTransporterInfo.innerContainer. " +
                            this.DescribeHandoff(handoff) +
                            " remaining=" +
                            this.DescribeOwnerContents(activeContents);
                        return false;
                    }

                    Log.Warning("[CeleTech Shuttle] Rolled back shuttle launch cargo by dropping it near the launch cell because the source transporter was gone.");
                    return true;
                }
                catch (Exception exception)
                {
                    failureReason = "Could not drop rolled-back shuttle launch cargo. " +
                        this.DescribeHandoff(handoff) +
                        " exception=" +
                        exception.Message;
                    return false;
                }
            }

            failureReason = "Could not roll back shuttle launch cargo because the source transporter and launch map are unavailable. " +
                this.DescribeHandoff(handoff);
            return false;
        }

        private bool TryCommitSingleHandoff(
            ShuttleLaunchCargoHandoffPlan plan,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            failureReason = null;

            if (plan == null || plan.SourceTransporter == null || plan.SourceTransporter.parent == null)
            {
                failureReason = "CT_Shuttle_Launch_Failed_HandoffPlanNoLongerValid".Translate().ToString();
                return false;
            }

            if (plan.ActiveTransporterDef == null)
            {
                failureReason = "CT_Shuttle_Launch_Failed_ActiveTransporterDefUnavailable".Translate().ToString();
                return false;
            }

            ThingOwner sourceContents = plan.SourceTransporter.GetDirectlyHeldThings();
            if (sourceContents == null)
            {
                failureReason = "CT_Shuttle_Launch_Failed_SourceTransporterCargoUnavailable".Translate().ToString();
                return false;
            }

            int sourceStackCountBefore = this.CountHeldStacks(sourceContents);
            ActiveTransporter activeTransporter = ThingMaker.MakeThing(plan.ActiveTransporterDef, null) as ActiveTransporter;
            if (activeTransporter == null)
            {
                failureReason = "CT_Shuttle_Launch_Failed_ActiveTransporterCreateFailed".Translate().ToString();
                return false;
            }

            activeTransporter.Contents = new ActiveTransporterInfo();
            if (activeTransporter.Contents.innerContainer == null)
            {
                failureReason = "CT_Shuttle_Launch_Failed_ActiveTransporterCargoUnavailable".Translate().ToString();
                return false;
            }

            ShuttleLaunchCargoHandoff handoff = new ShuttleLaunchCargoHandoff(plan, activeTransporter);
            handoffs.Add(handoff);

            string transferFailureReason;
            bool transferred = this.TryAddRangeOrTransferAndVerifyEmptied(
                activeTransporter.Contents.innerContainer,
                sourceContents,
                out transferFailureReason);
            if (!transferred)
            {
                failureReason = "CT_Shuttle_Launch_Failed_CargoTransferIncompleteCountMismatch".Translate().ToString() +
                    " TryAddRangeOrTransfer failed postcondition. reason=" +
                    (transferFailureReason ?? "null") +
                    " " +
                    this.DescribeHandoffWithActiveContents(
                        handoff,
                        activeTransporter.Contents.innerContainer) +
                    " sourceRemaining=" +
                    this.DescribeOwnerContents(sourceContents);
                return false;
            }

            if (!this.SourceTransporterContentsAreEmpty(sourceContents))
            {
                failureReason = "CT_Shuttle_Launch_Failed_CargoTransferIncompleteSourceNotEmpty".Translate().ToString() +
                    " " +
                    this.DescribeHandoffWithActiveContents(
                        handoff,
                        activeTransporter.Contents.innerContainer) +
                    " sourceRemaining=" +
                    this.DescribeOwnerContents(sourceContents);
                return false;
            }

            int destinationStackCount = this.CountHeldStacks(activeTransporter.Contents.innerContainer);
            if (destinationStackCount != sourceStackCountBefore)
            {
                failureReason = "CT_Shuttle_Launch_Failed_CargoTransferIncompleteCountMismatch".Translate().ToString() +
                    " " +
                    this.DescribeHandoffWithActiveContents(
                        handoff,
                        activeTransporter.Contents.innerContainer) +
                    " expectedStacks=" +
                    sourceStackCountBefore +
                    " actualStacks=" +
                    destinationStackCount;
                return false;
            }

            activeTransporter.Contents.sentTransporterDef = plan.SentTransporterDef;
            activeTransporter.Rotation = plan.Rotation;

            if (plan.SourceIsShuttle)
            {
                activeTransporter.Contents.SetShuttle(plan.SourceTransporter.parent);
                ThrowIfDevSetShuttleFailureInjectionArmed();
            }

            return true;
        }

        private void RollbackCommittedHandoffsAfterFailedCommit(
            List<ShuttleLaunchCargoHandoff> handoffs,
            Map rollbackMap,
            ref string failureReason)
        {
            LaunchCargoRollbackResult rollbackResult = this.RollbackLaunchCargoHandoffs(handoffs, rollbackMap);
            if (rollbackResult.Status == LaunchCargoRollbackStatus.FatalUnresolved)
            {
                failureReason = (failureReason ?? "Launch cargo commit failed.") +
                    " Rollback fatal unresolved: " +
                    (rollbackResult.FailureReason ?? "null");
                return;
            }

            if (handoffs != null)
            {
                handoffs.Clear();
            }
        }

        private bool SourceTransporterContentsAreEmpty(ThingOwner sourceContents)
        {
            return sourceContents != null &&
                sourceContents.Count == 0 &&
                this.CountHeldStacks(sourceContents) == 0;
        }

        private int CountHeldStacks(ThingOwner contents)
        {
            int stackCount = 0;
            if (contents == null)
            {
                return stackCount;
            }

            foreach (Thing thing in contents)
            {
                if (thing == null)
                {
                    continue;
                }

                stackCount += thing.stackCount;
            }

            return stackCount;
        }

        private ThingOwner GetActiveContents(ShuttleLaunchCargoHandoff handoff)
        {
            return handoff != null &&
                handoff.ActiveTransporter != null &&
                handoff.ActiveTransporter.Contents != null
                ? handoff.ActiveTransporter.Contents.innerContainer
                : null;
        }

        private bool ActiveContentsAreEmpty(ThingOwner contents)
        {
            return contents == null || contents.Count == 0;
        }

        private bool TryAddRangeOrTransferAndVerifyEmptied(
            ThingOwner destination,
            ThingOwner source,
            out string failureReason)
        {
            failureReason = null;
            if (destination == null)
            {
                failureReason = "destination owner is null";
                return false;
            }

            if (source == null)
            {
                failureReason = "source owner is null";
                return false;
            }

            destination.TryAddRangeOrTransfer(
                source,
                true,
                true);

            if (source.Count != 0)
            {
                failureReason = "source owner still contains " +
                    source.Count +
                    " stack(s): " +
                    this.DescribeOwnerContents(source);
                return false;
            }

            return true;
        }

        private bool CanRespawnHandoffShuttle(ShuttleLaunchCargoHandoff handoff, Map map)
        {
            return map != null &&
                handoff != null &&
                handoff.SpawnCell.IsValid &&
                handoff.SpawnCell.InBounds(map);
        }

        private bool TryRestoreShuttleToHandoff(ShuttleLaunchCargoHandoff handoff, Thing shuttle)
        {
            if (handoff == null ||
                handoff.ActiveTransporter == null ||
                handoff.ActiveTransporter.Contents == null ||
                shuttle == null ||
                shuttle.Destroyed ||
                shuttle.Spawned)
            {
                return false;
            }

            try
            {
                if (handoff.ActiveTransporter.Contents.GetShuttle() == shuttle)
                {
                    return true;
                }

                handoff.ActiveTransporter.Contents.SetShuttle(shuttle);
                return handoff.ActiveTransporter.Contents.GetShuttle() == shuttle;
            }
            catch (Exception exception)
            {
                Log.Error("[CeleTech Shuttle] Failed to restore shuttle host to ActiveTransporterInfo after rollback exception: " +
                    exception);
                return false;
            }
        }

        private static void ThrowIfDevSetShuttleFailureInjectionArmed()
        {
            if (!Prefs.DevMode)
            {
                devInjectFailureAfterSetShuttle = false;
                return;
            }

            if (!devInjectFailureAfterSetShuttle)
            {
                return;
            }

            devInjectFailureAfterSetShuttle = false;
            throw new InvalidOperationException("[DEV] Injected launch cargo failure after ActiveTransporterInfo.SetShuttle.");
        }

        private string DescribeHandoff(ShuttleLaunchCargoHandoff handoff)
        {
            if (handoff == null)
            {
                return "handoff=null";
            }

            return "handoff(groupID=" +
                handoff.GroupID +
                ", sourceIsShuttle=" +
                handoff.SourceIsShuttle +
                ", spawnCell=" +
                handoff.SpawnCell +
                ", source=" +
                this.DescribeThingIDAndDef(handoff.SourceTransporter != null ? handoff.SourceTransporter.parent : null) +
                ", activeTransporter=" +
                this.DescribeThingIDAndDef(handoff.ActiveTransporter) +
                ")";
        }

        private string DescribeHandoffWithActiveContents(
            ShuttleLaunchCargoHandoff handoff,
            ThingOwner activeContents)
        {
            return this.DescribeHandoff(handoff) +
                " remainingActive=" +
                this.DescribeOwnerContents(activeContents);
        }

        private string DescribeThingIDAndDef(Thing thing)
        {
            if (thing == null)
            {
                return "null";
            }

            return thing.GetType().Name +
                "(thingID=" +
                thing.thingIDNumber +
                ", def=" +
                (thing.def != null ? thing.def.defName : "null") +
                ")";
        }

        private string DescribeOwnerContents(ThingOwner contents)
        {
            if (contents == null)
            {
                return "owner=null";
            }

            if (contents.Count == 0)
            {
                return "empty";
            }

            List<string> descriptions = new List<string>();
            for (int i = 0; i < contents.Count; i++)
            {
                Thing thing = contents[i];
                descriptions.Add(
                    "thing=" +
                    thing +
                    " def=" +
                    (thing != null && thing.def != null ? thing.def.defName : "null") +
                    " thingID=" +
                    (thing != null ? thing.thingIDNumber : -1) +
                    " stack=" +
                    (thing != null ? thing.stackCount : 0));
            }

            return string.Join("; ", descriptions.ToArray());
        }
    }
}

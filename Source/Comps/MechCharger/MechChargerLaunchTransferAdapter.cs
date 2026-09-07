using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.MechCharging;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttleMechChargerOccupancy
    {
        internal static class MechChargerLaunchTransferAdapter
        {
            internal static bool CanTransferChargingMechsForLaunch(
                CompShuttleMechChargerOccupancy owner,
                out string failureReason)
            {
                List<MechChargerLaunchTransferPlan> plans;
                return TryBuildMechChargerLaunchExportPlans(owner, out plans, out failureReason);
            }

            internal static bool TryExportChargingMechsForLaunch(
                CompShuttleMechChargerOccupancy owner,
                ShuttleHolderLaunchManifest manifest,
                ThingOwner<Thing> destination,
                out string failureReason)
            {
                failureReason = null;
                owner.EnsureInitialized();

                if (manifest == null)
                {
                    failureReason = "[CeleTech Shuttle] MechCharger export requires a holder launch manifest.";
                    return false;
                }

                if (destination == null)
                {
                    failureReason = "[CeleTech Shuttle] MechCharger export requires a launch handoff holder.";
                    return false;
                }

                manifest.EnsureInitialized();
                if (manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.MechChargerHolderKind).Count > 0)
                {
                    failureReason = "[CeleTech Shuttle] MechCharger export refused because MechCharger manifest entries are already active.";
                    return false;
                }

                List<MechChargerLaunchTransferPlan> plans;
                if (!TryBuildMechChargerLaunchExportPlans(owner, out plans, out failureReason))
                {
                    return false;
                }

                if (plans.Count == 0)
                {
                    return true;
                }

                int exportTick = Find.TickManager != null ? Find.TickManager.TicksGame : -1;
                List<Pawn> movedPawns = new List<Pawn>();
                for (int i = 0; i < plans.Count; i++)
                {
                    MechChargerLaunchTransferPlan plan = plans[i];
                    ShuttleHolderLaunchManifestEntry entry = manifest.AddMechChargerEntry(
                        plan.PawnThingID,
                        plan.PawnDefName,
                        plan.PawnLabel,
                        plan.OriginalRecordIndex,
                        plan.ModuleInstanceID,
                        plan.ChargeRateFactor,
                        plan.EntryTick,
                        plan.LastChargeTick,
                        exportTick,
                        ShuttleHolderLaunchManifestConstants.PhaseNone,
                        "MechCharger export transaction entry created before movement commit.");

                    if (!destination.TryAddOrTransfer(plan.Pawn, false))
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        entry.DebugNotes = "TryAddOrTransfer to launch handoff failed.";
                        failureReason = "[CeleTech Shuttle] MechCharger export failed while moving mech thingID=" +
                            plan.PawnThingID +
                            ". Attempting rollback for already moved mechs.";
                        bool rollbackSucceeded = TryRollbackMovedMechsToCharger(owner, movedPawns, destination, failureReason);
                        if (rollbackSucceeded)
                        {
                            manifest.RemoveMechChargerEntries();
                            failureReason += " Already moved mechs were returned to the MechCharger holder and temporary manifest entries were cleared.";
                        }
                        else
                        {
                            failureReason += " Rollback failed, so MechCharger manifest entries remain active for diagnostics and recovery.";
                        }

                        return false;
                    }

                    if (!destination.Contains(plan.Pawn) || owner.IsHeldPawn(plan.Pawn))
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        entry.DebugNotes = "TryAddOrTransfer reported success but export postcondition failed. destinationContains=" +
                            destination.Contains(plan.Pawn) +
                            " stillInMechCharger=" +
                            owner.IsHeldPawn(plan.Pawn);
                        failureReason = "[CeleTech Shuttle] MechCharger export failed postcondition for mech thingID=" +
                            plan.PawnThingID +
                            ". Attempting rollback for already moved mechs.";
                        movedPawns.Add(plan.Pawn);
                        bool rollbackSucceeded = TryRollbackMovedMechsToCharger(owner, movedPawns, destination, failureReason);
                        if (rollbackSucceeded)
                        {
                            manifest.RemoveMechChargerEntries();
                            failureReason += " Already moved mechs were returned to the MechCharger holder and temporary manifest entries were cleared.";
                        }
                        else
                        {
                            failureReason += " Rollback failed, so MechCharger manifest entries remain active for diagnostics and recovery.";
                        }

                        return false;
                    }

                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseInFlight;
                    entry.DebugNotes = "Exported from MechCharger holder to launch handoff.";
                    movedPawns.Add(plan.Pawn);
                }

                for (int i = 0; i < plans.Count; i++)
                {
                    MechChargerLaunchTransferPlan plan = plans[i];
                    owner.RemoveChargingRecord(plan.Pawn);
                    owner.ReleaseChargingReservation(plan.PawnThingID);
                }

                failureReason = "[CeleTech Shuttle] MechCharger export staged " + plans.Count + " charging mech(s).";
                return true;
            }

            internal static bool TryRollbackChargingMechLaunchExport(
                CompShuttleMechChargerOccupancy owner,
                ShuttleHolderLaunchManifest manifest,
                ThingOwner<Thing> source,
                out string notice)
            {
                return TryReturnChargingMechsFromLaunchStaging(
                    owner,
                    manifest,
                    source,
                    null,
                    owner.parent != null ? owner.parent.Map : null,
                    owner.GetEjectCell(owner.parent != null ? owner.parent.Map : null),
                    null,
                    ShuttleHolderLaunchManifestConstants.PhaseRolledBack,
                    "Rolled back from MechCharger launch transfer.",
                    true,
                    out notice);
            }

            internal static bool TryRestoreChargingMechsFromLaunchStaging(
                CompShuttleMechChargerOccupancy owner,
                ShuttleHolderLaunchManifest manifest,
                ThingOwner<Thing> primarySource,
                ThingOwner<Thing> secondarySource,
                Map map,
                IntVec3 fallbackCell,
                ThingOwner<Thing> emergencyRecoveryOwner,
                out string notice)
            {
                return TryReturnChargingMechsFromLaunchStaging(
                    owner,
                    manifest,
                    primarySource,
                    secondarySource,
                    map,
                    fallbackCell,
                    emergencyRecoveryOwner,
                    ShuttleHolderLaunchManifestConstants.PhaseRestored,
                    "Restored from incoming launch handoff before base.Impact.",
                    true,
                    out notice);
            }

            private static bool TryBuildMechChargerLaunchExportPlans(
                CompShuttleMechChargerOccupancy owner,
                out List<MechChargerLaunchTransferPlan> plans,
                out string failureReason)
            {
                plans = new List<MechChargerLaunchTransferPlan>();
                failureReason = null;
                owner.EnsureInitialized();
                owner.ReconcileChargingRecordsToHeldMechs();

                HashSet<int> seenMechThingIDs = new HashSet<int>();
                for (int i = 0; i < owner.chargingRecords.Count; i++)
                {
                    ShuttleMechChargingRecord record = owner.chargingRecords[i];
                    Pawn mech = record != null ? record.Pawn : null;
                    if (record == null || mech == null)
                    {
                        failureReason = "[CeleTech Shuttle] MechCharger launch transfer refused because a charging record is missing its mech.";
                        return false;
                    }

                    record.Sanitize();
                    if (record.PawnThingID <= 0 ||
                        !seenMechThingIDs.Add(record.PawnThingID))
                    {
                        failureReason = "[CeleTech Shuttle] MechCharger launch transfer refused because charging records are duplicated or invalid.";
                        return false;
                    }

                    if (!owner.IsValidHeldChargingMech(mech))
                    {
                        failureReason = "[CeleTech Shuttle] MechCharger launch transfer refused because a held mech is invalid. mech=" +
                            (record.PawnLabel ?? "unknown");
                        return false;
                    }

                    if (!owner.TryResolveRecordModule(record))
                    {
                        failureReason = "[CeleTech Shuttle] MechCharger launch transfer refused because no enabled charger module can hold mech=" +
                            (record.PawnLabel ?? "unknown") +
                            ".";
                        return false;
                    }

                    plans.Add(new MechChargerLaunchTransferPlan(
                        mech,
                        i,
                        record.ModuleInstanceID,
                        record.ChargeRateFactor,
                        record.EntryTick,
                        record.LastChargeTick));
                }

                return true;
            }

            private static bool TryReturnChargingMechsFromLaunchStaging(
                CompShuttleMechChargerOccupancy owner,
                ShuttleHolderLaunchManifest manifest,
                ThingOwner<Thing> primarySource,
                ThingOwner<Thing> secondarySource,
                Map map,
                IntVec3 fallbackCell,
                ThingOwner<Thing> emergencyRecoveryOwner,
                string successPhase,
                string successNotes,
                bool allowSafeEjectWhenNoModule,
                out string notice)
            {
                notice = null;
                owner.EnsureInitialized();

                if (manifest == null)
                {
                    notice = "[CeleTech Shuttle] MechCharger restore requires a holder launch manifest.";
                    return false;
                }

                if (primarySource == null && secondarySource == null)
                {
                    notice = "[CeleTech Shuttle] MechCharger restore requires a launch handoff or incoming holder.";
                    return false;
                }

                manifest.EnsureInitialized();
                List<ShuttleHolderLaunchManifestEntry> entries =
                    manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.MechChargerHolderKind);
                if (entries.Count == 0)
                {
                    notice = "[CeleTech Shuttle] No MechCharger manifest entries to restore.";
                    return true;
                }

                List<MechChargerReturnPlan> plans;
                if (!TryBuildMechChargerReturnPlans(
                    owner,
                    entries,
                    primarySource,
                    secondarySource,
                    out plans,
                    out notice))
                {
                    return false;
                }

                List<MechChargerReturnPlan> movedPlans = new List<MechChargerReturnPlan>();
                bool quarantinedAny = false;
                for (int i = 0; i < plans.Count; i++)
                {
                    MechChargerReturnPlan plan = plans[i];
                    string moduleInstanceID;
                    float chargeRateFactor;
                    bool moduleAvailable = TryResolveMechChargerRestoreModule(
                        owner,
                        plan.Entry,
                        out moduleInstanceID,
                        out chargeRateFactor);
                    if (!moduleAvailable)
                    {
                        if (allowSafeEjectWhenNoModule &&
                            TrySafeEjectReturnedMech(
                                owner,
                                plan.Pawn,
                                plan.SourceOwner,
                                map,
                                fallbackCell,
                                out string ejectFailureReason))
                        {
                            plan.Entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseEjected;
                            plan.Entry.DebugNotes = "MechCharger restore safely ejected mech because no enabled charger module was available.";
                            owner.ReleaseChargingReservation(plan.Entry.ThingID);
                            continue;
                        }

                        if (emergencyRecoveryOwner != null &&
                            plan.SourceOwner != null &&
                            plan.SourceOwner.Contains(plan.Pawn) &&
                            emergencyRecoveryOwner.TryAddOrTransfer(plan.Pawn, false))
                        {
                            plan.Entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseQuarantined;
                            plan.Entry.DebugNotes = "MechCharger restore quarantined mech because no enabled charger module was available and safe eject was unavailable.";
                            quarantinedAny = true;
                            continue;
                        }

                        notice = "[CeleTech Shuttle] MechCharger restore failed because no enabled charger module was available for mech thingID=" +
                            plan.Entry.ThingID +
                            ".";
                        TryRollbackReturnedMechsToSources(owner, movedPlans, notice);
                        return false;
                    }

                    if (!plan.AlreadyInMechCharger)
                    {
                        if (!owner.mechChargingHeldThings.TryAddOrTransfer(plan.Pawn, false))
                        {
                            notice = "[CeleTech Shuttle] MechCharger restore failed while returning mech thingID=" +
                                plan.Entry.ThingID +
                                ". Rolling back already-returned mechs.";
                            TryRollbackReturnedMechsToSources(owner, movedPlans, notice);
                            return false;
                        }

                        if (!owner.IsHeldPawn(plan.Pawn) ||
                            (plan.SourceOwner != null && plan.SourceOwner.Contains(plan.Pawn)))
                        {
                            notice = "[CeleTech Shuttle] MechCharger restore failed postcondition for mech thingID=" +
                                plan.Entry.ThingID +
                                ". Rolling back already-returned mechs.";
                            movedPlans.Add(plan);
                            TryRollbackReturnedMechsToSources(owner, movedPlans, notice);
                            return false;
                        }
                    }

                    owner.RemoveChargingRecord(plan.Pawn);
                    owner.chargingRecords.Add(new ShuttleMechChargingRecord(
                        plan.Pawn,
                        moduleInstanceID,
                        chargeRateFactor,
                        plan.Entry.HolderEntryTick,
                        plan.Entry.LastChargeTick));
                    owner.ReleaseChargingReservation(plan.Entry.ThingID);
                    plan.Entry.TransferPhase = successPhase;
                    plan.Entry.DebugNotes = successNotes;
                    movedPlans.Add(plan);
                }

                if (quarantinedAny)
                {
                    notice = "[CeleTech Shuttle] MechCharger restore quarantined one or more mechs for manual recovery.";
                    return false;
                }

                manifest.RemoveMechChargerEntries();
                notice = "[CeleTech Shuttle] MechCharger restore completed for " + plans.Count + " mech(s).";
                return true;
            }

            private static bool TryBuildMechChargerReturnPlans(
                CompShuttleMechChargerOccupancy owner,
                List<ShuttleHolderLaunchManifestEntry> entries,
                ThingOwner<Thing> primarySource,
                ThingOwner<Thing> secondarySource,
                out List<MechChargerReturnPlan> plans,
                out string failureReason)
            {
                plans = new List<MechChargerReturnPlan>();
                failureReason = null;
                HashSet<int> seenThingIDs = new HashSet<int>();
                for (int i = 0; i < entries.Count; i++)
                {
                    ShuttleHolderLaunchManifestEntry entry = entries[i];
                    if (entry == null ||
                        entry.HolderKind != ShuttleHolderLaunchManifestConstants.MechChargerHolderKind ||
                        entry.ThingID <= 0 ||
                        !seenThingIDs.Add(entry.ThingID))
                    {
                        failureReason = "[CeleTech Shuttle] MechCharger restore refused invalid or duplicate manifest entry.";
                        return false;
                    }

                    Pawn heldPawn = owner.FindHeldMechByThingID(entry.ThingID);
                    Pawn sourcePawn;
                    ThingOwner<Thing> sourceOwner;
                    TryFindMechInSources(owner, entry.ThingID, primarySource, secondarySource, out sourcePawn, out sourceOwner);
                    if (sourcePawn == null && heldPawn == null)
                    {
                        failureReason = "[CeleTech Shuttle] MechCharger restore could not find mech thingID=" +
                            entry.ThingID +
                            " in launch handoff, incoming holder, or MechCharger holder.";
                        return false;
                    }

                    if (sourcePawn != null && heldPawn != null && sourcePawn != heldPawn)
                    {
                        failureReason = "[CeleTech Shuttle] MechCharger restore refused duplicate mech owner state for thingID=" +
                            entry.ThingID +
                            ".";
                        return false;
                    }

                    Pawn pawn = sourcePawn ?? heldPawn;
                    if (!ValidateReturnedMech(entry, pawn, out failureReason))
                    {
                        return false;
                    }

                    plans.Add(new MechChargerReturnPlan(
                        entry,
                        pawn,
                        sourceOwner,
                        heldPawn == pawn || owner.IsHeldPawn(pawn)));
                }

                return true;
            }

            private static bool ValidateReturnedMech(
                ShuttleHolderLaunchManifestEntry entry,
                Pawn pawn,
                out string failureReason)
            {
                failureReason = null;
                if (pawn == null || pawn.Destroyed || pawn.Dead)
                {
                    failureReason = "[CeleTech Shuttle] MechCharger restore refused invalid mech for thingID=" +
                        (entry != null ? entry.ThingID : -1) +
                        ".";
                    return false;
                }

                if (pawn.RaceProps == null ||
                    !pawn.RaceProps.IsMechanoid ||
                    !ShuttleMechChargeNeedUtility.HasChargeNeed(pawn))
                {
                    failureReason = "[CeleTech Shuttle] MechCharger restore refused non-mech or mech without energy need. thingID=" +
                        (entry != null ? entry.ThingID : -1) +
                        ".";
                    return false;
                }

                return true;
            }

            private static bool TryResolveMechChargerRestoreModule(
                CompShuttleMechChargerOccupancy owner,
                ShuttleHolderLaunchManifestEntry entry,
                out string moduleInstanceID,
                out float chargeRateFactor)
            {
                moduleInstanceID = entry != null ? entry.ModuleInstanceID : null;
                chargeRateFactor = entry != null ? entry.ChargeRateFactor : 1f;
                int mechThingID = entry != null ? entry.ThingID : -1;
                if (owner.IsModuleAvailableForCharging(moduleInstanceID, mechThingID))
                {
                    chargeRateFactor = owner.GetValidChargeRateFactor(chargeRateFactor);
                    return true;
                }

                return owner.TryFindAvailableChargerModule(mechThingID, out moduleInstanceID, out chargeRateFactor);
            }

            private static bool TryFindMechInSources(
                CompShuttleMechChargerOccupancy owner,
                int thingID,
                ThingOwner<Thing> primarySource,
                ThingOwner<Thing> secondarySource,
                out Pawn pawn,
                out ThingOwner<Thing> sourceOwner)
            {
                pawn = FindPawnInOwner(primarySource, thingID);
                if (pawn != null)
                {
                    sourceOwner = primarySource;
                    return true;
                }

                pawn = FindPawnInOwner(secondarySource, thingID);
                if (pawn != null)
                {
                    sourceOwner = secondarySource;
                    return true;
                }

                sourceOwner = null;
                return false;
            }

            private static Pawn FindPawnInOwner(ThingOwner<Thing> owner, int thingID)
            {
                if (owner == null || thingID <= 0)
                {
                    return null;
                }

                for (int i = 0; i < owner.Count; i++)
                {
                    Pawn pawn = owner[i] as Pawn;
                    if (pawn != null && pawn.thingIDNumber == thingID)
                    {
                        return pawn;
                    }
                }

                return null;
            }

            private static bool TrySafeEjectReturnedMech(
                CompShuttleMechChargerOccupancy owner,
                Pawn pawn,
                ThingOwner<Thing> sourceOwner,
                Map map,
                IntVec3 fallbackCell,
                out string failureReason)
            {
                failureReason = null;
                if (pawn == null || pawn.Destroyed)
                {
                    failureReason = "[CeleTech Shuttle] MechCharger restore could not eject invalid mech.";
                    return false;
                }

                if (map == null || !fallbackCell.IsValid)
                {
                    failureReason = "[CeleTech Shuttle] MechCharger restore could not eject mech because no map or fallback cell is available.";
                    return false;
                }

                if (sourceOwner != null && sourceOwner.Contains(pawn))
                {
                    Thing resultingThing;
                    if (sourceOwner.TryDrop(
                        pawn,
                        fallbackCell,
                        map,
                        ThingPlaceMode.Near,
                        out resultingThing,
                        null,
                        null))
                    {
                        return true;
                    }

                    failureReason = "[CeleTech Shuttle] MechCharger restore source holder refused to drop mech.";
                    return false;
                }

                if (owner.IsHeldPawn(pawn))
                {
                    return owner.TryEjectChargingMech(pawn, map, out failureReason);
                }

                if (pawn.Spawned)
                {
                    return true;
                }

                if (GenPlace.TryPlaceThing(pawn, fallbackCell, map, ThingPlaceMode.Near))
                {
                    return true;
                }

                failureReason = "[CeleTech Shuttle] MechCharger restore could not place mech near incoming shuttle.";
                return false;
            }

            private static bool TryRollbackMovedMechsToCharger(
                CompShuttleMechChargerOccupancy owner,
                List<Pawn> movedPawns,
                ThingOwner<Thing> source,
                string reason)
            {
                if (movedPawns == null || movedPawns.Count == 0)
                {
                    return true;
                }

                bool allReturned = true;
                for (int i = movedPawns.Count - 1; i >= 0; i--)
                {
                    Pawn pawn = movedPawns[i];
                    if (pawn == null || pawn.Destroyed || owner.IsHeldPawn(pawn))
                    {
                        continue;
                    }

                    if (source == null || !source.Contains(pawn) || !owner.mechChargingHeldThings.TryAddOrTransfer(pawn, false))
                    {
                        allReturned = false;
                        Log.Error("[CeleTech Shuttle] MechCharger export rollback failed for mech=" +
                            pawn +
                            " reason=" +
                            (reason ?? "null"));
                        continue;
                    }
                }

                owner.ReconcileChargingRecordsToHeldMechs();
                return allReturned;
            }

            private static bool TryRollbackReturnedMechsToSources(
                CompShuttleMechChargerOccupancy owner,
                List<MechChargerReturnPlan> movedPlans,
                string reason)
            {
                if (movedPlans == null || movedPlans.Count == 0)
                {
                    return true;
                }

                bool allReturned = true;
                for (int i = movedPlans.Count - 1; i >= 0; i--)
                {
                    MechChargerReturnPlan plan = movedPlans[i];
                    if (plan == null || plan.AlreadyInMechCharger || plan.SourceOwner == null)
                    {
                        continue;
                    }

                    owner.RemoveChargingRecord(plan.Pawn);
                    if (owner.IsHeldPawn(plan.Pawn) &&
                        !plan.SourceOwner.TryAddOrTransfer(plan.Pawn, false))
                    {
                        allReturned = false;
                        Log.Error("[CeleTech Shuttle] MechCharger restore rollback failed for mech=" +
                            plan.Pawn +
                            " reason=" +
                            (reason ?? "null"));
                    }
                }

                return allReturned;
            }

            private sealed class MechChargerLaunchTransferPlan
            {
                internal MechChargerLaunchTransferPlan(
                    Pawn pawn,
                    int originalRecordIndex,
                    string moduleInstanceID,
                    float chargeRateFactor,
                    int entryTick,
                    int lastChargeTick)
                {
                    this.Pawn = pawn;
                    this.PawnThingID = pawn != null ? pawn.thingIDNumber : -1;
                    this.PawnDefName = pawn != null && pawn.def != null ? pawn.def.defName : null;
                    this.PawnLabel = pawn != null ? pawn.LabelShort : null;
                    this.OriginalRecordIndex = originalRecordIndex;
                    this.ModuleInstanceID = moduleInstanceID;
                    this.ChargeRateFactor = chargeRateFactor;
                    this.EntryTick = entryTick;
                    this.LastChargeTick = lastChargeTick;
                }

                internal readonly Pawn Pawn;
                internal readonly int PawnThingID;
                internal readonly string PawnDefName;
                internal readonly string PawnLabel;
                internal readonly int OriginalRecordIndex;
                internal readonly string ModuleInstanceID;
                internal readonly float ChargeRateFactor;
                internal readonly int EntryTick;
                internal readonly int LastChargeTick;
            }

            private sealed class MechChargerReturnPlan
            {
                internal MechChargerReturnPlan(
                    ShuttleHolderLaunchManifestEntry entry,
                    Pawn pawn,
                    ThingOwner<Thing> sourceOwner,
                    bool alreadyInMechCharger)
                {
                    this.Entry = entry;
                    this.Pawn = pawn;
                    this.SourceOwner = sourceOwner;
                    this.AlreadyInMechCharger = alreadyInMechCharger;
                }

                internal readonly ShuttleHolderLaunchManifestEntry Entry;
                internal readonly Pawn Pawn;
                internal readonly ThingOwner<Thing> SourceOwner;
                internal readonly bool AlreadyInMechCharger;
            }
        }
    }
}

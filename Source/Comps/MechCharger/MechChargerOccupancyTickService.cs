using CeleTech.ShuttleExtension.ModularShuttle.MechCharging;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttleMechChargerOccupancy
    {
        internal static class MechChargerOccupancyTickService
        {
            internal static void TickChargingMechs(CompShuttleMechChargerOccupancy owner)
            {
                if (owner.chargingRecords == null || owner.chargingRecords.Count == 0)
                {
                    return;
                }

                bool powered = MechChargerAdmissionValidator.IsMechChargerPowered(owner.parent);
                Map map = owner.parent != null ? owner.parent.Map : null;
                int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : -1;
                for (int i = owner.chargingRecords.Count - 1; i >= 0; i--)
                {
                    ShuttleMechChargingRecord record = owner.chargingRecords[i];
                    Pawn mech = record != null ? record.Pawn : null;
                    if (!owner.IsValidHeldChargingMech(mech))
                    {
                        owner.HandleInvalidChargingRecord(record, map);
                        continue;
                    }

                    if (!owner.TryResolveRecordModule(record))
                    {
                        owner.TryEjectChargingMech(mech, map, out string unusedFailureReason);
                        continue;
                    }

                    if (!powered)
                    {
                        continue;
                    }

                    if (ShuttleMechChargeNeedUtility.IsAtRechargeLimit(mech))
                    {
                        MechChargerOccupancyEjectionService.TryCompleteChargingMech(
                            owner,
                            mech,
                            map,
                            out string unusedFailureReason);
                        continue;
                    }

                    float chargeRate = VanillaMechChargePerTick * record.ChargeRateFactor;
                    bool reachedLimit;
                    if (!ShuttleMechChargeNeedUtility.TryCharge(mech, chargeRate, out reachedLimit))
                    {
                        owner.TryEjectChargingMech(mech, map, out string unusedFailureReason);
                        continue;
                    }

                    record.UpdateChargeTick(currentTick);

                    if (reachedLimit)
                    {
                        MechChargerOccupancyEjectionService.TryCompleteChargingMech(
                            owner,
                            mech,
                            map,
                            out string unusedFailureReason);
                    }
                }
            }
        }
    }
}

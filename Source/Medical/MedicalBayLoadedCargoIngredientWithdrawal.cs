using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    /// <summary>
    /// Medical-facing aggregate over exact Cargo withdrawals used by one virtual surgery.
    /// Cargo retains source rollback and emergency placement mechanics.
    /// </summary>
    internal sealed class MedicalBayLoadedCargoIngredientWithdrawal
    {
        private readonly List<ShuttleCargoWithdrawal> withdrawals =
            new List<ShuttleCargoWithdrawal>();
        private bool isCommitted;

        internal IReadOnlyList<Thing> TakenThings
        {
            get
            {
                List<Thing> things = new List<Thing>();
                for (int i = 0; i < this.withdrawals.Count; i++)
                {
                    Thing thing = this.withdrawals[i] != null
                        ? this.withdrawals[i].Thing
                        : null;
                    if (thing != null)
                    {
                        things.Add(thing);
                    }
                }

                return things;
            }
        }

        internal void Add(ShuttleCargoWithdrawal withdrawal)
        {
            if (withdrawal != null)
            {
                this.withdrawals.Add(withdrawal);
            }
        }

        internal void CommitConsumed(RecipeDef recipeDef, Map map)
        {
            for (int i = 0; i < this.withdrawals.Count; i++)
            {
                ShuttleCargoWithdrawal withdrawal = this.withdrawals[i];
                Thing thing = withdrawal != null ? withdrawal.Thing : null;
                if (withdrawal == null || withdrawal.IsCompleted)
                {
                    continue;
                }

                if (thing != null && !thing.Destroyed)
                {
                    if (recipeDef != null && recipeDef.Worker != null)
                    {
                        recipeDef.Worker.ConsumeIngredient(thing, recipeDef, map);
                    }
                    else
                    {
                        thing.Destroy(DestroyMode.Vanish);
                    }
                }

                if (!withdrawal.CommitAlreadyConsumed())
                {
                    withdrawal.CommitReleased();
                }
            }

            this.isCommitted = true;
        }

        internal bool RollbackOrDrop(
            Pawn doctor,
            ThingWithComps shuttleHost,
            out string notice)
        {
            notice = null;
            if (this.isCommitted)
            {
                return false;
            }

            bool allResolved = true;
            for (int i = this.withdrawals.Count - 1; i >= 0; i--)
            {
                ShuttleCargoWithdrawal withdrawal = this.withdrawals[i];
                if (withdrawal == null || withdrawal.IsCompleted)
                {
                    continue;
                }

                Thing thing = withdrawal.Thing;
                if (thing == null || thing.Destroyed || thing.stackCount <= 0)
                {
                    withdrawal.CommitAlreadyConsumed();
                    continue;
                }

                string failureReason;
                if (withdrawal.TryRollBackToSource(out failureReason))
                {
                    notice = "CT_Shuttle_MedicalBay_LoadedCargoMedicineReturned"
                        .Translate()
                        .ToString();
                    continue;
                }

                ThingOwner doctorInventory = doctor != null && doctor.inventory != null
                    ? doctor.inventory.innerContainer
                    : null;
                if (doctorInventory != null &&
                    withdrawal.TryRecoverToOwner(
                        doctorInventory,
                        out failureReason))
                {
                    notice = "CT_Shuttle_MedicalBay_MedicineReturned"
                        .Translate()
                        .ToString();
                    continue;
                }

                if (shuttleHost != null &&
                    withdrawal.TryRecoverNear(shuttleHost, out failureReason))
                {
                    notice = "CT_Shuttle_MedicalBay_MedicineDroppedOnFailure"
                        .Translate()
                        .ToString();
                    continue;
                }

                if (doctor != null &&
                    withdrawal.TryRecoverNear(doctor, out failureReason))
                {
                    notice = "CT_Shuttle_MedicalBay_MedicineDroppedOnFailure"
                        .Translate()
                        .ToString();
                    continue;
                }

                allResolved = false;
                if (Prefs.DevMode)
                {
                    Log.Warning(
                        "[CeleTech Shuttle] Medical Bay surgery ingredient rollback failed; " +
                        "ingredient remains in its current state. failure=" +
                        (failureReason ?? "null"));
                }
            }

            return allResolved;
        }
    }
}

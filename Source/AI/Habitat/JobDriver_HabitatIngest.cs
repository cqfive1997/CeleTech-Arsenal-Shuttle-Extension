using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat
{
    public sealed class JobDriver_HabitatIngest : JobDriver
    {
        private Thing ShuttleHost
        {
            get
            {
                return this.job != null ? this.job.GetTarget(TargetIndex.A).Thing : null;
            }
        }

        private Thing Food
        {
            get
            {
                return this.job != null ? this.job.GetTarget(TargetIndex.B).Thing : null;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Thing shuttleHost = this.ShuttleHost;
            if (shuttleHost == null || !shuttleHost.Spawned)
            {
                return false;
            }

            if (!this.IsCargoFoodMode() && !this.FoodStillInInventory())
            {
                return false;
            }

            string failReason;
            if (!HabitatUtility.CanUseHabitatForDining(this.pawn, shuttleHost, out failReason))
            {
                return false;
            }

            if (!HabitatUtility.HasAvailableDiningSlot(this.pawn, shuttleHost, out failReason) &&
                !this.HasAvailableDiningSlotForCurrentJob())
            {
                return false;
            }

            ShuttleProfile profile;
            HabitatProfile habitat;
            if (!HabitatUtility.TryGetPoweredHabitatProfile(shuttleHost, out profile, out habitat))
            {
                return false;
            }

            return this.pawn.Reserve(shuttleHost, this.job, habitat.DiningSlots, 0, null, errorOnFailed, false);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !this.CanStillUseHabitatForDining());
            this.FailOn(() => !this.FoodSourceStillValid());

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A)
                .FailOn(() => !this.CanStillUseHabitatForDining())
                .FailOn(() => !this.FoodSourceStillValid())
                .FailOn(() => !this.HasAvailableDiningSlotForCurrentJob());

            yield return this.MakeEnterHabitatDiningToil();
        }

        private Toil MakeEnterHabitatDiningToil()
        {
            Toil toil = ToilMaker.MakeToil("EnterHabitatIngest");
            toil.initAction = delegate
            {
                CompShuttleHabitatOccupancy occupancy = this.GetHabitatOccupancy();
                if (occupancy == null)
                {
                    this.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                string failReason;
                if (this.IsCargoFoodMode())
                {
                    if (!this.TryEnterHabitatDiningFromCargo(occupancy, out failReason))
                    {
                        this.EndJobWith(JobCondition.Incompletable);
                    }

                    return;
                }

                int count = this.job != null && this.job.count > 0 ? this.job.count : 1;
                if (!occupancy.TryEnterForDining(this.pawn, this.Food, count, out failReason))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private bool TryEnterHabitatDiningFromCargo(
            CompShuttleHabitatOccupancy occupancy,
            out string failReason)
        {
            failReason = null;

            ThingWithComps shuttleWithComps = this.ShuttleHost as ThingWithComps;
            if (shuttleWithComps == null)
            {
                failReason = "CT_Shuttle_HabitatDiningUnavailable".Translate().ToString();
                return false;
            }

            HabitatProfile habitat;
            if (!this.TryGetCurrentHabitat(out habitat) ||
                habitat == null ||
                !habitat.AllowsCargoFoodWithdrawal)
            {
                failReason = "CT_Shuttle_HabitatNoFood".Translate().ToString();
                return false;
            }

            IShuttleHabitatFoodSource foodSource;
            if (!HabitatUtility.TryGetHabitatFoodSource(shuttleWithComps, out foodSource))
            {
                failReason = "CT_Shuttle_HabitatNoFood".Translate().ToString();
                return false;
            }

            HabitatFoodWithdrawal withdrawal;
            if (!occupancy.TryBeginDiningCargoFoodWithdrawal(
                this.pawn,
                habitat,
                foodSource,
                out withdrawal,
                out failReason))
            {
                return false;
            }

            if (!occupancy.TryEnterForDiningFromCargoWithdrawal(this.pawn, withdrawal, out failReason))
            {
                return false;
            }

            return true;
        }

        private bool CanStillUseHabitatForDining()
        {
            string failReason;
            return HabitatUtility.CanUseHabitatForDining(this.pawn, this.ShuttleHost, out failReason);
        }

        private bool HasAvailableDiningSlotForCurrentJob()
        {
            HabitatProfile habitat;
            if (!this.TryGetCurrentHabitat(out habitat))
            {
                return false;
            }

            int currentUsers = HabitatUtility.CountCurrentHabitatDiningUsers(this.ShuttleHost, this.pawn);
            return currentUsers < habitat.DiningSlots;
        }

        private bool TryGetCurrentHabitat(out HabitatProfile habitat)
        {
            habitat = null;
            ShuttleProfile profile;
            return HabitatUtility.TryGetPoweredHabitatProfile(
                this.ShuttleHost,
                out profile,
                out habitat);
        }

        private bool FoodStillInInventory()
        {
            Thing food = this.Food;
            return food != null &&
                !food.Destroyed &&
                this.pawn != null &&
                this.pawn.inventory != null &&
                this.pawn.inventory.Contains(food) &&
                HabitatIngestUtility.IsUsableHabitatFood(this.pawn, food);
        }

        private bool FoodSourceStillValid()
        {
            if (!this.IsCargoFoodMode())
            {
                return this.FoodStillInInventory();
            }

            return this.CanStillProvideCargoFood();
        }

        private bool CanStillProvideCargoFood()
        {
            ThingWithComps shuttleWithComps = this.ShuttleHost as ThingWithComps;
            HabitatProfile habitat;
            IShuttleHabitatFoodSource foodSource;
            return shuttleWithComps != null &&
                this.TryGetCurrentHabitat(out habitat) &&
                habitat != null &&
                habitat.AllowsCargoFoodWithdrawal &&
                HabitatUtility.TryGetHabitatFoodSource(shuttleWithComps, out foodSource) &&
                foodSource.CanProvideFood(shuttleWithComps, this.pawn, habitat);
        }

        private bool IsCargoFoodMode()
        {
            if (this.job == null)
            {
                return false;
            }

            // Cargo-food mode uses a targetA-only Habitat ingest job. A valid targetB means
            // the original inventory-food path and must remain preferred.
            LocalTargetInfo foodTarget = this.job.GetTarget(TargetIndex.B);
            return !foodTarget.IsValid || !foodTarget.HasThing;
        }

        private CompShuttleHabitatOccupancy GetHabitatOccupancy()
        {
            ThingWithComps shuttleWithComps = this.ShuttleHost as ThingWithComps;
            return shuttleWithComps != null
                ? shuttleWithComps.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
        }
    }
}

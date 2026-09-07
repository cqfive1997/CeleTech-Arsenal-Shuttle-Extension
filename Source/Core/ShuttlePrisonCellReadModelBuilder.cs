using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Prisoners;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    /// <summary>
    /// Builds a read-only Prison Cell snapshot from profile, holder state, and map pawns.
    /// It never changes guest status, jobs, holder ownership, or prisoner records.
    /// </summary>
    internal sealed class ShuttlePrisonCellReadModelBuilder
    {
        private const int MaxCandidateReadModels = 64;
        private const int MaxCarrierReadModels = 32;
        private readonly PrisonCellAdmissionValidator admissionValidator =
            new PrisonCellAdmissionValidator();
        private readonly PrisonCellFeedingValidator feedingValidator =
            new PrisonCellFeedingValidator();
        private readonly PrisonCellTreatmentValidator treatmentValidator =
            new PrisonCellTreatmentValidator();

        internal ShuttlePrisonCellReadModel Build(
            ShuttleProfile profile,
            ThingWithComps shuttleHost,
            ShuttlePrisonCellSupplyConfigState supplyConfig,
            bool includeActionDetails = true)
        {
            ShuttlePrisonCellReadModel model = new ShuttlePrisonCellReadModel();
            if (supplyConfig != null)
            {
                model.CargoFoodSupplyConfiguredEnabled =
                    supplyConfig.CargoFoodSupplyEnabled;
                model.RefrigeratedFoodSupplyConfiguredEnabled =
                    supplyConfig.RefrigeratedFoodSupplyEnabled;
                model.MaximumCargoFoodPreferability =
                    supplyConfig.MaximumFoodPreferability;
            }

            PrisonCellProfile prisonCell = profile != null ? profile.PrisonCell : null;
            if (prisonCell != null)
            {
                model.HasPrisonCell = prisonCell.HasPrisonCell;
                model.PrisonerSlots = prisonCell.HasPrisonCell ? prisonCell.PrisonerSlots : 0;
                model.SupportsCargoFoodSupply = prisonCell.SupportsCargoFoodSupply;
                model.SupportsRefrigeratedCargoFoodSupply =
                    prisonCell.SupportsRefrigeratedCargoFoodSupply;
                model.CargoFoodSupplyEnabled = prisonCell.CargoFoodSupplyEnabled;
                model.RefrigeratedCargoFoodSupplyEnabled =
                    prisonCell.RefrigeratedCargoFoodSupplyEnabled;
                model.RequiresCargoLogisticsForFoodSupply =
                    prisonCell.RequiresCargoLogisticsForFoodSupply;
                model.MaximumCargoFoodPreferability =
                    prisonCell.MaximumCargoFoodPreferability;
            }

            CompShuttlePrisonCellOccupancy occupancy = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttlePrisonCellOccupancy>()
                : null;
            if (!model.HasPrisonCell)
            {
                model.UnavailableReason = "CT_Shuttle_PrisonCell_NotInstalled".Translate().ToString();
                model.Prisoners = new List<ShuttleHeldPrisonerReadModel>();
                model.Candidates = new List<ShuttlePrisonerCandidateReadModel>();
                model.Carriers = new List<ShuttlePrisonerCarrierReadModel>();
                model.Feeders = new List<ShuttlePrisonerFeederReadModel>();
                model.Doctors = new List<ShuttlePrisonerDoctorReadModel>();
                return model;
            }

            if (occupancy == null)
            {
                model.UnavailableReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                model.Prisoners = new List<ShuttleHeldPrisonerReadModel>();
                model.Candidates = new List<ShuttlePrisonerCandidateReadModel>();
                model.Carriers = new List<ShuttlePrisonerCarrierReadModel>();
                model.Feeders = new List<ShuttlePrisonerFeederReadModel>();
                model.Doctors = new List<ShuttlePrisonerDoctorReadModel>();
                return model;
            }

            model.PrisonerCount = occupancy.PrisonerCount;
            model.FreePrisonerSlots = occupancy.FreePrisonerSlots;
            model.CanAdmitMore = model.FreePrisonerSlots > 0;
            if (!model.CanAdmitMore)
            {
                model.UnavailableReason = "CT_Shuttle_PrisonCell_Full".Translate().ToString();
            }

            List<Pawn> candidatePawns = includeActionDetails
                ? this.FindCandidatePawns(shuttleHost, occupancy)
                : new List<Pawn>();
            model.Carriers = includeActionDetails
                ? this.BuildCarrierModels(shuttleHost)
                : new List<ShuttlePrisonerCarrierReadModel>();
            model.Candidates = includeActionDetails
                ? this.BuildCandidateModels(shuttleHost, candidatePawns, model.Carriers)
                : new List<ShuttlePrisonerCandidateReadModel>();
            model.Feeders = includeActionDetails
                ? this.BuildFeederModels(shuttleHost)
                : new List<ShuttlePrisonerFeederReadModel>();
            model.Doctors = includeActionDetails
                ? this.BuildDoctorModels(shuttleHost)
                : new List<ShuttlePrisonerDoctorReadModel>();
            model.Prisoners = this.BuildHeldPrisonerModels(
                shuttleHost,
                occupancy,
                model.Feeders,
                model.Doctors,
                includeActionDetails);
            return model;
        }

        private IReadOnlyList<ShuttleHeldPrisonerReadModel> BuildHeldPrisonerModels(
            ThingWithComps shuttleHost,
            CompShuttlePrisonCellOccupancy occupancy,
            IReadOnlyList<ShuttlePrisonerFeederReadModel> feeders,
            IReadOnlyList<ShuttlePrisonerDoctorReadModel> doctors,
            bool includeActionDetails)
        {
            List<ShuttleHeldPrisonerReadModel> models =
                new List<ShuttleHeldPrisonerReadModel>();
            IReadOnlyList<Pawn> prisoners = occupancy != null
                ? occupancy.HeldPrisonersForReading
                : null;
            if (prisoners == null)
            {
                return models;
            }

            for (int i = 0; i < prisoners.Count; i++)
            {
                Pawn pawn = prisoners[i];
                if (pawn == null || pawn.Destroyed)
                {
                    continue;
                }

                ShuttleHeldPrisonerReadModel model = new ShuttleHeldPrisonerReadModel();
                model.ThingIDNumber = pawn.thingIDNumber;
                model.Label = pawn.LabelShortCap;
                model.FactionLabel = this.GetFactionLabel(pawn.Faction);
                model.HostFactionLabel = this.GetFactionLabel(
                    pawn.guest != null ? pawn.guest.HostFaction : null);
                model.InteractionModeLabel = this.GetInteractionModeLabel(pawn);
                model.FoodLevelPercent = this.GetFoodLevelPercent(pawn);
                model.FoodLabel = this.FormatPercentOrUnknown(model.FoodLevelPercent);
                model.HealthLabel = this.GetHealthLabel(pawn);
                model.IsDowned = pawn.Downed;
                model.IsDead = pawn.Dead;
                model.CanEject = pawn.thingIDNumber > 0 && !pawn.Destroyed;
                model.NeedsFeeding = this.NeedsFeeding(pawn);
                model.CareAvailabilityKnown = includeActionDetails;
                string feedFailure = null;
                model.CanFeed = includeActionDetails &&
                    this.HasFeederForPrisoner(shuttleHost, pawn, feeders, out feedFailure);
                model.FeedTooltip = model.CanFeed
                    ? "CT_Shuttle_PrisonCell_FeedTooltip".Translate().ToString()
                    : includeActionDetails ? feedFailure : string.Empty;
                model.NeedsTending = PrisonCellTreatmentValidator.PrisonerNeedsTending(pawn);
                model.TendStatusLabel = model.NeedsTending
                    ? "CT_Shuttle_PrisonCell_NeedsTending".Translate().ToString()
                    : "CT_Shuttle_PrisonCell_NoTendableInjury".Translate().ToString();
                string tendFailure = null;
                model.CanTend = includeActionDetails &&
                    this.HasDoctorForPrisoner(shuttleHost, pawn, doctors, out tendFailure);
                model.TendTooltip = model.CanTend
                    ? this.BuildTendTooltip(shuttleHost, pawn)
                    : includeActionDetails ? tendFailure : string.Empty;
                model.DisplayThing = pawn;
                models.Add(model);
            }

            models.Sort(CompareHeldPrisoners);
            return models;
        }

        private IReadOnlyList<ShuttlePrisonerFeederReadModel> BuildFeederModels(
            ThingWithComps shuttleHost)
        {
            List<ShuttlePrisonerFeederReadModel> feeders =
                new List<ShuttlePrisonerFeederReadModel>();
            if (shuttleHost == null || shuttleHost.Map == null || shuttleHost.Map.mapPawns == null)
            {
                return feeders;
            }

            IReadOnlyList<Pawn> pawns = shuttleHost.Map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return feeders;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!this.IsFeederCandidatePawn(pawn, shuttleHost))
                {
                    continue;
                }

                feeders.Add(this.BuildFeederModel(shuttleHost, pawn));
            }

            feeders.Sort(CompareFeeders);
            if (feeders.Count > MaxCarrierReadModels)
            {
                feeders.RemoveRange(
                    MaxCarrierReadModels,
                    feeders.Count - MaxCarrierReadModels);
            }

            return feeders;
        }

        private IReadOnlyList<ShuttlePrisonerDoctorReadModel> BuildDoctorModels(
            ThingWithComps shuttleHost)
        {
            List<ShuttlePrisonerDoctorReadModel> doctors =
                new List<ShuttlePrisonerDoctorReadModel>();
            if (shuttleHost == null || shuttleHost.Map == null || shuttleHost.Map.mapPawns == null)
            {
                return doctors;
            }

            IReadOnlyList<Pawn> pawns = shuttleHost.Map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return doctors;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!this.IsDoctorCandidatePawn(pawn, shuttleHost))
                {
                    continue;
                }

                doctors.Add(this.BuildDoctorModel(shuttleHost, pawn));
            }

            doctors.Sort(CompareDoctors);
            if (doctors.Count > MaxCarrierReadModels)
            {
                doctors.RemoveRange(
                    MaxCarrierReadModels,
                    doctors.Count - MaxCarrierReadModels);
            }

            return doctors;
        }

        private ShuttlePrisonerFeederReadModel BuildFeederModel(
            ThingWithComps shuttleHost,
            Pawn feeder)
        {
            ShuttlePrisonerFeederReadModel model =
                new ShuttlePrisonerFeederReadModel();
            model.ThingIDNumber = feeder != null ? feeder.thingIDNumber : -1;
            model.Label = feeder != null
                ? feeder.LabelShortCap
                : "CT_Shuttle_PrisonCell_NoFeeder".Translate().ToString();
            model.DisplayThing = feeder;

            // Display lists use static worker eligibility only. Exact reachability and
            // reservation checks remain at command/job start for the selected pair.
            model.CanFeed = this.IsFeederCandidatePawn(feeder, shuttleHost);
            model.CannotFeedReason = model.CanFeed
                ? string.Empty
                : "CT_Shuttle_PrisonCell_FeederUnavailable".Translate().ToString();
            return model;
        }

        private ShuttlePrisonerDoctorReadModel BuildDoctorModel(
            ThingWithComps shuttleHost,
            Pawn doctor)
        {
            ShuttlePrisonerDoctorReadModel model =
                new ShuttlePrisonerDoctorReadModel();
            model.ThingIDNumber = doctor != null ? doctor.thingIDNumber : -1;
            model.Label = doctor != null
                ? doctor.LabelShortCap
                : "CT_Shuttle_PrisonCell_NoDoctor".Translate().ToString();
            model.DisplayThing = doctor;

            model.CanTend = this.IsDoctorCandidatePawn(doctor, shuttleHost);
            model.CannotTendReason = model.CanTend
                ? string.Empty
                : "CT_Shuttle_PrisonCell_DoctorUnavailable".Translate().ToString();
            return model;
        }

        private List<Pawn> FindCandidatePawns(
            ThingWithComps shuttleHost,
            CompShuttlePrisonCellOccupancy occupancy)
        {
            List<Pawn> candidates = new List<Pawn>();
            if (shuttleHost == null || shuttleHost.Map == null || shuttleHost.Map.mapPawns == null)
            {
                return candidates;
            }

            IReadOnlyList<Pawn> pawns = shuttleHost.Map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return candidates;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!this.IsCandidatePawn(pawn) ||
                    (occupancy != null && occupancy.ContainsPrisoner(pawn)))
                {
                    continue;
                }

                candidates.Add(pawn);
            }

            candidates.Sort(ComparePawnsByLabel);
            if (candidates.Count > MaxCandidateReadModels)
            {
                candidates.RemoveRange(
                    MaxCandidateReadModels,
                    candidates.Count - MaxCandidateReadModels);
            }

            return candidates;
        }

        private IReadOnlyList<ShuttlePrisonerCandidateReadModel> BuildCandidateModels(
            ThingWithComps shuttleHost,
            List<Pawn> candidatePawns,
            IReadOnlyList<ShuttlePrisonerCarrierReadModel> carriers)
        {
            List<ShuttlePrisonerCandidateReadModel> models =
                new List<ShuttlePrisonerCandidateReadModel>();
            if (candidatePawns == null)
            {
                return models;
            }

            for (int i = 0; i < candidatePawns.Count; i++)
            {
                Pawn pawn = candidatePawns[i];
                ShuttlePrisonerCandidateReadModel model =
                    this.BuildCandidateModel(shuttleHost, pawn, carriers);
                if (model != null)
                {
                    models.Add(model);
                }
            }

            models.Sort(CompareCandidates);
            return models;
        }

        private ShuttlePrisonerCandidateReadModel BuildCandidateModel(
            ThingWithComps shuttleHost,
            Pawn pawn,
            IReadOnlyList<ShuttlePrisonerCarrierReadModel> carriers)
        {
            if (pawn == null || pawn.thingIDNumber <= 0)
            {
                return null;
            }

            ShuttlePrisonerCandidateReadModel model =
                new ShuttlePrisonerCandidateReadModel();
            model.ThingIDNumber = pawn.thingIDNumber;
            model.Label = pawn.LabelShortCap;
            model.FactionLabel = this.GetFactionLabel(pawn.Faction);
            model.IsExistingPrisoner = this.IsPlayerPrisonerCandidate(pawn);
            model.IsDownedHostile = this.IsDownedHostileCandidate(pawn);
            model.DisplayThing = pawn;
            model.ReasonLabel = model.IsExistingPrisoner
                ? "CT_Shuttle_PrisonCell_ExistingPrisoner".Translate().ToString()
                : "CT_Shuttle_PrisonCell_DownedHostile".Translate().ToString();

            string failReason;
            if (!this.admissionValidator.CanAdmitSpawnedPrisoner(
                shuttleHost,
                pawn,
                out failReason))
            {
                model.CanAdmit = false;
                model.CannotAdmitReason = failReason;
                return model;
            }

            if (!this.HasCarrierForCandidate(pawn, carriers, out failReason))
            {
                model.CanAdmit = false;
                model.CannotAdmitReason = failReason;
                return model;
            }

            model.CanAdmit = true;
            model.CannotAdmitReason = string.Empty;
            return model;
        }

        private IReadOnlyList<ShuttlePrisonerCarrierReadModel> BuildCarrierModels(
            ThingWithComps shuttleHost)
        {
            List<ShuttlePrisonerCarrierReadModel> carriers =
                new List<ShuttlePrisonerCarrierReadModel>();
            if (shuttleHost == null || shuttleHost.Map == null || shuttleHost.Map.mapPawns == null)
            {
                return carriers;
            }

            IReadOnlyList<Pawn> pawns = shuttleHost.Map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return carriers;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!this.IsCarrierCandidatePawn(pawn, shuttleHost))
                {
                    continue;
                }

                ShuttlePrisonerCarrierReadModel model =
                    this.BuildCarrierModel(shuttleHost, pawn);
                carriers.Add(model);
            }

            carriers.Sort(CompareCarriers);
            if (carriers.Count > MaxCarrierReadModels)
            {
                carriers.RemoveRange(
                    MaxCarrierReadModels,
                    carriers.Count - MaxCarrierReadModels);
            }

            return carriers;
        }

        private ShuttlePrisonerCarrierReadModel BuildCarrierModel(
            ThingWithComps shuttleHost,
            Pawn carrier)
        {
            ShuttlePrisonerCarrierReadModel model =
                new ShuttlePrisonerCarrierReadModel();
            model.ThingIDNumber = carrier != null ? carrier.thingIDNumber : -1;
            model.Label = carrier != null
                ? carrier.LabelShortCap
                : "CT_Shuttle_PrisonCell_NoCarrier".Translate().ToString();
            model.DisplayThing = carrier;

            model.CanCarry = this.IsCarrierCandidatePawn(carrier, shuttleHost);
            model.CannotCarryReason = model.CanCarry
                ? string.Empty
                : "CT_Shuttle_PrisonCell_CarrierUnavailable".Translate().ToString();
            return model;
        }

        private bool HasCarrierForCandidate(
            Pawn candidate,
            IReadOnlyList<ShuttlePrisonerCarrierReadModel> carriers,
            out string failReason)
        {
            failReason = null;
            if (carriers == null || carriers.Count == 0)
            {
                failReason = "CT_Shuttle_PrisonCell_NoCarrier".Translate().ToString();
                return false;
            }

            for (int i = 0; i < carriers.Count; i++)
            {
                ShuttlePrisonerCarrierReadModel carrierModel = carriers[i];
                if (carrierModel != null &&
                    carrierModel.CanCarry &&
                    carrierModel.DisplayThing != candidate)
                {
                    return true;
                }
            }

            failReason = "CT_Shuttle_PrisonCell_NoCarrier".Translate().ToString();
            return false;
        }

        private bool IsCandidatePawn(Pawn pawn)
        {
            return this.IsPlayerPrisonerCandidate(pawn) ||
                this.IsDownedHostileCandidate(pawn);
        }

        private bool IsPlayerPrisonerCandidate(Pawn pawn)
        {
            return pawn != null &&
                pawn.guest != null &&
                pawn.guest.IsPrisoner &&
                (pawn.guest.HostFaction == Faction.OfPlayer ||
                    pawn.guest.HostFaction == null);
        }

        private bool IsDownedHostileCandidate(Pawn pawn)
        {
            return pawn != null &&
                pawn.RaceProps != null &&
                pawn.RaceProps.Humanlike &&
                !pawn.RaceProps.IsMechanoid &&
                pawn.Downed &&
                pawn.HostileTo(Faction.OfPlayer);
        }

        private bool IsCarrierCandidatePawn(Pawn pawn, ThingWithComps shuttleHost)
        {
            if (pawn == null ||
                pawn.Destroyed ||
                pawn.Dead ||
                !pawn.Spawned ||
                pawn.Map == null ||
                pawn.Downed ||
                pawn.Drafted ||
                pawn.MentalState != null ||
                pawn.carryTracker == null ||
                pawn.RaceProps == null ||
                !pawn.RaceProps.Humanlike ||
                pawn.Faction != Faction.OfPlayer ||
                !pawn.IsColonist)
            {
                return false;
            }

            if (shuttleHost == null ||
                shuttleHost.Map == null ||
                pawn.Map != shuttleHost.Map)
            {
                return false;
            }

            return this.CanUseCapacity(pawn, PawnCapacityDefOf.Moving) &&
                this.CanUseCapacity(pawn, PawnCapacityDefOf.Manipulation);
        }

        private bool IsFeederCandidatePawn(Pawn pawn, ThingWithComps shuttleHost)
        {
            string failReason;
            return this.feedingValidator.CanUseFeeder(shuttleHost, pawn, out failReason);
        }

        private bool IsDoctorCandidatePawn(Pawn pawn, ThingWithComps shuttleHost)
        {
            string failReason;
            return this.treatmentValidator.CanUseDoctor(shuttleHost, pawn, out failReason);
        }

        private bool NeedsFeeding(Pawn pawn)
        {
            return pawn != null &&
                pawn.needs != null &&
                pawn.needs.food != null &&
                pawn.needs.food.CurLevelPercentage < PrisonCellFeedingValidator.NeedsFeedingThreshold;
        }

        private bool HasFeederForPrisoner(
            ThingWithComps shuttleHost,
            Pawn prisoner,
            IReadOnlyList<ShuttlePrisonerFeederReadModel> feeders,
            out string failReason)
        {
            failReason = null;
            string prisonerFailure;
            if (!this.feedingValidator.CanUsePrisonerForFeeding(prisoner, out prisonerFailure))
            {
                failReason = prisonerFailure;
                return false;
            }

            if (!this.feedingValidator.HasFoodAvailable(shuttleHost, prisoner))
            {
                failReason = "CT_Shuttle_PrisonCell_NoFood".Translate().ToString();
                return false;
            }

            if (feeders == null || feeders.Count == 0)
            {
                failReason = "CT_Shuttle_PrisonCell_NoFeeder".Translate().ToString();
                return false;
            }

            for (int i = 0; i < feeders.Count; i++)
            {
                ShuttlePrisonerFeederReadModel feederModel = feeders[i];
                if (feederModel != null && feederModel.CanFeed)
                {
                    return true;
                }
            }

            failReason = "CT_Shuttle_PrisonCell_NoFeeder".Translate().ToString();
            return false;
        }

        private bool HasDoctorForPrisoner(
            ThingWithComps shuttleHost,
            Pawn prisoner,
            IReadOnlyList<ShuttlePrisonerDoctorReadModel> doctors,
            out string failReason)
        {
            failReason = null;
            string prisonerFailure;
            if (!this.treatmentValidator.CanUsePrisonerForTending(prisoner, out prisonerFailure))
            {
                failReason = prisonerFailure;
                return false;
            }

            if (doctors == null || doctors.Count == 0)
            {
                failReason = "CT_Shuttle_PrisonCell_NoDoctor".Translate().ToString();
                return false;
            }

            for (int i = 0; i < doctors.Count; i++)
            {
                ShuttlePrisonerDoctorReadModel doctorModel = doctors[i];
                if (doctorModel != null && doctorModel.CanTend)
                {
                    return true;
                }
            }

            failReason = "CT_Shuttle_PrisonCell_NoDoctor".Translate().ToString();
            return false;
        }

        private string BuildTendTooltip(ThingWithComps shuttleHost, Pawn prisoner)
        {
            if (!this.treatmentValidator.HasLoadedMedicineAvailable(shuttleHost, prisoner))
            {
                return "CT_Shuttle_PrisonCell_TendWithoutMedicine".Translate().ToString();
            }

            return "CT_Shuttle_PrisonCell_TendTooltip".Translate().ToString();
        }

        private bool CanUseCapacity(Pawn pawn, PawnCapacityDef capacityDef)
        {
            return pawn != null &&
                pawn.health != null &&
                pawn.health.capacities != null &&
                capacityDef != null &&
                pawn.health.capacities.CapableOf(capacityDef);
        }

        private float GetFoodLevelPercent(Pawn pawn)
        {
            return pawn != null && pawn.needs != null && pawn.needs.food != null
                ? Clamp01(pawn.needs.food.CurLevelPercentage)
                : -1f;
        }

        private string GetHealthLabel(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.summaryHealth == null)
            {
                return "-";
            }

            float percent = Clamp01(pawn.health.summaryHealth.SummaryHealthPercent);
            return MathfRoundToInt(percent * 100f).ToString() + "%";
        }

        private string FormatPercentOrUnknown(float percent)
        {
            if (percent < 0f)
            {
                return "-";
            }

            return MathfRoundToInt(Clamp01(percent) * 100f).ToString() + "%";
        }

        private string GetFactionLabel(Faction faction)
        {
            return faction != null && !string.IsNullOrEmpty(faction.Name)
                ? faction.Name
                : "-";
        }

        private string GetInteractionModeLabel(Pawn pawn)
        {
            PrisonerInteractionModeDef mode = pawn != null && pawn.guest != null
                ? pawn.guest.ExclusiveInteractionMode
                : null;
            return mode != null && !string.IsNullOrEmpty(mode.label)
                ? mode.label.CapitalizeFirst()
                : "-";
        }

        private static int CompareHeldPrisoners(
            ShuttleHeldPrisonerReadModel left,
            ShuttleHeldPrisonerReadModel right)
        {
            return string.Compare(
                left != null ? left.Label : string.Empty,
                right != null ? right.Label : string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareCandidates(
            ShuttlePrisonerCandidateReadModel left,
            ShuttlePrisonerCandidateReadModel right)
        {
            bool leftCan = left != null && left.CanAdmit;
            bool rightCan = right != null && right.CanAdmit;
            if (leftCan != rightCan)
            {
                return leftCan ? -1 : 1;
            }

            return string.Compare(
                left != null ? left.Label : string.Empty,
                right != null ? right.Label : string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareCarriers(
            ShuttlePrisonerCarrierReadModel left,
            ShuttlePrisonerCarrierReadModel right)
        {
            bool leftCan = left != null && left.CanCarry;
            bool rightCan = right != null && right.CanCarry;
            if (leftCan != rightCan)
            {
                return leftCan ? -1 : 1;
            }

            return string.Compare(
                left != null ? left.Label : string.Empty,
                right != null ? right.Label : string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareFeeders(
            ShuttlePrisonerFeederReadModel left,
            ShuttlePrisonerFeederReadModel right)
        {
            bool leftCan = left != null && left.CanFeed;
            bool rightCan = right != null && right.CanFeed;
            if (leftCan != rightCan)
            {
                return leftCan ? -1 : 1;
            }

            return string.Compare(
                left != null ? left.Label : string.Empty,
                right != null ? right.Label : string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareDoctors(
            ShuttlePrisonerDoctorReadModel left,
            ShuttlePrisonerDoctorReadModel right)
        {
            bool leftCan = left != null && left.CanTend;
            bool rightCan = right != null && right.CanTend;
            if (leftCan != rightCan)
            {
                return leftCan ? -1 : 1;
            }

            return string.Compare(
                left != null ? left.Label : string.Empty,
                right != null ? right.Label : string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        private static int ComparePawnsByLabel(Pawn left, Pawn right)
        {
            return string.Compare(
                left != null ? left.LabelShortCap : string.Empty,
                right != null ? right.LabelShortCap : string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        private static float Clamp01(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return -1f;
            }

            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }

        private static int MathfRoundToInt(float value)
        {
            return UnityEngine.Mathf.RoundToInt(value);
        }
    }
}

using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ShuttleExternalOccupantReadPort : IShuttleExternalOccupantReadPort
    {
        private readonly ThingWithComps host;

        internal ShuttleExternalOccupantReadPort(ThingWithComps host)
        {
            this.host = host;
        }

        public IReadOnlyList<ShuttleExternalOccupantInfo> GetOccupants(
            ShuttleExternalOccupantQuery query)
        {
            List<ShuttleExternalOccupantInfo> occupants = new List<ShuttleExternalOccupantInfo>();
            if (this.host == null)
            {
                return occupants;
            }

            ShuttleExternalOccupantQuery effectiveQuery = query ?? new ShuttleExternalOccupantQuery();
            Dictionary<int, OccupantInfoBuilder> builders = new Dictionary<int, OccupantInfoBuilder>();
            this.AddLoadedCargoPawns(builders, effectiveQuery);
            this.AddHabitatPawns(builders, effectiveQuery);
            this.AddMedicalPatients(builders, effectiveQuery);
            this.AddPrisoners(builders, effectiveQuery);
            this.AddChargingMechs(builders, effectiveQuery);

            foreach (OccupantInfoBuilder builder in builders.Values)
            {
                ShuttleExternalOccupantInfo info = builder.Build();
                if (this.MatchesQuery(info, effectiveQuery))
                {
                    occupants.Add(info);
                }
            }

            return occupants;
        }

        private void AddLoadedCargoPawns(
            Dictionary<int, OccupantInfoBuilder> builders,
            ShuttleExternalOccupantQuery query)
        {
            if (!this.IncludesRole(query, ShuttleExternalOccupantRole.LoadedCargo))
            {
                return;
            }

            List<CompTransporter> transporters = this.ResolveTransporters();
            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                ThingOwner contents = transporter != null ? transporter.GetDirectlyHeldThings() : null;
                if (contents == null)
                {
                    continue;
                }

                for (int j = 0; j < contents.Count; j++)
                {
                    this.MergePawn(
                        builders,
                        contents[j] as Pawn,
                        ShuttleExternalOccupantRole.LoadedCargo,
                        "loaded_cargo");
                }
            }
        }

        private void AddHabitatPawns(
            Dictionary<int, OccupantInfoBuilder> builders,
            ShuttleExternalOccupantQuery query)
        {
            if (!this.IncludesRole(query, ShuttleExternalOccupantRole.Habitat))
            {
                return;
            }

            CompShuttleHabitatOccupancy habitat = this.host.TryGetComp<CompShuttleHabitatOccupancy>();
            if (habitat == null)
            {
                return;
            }

            this.MergePawnList(builders, habitat.GetSleepingOccupantsForReading(), ShuttleExternalOccupantRole.Habitat, "habitat.sleeping");
            this.MergePawnList(builders, habitat.GetDiningOccupantsForReading(), ShuttleExternalOccupantRole.Habitat, "habitat.dining");
            this.MergePawnList(builders, habitat.GetJoyOccupantsForReading(), ShuttleExternalOccupantRole.Habitat, "habitat.joy");
            this.MergePawnList(builders, habitat.OccupantsForReading, ShuttleExternalOccupantRole.Habitat, "habitat");
        }

        private void AddMedicalPatients(
            Dictionary<int, OccupantInfoBuilder> builders,
            ShuttleExternalOccupantQuery query)
        {
            if (!this.IncludesRole(query, ShuttleExternalOccupantRole.MedicalPatient))
            {
                return;
            }

            CompShuttleMedicalBayOccupancy medicalBay = this.host.TryGetComp<CompShuttleMedicalBayOccupancy>();
            this.MergePawnList(
                builders,
                medicalBay != null ? medicalBay.HeldPatients : null,
                ShuttleExternalOccupantRole.MedicalPatient,
                "medical.patient");
        }

        private void AddPrisoners(
            Dictionary<int, OccupantInfoBuilder> builders,
            ShuttleExternalOccupantQuery query)
        {
            if (!this.IncludesRole(query, ShuttleExternalOccupantRole.Prisoner))
            {
                return;
            }

            CompShuttlePrisonCellOccupancy prisonCell = this.host.TryGetComp<CompShuttlePrisonCellOccupancy>();
            this.MergePawnList(
                builders,
                prisonCell != null ? prisonCell.HeldPrisonersForReading : null,
                ShuttleExternalOccupantRole.Prisoner,
                "prisoner");
        }

        private void AddChargingMechs(
            Dictionary<int, OccupantInfoBuilder> builders,
            ShuttleExternalOccupantQuery query)
        {
            if (!this.IncludesRole(query, ShuttleExternalOccupantRole.MechCharger))
            {
                return;
            }

            CompShuttleMechChargerOccupancy mechCharger = this.host.TryGetComp<CompShuttleMechChargerOccupancy>();
            this.MergePawnList(
                builders,
                mechCharger != null ? mechCharger.HeldChargingMechs : null,
                ShuttleExternalOccupantRole.MechCharger,
                "mech.charging");
        }

        private void MergePawnList(
            Dictionary<int, OccupantInfoBuilder> builders,
            IEnumerable<Pawn> pawns,
            ShuttleExternalOccupantRole role,
            string source)
        {
            if (pawns == null)
            {
                return;
            }

            foreach (Pawn pawn in pawns)
            {
                this.MergePawn(builders, pawn, role, source);
            }
        }

        private void MergePawn(
            Dictionary<int, OccupantInfoBuilder> builders,
            Pawn pawn,
            ShuttleExternalOccupantRole role,
            string source)
        {
            if (pawn == null)
            {
                return;
            }

            int thingId = pawn.thingIDNumber;
            if (thingId < 0)
            {
                return;
            }

            OccupantInfoBuilder builder;
            if (!builders.TryGetValue(thingId, out builder))
            {
                builder = new OccupantInfoBuilder(pawn);
                builders.Add(thingId, builder);
            }

            builder.Merge(role, source);
        }

        private bool MatchesQuery(
            ShuttleExternalOccupantInfo info,
            ShuttleExternalOccupantQuery query)
        {
            if (info == null)
            {
                return false;
            }

            if (!this.IncludesRole(query, info.Role))
            {
                return false;
            }

            if (query != null && query.HumanlikeOnly && !info.IsHumanlike)
            {
                return false;
            }

            return query == null || !query.ServiceableOnly || info.CanReceiveExternalService;
        }

        private bool IncludesRole(
            ShuttleExternalOccupantQuery query,
            ShuttleExternalOccupantRole role)
        {
            ShuttleExternalOccupantRole mask = query != null
                ? query.RoleMask
                : ShuttleExternalOccupantRole.Any;
            if (mask == ShuttleExternalOccupantRole.None)
            {
                return false;
            }

            return (mask & role) != ShuttleExternalOccupantRole.None;
        }

        private List<CompTransporter> ResolveTransporters()
        {
            List<CompTransporter> transporters = new List<CompTransporter>();
            if (this.host == null)
            {
                return transporters;
            }

            CompTransporter primary = this.host.TryGetComp<CompTransporter>();
            if (primary == null)
            {
                return transporters;
            }

            if (this.host.Map != null)
            {
                List<CompTransporter> group = primary.TransportersInGroup(this.host.Map);
                if (group != null)
                {
                    return group;
                }
            }

            transporters.Add(primary);
            return transporters;
        }

        private sealed class OccupantInfoBuilder
        {
            private readonly Pawn pawn;
            private readonly List<string> sources = new List<string>();
            private ShuttleExternalOccupantRole role;

            internal OccupantInfoBuilder(Pawn pawn)
            {
                this.pawn = pawn;
            }

            internal void Merge(ShuttleExternalOccupantRole role, string source)
            {
                this.role |= role;
                if (!string.IsNullOrEmpty(source) && !this.sources.Contains(source))
                {
                    this.sources.Add(source);
                }
            }

            internal ShuttleExternalOccupantInfo Build()
            {
                bool isHumanlike = this.pawn.RaceProps != null && this.pawn.RaceProps.Humanlike;
                bool isMechanoid = this.pawn.RaceProps != null && this.pawn.RaceProps.IsMechanoid;
                bool isLoadedCargo = (this.role & ShuttleExternalOccupantRole.LoadedCargo) != 0;
                bool isHabitat = (this.role & ShuttleExternalOccupantRole.Habitat) != 0;
                bool isMedicalPatient = (this.role & ShuttleExternalOccupantRole.MedicalPatient) != 0;
                bool isPrisonCellPrisoner = (this.role & ShuttleExternalOccupantRole.Prisoner) != 0;
                bool isMechCharger = (this.role & ShuttleExternalOccupantRole.MechCharger) != 0;
                bool isServiceable =
                    isHumanlike &&
                    !isMechanoid &&
                    !this.pawn.Destroyed &&
                    !this.pawn.Dead;

                // TODO: Wire exact holder-transfer manifest state once the public service
                // contract needs transfer-aware external service gating.
                bool isInInternalTransfer = false;
                bool isInLaunchTransfer = false;
                bool canReceiveExternalService = isServiceable;

                return new ShuttleExternalOccupantInfo(
                    this.pawn,
                    this.role,
                    string.Join(";", this.sources.ToArray()),
                    this.pawn.thingIDNumber,
                    this.pawn.ThingID,
                    this.pawn.LabelShort,
                    this.pawn.def != null ? this.pawn.def.defName : null,
                    this.pawn.kindDef != null ? this.pawn.kindDef.defName : null,
                    this.pawn.Faction != null && this.pawn.Faction.def != null
                        ? this.pawn.Faction.def.defName
                        : null,
                    isHumanlike,
                    this.pawn.IsColonist,
                    isPrisonCellPrisoner || this.pawn.IsPrisonerOfColony || this.pawn.IsPrisoner,
                    this.pawn.IsSlaveOfColony || this.pawn.IsSlave,
                    isMechanoid,
                    this.pawn.Dead,
                    this.pawn.Downed,
                    isLoadedCargo,
                    isHabitat,
                    isMedicalPatient,
                    isPrisonCellPrisoner,
                    isMechCharger,
                    isServiceable,
                    isInInternalTransfer,
                    isInLaunchTransfer,
                    canReceiveExternalService);
            }
        }
    }
}

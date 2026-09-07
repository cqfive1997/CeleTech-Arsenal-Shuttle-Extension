using System;
using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.Runtime
{
    [Flags]
    public enum ShuttleExternalOccupantRole
    {
        None = 0,
        LoadedCargo = 1,
        Habitat = 2,
        MedicalPatient = 4,
        Prisoner = 8,
        MechCharger = 16,
        Any = LoadedCargo | Habitat | MedicalPatient | Prisoner | MechCharger
    }

    public sealed class ShuttleExternalOccupantInfo
    {
        private readonly Pawn unsafeLivePawn;

        public ShuttleExternalOccupantInfo(
            Pawn pawn,
            ShuttleExternalOccupantRole role,
            string source,
            int thingIdNumber,
            string thingId,
            string label,
            string raceDefName,
            string kindDefName,
            string factionDefName,
            bool isHumanlike,
            bool isColonist,
            bool isPrisoner,
            bool isSlave,
            bool isMechanoid,
            bool isDead,
            bool isDowned,
            bool isLoadedCargo,
            bool isHabitatOccupant,
            bool isMedicalPatient,
            bool isPrisonCellPrisoner,
            bool isMechChargerOccupant,
            bool isServiceable,
            bool isInInternalTransfer,
            bool isInLaunchTransfer,
            bool canReceiveExternalService)
        {
            this.unsafeLivePawn = pawn;
            this.Role = role;
            this.Source = source;
            this.ThingIdNumber = thingIdNumber;
            this.ThingId = thingId;
            this.Label = label;
            this.RaceDefName = raceDefName;
            this.KindDefName = kindDefName;
            this.FactionDefName = factionDefName;
            this.IsHumanlike = isHumanlike;
            this.IsColonist = isColonist;
            this.IsPrisoner = isPrisoner;
            this.IsSlave = isSlave;
            this.IsMechanoid = isMechanoid;
            this.IsDead = isDead;
            this.IsDowned = isDowned;
            this.IsLoadedCargo = isLoadedCargo;
            this.IsHabitatOccupant = isHabitatOccupant;
            this.IsMedicalPatient = isMedicalPatient;
            this.IsPrisonCellPrisoner = isPrisonCellPrisoner;
            this.IsMechChargerOccupant = isMechChargerOccupant;
            this.IsServiceable = isServiceable;
            this.IsInInternalTransfer = isInInternalTransfer;
            this.IsInLaunchTransfer = isInLaunchTransfer;
            this.CanReceiveExternalService = canReceiveExternalService;
        }

        /// <summary>
        /// Unsafe live Verse reference retained for source compatibility.
        /// Third-party systems must not move, destroy, unload, or re-parent this pawn through
        /// the occupant API. Stable integrations should use the snapshot fields on this DTO.
        /// </summary>
        [Obsolete("Unsafe live Verse reference; use snapshot fields instead.")]
        public Pawn Pawn
        {
            get { return this.unsafeLivePawn; }
        }

        /// <summary>
        /// Explicit unsafe live pawn reference. Prefer snapshot fields for stable use.
        /// </summary>
        public Pawn UnsafeLivePawn
        {
            get { return this.unsafeLivePawn; }
        }

        public ShuttleExternalOccupantRole Role { get; private set; }
        public string Source { get; private set; }
        public int ThingIdNumber { get; private set; }
        public string ThingId { get; private set; }
        public string Label { get; private set; }
        public string RaceDefName { get; private set; }
        public string KindDefName { get; private set; }
        public string FactionDefName { get; private set; }
        public bool IsHumanlike { get; private set; }
        public bool IsColonist { get; private set; }
        public bool IsPrisoner { get; private set; }
        public bool IsSlave { get; private set; }
        public bool IsMechanoid { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsDowned { get; private set; }
        public bool IsLoadedCargo { get; private set; }
        public bool IsHabitatOccupant { get; private set; }
        public bool IsMedicalPatient { get; private set; }
        public bool IsPrisonCellPrisoner { get; private set; }
        public bool IsMechChargerOccupant { get; private set; }
        public bool IsServiceable { get; private set; }
        public bool IsInInternalTransfer { get; private set; }
        public bool IsInLaunchTransfer { get; private set; }
        public bool CanReceiveExternalService { get; private set; }
    }

    public sealed class ShuttleExternalOccupantQuery
    {
        public ShuttleExternalOccupantQuery()
        {
            this.RoleMask = ShuttleExternalOccupantRole.Any;
        }

        public ShuttleExternalOccupantRole RoleMask { get; set; }
        public bool HumanlikeOnly { get; set; }
        public bool ServiceableOnly { get; set; }
    }

    public interface IShuttleExternalOccupantReadPort
    {
        IReadOnlyList<ShuttleExternalOccupantInfo> GetOccupants(ShuttleExternalOccupantQuery query);
    }
}

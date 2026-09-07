using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.MechCharging;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    /// <summary>
    /// Builds a read-only mech charger UI snapshot without exposing holder ownership.
    /// </summary>
    internal sealed class ShuttleMechChargerReadModelBuilder
    {
        internal ShuttleMechChargerReadModel Build(
            ShuttleProfile profile,
            ThingWithComps shuttleHost)
        {
            ShuttleMechChargerReadModel model = new ShuttleMechChargerReadModel();
            MechChargerProfile mechCharger = profile != null ? profile.MechCharger : null;
            if (mechCharger != null)
            {
                model.HasMechCharger = mechCharger.HasMechCharger;
                model.MechChargeSlots = mechCharger.HasMechCharger
                    ? mechCharger.MechChargeSlots
                    : 0;
            }

            model.MechChargerPoweredKnown = shuttleHost != null;
            model.MechChargerPowered = model.HasMechCharger &&
                model.MechChargerPoweredKnown &&
                MechChargerAdmissionValidator.IsMechChargerPowered(shuttleHost);

            CompShuttleMechChargerOccupancy occupancy = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleMechChargerOccupancy>()
                : null;
            if (occupancy == null)
            {
                model.ChargingMechs = new List<ShuttleMechChargingPawnReadModel>();
                return model;
            }

            model.ChargingMechCount = occupancy.ChargingMechCount;
            model.FreeChargingSlots = occupancy.FreeChargingSlots;
            model.HasChargingMechs = occupancy.HasChargingMechs;

            List<ShuttleMechChargingPawnReadModel> chargingMechs =
                new List<ShuttleMechChargingPawnReadModel>();
            IReadOnlyList<Pawn> heldMechs = occupancy.HeldChargingMechs;
            if (heldMechs != null)
            {
                for (int i = 0; i < heldMechs.Count; i++)
                {
                    Pawn mech = heldMechs[i];
                    if (mech != null && !mech.Destroyed)
                    {
                        chargingMechs.Add(this.BuildChargingMechModel(mech));
                    }
                }
            }

            model.ChargingMechs = chargingMechs;
            return model;
        }

        private ShuttleMechChargingPawnReadModel BuildChargingMechModel(Pawn mech)
        {
            ShuttleMechChargingPawnReadModel model = new ShuttleMechChargingPawnReadModel();
            model.PawnThingID = mech != null ? mech.thingIDNumber : -1;
            model.PawnLabel = mech != null ? mech.LabelShort : string.Empty;
            model.DisplayThing = mech;
            model.EnergyPct = this.GetMechEnergyPct(mech);
            return model;
        }

        private float GetMechEnergyPct(Pawn pawn)
        {
            return ShuttleMechChargeNeedUtility.GetChargeNeedPct(pawn);
        }
    }
}

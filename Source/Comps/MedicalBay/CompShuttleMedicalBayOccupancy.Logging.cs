using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttleMedicalBayOccupancy
    {
        private void LogPatientPreservedWithoutMap(ShuttleMedicalPatientRecord record, bool medicalBayUnavailable)
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            if (!ShuttleLogThrottle.Global.ShouldLog(
                this.GetMedicalBayLogKey(
                    "preserved-without-map",
                    (medicalBayUnavailable ? "medical-missing:" : "map-missing:") +
                        (record != null ? record.PawnThingID.ToString() : "null"))))
            {
                return;
            }

            Log.Warning(
                "[CeleTech Shuttle] Preserving Medical Bay patient because no map is available for safe eject. " +
                "medicalBayUnavailable=" + medicalBayUnavailable +
                " patient=" + (record != null ? record.PawnLabel : "null"));
        }

        private string GetMedicalBayLogKey(string category, string detail)
        {
            return "MedicalBay:" +
                (this.parent != null ? this.parent.thingIDNumber : -1) +
                ":" +
                (category ?? "unknown") +
                ":" +
                (detail ?? "null");
        }
    }
}

using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Medical
{
    internal sealed class MedicalBayProcedureRuntimeSystem
    {
        private readonly MedicalBayProcedureService procedureService =
            new MedicalBayProcedureService();

        internal void Tick(
            ThingWithComps shuttle,
            ShuttleRuntimeState runtimeState,
            int ticksGame)
        {
            if (shuttle == null || runtimeState == null || this.procedureService == null)
            {
                return;
            }

            MedicalBayProcedureRuntimeState procedureState = runtimeState.MedicalProcedures;
            if (procedureState == null)
            {
                return;
            }

            IReadOnlyList<MedicalBayProcedureRecord> records = procedureState.ActiveProcedures;
            if (records == null || records.Count == 0)
            {
                return;
            }

            for (int i = 0; i < records.Count; i++)
            {
                MedicalBayProcedureRecord record = records[i];
                if (record == null ||
                    !record.IsActive ||
                    record.Status != MedicalBayProcedureRecord.StatusInProgress)
                {
                    continue;
                }

                if (record.ProcedureType == MedicalBayProcedureRecord.TypeTend)
                {
                    this.TickTendProcedure(shuttle, procedureState, record);
                }
                else if (record.ProcedureType == MedicalBayProcedureRecord.TypeSurgery)
                {
                    this.TickSurgeryProcedure(shuttle, procedureState, record);
                }
            }
        }

        private void TickTendProcedure(
            ThingWithComps shuttle,
            MedicalBayProcedureRuntimeState procedureState,
            MedicalBayProcedureRecord record)
        {
            string reason;
            if (!this.procedureService.TryValidateTendProcedureParticipants(shuttle, record, out reason))
            {
                this.procedureService.TryFailProcedureAndExitDoctor(shuttle, record, reason);
                return;
            }

            record.AddWorkTicks(1);
            if (record.WorkTicksTotal > 0 && record.WorkTicksDone < record.WorkTicksTotal)
            {
                return;
            }

            if (!this.procedureService.TryCompleteTendProcedure(shuttle, record, out reason) &&
                Prefs.DevMode &&
                !string.IsNullOrEmpty(reason))
            {
                Log.Warning("[CeleTech Shuttle] Medical Bay procedure completion failed: " + reason);
            }
        }

        private void TickSurgeryProcedure(
            ThingWithComps shuttle,
            MedicalBayProcedureRuntimeState procedureState,
            MedicalBayProcedureRecord record)
        {
            string reason;
            if (!this.procedureService.TryValidateSurgeryProcedureParticipants(shuttle, record, out reason))
            {
                this.procedureService.TryFailProcedureAndExitDoctor(shuttle, record, reason);
                return;
            }

            record.AddWorkTicks(1);
            if (record.WorkTicksTotal > 0 && record.WorkTicksDone < record.WorkTicksTotal)
            {
                return;
            }

            if (!this.procedureService.TryCompleteSurgeryProcedure(shuttle, record, out reason) &&
                Prefs.DevMode &&
                !string.IsNullOrEmpty(reason))
            {
                Log.Warning("[CeleTech Shuttle] Medical Bay surgery procedure completion failed: " + reason);
            }
        }
    }
}

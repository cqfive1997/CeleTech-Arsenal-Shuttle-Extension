namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated
{
    internal sealed class RefrigeratedCargoCoolingStatus
    {
        internal RefrigeratedCargoCoolingStatus(
            string moduleInstanceID,
            string moduleDefName,
            bool coolingActive,
            string inactiveReason,
            float targetTemperatureC,
            float coldCargoMassCapacityKg,
            float storedMassKg,
            int stackCount)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.ModuleDefName = moduleDefName;
            this.CoolingActive = coolingActive;
            this.InactiveReason = inactiveReason;
            this.TargetTemperatureC = targetTemperatureC;
            this.ColdCargoMassCapacityKg = coldCargoMassCapacityKg;
            this.StoredMassKg = storedMassKg;
            this.StackCount = stackCount;
            this.HasColdCargo = stackCount > 0;
        }

        internal string ModuleInstanceID { get; private set; }
        internal string ModuleDefName { get; private set; }
        internal bool CoolingActive { get; private set; }
        internal string InactiveReason { get; private set; }
        internal float TargetTemperatureC { get; private set; }
        internal float ColdCargoMassCapacityKg { get; private set; }
        internal float StoredMassKg { get; private set; }
        internal int StackCount { get; private set; }
        internal bool HasColdCargo { get; private set; }
    }
}

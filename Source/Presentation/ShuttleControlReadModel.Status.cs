using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    public sealed class ShuttleAssemblyConstructionQueueItemReadModel
    {
        public string OrderID;
        public string TargetLabel;
        public string CostSummary;
        public int Position;
    }

    public sealed class ShuttleAssemblyConstructionReadModel
    {
        public bool HasActiveOrder;
        public string OrderID;
        public string Label;
        public string TargetLabel;
        public string StatusLabel;
        public float Progress01;
        public string MaterialSummary;
        public float MaterialProgress01;
        public bool AwaitingMaterials;
        public bool MaterialsReady;
        public int WorkDone;
        public int WorkTotal;
        public string CostSummary;
        public string LastFailureReason;
        public List<ShuttleAssemblyConstructionQueueItemReadModel> QueuedOrders =
            new List<ShuttleAssemblyConstructionQueueItemReadModel>();
    }

    public sealed class ShuttleControlPageAvailabilityReadModel
    {
        public bool Crew;
        public bool Loading;
        public bool Defense;
        public bool Medical;
        public bool PrisonCell;
        public bool Processing;
        public bool ExternalModules;
    }

    public sealed class ShuttleHullReadModel
    {
        public bool HasHull;
        public float CurrentHitPoints;
        public int MaxHitPoints;
        public float IntegrityPct;
        public int IntegrityPctRounded;
        public int ArmorModuleCount;
        public float ArmorProtection01;
        public int ArmorProtectionPctRounded;
        public float SharpDamageMultiplier = 1f;
        public float BluntDamageMultiplier = 1f;
        public float HeatDamageMultiplier = 1f;
        public float ExplosionDamageMultiplier = 1f;
        public float EmpDamageMultiplier = 1f;
        public float FlatDamageReduction;
        public string StatusKey;
        public string StatusLabel;
        public string WarningLabel;
        public bool NeedsRepair;
        public float MissingHitPoints;
        public string RepairCostSummary;
        public int RepairWorkTicks;
    }
}

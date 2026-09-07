using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    public sealed class ShuttleAssemblyConstructionOrder : IExposable
    {
        private string orderID;
        private ShuttleAssemblyConstructionKind kind;
        private ShuttleAssemblyConstructionStatus status = ShuttleAssemblyConstructionStatus.Working;
        private string segmentSlotID;
        private string segmentInstanceID;
        private string moduleSlotID;
        private string targetDefName;
        private string selectedStuffDefName;
        private string label;
        private string targetLabel;
        private int workDone;
        private int workTotal;
        private string costSummary;
        private List<ThingDefCountClass> requiredCostList = new List<ThingDefCountClass>();
        private string lastFailureReason;

        public ShuttleAssemblyConstructionOrder()
        {
        }

        public ShuttleAssemblyConstructionOrder(
            string orderID,
            ShuttleAssemblyConstructionKind kind,
            string segmentSlotID,
            string segmentInstanceID,
            string moduleSlotID,
            string targetDefName,
            string selectedStuffDefName,
            string label,
            string targetLabel,
            int workTotal,
            string costSummary,
            List<ThingDefCountClass> requiredCostList)
        {
            this.orderID = orderID;
            this.kind = kind;
            this.status = ShuttleAssemblyConstructionStatus.AwaitingMaterials;
            this.segmentSlotID = segmentSlotID;
            this.segmentInstanceID = segmentInstanceID;
            this.moduleSlotID = moduleSlotID;
            this.targetDefName = targetDefName;
            this.selectedStuffDefName = SanitizeStuffDefName(selectedStuffDefName);
            this.label = label;
            this.targetLabel = targetLabel;
            this.workTotal = Mathf.Max(0, workTotal);
            this.costSummary = costSummary;
            this.requiredCostList = CloneValidCostList(requiredCostList);
            if (this.requiredCostList.Count == 0)
            {
                this.status = ShuttleAssemblyConstructionStatus.Working;
            }
        }

        public string OrderID { get { return this.orderID; } }
        public ShuttleAssemblyConstructionKind Kind { get { return this.kind; } }
        public ShuttleAssemblyConstructionStatus Status { get { return this.status; } }
        public string SegmentSlotID { get { return this.segmentSlotID; } }
        public string SegmentInstanceID { get { return this.segmentInstanceID; } }
        public string ModuleSlotID { get { return this.moduleSlotID; } }
        public string TargetDefName { get { return this.targetDefName; } }
        public string SelectedStuffDefName { get { return this.selectedStuffDefName; } }
        public string Label { get { return this.label; } }
        public string TargetLabel { get { return this.targetLabel; } }
        public int WorkDone { get { return this.workDone; } }
        public int WorkTotal { get { return this.workTotal; } }
        public string CostSummary { get { return this.costSummary; } }
        public IReadOnlyList<ThingDefCountClass> RequiredCostList { get { return this.requiredCostList; } }
        public string LastFailureReason { get { return this.lastFailureReason; } }

        public float Progress01
        {
            get
            {
                return this.workTotal > 0 ? Mathf.Clamp01(this.workDone / (float)this.workTotal) : 1f;
            }
        }

        internal bool IsActive
        {
            get
            {
                return !string.IsNullOrEmpty(this.orderID) &&
                    (this.status == ShuttleAssemblyConstructionStatus.AwaitingMaterials ||
                        this.status == ShuttleAssemblyConstructionStatus.Working ||
                        this.status == ShuttleAssemblyConstructionStatus.Completing ||
                        this.status == ShuttleAssemblyConstructionStatus.Failed);
            }
        }

        internal void MarkAwaitingMaterials()
        {
            if (this.status == ShuttleAssemblyConstructionStatus.Failed)
            {
                return;
            }

            this.status = ShuttleAssemblyConstructionStatus.AwaitingMaterials;
            this.lastFailureReason = null;
        }

        internal void MarkWorking()
        {
            if (this.status == ShuttleAssemblyConstructionStatus.Completing)
            {
                return;
            }

            this.status = ShuttleAssemblyConstructionStatus.Working;
            this.lastFailureReason = null;
        }

        internal void AddWork(int ticks)
        {
            if (this.status != ShuttleAssemblyConstructionStatus.Working)
            {
                return;
            }

            this.workDone = Mathf.Clamp(this.workDone + Mathf.Max(0, ticks), 0, Mathf.Max(0, this.workTotal));
        }

        internal void SetProgress01(float progress01)
        {
            this.workDone = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Clamp01(progress01) * Mathf.Max(0, this.workTotal)),
                0,
                Mathf.Max(0, this.workTotal));
            if (this.status == ShuttleAssemblyConstructionStatus.Failed)
            {
                this.status = ShuttleAssemblyConstructionStatus.Working;
                this.lastFailureReason = null;
            }
        }

        internal void MarkCompleting()
        {
            this.status = ShuttleAssemblyConstructionStatus.Completing;
        }

        internal void MarkFailed(string reason)
        {
            this.status = ShuttleAssemblyConstructionStatus.Failed;
            this.lastFailureReason = reason;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.orderID, "orderID");
            Scribe_Values.Look(ref this.kind, "kind", ShuttleAssemblyConstructionKind.None);
            Scribe_Values.Look(ref this.status, "status", ShuttleAssemblyConstructionStatus.Working);
            Scribe_Values.Look(ref this.segmentSlotID, "segmentSlotID");
            Scribe_Values.Look(ref this.segmentInstanceID, "segmentInstanceID");
            Scribe_Values.Look(ref this.moduleSlotID, "moduleSlotID");
            Scribe_Values.Look(ref this.targetDefName, "targetDefName");
            Scribe_Values.Look(ref this.selectedStuffDefName, "selectedStuffDefName");
            Scribe_Values.Look(ref this.label, "label");
            Scribe_Values.Look(ref this.targetLabel, "targetLabel");
            Scribe_Values.Look(ref this.workDone, "workDone", 0);
            Scribe_Values.Look(ref this.workTotal, "workTotal", 0);
            Scribe_Values.Look(ref this.costSummary, "costSummary");
            Scribe_Collections.Look(ref this.requiredCostList, "requiredCostList", LookMode.Deep);
            Scribe_Values.Look(ref this.lastFailureReason, "lastFailureReason");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.SanitizeRequiredCostList();
                this.workDone = Mathf.Max(0, this.workDone);
                this.workTotal = Mathf.Max(0, this.workTotal);
                this.selectedStuffDefName = SanitizeStuffDefName(this.selectedStuffDefName);
                if (this.workDone > this.workTotal)
                {
                    this.workDone = this.workTotal;
                }

                if (this.status == ShuttleAssemblyConstructionStatus.AwaitingMaterials &&
                    this.requiredCostList.Count == 0)
                {
                    this.status = ShuttleAssemblyConstructionStatus.Working;
                }
            }
        }

        private static List<ThingDefCountClass> CloneValidCostList(IReadOnlyList<ThingDefCountClass> costList)
        {
            List<ThingDefCountClass> result = new List<ThingDefCountClass>();
            if (costList == null)
            {
                return result;
            }

            for (int i = 0; i < costList.Count; i++)
            {
                ThingDefCountClass cost = costList[i];
                if (cost == null || cost.thingDef == null || cost.count <= 0)
                {
                    continue;
                }

                result.Add(new ThingDefCountClass(cost.thingDef, cost.count));
            }

            return result;
        }

        private static string SanitizeStuffDefName(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private void SanitizeRequiredCostList()
        {
            if (this.requiredCostList == null)
            {
                this.requiredCostList = new List<ThingDefCountClass>();
                return;
            }

            for (int i = this.requiredCostList.Count - 1; i >= 0; i--)
            {
                ThingDefCountClass cost = this.requiredCostList[i];
                if (cost == null || cost.thingDef == null || cost.count <= 0)
                {
                    this.requiredCostList.RemoveAt(i);
                }
            }
        }
    }
}

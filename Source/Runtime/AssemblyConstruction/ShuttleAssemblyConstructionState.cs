using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    public sealed class ShuttleAssemblyConstructionState : IExposable, IThingHolder
    {
        private ShuttleAssemblyConstructionOrder activeOrder;
        private List<ShuttleAssemblyConstructionOrder> queuedOrders =
            new List<ShuttleAssemblyConstructionOrder>();
        private ThingOwner<Thing> stagedIngredients;
        private ThingOwnerHolderRoot stagedIngredientsRoot;
        private string lastFailureReason;
        private int nextOrderIndex = 1;

        public ShuttleAssemblyConstructionState()
        {
            this.stagedIngredients = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            this.EnsureHolderRoots();
        }

        public IThingHolder ParentHolder
        {
            get
            {
                return null;
            }
        }

        public ShuttleAssemblyConstructionOrder ActiveOrder
        {
            get
            {
                return this.activeOrder;
            }
        }

        public bool HasActiveOrder
        {
            get
            {
                return this.activeOrder != null && this.activeOrder.IsActive;
            }
        }

        public IReadOnlyList<ShuttleAssemblyConstructionOrder> QueuedOrders
        {
            get
            {
                this.EnsureInitialized();
                return this.queuedOrders;
            }
        }

        public int QueuedOrderCount
        {
            get
            {
                this.EnsureInitialized();
                return this.queuedOrders.Count;
            }
        }

        public string LastFailureReason
        {
            get
            {
                return this.lastFailureReason;
            }
        }

        internal ThingOwner<Thing> StagedIngredients
        {
            get
            {
                this.EnsureInitialized();
                return this.stagedIngredients;
            }
        }

        internal string AllocateOrderID()
        {
            int index = this.nextOrderIndex;
            this.nextOrderIndex++;
            return "assembly-construction-" + index;
        }

        internal bool EnqueueOrder(ShuttleAssemblyConstructionOrder order)
        {
            this.EnsureInitialized();
            if (this.HasActiveOrder)
            {
                this.queuedOrders.Add(order);
                return false;
            }

            this.activeOrder = order;
            this.lastFailureReason = null;
            return true;
        }

        internal ShuttleAssemblyConstructionOrder AdvanceToNextOrder()
        {
            this.EnsureInitialized();
            this.activeOrder = null;
            if (this.queuedOrders.Count > 0)
            {
                this.activeOrder = this.queuedOrders[0];
                this.queuedOrders.RemoveAt(0);
            }

            this.lastFailureReason = null;
            return this.activeOrder;
        }

        internal bool TryRemoveQueuedOrder(
            string orderID,
            out ShuttleAssemblyConstructionOrder removedOrder)
        {
            removedOrder = null;
            this.EnsureInitialized();
            if (string.IsNullOrEmpty(orderID))
            {
                return false;
            }

            for (int i = 0; i < this.queuedOrders.Count; i++)
            {
                ShuttleAssemblyConstructionOrder order = this.queuedOrders[i];
                if (order != null && order.OrderID == orderID)
                {
                    removedOrder = order;
                    this.queuedOrders.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        internal void RecordFailure(string reason)
        {
            this.lastFailureReason = reason;
            if (this.activeOrder != null)
            {
                this.activeOrder.MarkFailed(reason);
            }
        }

        public void EnsureInitialized()
        {
            if (this.stagedIngredients == null)
            {
                this.stagedIngredients = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            }

            if (this.queuedOrders == null)
            {
                this.queuedOrders = new List<ShuttleAssemblyConstructionOrder>();
            }

            this.EnsureHolderRoots();
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return null;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            this.EnsureHolderRoots();
            outChildren.Add(this.stagedIngredientsRoot);
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref this.activeOrder, "activeOrder");
            Scribe_Collections.Look(
                ref this.queuedOrders,
                "queuedOrders",
                LookMode.Deep);
            Scribe_Deep.Look(ref this.stagedIngredients, "stagedIngredients", new object[] { this });
            Scribe_Values.Look(ref this.lastFailureReason, "lastFailureReason");
            Scribe_Values.Look(ref this.nextOrderIndex, "nextOrderIndex", 1);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
                this.SanitizeQueueAfterLoad();
                if (this.nextOrderIndex < 1)
                {
                    this.nextOrderIndex = 1;
                }
            }
        }

        private void SanitizeQueueAfterLoad()
        {
            if (this.queuedOrders == null)
            {
                this.queuedOrders = new List<ShuttleAssemblyConstructionOrder>();
            }

            HashSet<string> seenOrderIDs = new HashSet<string>();
            if (this.activeOrder != null && this.activeOrder.IsActive)
            {
                if (!string.IsNullOrEmpty(this.activeOrder.OrderID))
                {
                    seenOrderIDs.Add(this.activeOrder.OrderID);
                }
            }
            else
            {
                this.activeOrder = null;
            }

            for (int i = 0; i < this.queuedOrders.Count;)
            {
                ShuttleAssemblyConstructionOrder order = this.queuedOrders[i];
                if (order == null ||
                    !order.IsActive ||
                    string.IsNullOrEmpty(order.OrderID) ||
                    !seenOrderIDs.Add(order.OrderID))
                {
                    this.queuedOrders.RemoveAt(i);
                    continue;
                }

                i++;
            }

            if (this.activeOrder == null && this.queuedOrders.Count > 0)
            {
                this.activeOrder = this.queuedOrders[0];
                this.queuedOrders.RemoveAt(0);
            }
        }

        private void EnsureHolderRoots()
        {
            if (this.stagedIngredientsRoot == null)
            {
                this.stagedIngredientsRoot = new ThingOwnerHolderRoot(
                    this,
                    delegate
                    {
                        this.EnsureInitialized();
                        return this.stagedIngredients;
                    },
                    "assembly construction staged ingredients");
            }
        }
    }
}

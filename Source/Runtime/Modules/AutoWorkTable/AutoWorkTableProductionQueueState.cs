using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    /// <summary>
    /// Owns durable queue topology and mutation rules. Active transaction identity stays in
    /// AutoWorkTableRuntimeState because it belongs to the staged-work transaction, not the list.
    /// </summary>
    internal sealed class AutoWorkTableProductionQueueState
    {
        private List<AutoWorkTableProductionOrderState> orders =
            new List<AutoWorkTableProductionOrderState>();
        private int nextOrderId = 1;
        private int revision = 1;
        private int lastObservedCargoRevision = int.MinValue;
        private bool initialized;

        internal IReadOnlyList<AutoWorkTableProductionOrderState> Orders
        {
            get
            {
                this.EnsureInitialized();
                return this.orders;
            }
        }

        internal int Count
        {
            get { return this.orders != null ? this.orders.Count : 0; }
        }

        internal int Revision { get { return this.revision; } }

        internal void EnsureInitialized()
        {
            if (this.initialized)
            {
                return;
            }

            if (this.orders == null)
            {
                this.orders = new List<AutoWorkTableProductionOrderState>();
            }

            HashSet<int> usedIds = new HashSet<int>();
            for (int i = this.orders.Count - 1; i >= 0; i--)
            {
                AutoWorkTableProductionOrderState order = this.orders[i];
                if (order == null)
                {
                    this.orders.RemoveAt(i);
                    continue;
                }

                int orderId = order.OrderId;
                if (orderId <= 0 || usedIds.Contains(orderId))
                {
                    int repairedId = this.nextOrderId > 0 ? this.nextOrderId : 1;
                    while (usedIds.Contains(repairedId))
                    {
                        repairedId = repairedId == int.MaxValue ? 1 : repairedId + 1;
                    }

                    order.RepairOrderId(repairedId);
                    orderId = repairedId;
                    this.nextOrderId = repairedId == int.MaxValue ? 1 : repairedId + 1;
                }

                usedIds.Add(orderId);
            }

            int maximumId = 0;
            for (int i = 0; i < this.orders.Count; i++)
            {
                AutoWorkTableProductionOrderState order = this.orders[i];
                if (order != null && order.OrderId > maximumId)
                {
                    maximumId = order.OrderId;
                }
            }

            if (this.nextOrderId <= maximumId)
            {
                this.nextOrderId = maximumId == int.MaxValue ? 1 : maximumId + 1;
            }

            this.initialized = true;
        }

        internal AutoWorkTableProductionOrderState AddUnchecked(
            string sourceBenchDefName,
            string recipeDefName)
        {
            this.EnsureInitialized();
            AutoWorkTableProductionOrderState order =
                new AutoWorkTableProductionOrderState(
                    this.AllocateOrderId(),
                    sourceBenchDefName,
                    recipeDefName);
            this.orders.Add(order);
            return order;
        }

        internal AutoWorkTableProductionOrderState GetOrder(int orderId)
        {
            this.EnsureInitialized();
            for (int i = 0; i < this.orders.Count; i++)
            {
                AutoWorkTableProductionOrderState order = this.orders[i];
                if (order != null && order.OrderId == orderId)
                {
                    return order;
                }
            }

            return null;
        }

        internal AutoWorkTableProductionOrderState GetFirstOrder()
        {
            this.EnsureInitialized();
            return this.orders.Count > 0 ? this.orders[0] : null;
        }

        internal void Clear()
        {
            this.EnsureInitialized();
            this.orders.Clear();
        }

        internal bool TryRemove(int orderId, int activeOrderId, out string failureReason)
        {
            failureReason = null;
            int index = this.FindIndex(orderId);
            if (index < 0)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_OrderMissing".Translate().ToString();
                return false;
            }

            if (activeOrderId == orderId)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_CannotChangeActiveOrder".Translate().ToString();
                return false;
            }

            this.orders.RemoveAt(index);
            this.AdvanceRevision();
            return true;
        }

        internal bool TryMove(int orderId, int direction, out string failureReason)
        {
            failureReason = null;
            int index = this.FindIndex(orderId);
            int targetIndex = index + (direction < 0 ? -1 : 1);
            if (index < 0)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_OrderMissing".Translate().ToString();
                return false;
            }

            if (targetIndex < 0 || targetIndex >= this.orders.Count)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_OrderCannotMove".Translate().ToString();
                return false;
            }

            AutoWorkTableProductionOrderState order = this.orders[index];
            this.orders[index] = this.orders[targetIndex];
            this.orders[targetIndex] = order;
            this.AdvanceRevision();
            return true;
        }

        internal bool ObserveCargoRevision(int cargoRevision)
        {
            if (this.lastObservedCargoRevision == cargoRevision)
            {
                return false;
            }

            this.lastObservedCargoRevision = cargoRevision;
            return true;
        }

        internal void AdvanceRevision()
        {
            this.revision = this.revision == int.MaxValue ? 1 : this.revision + 1;
        }

        internal void ResetTransientRevision()
        {
            this.revision = 1;
            this.lastObservedCargoRevision = int.MinValue;
        }

        internal void ExposeFlatData()
        {
            // Called by the parent ExposeData so existing flat Scribe labels stay compatible.
            Scribe_Collections.Look(ref this.orders, "productionOrders", LookMode.Deep);
            Scribe_Values.Look(ref this.nextOrderId, "nextOrderId", 1);
            if (Scribe.mode == LoadSaveMode.LoadingVars ||
                Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.initialized = false;
            }
        }

        private int AllocateOrderId()
        {
            if (this.nextOrderId <= 0)
            {
                this.nextOrderId = 1;
            }

            while (this.GetOrderWithoutEnsure(this.nextOrderId) != null)
            {
                this.nextOrderId = this.nextOrderId == int.MaxValue
                    ? 1
                    : this.nextOrderId + 1;
            }

            int result = this.nextOrderId;
            this.nextOrderId = this.nextOrderId == int.MaxValue
                ? 1
                : this.nextOrderId + 1;
            return result;
        }

        private AutoWorkTableProductionOrderState GetOrderWithoutEnsure(int orderId)
        {
            if (orderId <= 0 || this.orders == null)
            {
                return null;
            }

            for (int i = 0; i < this.orders.Count; i++)
            {
                AutoWorkTableProductionOrderState order = this.orders[i];
                if (order != null && order.OrderId == orderId)
                {
                    return order;
                }
            }

            return null;
        }

        private int FindIndex(int orderId)
        {
            this.EnsureInitialized();
            for (int i = 0; i < this.orders.Count; i++)
            {
                AutoWorkTableProductionOrderState order = this.orders[i];
                if (order != null && order.OrderId == orderId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}

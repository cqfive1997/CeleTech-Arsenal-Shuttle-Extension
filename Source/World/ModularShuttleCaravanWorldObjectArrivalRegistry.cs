using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.World
{
    public sealed class ModularShuttleCaravanWorldObjectArrivalRegistry : GameComponent
    {
        private List<Caravan> trackedCaravans = new List<Caravan>();

        public ModularShuttleCaravanWorldObjectArrivalRegistry(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.trackedCaravans, "trackedCaravans", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (this.trackedCaravans == null)
                {
                    this.trackedCaravans = new List<Caravan>();
                }

                this.PruneDestroyedCaravans();
            }
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager == null || Find.TickManager.TicksGame % 250 != 0)
            {
                return;
            }

            this.PruneDestroyedCaravans();
        }

        internal static void Register(Caravan caravan)
        {
            ModularShuttleCaravanWorldObjectArrivalRegistry registry = CurrentRegistry;
            if (registry == null || caravan == null)
            {
                return;
            }

            registry.PruneDestroyedCaravans();
            if (!registry.trackedCaravans.Contains(caravan))
            {
                registry.trackedCaravans.Add(caravan);
            }
        }

        internal static bool IsTracked(Caravan caravan)
        {
            ModularShuttleCaravanWorldObjectArrivalRegistry registry = CurrentRegistry;
            if (registry == null || caravan == null)
            {
                return false;
            }

            registry.PruneDestroyedCaravans();
            return registry.trackedCaravans.Contains(caravan);
        }

        internal static void Unregister(Caravan caravan)
        {
            ModularShuttleCaravanWorldObjectArrivalRegistry registry = CurrentRegistry;
            if (registry == null || caravan == null)
            {
                return;
            }

            registry.trackedCaravans.Remove(caravan);
        }

        private static ModularShuttleCaravanWorldObjectArrivalRegistry CurrentRegistry
        {
            get
            {
                return Current.Game != null
                    ? Current.Game.GetComponent<ModularShuttleCaravanWorldObjectArrivalRegistry>()
                    : null;
            }
        }

        private void PruneDestroyedCaravans()
        {
            if (this.trackedCaravans == null)
            {
                this.trackedCaravans = new List<Caravan>();
                return;
            }

            for (int i = this.trackedCaravans.Count - 1; i >= 0; i--)
            {
                Caravan caravan = this.trackedCaravans[i];
                if (caravan == null || caravan.Destroyed || !caravan.Spawned)
                {
                    this.trackedCaravans.RemoveAt(i);
                }
            }
        }
    }
}

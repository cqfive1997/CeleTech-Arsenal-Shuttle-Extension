using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    public sealed partial class ShuttleController
    {
        internal void Tick()
        {
            ShuttleControllerTickPipeline.Tick(this);
        }

        internal ShuttleProfile ReconcileProfileToHost()
        {
            return ShuttleControllerTickPipeline.ReconcileProfileToHost(this);
        }

        internal ShuttleProfile EnsureProfile()
        {
            return this.ReconcileProfileToHost();
        }

        private int GetTicksGameSafe()
        {
            return ShuttleTickUtility.TicksGameOrMinusOne();
        }
    }
}

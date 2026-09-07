using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels
{
    internal sealed class ShuttleControlReadModelCacheReader
    {
        private readonly IShuttleControlReadPort controlReadPort;
        private readonly IShuttleCachedControlReadPort cachedControlReadPort;
        private readonly IShuttleCargoReadPort cargoReadPort;
        private readonly IShuttleWeaponBayReadPort weaponBayReadPort;
        private readonly IShuttleAutoWorkTableReadPort autoWorkTableReadPort;

        internal ShuttleControlReadModelCacheReader(
            IShuttleControlReadPort controlReadPort,
            IShuttleCargoReadPort cargoReadPort,
            IShuttleWeaponBayReadPort weaponBayReadPort,
            IShuttleAutoWorkTableReadPort autoWorkTableReadPort)
        {
            this.controlReadPort = controlReadPort;
            this.cachedControlReadPort = controlReadPort as IShuttleCachedControlReadPort;
            this.cargoReadPort = cargoReadPort;
            this.weaponBayReadPort = weaponBayReadPort;
            this.autoWorkTableReadPort = autoWorkTableReadPort;
        }

        internal ShuttleControlReadModel BuildControlReadModelSafe()
        {
            return this.BuildControlReadModelSafe(null, false);
        }

        internal ShuttleControlReadModel BuildControlReadModelSafe(
            ShuttleCargoSnapshot cargoSnapshot,
            ShuttleControlReadDetail detail)
        {
            return this.BuildControlReadModelSafe(cargoSnapshot, true, detail);
        }

        private ShuttleControlReadModel BuildControlReadModelSafe(
            ShuttleCargoSnapshot cargoSnapshot,
            bool useCachedCargoSnapshot,
            ShuttleControlReadDetail detail = ShuttleControlReadDetail.All)
        {
            try
            {
                if (useCachedCargoSnapshot && this.cachedControlReadPort != null)
                {
                    return this.cachedControlReadPort.BuildControlReadModel(cargoSnapshot, detail);
                }

                return this.controlReadPort != null
                    ? this.controlReadPort.BuildControlReadModel()
                    : new ShuttleControlReadModel();
            }
            catch (Exception exception)
            {
                ShuttleReadModelFailureLogger.LogOnce("BuildControlReadModelSafe", exception);
                return new ShuttleControlReadModel();
            }
        }

        internal ShuttleCargoSnapshot BuildCargoSnapshotSafe()
        {
            try
            {
                return this.cargoReadPort != null
                    ? this.cargoReadPort.BuildCargoSnapshot()
                    : new ShuttleCargoSnapshot();
            }
            catch (Exception exception)
            {
                ShuttleReadModelFailureLogger.LogOnce("BuildCargoSnapshotSafe", exception);
                return new ShuttleCargoSnapshot();
            }
        }

        internal ShuttleWeaponBayReadModel BuildWeaponBayReadModelSafe()
        {
            try
            {
                return this.weaponBayReadPort != null
                    ? this.weaponBayReadPort.BuildWeaponBayReadModel()
                    : ShuttleWeaponBayReadModel.Empty;
            }
            catch (Exception exception)
            {
                ShuttleReadModelFailureLogger.LogOnce("BuildWeaponBayReadModelSafe", exception);
                return ShuttleWeaponBayReadModel.Empty;
            }
        }

        internal ShuttleAutoWorkTableReadModel BuildAutoWorkTableReadModelSafe()
        {
            try
            {
                return this.autoWorkTableReadPort != null
                    ? this.autoWorkTableReadPort.BuildAutoWorkTableReadModel()
                    : new ShuttleAutoWorkTableReadModel();
            }
            catch (Exception exception)
            {
                ShuttleReadModelFailureLogger.LogOnce("BuildAutoWorkTableReadModelSafe", exception);
                return new ShuttleAutoWorkTableReadModel();
            }
        }
    }
}

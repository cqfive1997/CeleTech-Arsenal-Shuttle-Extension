using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Diagnostics;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels
{
    internal sealed class ShuttleControlReadModelCache
    {
        private readonly ShuttleControlReadModelCacheReader reader;
        private readonly ShuttleControlReadModelRefreshPolicy refreshPolicy =
            new ShuttleControlReadModelRefreshPolicy();

        private ShuttleControlReadModel cachedControlModel;
        private ShuttleCargoSnapshot cachedCargoSnapshot;
        private ShuttleWeaponBayReadModel cachedWeaponBayModel;
        private ShuttleAutoWorkTableReadModel cachedAutoWorkTableModel;
        private int cachedControlModelTick = int.MinValue;
        private int cachedCargoSnapshotTick = int.MinValue;
        private int cachedCargoSnapshotRevision;
        private int cachedWeaponBayModelTick = int.MinValue;
        private int cachedAutoWorkTableModelTick = int.MinValue;
        private ShuttleControlReadDetail cachedControlDetail = ShuttleControlReadDetail.None;
        private bool UIModelCacheDirty = true;
        private bool cargoSnapshotDirty = true;
        private bool weaponBayModelDirty = true;
        private bool autoWorkTableModelDirty = true;

        internal ShuttleControlReadModelCache(
            IShuttleControlReadPort controlReadPort,
            IShuttleCargoReadPort cargoReadPort,
            IShuttleWeaponBayReadPort weaponBayReadPort,
            IShuttleAutoWorkTableReadPort autoWorkTableReadPort)
        {
            this.reader = new ShuttleControlReadModelCacheReader(
                controlReadPort,
                cargoReadPort,
                weaponBayReadPort,
                autoWorkTableReadPort);
        }

        internal int CargoSnapshotRevision
        {
            get { return this.cachedCargoSnapshotRevision; }
        }

        internal ShuttleControlReadModel GetControlModelForUI(ShuttleControlReadDetail detail)
        {
            int ticks = this.refreshPolicy.GetUITicks();
            this.RefreshCargoSnapshotForControlIfNeeded(ticks);
            bool detailChanged = this.cachedControlDetail != detail;
            int refreshTicks = detail == ShuttleControlReadDetail.None
                ? this.refreshPolicy.GetControlUICacheRefreshTicks()
                : this.refreshPolicy.GetHeavyUICacheRefreshTicks();
            if (this.cachedControlModel == null ||
                (this.refreshPolicy.CanRefreshLightUINow(this.cachedControlModel == null) &&
                    this.refreshPolicy.ShouldRefresh(
                        this.cachedControlModelTick,
                        refreshTicks,
                        this.UIModelCacheDirty || detailChanged)))
            {
                using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.ControlModelSourceRead))
                {
                    this.cachedControlModel = this.reader.BuildControlReadModelSafe(
                        this.cachedCargoSnapshot,
                        detail);
                }

                this.cachedControlModelTick = ticks;
                this.cachedControlDetail = detail;
                this.UIModelCacheDirty = false;
            }

            return this.cachedControlModel ?? new ShuttleControlReadModel();
        }

        internal ShuttleControlReadModel RefreshControlModelForPageMenu()
        {
            int ticks = this.refreshPolicy.GetUITicks();
            this.RefreshCargoSnapshotForControlIfNeeded(ticks);
            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.ControlModelSourceRead))
            {
                this.cachedControlModel = this.reader.BuildControlReadModelSafe(
                    this.cachedCargoSnapshot,
                    this.cachedControlDetail);
            }

            this.cachedControlModelTick = ticks;
            this.UIModelCacheDirty = false;
            return this.cachedControlModel ?? new ShuttleControlReadModel();
        }

        internal ShuttleCargoSnapshot GetCargoSnapshotForUI(bool heavyAllowed)
        {
            int ticks = this.refreshPolicy.GetUITicks();
            if (this.cachedCargoSnapshot == null)
            {
                this.RefreshCargoSnapshotCache(ticks);
                return this.cachedCargoSnapshot;
            }

            if (this.cargoSnapshotDirty)
            {
                if (this.refreshPolicy.CanRefreshHeavyUINow(this.cachedCargoSnapshot == null))
                {
                    this.RefreshCargoSnapshotCache(ticks);
                }

                return this.cachedCargoSnapshot ?? new ShuttleCargoSnapshot();
            }

            if (!heavyAllowed)
            {
                return this.cachedCargoSnapshot;
            }

            if (!this.refreshPolicy.CanRefreshHeavyUINow(this.cachedCargoSnapshot == null))
            {
                return this.cachedCargoSnapshot;
            }

            if (this.refreshPolicy.ShouldRefresh(
                this.cachedCargoSnapshotTick,
                this.refreshPolicy.GetHeavyUICacheRefreshTicks(),
                false))
            {
                this.RefreshCargoSnapshotCache(ticks);
            }

            return this.cachedCargoSnapshot ?? new ShuttleCargoSnapshot();
        }

        internal ShuttleCargoSnapshot GetCargoSnapshotForCargoPageUI()
        {
            return this.GetCargoSnapshotForUI(true);
        }

        internal ShuttleWeaponBayReadModel GetWeaponBayModelForUI(bool heavyAllowed)
        {
            int ticks = this.refreshPolicy.GetUITicks();
            if (this.cachedWeaponBayModel == null)
            {
                if (!heavyAllowed && !this.refreshPolicy.CanRefreshLightUINow(true))
                {
                    return ShuttleWeaponBayReadModel.Empty;
                }

                this.RefreshWeaponBayModelCache(ticks);
                return this.cachedWeaponBayModel;
            }

            if (this.weaponBayModelDirty)
            {
                if (this.refreshPolicy.CanRefreshHeavyUINow(this.cachedWeaponBayModel == null))
                {
                    this.RefreshWeaponBayModelCache(ticks);
                }

                return this.cachedWeaponBayModel ?? ShuttleWeaponBayReadModel.Empty;
            }

            if (!heavyAllowed)
            {
                return this.cachedWeaponBayModel;
            }

            if (this.refreshPolicy.CanRefreshHeavyUINow(this.cachedWeaponBayModel == null) &&
                this.refreshPolicy.ShouldRefresh(
                    this.cachedWeaponBayModelTick,
                    this.refreshPolicy.GetHeavyUICacheRefreshTicks(),
                    false))
            {
                this.RefreshWeaponBayModelCache(ticks);
            }

            return this.cachedWeaponBayModel ?? ShuttleWeaponBayReadModel.Empty;
        }

        internal ShuttleAutoWorkTableReadModel GetAutoWorkTableModelForUI()
        {
            int ticks = this.refreshPolicy.GetUITicks();
            if (this.cachedAutoWorkTableModel == null ||
                (this.refreshPolicy.CanRefreshHeavyUINow(this.cachedAutoWorkTableModel == null) &&
                    this.refreshPolicy.ShouldRefresh(
                        this.cachedAutoWorkTableModelTick,
                        this.refreshPolicy.GetHeavyUICacheRefreshTicks(),
                        this.autoWorkTableModelDirty)))
            {
                using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.AutoWorkTableSourceRead))
                {
                    this.cachedAutoWorkTableModel =
                        this.reader.BuildAutoWorkTableReadModelSafe();
                }

                this.cachedAutoWorkTableModelTick = ticks;
                this.autoWorkTableModelDirty = false;
            }

            return this.cachedAutoWorkTableModel ?? new ShuttleAutoWorkTableReadModel();
        }

        internal void MarkDirty()
        {
            this.UIModelCacheDirty = true;
            this.cargoSnapshotDirty = true;
            this.weaponBayModelDirty = true;
            this.autoWorkTableModelDirty = true;
        }

        private void RefreshCargoSnapshotCache(int ticks)
        {
            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.CargoSnapshotSourceRead))
            {
                this.cachedCargoSnapshot = this.reader.BuildCargoSnapshotSafe();
            }

            this.cachedCargoSnapshotTick = ticks;
            this.cachedCargoSnapshotRevision++;
            this.cargoSnapshotDirty = false;
            this.UIModelCacheDirty = true;
        }

        private void RefreshCargoSnapshotForControlIfNeeded(int ticks)
        {
            bool cacheMissing = this.cachedCargoSnapshot == null;
            if (!this.refreshPolicy.CanRefreshHeavyUINow(cacheMissing))
            {
                return;
            }

            if (cacheMissing ||
                this.refreshPolicy.ShouldRefresh(
                    this.cachedCargoSnapshotTick,
                    this.refreshPolicy.GetHeavyUICacheRefreshTicks(),
                    this.cargoSnapshotDirty))
            {
                this.RefreshCargoSnapshotCache(ticks);
            }
        }

        private void RefreshWeaponBayModelCache(int ticks)
        {
            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.WeaponBaySourceRead))
            {
                this.cachedWeaponBayModel = this.reader.BuildWeaponBayReadModelSafe();
            }

            this.cachedWeaponBayModelTick = ticks;
            this.weaponBayModelDirty = false;
        }
    }
}

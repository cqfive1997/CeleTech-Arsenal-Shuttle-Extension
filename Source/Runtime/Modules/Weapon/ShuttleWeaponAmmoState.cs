using System;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal enum ShuttleWeaponReloadRequestKind
    {
        None,
        Manual,
        RequiredForFire,
        AutoTopOff
    }

    internal enum ShuttleWeaponReloadExecutorKind
    {
        None,
        AutomaticLoader,
        Pawn
    }

    /// <summary>
    /// Durable magazine and reload-ownership state for one shuttle weapon module. Automatic
    /// retry and power-admission cadence remain runtime-only helpers.
    /// </summary>
    internal sealed class ShuttleWeaponAmmoState : IExposable
    {
        private string moduleInstanceID;
        private string selectedAmmoDefName;
        private int loadedAmmoCount;
        private int magazineCapacity;
        private bool reloadInProgress;
        private int reloadWorkDone;
        private int reloadWorkTotal;
        private int reservedAmmoThingID = -1;
        private string lastFailureReason;
        private bool autoReloadEnabled;
        private bool logisticsAutoFeedEnabled;
        private bool reloadRequested;
        private bool manualReloadAllowed;
        private bool manualReloadJobActive;
        private int manualReloadReservationTick = -1;
        private int manualReloadPawnThingID = -1;
        private int manualReloadJobLoadID = -1;
        private string lastReloadBlockerReason;
        private string lastAutomaticReloadBlockerReason;
        private ShuttleWeaponReloadRequestKind reloadRequestKind = ShuttleWeaponReloadRequestKind.None;
        // Runtime-only throttle for automatic reload probes. It prevents no-ammo/top-off
        // requests from querying cargo every weapon tick.
        private int nextAutomaticReloadCheckTick = -1;
        private ShuttleWeaponReloadRequestKind activeReloadRequestKind = ShuttleWeaponReloadRequestKind.None;
        private ShuttleWeaponReloadExecutorKind reloadExecutorKind = ShuttleWeaponReloadExecutorKind.None;
        // Power admission is recalculated after load. These two fields only coordinate the
        // power-demand pass and module tick within the current game session.
        private int nextReloadPowerDemandCheckTick = -1;
        private int lastReloadPowerDemandAttemptTick = -1;

        internal string ModuleInstanceID
        {
            get { return this.moduleInstanceID; }
        }

        internal string SelectedAmmoDefName
        {
            get { return this.selectedAmmoDefName; }
        }

        internal int LoadedAmmoCount
        {
            get { return this.loadedAmmoCount; }
        }

        internal int MagazineCapacity
        {
            get { return this.magazineCapacity; }
        }

        internal bool ReloadInProgress
        {
            get { return this.reloadInProgress; }
        }

        internal int ReloadWorkDone
        {
            get { return this.reloadWorkDone; }
        }

        internal int ReloadWorkTotal
        {
            get { return this.reloadWorkTotal; }
        }

        internal int ReservedAmmoThingID
        {
            get { return this.reservedAmmoThingID; }
        }

        internal string LastFailureReason
        {
            get { return this.lastFailureReason; }
        }

        internal bool AutoReloadEnabled
        {
            get { return this.autoReloadEnabled; }
        }

        internal bool LogisticsAutoFeedEnabled
        {
            get { return this.logisticsAutoFeedEnabled; }
        }

        internal bool ReloadRequested
        {
            get { return this.reloadRequested; }
        }

        internal ShuttleWeaponReloadRequestKind ReloadRequestKind
        {
            get
            {
                return this.reloadRequested
                    ? this.reloadRequestKind
                    : ShuttleWeaponReloadRequestKind.None;
            }
        }

        internal ShuttleWeaponReloadRequestKind ActiveReloadRequestKind
        {
            get
            {
                return this.reloadInProgress || this.manualReloadJobActive
                    ? this.activeReloadRequestKind
                    : ShuttleWeaponReloadRequestKind.None;
            }
        }

        internal bool ManualReloadAllowed
        {
            get { return this.manualReloadAllowed; }
        }

        internal bool ManualReloadJobActive
        {
            get { return this.manualReloadJobActive; }
        }

        internal int ManualReloadReservationTick
        {
            get { return this.manualReloadReservationTick; }
        }

        internal int ManualReloadPawnThingID
        {
            get { return this.manualReloadPawnThingID; }
        }

        internal int ManualReloadJobLoadID
        {
            get { return this.manualReloadJobLoadID; }
        }

        internal ShuttleWeaponReloadExecutorKind ReloadExecutorKind
        {
            get { return this.reloadExecutorKind; }
        }

        internal string LastReloadBlockerReason
        {
            get { return this.lastReloadBlockerReason; }
        }

        internal string LastAutomaticReloadBlockerReason
        {
            get { return this.lastAutomaticReloadBlockerReason; }
        }

        internal float ReloadProgress01
        {
            get
            {
                return this.reloadWorkTotal > 0
                    ? Math.Min(1f, Math.Max(0f, (float)this.reloadWorkDone / this.reloadWorkTotal))
                    : this.reloadInProgress ? 1f : 0f;
            }
        }

        internal void Configure(
            string moduleInstanceID,
            string defaultAmmoDefName,
            int magazineCapacity,
            bool autoReloadEnabledByDefault,
            bool logisticsAutoFeedByDefault,
            bool manualReloadAllowedByDefault)
        {
            this.moduleInstanceID = moduleInstanceID;
            this.magazineCapacity = Math.Max(0, magazineCapacity);

            if (string.IsNullOrEmpty(this.selectedAmmoDefName))
            {
                this.selectedAmmoDefName = defaultAmmoDefName;
                this.autoReloadEnabled = autoReloadEnabledByDefault;
                this.logisticsAutoFeedEnabled = logisticsAutoFeedByDefault;
                this.manualReloadAllowed = manualReloadAllowedByDefault;
            }

            this.SanitizeBasic();
        }

        internal void ResetSelectedAmmo(string ammoDefName)
        {
            this.selectedAmmoDefName = ammoDefName;
            this.lastFailureReason = null;
            this.lastAutomaticReloadBlockerReason = null;
            this.SanitizeBasic();
        }

        internal void ChangeSelectedAmmoAndClearMagazine(string ammoDefName)
        {
            // Ammo type changes discard the currently loaded magazine and cancel active work;
            // the new ammo type must be reloaded through the normal manual/automatic paths.
            this.selectedAmmoDefName = ammoDefName;
            this.loadedAmmoCount = 0;
            this.CancelReload();
            this.ClearAutomaticReloadRetry();
            this.lastFailureReason = null;
            this.lastAutomaticReloadBlockerReason = null;
            this.SanitizeBasic();
        }

        internal void SetLoadedAmmoCount(int count)
        {
            this.loadedAmmoCount = Clamp(count, 0, this.magazineCapacity);
        }

        internal void AddLoadedAmmo(int count)
        {
            this.SetLoadedAmmoCount(this.loadedAmmoCount + Math.Max(0, count));
        }

        internal bool TryAddLoadedAmmoExact(int count)
        {
            if (count <= 0)
            {
                return false;
            }

            int availableCapacity = this.magazineCapacity - this.loadedAmmoCount;
            if (availableCapacity < count)
            {
                return false;
            }

            this.loadedAmmoCount += count;
            return true;
        }

        internal bool TryConsumeLoadedAmmo(int count)
        {
            if (count <= 0)
            {
                return true;
            }

            if (this.loadedAmmoCount < count)
            {
                return false;
            }

            this.loadedAmmoCount -= count;
            return true;
        }

        /// <summary>
        /// Relinquishes only the durable magazine values during a synchronous backend authority
        /// transfer. Player preferences and a queued (but not executing) reload request remain
        /// common shuttle state. An executing automatic reload or Pawn claim is never split
        /// between backends.
        /// </summary>
        internal bool TryRelinquishMagazineAuthorityForRuntimeOnly(
            string expectedAmmoDefName,
            int expectedLoadedCount,
            out string failureReason)
        {
            failureReason = null;
            if (this.reloadInProgress ||
                this.manualReloadJobActive ||
                this.reloadExecutorKind != ShuttleWeaponReloadExecutorKind.None)
            {
                failureReason = "reload-executor-active";
                return false;
            }

            if (this.selectedAmmoDefName != expectedAmmoDefName)
            {
                failureReason = "core-selected-ammo-changed";
                return false;
            }

            if (this.loadedAmmoCount != expectedLoadedCount)
            {
                failureReason = "core-loaded-count-changed";
                return false;
            }

            this.selectedAmmoDefName = null;
            this.loadedAmmoCount = 0;
            this.activeReloadRequestKind = ShuttleWeaponReloadRequestKind.None;
            this.ClearAutomaticReloadRetry();
            this.ClearReloadPowerAdmission();
            return true;
        }

        internal bool TryRestoreRelinquishedMagazineForRuntimeOnly(
            string ammoDefName,
            int loadedCount)
        {
            if (!string.IsNullOrEmpty(this.selectedAmmoDefName) ||
                this.loadedAmmoCount != 0 ||
                this.reloadInProgress ||
                this.manualReloadJobActive ||
                this.reloadExecutorKind != ShuttleWeaponReloadExecutorKind.None ||
                string.IsNullOrEmpty(ammoDefName) ||
                loadedCount < 0 ||
                loadedCount > this.magazineCapacity)
            {
                return false;
            }

            this.selectedAmmoDefName = ammoDefName;
            this.loadedAmmoCount = loadedCount;
            return true;
        }

        internal bool TryStartAutomaticReload(
            int workTotal,
            ShuttleWeaponReloadRequestKind requestKind)
        {
            if (this.manualReloadJobActive ||
                this.reloadExecutorKind == ShuttleWeaponReloadExecutorKind.Pawn)
            {
                return false;
            }

            this.reloadInProgress = true;
            this.reloadExecutorKind = ShuttleWeaponReloadExecutorKind.AutomaticLoader;
            this.reloadWorkDone = 0;
            this.reloadWorkTotal = Math.Max(0, workTotal);
            this.reservedAmmoThingID = -1;
            this.lastFailureReason = null;
            this.lastReloadBlockerReason = null;
            this.activeReloadRequestKind = requestKind != ShuttleWeaponReloadRequestKind.None
                ? requestKind
                : this.reloadRequestKind;
            if (this.activeReloadRequestKind == ShuttleWeaponReloadRequestKind.None)
            {
                this.activeReloadRequestKind = ShuttleWeaponReloadRequestKind.RequiredForFire;
            }

            this.ClearAutomaticReloadRetry();
            this.ClearReloadPowerAdmission();
            return true;
        }

        internal void AddReloadWork(int ticks)
        {
            if (!this.reloadInProgress)
            {
                return;
            }

            this.reloadWorkDone = Clamp(
                this.reloadWorkDone + Math.Max(0, ticks),
                0,
                Math.Max(0, this.reloadWorkTotal));
        }

        internal void FinishReload()
        {
            this.reloadInProgress = false;
            this.reloadWorkDone = 0;
            this.reloadWorkTotal = 0;
            this.reservedAmmoThingID = -1;
            this.reloadRequested = false;
            this.reloadRequestKind = ShuttleWeaponReloadRequestKind.None;
            this.activeReloadRequestKind = ShuttleWeaponReloadRequestKind.None;
            this.reloadExecutorKind = ShuttleWeaponReloadExecutorKind.None;
            this.manualReloadJobActive = false;
            this.manualReloadReservationTick = -1;
            this.manualReloadPawnThingID = -1;
            this.manualReloadJobLoadID = -1;
            this.lastReloadBlockerReason = null;
            this.lastAutomaticReloadBlockerReason = null;
            this.ClearAutomaticReloadRetry();
            this.ClearReloadPowerAdmission();
        }

        internal void FailReload(string reason)
        {
            this.reloadInProgress = false;
            this.reloadWorkDone = 0;
            this.reloadWorkTotal = 0;
            this.reservedAmmoThingID = -1;
            this.lastFailureReason = reason;
            this.lastReloadBlockerReason = reason;
            if (this.activeReloadRequestKind == ShuttleWeaponReloadRequestKind.RequiredForFire ||
                this.activeReloadRequestKind == ShuttleWeaponReloadRequestKind.AutoTopOff)
            {
                this.lastAutomaticReloadBlockerReason = reason;
            }

            this.activeReloadRequestKind = ShuttleWeaponReloadRequestKind.None;
            this.reloadExecutorKind = ShuttleWeaponReloadExecutorKind.None;
            this.manualReloadJobActive = false;
            this.manualReloadReservationTick = -1;
            this.manualReloadPawnThingID = -1;
            this.manualReloadJobLoadID = -1;
            this.ClearReloadPowerAdmission();
        }

        internal void CancelReload()
        {
            this.reloadInProgress = false;
            this.reloadWorkDone = 0;
            this.reloadWorkTotal = 0;
            this.reservedAmmoThingID = -1;
            this.reloadRequested = false;
            this.reloadRequestKind = ShuttleWeaponReloadRequestKind.None;
            this.activeReloadRequestKind = ShuttleWeaponReloadRequestKind.None;
            this.reloadExecutorKind = ShuttleWeaponReloadExecutorKind.None;
            this.manualReloadJobActive = false;
            this.manualReloadReservationTick = -1;
            this.manualReloadPawnThingID = -1;
            this.manualReloadJobLoadID = -1;
            this.lastReloadBlockerReason = null;
            this.lastAutomaticReloadBlockerReason = null;
            this.ClearAutomaticReloadRetry();
            this.ClearReloadPowerAdmission();
        }

        internal void SetAutoReloadEnabled(bool enabled)
        {
            this.autoReloadEnabled = enabled;
            this.ClearAutomaticReloadRetry();
            this.lastAutomaticReloadBlockerReason = null;
        }

        internal void SetLogisticsAutoFeedEnabled(bool enabled)
        {
            this.logisticsAutoFeedEnabled = enabled;
            this.ClearAutomaticReloadRetry();
            this.lastAutomaticReloadBlockerReason = null;
        }

        internal void RequestReload(ShuttleWeaponReloadRequestKind requestKind)
        {
            if (requestKind == ShuttleWeaponReloadRequestKind.None)
            {
                requestKind = ShuttleWeaponReloadRequestKind.Manual;
            }

            bool changedKind = !this.reloadRequested || this.reloadRequestKind != requestKind;
            this.reloadRequested = true;
            this.reloadRequestKind = requestKind;
            if (requestKind == ShuttleWeaponReloadRequestKind.Manual || changedKind)
            {
                this.lastReloadBlockerReason = null;
                this.lastAutomaticReloadBlockerReason = null;
                this.lastFailureReason = null;
                this.ClearAutomaticReloadRetry();
            }
        }

        internal void SetReloadRequestKindForRuntimeOnly(ShuttleWeaponReloadRequestKind requestKind)
        {
            if (!this.reloadRequested)
            {
                this.reloadRequestKind = ShuttleWeaponReloadRequestKind.None;
                return;
            }

            this.reloadRequestKind = requestKind != ShuttleWeaponReloadRequestKind.None
                ? requestKind
                : ShuttleWeaponReloadRequestKind.RequiredForFire;
        }

        internal void ClearReloadRequest()
        {
            this.reloadRequested = false;
            this.reloadRequestKind = ShuttleWeaponReloadRequestKind.None;
            this.reloadExecutorKind = ShuttleWeaponReloadExecutorKind.None;
            this.manualReloadJobActive = false;
            this.manualReloadReservationTick = -1;
            this.manualReloadPawnThingID = -1;
            this.manualReloadJobLoadID = -1;
            this.lastReloadBlockerReason = null;
            this.lastAutomaticReloadBlockerReason = null;
            this.ClearAutomaticReloadRetry();
        }

        internal void SetManualReloadAllowed(bool enabled)
        {
            this.manualReloadAllowed = enabled;
            if (enabled)
            {
                this.lastReloadBlockerReason = null;
            }
        }

        internal bool TryMarkManualReloadJobActive(
            int ticksGame,
            int pawnThingID,
            int jobLoadID)
        {
            if (this.reloadInProgress ||
                this.manualReloadJobActive ||
                this.reloadExecutorKind != ShuttleWeaponReloadExecutorKind.None ||
                pawnThingID < 0 ||
                jobLoadID < 0)
            {
                return false;
            }

            this.manualReloadJobActive = true;
            this.reloadExecutorKind = ShuttleWeaponReloadExecutorKind.Pawn;
            this.manualReloadReservationTick = ticksGame >= 0 ? ticksGame : -1;
            this.manualReloadPawnThingID = pawnThingID;
            this.manualReloadJobLoadID = jobLoadID;
            this.activeReloadRequestKind =
                this.reloadRequestKind != ShuttleWeaponReloadRequestKind.None
                    ? this.reloadRequestKind
                    : ShuttleWeaponReloadRequestKind.RequiredForFire;
            this.lastReloadBlockerReason = null;
            return true;
        }

        internal void ClearManualReloadJobActive()
        {
            this.manualReloadJobActive = false;
            if (this.reloadExecutorKind == ShuttleWeaponReloadExecutorKind.Pawn)
            {
                this.reloadExecutorKind = ShuttleWeaponReloadExecutorKind.None;
            }

            this.manualReloadReservationTick = -1;
            this.manualReloadPawnThingID = -1;
            this.manualReloadJobLoadID = -1;
            if (!this.reloadInProgress)
            {
                this.activeReloadRequestKind = ShuttleWeaponReloadRequestKind.None;
            }
        }

        internal bool IsManualReloadClaim(int pawnThingID, int jobLoadID)
        {
            return this.manualReloadJobActive &&
                this.reloadExecutorKind == ShuttleWeaponReloadExecutorKind.Pawn &&
                pawnThingID >= 0 &&
                jobLoadID >= 0 &&
                this.manualReloadPawnThingID == pawnThingID &&
                this.manualReloadJobLoadID == jobLoadID;
        }

        internal bool TryClearManualReloadJobActive(int pawnThingID, int jobLoadID)
        {
            if (!this.IsManualReloadClaim(pawnThingID, jobLoadID))
            {
                return false;
            }

            this.ClearManualReloadJobActive();
            return true;
        }

        internal bool CanAttemptReloadPowerDemand(int ticksGame)
        {
            return this.nextReloadPowerDemandCheckTick < 0 ||
                ticksGame < 0 ||
                ticksGame >= this.nextReloadPowerDemandCheckTick;
        }

        internal void MarkReloadPowerDemandAttempt(int ticksGame)
        {
            this.lastReloadPowerDemandAttemptTick = ticksGame;
        }

        internal bool WasReloadPowerDemandAttemptedAt(int ticksGame)
        {
            return ticksGame >= 0 && this.lastReloadPowerDemandAttemptTick == ticksGame;
        }

        internal void MarkReloadPowerBlocked(int ticksGame, int retryDelayTicks)
        {
            this.nextReloadPowerDemandCheckTick = ticksGame >= 0
                ? ticksGame + Math.Max(1, retryDelayTicks)
                : -1;
        }

        internal void ClearReloadPowerAdmission()
        {
            this.nextReloadPowerDemandCheckTick = -1;
            this.lastReloadPowerDemandAttemptTick = -1;
        }

        internal bool CanAttemptAutomaticReloadCheck(int ticksGame)
        {
            return this.nextAutomaticReloadCheckTick < 0 ||
                ticksGame < 0 ||
                ticksGame >= this.nextAutomaticReloadCheckTick;
        }

        internal void ScheduleAutomaticReloadRetry(int ticksGame, int delayTicks)
        {
            if (ticksGame < 0)
            {
                this.nextAutomaticReloadCheckTick = -1;
                return;
            }

            this.nextAutomaticReloadCheckTick = ticksGame + Math.Max(1, delayTicks);
        }

        internal void ClearAutomaticReloadRetry()
        {
            this.nextAutomaticReloadCheckTick = -1;
        }

        internal void SetLastAutomaticReloadBlockerReason(string reason)
        {
            this.lastAutomaticReloadBlockerReason = reason;
        }

        internal void ClearLastAutomaticReloadBlockerReason()
        {
            this.lastAutomaticReloadBlockerReason = null;
        }

        internal void SetLastReloadBlockerReason(string reason)
        {
            this.lastReloadBlockerReason = reason;
            if (!string.IsNullOrEmpty(reason))
            {
                this.lastFailureReason = reason;
            }
        }

        internal void SetLastFailureReason(string reason)
        {
            this.lastFailureReason = reason;
        }

        internal void SanitizeBasic()
        {
            if (this.reservedAmmoThingID < 0)
            {
                this.reservedAmmoThingID = -1;
            }

            this.magazineCapacity = Math.Max(0, this.magazineCapacity);
            this.loadedAmmoCount = Clamp(this.loadedAmmoCount, 0, this.magazineCapacity);
            this.reloadWorkTotal = Math.Max(0, this.reloadWorkTotal);
            this.reloadWorkDone = Clamp(this.reloadWorkDone, 0, this.reloadWorkTotal);
            if (this.manualReloadReservationTick < 0)
            {
                this.manualReloadReservationTick = -1;
            }
            if (!Enum.IsDefined(typeof(ShuttleWeaponReloadRequestKind), this.reloadRequestKind))
            {
                this.reloadRequestKind = ShuttleWeaponReloadRequestKind.None;
            }
            if (!Enum.IsDefined(typeof(ShuttleWeaponReloadRequestKind), this.activeReloadRequestKind))
            {
                this.activeReloadRequestKind = ShuttleWeaponReloadRequestKind.None;
            }
            if (!Enum.IsDefined(typeof(ShuttleWeaponReloadExecutorKind), this.reloadExecutorKind))
            {
                this.reloadExecutorKind = ShuttleWeaponReloadExecutorKind.None;
            }
            if (!this.reloadRequested)
            {
                this.reloadRequestKind = ShuttleWeaponReloadRequestKind.None;
            }
            if (this.nextAutomaticReloadCheckTick < 0)
            {
                this.nextAutomaticReloadCheckTick = -1;
            }
            if (!this.reloadInProgress)
            {
                this.reloadWorkDone = 0;
                this.reloadWorkTotal = 0;
                this.reservedAmmoThingID = -1;
                if (!this.manualReloadJobActive)
                {
                    this.activeReloadRequestKind = ShuttleWeaponReloadRequestKind.None;
                }
                if (this.reloadExecutorKind == ShuttleWeaponReloadExecutorKind.AutomaticLoader)
                {
                    this.reloadExecutorKind = ShuttleWeaponReloadExecutorKind.None;
                }
            }
            else if (this.reloadExecutorKind == ShuttleWeaponReloadExecutorKind.None)
            {
                // Compatibility for saves written before executor ownership was persisted.
                this.reloadExecutorKind = ShuttleWeaponReloadExecutorKind.AutomaticLoader;
            }
            if (this.manualReloadJobActive)
            {
                this.reloadExecutorKind = ShuttleWeaponReloadExecutorKind.Pawn;
            }
            else if (this.reloadExecutorKind == ShuttleWeaponReloadExecutorKind.Pawn)
            {
                this.reloadExecutorKind = ShuttleWeaponReloadExecutorKind.None;
            }

        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.moduleInstanceID, "moduleInstanceID");
            Scribe_Values.Look(ref this.selectedAmmoDefName, "selectedAmmoDefName");
            Scribe_Values.Look(ref this.loadedAmmoCount, "loadedAmmoCount", 0);
            Scribe_Values.Look(ref this.magazineCapacity, "magazineCapacity", 0);
            Scribe_Values.Look(ref this.reloadInProgress, "reloadInProgress", false);
            Scribe_Values.Look(ref this.reloadWorkDone, "reloadWorkDone", 0);
            Scribe_Values.Look(ref this.reloadWorkTotal, "reloadWorkTotal", 0);
            Scribe_Values.Look(ref this.reservedAmmoThingID, "reservedAmmoThingID", -1);
            Scribe_Values.Look(ref this.lastFailureReason, "lastFailureReason");
            Scribe_Values.Look(ref this.autoReloadEnabled, "autoReloadEnabled", false);
            Scribe_Values.Look(ref this.logisticsAutoFeedEnabled, "logisticsAutoFeedEnabled", false);
            Scribe_Values.Look(ref this.reloadRequested, "reloadRequested", false);
            Scribe_Values.Look(
                ref this.reloadRequestKind,
                "reloadRequestKind",
                ShuttleWeaponReloadRequestKind.None);
            Scribe_Values.Look(
                ref this.activeReloadRequestKind,
                "activeReloadRequestKind",
                ShuttleWeaponReloadRequestKind.None);
            Scribe_Values.Look(
                ref this.reloadExecutorKind,
                "reloadExecutorKind",
                ShuttleWeaponReloadExecutorKind.None);
            Scribe_Values.Look(ref this.manualReloadAllowed, "manualReloadAllowed", false);
            Scribe_Values.Look(ref this.manualReloadJobActive, "manualReloadJobActive", false);
            Scribe_Values.Look(ref this.manualReloadReservationTick, "manualReloadReservationTick", -1);
            Scribe_Values.Look(ref this.manualReloadPawnThingID, "manualReloadPawnThingID", -1);
            Scribe_Values.Look(ref this.manualReloadJobLoadID, "manualReloadJobLoadID", -1);
            Scribe_Values.Look(ref this.lastReloadBlockerReason, "lastReloadBlockerReason");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.SanitizeBasic();
                this.ClearReloadPowerAdmission();
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}

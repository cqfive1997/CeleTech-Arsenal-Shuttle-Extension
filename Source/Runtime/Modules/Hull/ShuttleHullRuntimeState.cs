using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull
{
    /// <summary>
    /// Durable whole-shuttle hull integrity state. Damage routing and armor math are owned by
    /// later runtime services; this bucket only stores current unified hull HP.
    /// </summary>
    public sealed class ShuttleHullRuntimeState : IExposable
    {
        private bool hasInitialized;
        private float currentHitPoints;
        private int lastMaxHitPoints;
        private int lastDamageTick;
        private bool breached;

        public bool HasInitialized
        {
            get
            {
                return this.hasInitialized;
            }
        }

        public float CurrentHitPoints
        {
            get
            {
                return this.currentHitPoints;
            }
        }

        public int LastMaxHitPoints
        {
            get
            {
                return this.lastMaxHitPoints;
            }
        }

        public int LastDamageTick
        {
            get
            {
                return this.lastDamageTick;
            }
        }

        public bool Breached
        {
            get
            {
                return this.breached;
            }
        }

        public float IntegrityPct(int maxHitPoints)
        {
            if (maxHitPoints <= 0)
            {
                return 0f;
            }

            return this.Clamp01(this.currentHitPoints / maxHitPoints);
        }

        public void ReconcileMaxHitPoints(int maxHitPoints)
        {
            if (maxHitPoints <= 0)
            {
                this.currentHitPoints = 0f;
                this.lastMaxHitPoints = 0;
                this.breached = true;
                this.hasInitialized = true;
                return;
            }

            if (!this.hasInitialized)
            {
                this.currentHitPoints = maxHitPoints;
                this.lastMaxHitPoints = maxHitPoints;
                this.breached = maxHitPoints <= 0;
                this.hasInitialized = true;
                return;
            }

            if (maxHitPoints > this.lastMaxHitPoints)
            {
                this.currentHitPoints += maxHitPoints - this.lastMaxHitPoints;
                this.lastMaxHitPoints = maxHitPoints;
                this.currentHitPoints = this.Clamp(this.currentHitPoints, 0f, maxHitPoints);
            }
            else if (maxHitPoints < this.lastMaxHitPoints)
            {
                this.currentHitPoints = this.Min(this.currentHitPoints, maxHitPoints);
                this.lastMaxHitPoints = maxHitPoints;
                this.currentHitPoints = this.Clamp(this.currentHitPoints, 0f, maxHitPoints);
            }
            else
            {
                this.currentHitPoints = this.Clamp(this.currentHitPoints, 0f, maxHitPoints);
            }

            this.breached = this.currentHitPoints <= 0f && maxHitPoints > 0;
        }

        public float ApplyDamage(float amount, int ticksGame)
        {
            if (!this.IsFiniteFloat(amount) || amount <= 0f)
            {
                return 0f;
            }

            float absorbed = this.Min(this.currentHitPoints, amount);
            if (absorbed < 0f)
            {
                absorbed = 0f;
            }

            this.currentHitPoints -= absorbed;
            if (this.currentHitPoints < 0f)
            {
                this.currentHitPoints = 0f;
            }

            this.lastDamageTick = ticksGame;
            if (this.currentHitPoints <= 0f)
            {
                this.breached = true;
            }

            return absorbed;
        }

        public void Repair(float amount)
        {
            if (!this.IsFiniteFloat(amount) || amount <= 0f)
            {
                return;
            }

            this.currentHitPoints += amount;
            this.currentHitPoints = this.Clamp(this.currentHitPoints, 0f, this.lastMaxHitPoints);
            if (this.currentHitPoints > 0f)
            {
                this.breached = false;
            }
        }

        public void FillToMax(int maxHitPoints)
        {
            this.ReconcileMaxHitPoints(maxHitPoints);
            this.currentHitPoints = maxHitPoints > 0 ? maxHitPoints : 0f;
            this.breached = maxHitPoints <= 0;
            this.hasInitialized = true;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.hasInitialized, "hasInitialized", false);
            Scribe_Values.Look(ref this.currentHitPoints, "currentHitPoints", 0f);
            Scribe_Values.Look(ref this.lastMaxHitPoints, "lastMaxHitPoints", 0);
            Scribe_Values.Look(ref this.lastDamageTick, "lastDamageTick", -1);
            Scribe_Values.Look(ref this.breached, "breached", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.currentHitPoints = this.SanitizeNonNegativeFinite(this.currentHitPoints);
                if (this.lastMaxHitPoints < 0)
                {
                    this.lastMaxHitPoints = 0;
                }

                this.currentHitPoints = this.Clamp(this.currentHitPoints, 0f, this.lastMaxHitPoints);
                this.breached = this.currentHitPoints <= 0f && this.lastMaxHitPoints > 0;
            }
        }

        private float SanitizeNonNegativeFinite(float value)
        {
            if (!this.IsFiniteFloat(value) || value < 0f)
            {
                return 0f;
            }

            return value;
        }

        private bool IsFiniteFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private float Clamp01(float value)
        {
            return this.Clamp(value, 0f, 1f);
        }

        private float Min(float left, float right)
        {
            return left < right ? left : right;
        }
    }
}

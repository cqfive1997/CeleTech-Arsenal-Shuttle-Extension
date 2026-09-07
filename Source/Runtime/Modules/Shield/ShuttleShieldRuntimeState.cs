using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield
{
    /// <summary>
    /// Durable player setting for one installed shield module.
    /// Volatile interceptor internals stay with the host comp, including HP and cooldown.
    /// </summary>
    internal sealed class ShuttleShieldRuntimeState : IShuttleModuleRuntimeState
    {
        // Sentinel for saves or newly installed modules that have not chosen a radius yet.
        internal const float MissingSelectedRadius = -1f;

        private float selectedRadius = MissingSelectedRadius;

        public float SelectedRadius
        {
            get
            {
                return this.selectedRadius;
            }
        }

        public void EnsureInitialized()
        {
            if (!this.IsFiniteFloat(this.selectedRadius))
            {
                this.selectedRadius = MissingSelectedRadius;
            }
        }

        internal void SetSelectedRadius(float value)
        {
            this.selectedRadius = value;
            this.EnsureInitialized();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.selectedRadius, "selectedRadius", MissingSelectedRadius);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
            }
        }

        private bool IsFiniteFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

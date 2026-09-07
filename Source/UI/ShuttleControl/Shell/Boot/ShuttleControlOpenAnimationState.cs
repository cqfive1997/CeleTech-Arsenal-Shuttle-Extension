using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Boot
{
    internal sealed class ShuttleControlOpenAnimationState
    {
        private bool started;

        internal bool Finished { get; private set; }

        internal ShuttleControlBootAnimationMode Mode { get; private set; }

        internal float StartRealTime { get; private set; }

        internal float Duration { get; private set; }

        internal bool InputLocked
        {
            get
            {
                return this.IsPlaying;
            }
        }

        internal float Elapsed
        {
            get
            {
                if (!this.started)
                {
                    return 0f;
                }

                return Mathf.Max(0f, Time.realtimeSinceStartup - this.StartRealTime);
            }
        }

        internal float Progress01
        {
            get
            {
                if (this.Duration <= 0f)
                {
                    return 1f;
                }

                return Mathf.Clamp01(this.Elapsed / this.Duration);
            }
        }

        internal bool IsPlaying
        {
            get
            {
                return this.started && !this.Finished;
            }
        }

        internal ShuttleControlOpenAnimationState()
        {
            this.Mode = ShuttleControlBootAnimationMode.NormalShort;
            this.Duration = ShuttleControlAnimationUtility.GetTotalDuration(this.Mode);
        }

        internal void EnsureStarted()
        {
            if (!this.started)
            {
                this.Restart(this.Mode);
            }
        }

        internal void Restart()
        {
            this.Restart(ShuttleControlBootAnimationMode.NormalShort);
        }

        internal void Restart(ShuttleControlBootAnimationMode mode)
        {
            this.Mode = mode;
            this.started = true;
            this.Finished = false;
            this.StartRealTime = Time.realtimeSinceStartup;
            this.Duration = ShuttleControlAnimationUtility.GetTotalDuration(mode);
        }

        internal void Update()
        {
            if (!this.Finished && this.started && this.Progress01 >= 1f)
            {
                this.Finished = true;
            }
        }

        internal void Finish()
        {
            this.Finished = true;
        }
    }
}

using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels
{
    internal sealed class ShuttleControlReadModelRefreshPolicy
    {
        private const int FallbackControlUICacheRefreshTicks = 15;
        private const int FallbackHeavyUICacheRefreshTicks = 60;

        internal bool ShouldRefresh(int lastTick, int interval, bool dirty)
        {
            int ticks = this.GetUITicks();
            return dirty || lastTick == int.MinValue || ticks < lastTick || ticks - lastTick >= interval;
        }

        internal bool CanRefreshLightUINow(bool cacheMissing)
        {
            Event current = Event.current;
            return current == null ||
                current.type == EventType.Repaint ||
                cacheMissing;
        }

        internal bool CanRefreshHeavyUINow(bool cacheMissing)
        {
            Event current = Event.current;
            return current == null ||
                current.type == EventType.Repaint ||
                cacheMissing;
        }

        internal int GetUITicks()
        {
            return ShuttleTickUtility.TicksGameOrZero();
        }

        internal int GetControlUICacheRefreshTicks()
        {
            ShuttleEffectiveSettings effective = CeleTechShuttleMod.EffectiveSettings;
            return effective != null ? effective.ControlRefreshTicks : FallbackControlUICacheRefreshTicks;
        }

        internal int GetHeavyUICacheRefreshTicks()
        {
            ShuttleEffectiveSettings effective = CeleTechShuttleMod.EffectiveSettings;
            return effective != null ? effective.HeavyRefreshTicks : FallbackHeavyUICacheRefreshTicks;
        }
    }
}

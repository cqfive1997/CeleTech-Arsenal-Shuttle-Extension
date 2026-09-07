using System;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings.Dialogs
{
    internal sealed class DefenseSettingDefinition
    {
        internal readonly ShuttleCombatTuningCategory Category;
        internal readonly string LabelKey;
        internal readonly string TooltipKey;
        internal readonly float Min;
        internal readonly float Max;
        internal readonly float Step;
        internal readonly Func<ShuttleCombatTuningSettings, float> Getter;
        internal readonly Action<ShuttleCombatTuningSettings, float> Setter;

        internal DefenseSettingDefinition(
            ShuttleCombatTuningCategory category,
            string labelKey,
            string tooltipKey,
            float min,
            float max,
            float step,
            Func<ShuttleCombatTuningSettings, float> getter,
            Action<ShuttleCombatTuningSettings, float> setter)
        {
            this.Category = category;
            this.LabelKey = labelKey;
            this.TooltipKey = tooltipKey;
            this.Min = min;
            this.Max = max;
            this.Step = step;
            this.Getter = getter;
            this.Setter = setter;
        }
    }
}

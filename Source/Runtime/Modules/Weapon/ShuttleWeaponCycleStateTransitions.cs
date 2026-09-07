namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Small shared mutations for host-level target, warmup and active-power cycle state.
    /// </summary>
    internal static class ShuttleWeaponCycleStateTransitions
    {
        internal static void DecrementActivePower(ShuttleWeaponRuntimeState state)
        {
            int activeTicks = state != null
                ? state.GetActivePowerTicksForRuntimeOnly()
                : 0;
            if (activeTicks > 0)
            {
                state.SetActivePowerTicksForRuntimeOnly(activeTicks - 1);
            }
        }

        internal static void ResetCurrentTargetAndWarmup(ShuttleWeaponRuntimeState state)
        {
            if (state == null)
            {
                return;
            }

            state.ResetCurrentTargetForRuntimeOnly();
            state.SetWarmupTicksForRuntimeOnly(0);
        }
    }
}

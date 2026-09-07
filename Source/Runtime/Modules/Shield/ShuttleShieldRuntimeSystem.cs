using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield
{
    /// <summary>
    /// Reconcile-only shield settings system. It creates and clamps the selected radius state,
    /// but does not perform projectile interception or host-comp synchronization.
    /// </summary>
    internal sealed class ShuttleShieldRuntimeSystem : ShuttleModuleRuntimeSystemBase
    {
        public static readonly ShuttleShieldRuntimeSystem Instance = new ShuttleShieldRuntimeSystem();

        internal const string ShieldRuntimeSystemKey = ShuttleRuntimeSystemKeyUtility.Shield;

        private ShuttleShieldRuntimeSystem()
        {
        }

        public override string RuntimeSystemKey
        {
            get
            {
                return ShieldRuntimeSystemKey;
            }
        }

        public override int TickInterval
        {
            get
            {
                // Radius normalization is reconcile/install work; there is no continuous shield tick here.
                return 0;
            }
        }

        public override bool AppliesTo(ShuttleModule module)
        {
            ShuttleShieldModuleDef shieldDef = module != null
                ? module.ModuleDef as ShuttleShieldModuleDef
                : null;
            return shieldDef != null && shieldDef.supportsRadiusControl;
        }

        public override bool AppliesTo(ShuttleLaunchModuleRecord moduleRecord)
        {
            ShuttleShieldModuleDef shieldDef = moduleRecord != null
                ? moduleRecord.ModuleDef as ShuttleShieldModuleDef
                : null;
            return shieldDef != null && shieldDef.supportsRadiusControl;
        }

        public override IShuttleModuleRuntimeState CreateState()
        {
            return new ShuttleShieldRuntimeState();
        }

        public override void Reconcile(ShuttleModuleRuntimeContext context)
        {
            this.NormalizeSelectedRadius(context);
        }

        public override void OnInstalled(ShuttleModuleRuntimeContext context)
        {
            this.NormalizeSelectedRadius(context);
        }

        private void NormalizeSelectedRadius(ShuttleModuleRuntimeContext context)
        {
            if (context == null)
            {
                return;
            }

            ShuttleShieldRuntimeState state = context.State as ShuttleShieldRuntimeState;
            ShuttleShieldModuleDef shieldDef = context.ModuleDef as ShuttleShieldModuleDef;
            ShuttleShieldRuntimeUtility.EnsureSelectedRadiusInitialized(state, shieldDef);
        }
    }

    internal static class ShuttleShieldRuntimeUtility
    {
        internal static void EnsureSelectedRadiusInitialized(
            ShuttleShieldRuntimeState state,
            ShuttleShieldModuleDef shieldDef)
        {
            if (state == null || shieldDef == null || !shieldDef.supportsRadiusControl)
            {
                return;
            }

            state.SetSelectedRadius(ClampRadiusOrDefault(state.SelectedRadius, shieldDef));
        }

        internal static float ClampRadiusOrDefault(float requestedRadius, ShuttleShieldModuleDef shieldDef)
        {
            if (shieldDef == null || !shieldDef.supportsRadiusControl)
            {
                return 0f;
            }

            float minRadius = SanitizeNonNegativeFinite(shieldDef.minRadius);
            float maxRadius = SanitizeNonNegativeFinite(shieldDef.maxRadius);
            if (maxRadius < minRadius)
            {
                maxRadius = minRadius;
            }

            float defaultRadius = SanitizeFinite(shieldDef.defaultRadius, minRadius);
            if (defaultRadius < minRadius)
            {
                defaultRadius = minRadius;
            }
            else if (defaultRadius > maxRadius)
            {
                defaultRadius = maxRadius;
            }

            if (!IsFiniteFloat(requestedRadius) ||
                requestedRadius == ShuttleShieldRuntimeState.MissingSelectedRadius)
            {
                // Missing or invalid runtime values fall back to Def-authored default radius.
                return defaultRadius;
            }

            if (requestedRadius < minRadius)
            {
                return minRadius;
            }

            if (requestedRadius > maxRadius)
            {
                return maxRadius;
            }

            return requestedRadius;
        }

        private static float SanitizeNonNegativeFinite(float value)
        {
            value = SanitizeFinite(value, 0f);
            return value < 0f ? 0f : value;
        }

        private static float SanitizeFinite(float value, float fallback)
        {
            return IsFiniteFloat(value) ? value : fallback;
        }

        private static bool IsFiniteFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

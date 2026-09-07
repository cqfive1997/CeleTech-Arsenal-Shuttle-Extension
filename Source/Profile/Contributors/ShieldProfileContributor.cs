using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Contributors
{
    /// <summary>
    /// Static shield profile contribution. Live shield HP, cooldown, recharge state, and
    /// vanilla interceptor comp telemetry remain runtime/backend-owned.
    /// </summary>
    public sealed class ShieldProfileContributor : IShuttleProfileContributor
    {
        private static readonly ShieldProfileContributor instance = new ShieldProfileContributor();

        private ShieldProfileContributor()
        {
        }

        public static ShieldProfileContributor Instance
        {
            get
            {
                return instance;
            }
        }

        public void Contribute(ShuttleProfileContributionContext context)
        {
            if (context == null || context.Contributions == null)
            {
                return;
            }

            ShuttleShieldModuleDef shieldDef = context.ModuleDef as ShuttleShieldModuleDef;
            if (shieldDef == null)
            {
                return;
            }

            IShuttleShieldProfileContributionSink shieldSink =
                context.Contributions as IShuttleShieldProfileContributionSink;
            if (shieldSink == null)
            {
                return;
            }

            ShuttleSurfaceShieldModuleDef surfaceShieldDef =
                shieldDef as ShuttleSurfaceShieldModuleDef;
            if (surfaceShieldDef != null)
            {
                shieldSink.AddSurfaceShieldModule(
                    surfaceShieldDef.maxHitPoints,
                    surfaceShieldDef.rechargeEnergyPerHitPointWd);
                return;
            }

            ShuttleVanillaInterceptorShieldModuleDef vanillaShieldDef =
                shieldDef as ShuttleVanillaInterceptorShieldModuleDef;
            if (vanillaShieldDef != null)
            {
                shieldSink.AddVanillaInterceptorShieldModule(vanillaShieldDef.shieldHitPoints);
                return;
            }

            // The common base shield def has no static backend capacity of its own.
            // Third-party shield backends should provide an explicit contributor.
        }
    }
}

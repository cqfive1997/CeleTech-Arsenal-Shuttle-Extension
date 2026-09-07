using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Resolves whether the weapon's parent segment has an enabled ammunition loader.
    /// It owns no reload request, progress, supply or magazine state.
    /// </summary>
    internal sealed class ShuttleWeaponAmmoLoaderAvailability
    {
        internal bool HasEnabledLoader(ShuttleModuleRuntimeContext context)
        {
            if (context == null ||
                context.Profile == null ||
                context.Profile.Layout == null ||
                context.Profile.Layout.Modules == null)
            {
                return false;
            }

            IReadOnlyList<ModuleLayoutEntry> modules = context.Profile.Layout.Modules;
            for (int i = 0; i < modules.Count; i++)
            {
                ModuleLayoutEntry module = modules[i];
                if (module == null ||
                    !module.IsEnabled ||
                    string.IsNullOrEmpty(module.ModuleDefName) ||
                    module.ParentSegmentInstanceID != context.ParentSegmentInstanceID)
                {
                    continue;
                }

                ShuttleModuleBaseDef moduleDef =
                    DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(module.ModuleDefName);
                if (moduleDef != null && moduleDef.ModuleType == ShuttleModuleType.AmmoLoader)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo
{
    internal sealed class ShuttleCargoBayConfigActionTarget
    {
        internal string BayKey;
        internal string Label;
        internal bool IsRefrigerated;
        internal bool AllowHumans;
        internal bool AllowAnimals;
        internal bool AllowMechs;
        internal int RegionIndex = -1;
        internal string ModuleInstanceID;
        internal bool AutoTransferEnabled;
        internal bool HasCustomAutoTransferFilter;
        internal ThingFilter ItemFilter;
        internal ThingFilter AutoTransferFilter;

        internal static ShuttleCargoBayConfigActionTarget FromBay(
            ShuttleCargoBayActionTarget bay)
        {
            ShuttleCargoBayConfigActionTarget target =
                new ShuttleCargoBayConfigActionTarget();
            if (bay == null)
            {
                target.ItemFilter = ShuttleCargoThingFilterUtility.CopyFilter(null);
                target.AutoTransferFilter = ShuttleCargoThingFilterUtility.CopyFilter(null);
                ForceAllPawnCategoriesAllowed(target);
                return target;
            }

            target.BayKey = bay.BayKey;
            target.Label = bay.Label;
            target.IsRefrigerated = bay.IsRefrigerated;
            target.AllowHumans = bay.AllowHumans;
            target.AllowAnimals = bay.AllowAnimals;
            target.AllowMechs = bay.AllowMechs;
            target.RegionIndex = bay.RegionIndex;
            target.ModuleInstanceID = bay.ModuleInstanceID;
            target.AutoTransferEnabled = bay.AutoTransferEnabled;
            target.HasCustomAutoTransferFilter = bay.HasCustomAutoTransferFilter;
            target.ItemFilter = ShuttleCargoThingFilterUtility.CopyFilter(bay.ItemFilter);
            target.AutoTransferFilter =
                ShuttleCargoThingFilterUtility.CopyFilter(bay.AutoTransferFilter);
            if (!target.IsRefrigerated)
            {
                ForceAllPawnCategoriesAllowed(target);
            }

            return target;
        }

        internal ShuttleCargoBayConfigActionTarget Copy()
        {
            ShuttleCargoBayConfigActionTarget copy =
                new ShuttleCargoBayConfigActionTarget();
            copy.BayKey = this.BayKey;
            copy.Label = this.Label;
            copy.IsRefrigerated = this.IsRefrigerated;
            copy.AllowHumans = this.AllowHumans;
            copy.AllowAnimals = this.AllowAnimals;
            copy.AllowMechs = this.AllowMechs;
            copy.RegionIndex = this.RegionIndex;
            copy.ModuleInstanceID = this.ModuleInstanceID;
            copy.AutoTransferEnabled = this.AutoTransferEnabled;
            copy.HasCustomAutoTransferFilter = this.HasCustomAutoTransferFilter;
            copy.ItemFilter = ShuttleCargoThingFilterUtility.CopyFilter(this.ItemFilter);
            copy.AutoTransferFilter =
                ShuttleCargoThingFilterUtility.CopyFilter(this.AutoTransferFilter);
            if (!copy.IsRefrigerated)
            {
                ForceAllPawnCategoriesAllowed(copy);
            }

            return copy;
        }

        private static void ForceAllPawnCategoriesAllowed(
            ShuttleCargoBayConfigActionTarget target)
        {
            if (target == null)
            {
                return;
            }

            target.AllowHumans = true;
            target.AllowAnimals = true;
            target.AllowMechs = true;
        }
    }
}

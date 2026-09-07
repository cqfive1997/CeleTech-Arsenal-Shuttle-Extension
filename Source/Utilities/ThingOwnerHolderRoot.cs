using System;
using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Utilities
{
    internal sealed class ThingOwnerHolderRoot : IThingHolder
    {
        private readonly IThingHolder parent;
        private readonly Func<ThingOwner> ownerGetter;
        private readonly string debugName;

        internal ThingOwnerHolderRoot(
            IThingHolder parent,
            Func<ThingOwner> ownerGetter,
            string debugName)
        {
            this.parent = parent;
            this.ownerGetter = ownerGetter;
            this.debugName = debugName;
        }

        public IThingHolder ParentHolder
        {
            get
            {
                return this.parent;
            }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return this.ownerGetter != null ? this.ownerGetter() : null;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwner owner = this.GetDirectlyHeldThings();
            if (owner != null)
            {
                ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, owner);
            }
        }

        public override string ToString()
        {
            return !string.IsNullOrEmpty(this.debugName)
                ? this.debugName
                : base.ToString();
        }
    }
}

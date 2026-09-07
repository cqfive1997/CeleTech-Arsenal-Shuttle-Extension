using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.UI.External;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Icons
{
    internal sealed class ShuttleControlIconRegistry
    {
        private readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        private int externalIconRevision = ExternalShuttleIconRegistry.Revision;

        internal Texture2D GetIcon(string key)
        {
            this.RefreshExternalIconCacheIfNeeded();
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            Texture2D texture;
            if (this.cache.TryGetValue(key, out texture))
            {
                return texture;
            }

            texture = this.LoadTexture(this.ResolvePath(key));
            this.cache[key] = texture;
            return texture;
        }

        private string ResolvePath(string key)
        {
            ShuttleControlIconDef iconDef =
                DefDatabase<ShuttleControlIconDef>.GetNamedSilentFail(key);
            if (iconDef != null)
            {
                return iconDef.texPath;
            }

            string registeredPath;
            return ExternalShuttleIconRegistry.TryResolveTexPath(key, out registeredPath)
                ? registeredPath
                : null;
        }

        private Texture2D LoadTexture(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                return ContentFinder<Texture2D>.Get(path, false);
            }
            catch
            {
                return null;
            }
        }

        private void RefreshExternalIconCacheIfNeeded()
        {
            int revision = ExternalShuttleIconRegistry.Revision;
            if (revision == this.externalIconRevision)
            {
                return;
            }

            this.cache.Clear();
            this.externalIconRevision = revision;
        }
    }
}

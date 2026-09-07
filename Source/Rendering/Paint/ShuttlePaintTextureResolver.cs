using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.Paint
{
    internal sealed class ShuttlePaintTextureResolver
    {
        private readonly Dictionary<string, Texture2D> textureCache =
            new Dictionary<string, Texture2D>();
        private readonly HashSet<string> attemptedTexturePaths =
            new HashSet<string>();

        internal bool TryResolve(out Texture2D paintSource, out Texture2D paintMask)
        {
            return this.TryResolve(Rot4.East, out paintSource, out paintMask);
        }

        internal bool TryResolve(
            Rot4 rotation,
            out Texture2D paintSource,
            out Texture2D paintMask)
        {
            paintSource = this.LoadTexture(
                ShuttlePaintVisualAssetSet.GetPaintSourceTexturePath(rotation),
                "paint source");
            paintMask = this.LoadTexture(
                ShuttlePaintVisualAssetSet.GetPaintMaskTexturePath(rotation),
                "RGB paint mask");
            return paintSource != null && paintMask != null;
        }

        private Texture2D LoadTexture(string texturePath, string label)
        {
            if (string.IsNullOrWhiteSpace(texturePath))
            {
                return null;
            }

            if (this.textureCache.TryGetValue(texturePath, out Texture2D cached))
            {
                return cached;
            }

            if (this.attemptedTexturePaths.Contains(texturePath))
            {
                return null;
            }

            this.attemptedTexturePaths.Add(texturePath);
            try
            {
                Texture2D texture = ContentFinder<Texture2D>.Get(texturePath, false);
                if (texture == null)
                {
                    this.WarnOnce(
                        "missing-texture|" + texturePath,
                        "Map paint overlay skipped because the " + label +
                        " texture is missing: " + texturePath);
                }
                else
                {
                    this.textureCache[texturePath] = texture;
                }

                return texture;
            }
            catch (Exception exception)
            {
                this.WarnOnce(
                    "texture-load-failed|" + texturePath,
                    "Map paint overlay skipped because the " + label +
                    " texture failed to load: " + texturePath +
                    ". reason=" + exception.Message);
                return null;
            }
        }

        private void WarnOnce(string key, string message)
        {
            if (Prefs.DevMode)
            {
                ShuttleLog.WarnOnce("ShuttlePaintOverlay", key, message);
            }
        }
    }
}

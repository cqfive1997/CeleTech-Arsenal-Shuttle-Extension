using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed class CompProperties_ModularShuttleLandingGearVisual : CompProperties
    {
        public string texturePath =
            "Things/Building/ModularShuttle/KunPeng/KunPengLandingGearLayer";
        public Vector2 drawSize = new Vector2(7f, 9f);
        public Vector3 drawOffset = Vector3.zero;
        public float drawAltitudeOffset = -0.05f;

        public CompProperties_ModularShuttleLandingGearVisual()
        {
            this.compClass = typeof(CompModularShuttleLandingGearVisual);
        }
    }

    public sealed class CompModularShuttleLandingGearVisual : ThingComp
    {
        private const float MinimumDrawSize = 0.05f;
        private static readonly string[] RotationSuffixes =
        {
            "_north",
            "_east",
            "_south",
            "_west"
        };

        private Graphic graphic;
        private string loadedTexturePath;
        private Vector2 loadedDrawSize;
        private int loadedRotationIndex = -1;
        private bool loggedMissingTexture;

        private CompProperties_ModularShuttleLandingGearVisual Props
        {
            get
            {
                return (CompProperties_ModularShuttleLandingGearVisual)this.props;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();

            if (this.parent == null || !this.parent.Spawned || this.Props == null)
            {
                return;
            }

            Vector2 size = this.Props.drawSize;
            if (size.x < MinimumDrawSize || size.y < MinimumDrawSize)
            {
                return;
            }

            Graphic gearGraphic = this.GetGraphic();
            if (gearGraphic == null)
            {
                return;
            }

            Vector3 drawPos = this.parent.DrawPos;
            if (this.parent.Graphic != null)
            {
                drawPos += this.parent.Graphic.DrawOffset(this.parent.Rotation);
            }
            else
            {
                drawPos += this.Props.drawOffset;
            }

            drawPos.y = this.parent.DrawPos.y + this.Props.drawAltitudeOffset;
            gearGraphic.Draw(drawPos, this.parent.Rotation, this.parent);
        }

        private Graphic GetGraphic()
        {
            string basePath = this.Props != null ? this.Props.texturePath : null;
            if (string.IsNullOrWhiteSpace(basePath))
            {
                return null;
            }

            string texturePath = basePath.Trim();
            Vector2 drawSize = this.Props.drawSize;
            int rotationIndex = this.parent.Rotation.AsInt;
            if (this.graphic != null &&
                this.loadedTexturePath == texturePath &&
                this.loadedDrawSize == drawSize &&
                this.loadedRotationIndex == rotationIndex)
            {
                return this.graphic;
            }

            string currentTexturePath = texturePath + GetRotationSuffix(this.parent.Rotation);
            if (!this.HasTexture(currentTexturePath))
            {
                this.LogMissingTextureOnce(currentTexturePath);
                this.loadedTexturePath = texturePath;
                this.loadedDrawSize = drawSize;
                this.loadedRotationIndex = rotationIndex;
                this.graphic = null;
                return null;
            }

            this.loadedTexturePath = texturePath;
            this.loadedDrawSize = drawSize;
            this.loadedRotationIndex = rotationIndex;
            this.graphic = GraphicDatabase.Get<Graphic_Multi>(
                texturePath,
                ShaderDatabase.Cutout,
                drawSize,
                Color.white);
            return this.graphic;
        }

        private void LogMissingTextureOnce(string texturePath)
        {
            if (this.loggedMissingTexture)
            {
                return;
            }

            this.loggedMissingTexture = true;
            Log.Warning("[CeleTech Shuttle] Missing shuttle landing gear texture: " +
                (texturePath ?? "<null>"));
        }

        private bool HasTexture(string texturePath)
        {
            return ContentFinder<Texture2D>.Get(texturePath, false) != null;
        }

        private static string GetRotationSuffix(Rot4 rotation)
        {
            int index = rotation.AsInt;
            return index >= 0 && index < RotationSuffixes.Length
                ? RotationSuffixes[index]
                : "_east";
        }
    }
}

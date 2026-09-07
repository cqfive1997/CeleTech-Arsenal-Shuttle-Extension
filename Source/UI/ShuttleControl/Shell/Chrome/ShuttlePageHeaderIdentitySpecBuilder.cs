using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Chrome
{
    internal sealed class ShuttlePageHeaderIdentitySpecBuilder
    {
        internal string GetTitle(ShuttleControlReadModel controlModel)
        {
            if (controlModel != null && !string.IsNullOrEmpty(controlModel.ShuttleLabel))
            {
                return controlModel.ShuttleLabel;
            }

            return ShuttleUIText.Tr("CT_Shuttle_UI_DefaultShuttleLabel");
        }

        internal Texture2D GetLogo(ShuttlePageDrawContext context)
        {
            Texture2D logo = this.GetIcon(context, "console_logo");
            return logo != null ? logo : this.GetIcon(context, "logo");
        }

        private Texture2D GetIcon(ShuttlePageDrawContext context, string key)
        {
            return context != null &&
                context.Services != null &&
                context.Services.Icons != null
                    ? context.Services.Icons.GetIcon(key)
                    : null;
        }
    }
}

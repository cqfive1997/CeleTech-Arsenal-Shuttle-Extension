using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    /// <summary>
    /// Draws magazine capacity and reload progress without changing ammo state.
    /// </summary>
    internal sealed class V3DefenseWeaponAmmoMeterDrawer
    {
        private readonly V3DefenseText text;
        private readonly V3DefenseWeaponCardText cardText;

        internal V3DefenseWeaponAmmoMeterDrawer(
            V3DefenseText text,
            V3DefenseWeaponCardText cardText)
        {
            this.text = text;
            this.cardText = cardText;
        }

        internal void Draw(
            Rect rect,
            V3DefenseWeaponEntryModel weapon,
            bool selected)
        {
            if (rect.width < 32f || rect.height < 18f)
            {
                return;
            }

            string tooltip = weapon != null
                ? this.cardText.GetReloadCommandTooltip(weapon)
                : string.Empty;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x, rect.y, rect.width, 16f),
                this.text.Tr("CT_Shuttle_WeaponAmmo_Ammo"),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.WithAlpha(
                    V3DefenseText.BlueColor,
                    selected ? 1f : 0.88f),
                tooltip,
                TextAnchor.MiddleLeft);
            if (weapon == null || !weapon.HasAmmoSystem)
            {
                ShuttleUILayout.DrawFittedSingleLineLabel(
                    new Rect(rect.x, rect.y + 20f, rect.width, 34f),
                    this.text.Tr("CT_Shuttle_WeaponAmmo_NoAmmoSystem"),
                    GameFont.Tiny,
                    GameFont.Tiny,
                    ShuttleUIStyle.MutedTextColor,
                    weapon != null ? weapon.Tooltip : string.Empty,
                    TextAnchor.MiddleCenter);
                return;
            }

            float countWidth = Mathf.Clamp(rect.width * 0.34f, 54f, 82f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(
                    rect.x,
                    rect.y + 17f,
                    Mathf.Max(0f, rect.width - countWidth - 6f),
                    17f),
                this.text.ValueOrDash(weapon.SelectedAmmoLabel),
                GameFont.Tiny,
                GameFont.Tiny,
                Color.white,
                tooltip,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(
                    rect.xMax - countWidth,
                    rect.y + 17f,
                    countWidth,
                    17f),
                ShuttleUIText.Tr(
                    "CT_Shuttle_Defense_AmmoMeterCountFormat",
                    weapon.LoadedAmmoCount,
                    weapon.MagazineCapacity),
                GameFont.Tiny,
                GameFont.Tiny,
                Color.white,
                tooltip,
                TextAnchor.MiddleRight);

            Rect magazineRect = new Rect(
                rect.x,
                rect.y + 38f,
                rect.width,
                6f);
            float magazineFraction = weapon.MagazineCapacity > 0
                ? Mathf.Clamp01(
                    (float)weapon.LoadedAmmoCount / weapon.MagazineCapacity)
                : 0f;
            ShuttleUILayout.DrawLinearMeter(
                magazineRect,
                magazineFraction,
                GetMagazineColor(magazineFraction));
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x, rect.y + 48f, rect.width, 16f),
                ShuttleUIText.Tr(
                    "CT_Shuttle_Defense_AmmoMeterStockFormat",
                    weapon.AmmoStockCount),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                tooltip,
                TextAnchor.MiddleLeft);

            if (weapon.ReloadInProgress)
            {
                Rect reloadRect = new Rect(
                    rect.x,
                    rect.yMax - 3f,
                    rect.width,
                    3f);
                ShuttleUILayout.DrawLinearMeter(
                    reloadRect,
                    Mathf.Clamp01(weapon.ReloadProgress01),
                    V3DefenseText.BlueColor);
            }

            this.text.AddTooltip(rect, tooltip);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private static Color GetMagazineColor(float fraction)
        {
            if (fraction <= 0.001f)
            {
                return V3DefenseText.RedColor;
            }

            if (fraction < 0.25f)
            {
                return V3DefenseText.YellowColor;
            }

            return V3DefenseText.BlueColor;
        }
    }
}

using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phinix.LegacyTalentTradeExtension.Client
{
    internal static class TalentPawnIcons
    {
        private static Texture2D person;
        internal static bool UsesPortrait(ThingDef def) => def?.race?.Humanlike == true;
        internal static HostRow Local(HostRow row, Pawn pawn)
        {
            if (!UsesPortrait(pawn?.def)) { row.Icon = pawn?.def?.uiIcon; return row; }
            var game = Current.Game;
            var map = pawn.Map;
            row.IconSource = () => ReferenceEquals(Current.Game, game) && !pawn.Destroyed && pawn.Map == map
                ? PortraitsCache.Get(pawn, new Vector2(64, 64), Rot4.South) : Person();
            return row;
        }
        internal static HostRow Remote(HostRow row, PawnSummary summary)
        {
            var def = summary == null ? null : DefDatabase<ThingDef>.GetNamedSilentFail(summary.RaceDefName ?? "Human");
            if (UsesPortrait(def) || def == null && summary != null && summary.PawnKind != "Animal" && summary.PawnKind != "Mechanoid")
                row.IconSource = Person;
            else row.Icon = def?.uiIcon;
            return row;
        }
        private static Texture Person()
        {
            if (person != null) return person;
            person = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = "TalentPersonPlaceholder", filterMode = FilterMode.Bilinear };
            var pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                float dx = x - 15.5f, headY = y - 22;
                bool head = dx * dx + headY * headY <= 30;
                bool body = y >= 4 && y <= 15 && dx * dx / 121 + (y - 5) * (y - 5) / 144f <= 1;
                pixels[y * 32 + x] = head || body ? new Color(.8f, .82f, .85f, 1) : Color.clear;
            }
            person.SetPixels(pixels);person.Apply(false, true);
            return person;
        }
        internal static void Stop() { if (person != null) UnityEngine.Object.Destroy(person); person = null; }
    }
}

using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AlteredCarbon
{
    [HotSwappable]
    [HarmonyPatch(typeof(SocialCardUtility), "DrawRelationsAndOpinions")]
    public static class SocialCardUtility_DrawRelationsAndOpinions_Patch
    {
        public static bool Prefix(Rect rect, Pawn selPawnForSocialInfo)
        {
            if (selPawnForSocialInfo.IsDummyPawn())
            {
                Rect label = new Rect(rect.x, rect.y + 50, rect.width, 70);
                Widgets.Label(label, "AC.RelationshipsUnavailable".Translate(selPawnForSocialInfo));
                return false;
            }
            return true;
        }
    }
}

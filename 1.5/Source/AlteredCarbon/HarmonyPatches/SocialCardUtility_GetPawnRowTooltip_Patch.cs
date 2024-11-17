using HarmonyLib;
using RimWorld;
using System.Text;
using UnityEngine;
using Verse;
using static RimWorld.SocialCardUtility;

namespace AlteredCarbon
{
    [HotSwappable]
    [HarmonyPatch(typeof(SocialCardUtility), "GetPawnRowTooltip")]
    public static class SocialCardUtility_GetPawnRowTooltip_Patch
    {
        public static bool Prefix(CachedSocialTabEntry entry, Pawn selPawnForSocialInfo, ref string __result)
        {
            if (entry.otherPawn.IsDummyPawn())
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(OpinionExplanation(entry, selPawnForSocialInfo, entry.otherPawn));
                stringBuilder.AppendLine();
                stringBuilder.Append(("SomeonesOpinionOfMe".Translate(entry.otherPawn.LabelShort) + ": ").Colorize(ColoredText.TipSectionTitleColor));
                stringBuilder.Append(entry.opinionOfMe.ToStringWithSign());
                __result = stringBuilder.ToString().TrimEndNewlines();
                return false;
            }
            return true;
        }

        public static string OpinionExplanation(CachedSocialTabEntry entry, Pawn pawn, Pawn other)
        {
            if (!other.RaceProps.Humanlike || pawn == other)
            {
                return "";
            }
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("OpinionOf".Translate(other.LabelShort).Colorize(ColoredText.TipSectionTitleColor) + ": " + OpinionOf(entry, pawn, other).ToStringWithSign());
            string pawnSituationLabel = SocialCardUtility.GetPawnSituationLabel(other, pawn);
            if (!pawnSituationLabel.NullOrEmpty())
            {
                stringBuilder.AppendLine(" (" + pawnSituationLabel + ")");
            }
            else
            {
                stringBuilder.AppendLine();
            }
            bool flag = false;
            if (pawn.Dead)
            {
                stringBuilder.AppendLine(" - " + "IAmDead".Translate());
                flag = true;
            }
            else
            {
                foreach (PawnRelationDef relation in entry.relations)
                {
                    stringBuilder.AppendLine(" - " + relation.GetGenderSpecificLabelCap(other) + ": " + relation.opinionOffset.ToStringWithSign());
                    flag = true;
                }
            }
            if (!flag)
            {
                stringBuilder.AppendLine(" - " + "NoneBrackets".Translate());
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public static int OpinionOf(CachedSocialTabEntry entry, Pawn pawn, Pawn other)
        {
            if (!other.RaceProps.Humanlike || pawn == other)
            {
                return 0;
            }
            if (pawn.Dead)
            {
                return 0;
            }
            int num = 0;
            foreach (PawnRelationDef relation in entry.relations)
            {
                num += relation.opinionOffset;
            }
            return Mathf.Clamp(num, -100, 100);
        }
    }
}

using HarmonyLib;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(HediffSet), "AddDirect")]
    public static class HediffSet_AddDirect_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        private static bool Prefix(HediffSet __instance, Pawn ___pawn, Hediff hediff)
        {
            return Pawn_HealthTracker_AddHediff_Patch.HandleHediff(___pawn, hediff);
        }
    }
}

using HarmonyLib;
using RimWorld;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(Pawn_NeedsTracker), "NeedsTrackerTickInterval")]
    public static class Pawn_NeedsTracker_NeedsTrackerTickInterval_Patch
    {
        private static bool Prefix(Pawn_NeedsTracker __instance)
        {
            if (__instance.pawn.HasHediff(AC_DefOf.AC_CryptoStasis))
            {
                return false;
            }
            return true;
        }
    }
}


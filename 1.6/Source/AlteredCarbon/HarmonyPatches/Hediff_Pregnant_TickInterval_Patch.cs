using HarmonyLib;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(Hediff_Pregnant), "TickInterval")]
    public static class Hediff_Pregnant_TickInterval_Patch
    {
        public static bool Prefix(Pawn ___pawn)
        {
            if (___pawn.HasHediff(AC_DefOf.AC_CryptoStasis))
            {
                return false;
            }
            return true;
        }
    }
}


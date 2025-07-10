using HarmonyLib;
using RimWorld;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(HealthUtility), "TryAnesthetize")]
    public static class HealthUtility_TryAnesthetize_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(Pawn pawn, ref bool __result)
        {
            if (pawn.CurrentBed() is Building_SleeveCasket || pawn.IsEmptySleeve())
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}


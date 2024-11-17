using HarmonyLib;
using RimWorld;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(Building_GeneExtractor), "CanAcceptPawn")]
    public static class Building_GeneExtractor_CanAcceptPawn_Patch
    {
        public static void Postfix(Pawn pawn, ref AcceptanceReport __result)
        {
            if (pawn.IsEmptySleeve())
            {
                __result = "AC.IsEmptySleeve".Translate();
            }
        }
    }

}

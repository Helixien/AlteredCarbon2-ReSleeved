using HarmonyLib;
using RimWorld;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(Pawn_FoodRestrictionTracker), "Configurable", MethodType.Getter)]
    public static class Pawn_FoodRestrictionTracker_Configurable_Patch
    {
        public static void Postfix(Pawn_FoodRestrictionTracker __instance, ref bool __result)
        {
            if (__result is false && __instance.pawn.IsEmptySleeve())
            {
                __result = true;
            }
        }
    }
}

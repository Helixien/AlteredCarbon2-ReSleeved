using HarmonyLib;
using RimWorld;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(Building_SubcoreScanner), "KillOccupant")]
    public static class Building_SubcoreScanner_KillOccupant_Patch
    {
        public static void Prefix(Building_SubcoreScanner __instance)
        {
            var stack = __instance.Occupant.GetNeuralStack();
            if (stack != null)
            {
                stack.preventKill = true;
                __instance.Occupant.health.RemoveHediff(stack);
            }
        }
    }
}

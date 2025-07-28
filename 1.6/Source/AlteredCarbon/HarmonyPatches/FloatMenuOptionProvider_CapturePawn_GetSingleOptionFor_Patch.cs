using HarmonyLib;
using RimWorld;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(FloatMenuOptionProvider_CapturePawn), "GetSingleOptionFor")]
    public static class FloatMenuOptionProvider_CapturePawn_GetSingleOptionFor_Patch
    {
        public static void Postfix(Pawn clickedPawn, FloatMenuContext context, ref FloatMenuOption __result)
        {
            if (clickedPawn.IsEmptySleeve())
            {
                __result = null;
            }
        }
    }
}

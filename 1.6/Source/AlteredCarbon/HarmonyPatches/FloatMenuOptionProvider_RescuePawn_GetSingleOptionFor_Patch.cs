using HarmonyLib;
using RimWorld;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(FloatMenuOptionProvider_RescuePawn), "GetSingleOptionFor")]
    public static class FloatMenuOptionProvider_RescuePawn_GetSingleOptionFor_Patch
    {
        public static void Postfix(Pawn clickedPawn, FloatMenuContext context, ref FloatMenuOption __result)
        {
            if (__result != null && clickedPawn.IsEmptySleeve())
            {
                __result.Label = "AC.TakeToSleeveCasketOrMedicalBed".Translate();
                __result.action = delegate
                {
                    AC_Utils.AddTakeEmptySleeveJob(context.FirstSelectedPawn, clickedPawn, true);
                };
            }
        }
    }
}

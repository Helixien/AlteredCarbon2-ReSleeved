using HarmonyLib;
using RimWorld;
using Verse;

namespace AlteredCarbon
{
    [HotSwappable]
    [HarmonyPatch(typeof(ContainingSelectionUtility), "SelectContainingThingGizmo")]
    public static class ContainingSelectionUtility_SelectContainingThingGizmo_Patch
    {
        public static void Postfix(ref Gizmo __result, Thing containedThing)
        {
            if (containedThing is Pawn pawn && pawn.CurrentBed() is Building_SleeveCasket casket)
            {
                __result = ContainingSelectionUtility.CreateSelectGizmo("CommandSelectContainerThing", "CommandSelectContainerThingDesc", casket);
                var giz = __result as Command_SelectStorage;
                giz.defaultLabel = "CommandSelectContainerThing".Translate(casket);
            }
        }
    }
}
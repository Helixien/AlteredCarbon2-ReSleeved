using HarmonyLib;
using RimWorld;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(ThingStyleHelper), "SetStyleDef")]
    public static class ThingStyleHelper_SetStyleDef_Patch
    {
        public static void Postfix(Thing thing)
        {
            if (thing is NeuralStack stack)
            {
                stack.ResetStackGraphics();
            }
        }
    }
}

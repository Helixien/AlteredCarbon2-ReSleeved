using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(HealthCardUtility), "DrawOverviewTab")]
    public static class HealthCardUtility_DrawOverviewTab_Patch
    {
        public static bool CheckEmptySleeve(bool original, Pawn pawn)
        {
            if (pawn.IsEmptySleeve())
            {
                return true;
            }
            return original;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                var instruction = codes[i]; 
                yield return instruction;
                if (instruction.opcode == OpCodes.Ldloc_2)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HealthCardUtility_DrawOverviewTab_Patch), nameof(CheckEmptySleeve)));
                }
            }
        }
    }
}

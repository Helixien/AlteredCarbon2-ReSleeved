using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(MainTabWindow_Inspect), "DoInspectPaneButtons")]
    public static class MainTabWindow_Inspect_DoInspectPaneButtons_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            var codes = codeInstructions.ToList();
            bool patched = false;
            for (var i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                yield return code;
                if (patched is false && code.opcode == OpCodes.Stind_R4 && codes[i - 2].opcode == OpCodes.Ldc_R4 && codes[i - 2].OperandIs(24))
                {
                    patched = true;
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 2);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(MainTabWindow_Inspect_DoInspectPaneButtons_Patch), nameof(AddRenameButton)));
                }
            }
        }

        public static void AddRenameButton(Thing thing, ref float num, ref float lineEndWidth)
        {
            if (thing is Building_NeuralMatrix matrix)
            {
                num -= 30f;
                DrawRenameButton(new Rect(num, 0f, 30f, 30f), matrix);
                lineEndWidth += 30f;
            }
        }

        public static void DrawRenameButton(Rect rect, Building_NeuralMatrix matrix)
        {
            if (Widgets.ButtonImage(rect, TexButton.Rename))
            {
                Find.WindowStack.Add(new Dialog_RenameMatrix(matrix));
            }
        }
    }
}
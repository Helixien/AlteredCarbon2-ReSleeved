using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace AlteredCarbon
{
    [HotSwappable]
    [HarmonyPatch(typeof(SocialCardUtility), "DrawPawnRow")]
    public static class SocialCardUtility_DrawPawnRow_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            var targetString = "MessageCantSelectOffMapPawn";
            var messageMethod = AccessTools.Method(typeof(Messages), nameof(Messages.Message), new[] { typeof(string), typeof(MessageTypeDef), typeof(bool) });
            var replacementMethod = AccessTools.Method(typeof(SocialCardUtility_DrawPawnRow_Patch), nameof(TrySendMessageOrSelectStack));
            for (int i = 0; i < code.Count; i++)
            {
                if (i > 13 && code[i - 13].opcode == OpCodes.Ldstr && code[i - 13].OperandIs(targetString) && code[i].Calls(messageMethod))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_3);
                    yield return new CodeInstruction(OpCodes.Call, replacementMethod);
                }
                else
                {
                    yield return code[i];
                }
            }
        }

        public static void TrySendMessageOrSelectStack(string text, MessageTypeDef def, bool historical, Pawn otherPawn)
        {
            if (NeuralData.dummyPawns.TryGetValue(otherPawn, out var stack))
            {
                if (stack != null)
                {
                    Messages.Message("AC.PawnHasNoSleeve".Translate(otherPawn.Named("PAWN")), stack, def);
                    CameraJumper.TryJumpAndSelect(stack);
                }
            }
            else
            {
                Messages.Message(text, def, historical);
            }
        }
    }
}

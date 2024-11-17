using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using static RimWorld.ColonistBarColonistDrawer;

namespace AlteredCarbon
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(ColonistBarColonistDrawer), "DrawIcons")]
    public static class ColonistBarColonistDrawer_DrawIcons_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            for (var i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                yield return code;
                if (code.operand is MethodInfo info && info.ToString().Contains("Void Clear()"))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ColonistBarColonistDrawer_DrawIcons_Patch),
                        nameof(AddIcons)));
                }
            }
        }
        private static List<IconDrawCall> tmpIconsToDraw = new List<IconDrawCall>();

        public static void Postfix(ColonistBarColonistDrawer __instance, Rect rect, Pawn colonist)
        {
            if (colonist.Dead)
            {
                if (colonist.HasNeuralStack(out var stack))
                {
                    tmpIconsToDraw.Clear();
                    tmpIconsToDraw.Add(new IconDrawCall
                    (Icon_StackDead, "AC.SleeveWithStack".Translate()));
                    float num = Mathf.Min(BaseIconAreaWidth / (float)tmpIconsToDraw.Count, BaseIconMaxSize) 
                        * __instance.ColonistBar.Scale;
                    Vector2 pos = new Vector2(rect.x + 1f, rect.yMax - num - 1f);
                    foreach (IconDrawCall item in tmpIconsToDraw)
                    {
                        GUI.color = item.color ?? Color.white;
                        __instance.DrawIcon(item.texture, ref pos, num, item.tooltip);
                        GUI.color = Color.white;
                    }
                    tmpIconsToDraw.Clear();
                }
            }
        }

        public static readonly Texture2D Icon_StackDead = ContentFinder<Texture2D>.Get("UI/Icons/StackDead");
        public static readonly Texture2D ActiveNeedlecasting = ContentFinder<Texture2D>.Get("UI/Icons/ActiveNeedlecasting");

        public static void AddIcons(Pawn pawn)
        {
            if (pawn.HasNeuralStack(out var stack))
            {
                if (stack.Needlecasting)
                {
                    ColonistBarColonistDrawer.tmpIconsToDraw.Add(new ColonistBarColonistDrawer.IconDrawCall
                    (ActiveNeedlecasting, tooltip: "AC.NeedlecastingCaster".Translate(stack.needleCastingInto.pawn.Name.ToStringShort)));
                }
            }
            if (pawn.HasRemoteStack(out var remote) && remote.Needlecasted)
            {
                ColonistBarColonistDrawer.tmpIconsToDraw.Add(new ColonistBarColonistDrawer.IconDrawCall
                    (ActiveNeedlecasting, tooltip: "AC.NeedlecastingTarget".Translate(remote.Source.NeuralData.name.ToStringShort)));
            }
        }
    }
}


using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace AlteredCarbon
{
    [HotSwappable]
    [HarmonyPatch(typeof(TransferableUIUtility), "DoExtraIcons")]
    public static class TransferableUIUtility_DoExtraIcons_Patch
    {
        private static float IconWidth = 24f;
        public static void Postfix(Transferable trad, Rect rect, ref float curX)
        {
            var pawn = trad.AnyThing is Corpse corpse ? corpse.InnerPawn : trad.AnyThing as Pawn;
            if (pawn != null)
            {
                if (pawn.HasNeuralStack())
                {
                    var iconRect = new Rect(curX - IconWidth, (rect.height - IconWidth) / 2f, IconWidth, IconWidth);
                    GUI.DrawTexture(iconRect, ColonistBarColonistDrawer_DrawIcons_Patch.Icon_StackDead);
                    curX -= IconWidth;
                    TooltipHandler.TipRegion(iconRect, "AC.PawnHasNeutralStack".Translate(pawn));
                }
                else if ((Find.WindowStack.IsOpen<Dialog_FormCaravan>() || Find.WindowStack.IsOpen<Dialog_LoadTransporters>()) 
                    && pawn.HasRemoteStack(out var remoteStack) && remoteStack.Needlecasted)
                {
                    var iconRect = new Rect(curX - IconWidth, (rect.height - IconWidth) / 2f, IconWidth, IconWidth);
                    GUI.DrawTexture(iconRect, ColonistBarColonistDrawer_DrawIcons_Patch.ActiveNeedlecasting);
                    curX -= IconWidth;
                    var matrix = remoteStack.GetSourceMatrix();

                    TooltipHandler.TipRegion(iconRect, "AC.CaravanNeedlecastingIconTooltip".Translate
                        (remoteStack.Source.NeuralData.DummyPawn, matrix.NeedleCastRange()));
                }
            }

        }
    }
}

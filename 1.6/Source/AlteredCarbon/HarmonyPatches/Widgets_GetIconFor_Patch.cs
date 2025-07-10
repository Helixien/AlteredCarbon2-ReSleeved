using HarmonyLib;
using System;
using UnityEngine;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(Widgets), "GetIconFor", new Type[] { typeof(Thing), typeof(Vector2), typeof(Rot4?),
        typeof(bool), typeof(float), typeof(float), typeof(Vector2), typeof(Color), typeof(Material) },
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, 
            ArgumentType.Out, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out})]
    public static class Widgets_GetIconFor_Patch
    {
        public static void Postfix(ref Texture __result, Thing thing)
        {
            if (thing is NeuralStack stack)
            {
                __result = stack.Graphic.MatSingle.mainTexture;
            }
        }
    }
}

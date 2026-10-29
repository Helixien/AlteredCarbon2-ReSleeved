using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(LookTargets), MethodType.Constructor, new Type[] { typeof(Thing) })]
    public static class LookTargets_Patch
    {
        public static readonly ConditionalWeakTable<Pawn, List<LookTargets>> targets = new();
        public static void Postfix(LookTargets __instance, Thing t)
        {
            if (t is Pawn pawn)
            {
                targets.GetOrCreateValue(pawn).Add(__instance);
            }
        }
    }
}


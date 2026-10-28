using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(Bill_Medical), nameof(Bill_Medical.Notify_IterationCompleted))]
    public static class Bill_Medical_Notify_IterationCompleted_Patch
    {
        private static List<NeuralStack> ingredientStacks;
        private static List<NeuralStack> spawnedStacks = [];

        [HarmonyPriority(int.MaxValue)]
        public static void Prefix(List<Thing> ingredients)
        {
            ingredientStacks = ingredients.OfType<NeuralStack>().ToList();
            if (ingredientStacks.NullOrEmpty())
            {
                ingredientStacks = null;
            }
        }

        public static bool TryDeferSpawnedStack(NeuralStack stack)
        {
            if (ingredientStacks is null || ingredientStacks.Contains(stack))
            {
                return false;
            }
            spawnedStacks.Add(stack);
            return true;
        }

        [HarmonyPriority(int.MinValue)]
        public static void Finalizer()
        {
            var ingredients = ingredientStacks;
            ingredientStacks = null;
            if (ingredients is null)
            {
                return;
            }
            var stacks = spawnedStacks.ToList();
            spawnedStacks.Clear();
            foreach (var stack in stacks)
            {
                if (stack.Destroyed)
                {
                    continue;
                }
                if (ingredients.Any(x => x.def == stack.def && x.Destroyed is false))
                {
                    stack.DestroyNoKill();
                }
                else if (stack.Spawned && stack.IsActiveStack && stack.NeuralData.ContainsData is false)
                {
                    stack.CreateInnerPawn();
                }
            }
        }
    }
}

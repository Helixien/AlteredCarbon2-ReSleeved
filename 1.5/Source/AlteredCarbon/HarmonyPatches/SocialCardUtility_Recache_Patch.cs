using HarmonyLib;
using RimWorld;
using Verse;
using static RimWorld.SocialCardUtility;

namespace AlteredCarbon
{
    [HotSwappable]
    [HarmonyPatch(typeof(SocialCardUtility), "Recache")]
    public static class SocialCardUtility_Recache_Patch
    {
        public static void Postfix(Pawn selPawnForSocialInfo)
        {
            if (selPawnForSocialInfo.HasNeuralStack(out var stack))
            {
                var stackData = stack.NeuralData.StackGroupData;
                foreach (var copiedStack in stackData.copiedStacks)
                {
                    AddDummyRelationship(copiedStack, AC_DefOf.AC_Copy, AC_DefOf.AC_Original);
                }
                if (stackData.originalStack != null)
                {
                    AddDummyRelationship(stackData.originalStack, AC_DefOf.AC_Original, AC_DefOf.AC_Copy);
                }
            }
        }

        private static void AddDummyRelationship(NeuralStack stack, PawnRelationDef my, PawnRelationDef other)
        {
            var dummyPawn = stack.NeuralData.DummyPawn;
            if (cachedEntries.Any(x => x.otherPawn == dummyPawn) is false)
            {
                CachedSocialTabEntry cachedSocialTabEntry = new CachedSocialTabEntry();
                cachedSocialTabEntry.otherPawn = dummyPawn;
                cachedSocialTabEntry.relations.Add(my);
                cachedSocialTabEntry.opinionOfOtherPawn = my.opinionOffset;
                cachedSocialTabEntry.opinionOfMe = other.opinionOffset;
                cachedEntries.Add(cachedSocialTabEntry);
            }
        }
    }
}

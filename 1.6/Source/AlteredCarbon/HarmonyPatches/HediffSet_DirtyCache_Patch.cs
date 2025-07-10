using HarmonyLib;
using Verse;

namespace AlteredCarbon
{

    [HarmonyPatch(typeof(HediffSet), nameof(HediffSet.DirtyCache))]
    public static class HediffSet_DirtyCache_Patch
    {
        public static bool looking;
        public static void Postfix(HediffSet __instance)
        {
            if (looking) return;
            looking = true;
            if (__instance.pawn.HasRemoteStack(out var remote) && remote.Needlecasted)
            {
                var status = remote.GetConnectStatus(remote.Source);
                if (status != ConnectStatus.Connectable)
                {
                    remote.EndNeedlecasting(status);
                }
            }
            else if (__instance.pawn.HasNeuralStack(out var neural) && neural.needleCastingInto != null)
            {
                var status = neural.needleCastingInto.GetConnectStatus(neural);
                if (status != ConnectStatus.Connectable)
                {
                    neural.needleCastingInto.EndNeedlecasting(status);
                }
            }
            looking = false;
        }
    }
}

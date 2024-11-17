using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker), "AppendDrawRequests")]
    public static class PawnRenderNodeWorker_AppendDrawRequests_Patch
    {
        public static bool Prefix(PawnRenderNode node, PawnDrawParms parms, List<PawnGraphicDrawRequest> requests)
        {
            if (node is PawnRenderNode_Apparel)
            {
                if (parms.pawn.CurrentBed() is Building_SleeveCasket)
                {
                    requests.Add(new PawnGraphicDrawRequest(node));
                    return false;
                }
            }
            return true;
        }
    }
}
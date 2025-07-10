using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Linq;
using System.Text;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(Caravan), "GetInspectString")]
    public static class Caravan_GetInspectString_Patch
    {
        public static void Postfix(Caravan __instance, ref string __result)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(__result);
            if (stringBuilder.Length != 0)
            {
                var needlecastingPawn = __instance.PawnsListForReading.Select(x => 
                x.GetRemoteStack()).Where(x => x != null && x.Needlecasted).FirstOrDefault();
                if (needlecastingPawn != null)
                {
                    var matrix = needlecastingPawn.GetSourceMatrix();
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine("AC.MaximumNeedlecastingRange".Translate(matrix.NeedleCastRange()));
                    var tile = matrix.Tile;
                    var distance = Find.WorldGrid.ApproxDistanceInTiles(matrix.Tile, __instance.Tile);
                    stringBuilder.AppendLine("AC.CurrentDistanceToCastingOrigin".Translate(distance));
                    __result = stringBuilder.ToString().TrimEndNewlines();
                }
            }
        }
    }
}

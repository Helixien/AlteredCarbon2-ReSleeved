using HarmonyLib;
using RimWorld;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(RoomRoleWorker_Laboratory), nameof(RoomRoleWorker_Laboratory.GetScore))]
    public class RoomRoleWorker_Laboratory_GetScore_Patch
    {
        private const float UltraTechScorePerBuilding = 100001f;

        public static void Postfix(Room room, ref float __result)
        {
            var count = 0;
            foreach (var allContainedThing in room.ContainedAndAdjacentThings)
            {
                if (allContainedThing.def.IsUltraTechBuilding())
                {
                    count++;
                }
            }
            __result += UltraTechScorePerBuilding * count;
        }
    }
}

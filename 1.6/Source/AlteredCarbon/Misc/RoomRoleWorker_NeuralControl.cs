using System.Collections.Generic;
using Verse;

namespace AlteredCarbon
{
    public class RoomRoleWorker_NeuralControl : RoomRoleWorker
    {
        public override float GetScore(Room room)
        {
            int count = 0;
            List<Thing> allContainedThings = room.ContainedAndAdjacentThings;
            for (int i = 0; i < allContainedThings.Count; i++)
            {
                Thing th = allContainedThings[i];
                if (th.def.building?.buildingTags != null && th.def.building.buildingTags.Contains("Neural"))
                {
                    count++;
                }
            }
            return 100002f * (float)count;
        }
    }
}


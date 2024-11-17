using RimWorld;
using Verse;

namespace AlteredCarbon
{
    public class Recipe_RemoveRemoteStack : Recipe_RemoveImplant
	{
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (thing is Pawn pawn && pawn.HasNeuralStack())
            {
                return false;
            }
            return base.AvailableOnNow(thing, part);
        }
    }
}


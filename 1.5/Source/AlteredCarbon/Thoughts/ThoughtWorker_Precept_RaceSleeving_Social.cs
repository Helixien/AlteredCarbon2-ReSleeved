using RimWorld;
using Verse;

namespace AlteredCarbon;

public class ThoughtWorker_Precept_RaceSleeving_Social : ThoughtWorker_Precept_Social
{
    public override ThoughtState ShouldHaveThought(Pawn p, Pawn otherPawn)
    {
        return otherPawn.HasNeuralStack(out var stack) && (stack.NeuralData.OriginalRace != otherPawn.def);
    }
}
using RimWorld;
using Verse;

namespace AlteredCarbon;

public class ThoughtWorker_Precept_NoStack : ThoughtWorker_Precept
{
    public override ThoughtState ShouldHaveThought(Pawn p)
    {
        return p.AcceptsStacks() && !p.HasNeuralStack(out _) 
            && (p.HasRemoteStack(out var remote) is false || remote.Needlecasted is false);
    }
}
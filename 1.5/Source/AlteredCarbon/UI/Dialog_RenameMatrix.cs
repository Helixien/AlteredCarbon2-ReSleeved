using System.Linq;
using RimWorld;
using Verse;

namespace AlteredCarbon
{
    public class Dialog_RenameMatrix : Dialog_Rename<IRenameable>
    {
        public Dialog_RenameMatrix(IRenameable storage)
            : base(storage)
        {
        }

        public override AcceptanceReport NameIsValid(string name)
        {
            AcceptanceReport result = base.NameIsValid(name);
            if (!result.Accepted)
            {
                return result;
            }
            if (Current.Game.CurrentMap.listerThings.AllThings.OfType<Building_NeuralMatrix>().Any(m => m.name == name))
            {
                return "NameIsInUse".Translate();
            }
            return true;
        }
    }
}
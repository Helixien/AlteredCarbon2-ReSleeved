using Verse;

namespace PawnSnapshot
{
    public static class PawnExtensions
    {
        public static bool CanThink(this Pawn pawn)
        {
            return pawn?.needs?.mood?.thoughts?.memories != null;
        }
    }
}

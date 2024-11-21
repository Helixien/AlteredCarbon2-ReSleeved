using RimWorld;
using Verse;

namespace AlteredCarbon
{
    public class ThingFilterBladelink : ThingFilter
    {
        public override bool Allows(Thing t)
        {
            var comp = t.TryGetComp<CompBladelinkWeapon>();
            if (comp != null && comp.Biocoded)
            {
                return true;
            }
            return false;
        }
    }
}


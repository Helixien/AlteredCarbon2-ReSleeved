using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace AlteredCarbon
{
    public class Command_ActionOnPersona : Command_ActionOnThing
    {
        public Command_ActionOnPersona(Thing source, CommandInfo info) : base(source, info)
        {
        }

        public override HashSet<Thing> Things
        {
            get
            {
                var things = new HashSet<Thing>();
                foreach (var thing in source.MapHeld.listerThings.AllThings)
                {
                    var comp = thing.TryGetComp<CompBiocodable>();
                    if (comp != null && comp is CompBladelinkWeapon && comp.CodedPawn != null)
                    {
                        things.Add(thing);
                    }
                }
                return things.Where(x => x.PositionHeld.Fogged(x.MapHeld) is false).ToHashSet();
            }
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AlteredCarbon
{
    public class StorytellerCompProperties_SingleOnceRetrying : StorytellerCompProperties_SingleOnceFixed
    {
        public int retryIntervals = 15;

        public StorytellerCompProperties_SingleOnceRetrying() => compClass = typeof(StorytellerComp_SingleOnceRetrying);
    }

    public class StorytellerComp_SingleOnceRetrying : StorytellerComp
    {
        private StorytellerCompProperties_SingleOnceRetrying Props => (StorytellerCompProperties_SingleOnceRetrying)props;

        public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
        {
            var ticksGame = Find.TickManager.TicksGame;
            if (ticksGame < Props.fireAfterDaysPassed * GenDate.TicksPerDay || ticksGame / 1000 % Props.retryIntervals != 0)
            {
                yield break;
            }
            var incident = Props.incident;
            if (target.StoryState.lastFireTicks.ContainsKey(incident))
            {
                yield break;
            }
            if (incident.questScriptDef is not null && Find.QuestManager.QuestsListForReading.Any(x => x.root == incident.questScriptDef))
            {
                yield break;
            }
            if (incident.TargetAllowed(target))
            {
                yield return new FiringIncident(incident, this, GenerateParms(incident.category, target));
            }
        }
    }
}

using ModSettingsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AlteredCarbon
{
    public class AlteredCarbonSettingsWorker_General : PatchOperationWorker
    {
        public bool enableStackSpawning = true;
        public bool sleeveDeathDoesNotCauseGearTainting = true;
        public bool singleUseMentalFuses = true;
        public bool singleUseMentalFusePop = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableStackSpawning, "enableStackSpawning", true);
            Scribe_Values.Look(ref sleeveDeathDoesNotCauseGearTainting, "sleeveDeathDoesNotCauseGearTainting", true);
            Scribe_Values.Look(ref singleUseMentalFuses, "singleUseMentalFuses", true);
            Scribe_Values.Look(ref singleUseMentalFusePop, "singleUseMentalFusePop", true);
        }

        public override void CopyFrom(PatchOperationWorker savedWorker)
        {
            var copy = savedWorker as AlteredCarbonSettingsWorker_General;
            this.enableStackSpawning = copy.enableStackSpawning;
            this.sleeveDeathDoesNotCauseGearTainting = copy.sleeveDeathDoesNotCauseGearTainting;
            this.singleUseMentalFuses = copy.singleUseMentalFuses;
            this.singleUseMentalFusePop = copy.singleUseMentalFusePop;
        }

        public override void DoSettings(ModSettingsContainer container, Listing_Standard list)
        {
            DoCheckbox(list, "AC.EnableStackSpawning".Translate(), ref enableStackSpawning, "AC.EnableStackSpawningDesc".Translate());
            DoCheckbox(list, "AC.SleeveDeathDoesNotCauseGearTainting".Translate(), ref sleeveDeathDoesNotCauseGearTainting, null);
            DoCheckbox(list, "AC.SingleUseMentalFuses".Translate(), ref singleUseMentalFuses, "AC.SingleUseMentalFusesDesc".Translate());
            DoCheckbox(list, "AC.SingleUseMentalFusePop".Translate(), ref singleUseMentalFusePop, null);
        }

        public override void Reset()
        {
            enableStackSpawning = true;
            sleeveDeathDoesNotCauseGearTainting = true;
            singleUseMentalFuses = true;
            singleUseMentalFusePop = true;
        }
    }
}

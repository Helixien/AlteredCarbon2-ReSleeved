using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AlteredCarbon;

[HarmonyPatch(typeof(MentalBreakWorker), "TryStart")]
public class MentalBreakWorker_TryStart_Patch
{
    public static bool Prefix(Pawn pawn)
    {
        if (pawn.IsEmptySleeve())
        {
            return false;
        }
        
        if (pawn.health.hediffSet.TryGetHediff(AC_DefOf.AC_MentalFuse, out Hediff acMentalFuse))
        {
            //Send Letter
            var letter = LetterMaker.MakeLetter(
                "AC.TriggeredMentalFuseLabel".Translate(pawn.Named("PAWN")).AdjustedFor(pawn),
                "AC.TriggeredMentalFuseText".Translate(pawn.Named("PAWN")).AdjustedFor(pawn),
                LetterDefOf.NeutralEvent, new LookTargets(pawn));
            Find.LetterStack.ReceiveLetter(letter);

            //Stop current job
            pawn.jobs.StopAll();
            if (pawn.IsCarrying())
            {
                pawn.carryTracker.TryDropCarriedThing(pawn.PositionHeld, ThingPlaceMode.Near, out _);
            }

            //Give Catharsis
            pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.Catharsis);

            //Roll 5% chance
            if (!pawn.health.hediffSet.HasHediff(AC_DefOf.TraumaSavant) && Rand.Chance(0.05f))
            {
                //Give TraumaSavant
                pawn.health.AddHediff(AC_DefOf.TraumaSavant);

                //Send Trauma LetterX
                Find.LetterStack.ReceiveLetter(
                    "AC.MentalFuseTraumaLabel".Translate(),
                    "AC.MentalFuseTraumaText".Translate(pawn.Named("PAWN")).AdjustedFor(pawn),
                    LetterDefOf.NegativeEvent, pawn);
            }

            if (AC_Utils.generalSettings.singleUseMentalFuses)
            {
                pawn.health.RemoveHediff(acMentalFuse);
                Messages.Message("AC.MentalFuseBlownText".Translate(pawn.Named("PAWN")), MessageTypeDefOf.NegativeHealthEvent);

                if (AC_Utils.generalSettings.singleUseMentalFusePop)
                {
                    Effecter effecter = EffecterDefOf.ExtinguisherExplosion.Spawn();
                    effecter.Trigger(new TargetInfo(pawn.Position, pawn.MapHeld),
                        new TargetInfo(pawn.Position, pawn.MapHeld));
                    effecter.Cleanup();
                    GenExplosion.DoExplosion(pawn.Position, pawn.MapHeld, 2, DamageDefOf.Extinguish, null,
                        explosionSound: SoundDefOf.Explosion_FirefoamPopper,
                        postExplosionSpawnThingDef: ThingDefOf.Filth_FireFoam, postExplosionSpawnChance: 1f,
                        applyDamageToExplosionCellsNeighbors: true);
                }
            }

            return false;
        }

        return true;
    }
}
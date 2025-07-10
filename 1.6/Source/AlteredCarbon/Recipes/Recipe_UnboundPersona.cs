using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AlteredCarbon
{
    public class Recipe_UnboundPersona : RecipeWorker
    {
        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {

        }
        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            base.Notify_IterationCompleted(billDoer, ingredients);
            var personaWeapon = ingredients.FirstOrDefault(x => x.TryGetComp<CompBladelinkWeapon>() != null);
            var comp = personaWeapon.TryGetComp<CompBladelinkWeapon>();
            var intelSkill = billDoer.skills.GetSkill(SkillDefOf.Intellectual)?.Level ?? 0;
            float breakChance;

            if (intelSkill < 10)
            {
                breakChance = 1.0f;
            }
            else
            {
                breakChance = 0.55f - (0.05f * (intelSkill - 10));
                breakChance = Mathf.Clamp(breakChance, 0.05f, 0.55f); // Ensure it stays within 5% to 55%
            }

            if (!Rand.Chance(breakChance))
            {
                comp.UnCode();
                Messages.Message("AC.UnbondingPersonaSuccess".Translate(personaWeapon.LabelShort), personaWeapon, MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                personaWeapon.Destroy();
                Messages.Message("AC.UnbondingPersonaFailed".Translate(personaWeapon.LabelShort), billDoer, MessageTypeDefOf.NegativeEvent);
            }
        }
    }
}


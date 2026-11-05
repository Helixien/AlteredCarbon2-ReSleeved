using System;
using System.Collections.Generic;
using Verse;

namespace PawnSnapshot
{
    public class PawnRestoreOptions
    {
        public bool RestoreBody;
        public bool RestoreRelations = true;
        public bool RestoreFactionLeadership = true;
        public bool RestorePsycasts = true;
        public bool RestoreRoyalty = true;
        public bool RestoreIdeology = true;
        public bool IsDummyRestoration;
        public List<string> IgnoredTraitDefNames;
        public Func<Def, bool> RetainExtraAbility;
        public Func<Def, bool> RetainLearnedAbility;
    }
}

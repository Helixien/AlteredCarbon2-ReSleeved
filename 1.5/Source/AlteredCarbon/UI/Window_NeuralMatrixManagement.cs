﻿using Verse;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using RimWorld;
using Verse.Sound;

namespace AlteredCarbon
{
    [StaticConstructorOnStartup]
    [HotSwappable]
    public class Window_NeuralMatrixManagement : Window
    {
        public Building_NeuralMatrix matrix;
        private Vector2 scrollPosition;
        private IStackHolder selectedStack;
        private FilterManager<IStackHolder> filterManager;
        private List<IStackHolder> allItems, currentItems;
        private List<TabRecord> tabsList = new();

        public enum StackTab : byte
        {
            Social,
            Character,
            Needs,
            Records,
        }

        private StackTab tab;

        public Window_NeuralMatrixManagement(Building_NeuralMatrix matrix)
        {
            this.matrix = matrix;
            this.doCloseX = true;
            this.doWindowBackground = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            CreateFilters();
            RefreshItems();
            var trackedItems = currentItems.Where(item => item.ThingHolder is Pawn || item.NeuralData.trackedToMatrix == matrix).ToList();
            selectedStack = trackedItems.FirstOrDefault();
        }
        private void CreateFilters()
        {
            var filters = new List<Filter<IStackHolder>>
                {
                    new("AC.Tracked".Translate(), stackHolder => stackHolder.ThingHolder is Pawn
                        || stackHolder.NeuralData.trackedToMatrix == matrix),
                    new("AC.Untracked".Translate(), stackHolder => stackHolder.ThingHolder is not Pawn
                        && stackHolder.NeuralData.trackedToMatrix != matrix),
                    new("AC.Colonists".Translate(), stackHolder => stackHolder.NeuralData.Colonist),
                    new("AC.Friendly".Translate(), stackHolder => stackHolder.NeuralData.Friendly),
                    new("AC.Hostile".Translate(), stackHolder => stackHolder.NeuralData.Hostile),
                    new("AC.Needlecasting".Translate(), stackHolder => (stackHolder as INeedlecastable).Needlecasting),
                    new("AC.Sleeved".Translate(), stackHolder => stackHolder.ThingHolder is Pawn),
                    new("AC.Archostack".Translate(), stackHolder => stackHolder.ThingHolder is NeuralStack stack
                        && stack.IsArchotechStack),
                    new("AC.Duplicates".Translate(), stackHolder => stackHolder.Pawn.IsCopy())
                };


            this.filterManager = new FilterManager<IStackHolder>(
                filters,
                newItems => currentItems = newItems,
                GetItems
            );
        }


        public enum StackState
        {
            Sleeved,
            Dead,
            Needlecasting,
            Stored,
            Dormant,
            Lost
        }

        string GetStackStateStatus(IStackHolder stack)
        {
            var states = GetStates(stack);
            return string.Join(", ", states.Select(state => (string)(state switch
            {
                StackState.Sleeved => "AC.Sleeved".Translate(),
                StackState.Dead => "AC.Dead".Translate(),
                StackState.Needlecasting => "AC.Needlecasting".Translate(),
                StackState.Stored => "AC.Stored".Translate(),
                StackState.Dormant => "AC.Dormant".Translate(),
                StackState.Lost => "AC.Lost".Translate(),
                _ => string.Empty
            }))).ToLower().CapitalizeFirst();
        }

        IEnumerable<StackState> GetStates(IStackHolder stackHolder)
        {
            var needleCastable = stackHolder as INeedlecastable;
            bool needlecasting = needleCastable.Needlecasting;
            if (needlecasting)
            {
                yield return StackState.Needlecasting;
            }
            if (stackHolder.ThingHolder is NeuralStack neuralStack)
            {
                if (neuralStack.ParentHolder is CompNeuralCache)
                {
                    yield return StackState.Stored;
                }
                if (needlecasting is false)

                    yield return StackState.Dormant;
            }
            else if (stackHolder.ThingHolder is Pawn pawn)
            {
                yield return StackState.Sleeved;
                if (pawn.Dead)
                {
                    yield return StackState.Dead;
                }
                if (pawn.IsLost())
                {
                    yield return StackState.Lost;
                }
            }
        }

        private void RefreshItems()
        {
            var items = matrix.AllNeuralStacks.Concat(matrix.Map.listerThings.GetThingsOfType<NeuralStack>()
                .Where(x => x.Fogged() is false && x.IsActiveStack && x.NeuralData.trackedToMatrix == matrix)).Cast<IStackHolder>();
            var hediffs = PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead.Select(pawn => pawn.GetNeuralStack())
                .Where(x => x != null&& x.NeuralData.trackedToMatrix == matrix).Cast<IStackHolder>();
            allItems = [.. items, .. hediffs];
            currentItems = GetItems();
        }

        private void CreateTabs()
        {
            tabsList.Clear();
            tabsList.Add(new TabRecord("TabSocial".Translate(), delegate
            {
                tab = StackTab.Social;
            }, tab == StackTab.Social));
            tabsList.Add(new TabRecord("TabCharacter".Translate(), delegate
            {
                tab = StackTab.Character;
            }, tab == StackTab.Character));
            tabsList.Add(new TabRecord("TabNeeds".Translate(), delegate
            {
                tab = StackTab.Needs;
            }, tab == StackTab.Needs));
            tabsList.Add(new TabRecord("TabRecords".Translate(), delegate
            {
                tab = StackTab.Records;
            }, tab == StackTab.Records));
        }

        public List<IStackHolder> GetItems()
        {
            var items = new List<IStackHolder>();
            if (filterManager.currentFilter != null)
            {
                items = allItems.Where(x => filterManager.currentFilter.Logic(x)).ToList();
            }
            else
            {
                items = allItems;
            }
            return items.OrderBy(x => x.NeuralData.PawnNameColored.ToString()).ToList();
        }

        public override Vector2 InitialSize => new(1024f, 768f);

        public override void DoWindowContents(Rect inRect)
        {
            Rect leftRect = new(inRect.x, inRect.y, 450, inRect.height);
            Rect rightRect = new(leftRect.xMax + 15, inRect.y, inRect.width - leftRect.width - 15, inRect.height);
            DrawLeftPanel(leftRect);
            if (selectedStack != null)
            {
                DrawRightPanel(rightRect);
            }
        }

        private void DrawLeftPanel(Rect rect)
        {
            Rect leftRect = rect;
            leftRect.height = 30f;
            Text.Font = GameFont.Medium;
            Widgets.Label(leftRect, "AC.NeuralMatrixManagement".Translate());
            Text.Font = GameFont.Tiny;
            leftRect.y += 50f;
            GUI.color = Color.grey;
            Widgets.Label(leftRect, "AC.StorageFilters".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            leftRect.y += 15f;

            DrawCheckboxAndUpdateCaches(ref matrix.compCache.allowColonistNeuralStacks, "AC.AllowColonistNeuralStacks", ref leftRect, (comp, val) => comp.allowColonistNeuralStacks = val);
            DrawCheckboxAndUpdateCaches(ref matrix.compCache.allowStrangerNeuralStacks, "AC.AllowStrangerNeuralStacks", ref leftRect, (comp, val) => comp.allowStrangerNeuralStacks = val);
            DrawCheckboxAndUpdateCaches(ref matrix.compCache.allowHostileNeuralStacks, "AC.AllowHostileNeuralStacks", ref leftRect, (comp, val) => comp.allowHostileNeuralStacks = val);
            leftRect.y += 10f;

            Text.Font = GameFont.Tiny;
            GUI.color = Color.grey;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(leftRect, "AC.StacksStored".Translate(matrix.AllNeuralStacks.Count(), matrix.AllNeuralCaches.Sum(x => x.Props.stackLimit)));
            leftRect.y += 15f;
            Widgets.Label(leftRect, "AC.StacksTracked".Translate(allItems.Where(x =>
            x.NeuralData.trackedToMatrix == matrix).Count()));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            filterManager.DoFilters(leftRect);
            leftRect.y += 25f;
            GUI.color = Color.grey;
            leftRect.y += 5f;
            GUI.DrawTexture(new Rect(leftRect.x, leftRect.y, rect.width, 2f), BaseContent.WhiteTex);
            GUI.color = Color.white;
            leftRect.y += 5f;

            var trackedItems = currentItems.Where(item => item.ThingHolder is Pawn || item.NeuralData.trackedToMatrix == matrix).ToList();
            var untrackedItems = currentItems.Where(x => trackedItems.Contains(x) is false).ToList();
            Rect outRect = new(rect.x, leftRect.y, rect.width, rect.height - 40f - leftRect.y);
            Rect viewRect = new(0f, 0f, rect.width - 16f, (25 + (trackedItems.Count + untrackedItems.Count)
                * 52f) + (untrackedItems.Any() ? 5 : 30) + (trackedItems.Any() ? 0 : 24));
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float curY = 0f;
            if (trackedItems.Any())
            {
                DrawStackEntries(trackedItems, ref curY, viewRect.width);
            }
            else
            {
                Widgets.Label(new Rect(leftRect.x, curY, viewRect.width, 24), "AC.NoStacksRegistered".Translate());
                curY += 24f;
            }
            curY += 5f;
            DrawSection(ref curY, "AC.Untracked".Translate() + ":", viewRect.width, rect);
            if (untrackedItems.Any())
            {
                DrawStackEntries(untrackedItems, ref curY, viewRect.width);
            }
            else
            {
                Widgets.Label(new Rect(leftRect.x, curY, viewRect.width, 24), "AC.NoUntrackedStacksStored".Translate());
                curY += 24f;
            }
            Widgets.EndScrollView();
        }

        public void DrawCheckboxAndUpdateCaches(ref bool value, string translationKey, ref Rect rect, Action<CompNeuralCache, bool> updateField)
        {
            Widgets.CheckboxLabeled(rect, translationKey.Translate(), ref value);
            foreach (var comp in matrix.AllNeuralCaches)
            {
                updateField(comp, value);
            }
            rect.y += 30f;
        }


        public void DrawSection(ref float curY, string header, float viewWidth, Rect rect)
        {
            GUI.color = Color.grey;
            Widgets.Label(new Rect(0f, curY, viewWidth, 24), header);
            curY += 20;

            GUI.DrawTexture(new Rect(0f, curY, rect.width, 2f), BaseContent.WhiteTex);
            GUI.color = Color.white;
            curY += 5f;
        }
        public void DrawStackEntries(List<IStackHolder> items, ref float curY, float viewWidth)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var stack = items[i];
                var stackEntryRect = new Rect(0f, curY, viewWidth, 50f);

                if (stack == selectedStack)
                {
                    Widgets.DrawHighlightSelected(stackEntryRect);
                }
                else if (i % 2 == 1)
                {
                    Widgets.DrawHighlight(stackEntryRect);
                }
                DrawStackEntry(stackEntryRect, stack);
                curY += 52;
            }
        }

        private static readonly Texture2D addIcon = ContentFinder<Texture2D>.Get("UI/Icons/Add", true);
        private static readonly Texture2D cancelIcon = ContentFinder<Texture2D>.Get("UI/Icons/Cancel", true);
        private static readonly Texture2D needlecastIcon = ContentFinder<Texture2D>.Get("UI/Icons/Needlecast", true);
        private static readonly Texture2D installStackIcon = ContentFinder<Texture2D>.Get("UI/Icons/InstallStack", true);
        private static readonly Texture2D downArrowIcon = ContentFinder<Texture2D>.Get("UI/Icons/Drop", true);
        private static readonly Texture2D trashIcon = ContentFinder<Texture2D>.Get("UI/Icons/Erase", true);

        private void DrawStackEntry(Rect rect, IStackHolder stack)
        {
            var data = stack.NeuralData;
            var thing = stack.ThingHolder;
            if (thing is Pawn && thing.Spawned is false && thing.ParentHolder is Thing holderThing)
            {
                thing = holderThing;
            }
            var needleCastingInto = (stack as INeedlecastable).NeedleCastingInto;
            if (needleCastingInto != null)
            {
                thing = needleCastingInto.pawn;
            }
            (Thing thing, TaggedString pawnName, string factionName) cache = new(thing, data.PawnNameColored, data.faction?.Name);
            Rect iconRect = new(rect.x, rect.y, rect.height, rect.height);
            Widgets.ThingIcon(iconRect, cache.thing);

            float iconSize = 25f;
            float totalIconsWidth = (iconSize + 5f) * 4;

            Rect nameRect = new(iconRect.xMax, rect.y, rect.width - iconRect.width - totalIconsWidth, 24);
            Widgets.Label(nameRect, cache.pawnName);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.grey;
            Rect factionRect = new(nameRect.x, nameRect.yMax - 5, nameRect.width, 15);
            if (stack.NeuralData.faction != null)
            {
                Widgets.Label(factionRect, "AC.Faction".Translate() + ": " + cache.factionName);
            }
            Rect statusRect = new(nameRect.x, factionRect.yMax, nameRect.width, factionRect.height);

            Widgets.Label(statusRect, "AC.Status".Translate() + ": " + GetStackStateStatus(stack));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            float iconSpacing = iconSize + 5f;
            float iconXPos = rect.xMax;
            float iconYPos = rect.y + (rect.height - iconSize) / 2f;

            Rect iconRectWithOffset = new(iconXPos - iconSize, iconYPos, iconSize, iconSize);
            var neuralStack = stack.ThingHolder as NeuralStack;
            var needleCastable = stack as INeedlecastable;
            var needleCasting = needleCastable.Needlecasting;

            bool isManagingButtonActive = true;
            if (needleCasting)
            {
                isManagingButtonActive = false;
            }
            bool tracked = stack.NeuralData.trackedToMatrix == matrix;
            if (tracked)
            {
                DrawButton(iconRectWithOffset, trashIcon, tooltip: "AC.TooltipStopManaging".Translate(cache.pawnName), isManagingButtonActive, delegate
                {
                    stack.NeuralData.trackedToMatrix = null;
                    RefreshItems();
                });
            }
            else
            {
                DrawButton(iconRectWithOffset, addIcon, tooltip: "AC.TooltipTrackStack".Translate(cache.pawnName), isManagingButtonActive, delegate
                {
                    stack.NeuralData.trackedToMatrix = matrix;
                    RefreshItems();
                });
            }

            iconRectWithOffset.x -= iconSpacing;

            bool isRemoveButtonActive = true;
            if (needleCasting)
            {
                isRemoveButtonActive = false;
            }
            if (neuralStack != null)
            {
                bool isStored = neuralStack.ParentHolder is CompNeuralCache;
                if (isStored)
                {
                    DrawButton(iconRectWithOffset, downArrowIcon, tooltip: "AC.TooltipRemoveStack".Translate(), isRemoveButtonActive, delegate
                    {
                        neuralStack.ParentHolder.GetDirectlyHeldThings().TryDrop(neuralStack, ThingPlaceMode.Near, out _);
                        RefreshItems();
                    });
                    iconRectWithOffset.x -= iconSpacing;
                }

                if (AC_Utils.stackRecipesByDef.TryGetValue(neuralStack.def, out var installInfo))
                {
                    var assignedPawn = neuralStack.AssignedPawnForInstalling;
                    bool isImplantingButtonActive = isStored && needleCasting is false;
                    if (assignedPawn != null)
                    {
                        DrawButton(iconRectWithOffset, cancelIcon, "AC.TooltipCancelImplanting".Translate(cache.pawnName, assignedPawn.Label),
                            isImplantingButtonActive, () => assignedPawn.BillStack.bills
                            .RemoveAll(x => x is Bill_InstallStack installStack 
                            && installStack.stackToInstall == neuralStack));
                    }
                    else
                    {
                        DrawButton(iconRectWithOffset, installStackIcon, tooltip:
                            "AC.TooltipImplantStack".Translate(cache.pawnName), isImplantingButtonActive, delegate
                            {
                                Close();
                                Find.Targeter.BeginTargeting(neuralStack.ForPawn(), delegate (LocalTargetInfo x)
                                {
                                    if (AC_Utils.CanImplantStackTo(installInfo.recipe.addsHediff, x.Pawn, neuralStack, true))
                                    {
                                        neuralStack.InstallStackRecipe(x.Pawn, installInfo.recipe);
                                    }
                                });
                            });
                    }

                    iconRectWithOffset.x -= iconSpacing;
                }
            }
            if (tracked && AC_DefOf.AC_NeuralCasting.IsFinished 
                && (neuralStack != null && neuralStack.ParentHolder is CompNeuralCache stackCache 
                && stackCache.parent.GetConnectedMatrix() == matrix || stack.ThingHolder is Pawn pawn 
                && pawn.ParentHolder is Building_CryptosleepCasket casket && casket.GetConnectedMatrix() == matrix))
            {
                bool isNeedlecastingButtonActive = stack.NeuralData.faction == Faction.OfPlayer;

                if (neuralStack != null && (neuralStack.ParentHolder is not CompNeuralCache 
                    || neuralStack.AssignedPawnForInstalling is not null || matrix.Powered is false))
                {
                    isNeedlecastingButtonActive = false;
                }

                if (needleCasting)
                {
                    DrawButton(iconRectWithOffset, cancelIcon, "AC.TooltipStopNeedlecasting".Translate(cache.pawnName, needleCastable.NeedleCastingInto.originalPawnData.name.ToStringShort),
                               isNeedlecastingButtonActive, delegate
                               {
                                   if (matrix.Tile != needleCastable.NeedleCastingInto.pawn.Tile)
                                   {
                                       Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("AC.NeedlecastingDisconnectWarning".Translate(needleCastable.NeedleCastingInto.pawn), delegate
                                       {
                                           needleCastable.NeedleCastingInto.EndNeedlecasting(ConnectStatus.StoppedManually);
                                       }));
                                   }
                                   else
                                   {
                                       needleCastable.NeedleCastingInto.EndNeedlecasting(ConnectStatus.StoppedManually);
                                   }
                               });
                }
                else
                {
                    DrawButton(iconRectWithOffset, needlecastIcon, "AC.TooltipNeedlecast".Translate(cache.pawnName),
                               isNeedlecastingButtonActive,
                               () =>
                               {
                                   var connectablePawns = needleCastable.GetAllConnectablePawns();
                                   var floatList = GetFloatList(needleCastable, connectablePawns);
                                   Find.WindowStack.Add(new FloatMenu(floatList));
                               });
                }
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Event.current.clickCount == 2
                    && Mouse.IsOver(rect))
            {
                Event.current.Use();
                Close();
                CameraJumper.TryJumpAndSelect(stack.ThingHolder);
            }
            else if (selectedStack != stack && Widgets.ButtonInvisible(rect))
            {
                selectedStack = stack;
            }
        }

        public static List<FloatMenuOption> GetFloatList(INeedlecastable needleCastable, Dictionary<Pawn, ConnectStatus> connectablePawns)
        {
            var floatList = new List<FloatMenuOption>();
            foreach (var otherPawn in connectablePawns)
            {
                if (otherPawn.Value != ConnectStatus.Connectable)
                {
                    var label = otherPawn.Key.LabelShort.Colorize(PawnNameColorUtility.PawnNameColorOf(otherPawn.Key)) + ": " + otherPawn.Value.GetLabel();
                    floatList.Add(new FloatMenuOption(label, null, iconThing: otherPawn.Key, iconColor: Color.white));
                }
                else
                {
                    floatList.Add(new FloatMenuOption(otherPawn.Key.LabelShort.Colorize(PawnNameColorUtility.PawnNameColorOf(otherPawn.Key)), delegate ()
                    {
                        Find.Targeter.StopTargeting();
                        needleCastable.NeedlecastTo(otherPawn.Key);
                    }, iconThing: otherPawn.Key, iconColor: Color.white));
                }
            }
            if (connectablePawns.Any() is false)
            {
                floatList.Add(new FloatMenuOption("None".Translate(), null));
            }
            return floatList;
        }

        private void DrawButton(Rect rect, Texture2D icon, string tooltip, bool isActive, Action action)
        {
            Color buttonColor = isActive ? Color.white : Color.grey;
            Color mouseoverColor = isActive ? GenUI.MouseoverColor : Color.grey;
            bool buttonClicked = Widgets.ButtonImage(rect, icon, buttonColor, mouseoverColor: mouseoverColor, tooltip: tooltip);
            if (isActive && buttonClicked)
            {
                action?.Invoke();
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            }
        }

        private Vector2 thoughtScrollPosition;

        private void DrawRightPanel(Rect rect)
        {
            var panelRect = rect;
            var thing = selectedStack.ThingHolder;
            var pawn = selectedStack.Pawn;
            if (thing is NeuralStack stack && stack.needleCastingInto != null)
            {
                pawn = stack.needleCastingInto.pawn;
            }
            CreateTabs();
            TabDrawer.DrawTabs(new Rect(rect.x, rect.y + 32, rect.width, rect.height), tabsList);
            rect.y += 32;
            rect.height -= 32;
            rect.height -= 200;
            Widgets.DrawMenuSection(rect);
            switch (tab)
            {
                case StackTab.Character:
                    {
                        Vector2 vector = CharacterCardUtility.PawnCardSize(pawn);
                        CharacterCardUtility.DrawCharacterCard(new Rect(rect.x + 17f, rect.y + 10f, vector.x, vector.y), pawn);
                    }
                    break;
                case StackTab.Social:
                    {
                        SocialCardUtility.DrawSocialCard(rect, pawn);
                    }
                    break;
                case StackTab.Needs:
                    {
                        if (pawn.IsDummyPawn() is false)
                        {
                            var size = NeedsCardUtility.GetSize(pawn);
                            Widgets.BeginGroup(rect);
                            NeedsCardUtility.DoNeedsMoodAndThoughts(new Rect(0, 0, size.x, size.y), pawn, ref thoughtScrollPosition);
                            Widgets.EndGroup();
                        }
                        else
                        {
                            Rect label = new Rect(rect.x + 10, rect.y + 125, rect.width, 70);
                            Widgets.Label(label, "AC.NeedsUnavailable".Translate(pawn));
                        }
                    }
                    break;
                case StackTab.Records:
                    {
                        RecordsCardUtility.DrawRecordsCard(rect, pawn);
                    }
                    break;
            }

            DrawBottom(rect, panelRect);
        }

        private void DrawBottom(Rect rect, Rect panelRect)
        {

            float bottomSectionHeight = 140f;
            Rect bottomRect = new(rect.x, rect.yMax + 15, rect.width, bottomSectionHeight);
            Widgets.DrawMenuSection(bottomRect);

            Rect stackInfoRect = new(bottomRect.x + 10f, bottomRect.y + 10f, bottomRect.width / 2 - 20f,
                bottomRect.height - 20f);
            Rect stackConfigRect = new(bottomRect.x + bottomRect.width / 2 + 10f, bottomRect.y + 10f, bottomRect.width / 2 - 20f, bottomRect.height - 20f);

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Medium;
            Widgets.Label(stackInfoRect, "AC.StackInformation".Translate());

            Text.Font = GameFont.Tiny;
            var neuralData = selectedStack.NeuralData;
            float infoContentY = stackInfoRect.y + Text.LineHeight + 10f;
            Rect infoContentRect = new(stackInfoRect.x, infoContentY, stackInfoRect.width, stackInfoRect.height - Text.LineHeight - 5f);
            var range = matrix.NeedleCastRange();
            Widgets.Label(infoContentRect,
                "AC.CurrentlyStatus".Translate() + ": " + GetStackStateStatus(selectedStack) + "\n" +
                "AC.OriginalGender".Translate() + ": " + neuralData.OriginalGender.ToString() + "\n" +
                "AC.NeedlecastingRange".Translate() + ": " + range + " " +
                (range > 1 ? "TilesLower".Translate() : "AC.Tile".Translate()) + "\n" +
                "AC.StackDegradation".Translate(neuralData.stackDegradation.ToStringPercent().Colorize(Color.red)) + "\n" +
                "AC.IsCopy".Translate() + ": " + (neuralData.isCopied ? "AC.True".Translate() : "AC.False".Translate()));
            Text.Font = GameFont.Small;

            if (selectedStack.ThingHolder is NeuralStack neuralStack)
            {
                Text.Font = GameFont.Medium;
                Widgets.Label(stackConfigRect, "AC.StackConfiguration".Translate());
                Text.Font = GameFont.Small;
                float configContentY = stackConfigRect.y + Text.LineHeight + 5f;
                float checkboxHeight = 24f;
                Rect autobankRect = new(stackConfigRect.x, configContentY, stackConfigRect.width, checkboxHeight);
                Widgets.CheckboxLabeled(autobankRect, "AC.AutobankStack".Translate(), ref neuralStack.autoLoad);
            }

            Rect closeButtonRect = new(rect.xMax - 150f, panelRect.height - 30f, 150, 30f);
            if (Widgets.ButtonText(closeButtonRect, "Close".Translate()))
            {
                Close();
            }
        }
    }
}

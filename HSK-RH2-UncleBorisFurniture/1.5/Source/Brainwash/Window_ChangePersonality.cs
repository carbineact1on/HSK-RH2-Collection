using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Brainwash
{
    [HotSwappable]
    public class Window_ChangePersonality : Window
    {
        public CompChangePersonality comp;
        public List<TraitEntry> traitsToSet = new();
        public List<SkillEntry> skillsToSet = new();
        public List<TraitEntry> alltraits = new();
        public List<BackstoryDef> backstoriesToSet = new();

        public bool reduceCertainty;
        public override Vector2 InitialSize => new(600, GetHeight());

        private int GetHeight()
        {
            var baseHeight = 400;
            if (BrainwashSettings.modifyBackstoriesForBrainwashing)
            {
                baseHeight += 40;
                baseHeight += 24;
                if (comp.pawn.story.AllBackstories.Count == 2)
                {
                    baseHeight += 24;
                }
                baseHeight += 15;
            }
            return baseHeight;
        }

        public Action actionOnAccept;
        public Window_ChangePersonality(CompChangePersonality comp, Action actionOnAccept)
        {
            this.comp = comp;
            this.comp.traitsToSet = null;
            this.comp.skillsToSet = null;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.comp.backstoriesToSet = null;
            traitsToSet = new List<TraitEntry>();
            alltraits = new List<TraitEntry>();
            foreach (Trait trait in comp.pawn.story.traits.allTraits)
            {
                if (trait.sourceGene is null)
                {
                    traitsToSet.Add(new TraitEntry
                    {
                        traitDef = trait.def,
                        degree = trait.degree,
                    });
                }
            }
            foreach (TraitDef trait in DefDatabase<TraitDef>.AllDefs)
            {
                if (!comp.Props.traitsToExclude.Contains(trait))
                {
                    for (int i = 0; i < trait.degreeDatas.Count; i++)
                    {
                        alltraits.Add(new TraitEntry
                        {
                            traitDef = trait,
                            degree = trait.degreeDatas[i].degree,
                        });
                    }
                }
            }
            skillsToSet = new List<SkillEntry>();
            foreach (SkillRecord skill in comp.pawn.skills.skills)
            {
                skillsToSet.Add(new SkillEntry
                {
                    skillDef = skill.def,
                    level = skill.Level,
                    passion = skill.passion
                });
            }

            backstoriesToSet = comp.pawn.story.AllBackstories.ToList();
            this.actionOnAccept = actionOnAccept;
        }

        private Vector2 firstColumnPos;
        private Vector2 secondColumnPos;
        private string buf1;
        public override void DoWindowContents(Rect inRect)
        {
            firstColumnPos.x = 0;
            firstColumnPos.y = 0;
            if (BrainwashSettings.modifyBackstoriesForBrainwashing)
            {
                Rect backstoriesTitle = new(firstColumnPos.x, firstColumnPos.y, 200, 32);
                Text.Font = GameFont.Medium;
                Widgets.Label(backstoriesTitle, "Brainwash_Backstories".Translate());
                Text.Font = GameFont.Small;
                firstColumnPos.y += 40;
                BackstoryDef backstoryToRemove = null;
                var backstoryCount = comp.pawn.story.AllBackstories.Count;
                for (int i = 0; i < backstoryCount; i++)
                {
                    BackstorySlot slot = i == 0 ? BackstorySlot.Childhood : BackstorySlot.Adulthood;
                    var backstory = backstoriesToSet.Count > i ? backstoriesToSet[i] : null;
                    List<BackstoryDef> allBackstories = DefDatabase<BackstoryDef>.AllDefs.Where(x => x.slot == slot && x != backstory)
                        .GroupBy(x => x.TitleCapFor(comp.pawn.gender)).Select(x => x.First()).ToList();
                    Rect buttonRect = DoButton(ref firstColumnPos, backstory?.TitleCapFor(comp.pawn.gender)
                        ?? "Brainwash_SelectBackstory".Translate(),
                        delegate
                        {
                            Find.WindowStack.Add(new Window_SelectItem<BackstoryDef>(allBackstories.ToList(), delegate (BackstoryDef selected)
                            {
                                if (slot == BackstorySlot.Childhood)
                                {
                                    backstoriesToSet[0] = selected;
                                }
                                else
                                {
                                    backstoriesToSet[1] = selected;
                                }
                            }, (BackstoryDef x) => 0, delegate (BackstoryDef x)
                            {
                                return x.TitleCapFor(comp.pawn.gender);
                            }));
                        });
                    if (backstory != null && slot == BackstorySlot.Adulthood)
                    {
                        Rect removeRect = new(buttonRect.xMax, buttonRect.y, 21f, 21f);
                        if (Widgets.ButtonImage(removeRect, TexButton.Delete))
                        {
                            backstoryToRemove = backstory;
                        }
                    }
                }


                if (backstoryToRemove != null)
                {
                    var index = backstoriesToSet.IndexOf(backstoryToRemove);
                    backstoriesToSet[index] = null;
                }
                firstColumnPos.y += 15;
            }

            Rect traitsTitle = new(firstColumnPos.x, firstColumnPos.y, 200, 32);
            Text.Font = GameFont.Medium;
            Widgets.Label(traitsTitle, "Traits".Translate());
            Text.Font = GameFont.Small;
            firstColumnPos.y += 40;
            TraitEntry toRemove = null;
            for (int i = 0; i < BrainwashSettings.traitCountToEdit; i++)
            {
                TraitEntry trait = traitsToSet.Count > i ? traitsToSet[i] : null;
                if (trait != null && comp.Props.traitsToExclude.Contains(trait.traitDef))
                {
                    GUI.color = Color.grey;
                    Rect buttonRect = new(firstColumnPos.x, firstColumnPos.y, 250, 24);
                    firstColumnPos.y += 24;
                    Widgets.ButtonText(buttonRect, trait?.GetLabel(comp.pawn), active: false);
                    GUI.color = Color.white;
                }
                else
                {
                    Rect buttonRect = DoButton(ref firstColumnPos, trait?.GetLabel(comp.pawn) ?? "Brainwash_SelectTrait".Translate(), delegate
                    {
                        Find.WindowStack.Add(new Window_SelectItem<TraitEntry>(alltraits.Where(x =>
                        !traitsToSet.Any(y => x.traitDef == y.traitDef || x.traitDef.ConflictsWith(y.traitDef))
                         && comp.pawn.story.traits.allTraits.Any(pawnTrait => pawnTrait.def == x.traitDef) is false).ToList(), delegate (TraitEntry selected)
                         {
                             if (trait != null)
                             {
                                 int index = traitsToSet.IndexOf(trait);
                                 traitsToSet[index] = selected;
                             }
                             else
                             {
                                 traitsToSet.Add(selected);
                             }
                         }, (TraitEntry x) => 0, delegate (TraitEntry x)
                         {
                             return x.GetLabel(comp.pawn);
                         }));
                    });
                    if (trait != null)
                    {
                        Rect removeRect = new(buttonRect.xMax, buttonRect.y, 21f, 21f);
                        if (Widgets.ButtonImage(removeRect, TexButton.Delete))
                        {
                            toRemove = trait;
                        }
                    }
                }
            }

            if (toRemove != null)
            {
                traitsToSet.Remove(toRemove);
            }

            firstColumnPos.y += 12;
            if (ModsConfig.IdeologyActive)
            {
                Rect ideologyTitle = new(firstColumnPos.x, firstColumnPos.y, 200, 32);
                Text.Font = GameFont.Medium;
                Widgets.Label(ideologyTitle, DefDatabase<ExpansionDef>.GetNamed("Ideology").LabelCap);
                Text.Font = GameFont.Small;
                firstColumnPos.y += 40;

                var reduceCertaintyRect = new Rect(ideologyTitle.x, ideologyTitle.yMax, 200, 24);
                Widgets.CheckboxLabeled(reduceCertaintyRect, "Brainwash_ReduceCertainty".Translate(), ref reduceCertainty);
                firstColumnPos.y += 24;
            }

            secondColumnPos.x = 300;
            secondColumnPos.y = 0;

            Rect skillsTitle = new(secondColumnPos.x, secondColumnPos.y, 150, 32);
            Text.Font = GameFont.Medium;
            Widgets.Label(skillsTitle, "Skills".Translate());
            Rect passionsTitle = new(skillsTitle.xMax + 15, secondColumnPos.y, 100, 32);
            Widgets.Label(passionsTitle, "Brainwash_Passions".Translate());
            Text.Font = GameFont.Small;
            secondColumnPos.y += 32;

            foreach (SkillEntry skillToSet in skillsToSet)
            {
                Rect labelRect = new(secondColumnPos.x, secondColumnPos.y, 120, 24);
                Widgets.Label(labelRect, skillToSet.skillDef.LabelCap);
                Rect inputRect = new(labelRect.xMax, labelRect.y, 30, 24);
                buf1 = skillToSet.level.ToString();
                if (BrainwashSettings.modifySkillsForBrainwashing)
                {
                    Widgets.TextFieldNumeric<int>(inputRect, ref skillToSet.level, ref buf1, 0, 20);
                }
                Rect passionRect = new(inputRect.xMax + 15, inputRect.y + 1, 22, 22);
                Widgets.DrawBox(passionRect);
                if ((int)skillToSet.passion > 0)
                {
                    Texture2D image = (skillToSet.passion == Passion.Major) ? SkillUI.PassionMajorIcon : SkillUI.PassionMinorIcon;
                    GUI.DrawTexture(passionRect, image);
                }
                if (Widgets.ButtonInvisible(passionRect))
                {
                    skillToSet.passion++;
                    if ((int)skillToSet.passion >= 3)
                    {
                        skillToSet.passion = 0;
                    }
                }
                secondColumnPos.y += 24;
            }

            Rect cancelButton = new(inRect.x, inRect.height - 32, 150, 32);
            if (Widgets.ButtonText(cancelButton, "Cancel".Translate()))
            {
                Close();
            }
            Rect startBrainwash = new(inRect.width - 150, inRect.height - 32, 150, 32);
            if (Widgets.ButtonText(startBrainwash, "Ok".Translate()))
            {
                comp.traitsToSet = traitsToSet;
                comp.skillsToSet = skillsToSet;
                comp.backstoriesToSet = backstoriesToSet;
                comp.reduceCertainty = reduceCertainty;
                actionOnAccept();
                Close();
            }
        }

        private static Rect DoButton(ref Vector2 pos, string label, Action action)
        {
            Rect buttonRect = new(pos.x, pos.y, 250, 24);
            pos.y += 24;
            if (Widgets.ButtonText(buttonRect, label))
            {
                UI.UnfocusCurrentControl();
                action();
            }
            return buttonRect;
        }
    }
}

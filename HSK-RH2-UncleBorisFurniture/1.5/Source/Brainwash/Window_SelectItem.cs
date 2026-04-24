using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Brainwash
{
    public class Window_SelectItem<T> : Window
    {
        private Vector2 scrollPosition;
        public override Vector2 InitialSize => new(620f, 500f);

        public List<T> allItems;
        public Action<T> actionOnSelect;
        public Func<T, int> ordering;
        public Func<T, string> labelGetter;
        public Window_SelectItem(List<T> items, Action<T> actionOnSelect, Func<T, int> ordering = null, Func<T, string> labelGetter = null)
        {
            doCloseButton = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = false;
            allItems = items;
            this.actionOnSelect = actionOnSelect;
            this.ordering = ordering;
            this.labelGetter = labelGetter;
        }

        private string searchKey;
        public string GetLabel(T item)
        {
            return labelGetter != null ? labelGetter(item) : item is Def def ? def.label : "";
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;

            Text.Anchor = TextAnchor.MiddleLeft;
            Rect searchLabel = new(inRect.x, inRect.y, 60, 24);
            Widgets.Label(searchLabel, "Brainwash_Search".Translate());
            Rect searchRect = new(searchLabel.xMax + 5, searchLabel.y, 200, 24f);
            searchKey = Widgets.TextField(searchRect, searchKey);
            Text.Anchor = TextAnchor.UpperLeft;

            Rect outRect = new(inRect)
            {
                y = searchRect.yMax + 5
            };
            outRect.yMax -= 70f;
            outRect.width -= 16f;

            List<T> items = searchKey.NullOrEmpty() ? allItems : allItems.Where(x => GetLabel(x).ToLower().Contains(searchKey.ToLower())).ToList();

            Rect viewRect = new(0f, 0f, outRect.width - 16f, items.Count() * 35f);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            try
            {
                float num = 0f;
                if (ordering != null)
                {
                    items = items.OrderBy(x => ordering(x)).ThenBy(x => GetLabel(x)).ToList();
                }
                foreach (T item in items)
                {
                    Rect iconRect = new(0f, num, 24, 32);
                    if (item is Def def)
                    {
                        Widgets.InfoCardButton(iconRect, def);
                    }
                    if (item is ThingDef thingDef2)
                    {
                        iconRect.x += 24;
                        Widgets.ThingIcon(iconRect, thingDef2);
                    }
                    Rect rect = new(iconRect.xMax + 5, num, viewRect.width * 0.7f, 32f);
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(rect, GetLabel(item));
                    Text.Anchor = TextAnchor.UpperLeft;
                    rect.x = rect.xMax + 10;
                    rect.width = 100;
                    if (Widgets.ButtonText(rect, "Brainwash_Select".Translate()))
                    {
                        actionOnSelect(item);
                        SoundDefOf.Click.PlayOneShotOnCamera();
                        Close();
                    }
                    num += 35f;
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }
    }
}

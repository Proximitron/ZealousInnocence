using HarmonyLib;
using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ZealousInnocence
{
    public static class Patch_HealthCardUtility_DrawLeftRow
    {
        // two-up rolling state
        private static bool sExpectRightCell = false;
        private static float sPairStartY = 0f;
        private static float sLeftCellHeight = 0f;

        public static bool Prefix(Rect rect, ref float curY, string leftLabel, string rightLabel, Color rightLabelColor, TipSignal tipSignal)
        {
            var s = LoadedModManager.GetMod<ZealousInnocence>().GetSettings<ZealousInnocenceSettings>();
            float ui = Mathf.Max(1f, Prefs.UIScale);
            float S(float px) => Mathf.Round(px * ui);

            float pairGap = S(s.pairGap);
            float minTwoUpCellWidth = S(s.minTwoUpCellWidth);
            float fullWidthHeightGate = S(s.fullWidthHeightGate);

            bool allowTwoUp = s.enableTwoUp && rect.width >= (minTwoUpCellWidth * 2f + pairGap);

            if (!allowTwoUp)
            {
                float usedH = DrawOneCell(new Rect(rect.x, curY, rect.width, 99999f), leftLabel, rightLabel, rightLabelColor, tipSignal, ui, s);
                curY += usedH + S(s.extraRowSpacing);
                sExpectRightCell = false;
                sLeftCellHeight = 0f;
                return false;
            }

            float halfW = (rect.width - pairGap) * 0.5f;

            if (!sExpectRightCell)
            {
                float predictedLeftH = PredictHeight(halfW, leftLabel, rightLabel, ui, s);
                if (predictedLeftH > fullWidthHeightGate)
                {
                    float usedH = DrawOneCell(new Rect(rect.x, curY, rect.width, 99999f), leftLabel, rightLabel, rightLabelColor, tipSignal, ui, s);
                    curY += usedH + S(s.extraRowSpacing);
                    return false;
                }

                Rect cellRect = new Rect(rect.x, curY, halfW, 99999f);
                float used = DrawOneCell(cellRect, leftLabel, rightLabel, rightLabelColor, tipSignal, ui, s);

                sPairStartY = curY;
                sLeftCellHeight = used;
                curY += used; // no extraRowSpacing yet; we add it when the pair completes

                sExpectRightCell = true;
                return false;
            }
            else
            {
                float predictedRightH = PredictHeight(halfW, leftLabel, rightLabel, ui, s);
                if (predictedRightH > fullWidthHeightGate)
                {
                    float usedH = DrawOneCell(new Rect(rect.x, curY, rect.width, 99999f), leftLabel, rightLabel, rightLabelColor, tipSignal, ui, s);
                    curY += usedH + S(s.extraRowSpacing);
                    sExpectRightCell = false;
                    sLeftCellHeight = 0f;
                    return false;
                }

                Rect cellRect = new Rect(rect.x + halfW + pairGap, sPairStartY, halfW, 99999f);
                float rightH = DrawOneCell(cellRect, leftLabel, rightLabel, rightLabelColor, tipSignal, ui, s);

                curY = sPairStartY + Mathf.Max(sLeftCellHeight, rightH) + S(s.extraRowSpacing);
                sExpectRightCell = false;
                sLeftCellHeight = 0f;
                return false;
            }
        }

        // single-cell renderer (one column unless too tall → two internal columns)
        private static float DrawOneCell(Rect cellRect, string leftLabel, string rightLabel, Color rightLabelColor, TipSignal tipSignal, float ui, ZealousInnocenceSettings s)
        {
            float padX = Mathf.Round(s.paddingX * ui);
            float rowMinH = Mathf.Round(s.rowMinHeight * ui);
            float gapInside = Mathf.Round(s.gapInside * ui);
            float twoColTriggerH = Mathf.Round(s.twoColTriggerH * ui);
            float twoColGap = Mathf.Round(s.twoColGap * ui);
            float twoColMinW = Mathf.Round(s.twoColMinWidth * ui);
            float maxLeftFrac = Mathf.Clamp01(s.maxLeftFrac);
            var highlightColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);

            if (s.snapToUIScale) cellRect = SnapForUIScale(cellRect);

            float rowH = rowMinH;

            Rect hoverRect = new Rect(cellRect.x + padX, cellRect.y, cellRect.width - (padX * 2f) - 10f, rowMinH);
            Rect fullRowRect = new Rect(cellRect.x, cellRect.y, cellRect.width, rowMinH);

            if (Mouse.IsOver(hoverRect))
            {
                using (new TextBlock(highlightColor))
                    GUI.DrawTexture(s.snapToUIScale ? SnapForUIScale(hoverRect) : hoverRect, TexUI.HighlightTex);
            }

            var oldAnchor = Text.Anchor;
            var oldFont = Text.Font;
            var oldWrap = Text.WordWrap;
            var oldColor = GUI.color;

            try
            {
                Text.WordWrap = true;
                Text.Font = GameFont.Small;

                float innerX = cellRect.x + padX;
                float innerWidth = cellRect.width - (padX * 2f) - 10f;

                float maxLeftW = innerWidth * maxLeftFrac;
                Vector2 leftSize = Text.CalcSize(leftLabel);
                float leftW = Mathf.Min(Mathf.Ceil(leftSize.x), maxLeftW);
                float rightW = Mathf.Max(0f, innerWidth - leftW - gapInside);

                Rect leftRect = new Rect(innerX, cellRect.y, leftW, rowMinH);
                Rect rightRect = new Rect(leftRect.xMax + gapInside, cellRect.y, rightW, rowMinH);

                if (s.snapToUIScale)
                {
                    leftRect = SnapForUIScale(leftRect);
                    rightRect = SnapForUIScale(rightRect);
                }

                float rightH_Small = Mathf.Ceil(Text.CalcHeight(rightLabel, rightRect.width));
                bool useTiny = false;
                if (s.tinyFontFallback)
                {
                    Text.Font = GameFont.Tiny;
                    float rightH_Tiny = Mathf.Ceil(Text.CalcHeight(rightLabel, rightRect.width));
                    useTiny = rightH_Tiny + 2f < rightH_Small;
                }
                Text.Font = useTiny ? GameFont.Tiny : GameFont.Small;

                float oneColH = Mathf.Ceil(Text.CalcHeight(rightLabel, rightRect.width));
                bool allowTwoCol = s.enableTwoColRight && rightRect.width >= twoColMinW;
                bool toTwoCols = allowTwoCol && oneColH > twoColTriggerH;

                // Compact one-line right value?
                bool fitsOneLine =
                    !toTwoCols &&
                    (useTiny ? (Mathf.Ceil(Text.CalcHeight(rightLabel, rightRect.width)) <= rowMinH + 0.1f) : (rightH_Small <= rowMinH + 0.1f)) &&
                    Text.CalcSize(rightLabel).x <= rightRect.width;

                if (fitsOneLine)
                {
                    rowH = rowMinH;

                    leftRect.height = rowH;
                    rightRect.height = rowH;
                    hoverRect.height = rowH;
                    fullRowRect.height = rowH;

                    if (s.snapToUIScale)
                    {
                        leftRect = SnapForUIScale(leftRect);
                        rightRect = SnapForUIScale(rightRect);
                        hoverRect = SnapForUIScale(hoverRect);
                        fullRowRect = SnapForUIScale(fullRowRect);
                    }

                    if (Mouse.IsOver(hoverRect))
                    {
                        using (new TextBlock(highlightColor))
                            GUI.DrawTexture(hoverRect, TexUI.HighlightTex);
                    }

                    Text.Anchor = TextAnchor.MiddleLeft;
                    Text.Font = GameFont.Small;
                    Widgets.Label(leftRect, leftLabel);

                    GUI.color = rightLabelColor;
                    Text.WordWrap = false;
                    Text.Anchor = TextAnchor.MiddleRight;
                    Text.Font = useTiny ? GameFont.Tiny : GameFont.Small;
                    Widgets.Label(rightRect, rightLabel);
                }
                else if (!toTwoCols)
                {
                    rowH = Mathf.Max(rowMinH, oneColH);

                    leftRect.height = rowH;
                    rightRect.height = rowH;
                    hoverRect.height = rowH;
                    fullRowRect.height = rowH;

                    if (s.snapToUIScale)
                    {
                        leftRect = SnapForUIScale(leftRect);
                        rightRect = SnapForUIScale(rightRect);
                        hoverRect = SnapForUIScale(hoverRect);
                        fullRowRect = SnapForUIScale(fullRowRect);
                    }

                    if (Mouse.IsOver(hoverRect))
                    {
                        using (new TextBlock(highlightColor))
                            GUI.DrawTexture(hoverRect, TexUI.HighlightTex);
                    }

                    Text.Anchor = TextAnchor.MiddleLeft;
                    Text.Font = GameFont.Small;
                    Widgets.Label(leftRect, leftLabel);

                    GUI.color = rightLabelColor;
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.Font = useTiny ? GameFont.Tiny : GameFont.Small;
                    Widgets.Label(rightRect, rightLabel);
                }
                else
                {
                    // 2 internal columns in right area
                    string[] parts =
                        rightLabel.IndexOf(',') >= 0 ? rightLabel.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) :
                        rightLabel.IndexOf(';') >= 0 ? rightLabel.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) :
                        rightLabel.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

                    int half = Mathf.CeilToInt(parts.Length / 2f);
                    string col1Text = string.Join(", ", parts, 0, Mathf.Min(half, parts.Length)).Trim();
                    string col2Text = parts.Length > half ? string.Join(", ", parts, half, parts.Length - half).Trim() : "";

                    float colW = (rightRect.width - twoColGap) * 0.5f;
                    float col1H = Mathf.Ceil(Text.CalcHeight(col1Text, colW));
                    float col2H = Mathf.Ceil(Text.CalcHeight(col2Text, colW));
                    rowH = Mathf.Max(rowMinH, Mathf.Max(col1H, col2H));

                    leftRect.height = rowH;
                    rightRect.height = rowH;
                    hoverRect.height = rowH;
                    fullRowRect.height = rowH;

                    if (s.snapToUIScale)
                    {
                        leftRect = SnapForUIScale(leftRect);
                        rightRect = SnapForUIScale(rightRect);
                        hoverRect = SnapForUIScale(hoverRect);
                        fullRowRect = SnapForUIScale(fullRowRect);
                    }

                    if (Mouse.IsOver(hoverRect))
                    {
                        using (new TextBlock(highlightColor))
                            GUI.DrawTexture(hoverRect, TexUI.HighlightTex);
                    }

                    Text.Anchor = TextAnchor.MiddleLeft;
                    Text.Font = GameFont.Small;
                    Widgets.Label(leftRect, leftLabel);

                    GUI.color = rightLabelColor;
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.Font = useTiny ? GameFont.Tiny : GameFont.Small;

                    Rect col1 = new Rect(rightRect.x, rightRect.y, colW, rowH);
                    Rect col2 = new Rect(col1.xMax + twoColGap, rightRect.y, colW, rowH);
                    if (s.snapToUIScale) { col1 = SnapForUIScale(col1); col2 = SnapForUIScale(col2); }
                    Widgets.Label(col1, col1Text);
                    if (!string.IsNullOrEmpty(col2Text))
                        Widgets.Label(col2, col2Text);
                }

                if (s.debuggingRects)
                {
                    var old = GUI.color;
                    GUI.color = new Color(1f, 0.5f, 0f, 0.6f);
                    Widgets.DrawBox(leftRect);
                    GUI.color = new Color(0f, 0.7f, 1f, 0.6f);
                    Widgets.DrawBox(rightRect);
                    GUI.color = old;
                }

                if (Mouse.IsOver(fullRowRect))
                    TooltipHandler.TipRegion(fullRowRect, tipSignal);

                return rowH;
            }
            finally
            {
                GUI.color = oldColor;
                Text.Anchor = oldAnchor;
                Text.Font = oldFont;
                Text.WordWrap = oldWrap;
            }
        }

        private static float PredictHeight(float cellWidth, string leftLabel, string rightLabel, float ui, ZealousInnocenceSettings s)
        {
            float padX = Mathf.Round(s.paddingX * ui);
            float rowMinH = Mathf.Round(s.rowMinHeight * ui);
            float gapInside = Mathf.Round(s.gapInside * ui);
            float twoColTriggerH = Mathf.Round(s.twoColTriggerH * ui);
            float twoColGap = Mathf.Round(s.twoColGap * ui);
            float twoColMinW = Mathf.Round(s.twoColMinWidth * ui);
            float maxLeftFrac = Mathf.Clamp01(s.maxLeftFrac);

            Rect r = new Rect(0, 0, cellWidth, rowMinH);
            if (s.snapToUIScale) r = SnapForUIScale(r);

            float innerWidth = r.width - (padX * 2f) - 10f;

            var oldFont = Text.Font;
            var oldWrap = Text.WordWrap;
            Text.WordWrap = true;
            Text.Font = GameFont.Small;

            float maxLeftW = innerWidth * maxLeftFrac;
            float leftW = Mathf.Min(Mathf.Ceil(Text.CalcSize(leftLabel).x), maxLeftW);
            float rightW = Mathf.Max(0f, innerWidth - leftW - gapInside);

            float hSmall = Mathf.Ceil(Text.CalcHeight(rightLabel, rightW));
            bool useTiny = false;
            if (s.tinyFontFallback)
            {
                Text.Font = GameFont.Tiny;
                float hTiny = Mathf.Ceil(Text.CalcHeight(rightLabel, rightW));
                useTiny = hTiny + 2f < hSmall;
            }
            Text.Font = useTiny ? GameFont.Tiny : GameFont.Small;

            float oneColH = Mathf.Ceil(Text.CalcHeight(rightLabel, rightW));
            bool twoCols = s.enableTwoColRight && oneColH > twoColTriggerH && rightW >= twoColMinW;

            float finalH;
            if (!twoCols)
            {
                finalH = Mathf.Max(rowMinH, oneColH);
            }
            else
            {
                // rough split for gating
                var parts =
                    rightLabel.IndexOf(',') >= 0 ? rightLabel.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) :
                    rightLabel.IndexOf(';') >= 0 ? rightLabel.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) :
                    rightLabel.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                int half = Mathf.CeilToInt(parts.Length / 2f);
                string c1 = string.Join(", ", parts, 0, Math.Min(half, parts.Length)).Trim();
                string c2 = parts.Length > half ? string.Join(", ", parts, half, parts.Length - half).Trim() : "";
                float colW = (rightW - twoColGap) * 0.5f;
                float c1H = Mathf.Ceil(Text.CalcHeight(c1, colW));
                float c2H = Mathf.Ceil(Text.CalcHeight(c2, colW));
                finalH = Mathf.Max(rowMinH, Mathf.Max(c1H, c2H));
            }

            Text.Font = oldFont;
            Text.WordWrap = oldWrap;
            return finalH;
        }

        // snap helper (vanilla-alike)
        private static Rect SnapForUIScale(Rect rect)
        {
            if (Prefs.UIScale <= 1f) return rect;
            float half = Prefs.UIScale / 2f;
            if (Math.Abs(half - Mathf.Floor(half)) <= 1E-45f) return rect;

            Rect r = rect;
            r.xMin = UIScaling.AdjustCoordToUIScalingFloor(rect.xMin);
            r.yMin = UIScaling.AdjustCoordToUIScalingFloor(rect.yMin);
            r.xMax = UIScaling.AdjustCoordToUIScalingCeil(rect.xMax + 1E-05f);
            r.yMax = UIScaling.AdjustCoordToUIScalingCeil(rect.yMax + 1E-05f);
            return r;
        }
    }
}

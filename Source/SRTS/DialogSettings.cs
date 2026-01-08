using System;
using UnityEngine;
using Verse;

namespace SRTS;

[StaticConstructorOnStartup]
public static class DialogSettings
{
    public static Color textColor = Color.white;
    public static Color highlightColor = new(0f, 0f, 0f, 0.25f);

    public static readonly Texture2D ButtonBGAtlas = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBG");

    public static readonly Texture2D ButtonBGAtlasMouseover =
        ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGMouseover");

    public static readonly Texture2D ButtonBGAtlasClick = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGClick");

    public static readonly Texture2D LightHighlight =
        SolidColorMaterials.NewSolidColorTexture(new Color(1f, 1f, 1f, 0.04f));

    public static void Draw_Label(Rect rect, string label, Color highlight, Color textColor,
        GameFont fontSize = GameFont.Medium, TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        var textSize = Text.Font;
        Text.Font = fontSize;
        GUI.color = highlight;
        GUI.DrawTexture(rect, BaseContent.WhiteTex);
        GUI.color = textColor;

        var anchorTmp = Text.Anchor;
        Text.Anchor = anchor;
        Widgets.Label(rect, label);
        Text.Font = textSize;
        Text.Anchor = anchorTmp;
    }

    extension(Listing_Standard lister)
    {
        public void Settings_IntegerBox(string text, ref int value, float labelLength,
            float padding, int min = int.MinValue, int max = int.MaxValue)
        {
            lister.Gap();
            var rect = lister.GetRect(Text.LineHeight);

            var rectLeft = new Rect(rect.x, rect.y, labelLength, rect.height);
            var rectRight = new Rect(rect.x + labelLength + padding, rect.y, rect.width - labelLength - padding,
                rect.height);

            var color = GUI.color;
            Widgets.Label(rectLeft, text);

            var align = Text.CurTextFieldStyle.alignment;
            Text.CurTextFieldStyle.alignment = TextAnchor.MiddleLeft;
            var buffer = value.ToString();
            Widgets.TextFieldNumeric(rectRight, ref value, ref buffer, min, max);

            Text.CurTextFieldStyle.alignment = align;
            GUI.color = color;
        }

        public void Settings_Numericbox(string text, ref float value,
            float labelLength, float padding, float min = -1E+09f, float max = 1E+09f)
        {
            lister.Gap();
            var rect = lister.GetRect(Text.LineHeight);

            var rectLeft = new Rect(rect.x, rect.y, labelLength, rect.height);
            var rectRight = new Rect(rect.x + labelLength + padding, rect.y, rect.width - labelLength - padding,
                rect.height);

            var color = GUI.color;
            Widgets.Label(rectLeft, text);

            var align = Text.CurTextFieldStyle.alignment;
            Text.CurTextFieldStyle.alignment = TextAnchor.MiddleLeft;
            var buffer = value.ToString();
            Widgets.TextFieldNumeric(rectRight, ref value, ref buffer, min, max);

            Text.CurTextFieldStyle.alignment = align;
            GUI.color = color;
        }

        public void Settings_SliderLabeled(string label, string endSymbol,
            ref float value, float min, float max, float multiplier = 1f, int decimalPlaces = 2, float endValue = -1f,
            string endValueDisplay = "")
        {
            lister.Gap();
            var rect = lister.GetRect(24f);
            var format = string.Format("{0}" + endSymbol, Math.Round(value * multiplier, decimalPlaces));
            if (!endValueDisplay.NullOrEmpty() && endValue > 0)
            {
                if (value >= endValue)
                {
                    format = endValueDisplay;
                }
            }

            value = Widgets.HorizontalSlider(rect, value, min, max, false, null, label, format);
            if (endValue > 0 && value >= max)
            {
                value = endValue;
            }
        }

        public void Settings_SliderLabeled(string label, string endSymbol,
            ref int value, int min, int max, int endValue = -1, string endValueDisplay = "")
        {
            lister.Gap();
            var rect = lister.GetRect(24f);
            var format = string.Format("{0}" + endSymbol, value);
            if (!endValueDisplay.NullOrEmpty() && endValue > 0)
            {
                if (value == endValue)
                {
                    format = endValueDisplay;
                }
            }

            value = (int)Widgets.HorizontalSlider(rect, value, min, max, false, null, label, format);
            if (endValue > 0 && value == max)
            {
                value = endValue;
            }
        }

        public void Settings_Header(string header, Color highlight,
            GameFont fontSize = GameFont.Medium, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var textSize = Text.Font;
            Text.Font = fontSize;

            var rect = lister.GetRect(Text.CalcHeight(header, lister.ColumnWidth));
            GUI.color = highlight;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = textColor;

            var anchorTmp = Text.Anchor;
            Text.Anchor = anchor;
            Widgets.Label(rect, header);
            Text.Font = textSize;
            Text.Anchor = anchorTmp;
            lister.Gap();
        }

        ///WORK IN PROGRESS
        public void Settings_BoxLabeled(Rect rect, string header, Color highlight,
            GameFont fontSize = GameFont.Small, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var textSize = Text.Font;
            Text.Font = fontSize;

            GUI.color = highlight;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = textColor;

            var anchorTmp = Text.Anchor;
            Text.Anchor = anchor;
            var labelRect = lister.GetRect(Text.CalcHeight(header, lister.ColumnWidth));
            Widgets.Label(labelRect, header);
            Text.Font = textSize;
            Text.Anchor = anchorTmp;
            lister.Gap();
        }

        public bool Settings_Button(string label, Rect rect, Color customColor,
            bool background = true, bool active = true)
        {
            var anchor = Text.Anchor;
            var color = GUI.color;

            if (background)
            {
                var atlas = ButtonBGAtlas;
                if (Mouse.IsOver(rect))
                {
                    atlas = ButtonBGAtlasMouseover;
                    if (Input.GetMouseButton(0))
                    {
                        atlas = ButtonBGAtlasClick;
                    }
                }

                Widgets.DrawAtlas(rect, atlas);
            }
            else
            {
                GUI.color = customColor;
                if (Mouse.IsOver(rect))
                {
                    GUI.color = Color.cyan;
                }
            }

            Text.Anchor = background ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;

            var wordWrap = Text.WordWrap;
            if (rect.height < Text.LineHeight * 2f)
            {
                Text.WordWrap = false;
            }

            Widgets.Label(rect, label);
            Text.Anchor = anchor;
            GUI.color = color;
            Text.WordWrap = wordWrap;
            lister.Gap(2f);
            return Widgets.ButtonInvisible(rect, false);
        }

        public bool Settings_ButtonLabeled(string header, string buttonLabel,
            Color highlightColor, float buttonWidth = 30f, bool background = true, bool active = true)
        {
            var anchor = Text.Anchor;
            var color = GUI.color;
            var rect = lister.GetRect(20f);
            var buttonRect = new Rect(rect.width - buttonWidth, rect.y, buttonWidth, rect.height);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(rect, header);

            if (background)
            {
                var atlas = ButtonBGAtlas;
                if (Mouse.IsOver(buttonRect))
                {
                    atlas = ButtonBGAtlasMouseover;
                    if (Input.GetMouseButton(0))
                    {
                        atlas = ButtonBGAtlasClick;
                    }
                }

                Widgets.DrawAtlas(buttonRect, atlas);
            }
            else
            {
                GUI.color = Color.white;
                if (Mouse.IsOver(buttonRect))
                {
                    GUI.color = highlightColor;
                }
            }

            Text.Anchor = background ? TextAnchor.MiddleCenter : TextAnchor.MiddleRight;

            var wordWrap = Text.WordWrap;
            if (buttonRect.height < Text.LineHeight * 2f)
            {
                Text.WordWrap = false;
            }

            Widgets.Label(buttonRect, buttonLabel);
            Text.Anchor = anchor;
            GUI.color = color;
            Text.WordWrap = wordWrap;
            lister.Gap(2f);
            return Widgets.ButtonInvisible(buttonRect, false);
        }
    }
}
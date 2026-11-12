using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Lab.Core.EditorTools
{
    /// <summary>
    /// Builds a screen-space panel of axis sliders in the open scene, bottom left, and wires an
    /// <see cref="AxisPanel"/> to the target. Uses Unity's UI and its built-in font, so it needs no imported assets.
    /// </summary>
    public static class AxisPanelBuilder
    {
        // The Game view is laid out at this height, like StatusLine, and scales with the window.
        static readonly Vector2 ReferenceResolution = new Vector2(1280f, 720f);
        const float Margin = 12f;
        const float Width = 410f;
        const float Padding = 10f;
        const float TitleHeight = 28f;
        const float RowHeight = 28f;
        const float ButtonHeight = 28f;
        const float HelpHeight = 34f;
        const int FontSize = 15;

        static readonly Color PanelColour = new Color(0.07f, 0.09f, 0.11f, 0.88f);
        static readonly Color TitleColour = new Color(0.55f, 0.85f, 0.9f);
        static readonly Color HelpColour = new Color(0.7f, 0.74f, 0.78f);
        static readonly Color TrackColour = new Color(0.22f, 0.25f, 0.29f);
        static readonly Color FillColour = new Color(0.33f, 0.62f, 0.68f);

        public static AxisPanel Build(MonoBehaviour target, string title, string help)
        {
            var adjustable = target as IAxisAdjustable;
            if (adjustable == null)
            {
                Debug.LogError($"{target.name} doesn't implement IAxisAdjustable, so no panel was built.");
                return null;
            }

            EnsureEventSystem();
            var canvas = new GameObject("Axis Panel Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            // Whole-pixel placement keeps the built-in font crisp.
            canvas.GetComponent<Canvas>().pixelPerfect = true;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 1f;

            var resources = Resources();
            var count = adjustable.AxisCount;
            var height = Padding * 2f + TitleHeight + RowHeight * count + 6f + ButtonHeight + HelpHeight;

            var panel = DefaultControls.CreatePanel(resources);
            panel.name = "Axis Panel";
            panel.transform.SetParent(canvas.transform, false);
            panel.GetComponent<Image>().color = PanelColour;
            Place(panel, new Vector2(0f, 0f), new Vector2(Margin, Margin), new Vector2(Width, height), new Vector2(0f, 0f));

            var top = -Padding;
            var heading = Label(panel.transform, resources, "Title", title, new Vector2(Padding + 2f, top), Width - Padding * 2f, TitleHeight, TextAnchor.MiddleLeft, TitleColour);
            heading.fontStyle = FontStyle.Bold;
            heading.fontSize = FontSize + 1;
            top -= TitleHeight;

            var rows = new AxisPanel.Row[count];
            for (var i = 0; i < count; i++)
            {
                rows[i] = MakeRow(panel.transform, resources, adjustable, i, top);
                top -= RowHeight;
            }

            top -= 6f;
            var button = DefaultControls.CreateButton(resources);
            button.name = "Reset";
            button.transform.SetParent(panel.transform, false);
            PlaceTopLeft(button, new Vector2(Padding, top), new Vector2(110f, ButtonHeight - 2f));
            var buttonText = button.GetComponentInChildren<Text>();
            buttonText.text = "Reset";
            Style(buttonText, FontSize, Color.black);
            top -= ButtonHeight;

            var helpText = Label(panel.transform, resources, "Help", help, new Vector2(Padding + 2f, top - 2f), Width - Padding * 2f, HelpHeight, TextAnchor.UpperLeft, HelpColour);
            helpText.fontSize = FontSize - 2;
            helpText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var axisPanel = panel.AddComponent<AxisPanel>();
            var wiring = new SerializedObject(axisPanel);
            wiring.FindProperty("target").objectReferenceValue = target;
            wiring.FindProperty("resetButton").objectReferenceValue = button.GetComponent<Button>();
            var rowsProperty = wiring.FindProperty("rows");
            rowsProperty.arraySize = count;
            for (var i = 0; i < count; i++)
            {
                var row = rowsProperty.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("slider").objectReferenceValue = rows[i].slider;
                row.FindPropertyRelative("label").objectReferenceValue = rows[i].label;
                row.FindPropertyRelative("value").objectReferenceValue = rows[i].value;
                row.FindPropertyRelative("background").objectReferenceValue = rows[i].background;
            }
            wiring.ApplyModifiedPropertiesWithoutUndo();
            return axisPanel;
        }

        static AxisPanel.Row MakeRow(Transform panel, DefaultControls.Resources resources, IAxisAdjustable target, int axis, float top)
        {
            var background = new GameObject($"Row {axis + 1}", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(panel, false);
            PlaceTopLeft(background, new Vector2(4f, top), new Vector2(Width - 8f, RowHeight - 2f));
            var image = background.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = false;

            var label = Label(background.transform, resources, "Label", target.AxisLabel(axis), new Vector2(8f, 0f), 128f, RowHeight - 2f, TextAnchor.MiddleLeft, Color.white);

            var sliderObject = DefaultControls.CreateSlider(resources);
            sliderObject.name = "Slider";
            sliderObject.transform.SetParent(background.transform, false);
            PlaceTopLeft(sliderObject, new Vector2(140f, -5f), new Vector2(172f, 16f));
            var slider = sliderObject.GetComponent<Slider>();
            slider.wholeNumbers = false;
            slider.minValue = target.AxisMin(axis);
            slider.maxValue = target.AxisMax(axis);
            slider.SetValueWithoutNotify(target.GetAxis(axis));
            sliderObject.transform.Find("Background").GetComponent<Image>().color = TrackColour;
            sliderObject.transform.Find("Fill Area/Fill").GetComponent<Image>().color = FillColour;

            var value = Label(background.transform, resources, "Value", AxisFormat.Readout(target.GetAxis(axis), target.AxisUnit(axis)),
                new Vector2(318f, 0f), 80f, RowHeight - 2f, TextAnchor.MiddleRight, TitleColour);

            return new AxisPanel.Row { slider = slider, label = label, value = value, background = image };
        }

        static Text Label(Transform parent, DefaultControls.Resources resources, string name, string content, Vector2 topLeft, float width, float height, TextAnchor anchor, Color colour)
        {
            var go = DefaultControls.CreateText(resources);
            go.name = name;
            go.transform.SetParent(parent, false);
            PlaceTopLeft(go, topLeft, new Vector2(width, height));
            var text = go.GetComponent<Text>();
            text.text = content;
            text.alignment = anchor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            Style(text, FontSize, colour);
            return text;
        }

        static void Style(Text text, int size, Color colour)
        {
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = colour;
        }

        static void PlaceTopLeft(GameObject go, Vector2 topLeft, Vector2 size) =>
            Place(go, new Vector2(0f, 1f), topLeft, size, new Vector2(0f, 1f));

        static void Place(GameObject go, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        // The project uses the new Input System only, so the UI needs its input module, not the legacy one.
        static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        static DefaultControls.Resources Resources() => new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
        };
    }
}

using System.Collections.Generic;
using System.Globalization;
using Lab.W01.VariableScrew;
using UnityEngine;
using UnityEngine.UI;

namespace Lab.W01.SizingStation
{
    /// <summary>
    /// The panel beside the sizing station: one coloured chip per diameter in the catalog, and a
    /// length slider with a tick at every length the chosen diameter ships in. It builds itself from
    /// the catalog when the scene starts, so a catalog with more or fewer sizes needs no new layout.
    /// It only calls the <see cref="ScrewSizer"/>; the sizer decides what is a real size.
    /// </summary>
    public class SizePanel : MonoBehaviour
    {
        static readonly Vector2 ReferenceResolution = new Vector2(1280f, 720f);
        const float Margin = 12f;
        const float Width = 452f;
        const float Height = 214f;
        const float Padding = 14f;
        const float ControlsLeft = 96f;
        const float ChipHeight = 30f;
        const float ChipGap = 4f;
        const float MaxChipWidth = 46f;
        const float SliderWidth = 268f;
        const float HandleWidth = 14f;
        const int MostTickLabels = 12;

        static readonly Color PanelColour = new Color(0.05f, 0.07f, 0.09f, 0.9f);
        static readonly Color TitleColour = new Color(0.55f, 0.85f, 0.9f);
        static readonly Color QuietColour = new Color(0.62f, 0.66f, 0.7f);
        static readonly Color TrackColour = new Color(0.22f, 0.25f, 0.29f);
        static readonly Color FillColour = new Color(0.33f, 0.62f, 0.68f);
        static readonly Color HandleColour = new Color(0.93f, 0.95f, 0.97f);
        static readonly Color DepthColour = new Color(1f, 0.78f, 0.4f);
        static readonly Color DarkText = new Color(0.08f, 0.09f, 0.1f);
        const float UnpickedChip = 0.5f;

        [SerializeField] ScrewSizer sizer;
        [Tooltip("The measured depth to size against, in millimetres. Below zero hides the line.")]
        [SerializeField] float depthMm = -1f;

        readonly List<Button> _chips = new List<Button>();
        readonly List<GameObject> _ticks = new List<GameObject>();
        Font _font;
        RectTransform _panel;
        RectTransform _chipRow;
        RectTransform _tickRow;
        Slider _slider;
        Text _maker;
        Text _lengthValue;
        Text _size;
        Text _depth;
        ImplantCatalog _shownCatalog;
        ScrewSpec _shownSpec;
        ScrewSizer _connected;

        public ScrewSizer Sizer => sizer;
        public float DepthMm => depthMm;
        public int ChipCount => _chips.Count;
        public Button Chip(int index) => _chips[index];
        public Slider LengthSlider => _slider;
        public int TickCount => _ticks.Count;
        public string SizeText => _size != null ? _size.text : "";
        public string DepthText => _depth != null ? _depth.text : "";
        public string MakerText => _maker != null ? _maker.text : "";

        void Start() => Build();

        void OnDestroy()
        {
            if (_connected != null) _connected.Changed -= Refresh;
        }

        /// <summary>Sets the sizer in code, before the panel is built.</summary>
        public void Connect(ScrewSizer to) => sizer = to;

        /// <summary>Shows the measured depth to size against.</summary>
        public void ShowDepth(float millimetres)
        {
            depthMm = millimetres;
            Refresh();
        }

        public void HideDepth() => ShowDepth(-1f);

        /// <summary>Makes the canvas and the controls. Called on Start; safe to call again.</summary>
        public void Build()
        {
            if (_panel != null || sizer == null) return;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasObject = new GameObject("Size Panel Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 1f;

            _panel = Rect("Size Panel", canvasObject.transform, 0f, 0f, Width, Height);
            _panel.anchorMin = _panel.anchorMax = _panel.pivot = Vector2.zero;
            _panel.anchoredPosition = new Vector2(Margin, Margin);
            Paint(_panel, PanelColour, true);

            var inner = Width - Padding * 2f;
            var title = Label("Title", _panel, Padding, 12f, 160f, 22f, "Screw size", 16, TitleColour, TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            _maker = Label("Catalog", _panel, Padding + 160f, 12f, inner - 160f, 22f, "", 13, QuietColour, TextAnchor.MiddleRight);

            Label("Diameter Label", _panel, Padding, 44f, 80f, ChipHeight, "Diameter", 14, Color.white, TextAnchor.MiddleLeft);
            _chipRow = Rect("Diameters", _panel, ControlsLeft, 44f, Width - ControlsLeft - Padding, ChipHeight);

            Label("Length Label", _panel, Padding, 88f, 80f, 22f, "Length", 14, Color.white, TextAnchor.MiddleLeft);
            _slider = MakeSlider(_panel, ControlsLeft, 89f);
            _lengthValue = Label("Length Value", _panel, ControlsLeft + SliderWidth + 8f, 88f, Width - ControlsLeft - SliderWidth - 8f - Padding, 22f,
                "", 14, TitleColour, TextAnchor.MiddleRight);
            _tickRow = Rect("Length Ticks", _panel, ControlsLeft, 112f, SliderWidth, 24f);

            _size = Label("Size", _panel, Padding, 142f, inner, 32f, "", 24, Color.white, TextAnchor.MiddleLeft);
            _size.fontStyle = FontStyle.Bold;
            _depth = Label("Depth", _panel, Padding, 178f, inner, 22f, "", 15, DepthColour, TextAnchor.MiddleLeft);

            _connected = sizer;
            _connected.Changed += Refresh;
            Refresh();
        }

        /// <summary>Brings the chips, the ticks and the readouts in line with the sizer.</summary>
        public void Refresh()
        {
            if (_panel == null || sizer == null) return;
            var catalog = sizer.Catalog;
            var specs = sizer.Specs;
            if (catalog != _shownCatalog || _chips.Count != specs.Count) MakeChips(catalog, specs);
            var spec = sizer.Spec;
            if (spec != _shownSpec) MakeTicks(spec);

            var picked = sizer.DiameterIndex;
            for (var i = 0; i < _chips.Count; i++)
            {
                var colour = specs[i] != null ? specs[i].trayColor : Color.grey;
                colour.a = 1f;
                var image = _chips[i].GetComponent<Image>();
                image.color = i == picked ? colour : Color.Lerp(PanelColour, colour, UnpickedChip);
                _chips[i].GetComponent<Outline>().enabled = i == picked;
                _chips[i].GetComponentInChildren<Text>().color = Luminance(image.color) > 0.55f ? DarkText : Color.white;
            }

            _slider.SetValueWithoutNotify(sizer.LengthIndex);
            _maker.text = catalog != null ? catalog.manufacturer : "";
            _lengthValue.text = Millimetres(sizer.LengthMm, "0.#");
            _size.text = sizer.Label;
            _depth.text = depthMm >= 0f ? "Measured depth: " + Millimetres(depthMm, "0.0") : "";
        }

        void MakeChips(ImplantCatalog catalog, IReadOnlyList<ScrewSpec> specs)
        {
            foreach (var chip in _chips) Remove(chip.gameObject);
            _chips.Clear();
            _shownCatalog = catalog;
            if (specs.Count == 0) return;

            var room = _chipRow.sizeDelta.x;
            var width = Mathf.Min(MaxChipWidth, (room - ChipGap * (specs.Count - 1)) / specs.Count);
            for (var i = 0; i < specs.Count; i++)
            {
                var index = i;
                var rect = Rect($"Diameter {i + 1}", _chipRow, i * (width + ChipGap), 0f, width, ChipHeight);
                Paint(rect, Color.white, true);
                var outline = rect.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.white;
                outline.effectDistance = new Vector2(2f, -2f);
                var button = rect.gameObject.AddComponent<Button>();
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => sizer.SelectDiameter(index));
                var text = specs[i] != null ? specs[i].diameterMm.ToString("0.#", CultureInfo.InvariantCulture) : "?";
                var label = Label("Text", rect, 0f, 0f, width, ChipHeight, text, 14, Color.white, TextAnchor.MiddleCenter);
                label.fontStyle = FontStyle.Bold;
                _chips.Add(button);
            }
        }

        void MakeTicks(ScrewSpec spec)
        {
            foreach (var tick in _ticks) Remove(tick);
            _ticks.Clear();
            _shownSpec = spec;
            var lengths = sizer.Lengths;
            // A shorter list would pull the handle back to its new end and report that as a choice.
            // Parking the handle at zero first means changing the range never picks a length.
            _slider.SetValueWithoutNotify(0f);
            _slider.minValue = 0f;
            _slider.maxValue = Mathf.Max(0, lengths.Count - 1);
            if (lengths.Count == 0) return;

            // The handle's centre travels between half a handle from each end, so the ticks do too.
            var travel = SliderWidth - HandleWidth;
            var labelEvery = lengths.Count > MostTickLabels ? 2 : 1;
            for (var i = 0; i < lengths.Count; i++)
            {
                var x = HandleWidth * 0.5f + (lengths.Count > 1 ? travel * i / (lengths.Count - 1) : travel * 0.5f);
                var tick = Rect($"Tick {i + 1}", _tickRow, x - 1f, 0f, 2f, i % labelEvery == 0 ? 7f : 4f);
                Paint(tick, QuietColour, false);
                _ticks.Add(tick.gameObject);
                if (i % labelEvery != 0) continue;
                var text = lengths[i].ToString("0.#", CultureInfo.InvariantCulture);
                var label = Label("Text", tick, -14f, 8f, 30f, 14f, text, 11, QuietColour, TextAnchor.UpperCenter);
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
        }

        Slider MakeSlider(RectTransform parent, float x, float y)
        {
            var root = Rect("Length Slider", parent, x, y, SliderWidth, 20f);
            // An invisible layer over the whole row, so a click anywhere on it moves the slider.
            Paint(root, new Color(0f, 0f, 0f, 0f), true);
            var track = Rect("Track", root, HandleWidth * 0.5f, 7f, SliderWidth - HandleWidth, 6f);
            Paint(track, TrackColour, false);

            var fillArea = Rect("Fill Area", root, HandleWidth * 0.5f, 7f, SliderWidth - HandleWidth, 6f);
            var fill = Stretch("Fill", fillArea);
            Paint(fill, FillColour, false);

            var handleArea = Rect("Handle Area", root, HandleWidth * 0.5f, 0f, SliderWidth - HandleWidth, 20f);
            var handle = Stretch("Handle", handleArea);
            handle.sizeDelta = new Vector2(HandleWidth, 0f);
            var handleImage = Paint(handle, HandleColour, true);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.wholeNumbers = true;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.onValueChanged.AddListener(value => sizer.SelectLength(Mathf.RoundToInt(value)));
            return slider;
        }

        // Placed from the parent's top-left corner, y going down, in the canvas's 720-high units.
        static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        // Fills its parent. The slider then moves the anchors itself.
        static RectTransform Stretch(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        static Image Paint(RectTransform rect, Color colour, bool takesClicks)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = colour;
            image.raycastTarget = takesClicks;
            return image;
        }

        Text Label(string name, Transform parent, float x, float y, float width, float height, string content, int size, Color colour, TextAnchor anchor)
        {
            var text = Rect(name, parent, x, y, width, height).gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.color = colour;
            text.alignment = anchor;
            text.text = content;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        static string Millimetres(float value, string format) =>
            value.ToString(format, CultureInfo.InvariantCulture) + " mm";

        static float Luminance(Color colour) => 0.2126f * colour.r + 0.7152f * colour.g + 0.0722f * colour.b;

        static void Remove(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }
}

using Lab.W01.VariableScrew;
using Unity.Profiling;
using UnityEngine;

namespace Lab.W01.SizingStation
{
    /// <summary>
    /// The box in the bottom-right corner of the Game view that shows what the screws cost. It counts
    /// the screws that are in the scene and measures what they share, a few times a second. One call
    /// switches it to what one mesh per size would take. Drawn with IMGUI, so it needs no canvas.
    /// </summary>
    public class MemoryReadout : MonoBehaviour
    {
        const float ReferenceHeight = 720f;
        const float Width = 372f;
        const float Margin = 12f;
        const float Padding = 12f;
        const float TitleSize = 16f;
        const float LineSize = 14f;
        const float Gap = 3f;

        static readonly Color BackgroundColour = new Color(0.05f, 0.07f, 0.09f, 0.9f);
        static readonly Color TitleColour = new Color(0.55f, 0.85f, 0.9f);
        static readonly Color ComparedTitleColour = new Color(1f, 0.72f, 0.36f);
        static readonly Color LineColour = new Color(0.9f, 0.92f, 0.94f);
        static readonly Color NoteColour = new Color(0.62f, 0.66f, 0.7f);

        [Tooltip("Its catalog decides how many sizes the comparison counts.")]
        [SerializeField] ScrewSizer sizer;
        [SerializeField, Min(0.05f)] float refreshSeconds = 0.25f;
        [SerializeField] string footnote = "M compares with one mesh per size.";
        [SerializeField] string comparedFootnote = "M goes back to the measured numbers.";

        // Unity counts a frame's draw calls in two groups. A renderer with its own property block,
        // which every screw has, is left out of the SRP Batcher and lands in the first group.
        ProfilerRecorder _oneByOne;
        ProfilerRecorder _batched;
        float _nextRefresh;
        GUIStyle _title;
        GUIStyle _line;

        public ScrewMemory Memory { get; private set; }
        public bool Comparing { get; private set; }
        public string Title => Comparing ? MemoryText.ComparedTitle : MemoryText.NowTitle;
        public string[] Lines { get; private set; } = new string[0];
        public long DrawnOneByOne { get; private set; } = -1;
        public long DrawnBatched { get; private set; } = -1;

        void OnEnable()
        {
            _oneByOne = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Standard Draw Calls Count");
            _batched = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SRP Batcher Draw Calls Count");
        }

        void OnDisable()
        {
            if (_oneByOne.Valid) _oneByOne.Dispose();
            if (_batched.Valid) _batched.Dispose();
        }

        /// <summary>Sets the sizer in code.</summary>
        public void Connect(ScrewSizer to)
        {
            sizer = to;
            Refresh();
        }

        /// <summary>Switches between the measured numbers and the one-mesh-per-size sum.</summary>
        public void ToggleComparison()
        {
            Comparing = !Comparing;
            Refresh();
        }

        /// <summary>Counts and measures the screws in the scene now.</summary>
        public void Refresh()
        {
            Memory = MemoryMeter.Measure(FindObjectsByType<Screw>());
            var catalog = sizer != null ? sizer.Catalog : null;
            if (Comparing && catalog != null)
            {
                Lines = MemoryText.Compared(Memory, MemoryMeter.SizesIn(catalog), catalog.manufacturer);
                return;
            }
            DrawnOneByOne = Last(_oneByOne);
            DrawnBatched = Last(_batched);
            Lines = MemoryText.Now(Memory, DrawnOneByOne, DrawnBatched);
        }

        // The last whole frame's count, or -1 before a frame has been counted.
        static long Last(ProfilerRecorder recorder) => recorder.Valid && recorder.Count > 0 ? recorder.LastValue : -1;

        void Update()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + refreshSeconds;
            Refresh();
        }

        void OnGUI()
        {
            var scale = Screen.height / ReferenceHeight;
            _title ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperLeft };
            _line ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, wordWrap = true };
            _title.fontSize = Mathf.RoundToInt(TitleSize * scale);
            _line.fontSize = Mathf.RoundToInt(LineSize * scale);

            var width = Width * scale;
            var padding = Padding * scale;
            var gap = Gap * scale;
            var inner = width - padding * 2f;
            var note = Comparing ? comparedFootnote : footnote;

            var titleHeight = _title.CalcHeight(new GUIContent(Title), inner);
            var height = padding * 2f + titleHeight + gap * 2f;
            foreach (var line in Lines) height += _line.CalcHeight(new GUIContent(line), inner) + gap;
            var noteHeight = string.IsNullOrEmpty(note) ? 0f : _line.CalcHeight(new GUIContent(note), inner);
            height += noteHeight;

            var box = new Rect(Screen.width - width - Margin * scale, Screen.height - height - Margin * scale, width, height);
            var previous = GUI.color;
            GUI.color = BackgroundColour;
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = previous;

            var y = box.y + padding;
            _title.normal.textColor = Comparing ? ComparedTitleColour : TitleColour;
            GUI.Label(new Rect(box.x + padding, y, inner, titleHeight), Title, _title);
            y += titleHeight + gap * 2f;

            _line.normal.textColor = LineColour;
            foreach (var line in Lines)
            {
                var lineHeight = _line.CalcHeight(new GUIContent(line), inner);
                GUI.Label(new Rect(box.x + padding, y, inner, lineHeight), line, _line);
                y += lineHeight + gap;
            }

            if (noteHeight <= 0f) return;
            _line.normal.textColor = NoteColour;
            GUI.Label(new Rect(box.x + padding, y, inner, noteHeight), note, _line);
        }
    }
}

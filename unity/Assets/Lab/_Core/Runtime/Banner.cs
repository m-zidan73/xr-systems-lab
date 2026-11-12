using UnityEngine;

namespace Lab.Core
{
    /// <summary>
    /// A large title in the Game view for moments that matter, such as a step being finished, with a line
    /// underneath for what happens next. It appears at once and stays until hidden. Drawn with IMGUI like
    /// <see cref="StatusLine"/>, so it needs no canvas.
    /// </summary>
    public class Banner : MonoBehaviour
    {
        const float ReferenceHeight = 720f;
        const float Width = 520f;
        const float TitleSize = 32f;
        const float SubtitleSize = 16f;
        const float Padding = 16f;
        const float AccentHeight = 4f;

        static readonly Color BackgroundColour = new Color(0.05f, 0.07f, 0.09f, 0.92f);
        static readonly Color SubtitleColour = new Color(0.82f, 0.86f, 0.9f);

        [SerializeField] Color accent = new Color(0.3f, 1f, 0.5f);
        [Tooltip("Where the banner's centre sits across the view, from 0 (left) to 1 (right).")]
        [SerializeField, Range(0f, 1f)] float horizontalCentre = 0.5f;
        [Tooltip("Where the banner's top edge sits down the view, from 0 (top) to 1 (bottom).")]
        [SerializeField, Range(0f, 1f)] float topEdge = 0.2f;

        GUIStyle _titleStyle;
        GUIStyle _subtitleStyle;

        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public bool IsShowing { get; private set; }
        public Color Accent => accent;

        public void Show(string title, string subtitle)
        {
            Title = title;
            Subtitle = subtitle;
            IsShowing = true;
        }

        public void Hide() => IsShowing = false;

        void OnGUI()
        {
            if (!IsShowing) return;
            var scale = Screen.height / ReferenceHeight;
            _titleStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold, wordWrap = true };
            _subtitleStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, wordWrap = true };
            _titleStyle.fontSize = Mathf.RoundToInt(TitleSize * scale);
            _titleStyle.normal.textColor = Color.Lerp(accent, Color.white, 0.35f);
            _subtitleStyle.fontSize = Mathf.RoundToInt(SubtitleSize * scale);
            _subtitleStyle.normal.textColor = SubtitleColour;

            var width = Width * scale;
            var padding = Padding * scale;
            var inner = width - padding * 2f;
            var titleHeight = _titleStyle.CalcHeight(new GUIContent(Title), inner);
            var subtitleHeight = string.IsNullOrEmpty(Subtitle) ? 0f : _subtitleStyle.CalcHeight(new GUIContent(Subtitle), inner);
            var height = padding * 2f + titleHeight + (subtitleHeight > 0f ? subtitleHeight + 4f * scale : 0f);
            var left = Mathf.Clamp(Screen.width * horizontalCentre - width * 0.5f, 0f, Mathf.Max(0f, Screen.width - width));
            var top = Mathf.Clamp(Screen.height * topEdge, 0f, Mathf.Max(0f, Screen.height - height));
            var box = new Rect(left, top, width, height);

            Fill(box, BackgroundColour);
            Fill(new Rect(box.x, box.y, box.width, AccentHeight * scale), accent);
            GUI.Label(new Rect(box.x + padding, box.y + padding, inner, titleHeight), Title, _titleStyle);
            if (subtitleHeight > 0f)
                GUI.Label(new Rect(box.x + padding, box.y + padding + titleHeight + 4f * scale, inner, subtitleHeight), Subtitle, _subtitleStyle);
        }

        static void Fill(Rect rect, Color colour)
        {
            var previous = GUI.color;
            GUI.color = colour;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}

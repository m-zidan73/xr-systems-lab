using UnityEngine;

namespace Lab.W01.StepGuidance
{
    /// <summary>
    /// The procedure in the top-right corner of the Game view: every step, with the open one picked out,
    /// the mistake count, and the latest mistake on the open step in red. Drawn with IMGUI, so it needs
    /// no canvas, and it reads the guide each frame, so it never falls out of step.
    /// </summary>
    public class StepPanel : MonoBehaviour
    {
        const float ReferenceHeight = 720f;
        const float Width = 380f;
        const float Margin = 12f;
        const float Padding = 12f;
        const float HeaderSize = 18f;
        const float LineSize = 15f;
        const float Gap = 6f;
        const string DoneMark = "✓";
        const string OpenMark = "→";

        static readonly Color BackgroundColour = new Color(0.05f, 0.07f, 0.09f, 0.88f);
        static readonly Color HeaderColour = new Color(0.55f, 0.85f, 0.9f);
        static readonly Color DoneColour = new Color(0.5f, 0.82f, 0.58f);
        static readonly Color OpenColour = Color.white;
        static readonly Color LaterColour = new Color(0.58f, 0.62f, 0.66f);
        static readonly Color CountColour = new Color(0.75f, 0.78f, 0.82f);
        static readonly Color MistakeColour = new Color(1f, 0.48f, 0.42f);

        [SerializeField] StepGuide guide;

        GUIStyle _header;
        GUIStyle _count;
        GUIStyle _mark;
        GUIStyle _line;

        public StepGuide Guide => guide;

        void OnGUI()
        {
            if (guide == null) return;
            var scale = Screen.height / ReferenceHeight;
            MakeStyles(scale);

            var width = Width * scale;
            var padding = Padding * scale;
            var gap = Gap * scale;
            var markWidth = 20f * scale;
            var inner = width - padding * 2f;
            var textWidth = inner - markWidth;

            var header = StepMessages.Header(guide.CurrentIndex, guide.StepCount);
            var count = StepMessages.MistakeCount(guide.Mistakes.Count);
            var headerHeight = _header.CalcHeight(new GUIContent(header), inner);
            var lineHeights = new float[guide.StepCount];
            var height = padding * 2f + headerHeight + gap;
            for (var i = 0; i < guide.StepCount; i++)
            {
                // Measured in the style it is drawn in: the open step is bold, so it can wrap sooner.
                _line.fontStyle = i == guide.CurrentIndex ? FontStyle.Bold : FontStyle.Normal;
                lineHeights[i] = _line.CalcHeight(new GUIContent(LineText(i)), textWidth);
                height += lineHeights[i] + gap * 0.5f;
            }
            var mistake = guide.CurrentStepMistake;
            _line.fontStyle = FontStyle.Bold;
            var mistakeHeight = mistake.HasValue ? _line.CalcHeight(new GUIContent(mistake.Value.Detail), inner) : 0f;
            if (mistake.HasValue) height += gap + mistakeHeight;

            var box = new Rect(Screen.width - width - Margin * scale, Margin * scale, width, height);
            Fill(box, BackgroundColour);

            var y = box.y + padding;
            _header.normal.textColor = guide.IsDone ? DoneColour : HeaderColour;
            GUI.Label(new Rect(box.x + padding, y, inner, headerHeight), header, _header);
            _count.normal.textColor = guide.Mistakes.Count > 0 ? MistakeColour : CountColour;
            GUI.Label(new Rect(box.x + padding, y, inner, headerHeight), count, _count);
            y += headerHeight + gap;

            for (var i = 0; i < guide.StepCount; i++)
            {
                var open = i == guide.CurrentIndex;
                var done = !open && guide.IsFinished(i);
                var colour = open ? OpenColour : done ? DoneColour : LaterColour;
                _mark.normal.textColor = colour;
                _line.normal.textColor = colour;
                _line.fontStyle = open ? FontStyle.Bold : FontStyle.Normal;
                GUI.Label(new Rect(box.x + padding, y, markWidth, lineHeights[i]), open ? OpenMark : done ? DoneMark : "", _mark);
                GUI.Label(new Rect(box.x + padding + markWidth, y, textWidth, lineHeights[i]), LineText(i), _line);
                y += lineHeights[i] + gap * 0.5f;
            }

            if (!mistake.HasValue) return;
            y += gap * 0.5f;
            _line.normal.textColor = MistakeColour;
            _line.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(box.x + padding, y, inner, mistakeHeight), mistake.Value.Detail, _line);
        }

        string LineText(int index) => $"{index + 1}. {StepMessages.Instruction(guide.Steps[index])}";

        void MakeStyles(float scale)
        {
            _header ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperLeft };
            _count ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight };
            _mark ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft };
            _line ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, wordWrap = true };
            _header.fontSize = Mathf.RoundToInt(HeaderSize * scale);
            _count.fontSize = Mathf.RoundToInt(LineSize * scale);
            _mark.fontSize = Mathf.RoundToInt(LineSize * scale);
            _line.fontSize = Mathf.RoundToInt(LineSize * scale);
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
